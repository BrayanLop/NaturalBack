using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

public partial class RegistroServicio
{
    public int Id { get; set; }

    public int PersonaId { get; set; }

    public int ServicioId { get; set; }

    public int? EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual Persona Persona { get; set; } = null!;

    public virtual Servicio Servicio { get; set; } = null!;
    public DateTime? FechaServicio { get; set; }
}
