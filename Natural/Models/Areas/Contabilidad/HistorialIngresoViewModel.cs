namespace Natural.Models.Areas.Contabilidad
{
    public class HistorialIngresoViewModel
    {
        public int PersonaId { get; set; }
        public string NombrePersona { get; set; } = string.Empty;
        public DateTime FechaIngreso { get; set; }
        public int TotalServicios { get; set; }
        public decimal TotalEstimado { get; set; }
    }
}