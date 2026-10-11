namespace Zuni.Models.BigFive;

// Sin navegación desde usuarios/perfiles: el contenido no se carga en administración.
public sealed class ParticipacionBigFive
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SolicitudAtencionId { get; set; }
    public Guid PerfilEstudianteId { get; set; }
    public string VersionInstrumento { get; set; } = string.Empty;
    public string VersionConsentimiento { get; set; } = string.Empty;
    public string TextoConsentimiento { get; set; } = string.Empty;
    public DateTime FechaConsentimientoUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public DateTime? FechaRetiroConsentimientoUtc { get; set; }
    public decimal? Apertura { get; set; }
    public decimal? Responsabilidad { get; set; }
    public decimal? Extraversion { get; set; }
    public decimal? Amabilidad { get; set; }
    public decimal? Neuroticismo { get; set; }
    public int Revision { get; set; }
}
