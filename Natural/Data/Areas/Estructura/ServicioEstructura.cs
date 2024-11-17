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

        #region Servicio
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

        #endregion Servicio

        #region Persona
        public async Task<Persona> CrearPersonaAsync(PersonaViewModel personaViewModel)
        {
            var persona = new Persona
            {
                Nombre = personaViewModel.Nombre,
                Apellido = personaViewModel.Apellido,
                Cedula = personaViewModel.Cedula,
                Residencia = personaViewModel.Residencia,
                Email = personaViewModel.Email,
                Edad = personaViewModel.Edad,
                Celular = personaViewModel.Celular,
                FechaNacimiento = personaViewModel.FechaNacimiento,
            };

            _context.Persona.Add(persona);
            await _context.SaveChangesAsync();
            return persona;
        }

        public async Task<List<Persona>> ObtenerTodasLasPersonasAsync()
        {
            return await _context.Persona.ToListAsync();
        }

        public async Task<Persona> ObtenerPersonaPorIdAsync(int id)
        {
            return await _context.Persona.FindAsync(id);
        }

        public async Task<bool> ActualizarPersonaAsync(PersonaViewModel personaViewModel)
        {
            var persona = await _context.Persona.FindAsync(personaViewModel.Id);
            if (persona == null)
            {
                return false;
            }

            persona.Nombre = personaViewModel.Nombre;
            persona.Apellido = personaViewModel.Apellido;
            persona.Cedula = personaViewModel.Cedula;
            persona.Residencia = personaViewModel.Residencia;
            persona.Email = personaViewModel.Email;
            persona.Edad = personaViewModel.Edad;
            persona.Celular = personaViewModel.Celular;
            persona.FechaNacimiento = personaViewModel.FechaNacimiento;

            _context.Persona.Update(persona);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarPersonaAsync(int id)
        {
            var persona = await _context.Persona.FindAsync(id);
            if (persona == null)
            {
                return false;
            }

            _context.Persona.Remove(persona);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion Persona

    }
}
