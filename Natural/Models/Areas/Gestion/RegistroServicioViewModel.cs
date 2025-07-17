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
    }
}
