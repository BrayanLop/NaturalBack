using Microsoft.EntityFrameworkCore;
using Natural.Data.Domain;
using Natural.Data.Domain.Gestion;
using Natural.Models.Areas.Contabilidad;

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
                var configGeneral = await _context.ConfiguracionGeneral
                    .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

                if (configGeneral == null)
                    throw new Exception("No se encontró la configuración para la empresa.");

                DateTime fechaLimiteUtc = DateTime.UtcNow.Date;
                switch (configGeneral.Periodicidad.ToUpper())
                {
                    case "M":
                        fechaLimiteUtc = fechaLimiteUtc.AddMonths(-configGeneral.ValorPeriodicidad);
                        break;
                    case "Q":
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-15);
                        break;
                    case "S":
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-7);
                        break;
                    case "D":
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-configGeneral.ValorPeriodicidad);
                        break;
                    default:
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-5);
                        break;
                }

                var fechaHoyUtc = DateTime.UtcNow.Date.AddDays(1);

                var registros = await _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.FechaServicio >= fechaLimiteUtc &&
                        r.FechaServicio < fechaHoyUtc &&
                        !r.Liquidado)
                    .Include(r => r.Persona)
                    .ToListAsync();

                var configuraciones = await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId)
                    .ToListAsync();

                var servicios = await _context.Servicio.ToListAsync();

                var configuracionesPorServicio = configuraciones
                    .GroupBy(c => c.ServicioId)
                    .ToDictionary(g => g.Key, g => g.First());

                var serviciosPorId = servicios
                    .GroupBy(s => s.Id)
                    .ToDictionary(g => g.Key, g => g.First());

                decimal CalcularPagoTrabajador(int servicioId)
                {
                    configuracionesPorServicio.TryGetValue(servicioId, out var config);
                    serviciosPorId.TryGetValue(servicioId, out var servicio);
                    var porcentaje = config?.PorcentajeTrabajador ?? 0m;
                    var precio = servicio?.Precio ?? 0m;
                    return precio * (porcentaje / 100m);
                }

                var pagosAgrupados = registros
                    .Where(r => r.Persona != null)
                    .GroupBy(r => new { r.PersonaId, r.Persona.Nombre, r.Persona.Apellido })
                    .Select(grupoPersona =>
                    {
                        var detallesServicio = grupoPersona
                            .GroupBy(r => r.ServicioId)
                            .Select(grupoServicio =>
                            {
                                serviciosPorId.TryGetValue(grupoServicio.Key, out var servicio);
                                return new ServicioDetalleViewModel
                                {
                                    Nombre = servicio?.Nombre ?? "Desconocido",
                                    Cantidad = grupoServicio.Count()
                                };
                            })
                            .ToList();

                        var total = grupoPersona
                            .Where(r => r.Confirmado)
                            .Sum(r => CalcularPagoTrabajador(r.ServicioId));
                        var totalSinConfirmar = grupoPersona
                            .Where(r => !r.Confirmado)
                            .Sum(r => CalcularPagoTrabajador(r.ServicioId));

                        return new PagoPersonaViewModel
                        {
                            PersonaId = grupoPersona.Key.PersonaId,
                            NombrePersona = $"{grupoPersona.Key.Nombre} {grupoPersona.Key.Apellido}",
                            TotalPagado = total,
                            TotalSinConfirmar = totalSinConfirmar,
                            Servicios = detallesServicio
                        };
                    })
                    .ToList();

                return pagosAgrupados;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<DetalleServicioPorDiaViewModel>> ObtenerDetalleServiciosPorPersonaAsync(int empresaId, int personaId)
        {
            try
            {
                // 1. Obtener configuración de la empresa
                var configGeneral = await _context.ConfiguracionGeneral
                    .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

                if (configGeneral == null)
                    throw new Exception("No se encontró la configuración para la empresa.");

                // 2. Calcular fecha límite según periodicidad
                DateTime fechaLimiteUtc = DateTime.UtcNow.Date;
                switch (configGeneral.Periodicidad.ToUpper())
                {
                    case "M": // Mes
                        fechaLimiteUtc = fechaLimiteUtc.AddMonths(-configGeneral.ValorPeriodicidad);
                        break;
                    case "Q": // Quincenal
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-15);
                        break;
                    case "S": // Semanal
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-7);
                        break;
                    case "D": // Días específicos
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-configGeneral.ValorPeriodicidad);
                        break;
                    default:
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-5); // fallback
                        break;
                }

                var mananaUtc = DateTime.UtcNow.Date.AddDays(1);

                // 3. Consultar registros según configuración
                var registros = await _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.PersonaId == personaId &&
                        r.FechaServicio >= fechaLimiteUtc &&
                        r.FechaServicio < mananaUtc &&
                        !r.Liquidado)
                    .Include(r => r.Servicio)
                    .ToListAsync();

                if (!registros.Any())
                {
                    return new List<DetalleServicioPorDiaViewModel>();
                }

                var serviciosIds = registros
                    .Select(r => r.ServicioId)
                    .Distinct()
                    .ToList();

                var configuracionesPorServicio = (await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId && serviciosIds.Contains(c.ServicioId))
                    .ToListAsync())
                    .GroupBy(c => c.ServicioId)
                    .ToDictionary(g => g.Key, g => g.First());

                decimal CalcularPagoTrabajador(RegistroServicio registro)
                {
                    if (!configuracionesPorServicio.TryGetValue(registro.ServicioId, out var configServicio))
                    {
                        return 0m;
                    }

                    var precioServicio = registro.Servicio?.Precio ?? 0m;
                    return precioServicio * (configServicio.PorcentajeTrabajador / 100m);
                }

                // 4. Agrupar por día y mapear
                return registros
                    .GroupBy(r => r.FechaServicio!.Value.Date)
                    .Select(g =>
                    {
                        var totalMontoDia = g
                            .Where(q => q.Confirmado)
                            .Sum(registro => CalcularPagoTrabajador(registro));

                        return new DetalleServicioPorDiaViewModel
                        {
                            Fecha = g.Key,
                            LiquidacionServicios = totalMontoDia,
                            TotalServicios = g.Count(),
                            Servicios = g.Select(s => new ServicioDetalleItem
                            {
                                RegistroServicioId = s.Id,
                                NombreServicio = s.Servicio?.Nombre ?? "Servicio desconocido",
                                Hora = s.FechaServicio!.Value,
                                Confirmado = s.Confirmado,
                                ValorTrabajador = CalcularPagoTrabajador(s)
                            }).ToList()
                        };
                    })
                    .OrderByDescending(d => d.Fecha)
                    .ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task LiquidarServiciosAsync(int empresaId, int personaId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Obtener configuración de la empresa
                var configGeneral = await _context.ConfiguracionGeneral
                    .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

                if (configGeneral == null)
                    throw new Exception("No se encontró la configuración para la empresa.");

                // 2. Calcular fecha límite según periodicidad
                DateTime fechaLimiteUtc = DateTime.UtcNow.Date;
                switch (configGeneral.Periodicidad.ToUpper())
                {
                    case "M": // Mes
                        fechaLimiteUtc = fechaLimiteUtc.AddMonths(-configGeneral.ValorPeriodicidad);
                        break;
                    case "Q": // Quincenal
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-15);
                        break;
                    case "S": // Semanal
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-7);
                        break;
                    case "D": // Días específicos
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-configGeneral.ValorPeriodicidad);
                        break;
                    default:
                        fechaLimiteUtc = fechaLimiteUtc.AddDays(-5); // fallback
                        break;
                }

                var fechaHoyUtc = DateTime.UtcNow.Date.AddDays(1);

                // 3. Obtener registros de servicios a liquidar
                var registros = await _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.PersonaId == personaId &&
                        r.FechaServicio >= fechaLimiteUtc &&
                        r.FechaServicio < fechaHoyUtc &&
                        r.Confirmado &&
                        !r.Liquidado)
                    .Include(r => r.Servicio)
                    .ToListAsync();

                if (!registros.Any())
                    return; // No hay servicios para liquidar

                // 4. Obtener configuración de cada servicio
                var serviciosIds = registros.Select(r => r.ServicioId).Distinct().ToList();
                var configuraciones = await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId && serviciosIds.Contains(c.ServicioId))
                    .ToListAsync();

                // 5. Crear lista de ingresos a guardar
                var ingresos = new List<IngresosEmpresa>();

                foreach (var registro in registros)
                {
                    var configServicio = configuraciones.FirstOrDefault(c => c.ServicioId == registro.ServicioId);
                    var porcentaje = configServicio?.PorcentajeEmpresa ?? 0;
                    var precio = registro.Servicio?.Precio ?? 0;

                    decimal valorIngreso = precio * (porcentaje / 100);

                    if (valorIngreso > 0)
                    {
                        ingresos.Add(new IngresosEmpresa
                        {
                            EmpresaId = empresaId,
                            FechaIngreso = DateOnly.FromDateTime(DateTime.UtcNow),
                            ValorIngreso = valorIngreso
                        });
                    }

                    // Marcar registro como liquidado
                    registro.Liquidado = true;
                }

                // 6. Guardar ingresos
                await _context.IngresosEmpresas.AddRangeAsync(ingresos);

                // 7. Guardar cambios en registros
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PagoLiquidadoViewModel>> ObtenerHistorialLiquidacionesAsync(int empresaId, DateTime fechaDesde, DateTime fechaHasta, int? personaId = null)
        {
            try
            {
                var inicioUtc = DateTime.SpecifyKind(fechaDesde.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaHasta.Date.AddDays(1), DateTimeKind.Utc);

                var query = _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.Liquidado &&
                        r.FechaServicio >= inicioUtc &&
                        r.FechaServicio < finUtc)
                    .Include(r => r.Persona)
                    .Include(r => r.Servicio)
                    .AsQueryable();

                // 🔹 Filtro opcional por persona
                if (personaId.HasValue)
                {
                    query = query.Where(r => r.PersonaId == personaId.Value);
                }

                var configuraciones = await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId)
                    .ToListAsync();

                var registros = await query.ToListAsync();

                var resultado = registros
                    .GroupBy(r => new
                    {
                        r.PersonaId,
                        r.Persona!.Nombre,
                        r.Persona.Apellido,
                        Fecha = r.FechaServicio!.Value.Date
                    })
                    .Select(g =>
                    {
                        var total = g.Sum(r =>
                        {
                            var config = configuraciones.FirstOrDefault(c => c.ServicioId == r.ServicioId);
                            var porcentaje = config?.PorcentajeTrabajador ?? 0;
                            var precio = r.Servicio?.Precio ?? 0;
                            return precio * (porcentaje / 100);
                        });

                        return new PagoLiquidadoViewModel
                        {
                            PersonaId = g.Key.PersonaId,
                            NombrePersona = $"{g.Key.Nombre} {g.Key.Apellido}",
                            FechaLiquidacion = g.Key.Fecha,
                            TotalPagado = total
                        };
                    })
                    .OrderByDescending(x => x.FechaLiquidacion)
                    .ToList();

                return resultado;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<HistorialIngresoViewModel>> ObtenerHistorialIngresosAsync(int empresaId, DateTime fechaDesde, DateTime fechaHasta, int? personaId = null)
        {
            try
            {
                var inicioUtc = DateTime.SpecifyKind(fechaDesde.Date, DateTimeKind.Utc);
                var finUtc = DateTime.SpecifyKind(fechaHasta.Date.AddDays(1), DateTimeKind.Utc);

                var query = _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.FechaServicio != null &&
                        r.FechaServicio >= inicioUtc &&
                        r.FechaServicio < finUtc)
                    .Include(r => r.Persona)
                    .Include(r => r.Servicio)
                    .AsQueryable();

                if (personaId.HasValue)
                {
                    query = query.Where(r => r.PersonaId == personaId.Value);
                }

                var configuraciones = await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId)
                    .ToDictionaryAsync(c => c.ServicioId, c => c);

                var registros = await query
                    .Where(r => r.Persona != null && r.Servicio != null && r.FechaServicio.HasValue)
                    .ToListAsync();

                return registros
                    .GroupBy(r => new
                    {
                        r.PersonaId,
                        r.Persona!.Nombre,
                        r.Persona.Apellido,
                        Fecha = r.FechaServicio!.Value.Date
                    })
                    .Select(g =>
                    {
                        decimal totalTrabajador = 0m;
                        decimal totalEmpresa = 0m;

                        foreach (var registro in g)
                        {
                            if (!configuraciones.TryGetValue(registro.ServicioId, out var configServicio))
                            {
                                continue;
                            }

                            var precio = registro.Servicio?.Precio ?? 0m;
                            totalTrabajador += precio * (configServicio.PorcentajeTrabajador / 100m);
                            totalEmpresa += precio * (configServicio.PorcentajeEmpresa / 100m);
                        }

                        return new HistorialIngresoViewModel
                        {
                            PersonaId = g.Key.PersonaId,
                            NombrePersona = $"{g.Key.Nombre} {g.Key.Apellido}",
                            FechaIngreso = g.Key.Fecha,
                            TotalServicios = g.Count(),
                            //TotalTrabajador = totalTrabajador,
                            //TotalEmpresa = totalEmpresa
                        };
                    })
                    .OrderByDescending(x => x.FechaIngreso)
                    .ToList();
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<HistorialIngresoEmpresaViewModel>> ObtenerHistorialIngresosEmpresaAsync(int empresaId, DateTime fechaDesde, DateTime fechaHasta)
        {
            try
            {
                var inicio = DateOnly.FromDateTime(fechaDesde.Date);
                var fin = DateOnly.FromDateTime(fechaHasta.Date);

                var ingresos = await _context.IngresosEmpresas
                    .Where(i =>
                        i.EmpresaId == empresaId &&
                        i.FechaIngreso >= inicio &&
                        i.FechaIngreso <= fin)
                    .ToListAsync();

                return ingresos
                    .GroupBy(i => i.FechaIngreso)
                    .Select(g => new HistorialIngresoEmpresaViewModel
                    {
                        Fecha = g.Key.ToDateTime(TimeOnly.MinValue),
                        TotalRegistros = g.Count(),
                        TotalIngresado = g.Sum(x => x.ValorIngreso ?? 0)
                    })
                    .OrderByDescending(x => x.Fecha)
                    .ToList();
            }
            catch
            {
                throw;
            }
        }
    }
}
