public class PagoPersonaViewModel
{
    public string NombrePersona { get; set; }
    public decimal TotalPagado { get; set; }
    public List<ServicioDetalleViewModel> Servicios { get; set; } = new();
}
