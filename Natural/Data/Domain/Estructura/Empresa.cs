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
}
