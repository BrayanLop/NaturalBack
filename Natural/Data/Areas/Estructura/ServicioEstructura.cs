using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;
using Natural.Data.Domain.Estructura;
using Natural.Models.Areas.Estructura;
using System.Security.Cryptography;
using System.Text;

namespace Natural.Data.Areas.Estructura
{
    public class ServicioEstructura
    {
        private static readonly Expression<Func<Servicio, ServicioViewModel>> ServicioProjection = servicio => new ServicioViewModel
        {
            Id = servicio.Id,
            Nombre = servicio.Nombre,
            Descripcion = servicio.Descripcion,
            EmpresaId = servicio.EmpresaId,
            Precio = servicio.Precio,
            Disponible = servicio.Disponible,
            FechaCreacion = servicio.FechaCreacion
        };

        private static readonly Expression<Func<Persona, PersonaViewModel>> PersonaProjection = persona => new PersonaViewModel
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
            Empresa = new EmpresaViewModel
            {
                Id = persona.Empresa.Id,
                Nombre = persona.Empresa.Nombre,
                Direccion = persona.Empresa.Direccion,
                Telefono = persona.Empresa.Telefono,
            }
        };

        private readonly NaturalDbContext _context;

        public ServicioEstructura(NaturalDbContext context)
        {
            _context = context;
        }

        #region Servicio

        public async Task<ServicioViewModel> CrearServicioAsync(ServicioViewModel servicioViewModel)
        {
            ArgumentNullException.ThrowIfNull(servicioViewModel);

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

            return MapServicio(servicio);
        }

        public Task<List<ServicioViewModel>> ObtenerTodosLosServicioAsync(int empresaId)
        {
            return _context.Servicio
                .AsNoTracking()
                .Where(q => q.EmpresaId == empresaId)
                .Select(ServicioProjection)
                .ToListAsync();
        }

        public Task<ServicioViewModel?> ObtenerServicioPorIdAsync(int id)
        {
            return _context.Servicio
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(ServicioProjection)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ActualizarServicioAsync(ServicioViewModel servicioViewModel)
        {
            ArgumentNullException.ThrowIfNull(servicioViewModel);

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
            ArgumentNullException.ThrowIfNull(personaViewModel);

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
                EmpresaId = personaViewModel.EmpresaId
            };

            var personaRol = new PersonaRol
            {
                Persona = persona,
                RolCodigo = personaViewModel.Rol
            };

            _context.Persona.Add(persona);
            _context.PersonaRol.Add(personaRol);

            await _context.SaveChangesAsync();

            return persona;
        }

        public Task<List<PersonaViewModel>> ObtenerTodasLasPersonasAsync(int empresaId)
        {
            return _context.Persona
                .AsNoTracking()
                .Where(q => q.EmpresaId == empresaId)
                .Select(PersonaProjection)
                .ToListAsync();
        }

        public Task<PersonaViewModel?> ObtenerPersonaPorIdAsync(int id)
        {
            return _context.Persona.Include("Empresa")
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(PersonaProjection)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ActualizarPersonaAsync(PersonaViewModel personaViewModel)
        {
            ArgumentNullException.ThrowIfNull(personaViewModel);

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

        public Task<string?> ObtenerRol(int personaId)
        {
            return (from pr in _context.PersonaRol.AsNoTracking()
                    join r in _context.Rol.AsNoTracking() on pr.RolCodigo equals r.Codigo
                    where pr.PersonaId == personaId
                    select r.Codigo).FirstOrDefaultAsync();
        }

        public Task<string?> ObtenerRolUsuario(int usuarioId)
        {
            return (from pr in _context.UsuarioRol.AsNoTracking()
                    join r in _context.Rol.AsNoTracking() on pr.RolCodigo equals r.Codigo
                    where pr.UsuarioId == usuarioId
                    select r.Codigo).FirstOrDefaultAsync();
        }

        #endregion Rol

        #region Usuario

        public async Task<bool> ActualizarContrasenaUsuarioAsync(int usuarioId, string nuevaContrasena)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nuevaContrasena);

            var usuario = await _context.Usuario.FindAsync(usuarioId);
            if (usuario == null)
            {
                return false;
            }

            usuario.ClaveHash = nuevaContrasena;

            _context.Usuario.Update(usuario);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion Usuario

        private static ServicioViewModel MapServicio(Servicio servicio)
        {
            return new ServicioViewModel
            {
                Id = servicio.Id,
                Nombre = servicio.Nombre,
                Descripcion = servicio.Descripcion,
                EmpresaId = servicio.EmpresaId,
                Precio = servicio.Precio,
                Disponible = servicio.Disponible,
                FechaCreacion = servicio.FechaCreacion
            };
        }
    }
}
