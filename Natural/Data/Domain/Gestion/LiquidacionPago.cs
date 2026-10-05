namespace Natural.Data.Domain.Gestion;

/// <summary>Forma en que se pagó (total o parcialmente) el neto de una liquidación.</summary>
public class LiquidacionPago
{
    public int Id { get; set; }

    public string IdLiquidacion { get; set; } = null!;

    /// <summary>'E' efectivo, 'T' transferencia.</summary>
    public char FormaPago { get; set; }

    public decimal Valor { get; set; }

    public virtual Liquidacion Liquidacion { get; set; } = null!;
}
