public class PagoLiquidadoViewModel
{
    public int PersonaId { get; set; }
    public string NombrePersona { get; set; } = string.Empty;
    public DateTime FechaLiquidacion { get; set; }
    public decimal TotalPagado { get; set; }
    /// <summary>Id de la liquidación, para abrir el comprobante (puede ser null en registros muy antiguos).</summary>
    public string? IdLiquidacion { get; set; }
}
