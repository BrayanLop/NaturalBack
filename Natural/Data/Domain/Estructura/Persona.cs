using Natural.Data.Domain.Gestion;

namespace Natural.Data.Domain.Estructura;

public partial class Persona
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Cedula { get; set; } = null!;

    public string Residencia { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int Edad { get; set; }

    public string Celular { get; set; } = null!;

    public DateTime FechaNacimiento { get; set; }

    public int? EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual ICollection<RegistroServicio> RegistroServicios { get; set; } = new List<RegistroServicio>();
}
