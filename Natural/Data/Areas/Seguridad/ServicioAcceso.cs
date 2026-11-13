using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Natural.Common;
using Natural.Data.Domain;
using Natural.Data.Domain.Estructura;
using Natural.Data.Domain.Sesion;
using Natural.Models.Areas.Seguridad;

namespace Natural.Data.Areas.Seguridad
{
    /// <summary>
    /// Gestión de accesos (Usuario + UsuarioRol + PersonaRol) de una empresa. Lo usan el administrador de la
    /// empresa (empresa tomada de su JWT) y la plataforma (empresa elegida por el superadmin).
    /// Toda operación recibe el <c>empresaId</c> y nunca toca usuarios de otra empresa.
    /// </summary>
    public class ServicioAcceso
    {
        private const int LongitudHashCliente = 44;
        private static readonly TimeSpan VigenciaCacheActivo = TimeSpan.FromSeconds(60);

        private readonly NaturalDbContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;
        private readonly OpcionesSeguridad _opciones;
        private readonly IMemoryCache _cache;

        public ServicioAcceso(
            NaturalDbContext context,
            IPasswordHasher<Usuario> passwordHasher,
            IOptions<OpcionesSeguridad> opciones,
            IMemoryCache cache)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _opciones = opciones.Value;
            _cache = cache;
        }

        #region Consulta

        public async Task<List<AccesoViewModel>> ListarAsync(int empresaId)
        {
            var personas = await _context.Persona
                .AsNoTracking()
                .Where(p => p.EmpresaId == empresaId)
                .OrderBy(p => p.Nombre).ThenBy(p => p.Apellido)
                .Select(p => new { p.Id, p.Nombre, p.Apellido, p.Email })
                .ToListAsync();

            var personaIds = personas.Select(p => p.Id).ToList();

            var usuarios = await _context.Usuario
                .AsNoTracking()
                .Where(u => personaIds.Contains(u.PersonaId))
                .Select(u => new { u.Id, u.NombreUsuario, u.Estado, u.PersonaId })
                .ToListAsync();

            var usuarioIds = usuarios.Select(u => u.Id).ToList();
            var rolesUsuario = await _context.UsuarioRol
                .AsNoTracking()
                .Where(r => usuarioIds.Contains(r.UsuarioId))
                .ToListAsync();

            var lista = new List<AccesoViewModel>();
            foreach (var persona in personas)
            {
                var susUsuarios = usuarios.Where(u => u.PersonaId == persona.Id).OrderBy(u => u.Id).ToList();
                if (susUsuarios.Count == 0)
                {
                    lista.Add(new AccesoViewModel
                    {
                        PersonaId = persona.Id,
                        Nombre = persona.Nombre,
                        Apellido = persona.Apellido,
                        Email = persona.Email
                    });
                    continue;
                }

                foreach (var usuario in susUsuarios)
                {
                    lista.Add(new AccesoViewModel
                    {
                        PersonaId = persona.Id,
                        Nombre = persona.Nombre,
                        Apellido = persona.Apellido,
                        Email = persona.Email,
                        UsuarioId = usuario.Id,
                        NombreUsuario = usuario.NombreUsuario,
                        TieneAcceso = true,
                        Activo = usuario.Estado,
                        Rol = rolesUsuario.Where(r => r.UsuarioId == usuario.Id).Select(r => r.RolCodigo).FirstOrDefault()
                    });
                }
            }

            return lista;
        }

        /// <summary>Empresa activa (Estado null se considera activa, como las empresas existentes).</summary>
        public async Task<bool> EmpresaActivaAsync(int empresaId)
        {
            var estado = await _context.Empresa
                .AsNoTracking()
                .Where(e => e.Id == empresaId)
                .Select(e => e.Estado)
                .FirstOrDefaultAsync();

            return estado != false;
        }

        /// <summary>
        /// ¿Puede seguir usándose el token de este usuario? Falso si el usuario o su empresa se desactivaron
        /// o el usuario se eliminó. El resultado se guarda 60 s para no consultar la BD en cada petición.
        /// </summary>
        public async Task<bool> UsuarioVigenteAsync(int usuarioId)
        {
            var clave = $"usuario-vigente:{usuarioId}";
            if (_cache.TryGetValue(clave, out bool vigente))
                return vigente;

            var datos = await _context.Usuario
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => new { u.Estado, EmpresaId = u.Persona.EmpresaId, EmpresaEstado = u.Persona.Empresa == null ? null : u.Persona.Empresa.Estado })
                .FirstOrDefaultAsync();

            vigente = datos is not null && datos.Estado && datos.EmpresaEstado != false;
            _cache.Set(clave, vigente, VigenciaCacheActivo);
            return vigente;
        }

        #endregion Consulta

        #region Acciones sobre accesos

        public async Task<Resultado<AccesoViewModel>> CrearAsync(int empresaId, CrearAccesoRequest? request)
        {
            if (request is null)
                return Resultado<AccesoViewModel>.Invalido("Debe enviar los datos del acceso.");

            var nombreUsuario = (request.NombreUsuario ?? "").Trim();
            var validacion = ValidarNuevoUsuario(nombreUsuario, request.Contrasena, request.Rol);
            if (validacion is not null)
                return Resultado<AccesoViewModel>.Invalido(validacion);

            var persona = await _context.Persona
                .FirstOrDefaultAsync(p => p.Id == request.PersonaId && p.EmpresaId == empresaId);
            if (persona is null)
                return Resultado<AccesoViewModel>.NoEncontrado("La persona no existe en esta empresa.");

            if (await _context.Usuario.AnyAsync(u => u.PersonaId == persona.Id))
                return Resultado<AccesoViewModel>.Conflicto("La persona ya tiene un acceso.");

            if (await NombreUsuarioEnUsoAsync(nombreUsuario))
                return Resultado<AccesoViewModel>.Conflicto("Ese nombre de usuario ya está en uso.");

            var usuario = await CrearUsuarioAsync(persona, nombreUsuario, request.Contrasena!, request.Rol!);

            return Resultado<AccesoViewModel>.Exito(new AccesoViewModel
            {
                PersonaId = persona.Id,
                Nombre = persona.Nombre,
                Apellido = persona.Apellido,
                Email = persona.Email,
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                TieneAcceso = true,
                Activo = true,
                Rol = request.Rol
            });
        }

        public async Task<Resultado<bool>> CambiarEstadoAsync(int empresaId, int usuarioId, bool activo, int? personaSesionId)
        {
            var usuario = await ObtenerUsuarioDeEmpresaAsync(empresaId, usuarioId);
            if (usuario is null)
                return Resultado<bool>.NoEncontrado("El usuario no existe en esta empresa.");

            if (!activo)
            {
                if (personaSesionId == usuario.PersonaId)
                    return Resultado<bool>.Invalido("No puedes desactivar tu propio acceso.");

                if (await EsUltimoAdminActivoAsync(empresaId, usuario))
                    return Resultado<bool>.Invalido("No se puede desactivar al único administrador activo de la empresa.");
            }

            usuario.Estado = activo;
            await _context.SaveChangesAsync();
            InvalidarCache(usuario.Id);

            return Resultado<bool>.Exito(true);
        }

        public async Task<Resultado<bool>> CambiarRolAsync(int empresaId, int usuarioId, string? rol, int? personaSesionId)
        {
            if (!EsRolAsignable(rol))
                return Resultado<bool>.Invalido("El rol no es válido.");

            var usuario = await ObtenerUsuarioDeEmpresaAsync(empresaId, usuarioId);
            if (usuario is null)
                return Resultado<bool>.NoEncontrado("El usuario no existe en esta empresa.");

            if (personaSesionId == usuario.PersonaId)
                return Resultado<bool>.Invalido("No puedes cambiar tu propio rol.");

            if (rol != Roles.Administrador && await EsUltimoAdminActivoAsync(empresaId, usuario))
                return Resultado<bool>.Invalido("No se puede quitar el rol al único administrador activo de la empresa.");

            await using var transaccion = await IniciarTransaccionAsync();

            var rolesUsuario = await _context.UsuarioRol.Where(r => r.UsuarioId == usuario.Id).ToListAsync();
            _context.UsuarioRol.RemoveRange(rolesUsuario);
            _context.UsuarioRol.Add(new UsuarioRol { UsuarioId = usuario.Id, RolCodigo = rol! });

            await ReemplazarRolPersonaAsync(usuario.PersonaId, rol!);
            await _context.SaveChangesAsync();
            if (transaccion is not null) await transaccion.CommitAsync();

            return Resultado<bool>.Exito(true);
        }

        public async Task<Resultado<bool>> RestablecerContrasenaAsync(int empresaId, int usuarioId, string? nuevaContrasena)
        {
            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length != LongitudHashCliente)
                return Resultado<bool>.Invalido("La contraseña debe enviarse en formato correcto.");

            var usuario = await ObtenerUsuarioDeEmpresaAsync(empresaId, usuarioId);
            if (usuario is null)
                return Resultado<bool>.NoEncontrado("El usuario no existe en esta empresa.");

            usuario.ClaveHash = HashClave(usuario, nuevaContrasena);
            await _context.SaveChangesAsync();

            return Resultado<bool>.Exito(true);
        }

        /// <summary>Quita el acceso (Usuario y sus roles). La Persona y su historial de servicios se conservan.</summary>
        public async Task<Resultado<bool>> EliminarAsync(int empresaId, int usuarioId, int? personaSesionId)
        {
            var usuario = await ObtenerUsuarioDeEmpresaAsync(empresaId, usuarioId);
            if (usuario is null)
                return Resultado<bool>.NoEncontrado("El usuario no existe en esta empresa.");

            if (personaSesionId == usuario.PersonaId)
                return Resultado<bool>.Invalido("No puedes eliminar tu propio acceso.");

            if (await EsUltimoAdminActivoAsync(empresaId, usuario))
                return Resultado<bool>.Invalido("No se puede eliminar al único administrador activo de la empresa.");

            await using var transaccion = await IniciarTransaccionAsync();

            var rolesUsuario = await _context.UsuarioRol.Where(r => r.UsuarioId == usuario.Id).ToListAsync();
            _context.UsuarioRol.RemoveRange(rolesUsuario);
            _context.Usuario.Remove(usuario);
            await _context.SaveChangesAsync();
            if (transaccion is not null) await transaccion.CommitAsync();

            InvalidarCache(usuario.Id);

            return Resultado<bool>.Exito(true);
        }

        #endregion Acciones sobre accesos

        #region Reutilizable por la plataforma

        /// <summary>Crea Usuario + UsuarioRol + PersonaRol de una persona ya guardada (o en el contexto). Valida antes con ValidarNuevoUsuario.</summary>
        internal async Task<Usuario> CrearUsuarioAsync(Persona persona, string nombreUsuario, string contrasenaCliente, string rol)
        {
            await using var transaccion = await IniciarTransaccionAsync();

            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Estado = true,
                Persona = persona
            };
            usuario.ClaveHash = HashClave(usuario, contrasenaCliente);

            _context.Usuario.Add(usuario);
            await _context.SaveChangesAsync(); // genera el Id del usuario

            _context.UsuarioRol.Add(new UsuarioRol { UsuarioId = usuario.Id, RolCodigo = rol });
            await ReemplazarRolPersonaAsync(persona.Id, rol);
            await _context.SaveChangesAsync();
            if (transaccion is not null) await transaccion.CommitAsync();

            return usuario;
        }

        /// <summary>Devuelve el mensaje de error o null si los datos son válidos.</summary>
        internal static string? ValidarNuevoUsuario(string nombreUsuario, string? contrasenaCliente, string? rol)
        {
            if (nombreUsuario.Length is < 3 or > 50)
                return "El nombre de usuario debe tener entre 3 y 50 caracteres.";

            if (nombreUsuario.Any(char.IsWhiteSpace))
                return "El nombre de usuario no puede contener espacios.";

            if (string.IsNullOrWhiteSpace(contrasenaCliente) || contrasenaCliente.Length != LongitudHashCliente)
                return "La contraseña debe enviarse en formato correcto.";

            if (!EsRolAsignable(rol))
                return "El rol no es válido.";

            return null;
        }

        internal Task<bool> NombreUsuarioEnUsoAsync(string nombreUsuario)
        {
            var minusculas = nombreUsuario.ToLower();
            return _context.Usuario.AnyAsync(u => u.NombreUsuario.ToLower() == minusculas);
        }

        internal void InvalidarCache(int usuarioId) => _cache.Remove($"usuario-vigente:{usuarioId}");

        internal void InvalidarCacheEmpresa(IEnumerable<int> usuarioIds)
        {
            foreach (var id in usuarioIds) InvalidarCache(id);
        }

        #endregion Reutilizable por la plataforma

        #region Privado

        private static bool EsRolAsignable(string? rol) => rol is not null && Roles.AsignablesEnEmpresa.Contains(rol);

        /// <summary>Sin migración activa se guarda el formato antiguo (SHA-256 del cliente), el que entienden las versiones anteriores del API.</summary>
        private string HashClave(Usuario usuario, string contrasenaCliente) =>
            _opciones.MigrarHashContrasenas
                ? _passwordHasher.HashPassword(usuario, contrasenaCliente)
                : contrasenaCliente;

        private Task<Usuario?> ObtenerUsuarioDeEmpresaAsync(int empresaId, int usuarioId) =>
            _context.Usuario.FirstOrDefaultAsync(u => u.Id == usuarioId && u.Persona.EmpresaId == empresaId);

        /// <summary>¿Es administrador activo y no queda otro administrador activo en la empresa?</summary>
        private async Task<bool> EsUltimoAdminActivoAsync(int empresaId, Usuario usuario)
        {
            if (!usuario.Estado)
                return false;

            var esAdmin = await _context.UsuarioRol.AnyAsync(r => r.UsuarioId == usuario.Id && r.RolCodigo == Roles.Administrador);
            if (!esAdmin)
                return false;

            var otrosAdminsActivos = await (from r in _context.UsuarioRol
                                            join u in _context.Usuario on r.UsuarioId equals u.Id
                                            where r.RolCodigo == Roles.Administrador
                                                  && u.Estado
                                                  && u.Id != usuario.Id
                                                  && u.Persona.EmpresaId == empresaId
                                            select u.Id).AnyAsync();
            return !otrosAdminsActivos;
        }

        private async Task ReemplazarRolPersonaAsync(int personaId, string rol)
        {
            var actuales = await _context.PersonaRol.Where(r => r.PersonaId == personaId).ToListAsync();
            if (actuales.Count == 1 && actuales[0].RolCodigo == rol)
                return;

            _context.PersonaRol.RemoveRange(actuales);
            _context.PersonaRol.Add(new PersonaRol { PersonaId = personaId, RolCodigo = rol });
        }

        /// <summary>Abre transacción solo si no hay una externa (p. ej. la creación de empresa + admin).</summary>
        private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> IniciarTransaccionAsync() =>
            _context.Database.CurrentTransaction is null ? await _context.Database.BeginTransactionAsync() : null;

        #endregion Privado
    }
}
