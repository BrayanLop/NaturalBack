using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Gestion
{
    public class ConsolidadoDiaViewModel
    {
        public int PersonaId { get; set; }
        public string NombrePersona { get; set; } = string.Empty;
        public int CantidadServicios { get; set; }
    }
}
