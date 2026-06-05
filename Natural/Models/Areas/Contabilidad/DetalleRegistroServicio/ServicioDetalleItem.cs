public class ServicioDetalleItem
{
    public int RegistroServicioId { get; set; }
    public string NombreServicio { get; set; }
    public DateTime Hora { get; set; }
    public bool Confirmado { get; set; }
    public decimal ValorTrabajador { get; set; }
    public decimal Propina { get; set; }
    public bool PropinaPagada { get; set; }
}
