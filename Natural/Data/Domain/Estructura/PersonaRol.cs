namespace Natural.Data.Domain.Estructura;

public partial class PersonaRol
{
    public int PersonaId { get; set; }

    public string RolCodigo { get; set; } = null!;

    public virtual Persona Persona { get; set; } = null!;

    public virtual Rol RolCodigoNavigation { get; set; } = null!;
}
