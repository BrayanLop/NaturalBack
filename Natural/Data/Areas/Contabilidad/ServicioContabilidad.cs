using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

        public async Task<List<PagoPersonaViewModel>> ObtenerPagosAgrupadosUltimosDiasAsync(int empresaId, int? personaId = null)
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
                        !r.Liquidado &&
                        (!personaId.HasValue || r.PersonaId == personaId.Value))
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

        public async Task LiquidarServiciosAsync(int empresaId, int personaId, string? egresosId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var configGeneral = await ObtenerConfiguracionGeneralRequeridaAsync(empresaId);
                var fechaLimiteUtc = CalcularFechaLimiteUtc(configGeneral);
                var fechaHoyUtc = CalcularFechaFinUtc();

                var listaEgresos = new List<EgresoEmpresaViewModel>();

                if (!egresosId.IsNullOrEmpty())
                {
                    var egresos = await ObtenerEgresosPorPersonaAsync(empresaId, personaId);
                    var ids = egresosId?
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => int.Parse(x))
                            .ToList();

                    foreach (var id in ids)
                    {
                        var egresosFiltrados = egresos.FindAll(e => e.EgresoId == id);
                        listaEgresos.AddRange(egresosFiltrados);
                    }
                }

                var valorTotalADescontar = listaEgresos.Sum(e => e.ValorEgreso);

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
                var idLiquidacion = Guid.NewGuid().ToString();

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
                            ValorIngreso = valorIngreso + valorTotalADescontar
                        });
                    }

                    registro.Liquidado = true;
                    registro.FechaLiquidado = DateTime.UtcNow;
                    registro.IdLiquidacion = idLiquidacion;

                    valorTotalADescontar = 0;
                }

                // ✅ PROCESAR LIQUIDACIÓN DE EGRESOS (solo si hay egresos)
                if (listaEgresos.Any())
                {
                    var idsEgresosALiquidar = listaEgresos
                        .Select(le => le.EgresoId)
                        .Distinct()
                        .ToList();

                    var egresosYaLiquidados = await _context.LiquidacionEgresos
                        .Where(le => idsEgresosALiquidar.Contains(le.IdEgreso))
                        .Select(le => le.IdEgreso)
                        .Distinct()
                        .ToListAsync();

                    var egresosPendientes = idsEgresosALiquidar
                        .Except(egresosYaLiquidados)
                        .ToList();

                    if (egresosPendientes.Any())
                    {
                        var egresosActualizar = await _context.EgresosEmpresa
                            .Where(e => egresosPendientes.Contains(e.EgresoId))
                            .ToListAsync();

                        foreach (var egreso in egresosActualizar)
                        {
                            egreso.Liquidado = true;
                        }

                        var liquidacionEgreso = egresosPendientes.Select(egresoId => new LiquidacionEgreso
                        {
                            EmpresaId = empresaId,
                            IdLiquidacion = idLiquidacion,
                            IdEgreso = egresoId
                        }).ToList();

                        await _context.LiquidacionEgresos.AddRangeAsync(liquidacionEgreso);
                    }
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
                        r.FechaLiquidado != null &&
                        r.FechaLiquidado >= inicioUtc &&
                        r.FechaLiquidado < finUtc)
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
                    .Where(r => r.Persona != null && r.Servicio != null && r.FechaLiquidado.HasValue)
                    .ToListAsync();

                // ✅ Obtener todos los IDs de liquidación únicos
                var idsLiquidacion = registros
                    .Where(r => !string.IsNullOrEmpty(r.IdLiquidacion))
                    .Select(r => r.IdLiquidacion!)
                    .Distinct()
                    .ToList();

                // ✅ Obtener egresos relacionados con estas liquidaciones
                var egresosLiquidados = await _context.LiquidacionEgresos
                    .Where(le => idsLiquidacion.Contains(le.IdLiquidacion))
                    .Include(le => le.EgresoEmpresa)
                    .ToListAsync();

                // ✅ Crear diccionario: IdLiquidacion -> TotalEgresos
                var egresosPorLiquidacion = egresosLiquidados
                    .GroupBy(le => le.IdLiquidacion)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(le => le.EgresoEmpresa?.ValorEgreso ?? 0m)
                    );

                return registros
                    .GroupBy(r =>
                    {
                        var fechaColombia = TimeZoneHelper.ConvertUtcToColombia(r.FechaLiquidado!.Value).Date;
                        return new
                        {
                            r.PersonaId,
                            Nombre = r.Persona!.Nombre,
                            Apellido = r.Persona.Apellido,
                            Fecha = fechaColombia,
                            IdLiquidacion = r.IdLiquidacion
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

                        // ✅ Restar egresos relacionados con esta liquidación
                        if (!string.IsNullOrEmpty(g.Key.IdLiquidacion)
                            && egresosPorLiquidacion.TryGetValue(g.Key.IdLiquidacion, out var egresosTotal))
                        {
                            total -= egresosTotal;
                        }

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

        public async Task<EgresosEmpresa> RegistrarEgresoAsync(int empresaId, EgresoEmpresaViewModel egresoEmpresa)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(egresoEmpresa.ValorEgreso, 0m);
            if (string.IsNullOrWhiteSpace(egresoEmpresa.Motivo))
            {
                throw new ArgumentException("El motivo del egreso es obligatorio.", nameof(egresoEmpresa.Motivo));
            }

            var fechaRegistroColombia = egresoEmpresa.FechaRegistro?.ToDateTime(TimeOnly.MinValue)
                ?? TimeZoneHelper.ColombiaNow;

            var fechaRegistroUtc = TimeZoneHelper.ConvertColombiaToUtc(fechaRegistroColombia);

            var egreso = new EgresosEmpresa
            {
                EmpresaId = empresaId,
                PersonaId = egresoEmpresa.PersonaId,
                ValorEgreso = egresoEmpresa.ValorEgreso,
                Motivo = egresoEmpresa.Motivo,
                FechaRegistro = fechaRegistroUtc,
                SeDescuenta = egresoEmpresa.SeDescuenta
            };

            await _context.EgresosEmpresa.AddAsync(egreso);
            await _context.SaveChangesAsync();

            return egreso;
        }

        public async Task<List<EgresoEmpresaViewModel>> ObtenerEgresosPorRangoAsync(int empresaId, DateOnly fechaInicio, DateOnly fechaFin)
        {
            if (fechaFin < fechaInicio)
            {
                (fechaInicio, fechaFin) = (fechaFin, fechaInicio);
            }

            var inicioUtc = TimeZoneHelper.ConvertColombiaToUtc(fechaInicio.ToDateTime(TimeOnly.MinValue));
            var finUtcExclusivo = TimeZoneHelper.ConvertColombiaToUtc(fechaFin.AddDays(1).ToDateTime(TimeOnly.MinValue));

            return await _context.EgresosEmpresa
                .AsNoTracking()
                .Include(e => e.Persona)
                .Where(e =>
                    e.EmpresaId == empresaId &&
                    e.FechaRegistro >= inicioUtc &&
                    e.FechaRegistro < finUtcExclusivo)
                .OrderByDescending(e => e.FechaRegistro)
                .ThenByDescending(e => e.EgresoId)
                .Select(e => new EgresoEmpresaViewModel
                {
                    EmpresaId = e.EmpresaId ?? empresaId,
                    PersonaId = e.PersonaId ?? 0,
                    Motivo = e.Motivo ?? string.Empty,
                    NombrePersona = e.Persona != null
                        ? ObtenerNombreCompleto(e.Persona.Nombre, e.Persona.Apellido)
                        : string.Empty,
                    ValorEgreso = e.ValorEgreso ?? 0m,
                    FechaRegistro = DateOnly.FromDateTime(TimeZoneHelper.ConvertUtcToColombia(e.FechaRegistro))
                })
                .ToListAsync();
        }

        public async Task<List<EgresoEmpresaViewModel>> ObtenerEgresosPorPersonaAsync(int empresaId, int personaId)
        {
            return await _context.EgresosEmpresa
                .AsNoTracking()
                .Include(e => e.Persona)
                .Where(e =>
                    e.EmpresaId == empresaId &&
                    e.PersonaId == personaId &&
                    e.SeDescuenta &&
                    !e.Liquidado)
                .OrderByDescending(e => e.FechaRegistro)
                .ThenByDescending(e => e.EgresoId)
                .Select(e => new EgresoEmpresaViewModel
                {
                    EgresoId = e.EgresoId,
                    EmpresaId = e.EmpresaId ?? empresaId,
                    PersonaId = e.PersonaId ?? 0,
                    Motivo = e.Motivo ?? string.Empty,
                    NombrePersona = e.Persona != null
                        ? ObtenerNombreCompleto(e.Persona.Nombre, e.Persona.Apellido)
                        : string.Empty,
                    ValorEgreso = e.ValorEgreso ?? 0m,
                    FechaRegistro = DateOnly.FromDateTime(TimeZoneHelper.ConvertUtcToColombia(e.FechaRegistro))
                })
                .ToListAsync();
        }

        public async Task<ConsolidadoIngresosEgresosViewModel> ObtenerConsolidadoIngresosEgresosAsync(int empresaId, DateTime fechaDesde, DateTime fechaHasta)
        {
            var inicio = DateOnly.FromDateTime(fechaDesde.Date);
            var fin = DateOnly.FromDateTime(fechaHasta.Date);

            // Ingresos
            var totalIngresos = await _context.IngresosEmpresas
                .Where(i => i.EmpresaId == empresaId && i.FechaIngreso >= inicio && i.FechaIngreso <= fin)
                .SumAsync(i => i.ValorIngreso ?? 0);

            // Egresos
            var inicioUtc = TimeZoneHelper.ConvertColombiaToUtc(fechaDesde.Date);
            var finUtcExclusivo = TimeZoneHelper.ConvertColombiaToUtc(fechaHasta.Date.AddDays(1));
            var totalEgresos = await _context.EgresosEmpresa
                .Where(e => e.EmpresaId == empresaId && e.FechaRegistro >= inicioUtc && e.FechaRegistro < finUtcExclusivo)
                .SumAsync(e => e.ValorEgreso ?? 0);

            return new ConsolidadoIngresosEgresosViewModel
            {
                TotalIngresos = totalIngresos,
                TotalEgresos = totalEgresos
            };
        }

        public async Task<ConsolidadoFormaPagoViewModel> ObtenerConsolidadoPorFormaPagoAsync(int empresaId, DateTime fechaDesde, DateTime fechaHasta, string? formaPago = null)
        {
            var (inicioUtc, finUtc) = ConstruirRangoUtc(fechaDesde, fechaHasta);

            var query = _context.RegistroServicio
                .Where(r => r.EmpresaId == empresaId && r.FechaServicio >= inicioUtc && r.FechaServicio < finUtc);

            if (!string.IsNullOrWhiteSpace(formaPago) && formaPago.Length == 1)
            {
                char formaPagoChar = formaPago[0];
                query = query.Where(r => r.FormaPago == formaPagoChar);
            }

            var registros = await query.Include(r => r.Servicio).ToListAsync();

            var cantidadTransferencia = registros.Count(r => r.FormaPago == 'T');
            var totalTransferencia = registros.Where(r => r.FormaPago == 'T').Sum(r => r.Servicio?.Precio ?? 0m);
            var cantidadEfectivo = registros.Count(r => r.FormaPago == 'E');
            var totalEfectivo = registros.Where(r => r.FormaPago == 'E').Sum(r => r.Servicio?.Precio ?? 0m);

            return new ConsolidadoFormaPagoViewModel
            {
                CantidadTransferencia = cantidadTransferencia,
                TotalTransferencia = totalTransferencia,
                CantidadEfectivo = cantidadEfectivo,
                TotalEfectivo = totalEfectivo
            };
        }

        private async Task<decimal> ObtenerValorEgresoAsync(int egresoId)
        {
            var egreso = await _context.EgresosEmpresa
                .FirstOrDefaultAsync(c => c.EgresoId == egresoId);

            return egreso?.ValorEgreso ?? 0;
        }

    }
}
