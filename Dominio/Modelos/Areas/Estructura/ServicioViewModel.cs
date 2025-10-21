using Dominio.Modelos.Areas.Configuracion;
using Dominio.Modelos.Areas.Gestion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Estructura
{
    public partial class ServicioViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public bool Disponible { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int? EmpresaId { get; set; }

    }
}
