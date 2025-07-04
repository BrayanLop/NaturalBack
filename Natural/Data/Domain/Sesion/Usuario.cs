using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Sesion
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; }
        public string ClaveHash { get; set; }
        public bool Estado { get; set; }
        public int PersonaId { get; set; }
        public virtual Persona Persona { get; set; }
    }
}
