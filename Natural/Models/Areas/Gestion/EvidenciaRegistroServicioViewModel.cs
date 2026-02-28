namespace Natural.Models.Areas.Gestion;

public class EvidenciaRegistroServicioViewModel
{
    public int Id { get; set; }
    public int RegistroServicioId { get; set; }
    public string UrlEvidencia { get; set; } = null!;
    public string? NombreArchivo { get; set; }
    public DateTime FechaSubida { get; set; }
}

public class SubirEvidenciaRequest
{
    public int RegistroServicioId { get; set; }
    public IFormFile Archivo { get; set; } = null!;
}
