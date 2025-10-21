using Dominio.Modelos.Areas.Estructura;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Sesion
{
    public class UsuarioModel
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; }
        public string ClaveHash { get; set; }
        public bool Estado { get; set; }
        public int PersonaId { get; set; }
        public virtual PersonaViewModel Persona { get; set; }
    }
}
