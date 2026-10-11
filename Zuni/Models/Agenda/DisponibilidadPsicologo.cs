namespace Zuni.Models.Agenda;

public enum ModalidadDisponibilidad { Presencial, Virtual, Ambas }
public enum ModalidadCita { Presencial, Virtual }
public enum EstadoCita { Reservada, Cancelada, Completada, NoAsistio }

// Horario semanal en America/Guatemala. No representa una cita ni un expediente.
public sealed class DisponibilidadPsicologo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PsicologoUsuarioId { get; set; } = string.Empty;
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int DuracionMinutos { get; set; }
    public ModalidadDisponibilidad Modalidad { get; set; }
    public bool Activo { get; set; } = true;
}
