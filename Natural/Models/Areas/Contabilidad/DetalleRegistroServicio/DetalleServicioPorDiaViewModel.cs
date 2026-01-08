public class DetalleServicioPorDiaViewModel
{
    public DateTime Fecha { get; set; }
    public int TotalServicios { get; set; }
    public decimal LiquidacionServicios { get; set; }

    public List<ServicioDetalleItem> Servicios { get; set; } = new();
}
