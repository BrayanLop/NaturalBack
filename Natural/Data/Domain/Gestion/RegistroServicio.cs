using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Natural.Data.Domain.Gestion
{
    [Table("RegistroServicio")]
    public class RegistroServicio
    {
        [Key]
        public int Id { get; set; }

        public int PersonaId { get; set; }
        public int ServicioId { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }

        [ForeignKey("PersonaId")]
        public Persona Persona { get; set; }

        [ForeignKey("ServicioId")]
        public Servicio Servicio { get; set; }
    }
}
