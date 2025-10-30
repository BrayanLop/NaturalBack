using Natural.Data.Domain.Gestion;
using Natural.Data.Domain.Sesion;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Natural.Data.Domain.Estructura;

public partial class Persona
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}
