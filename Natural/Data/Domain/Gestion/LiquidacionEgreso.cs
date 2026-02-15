using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

public partial class LiquidacionEgreso
{
    public int IdLiquidacionEgreso { get; set; }

    public string IdLiquidacion { get; set; }

    public int IdEgreso { get; set; }
    public int EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual EgresosEmpresa EgresoEmpresa { get; set; } = null!;
}
