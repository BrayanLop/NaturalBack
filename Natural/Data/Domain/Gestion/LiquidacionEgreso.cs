using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

public partial class LiquidacionEgreso
{
    public string IdLiquidacion { get; set; }

    public long IdEgreso { get; set; }
    public int EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual EgresosEmpresa EgresoEmpresa { get; set; } = null!;
}
