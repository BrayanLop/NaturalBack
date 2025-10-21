using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Contabilidad.DetalleRegistroServicio
{
    public class DetalleServicioPorDiaViewModel
    {
        public DateTime Fecha { get; set; }
        public int TotalServicios { get; set; }
        public List<ServicioDetalleItem> Servicios { get; set; } = new();
    }
}
