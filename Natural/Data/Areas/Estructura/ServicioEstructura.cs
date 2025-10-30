using Natural.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Natural.Models.Areas.Estructura;
using Natural.Data.Domain.Estructura;

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

        public async Task<ServicioViewModel> CrearServicioAsync(ServicioViewModel servicioViewModel)
        {
            var servicio = new Servicio
            {
                Nombre = servicioViewModel.Nombre,
                Descripcion = servicioViewModel.Descripcion,
                FechaCreacion = DateTime.UtcNow,
                Disponible = servicioViewModel.Disponible,
                Precio = servicioViewModel.Precio,
                EmpresaId = servicioViewModel.EmpresaId
            };

            _context.Servicio.Add(servicio);
            await _context.SaveChangesAsync();
            return servicioViewModel;
        }

        public async Task<List<ServicioViewModel>> ObtenerTodosLosServicioAsync(int empresaId)
        {
            return await _context.Servicio
                .Where(q => q.EmpresaId == empresaId)
                .Select(s => new ServicioViewModel
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    Descripcion = s.Descripcion,
                    EmpresaId = s.EmpresaId,
                    Precio = s.Precio,
                    Disponible = s.Disponible
                })
                .ToListAsync();
        }

        public async Task<ServicioViewModel?> ObtenerServicioPorIdAsync(int id)
        {
            var servicio = await _context.Servicio.FindAsync(id);
            if (servicio == null)
                return null;

            return new ServicioViewModel
            {
                Id = servicio.Id,
                Nombre = servicio.Nombre,
                Descripcion = servicio.Descripcion,
                EmpresaId = servicio.EmpresaId,
                Precio = servicio.Precio,
                Disponible = servicio.Disponible
            };
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
            servicio.EmpresaId = servicioViewModel.EmpresaId;

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
                FechaNacimiento = DateTime.SpecifyKind(personaViewModel.FechaNacimiento, DateTimeKind.Utc),
                EmpresaId = personaViewModel.EmpresaId,
            };

            _context.Persona.Add(persona);
            await _context.SaveChangesAsync();

            var personaRol = new PersonaRol
            {
                PersonaId = persona.Id,
                RolCodigo = personaViewModel.Rol
            };

            _context.PersonaRol.Add(personaRol);
            await _context.SaveChangesAsync();

            return persona;
        }

        public async Task<List<PersonaViewModel>> ObtenerTodasLasPersonasAsync(int empresaId)
        {
            return await _context.Persona
                .Where(q => q.EmpresaId == empresaId)
                .Select(p => new PersonaViewModel
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Apellido = p.Apellido,
                    Cedula = p.Cedula,
                    Residencia = p.Residencia,
                    Email = p.Email,
                    Edad = p.Edad,
                    Celular = p.Celular,
                    FechaNacimiento = p.FechaNacimiento,
                    EmpresaId = p.EmpresaId
                })
                .ToListAsync();
        }

        public async Task<PersonaViewModel?> ObtenerPersonaPorIdAsync(int id)
        {
            var persona = await _context.Persona.FindAsync(id);
            if (persona == null)
                return null;

            return new PersonaViewModel
            {
                Id = persona.Id,
                Nombre = persona.Nombre,
                Apellido = persona.Apellido,
                Cedula = persona.Cedula,
                Residencia = persona.Residencia,
                Email = persona.Email,
                Edad = persona.Edad,
                Celular = persona.Celular,
                FechaNacimiento = persona.FechaNacimiento,
                EmpresaId = persona.EmpresaId,
            };
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
            persona.FechaNacimiento = DateTime.SpecifyKind(personaViewModel.FechaNacimiento, DateTimeKind.Utc);
            persona.EmpresaId = personaViewModel.EmpresaId;

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

        #region Rol


        public async Task<string?> ObtenerRol(int personaId)
        {
            var rol = await (from pr in _context.PersonaRol
                             join r in _context.Rol on pr.RolCodigo equals r.Codigo
                             where pr.PersonaId == personaId
                             select r.Codigo).FirstOrDefaultAsync();
            return rol;
        }

        #endregion Rol
    }
}
