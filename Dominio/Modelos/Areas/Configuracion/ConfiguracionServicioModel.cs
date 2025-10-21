using Dominio.Modelos.Areas.Estructura;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Configuracion
{
    public class ConfiguracionServicioModel
    {
        public int? Id { get; set; } // Opcional para creación

        public int EmpresaId { get; set; }

        public int ServicioId { get; set; }

        public decimal PorcentajeTrabajador { get; set; }

        public decimal PorcentajeEmpresa { get; set; }

        public bool Estado { get; set; }

        public string? NombreServicio { get; set; }
    }
}


