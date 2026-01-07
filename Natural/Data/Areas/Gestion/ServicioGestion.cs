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
                    EmpresaId = r.EmpresaId,
                    FechaServicio = DateTime.UtcNow,
                    Liquidado = false
                }).OrderBy(q => q.FechaServicio).ToList();

                await _context.RegistroServicio.AddRangeAsync(registrosEntidad);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
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
                    FechaServicio = r.FechaServicio
                })
                .ToListAsync();
        }

        public async Task<List<RegistroServicioViewModel>> ObtenerRegistrosFechaActualAsync(
            int empresaId,
            int personaId)
        {
            var hoyUtc = DateTime.UtcNow.Date;
            var mananaUtc = hoyUtc.AddDays(1);

            var personaRol = await _context.PersonaRol
                .Where(pr => pr.PersonaId == personaId)
                .Select(pr => pr.RolCodigo)
                .FirstOrDefaultAsync();

            IQueryable<RegistroServicio> query = _context.RegistroServicio
                .Where(r =>
                    r.EmpresaId == empresaId &&
                    r.FechaServicio >= hoyUtc &&
                    r.FechaServicio < mananaUtc
                );

            if (personaRol != "01")
            {
                query = query.Where(r => r.PersonaId == personaId);
            }

            return await query
                .Include(r => r.Persona)
                .Include(r => r.Servicio)
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
                    FechaServicio = r.FechaServicio
                })
                .ToListAsync();
        }

        public async Task<List<RegistroServicioViewModel>> ObtenerRegistrosFechaActualPersonaAsync(int empresaId, int personaId)
        {
            return await _context.RegistroServicio
                .Where(r => r.EmpresaId == empresaId && r.FechaServicio.Value.Date == DateTime.Today && r.PersonaId == personaId)
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
                    FechaServicio = r.FechaServicio
                })
                .ToListAsync();
        }

        public async Task<List<ConsolidadoDiaViewModel>> ObtenerConsolidadoDiaAsync(
            int empresaId,
            int personaId)
        {
            var hoyUtc = DateTime.UtcNow.Date;
            var mananaUtc = hoyUtc.AddDays(1);

            var personaRol = await _context.PersonaRol
                .Where(pr => pr.PersonaId == personaId)
                .Select(pr => pr.RolCodigo)
                .FirstOrDefaultAsync();

            IQueryable<RegistroServicio> query = _context.RegistroServicio
                .Where(r =>
                    r.EmpresaId == empresaId &&
                    r.FechaServicio >= hoyUtc &&
                    r.FechaServicio < mananaUtc
                );

            if (personaRol != "01")
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
    }
}
