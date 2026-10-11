namespace Zuni.Models.Agenda;

public enum ModalidadDisponibilidad { Presencial, Virtual, Ambas }
public enum ModalidadCita { Presencial, Virtual }
// Los valores 0..3 ya están persistidos: nunca renumerarlos.
public enum EstadoCita { Reservada = 0, Cancelada = 1, Completada = 2, NoAsistio = 3, Solicitada = 4, Confirmada = 5, Rechazada = 6 }

// Horario semanal en America/Guatemala. No representa una cita ni un expediente.
public sealed class DisponibilidadPsicologo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PsicologoUsuarioId { get; set; } = string.Empty;
    // Null conserva la regla semanal; con valor, este intervalo pertenece solo a esa fecha.
    public Guid? ConfiguracionAgendaDiaId { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int DuracionMinutos { get; set; }
    public ModalidadDisponibilidad Modalidad { get; set; }
    public bool Activo { get; set; } = true;
}
