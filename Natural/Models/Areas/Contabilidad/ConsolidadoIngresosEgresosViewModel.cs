namespace Natural.Models.Areas.Contabilidad
{
 public class ConsolidadoIngresosEgresosViewModel
 {
 public decimal TotalIngresos { get; set; }
 public decimal TotalEgresos { get; set; }
 public decimal Consolidado => TotalIngresos - TotalEgresos;
 }
}