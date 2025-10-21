using Dominio.Modelos.Areas.Estructura;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio.Modelos.Areas.Gestion
{
    public partial class RegistroServicioViewModel
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public int ServicioId { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public int? EmpresaId { get; set; }
        public string? NombrePersona { get; set; }
        public string? NombreServicio { get; set; }
        public DateTime? FechaServicio { get; set; }
        public bool Liquidado { get; set; }
    }
}
