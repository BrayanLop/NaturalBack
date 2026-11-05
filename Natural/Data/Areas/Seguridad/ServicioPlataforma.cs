using Microsoft.EntityFrameworkCore;
using Natural.Common;
using Natural.Data.Domain;
using Natural.Data.Domain.Estructura;
using Natural.Models.Areas.Seguridad;

namespace Natural.Data.Areas.Seguridad
{
    /// <summary>
    /// Operaciones de plataforma (alta de empresas, activar/desactivar, soporte). Las llama el microservicio
    /// de plataforma, autenticado con la clave de servicio; no hay una empresa "de sesión", llega por parámetro.
    /// </summary>
    public class ServicioPlataforma
    {
        private readonly NaturalDbContext _context;
        private readonly ServicioAcceso _servicioAcceso;
        private readonly ServicioSeguridad _servicioSeguridad;

        public ServicioPlataforma(NaturalDbContext context, ServicioAcceso servicioAcceso, ServicioSeguridad servicioSeguridad)
        {
            _context = context;
            _servicioAcceso = servicioAcceso;
            _servicioSeguridad = servicioSeguridad;
        }

        public async Task<List<EmpresaPlataformaViewModel>> ListarEmpresasAsync()
        {
            var empresas = await _context.Empresa.AsNoTracking().OrderBy(e => e.Id).ToListAsync();

            var personas = await _context.Persona.AsNoTracking()
                .Where(p => p.EmpresaId != null)
                .GroupBy(p => p.EmpresaId!.Value)
                .Select(g => new { EmpresaId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.EmpresaId, x => x.Total);

            var accesos = await _context.Usuario.AsNoTracking()
                .Where(u => u.Persona.EmpresaId != null)
                .GroupBy(u => u.Persona.EmpresaId!.Value)
                .Select(g => new { EmpresaId = g.Key, Total = g.Count(), Activos = g.Count(u => u.Estado) })
                .ToDictionaryAsync(x => x.EmpresaId);

            return empresas.Select(e => new EmpresaPlataformaViewModel
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Nit = e.Nit,
                Direccion = e.Direccion,
                Telefono = e.Telefono,
                Activa = e.Estado != false,
                TotalPersonas = personas.GetValueOrDefault(e.Id),
                TotalAccesos = accesos.TryGetValue(e.Id, out var a) ? a.Total : 0,
                AccesosActivos = accesos.TryGetValue(e.Id, out var b) ? b.Activos : 0
            }).ToList();
        }

        /// <summary>Crea la empresa y su primer administrador (Persona + Usuario + roles) en una sola transacción.</summary>
        public async Task<Resultado<EmpresaPlataformaViewModel>> CrearEmpresaAsync(CrearEmpresaRequest? request)
        {
            if (request is null)
                return Resultado<EmpresaPlataformaViewModel>.Invalido("Debe enviar los datos de la empresa.");

            var nombre = (request.Nombre ?? "").Trim();
            if (nombre.Length is < 2 or > 100)
                return Resultado<EmpresaPlataformaViewModel>.Invalido("El nombre de la empresa es obligatorio (máx. 100 caracteres).");

            var admin = request.Admin;
            if (admin is null
                || string.IsNullOrWhiteSpace(admin.Nombre)
                || string.IsNullOrWhiteSpace(admin.Apellido)
                || string.IsNullOrWhiteSpace(admin.Cedula)
                || string.IsNullOrWhiteSpace(admin.Email)
                || string.IsNullOrWhiteSpace(admin.Celular))
                return Resultado<EmpresaPlataformaViewModel>.Invalido("Debe enviar los datos del administrador de la empresa.");

            var nombreUsuario = (admin.NombreUsuario ?? "").Trim();
            var error = ServicioAcceso.ValidarNuevoUsuario(nombreUsuario, admin.Contrasena, Roles.Administrador);
            if (error is not null)
                return Resultado<EmpresaPlataformaViewModel>.Invalido(error);

            if (await _servicioAcceso.NombreUsuarioEnUsoAsync(nombreUsuario))
                return Resultado<EmpresaPlataformaViewModel>.Conflicto("Ese nombre de usuario ya está en uso.");

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            // El Id de Empresa no es autogenerado: se toma el siguiente.
            var siguienteId = (await _context.Empresa.MaxAsync(e => (int?)e.Id) ?? 0) + 1;

            var empresa = new Empresa
            {
                Id = siguienteId,
                Nombre = nombre,
                Nit = request.Nit?.Trim(),
                Direccion = request.Direccion?.Trim(),
                Telefono = request.Telefono?.Trim(),
                Estado = true
            };
            _context.Empresa.Add(empresa);

            // Edad y fecha de nacimiento no se piden al dar de alta: el administrador los completa en Personas.
            var persona = new Persona
            {
                Nombre = admin.Nombre!.Trim(),
                Apellido = admin.Apellido!.Trim(),
                Cedula = admin.Cedula!.Trim(),
                Email = admin.Email!.Trim(),
                Celular = admin.Celular!.Trim(),
                Edad = 0,
                FechaNacimiento = DateTime.UtcNow,
                EmpresaId = empresa.Id
            };
            _context.Persona.Add(persona);
            await _context.SaveChangesAsync();

            await _servicioAcceso.CrearUsuarioAsync(persona, nombreUsuario, admin.Contrasena!, Roles.Administrador);
            await transaccion.CommitAsync();

            return Resultado<EmpresaPlataformaViewModel>.Exito(new EmpresaPlataformaViewModel
            {
                Id = empresa.Id,
                Nombre = empresa.Nombre,
                Nit = empresa.Nit,
                Direccion = empresa.Direccion,
                Telefono = empresa.Telefono,
                Activa = true,
                TotalPersonas = 1,
                TotalAccesos = 1,
                AccesosActivos = 1
            });
        }

        public async Task<Resultado<bool>> ActualizarEmpresaAsync(int empresaId, EmpresaRequest? request)
        {
            var nombre = (request?.Nombre ?? "").Trim();
            if (request is null || nombre.Length is < 2 or > 100)
                return Resultado<bool>.Invalido("El nombre de la empresa es obligatorio (máx. 100 caracteres).");

            var empresa = await _context.Empresa.FirstOrDefaultAsync(e => e.Id == empresaId);
            if (empresa is null)
                return Resultado<bool>.NoEncontrado("La empresa no existe.");

            empresa.Nombre = nombre;
            empresa.Nit = request.Nit?.Trim();
            empresa.Direccion = request.Direccion?.Trim();
            empresa.Telefono = request.Telefono?.Trim();
            await _context.SaveChangesAsync();

            return Resultado<bool>.Exito(true);
        }

        /// <summary>Activa o desactiva la empresa: desactivada, nadie de ella puede ingresar ni usar tokens ya emitidos.</summary>
        public async Task<Resultado<bool>> CambiarEstadoEmpresaAsync(int empresaId, bool activa)
        {
            var empresa = await _context.Empresa.FirstOrDefaultAsync(e => e.Id == empresaId);
            if (empresa is null)
                return Resultado<bool>.NoEncontrado("La empresa no existe.");

            empresa.Estado = activa;
            await _context.SaveChangesAsync();

            var usuarioIds = await _context.Usuario
                .Where(u => u.Persona.EmpresaId == empresaId)
                .Select(u => u.Id)
                .ToListAsync();
            _servicioAcceso.InvalidarCacheEmpresa(usuarioIds);

            return Resultado<bool>.Exito(true);
        }

        /// <summary>Token temporal como administrador de la empresa, para soporte. Exige un administrador activo y la empresa activa.</summary>
        public async Task<Resultado<object>> ImpersonarAsync(int empresaId, ImpersonarRequest? request)
        {
            var actor = (request?.Actor ?? "").Trim();
            if (actor.Length == 0)
                return Resultado<object>.Invalido("Debe indicar quién solicita el acceso.");

            if (!await _servicioAcceso.EmpresaActivaAsync(empresaId))
                return Resultado<object>.Invalido("La empresa está inactiva.");

            var admin = await (from r in _context.UsuarioRol
                               join u in _context.Usuario on r.UsuarioId equals u.Id
                               where r.RolCodigo == Roles.Administrador && u.Estado && u.Persona.EmpresaId == empresaId
                               orderby u.Id
                               select new { UsuarioId = u.Id, PersonaId = u.PersonaId, u.Persona.Nombre })
                              .FirstOrDefaultAsync();
            if (admin is null)
                return Resultado<object>.NoEncontrado("La empresa no tiene un administrador activo.");

            var empresa = await _context.Empresa.AsNoTracking().FirstAsync(e => e.Id == empresaId);
            var minutos = request!.Minutos <= 0 ? 30 : request.Minutos;
            var (token, expiraEn) = _servicioSeguridad.GenerarTokenImpersonacion(
                admin.PersonaId, admin.Nombre, empresaId, admin.UsuarioId, actor, minutos);

            return Resultado<object>.Exito(new
            {
                token,
                expiraEn,
                persona = new
                {
                    id = admin.PersonaId,
                    idUsuario = admin.UsuarioId,
                    nombre = admin.Nombre,
                    empresaId,
                    nombreEmpresa = empresa.Nombre,
                    rol = Roles.Administrador
                }
            });
        }
    }
}
