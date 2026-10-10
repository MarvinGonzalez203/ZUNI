namespace Zuni.Models.Atencion;
public sealed class EventoAccesoClinico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioId { get; set; } = string.Empty;
    public Guid SolicitudAtencionId { get; set; }
    public string TipoAcceso { get; set; } = "EXPEDIENTE_CONSULTADO";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}
