using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Estructura
{
    public partial class PersonaRolModel
    {
        public int PersonaId { get; set; }

        public string RolCodigo { get; set; } = null!;

        public virtual PersonaViewModel Persona { get; set; } = null!;

        public virtual RolModel RolCodigoNavigation { get; set; } = null!;
    }
}
