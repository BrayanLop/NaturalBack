using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Estructura
{
    public partial class RolModel
    {
        public string Codigo { get; set; } = null!;

        public string Nombre { get; set; } = null!;

        public bool Estado { get; set; }
    }

}
