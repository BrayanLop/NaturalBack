using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

/// <summary>
/// Snapshot de una liquidación de servicios a un trabajador. Los montos se guardan al momento
/// de liquidar y no se recalculan después (aunque cambien precios o porcentajes).
/// El Id es el mismo GUID que queda en <see cref="RegistroServicio.IdLiquidacion"/> y en <see cref="LiquidacionEgreso.IdLiquidacion"/>.
/// </summary>
public class Liquidacion
{
    public string IdLiquidacion { get; set; } = null!;

    public int EmpresaId { get; set; }

    public int PersonaId { get; set; }

    /// <summary>Fecha y hora de la liquidación en UTC.</summary>
    public DateTime FechaLiquidacion { get; set; }

    public int CantidadServicios { get; set; }

    /// <summary>Precio completo de los servicios liquidados.</summary>
    public decimal TotalFacturado { get; set; }

    /// <summary>Parte del trabajador (comisión) de los servicios liquidados.</summary>
    public decimal TotalComision { get; set; }

    /// <summary>Egresos descontados al trabajador en esta liquidación.</summary>
    public decimal TotalDeducciones { get; set; }

    /// <summary>TotalComision − TotalDeducciones.</summary>
    public decimal NetoPagado { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual Persona? Persona { get; set; }

    public virtual ICollection<LiquidacionPago> Pagos { get; set; } = new List<LiquidacionPago>();
}
