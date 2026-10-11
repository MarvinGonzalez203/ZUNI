namespace Zuni.Models.Agenda;

public sealed class Cita
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilEstudianteId { get; set; }
    public string PsicologoUsuarioId { get; set; } = string.Empty;
    public Guid DisponibilidadPsicologoId { get; set; }
    public DateTime FechaHoraInicioUtc { get; set; }
    public DateTime FechaHoraFinUtc { get; set; }
    public ModalidadCita Modalidad { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Reservada;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public string? MotivoCancelacion { get; set; }
}
