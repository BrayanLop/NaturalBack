namespace Natural.Models.Areas.Configuracion
{
    public class ConfiguracionServicioViewModel
    {
        public int? Id { get; set; } // Opcional para creación

        public int EmpresaId { get; set; }

        public int ServicioId { get; set; }

        public decimal PorcentajeTrabajador { get; set; }

        public decimal PorcentajeEmpresa { get; set; }

        public bool Estado { get; set; }
    }
}
