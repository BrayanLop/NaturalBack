using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

public partial class IngresosEmpresa
{
    public long IngresoId { get; set; }

    public DateOnly FechaIngreso { get; set; }

    public decimal? ValorIngreso { get; set; }
    public decimal? ValorDescontadoEnIngreso { get; set; }

    public int? EmpresaId { get; set; }

    public virtual Empresa Empresa { get; set; }
}
