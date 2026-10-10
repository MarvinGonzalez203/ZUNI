namespace Zuni.Models.Atencion;
public sealed class ConsentimientoAtencion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SolicitudAtencionId { get; set; }
    public string VersionConsentimiento { get; set; } = string.Empty;
    public bool Aceptado { get; set; }
    public DateTime FechaRespuestaUtc { get; set; } = DateTime.UtcNow;
}
