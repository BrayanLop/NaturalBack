public class PagoPersonaViewModel
{
    public int PersonaId { get; set; }
    public string NombrePersona { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal TotalSinConfirmar { get; set; }
    public List<ServicioDetalleViewModel> Servicios { get; set; } = new();
}
