namespace Natural.Models.Areas.Contabilidad
{
    public class ConsolidadoFormaPagoViewModel
    {
        public int CantidadTransferencia { get; set; }
        /// <summary>Precio completo de los servicios cobrados por transferencia en el rango.</summary>
        public decimal TotalTransferencia { get; set; }
        public int CantidadEfectivo { get; set; }
        /// <summary>Precio completo de los servicios cobrados en efectivo en el rango.</summary>
        public decimal TotalEfectivo { get; set; }

        /// <summary>Egresos registrados en el rango con forma de pago efectivo (incluye los que se descuentan al trabajador, porque también salen de caja).</summary>
        public decimal EgresosPagadosEfectivo { get; set; }
        /// <summary>Egresos registrados en el rango con forma de pago transferencia (incluye los que se descuentan al trabajador).</summary>
        public decimal EgresosPagadosTransferencia { get; set; }
        /// <summary>Suma de pagos en efectivo de las liquidaciones hechas en el rango (solo liquidaciones con pagos registrados).</summary>
        public decimal PagosLiquidacionesEfectivo { get; set; }
        /// <summary>Suma de pagos por transferencia de las liquidaciones hechas en el rango (solo liquidaciones con pagos registrados).</summary>
        public decimal PagosLiquidacionesTransferencia { get; set; }
        /// <summary>TotalEfectivo − EgresosPagadosEfectivo − PagosLiquidacionesEfectivo.</summary>
        public decimal NetoEfectivo { get; set; }
        /// <summary>TotalTransferencia − EgresosPagadosTransferencia − PagosLiquidacionesTransferencia.</summary>
        public decimal NetoTransferencia { get; set; }
    }
}
