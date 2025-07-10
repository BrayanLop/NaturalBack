using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Configuracion
{
    public class ConfiguracionServicio
    {
        public int Id { get; set; }

        public int EmpresaId { get; set; }

        public int ServicioId { get; set; }

        public decimal PorcentajeTrabajador { get; set; }

        public decimal PorcentajeEmpresa { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Estado { get; set; } = true;

        // Relaciones (opcional para navegación)
        public virtual Empresa Empresa { get; set; }
        public virtual Servicio Servicio { get; set; }
    }
}
