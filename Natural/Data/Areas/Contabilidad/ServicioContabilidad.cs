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
                                    ValorTrabajador = CalcularPagoRegistro(s),
                                    Propina = s.Propina,
                                    PropinaPagada = s.PropinaPagada
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

        /// <summary>
        /// Liquida los servicios confirmados y no liquidados de la persona dentro de la ventana de periodicidad.
        /// Crea el snapshot <see cref="Liquidacion"/> y, si vienen, sus <see cref="LiquidacionPago"/>.
        /// Devuelve el Id de la liquidación o null si no había servicios por liquidar.
        /// </summary>
        public async Task<string?> LiquidarServiciosAsync(
            int empresaId,
            int personaId,
            IReadOnlyCollection<int> idsEgresos,
            IReadOnlyCollection<PagoLiquidacionViewModel>? pagos = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var datos = await ObtenerDatosLiquidacionPendienteAsync(empresaId, personaId, idsEgresos);

                if (!datos.Registros.Any())
                {
                    return null;
                }

                var totales = CalcularTotalesLiquidacion(datos.Registros, datos.Configuraciones, datos.Egresos);
                var idLiquidacion = Guid.NewGuid().ToString();

                // Se valida antes de modificar cualquier entidad: si falla, no se toca nada.
                var pagosLiquidacion = ConstruirPagosLiquidacion(idLiquidacion, pagos, totales.NetoPagado);

                var fechaLiquidacionUtc = DateTime.UtcNow;
                var fechaIngreso = DateOnly.FromDateTime(TimeZoneHelper.ConvertUtcToColombia(fechaLiquidacionUtc));
                var valorTotalADescontar = totales.TotalDeducciones;
                var ingresos = new List<IngresosEmpresa>();

                foreach (var registro in datos.Registros)
                {
                    var porcentajeEmpresa = datos.Configuraciones.TryGetValue(registro.ServicioId, out var configServicio)
                        ? configServicio.PorcentajeEmpresa
                        : 0m;

                    var precio = registro.Servicio?.Precio ?? 0m;
                    var valorIngreso = CalcularMontoPorcentaje(precio, porcentajeEmpresa);

                    if (valorIngreso > 0)
                    {
                        ingresos.Add(new IngresosEmpresa
                        {
                            EmpresaId = empresaId,
                            FechaIngreso = fechaIngreso,
                            ValorIngreso = valorIngreso,
                            ValorDescontadoEnIngreso = valorTotalADescontar
                        });

                        // El descuento se registra una sola vez, en el primer ingreso generado.
                        valorTotalADescontar = 0;
                    }

                    registro.Liquidado = true;
                    registro.FechaLiquidado = fechaLiquidacionUtc;
                    registro.IdLiquidacion = idLiquidacion;
                }

                // Egresos descontados al trabajador (ya filtrados: de la persona, deducibles y no liquidados).
                foreach (var egreso in datos.Egresos)
                {
                    egreso.Liquidado = true;
                }

                await _context.LiquidacionEgresos.AddRangeAsync(datos.Egresos.Select(e => new LiquidacionEgreso
                {
                    EmpresaId = empresaId,
                    IdLiquidacion = idLiquidacion,
                    IdEgreso = e.EgresoId
                }));

                await _context.Liquidaciones.AddAsync(new Liquidacion
                {
                    IdLiquidacion = idLiquidacion,
                    EmpresaId = empresaId,
                    PersonaId = personaId,
                    FechaLiquidacion = fechaLiquidacionUtc,
                    CantidadServicios = totales.CantidadServicios,
                    TotalFacturado = totales.TotalFacturado,
                    TotalComision = totales.TotalComision,
                    TotalDeducciones = totales.TotalDeducciones,
                    NetoPagado = totales.NetoPagado,
                    Pagos = pagosLiquidacion
                });

                await _context.IngresosEmpresas.AddRangeAsync(ingresos);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return idLiquidacion;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Vista previa de la liquidación: mismos datos y cálculo que <see cref="LiquidarServiciosAsync"/>, sin guardar nada.
        /// Devuelve null si la persona no pertenece a la empresa.
        /// </summary>
        public async Task<ResumenLiquidacionViewModel?> ObtenerResumenLiquidacionAsync(int empresaId, int personaId, IReadOnlyCollection<int> idsEgresos)
        {
            var persona = await _context.Persona
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == personaId && p.EmpresaId == empresaId);

            if (persona is null)
            {
                return null;
            }

            var datos = await ObtenerDatosLiquidacionPendienteAsync(empresaId, personaId, idsEgresos, rastrear: false);
            var totales = CalcularTotalesLiquidacion(datos.Registros, datos.Configuraciones, datos.Egresos);

            return new ResumenLiquidacionViewModel
            {
                PersonaId = personaId,
                NombrePersona = ObtenerNombreCompleto(persona.Nombre, persona.Apellido),
                FechaDesde = TimeZoneHelper.ConvertUtcToColombia(datos.FechaDesdeUtc).Date,
                FechaHasta = TimeZoneHelper.ConvertUtcToColombia(datos.FechaHastaUtc).Date.AddDays(-1),
                CantidadServicios = totales.CantidadServicios,
                TotalFacturado = totales.TotalFacturado,
                TotalComision = totales.TotalComision,
                TotalDeducciones = totales.TotalDeducciones,
                NetoAPagar = totales.NetoPagado
            };
        }

        /// <summary>
        /// Datos del comprobante de una liquidación. Si la liquidación es anterior a la tabla Liquidacion,
        /// los totales se reconstruyen desde RegistroServicio/LiquidacionEgreso con la configuración actual.
        /// Devuelve null si la liquidación no existe en la empresa.
        /// </summary>
        public async Task<ComprobanteLiquidacionViewModel?> ObtenerComprobanteLiquidacionAsync(int empresaId, string idLiquidacion)
        {
            var liquidacion = await _context.Liquidaciones
                .AsNoTracking()
                .Include(l => l.Pagos)
                .FirstOrDefaultAsync(l => l.IdLiquidacion == idLiquidacion && l.EmpresaId == empresaId);

            var registros = await _context.RegistroServicio
                .AsNoTracking()
                .Include(r => r.Servicio)
                .Where(r => r.EmpresaId == empresaId && r.IdLiquidacion == idLiquidacion)
                .ToListAsync();

            if (liquidacion is null && registros.Count == 0)
            {
                return null;
            }

            var personaId = liquidacion?.PersonaId ?? registros[0].PersonaId;

            var persona = await _context.Persona
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == personaId && p.EmpresaId == empresaId);

            var empresa = await _context.Empresa
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == empresaId);

            var servicioIds = registros.Select(r => r.ServicioId).Distinct().ToList();
            var configuraciones = CrearDiccionarioConfiguraciones(await _context.ConfiguracionServicio
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId && servicioIds.Contains(c.ServicioId))
                .ToListAsync());

            var egresos = await _context.LiquidacionEgresos
                .AsNoTracking()
                .Where(le => le.EmpresaId == empresaId && le.IdLiquidacion == idLiquidacion)
                .Select(le => le.EgresoEmpresa)
                .OrderBy(e => e.FechaRegistro)
                .ToListAsync();

            var fechasServicio = registros
                .Where(r => r.FechaServicio.HasValue)
                .Select(r => r.FechaServicio!.Value)
                .ToList();

            var comprobante = new ComprobanteLiquidacionViewModel
            {
                IdLiquidacion = idLiquidacion,
                Empresa = new ComprobanteEmpresaViewModel
                {
                    Nombre = empresa?.Nombre ?? string.Empty,
                    Nit = empresa?.Nit ?? string.Empty
                },
                Persona = new ComprobantePersonaViewModel
                {
                    Id = personaId,
                    NombreCompleto = ObtenerNombreCompleto(persona?.Nombre, persona?.Apellido),
                    Cedula = persona?.Cedula ?? string.Empty
                },
                TotalPropinas = Redondear(registros.Sum(r => r.Propina)),
                PropinasEntregadas = Redondear(registros.Where(r => r.PropinaPagada).Sum(r => r.Propina)),
                PeriodoDesde = fechasServicio.Count > 0 ? TimeZoneHelper.ConvertUtcToColombia(fechasServicio.Min()) : null,
                PeriodoHasta = fechasServicio.Count > 0 ? TimeZoneHelper.ConvertUtcToColombia(fechasServicio.Max()) : null,
                Servicios = registros
                    .GroupBy(r => r.ServicioId)
                    .Select(g => new ComprobanteServicioViewModel
                    {
                        Nombre = g.First().Servicio?.Nombre ?? "Servicio desconocido",
                        Cantidad = g.Count(),
                        ValorFacturado = Redondear(g.Sum(r => r.Servicio?.Precio ?? 0m)),
                        Comision = Redondear(g.Sum(r => CalcularComisionRegistro(r, configuraciones)))
                    })
                    .OrderBy(s => s.Nombre)
                    .ToList(),
                Deducciones = egresos
                    .Select(e => new ComprobanteDeduccionViewModel
                    {
                        Fecha = DateOnly.FromDateTime(TimeZoneHelper.ConvertUtcToColombia(e.FechaRegistro)),
                        Motivo = e.Motivo ?? string.Empty,
                        Valor = e.ValorEgreso ?? 0m,
                        FormaPago = e.FormaPago
                    })
                    .ToList()
            };

            if (liquidacion is not null)
            {
                comprobante.FechaLiquidacion = TimeZoneHelper.ConvertUtcToColombia(liquidacion.FechaLiquidacion);
                comprobante.CantidadServicios = liquidacion.CantidadServicios;
                comprobante.TotalFacturado = liquidacion.TotalFacturado;
                comprobante.TotalComision = liquidacion.TotalComision;
                comprobante.TotalDeducciones = liquidacion.TotalDeducciones;
                comprobante.NetoPagado = liquidacion.NetoPagado;
                comprobante.Pagos = liquidacion.Pagos
                    .OrderBy(p => p.Id)
                    .Select(p => new PagoLiquidacionViewModel { FormaPago = p.FormaPago, Valor = p.Valor })
                    .ToList();
                return comprobante;
            }

            // Liquidación antigua: no hay snapshot, se reconstruye con la configuración actual.
            var totales = CalcularTotalesLiquidacion(registros, configuraciones, egresos);
            var fechaLiquidacionUtc = registros.Max(r => r.FechaLiquidado)
                ?? (fechasServicio.Count > 0 ? fechasServicio.Max() : DateTime.UtcNow);

            comprobante.FechaLiquidacion = TimeZoneHelper.ConvertUtcToColombia(fechaLiquidacionUtc);
            comprobante.CantidadServicios = totales.CantidadServicios;
            comprobante.TotalFacturado = totales.TotalFacturado;
            comprobante.TotalComision = totales.TotalComision;
            comprobante.TotalDeducciones = totales.TotalDeducciones;
            comprobante.NetoPagado = totales.NetoPagado;
            comprobante.EsReconstruido = true;
            return comprobante;
        }

        /// <summary>Insumos de una liquidación pendiente: servicios confirmados no liquidados en la ventana de periodicidad y egresos a descontar.</summary>
        private sealed record DatosLiquidacionPendiente(
            DateTime FechaDesdeUtc,
            DateTime FechaHastaUtc,
            List<RegistroServicio> Registros,
            Dictionary<int, ConfiguracionServicio> Configuraciones,
            List<EgresosEmpresa> Egresos);

        private sealed record TotalesLiquidacion(int CantidadServicios, decimal TotalFacturado, decimal TotalComision, decimal TotalDeducciones)
        {
            public decimal NetoPagado => TotalComision - TotalDeducciones;
        }

        private async Task<DatosLiquidacionPendiente> ObtenerDatosLiquidacionPendienteAsync(
            int empresaId,
            int personaId,
            IReadOnlyCollection<int> idsEgresos,
            bool rastrear = true)
        {
            var configGeneral = await ObtenerConfiguracionGeneralRequeridaAsync(empresaId);
            var fechaLimiteUtc = CalcularFechaLimiteUtc(configGeneral);
            var fechaHoyUtc = CalcularFechaFinUtc();

            var queryRegistros = _context.RegistroServicio
                .Where(r =>
                    r.EmpresaId == empresaId &&
                    r.PersonaId == personaId &&
                    r.FechaServicio >= fechaLimiteUtc &&
                    r.FechaServicio < fechaHoyUtc &&
                    r.Confirmado &&
                    !r.Liquidado)
                .Include(r => r.Servicio)
                .AsQueryable();

            var registros = await (rastrear ? queryRegistros : queryRegistros.AsNoTracking()).ToListAsync();

            var serviciosIds = registros.Select(r => r.ServicioId).Distinct().ToList();

            var configuraciones = CrearDiccionarioConfiguraciones(await _context.ConfiguracionServicio
                .AsNoTracking()
                .Where(c => c.EmpresaId == empresaId && serviciosIds.Contains(c.ServicioId))
                .ToListAsync());

            var egresos = new List<EgresosEmpresa>();

            if (idsEgresos.Count > 0)
            {
                // Solo egresos de la persona, deducibles, no liquidados y que no estén ya ligados a otra liquidación.
                var queryEgresos = _context.EgresosEmpresa
                    .Where(e =>
                        e.EmpresaId == empresaId &&
                        e.PersonaId == personaId &&
                        e.SeDescuenta &&
                        !e.Liquidado &&
                        idsEgresos.Contains(e.EgresoId) &&
                        !_context.LiquidacionEgresos.Any(le => le.IdEgreso == e.EgresoId));

                egresos = await (rastrear ? queryEgresos : queryEgresos.AsNoTracking()).ToListAsync();
            }

            return new DatosLiquidacionPendiente(fechaLimiteUtc, fechaHoyUtc, registros, configuraciones, egresos);
        }

        /// <summary>Cálculo único de totales, compartido por la vista previa, la liquidación y el comprobante reconstruido.</summary>
        private static TotalesLiquidacion CalcularTotalesLiquidacion(
            IReadOnlyCollection<RegistroServicio> registros,
            IReadOnlyDictionary<int, ConfiguracionServicio> configuraciones,
            IEnumerable<EgresosEmpresa> egresos)
        {
            return new TotalesLiquidacion(
                registros.Count,
                Redondear(registros.Sum(r => r.Servicio?.Precio ?? 0m)),
                Redondear(registros.Sum(r => CalcularComisionRegistro(r, configuraciones))),
                Redondear(egresos.Sum(e => e.ValorEgreso ?? 0m)));
        }

        private static decimal CalcularComisionRegistro(RegistroServicio registro, IReadOnlyDictionary<int, ConfiguracionServicio> configuraciones)
        {
            var porcentaje = configuraciones.TryGetValue(registro.ServicioId, out var config) ? config.PorcentajeTrabajador : 0m;
            return CalcularMontoPorcentaje(registro.Servicio?.Precio ?? 0m, porcentaje);
        }

        private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        /// <summary>Valida los pagos enviados contra el neto y los convierte en entidades. Lanza InvalidOperationException si no son válidos.</summary>
        private static List<LiquidacionPago> ConstruirPagosLiquidacion(string idLiquidacion, IReadOnlyCollection<PagoLiquidacionViewModel>? pagos, decimal netoPagado)
        {
            if (pagos is null || pagos.Count == 0)
            {
                return new List<LiquidacionPago>();
            }

            if (netoPagado <= 0)
            {
                throw new InvalidOperationException($"El neto a pagar es {netoPagado:N2}; no se deben registrar pagos para esta liquidación.");
            }

            var resultado = new List<LiquidacionPago>();

            foreach (var pago in pagos)
            {
                var formaPago = char.ToUpperInvariant(pago.FormaPago);
                if (formaPago != 'E' && formaPago != 'T')
                {
                    throw new InvalidOperationException($"La forma de pago '{pago.FormaPago}' no es válida. Use 'E' (efectivo) o 'T' (transferencia).");
                }

                if (pago.Valor <= 0)
                {
                    throw new InvalidOperationException("Cada pago debe tener un valor mayor a cero.");
                }

                resultado.Add(new LiquidacionPago
                {
                    IdLiquidacion = idLiquidacion,
                    FormaPago = formaPago,
                    Valor = Redondear(pago.Valor)
                });
            }

            var sumaPagos = resultado.Sum(p => p.Valor);
            if (Math.Abs(sumaPagos - netoPagado) > 0.01m)
            {
                throw new InvalidOperationException($"La suma de los pagos ({sumaPagos:N2}) debe ser igual al neto a pagar ({netoPagado:N2}).");
            }

            return resultado;
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
                            TotalPagado = total,
                            IdLiquidacion = g.Key.IdLiquidacion
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

            // Forma de pago opcional (la app anterior no la envía): si viene, debe ser 'E' o 'T'.
            char? formaPago = egresoEmpresa.FormaPago.HasValue ? char.ToUpperInvariant(egresoEmpresa.FormaPago.Value) : null;
            if (formaPago.HasValue && formaPago != 'E' && formaPago != 'T')
            {
                throw new ArgumentException("La forma de pago del egreso debe ser 'E' (efectivo) o 'T' (transferencia).", nameof(egresoEmpresa.FormaPago));
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
                SeDescuenta = egresoEmpresa.SeDescuenta,
                FormaPago = formaPago
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
                    e.FechaRegistro < finUtcExclusivo &&
                    !e.SeDescuenta)
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
                    FechaRegistro = DateOnly.FromDateTime(TimeZoneHelper.ConvertUtcToColombia(e.FechaRegistro)),
                    FormaPago = e.FormaPago
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
                    FechaRegistro = DateOnly.FromDateTime(TimeZoneHelper.ConvertUtcToColombia(e.FechaRegistro)),
                    FormaPago = e.FormaPago
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
                .Where(e => e.EmpresaId == empresaId && e.FechaRegistro >= inicioUtc && e.FechaRegistro < finUtcExclusivo && !e.SeDescuenta)
                .SumAsync(e => e.ValorEgreso ?? 0);

            // Egresos deducibles que ya se descontaron al trabajador en liquidaciones hechas dentro del rango.
            // La fecha de liquidación se toma de RegistroServicio.FechaLiquidado para cubrir también liquidaciones antiguas.
            var deduccionesRecuperadas = await _context.LiquidacionEgresos
                .Where(le =>
                    le.EmpresaId == empresaId &&
                    le.EgresoEmpresa.SeDescuenta &&
                    _context.RegistroServicio.Any(r =>
                        r.EmpresaId == empresaId &&
                        r.IdLiquidacion == le.IdLiquidacion &&
                        r.FechaLiquidado >= inicioUtc &&
                        r.FechaLiquidado < finUtcExclusivo))
                .SumAsync(le => le.EgresoEmpresa.ValorEgreso ?? 0);

            return new ConsolidadoIngresosEgresosViewModel
            {
                TotalIngresos = totalIngresos,
                TotalEgresos = totalEgresos,
                DeduccionesRecuperadas = deduccionesRecuperadas
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

            // Salidas de caja por forma de pago (si se filtró por una forma, solo se calcula esa).
            char? filtroFormaPago = !string.IsNullOrWhiteSpace(formaPago) && formaPago.Length == 1 ? formaPago[0] : null;

            var egresos = await _context.EgresosEmpresa
                .AsNoTracking()
                .Where(e =>
                    e.EmpresaId == empresaId &&
                    e.FechaRegistro >= inicioUtc &&
                    e.FechaRegistro < finUtc &&
                    e.FormaPago != null &&
                    (filtroFormaPago == null || e.FormaPago == filtroFormaPago))
                .Select(e => new { e.FormaPago, Valor = e.ValorEgreso ?? 0m })
                .ToListAsync();

            var pagosLiquidaciones = await _context.LiquidacionPagos
                .AsNoTracking()
                .Where(p =>
                    p.Liquidacion.EmpresaId == empresaId &&
                    p.Liquidacion.FechaLiquidacion >= inicioUtc &&
                    p.Liquidacion.FechaLiquidacion < finUtc &&
                    (filtroFormaPago == null || p.FormaPago == filtroFormaPago))
                .Select(p => new { p.FormaPago, p.Valor })
                .ToListAsync();

            var egresosEfectivo = egresos.Where(e => e.FormaPago == 'E').Sum(e => e.Valor);
            var egresosTransferencia = egresos.Where(e => e.FormaPago == 'T').Sum(e => e.Valor);
            var pagosEfectivo = pagosLiquidaciones.Where(p => p.FormaPago == 'E').Sum(p => p.Valor);
            var pagosTransferencia = pagosLiquidaciones.Where(p => p.FormaPago == 'T').Sum(p => p.Valor);

            return new ConsolidadoFormaPagoViewModel
            {
                CantidadTransferencia = cantidadTransferencia,
                TotalTransferencia = totalTransferencia,
                CantidadEfectivo = cantidadEfectivo,
                TotalEfectivo = totalEfectivo,
                EgresosPagadosEfectivo = egresosEfectivo,
                EgresosPagadosTransferencia = egresosTransferencia,
                PagosLiquidacionesEfectivo = pagosEfectivo,
                PagosLiquidacionesTransferencia = pagosTransferencia,
                NetoEfectivo = totalEfectivo - egresosEfectivo - pagosEfectivo,
                NetoTransferencia = totalTransferencia - egresosTransferencia - pagosTransferencia
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
