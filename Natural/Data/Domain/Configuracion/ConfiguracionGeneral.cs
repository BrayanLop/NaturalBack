using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Configuracion;

public partial class ConfiguracionGeneral
{
    public string Periodicidad { get; set; } = null!;

    public short ValorPeriodicidad { get; set; }

    public int EmpresaId { get; set; }

    public virtual Empresa Empresa { get; set; }
}
