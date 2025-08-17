using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;

namespace Natural.Data.Areas.Contabilidad
{
    public class ServicioContabilidad
    {
        private readonly NaturalDbContext _context;

        public ServicioContabilidad(NaturalDbContext context)
        {
            _context = context;
        }

        public async Task<List<PagoPersonaViewModel>> ObtenerPagosAgrupadosUltimosDiasAsync(int empresaId)
        {
            try
            {
                var fechaLimite = DateTime.Today.AddDays(-5);

                var registros = await _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.FechaServicio.HasValue &&
                        r.FechaServicio.Value.Date >= fechaLimite &&
                        !r.Liquidado)
                    .Include(r => r.Persona)
                    .ToListAsync();

                var configuraciones = await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId)
                    .ToListAsync();

                var servicios = await _context.Servicio.ToListAsync();

                var pagosAgrupados = registros
                    .Where(r => r.Persona != null)
                    .GroupBy(r => new { r.PersonaId, r.Persona.Nombre, r.Persona.Apellido })
                    .Select(grupoPersona =>
                    {
                        var detallesServicio = grupoPersona
                            .GroupBy(r => r.ServicioId)
                            .Select(grupoServicio =>
                            {
                                var servicio = servicios.FirstOrDefault(s => s.Id == grupoServicio.Key);
                                var config = configuraciones.FirstOrDefault(c => c.ServicioId == grupoServicio.Key);

                                var precio = servicio?.Precio ?? 0;
                                var porcentaje = config?.PorcentajeTrabajador ?? 0;
                                var cantidad = grupoServicio.Count();

                                return new ServicioDetalleViewModel
                                {
                                    Nombre = servicio?.Nombre ?? "Desconocido",
                                    Cantidad = cantidad
                                };
                            })
                            .ToList();

                        var total = grupoPersona.Sum(r =>
                        {
                            var config = configuraciones.FirstOrDefault(c => c.ServicioId == r.ServicioId);
                            var servicio = servicios.FirstOrDefault(s => s.Id == r.ServicioId);
                            var porcentaje = config?.PorcentajeTrabajador ?? 0;
                            var precio = servicio?.Precio ?? 0;
                            return precio * (porcentaje / 100);
                        });

                        return new PagoPersonaViewModel
                        {
                            PersonaId = grupoPersona.Key.PersonaId,
                            NombrePersona = $"{grupoPersona.Key.Nombre} {grupoPersona.Key.Apellido}",
                            TotalPagado = total,
                            Servicios = detallesServicio
                        };
                    })
                    .ToList();

                return pagosAgrupados;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<DetalleServicioPorDiaViewModel>> ObtenerDetalleServiciosPorPersonaAsync(int empresaId, int personaId)
        {
            var fechaLimite = DateTime.Today.AddDays(-5);

            var registros = await _context.RegistroServicio
                .Where(r =>
                    r.EmpresaId == empresaId &&
                    r.PersonaId == personaId &&
                    r.FechaServicio.HasValue &&
                    r.FechaServicio.Value.Date >= fechaLimite &&
                    !r.Liquidado)
                .Include(r => r.Servicio)
                .ToListAsync();

            var agrupadoPorFecha = registros
                .GroupBy(r => r.FechaServicio.Value.Date)
                .Select(g => new DetalleServicioPorDiaViewModel
                {                    
                    Fecha = g.Key,
                    TotalServicios = g.Count(),
                    Servicios = g.Select(s => new ServicioDetalleItem
                    {
                        NombreServicio = s.Servicio?.Nombre ?? "Servicio desconocido",
                        Hora = s.FechaServicio.Value
                    }).ToList()
                })
                .OrderByDescending(d => d.Fecha)
                .ToList();

            return agrupadoPorFecha;
        }
    }
}
