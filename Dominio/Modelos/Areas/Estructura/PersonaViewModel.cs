using Dominio.Modelos.Areas.Gestion;
using Dominio.Modelos.Areas.Sesion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Estructura
{
    public partial class PersonaViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Cedula { get; set; }
        public string Residencia { get; set; }
        public string Email { get; set; }
        public int Edad { get; set; }
        public string? Celular { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public int? EmpresaId { get; set; }
        public string Rol { get; set; }

    }
}
