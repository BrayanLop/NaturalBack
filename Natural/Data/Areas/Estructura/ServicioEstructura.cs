using Natural.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Natural.Models.Areas.Estructura;

namespace Natural.Data.Areas.Estructura
{
    public class ServicioEstructura
    {
        private readonly NaturalDbContext _context;

        public ServicioEstructura(NaturalDbContext context)
        {
            _context = context;
        }

        public async Task<Servicio> CrearServicioAsync(ServicioViewModel servicioViewModel)
        {
            var servicio = new Servicio
            {
                Nombre = servicioViewModel.Nombre,
                Descripcion = servicioViewModel.Descripcion,
                FechaCreacion = DateTime.UtcNow,
                Disponible = servicioViewModel.Disponible,
                Precio = servicioViewModel.Precio
            };

            _context.Servicio.Add(servicio);
            await _context.SaveChangesAsync();
            return servicio;
        }

        public async Task<List<Servicio>> ObtenerTodosLosServicioAsync()
        {
            return await _context.Servicio.ToListAsync();
        }

        public async Task<Servicio> ObtenerServicioPorIdAsync(int id)
        {
            return await _context.Servicio.FindAsync(id);
        }

        public async Task<bool> ActualizarServicioAsync(ServicioViewModel servicioViewModel)
        {
            var servicio = await _context.Servicio.FindAsync(servicioViewModel.Id);
            if (servicio == null)
            {
                return false;
            }

            servicio.Nombre = servicioViewModel.Nombre;
            servicio.Descripcion = servicioViewModel.Descripcion;
            servicio.Disponible = servicioViewModel.Disponible;
            servicio.Precio = servicioViewModel.Precio;

            _context.Servicio.Update(servicio);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarServicioAsync(int id)
        {
            var servicio = await _context.Servicio.FindAsync(id);
            if (servicio == null)
            {
                return false;
            }

            _context.Servicio.Remove(servicio);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
