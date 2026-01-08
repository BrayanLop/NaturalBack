public class PagoLiquidadoViewModel
{
    public int PersonaId { get; set; }
    public string NombrePersona { get; set; } = string.Empty;
    public DateTime FechaLiquidacion { get; set; }
    public decimal TotalPagado { get; set; }
}
