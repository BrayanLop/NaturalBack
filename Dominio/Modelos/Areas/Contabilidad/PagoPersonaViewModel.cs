using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Contabilidad
{
    public class PagoPersonaViewModel
    {
        public string NombrePersona { get; set; }
        public decimal TotalPagado { get; set; }
        public List<ServicioDetalleViewModel> Servicios { get; set; } = new();
    }

}
