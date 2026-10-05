using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

public partial class EgresosEmpresa
{
    public int EgresoId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public decimal? ValorEgreso { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public int? EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public int? PersonaId { get; set; }

    public virtual Persona? Persona { get; set; }
    public bool SeDescuenta { get; set; }
    public bool Liquidado { get; set; }

    /// <summary>'E' efectivo, 'T' transferencia. Null en egresos antiguos o registrados sin forma de pago.</summary>
    public char? FormaPago { get; set; }
}
