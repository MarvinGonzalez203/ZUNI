namespace Zuni.Models.Atencion;
public sealed class AsignacionEstudiantePsicologo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilEstudianteId { get; set; }
    public string PsicologoUsuarioId { get; set; } = string.Empty;
    public DateTime FechaAsignacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFinalizacionUtc { get; set; }
}
