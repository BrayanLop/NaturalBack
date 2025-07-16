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

                var fechaLimite = DateTime.Today.AddDays(-5); // hoy y 5 días hacia atrás

                var registros = await _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.FechaServicio.HasValue &&
                        r.FechaServicio.Value.Date >= fechaLimite)
                    .Include(r => r.Persona)
                    .ToListAsync();

                var configuraciones = await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId)
                    .ToListAsync();

                var servicios = await _context.Servicio.ToListAsync();

                var pagosAgrupados = registros
                    .Where(r => r.Persona != null)
                    .Select(r =>
                    {
                        var config = configuraciones.FirstOrDefault(c => c.ServicioId == r.ServicioId);
                        var servicio = servicios.FirstOrDefault(s => s.Id == r.ServicioId);

                        var porcentajeTrabajador = config?.PorcentajeTrabajador ?? 0;
                        var precioServicio = servicio?.Precio ?? 0;

                        var monto = precioServicio * (porcentajeTrabajador / 100);

                        return new
                        {
                            Nombre = $"{r.Persona.Nombre} {r.Persona.Apellido}",
                            Monto = monto
                        };
                    })
                    .GroupBy(x => x.Nombre)
                    .Select(g => new PagoPersonaViewModel
                    {
                        NombrePersona = g.Key,
                        TotalPagado = g.Sum(x => x.Monto)
                    })
                    .ToList();

                return pagosAgrupados;
            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }
}
