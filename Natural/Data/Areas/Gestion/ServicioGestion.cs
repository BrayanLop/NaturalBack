using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain.Gestion;
using Natural.Models.Areas.Gestion;
using Natural.Data.Domain;
using Natural.Common;

namespace Natural.Data.Areas.Gestion
{
    public class ServicioGestion
    {
        private readonly NaturalDbContext _context;

        public ServicioGestion(NaturalDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Guarda los registros y devuelve sus ids. Lanza InvalidOperationException si alguna persona
        /// o servicio no pertenece a la empresa.
        /// </summary>
        public async Task<List<int>> GuardarRegistrosServicioAsync(int empresaId, List<RegistroServicioViewModel> registros)
        {
            var personasIds = registros.Select(r => r.PersonaId).Distinct().ToList();
            var serviciosIds = registros.Select(r => r.ServicioId).Distinct().ToList();

            var personasValidas = await _context.Persona
                .CountAsync(p => personasIds.Contains(p.Id) && p.EmpresaId == empresaId);

            // Servicios de la empresa con su precio actual: valida pertenencia y sirve para el snapshot.
            var preciosServicio = await _context.Servicio
                .Where(s => serviciosIds.Contains(s.Id) && s.EmpresaId == empresaId)
                .ToDictionaryAsync(s => s.Id, s => s.Precio);

            if (personasValidas != personasIds.Count || preciosServicio.Count != serviciosIds.Count)
                throw new InvalidOperationException("Alguna persona o servicio no pertenece a la empresa.");

            var porcentajesTrabajador = await _context.ConfiguracionServicio
                .Where(c => serviciosIds.Contains(c.ServicioId) && c.EmpresaId == empresaId)
                .GroupBy(c => c.ServicioId)
                .Select(g => new { ServicioId = g.Key, g.First().PorcentajeTrabajador })
                .ToDictionaryAsync(x => x.ServicioId, x => x.PorcentajeTrabajador);

            var fechaActual = DateTime.UtcNow;

            // Snapshot histórico: valor, porcentaje y comisión al momento del registro.
            var registrosEntidad = registros
                .Select(r =>
                {
                    var valorServicio = preciosServicio[r.ServicioId];
                    var porcentaje = porcentajesTrabajador.TryGetValue(r.ServicioId, out var pct) ? pct : 0m;
                    var comision = Math.Round(valorServicio * porcentaje / 100m, 2);

                    return new RegistroServicio
                    {
                        PersonaId = r.PersonaId,
                        ServicioId = r.ServicioId,
                        EmpresaId = empresaId,
                        FechaServicio = r.FechaServicio ?? fechaActual,
                        Liquidado = false,
                        Confirmado = r.Confirmado,
                        Rechazado = false,
                        Propina = r.Propina,
                        Observaciones = r.Observaciones,
                        ValorServicio = valorServicio,
                        PorcentajeTrabajador = porcentaje,
                        Comision = comision,
                        FormaPago = r.FormaPago
                    };
                })
                .OrderBy(r => r.FechaServicio)
                .ToList();

            await _context.RegistroServicio.AddRangeAsync(registrosEntidad);
            await _context.SaveChangesAsync();

            return registrosEntidad.Select(r => r.Id).ToList();
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

        /// <param name="personaIdFiltro">Null = todos los registros de la empresa (administrador).</param>
        public async Task<List<RegistroServicioViewModel>> ObtenerRegistrosFechaActualAsync(int empresaId, int? personaIdFiltro, DateTime inicioUtc, DateTime finUtc)
        {
            var query = ConstruirQueryPorRango(empresaId, inicioUtc, finUtc);

            if (personaIdFiltro.HasValue)
            {
                query = query.Where(r => r.PersonaId == personaIdFiltro.Value);
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
                    Rechazado = r.Rechazado,
                    MotivoRechazo = r.MotivoRechazo,
                    Propina = r.Propina,
                    PropinaPagada = r.PropinaPagada,
                    FechaPropinaPagada = r.FechaPropinaPagada,
                    Observaciones = r.Observaciones,
                    ValorServicio = r.ValorServicio,
                    PorcentajeTrabajador = r.PorcentajeTrabajador,
                    Comision = r.Comision,
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

        public static (DateTime Inicio, DateTime Fin) NormalizarRangoUtc(DateTime fechaInicio, DateTime fechaFin)
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
            var (inicioDia, finDia) = TimeZoneHelper.RangoDiaActualUtc();

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
                    Rechazado = r.Rechazado,
                    MotivoRechazo = r.MotivoRechazo,
                    Propina = r.Propina,
                    PropinaPagada = r.PropinaPagada,
                    FechaPropinaPagada = r.FechaPropinaPagada,
                    Observaciones = r.Observaciones,
                    ValorServicio = r.ValorServicio,
                    PorcentajeTrabajador = r.PorcentajeTrabajador,
                    Comision = r.Comision,
                    FormaPago = r.FormaPago
                })
                .ToListAsync();
        }

        /// <param name="personaIdFiltro">Null = todas las personas de la empresa (administrador).</param>
        public async Task<List<ConsolidadoDiaViewModel>> ObtenerConsolidadoDiaAsync(int empresaId, int? personaIdFiltro)
        {
            var (inicioDia, finDia) = TimeZoneHelper.RangoDiaActualUtc();
            var query = ConstruirQueryPorRango(empresaId, inicioDia, finDia);

            if (personaIdFiltro.HasValue)
            {
                query = query.Where(r => r.PersonaId == personaIdFiltro.Value);
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

            registro.Confirmado = confirmado;

            // Al aprobar, se limpia cualquier rechazo previo.
            if (confirmado)
            {
                registro.Rechazado = false;
                registro.MotivoRechazo = null;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RechazarAsync(int registroServicioId, int empresaId, string? motivo)
        {
            var registro = await _context.RegistroServicio
                .FirstOrDefaultAsync(r => r.Id == registroServicioId && r.EmpresaId == empresaId);

            if (registro is null)
            {
                return false;
            }

            // No se puede rechazar un registro ya liquidado.
            if (registro.Liquidado)
            {
                return false;
            }

            registro.Rechazado = true;
            registro.Confirmado = false;
            registro.MotivoRechazo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarcarPropinaPagadaAsync(int registroServicioId, int empresaId, bool pagada)
        {
            var registro = await _context.RegistroServicio
                .FirstOrDefaultAsync(r => r.Id == registroServicioId && r.EmpresaId == empresaId);

            if (registro is null)
            {
                return false;
            }

            registro.PropinaPagada = pagada;
            registro.FechaPropinaPagada = pagada ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <param name="personaIdFiltro">Si se indica, el registro debe pertenecer a esa persona.</param>
        public async Task<bool> ActualizarFormaPagoAsync(int registroServicioId, int empresaId, char? formaPago, int? personaIdFiltro)
        {
            var registro = await _context.RegistroServicio
                .FirstOrDefaultAsync(r => r.Id == registroServicioId && r.EmpresaId == empresaId
                    && (personaIdFiltro == null || r.PersonaId == personaIdFiltro));

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

        private IQueryable<RegistroServicio> ConstruirQueryPorRango(int empresaId, DateTime inicio, DateTime fin)
        {
            return _context.RegistroServicio
                .Where(r =>
                    r.EmpresaId == empresaId &&
                    r.FechaServicio.HasValue &&
                    r.FechaServicio.Value >= inicio &&
                    r.FechaServicio.Value < fin);
        }
    }
}
