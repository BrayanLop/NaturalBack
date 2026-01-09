using Natural.Data.Domain.Configuracion;
using Natural.Data.Domain.Gestion;

namespace Natural.Data.Domain.Estructura;

public partial class Empresa
{
    public int Id { get; set; }

    public string? Nombre { get; set; }

    public string? Nit { get; set; }

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public bool? Estado { get; set; }

    public virtual ICollection<Persona> Personas { get; set; } = new List<Persona>();

    public virtual ICollection<RegistroServicio> RegistroServicios { get; set; } = new List<RegistroServicio>();

    public virtual ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
    public virtual ICollection<ConfiguracionServicio> ConfiguracionServicios { get; set; } = new List<ConfiguracionServicio>();
    public virtual ICollection<IngresosEmpresa> IngresosEmpresas { get; set; } = new List<IngresosEmpresa>();
    public virtual ICollection<EgresosEmpresa> EgresosEmpresas { get; set; } = new List<EgresosEmpresa>();
    public virtual ICollection<ConfiguracionGeneral> ConfiguracionGeneral { get; set; } = new List<ConfiguracionGeneral>();

}
