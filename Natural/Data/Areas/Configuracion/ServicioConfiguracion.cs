using Natural.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain.Configuracion;
using Natural.Models.Areas.Configuracion;

namespace Natural.Data.Areas.Configuracion
{
    public class ServicioConfiguracion
    {
        private readonly NaturalDbContext _context;

        public ServicioConfiguracion(NaturalDbContext context)
        {
            _context = context;
        }

        #region ConfiguracionServicio

        public async Task<ConfiguracionServicioViewModel> CrearConfiguracionServicioAsync(ConfiguracionServicioViewModel model)
        {
            if (model.PorcentajeTrabajador + model.PorcentajeEmpresa > 100)
                throw new ArgumentException("La suma de los porcentajes no puede superar el 100%");

            var yaExiste = await _context.ConfiguracionServicio
                .AnyAsync(c => c.EmpresaId == model.EmpresaId && c.ServicioId == model.ServicioId);

            if (yaExiste)
                throw new InvalidOperationException("Ya existe una configuración para este servicio y empresa");

            var config = new ConfiguracionServicio
            {
                EmpresaId = model.EmpresaId,
                ServicioId = model.ServicioId,
                PorcentajeTrabajador = model.PorcentajeTrabajador,
                PorcentajeEmpresa = model.PorcentajeEmpresa,
                FechaCreacion = DateTime.UtcNow,
                Estado = model.Estado
            };

            _context.ConfiguracionServicio.Add(config);
            await _context.SaveChangesAsync();

            // Obtener info completa para retornar
            var servicio = await _context.Servicio.FindAsync(model.ServicioId);

            return new ConfiguracionServicioViewModel
            {
                Id = config.Id,
                EmpresaId = config.EmpresaId,
                ServicioId = config.ServicioId,
                PorcentajeTrabajador = config.PorcentajeTrabajador,
                PorcentajeEmpresa = config.PorcentajeEmpresa,
                Estado = config.Estado,
            };
        }

        public async Task<bool> ActualizarConfiguracionServicioAsync(ConfiguracionServicioViewModel model)
        {
            var config = await _context.ConfiguracionServicio.FindAsync(model.Id);

            if (config == null)
                return false;

            if (model.PorcentajeTrabajador + model.PorcentajeEmpresa > 100)
                throw new ArgumentException("La suma de los porcentajes no puede superar el 100%");

            config.PorcentajeTrabajador = model.PorcentajeTrabajador;
            config.PorcentajeEmpresa = model.PorcentajeEmpresa;
            config.Estado = model.Estado;

            _context.ConfiguracionServicio.Update(config);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ConfiguracionServicioViewModel?> ObtenerConfiguracionPorIdAsync(int id)
        {
            return await _context.ConfiguracionServicio
                .Include(c => c.Servicio)
                .Where(c => c.Id == id)
                .Select(c => new ConfiguracionServicioViewModel
                {
                    Id = c.Id,
                    EmpresaId = c.EmpresaId,
                    ServicioId = c.ServicioId,
                    PorcentajeTrabajador = c.PorcentajeTrabajador,
                    PorcentajeEmpresa = c.PorcentajeEmpresa,
                    Estado = c.Estado,
                    NombreServicio = c.Servicio.Nombre
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<ConfiguracionServicioViewModel>> ObtenerConfiguracionesPorEmpresaAsync(int empresaId)
        {
            return await _context.ConfiguracionServicio
                .Where(c => c.EmpresaId == empresaId)
                .Include(c => c.Servicio)
                .Select(c => new ConfiguracionServicioViewModel
                {
                    Id = c.Id,
                    EmpresaId = c.EmpresaId,
                    ServicioId = c.ServicioId,
                    PorcentajeTrabajador = c.PorcentajeTrabajador,
                    PorcentajeEmpresa = c.PorcentajeEmpresa,
                    Estado = c.Estado,
                    NombreServicio = c.Servicio.Nombre
                })
                .ToListAsync();
        }

        public async Task<bool> EliminarConfiguracionServicioAsync(int id)
        {
            var config = await _context.ConfiguracionServicio.FindAsync(id);
            if (config == null)
                return false;

            _context.ConfiguracionServicio.Remove(config);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion ConfiguracionServicio
    }
}
