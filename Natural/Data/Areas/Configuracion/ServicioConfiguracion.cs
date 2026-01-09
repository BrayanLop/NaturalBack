using Natural.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain.Configuracion;
using Natural.Models.Areas.Configuracion;
using System.Linq.Expressions;

namespace Natural.Data.Areas.Configuracion
{
    public class ServicioConfiguracion
    {
        private readonly NaturalDbContext _context;

        private static readonly Expression<Func<ConfiguracionServicio, ConfiguracionServicioViewModel>> ConfiguracionServicioSelector =
            c => new ConfiguracionServicioViewModel
            {
                Id = c.Id,
                EmpresaId = c.EmpresaId,
                ServicioId = c.ServicioId,
                PorcentajeTrabajador = c.PorcentajeTrabajador,
                PorcentajeEmpresa = c.PorcentajeEmpresa,
                Estado = c.Estado,
                NombreServicio = c.Servicio.Nombre
            };

        public ServicioConfiguracion(NaturalDbContext context)
        {
            _context = context;
        }

        #region ConfiguracionServicio

        public async Task<ConfiguracionServicioViewModel> CrearConfiguracionServicioAsync(ConfiguracionServicioViewModel model)
        {
            ValidarPorcentajes(model.PorcentajeTrabajador, model.PorcentajeEmpresa);

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
                Estado = model.Estado
            };

            _context.ConfiguracionServicio.Add(config);
            await _context.SaveChangesAsync();
            await _context.Entry(config).Reference(c => c.Servicio).LoadAsync();

            return MapearConfiguracionServicio(config, config.Servicio?.Nombre);
        }

        public async Task<bool> ActualizarConfiguracionServicioAsync(ConfiguracionServicioViewModel model)
        {
            var config = await _context.ConfiguracionServicio.FindAsync(model.Id);

            if (config == null)
                return false;

            ValidarPorcentajes(model.PorcentajeTrabajador, model.PorcentajeEmpresa);

            config.PorcentajeTrabajador = model.PorcentajeTrabajador;
            config.PorcentajeEmpresa = model.PorcentajeEmpresa;
            config.Estado = model.Estado;
            config.ServicioId = model.ServicioId;

            _context.ConfiguracionServicio.Update(config);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ConfiguracionServicioViewModel?> ObtenerConfiguracionPorIdAsync(int id)
        {
            return await _context.ConfiguracionServicio
                .Where(c => c.Id == id)
                .Select(ConfiguracionServicioSelector)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ConfiguracionServicioViewModel>> ObtenerConfiguracionesPorEmpresaAsync(int empresaId)
        {
            return await _context.ConfiguracionServicio
                .Where(c => c.EmpresaId == empresaId)
                .Select(ConfiguracionServicioSelector)
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

        #region ConfiguracionGeneral

        public async Task<ConfiguracionGeneralViewModel> CambiarPeriodicidadAsync(ConfiguracionGeneralViewModel model, string nuevaPeriodicidad)
        {
            if (model.EmpresaId == null)
                throw new ArgumentException("EmpresaId es obligatorio");

            var actual = await _context.ConfiguracionGeneral
                .FirstOrDefaultAsync(c =>
                    c.EmpresaId == model.EmpresaId &&
                    c.Periodicidad == model.Periodicidad);

            if (actual != null)
            {
                _context.ConfiguracionGeneral.Remove(actual);
                await _context.SaveChangesAsync();
            }

            var nueva = new ConfiguracionGeneral
            {
                EmpresaId = model.EmpresaId.Value,
                Periodicidad = nuevaPeriodicidad,
                ValorPeriodicidad = model.ValorPeriodicidad
            };

            _context.ConfiguracionGeneral.Add(nueva);

            await _context.SaveChangesAsync();

            return new ConfiguracionGeneralViewModel
            {
                EmpresaId = nueva.EmpresaId,
                Periodicidad = nueva.Periodicidad,
                ValorPeriodicidad = nueva.ValorPeriodicidad
            };
        }

        public async Task<ConfiguracionGeneralViewModel?> ObtenerConfiguracionGeneralPorEmpresaAsync(int empresaId)
        {
            var config = await _context.ConfiguracionGeneral
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

            if (config == null)
                return null;

            return new ConfiguracionGeneralViewModel
            {
                EmpresaId = config.EmpresaId,
                Periodicidad = config.Periodicidad,
                ValorPeriodicidad = config.ValorPeriodicidad
            };
        }

        #endregion ConfiguracionGeneral

        #region Helpers

        private static void ValidarPorcentajes(decimal porcentajeTrabajador, decimal porcentajeEmpresa)
        {
            if (porcentajeTrabajador + porcentajeEmpresa > 100)
                throw new ArgumentException("La suma de los porcentajes no puede superar el 100%");
        }

        private static ConfiguracionServicioViewModel MapearConfiguracionServicio(ConfiguracionServicio config, string? nombreServicio)
        {
            return new ConfiguracionServicioViewModel
            {
                Id = config.Id,
                EmpresaId = config.EmpresaId,
                ServicioId = config.ServicioId,
                PorcentajeTrabajador = config.PorcentajeTrabajador,
                PorcentajeEmpresa = config.PorcentajeEmpresa,
                Estado = config.Estado,
                NombreServicio = nombreServicio
            };
        }

        #endregion Helpers
    }
}
