namespace Natural.Models.Areas.Gestion
{
    public class RegistroServicioViewModel
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
        public bool Confirmado { get; set; }
        public bool Rechazado { get; set; }
        public string? MotivoRechazo { get; set; }
        public decimal Propina { get; set; }
        public string? Observaciones { get; set; }
        public decimal? ValorServicio { get; set; }
        public decimal? PorcentajeTrabajador { get; set; }
        public decimal? Comision { get; set; }
        public char? FormaPago { get; set; }
        public List<EvidenciaRegistroServicioViewModel> Evidencias { get; set; } = new List<EvidenciaRegistroServicioViewModel>();
    }
}
