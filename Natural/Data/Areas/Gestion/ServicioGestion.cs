using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain.Gestion;
using Natural.Models.Areas.Gestion;
using Natural.Data.Domain;

namespace Natural.Data.Areas.Gestion
{
    public class ServicioGestion
    {
        private readonly NaturalDbContext _context;

        public ServicioGestion(NaturalDbContext context)
        {
            _context = context;
        }

        // Cambiar el tipo de retorno de bool a List<int>
        public async Task<List<int>> GuardarRegistrosServicioAsync(List<RegistroServicioViewModel> registros)
        {
            try
            {
                var fechaActual = DateTime.UtcNow;

                var registrosEntidad = registros
                    .Select(r => new RegistroServicio
                    {
                        PersonaId = r.PersonaId,
                        ServicioId = r.ServicioId,
                        EmpresaId = r.EmpresaId,
                        FechaServicio = r.FechaServicio ?? fechaActual,
                        Liquidado = false,
                        Confirmado = r.Confirmado,
                        FormaPago = r.FormaPago
                    })
                    .OrderBy(r => r.FechaServicio)
                    .ToList();

                await _context.RegistroServicio.AddRangeAsync(registrosEntidad);
                await _context.SaveChangesAsync();

                // Retornar los IDs de los registros guardados
                return registrosEntidad.Select(r => r.Id).ToList();
            }
            catch (Exception)
            {
                return new List<int>(); // Lista vacía en caso de error
            }
        }

        public async Task<List<RegistroServicioViewModel>> ObtenerRegistrosAsync(int empresaId)
        {
            return await _context.RegistroServicio
                .Where(r => r.EmpresaId == empresaId)
                .Include(r => r.Persona)
                .Include(r => r.Servicio)
                .Select(r => new RegistroServicioViewModel
                {
                    Id = r.Id,
                    PersonaId = r.PersonaId,
                    ServicioId = r.ServicioId,
                    EmpresaId = r.EmpresaId,
                    NombrePersona = r.Persona != null ? r.Persona.Nombre + " " + r.Persona.Apellido : null,
                    NombreServicio = r.Servicio != null ? r.Servicio.Nombre : null,
                    FechaServicio = r.FechaServicio,
                    FormaPago = r.FormaPago,
                })
                .ToListAsync();
        }

        public async Task<List<RegistroServicioViewModel>> ObtenerRegistrosFechaActualAsync(int empresaId, int personaId, DateTime fechaInicio, DateTime fechaFin)
        {
            var (inicioRango, finRango) = NormalizarRangoUtc(fechaInicio, fechaFin);
            var query = ConstruirQueryPorRango(empresaId, inicioRango, finRango);

            var esAdministrador = await PersonaEsAdministradorAsync(personaId);
            if (!esAdministrador)
            {
                query = query.Where(r => r.PersonaId == personaId);
            }

            return await query
                .Include(r => r.Persona)
                .Include(r => r.Servicio)
                .Include(r => r.Evidencias) // Agregar esto
                .Select(r => new RegistroServicioViewModel
                {
                    Id = r.Id,
                    PersonaId = r.PersonaId,
                    ServicioId = r.ServicioId,
                    EmpresaId = r.EmpresaId,
                    NombrePersona = r.Persona != null
                        ? r.Persona.Nombre + " " + r.Persona.Apellido
                        : null,
                    NombreServicio = r.Servicio != null
                        ? r.Servicio.Nombre
                        : null,
                    FechaServicio = r.FechaServicio,
                    Liquidado = r.Liquidado,
                    Confirmado = r.Confirmado,
                    FormaPago = r.FormaPago,

                    // Incluir evidencias directamente
                    Evidencias = r.Evidencias
                        .Where(e => e.EmpresaId == empresaId)
                        .OrderByDescending(e => e.FechaSubida)
                        .Select(e => new EvidenciaRegistroServicioViewModel
                        {
                            Id = e.Id,
                            RegistroServicioId = e.RegistroServicioId,
                            UrlEvidencia = e.UrlEvidencia,
                            NombreArchivo = e.NombreArchivo,
                            FechaSubida = e.FechaSubida
                        })
                        .ToList()
                })
                .ToListAsync();
        }

        private static (DateTime Inicio, DateTime Fin) NormalizarRangoUtc(DateTime fechaInicio, DateTime fechaFin)
        {
            var inicioUtc = fechaInicio.Kind == DateTimeKind.Utc ? fechaInicio : fechaInicio.ToUniversalTime();
            var finUtc = fechaFin.Kind == DateTimeKind.Utc ? fechaFin : fechaFin.ToUniversalTime();

            if (finUtc < inicioUtc)
            {
                (inicioUtc, finUtc) = (finUtc, inicioUtc);
            }

            if (finUtc == inicioUtc)
            {
                finUtc = finUtc.AddDays(1);
            }

            return (inicioUtc, finUtc);
        }

        public async Task<List<RegistroServicioViewModel>> ObtenerRegistrosFechaActualPersonaAsync(int empresaId, int personaId)
        {
            var (inicioDia, finDia) = ObtenerRangoUtcActual();

            return await ConstruirQueryPorRango(empresaId, inicioDia, finDia)
                .Where(r => r.PersonaId == personaId)
                .Include(r => r.Persona)
                .Include(r => r.Servicio)
                .Select(r => new RegistroServicioViewModel
                {
                    Id = r.Id,
                    PersonaId = r.PersonaId,
                    ServicioId = r.ServicioId,
                    EmpresaId = r.EmpresaId,
                    NombrePersona = r.Persona != null ? r.Persona.Nombre + " " + r.Persona.Apellido : null,
                    NombreServicio = r.Servicio != null ? r.Servicio.Nombre : null,
                    FechaServicio = r.FechaServicio,
                    Confirmado = r.Confirmado,
                    FormaPago = r.FormaPago
                })
                .ToListAsync();
        }

        public async Task<List<ConsolidadoDiaViewModel>> ObtenerConsolidadoDiaAsync(int empresaId, int personaId)
        {
            var (inicioDia, finDia) = ObtenerRangoUtcActual();
            var query = ConstruirQueryPorRango(empresaId, inicioDia, finDia);

            var esAdministrador = await PersonaEsAdministradorAsync(personaId);
            if (!esAdministrador)
            {
                query = query.Where(r => r.PersonaId == personaId);
            }

            return await query
                .Include(r => r.Persona)
                .GroupBy(r => new
                {
                    r.PersonaId,
                    NombrePersona = r.Persona.Nombre + " " + r.Persona.Apellido
                })
                .Select(g => new ConsolidadoDiaViewModel
                {
                    PersonaId = g.Key.PersonaId,
                    NombrePersona = g.Key.NombrePersona,
                    CantidadServicios = g.Count()
                })
                .ToListAsync();
        }

        public async Task<bool> ActualizarConfirmadoAsync(int registroServicioId, int empresaId, bool confirmado)
        {
            var registro = await _context.RegistroServicio
                .FirstOrDefaultAsync(r => r.Id == registroServicioId && r.EmpresaId == empresaId);

            if (registro is null)
            {
                return false;
            }

            if (registro.Confirmado == confirmado)
            {
                return true;
            }

            registro.Confirmado = confirmado;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ActualizarFormaPagoAsync(int registroServicioId, int empresaId, char? formaPago)
        {
            var registro = await _context.RegistroServicio
                .FirstOrDefaultAsync(r => r.Id == registroServicioId && r.EmpresaId == empresaId);

            if (registro is null)
            {
                return false;
            }

            if (registro.FormaPago == formaPago)
            {
                return true;
            }

            registro.FormaPago = formaPago;
            await _context.SaveChangesAsync();

            return true;
        }

        private static (DateTime InicioDia, DateTime FinDia) ObtenerRangoUtcActual()
        {
            var inicioDia = DateTime.UtcNow.Date;
            return (inicioDia, inicioDia.AddDays(1));
        }

        private IQueryable<RegistroServicio> ConstruirQueryPorRango(int empresaId, DateTime inicio, DateTime fin)
        {
            return _context.RegistroServicio
                .Where(r =>
                    r.EmpresaId == empresaId &&
                    r.FechaServicio.HasValue &&
                    r.FechaServicio.Value >= inicio &&
                    r.FechaServicio.Value < fin);
        }

        private async Task<bool> PersonaEsAdministradorAsync(int personaId)
        {
            var rol = await _context.PersonaRol
                .Where(pr => pr.PersonaId == personaId)
                .Select(pr => pr.RolCodigo)
                .FirstOrDefaultAsync();

            return rol == "01";
        }
    }
}
