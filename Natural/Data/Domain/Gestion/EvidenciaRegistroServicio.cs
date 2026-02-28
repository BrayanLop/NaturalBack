using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Natural.Data.Domain.Gestion;

public class EvidenciaRegistroServicio
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int RegistroServicioId { get; set; }

    public string UrlEvidencia { get; set; } = null!;

    public string? NombreArchivo { get; set; }

    public DateTime FechaSubida { get; set; }

    public int? EmpresaId { get; set; }

    [ForeignKey(nameof(RegistroServicioId))]
    public virtual RegistroServicio RegistroServicio { get; set; } = null!;
}
