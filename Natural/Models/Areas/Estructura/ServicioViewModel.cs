namespace Natural.Models.Areas.Estructura
{
    public class ServicioViewModel
    {
        public int Id { get; set; }                
        public string Nombre { get; set; }         
        public string Descripcion { get; set; }    
        public decimal Precio { get; set; }        
        public bool Disponible { get; set; }       
        public DateTime FechaCreacion { get; set; }
        public int? EmpresaId { get; set; }
    }

}
