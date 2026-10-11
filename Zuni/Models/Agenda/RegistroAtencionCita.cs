namespace Zuni.Models.Agenda;

public sealed class RegistroAtencionCita
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CitaId { get; set; }
    // Vinculación estable para autorización y auditoría; nunca procede del formulario.
    public Guid SolicitudAtencionId { get; set; }
    public bool Asistio { get; set; }
    public string? ResultadoProfesionalPrivado { get; set; }
    public string? ResenaRecomendacionesEstudiante { get; set; }
    public string? ResultadoBreveEstudiante { get; set; }
    public bool RecomiendaOtraCita { get; set; }
    public string? IndicacionesProximaCita { get; set; }
    public DateTime FechaCierreUtc { get; set; }
}
