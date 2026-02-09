using System.ComponentModel.DataAnnotations;

namespace Natural.Models.Areas.Contabilidad
{
    public class EgresoEmpresaViewModel
    {
        public int EgresoId { get; set; }

        public int EmpresaId { get; set; }

        public int PersonaId { get; set; }
        public string NombrePersona { get; set; }

        public decimal ValorEgreso { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public DateOnly? FechaRegistro { get; set; }

        public bool SeDescuenta { get; set; }
    }
}