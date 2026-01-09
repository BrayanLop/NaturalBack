using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

public partial class EgresosEmpresa
{
    public long EgresoId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public decimal? ValorEgreso { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public int? EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public int? PersonaId { get; set; }

    public virtual Persona? Persona { get; set; }
}
