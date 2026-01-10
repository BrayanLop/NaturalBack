using Natural.Data.Domain.Sesion;

namespace Natural.Data.Domain.Estructura;

public partial class UsuarioRol
{
    public int UsuarioId { get; set; }

    public string RolCodigo { get; set; } = null!;

    public virtual Usuario Persona { get; set; } = null!;

    public virtual Rol RolCodigoNavigation { get; set; } = null!;
}
