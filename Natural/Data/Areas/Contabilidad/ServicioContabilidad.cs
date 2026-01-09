using Microsoft.EntityFrameworkCore;
using Natural.Common;
using Natural.Data.Domain;
using Natural.Data.Domain.Configuracion;
using Natural.Data.Domain.Estructura;
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
                var configGeneral = await ObtenerConfiguracionGeneralRequeridaAsync(empresaId);
                var fechaLimiteUtc = CalcularFechaLimiteUtc(configGeneral);
                var fechaHoyUtc = CalcularFechaFinUtc();

                var registros = await _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.FechaServicio >= fechaLimiteUtc &&
                        r.FechaServicio < fechaHoyUtc &&
                        !r.Liquidado)
                    .Include(r => r.Persona)
                    .ToListAsync();

                if (!registros.Any())
                {
                    return new List<PagoPersonaViewModel>();
                }

                var servicioIds = registros
                    .Select(r => r.ServicioId)
                    .Distinct()
                    .ToList();

                var configuracionesPorServicio = CrearDiccionarioConfiguraciones(await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId && servicioIds.Contains(c.ServicioId))
                    .ToListAsync());

                var serviciosPorId = CrearDiccionarioServicios(await _context.Servicio
                    .Where(s => servicioIds.Contains(s.Id))
                    .ToListAsync());

                return registros
                    .Where(r => r.Persona != null)
                    .GroupBy(r => new { r.PersonaId, r.Persona!.Nombre, r.Persona.Apellido })
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

                        var totalPagado = grupoPersona
                            .Where(r => r.Confirmado)
                            .Sum(r => CalcularPagoTrabajadorPorServicio(r.ServicioId, configuracionesPorServicio, serviciosPorId));

                        var totalSinConfirmar = grupoPersona
                            .Where(r => !r.Confirmado)
                            .Sum(r => CalcularPagoTrabajadorPorServicio(r.ServicioId, configuracionesPorServicio, serviciosPorId));

                        return new PagoPersonaViewModel
                        {
                            PersonaId = grupoPersona.Key.PersonaId,
                            NombrePersona = ObtenerNombreCompleto(grupoPersona.Key.Nombre, grupoPersona.Key.Apellido),
                            TotalPagado = totalPagado,
                            TotalSinConfirmar = totalSinConfirmar,
                            Servicios = detallesServicio
                        };
                    })
                    .ToList();
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
                var configGeneral = await ObtenerConfiguracionGeneralRequeridaAsync(empresaId);
                var fechaLimiteUtc = CalcularFechaLimiteUtc(configGeneral);
                var mananaUtc = CalcularFechaFinUtc();

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

                var configuracionesPorServicio = CrearDiccionarioConfiguraciones(await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId && serviciosIds.Contains(c.ServicioId))
                    .ToListAsync());

                decimal CalcularPagoRegistro(RegistroServicio registro)
                {
                    if (!configuracionesPorServicio.TryGetValue(registro.ServicioId, out var configServicio))
                    {
                        return 0m;
                    }

                    var precioServicio = registro.Servicio?.Precio ?? 0m;
                    return CalcularMontoPorcentaje(precioServicio, configServicio.PorcentajeTrabajador);
                }

                return registros
                    .GroupBy(r => TimeZoneHelper.ConvertUtcToColombia(r.FechaServicio!.Value).Date)
                    .Select(g =>
                    {
                        var totalMontoDia = g
                            .Where(q => q.Confirmado)
                            .Sum(CalcularPagoRegistro);

                        return new DetalleServicioPorDiaViewModel
                        {
                            Fecha = g.Key,
                            LiquidacionServicios = totalMontoDia,
                            TotalServicios = g.Count(),
                            Servicios = g.Select(s =>
                            {
                                var horaColombia = TimeZoneHelper.ConvertUtcToColombia(s.FechaServicio!.Value);
                                return new ServicioDetalleItem
                                {
                                    RegistroServicioId = s.Id,
                                    NombreServicio = s.Servicio?.Nombre ?? "Servicio desconocido",
                                    Hora = horaColombia,
                                    Confirmado = s.Confirmado,
                                    ValorTrabajador = CalcularPagoRegistro(s)
                                };
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
                var configGeneral = await ObtenerConfiguracionGeneralRequeridaAsync(empresaId);
                var fechaLimiteUtc = CalcularFechaLimiteUtc(configGeneral);
                var fechaHoyUtc = CalcularFechaFinUtc();

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
                {
                    return;
                }

                var serviciosIds = registros.Select(r => r.ServicioId).Distinct().ToList();

                var configuraciones = CrearDiccionarioConfiguraciones(await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId && serviciosIds.Contains(c.ServicioId))
                    .ToListAsync());

                var ingresos = new List<IngresosEmpresa>();

                foreach (var registro in registros)
                {
                    var porcentajeEmpresa = configuraciones.TryGetValue(registro.ServicioId, out var configServicio)
                        ? configServicio.PorcentajeEmpresa
                        : 0m;

                    var precio = registro.Servicio?.Precio ?? 0m;
                    var valorIngreso = CalcularMontoPorcentaje(precio, porcentajeEmpresa);

                    if (valorIngreso > 0)
                    {
                        ingresos.Add(new IngresosEmpresa
                        {
                            EmpresaId = empresaId,
                            FechaIngreso = DateOnly.FromDateTime(TimeZoneHelper.ConvertUtcToColombia(DateTime.UtcNow)),
                            ValorIngreso = valorIngreso
                        });
                    }

                    registro.Liquidado = true;
                }

                await _context.IngresosEmpresas.AddRangeAsync(ingresos);
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
                var (inicioUtc, finUtc) = ConstruirRangoUtc(fechaDesde, fechaHasta);

                var query = _context.RegistroServicio
                    .Where(r =>
                        r.EmpresaId == empresaId &&
                        r.Liquidado &&
                        r.FechaServicio >= inicioUtc &&
                        r.FechaServicio < finUtc)
                    .Include(r => r.Persona)
                    .Include(r => r.Servicio)
                    .AsQueryable();

                if (personaId.HasValue)
                {
                    query = query.Where(r => r.PersonaId == personaId.Value);
                }

                var configuraciones = CrearDiccionarioConfiguraciones(await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId)
                    .ToListAsync());

                var registros = await query
                    .Where(r => r.Persona != null && r.Servicio != null && r.FechaServicio.HasValue)
                    .ToListAsync();

                return registros
                    .GroupBy(r =>
                    {
                        var fechaColombia = TimeZoneHelper.ConvertUtcToColombia(r.FechaServicio!.Value).Date;
                        return new
                        {
                            r.PersonaId,
                            Nombre = r.Persona!.Nombre,
                            Apellido = r.Persona.Apellido,
                            Fecha = fechaColombia
                        };
                    })
                    .Select(g =>
                    {
                        var total = g.Sum(r =>
                        {
                            var porcentaje = configuraciones.TryGetValue(r.ServicioId, out var config)
                                ? config.PorcentajeTrabajador
                                : 0m;
                            var precio = r.Servicio?.Precio ?? 0m;
                            return CalcularMontoPorcentaje(precio, porcentaje);
                        });

                        return new PagoLiquidadoViewModel
                        {
                            PersonaId = g.Key.PersonaId,
                            NombrePersona = ObtenerNombreCompleto(g.Key.Nombre, g.Key.Apellido),
                            FechaLiquidacion = g.Key.Fecha,
                            TotalPagado = total
                        };
                    })
                    .OrderByDescending(x => x.FechaLiquidacion)
                    .ToList();
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
                var (inicioUtc, finUtc) = ConstruirRangoUtc(fechaDesde, fechaHasta);

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

                var configuraciones = CrearDiccionarioConfiguraciones(await _context.ConfiguracionServicio
                    .Where(c => c.EmpresaId == empresaId)
                    .ToListAsync());

                var registros = await query
                    .Where(r => r.Persona != null && r.Servicio != null && r.FechaServicio.HasValue)
                    .ToListAsync();

                return registros
                    .GroupBy(r =>
                    {
                        var fechaColombia = TimeZoneHelper.ConvertUtcToColombia(r.FechaServicio!.Value).Date;
                        return new
                        {
                            r.PersonaId,
                            r.Persona!.Nombre,
                            r.Persona.Apellido,
                            Fecha = fechaColombia
                        };
                    })
                    .Select(g =>
                    {
                        var totalEstimado = g.Sum(registro =>
                        {
                            if (!configuraciones.TryGetValue(registro.ServicioId, out var configServicio))
                            {
                                return 0m;
                            }

                            var precio = registro.Servicio?.Precio ?? 0m;
                            var trabajador = CalcularMontoPorcentaje(precio, configServicio.PorcentajeTrabajador);
                            var empresa = CalcularMontoPorcentaje(precio, configServicio.PorcentajeEmpresa);
                            return trabajador + empresa;
                        });

                        return new HistorialIngresoViewModel
                        {
                            PersonaId = g.Key.PersonaId,
                            NombrePersona = ObtenerNombreCompleto(g.Key.Nombre, g.Key.Apellido),
                            FechaIngreso = g.Key.Fecha,
                            TotalServicios = g.Count(),
                            TotalEstimado = totalEstimado
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
                    .Select(g =>
                    {
                        var fechaLocal = DateTime.SpecifyKind(g.Key.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
                        return new HistorialIngresoEmpresaViewModel
                        {
                            Fecha = fechaLocal,
                            TotalRegistros = g.Count(),
                            TotalIngresado = g.Sum(x => x.ValorIngreso ?? 0)
                        };
                    })
                    .OrderByDescending(x => x.Fecha)
                    .ToList();
            }
            catch
            {
                throw;
            }
        }

        private async Task<ConfiguracionGeneral> ObtenerConfiguracionGeneralRequeridaAsync(int empresaId)
        {
            var configGeneral = await _context.ConfiguracionGeneral
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId);

            return configGeneral ?? throw new Exception("No se encontró la configuración para la empresa.");
        }

        private static DateTime CalcularFechaLimiteUtc(ConfiguracionGeneral configGeneral)
        {
            var fechaBaseColombia = TimeZoneHelper.ColombiaNow.Date;
            var periodicidad = configGeneral.Periodicidad?.ToUpperInvariant();

            var fechaLimiteColombia = periodicidad switch
            {
                "M" => fechaBaseColombia.AddMonths(-configGeneral.ValorPeriodicidad),
                "Q" => fechaBaseColombia.AddDays(-15),
                "S" => fechaBaseColombia.AddDays(-7),
                "D" => fechaBaseColombia.AddDays(-configGeneral.ValorPeriodicidad),
                _ => fechaBaseColombia.AddDays(-5)
            };

            return TimeZoneHelper.ConvertColombiaToUtc(fechaLimiteColombia);
        }

        private static DateTime CalcularFechaFinUtc() =>
            TimeZoneHelper.RangoDiaActualUtc().FinUtc;

        private static (DateTime InicioUtc, DateTime FinUtc) ConstruirRangoUtc(DateTime fechaDesde, DateTime fechaHasta) =>
            TimeZoneHelper.RangoUtc(fechaDesde, fechaHasta);

        private static Dictionary<int, ConfiguracionServicio> CrearDiccionarioConfiguraciones(IEnumerable<ConfiguracionServicio> configuraciones) =>
            configuraciones
                .GroupBy(c => c.ServicioId)
                .ToDictionary(g => g.Key, g => g.First());

        private static Dictionary<int, Servicio> CrearDiccionarioServicios(IEnumerable<Servicio> servicios) =>
            servicios
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());

        private static decimal CalcularPagoTrabajadorPorServicio(
            int servicioId,
            IReadOnlyDictionary<int, ConfiguracionServicio> configuraciones,
            IReadOnlyDictionary<int, Servicio> servicios)
        {
            configuraciones.TryGetValue(servicioId, out var configuracion);
            servicios.TryGetValue(servicioId, out var servicio);
            return CalcularMontoPorcentaje(servicio?.Precio ?? 0m, configuracion?.PorcentajeTrabajador ?? 0m);
        }

        private static decimal CalcularMontoPorcentaje(decimal montoBase, decimal porcentaje) =>
            montoBase * (porcentaje / 100m);

        private static string ObtenerNombreCompleto(string? nombre, string? apellido)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return apellido ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(apellido))
            {
                return nombre;
            }

            return $"{nombre} {apellido}";
        }
    }
}
