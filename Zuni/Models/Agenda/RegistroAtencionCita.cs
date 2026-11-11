namespace Zuni.Models.Agenda;

public sealed class RegistroAtencionCita
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CitaId { get; set; }
    // Opcional cuando la atención la inició el profesional: no inventar una solicitud
    // del estudiante. La cita conserva estudiante, profesional, propiedad y auditoría.
    public Guid? SolicitudAtencionId { get; set; }
    public bool Asistio { get; set; }
    public string? ResultadoProfesionalPrivado { get; set; }
    public string? ResenaRecomendacionesEstudiante { get; set; }
    public string? ResultadoBreveEstudiante { get; set; }
    public bool RecomiendaOtraCita { get; set; }
    public string? IndicacionesProximaCita { get; set; }
    public DateTime FechaCierreUtc { get; set; }
}
