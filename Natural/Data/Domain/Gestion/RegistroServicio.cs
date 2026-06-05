using Natural.Data.Domain.Estructura;

namespace Natural.Data.Domain.Gestion;

public partial class RegistroServicio
{
    public int Id { get; set; }

    public int PersonaId { get; set; }

    public int ServicioId { get; set; }

    public int? EmpresaId { get; set; }

    public virtual Empresa? Empresa { get; set; }

    public virtual Persona Persona { get; set; } = null!;

    public virtual Servicio Servicio { get; set; } = null!;
    
    public DateTime? FechaServicio { get; set; }
    
    public bool Liquidado { get; set; }

    public bool Confirmado { get; set; }

    /// <summary>Servicio rechazado por el administrador. Excluyente con Confirmado.</summary>
    public bool Rechazado { get; set; }

    /// <summary>Motivo del rechazo (cuando Rechazado = true).</summary>
    public string? MotivoRechazo { get; set; }

    /// <summary>Propina recibida. Pertenece 100% al trabajador, no genera utilidad para la empresa.</summary>
    public decimal Propina { get; set; }

    /// <summary>Observaciones registradas por el trabajador al crear el servicio.</summary>
    public string? Observaciones { get; set; }

    // --- Snapshot histórico inmutable (capturado al registrar el servicio) ---

    /// <summary>Valor del servicio al momento del registro. No cambia si el servicio se edita después.</summary>
    public decimal? ValorServicio { get; set; }

    /// <summary>Porcentaje del trabajador aplicado al momento del registro.</summary>
    public decimal? PorcentajeTrabajador { get; set; }

    /// <summary>Comisión generada para el trabajador al momento del registro (ValorServicio * Porcentaje / 100).</summary>
    public decimal? Comision { get; set; }

    public char? FormaPago { get; set; }

    public DateTime? FechaLiquidado { get; set; }

    public string? IdLiquidacion { get; set; }

    public virtual ICollection<EvidenciaRegistroServicio> Evidencias { get; set; } = new List<EvidenciaRegistroServicio>();
}
