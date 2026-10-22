namespace Natural.Models.Areas.Contabilidad
{
    /// <summary>Cuerpo opcional de POST LiquidarPersona.</summary>
    public class LiquidarPersonaRequest
    {
        /// <summary>Cómo se pagó el neto. Opcional: la app anterior no lo envía.</summary>
        public List<PagoLiquidacionViewModel>? Pagos { get; set; }
    }

    public class PagoLiquidacionViewModel
    {
        /// <summary>'E' efectivo, 'T' transferencia.</summary>
        public char FormaPago { get; set; }

        public decimal Valor { get; set; }
    }

    public class LiquidacionCreadaViewModel
    {
        /// <summary>Id de la liquidación creada; null si no había servicios confirmados pendientes.</summary>
        public string? IdLiquidacion { get; set; }
    }

    /// <summary>Vista previa de lo que se liquidaría hoy a una persona.</summary>
    public class ResumenLiquidacionViewModel
    {
        public int PersonaId { get; set; }
        public string NombrePersona { get; set; } = string.Empty;
        /// <summary>Inicio de la ventana de periodicidad (fecha Colombia).</summary>
        public DateTime FechaDesde { get; set; }
        /// <summary>Fin de la ventana de periodicidad, hoy (fecha Colombia).</summary>
        public DateTime FechaHasta { get; set; }
        public int CantidadServicios { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalComision { get; set; }
        public decimal TotalDeducciones { get; set; }
        public decimal NetoAPagar { get; set; }
    }

    public class ComprobanteLiquidacionViewModel
    {
        public string IdLiquidacion { get; set; } = string.Empty;
        public ComprobanteEmpresaViewModel Empresa { get; set; } = new();
        public ComprobantePersonaViewModel Persona { get; set; } = new();
        /// <summary>Fecha y hora de la liquidación (hora Colombia).</summary>
        public DateTime FechaLiquidacion { get; set; }
        /// <summary>Fecha del primer servicio liquidado (hora Colombia).</summary>
        public DateTime? PeriodoDesde { get; set; }
        /// <summary>Fecha del último servicio liquidado (hora Colombia).</summary>
        public DateTime? PeriodoHasta { get; set; }
        public int CantidadServicios { get; set; }
        /// <summary>Detalle por servicio, calculado con precios y porcentajes actuales (informativo).</summary>
        public List<ComprobanteServicioViewModel> Servicios { get; set; } = new();
        public decimal TotalFacturado { get; set; }
        public decimal TotalComision { get; set; }
        public List<ComprobanteDeduccionViewModel> Deducciones { get; set; } = new();
        public decimal TotalDeducciones { get; set; }
        public decimal NetoPagado { get; set; }
        /// <summary>Propinas de los servicios liquidados (100% del trabajador). Informativo: no hace parte del neto.</summary>
        public decimal TotalPropinas { get; set; }
        /// <summary>Parte de <see cref="TotalPropinas"/> que ya se marcó como entregada.</summary>
        public decimal PropinasEntregadas { get; set; }
        public decimal PropinasPendientes => TotalPropinas - PropinasEntregadas;
        public List<PagoLiquidacionViewModel> Pagos { get; set; } = new();
        /// <summary>True si la liquidación es anterior a la tabla Liquidacion y sus totales se reconstruyeron con la configuración actual.</summary>
        public bool EsReconstruido { get; set; }
    }

    public class ComprobanteEmpresaViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Nit { get; set; } = string.Empty;
    }

    public class ComprobantePersonaViewModel
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
    }

    public class ComprobanteServicioViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal ValorFacturado { get; set; }
        public decimal Comision { get; set; }
    }

    public class ComprobanteDeduccionViewModel
    {
        /// <summary>Fecha de registro del egreso (Colombia).</summary>
        public DateOnly Fecha { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public char? FormaPago { get; set; }
    }
}
