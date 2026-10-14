namespace Zuni.Models.Agenda;

public sealed class Cita
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilEstudianteId { get; set; }
    public string PsicologoUsuarioId { get; set; } = string.Empty;
    public Guid? DisponibilidadPsicologoId { get; set; }
    public bool EsImprevista { get; set; }
    public bool FinAproximado { get; set; }
    public bool ProgramadaDirectamente { get; set; }
    public string? ProgramadorUsuarioId { get; set; }
    public string? ProgramadorRol { get; set; }
    public DateTime FechaHoraInicioUtc { get; set; }
    public DateTime FechaHoraFinUtc { get; set; }
    public ModalidadCita Modalidad { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Reservada;
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public string? MotivoCancelacion { get; set; }
    public string? EnlaceReunion { get; set; }
    public DateTime? FechaEnlaceUtc { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}

public sealed class EnlaceReunionUsado
{
    public string Huella { get; set; } = "";
    public Guid CitaId { get; set; }
    public DateTime FechaUtc { get; set; }
}
