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

        public async Task<bool> GuardarRegistrosServicioAsync(List<RegistroServicioViewModel> registros)
        {
            try
            {
                var registrosEntidad = registros.Select(r => new RegistroServicio
                {
                    PersonaId = r.PersonaId,
                    ServicioId = r.ServicioId,
                    Cantidad = r.Cantidad,
                    Total = r.Total,
                    EmpresaId = r.EmpresaId
                }).ToList();

                await _context.RegistroServicio.AddRangeAsync(registrosEntidad);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<RegistroServicio>> ObtenerRegistrosPorPersonaAsync(int empresaId)
        {
            return await _context.RegistroServicio
                .Where(r => r.EmpresaId == empresaId)
                .ToListAsync();
        }
    }
}
