using Dominio.Modelos.Areas.Configuracion;
using Dominio.Modelos.Areas.Gestion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Estructura
{
    public partial class EmpresaModel
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }

        public string? Nit { get; set; }

        public string? Direccion { get; set; }

        public string? Telefono { get; set; }

        public bool? Estado { get; set; }

        public virtual ICollection<PersonaViewModel> Personas { get; set; } = new List<PersonaViewModel>();

        public virtual ICollection<RegistroServicioViewModel> RegistroServicios { get; set; } = new List<RegistroServicioViewModel>();

        public virtual ICollection<ServicioViewModel> Servicios { get; set; } = new List<ServicioViewModel>();
        public virtual ICollection<ConfiguracionServicioModel> ConfiguracionServicios { get; set; } = new List<ConfiguracionServicioModel>();
    }
}
