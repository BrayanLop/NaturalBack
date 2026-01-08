namespace Natural.Models.Areas.Contabilidad
{
    public class HistorialIngresoEmpresaViewModel
    {
        public DateTime Fecha { get; set; }
        public int TotalRegistros { get; set; }
        public decimal TotalIngresado { get; set; }
    }
}