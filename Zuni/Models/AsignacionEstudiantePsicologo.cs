namespace Zuni.Models;

public sealed class AsignacionEstudiantePsicologo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilEstudianteId { get; set; }
    public string PsicologoUsuarioId { get; set; } = string.Empty;
    public DateTime FechaAsignacionUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public PerfilEstudiante PerfilEstudiante { get; set; } = null!;
    public ApplicationUser PsicologoUsuario { get; set; } = null!;
}
