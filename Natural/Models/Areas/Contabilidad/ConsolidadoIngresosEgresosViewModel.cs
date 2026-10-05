namespace Natural.Models.Areas.Contabilidad
{
    public class ConsolidadoIngresosEgresosViewModel
    {
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Consolidado => TotalIngresos - TotalEgresos;

        /// <summary>
        /// Egresos con SeDescuenta = true que quedaron descontados al trabajador (vía LiquidacionEgreso)
        /// en liquidaciones hechas dentro del rango. Informativo: no altera TotalEgresos ni Consolidado.
        /// </summary>
        public decimal DeduccionesRecuperadas { get; set; }
    }
}
