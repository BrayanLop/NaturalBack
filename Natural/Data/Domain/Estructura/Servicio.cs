using Natural.Data.Domain.Gestion;

namespace Natural.Data.Domain.Estructura;

public partial class Servicio
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal Precio { get; set; }

    public bool Disponible { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int? EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual ICollection<RegistroServicio> RegistroServicios { get; set; } = new List<RegistroServicio>();
}
