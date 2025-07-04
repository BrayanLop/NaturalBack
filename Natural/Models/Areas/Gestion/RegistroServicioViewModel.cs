namespace Natural.Models.Areas.Gestion
{
    public class RegistroServicioViewModel
    {
        public int PersonaId { get; set; }
        public int ServicioId { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public int? EmpresaId { get; set; }
    }
}
