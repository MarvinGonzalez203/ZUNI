using System.ComponentModel.DataAnnotations;

namespace Zuni.Models.Agenda;

public sealed class DisponibilidadInput
{
    [EnumDataType(typeof(DayOfWeek))] public DayOfWeek DiaSemana { get; set; }
    [Required] public TimeOnly? HoraInicio { get; set; }
    [Required] public TimeOnly? HoraFin { get; set; }
    [Range(15, 120)] public int DuracionMinutos { get; set; } = 50;
    [EnumDataType(typeof(ModalidadDisponibilidad))] public ModalidadDisponibilidad Modalidad { get; set; }
}

public sealed class ReservaCitaInput
{
    [Required] public DateTimeOffset? Inicio { get; set; }
    [EnumDataType(typeof(ModalidadCita))] public ModalidadCita Modalidad { get; set; }
}

public sealed class CambiarCitaInput
{
    public Guid CitaId { get; set; }
    [EnumDataType(typeof(EstadoCita))] public EstadoCita Estado { get; set; }
    [StringLength(300)] public string? MotivoCancelacion { get; set; }
}

public sealed record DisponibilidadResumen(Guid Id, DayOfWeek DiaSemana, TimeOnly HoraInicio,
    TimeOnly HoraFin, int DuracionMinutos, ModalidadDisponibilidad Modalidad, bool Activo);
public sealed record HorarioDisponible(DateTime InicioUtc, DateTime FinUtc, ModalidadDisponibilidad Modalidad);
public sealed class CitaResumen
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateTime InicioUtc { get; init; }
    public DateTime FinUtc { get; init; }
    public ModalidadCita Modalidad { get; init; }
    public EstadoCita Estado { get; init; }
}

public sealed class AgendaPsicologoViewModel
{
    public DateOnly Fecha { get; init; }
    public IReadOnlyList<DisponibilidadResumen> Disponibilidades { get; init; } = [];
    public IReadOnlyList<CitaResumen> Citas { get; init; } = [];
}
public sealed class CitasEstudianteViewModel
{
    public DateOnly Fecha { get; init; }
    public bool PuedeReservar { get; init; }
    public string? Psicologo { get; init; }
    public IReadOnlyList<HorarioDisponible> Horarios { get; init; } = [];
    public IReadOnlyList<CitaResumen> Citas { get; init; } = [];
    public int Pagina { get; init; } = 1;
    public int Paginas { get; init; } = 1;
}
public sealed class HistorialCitasViewModel
{
    public string Busqueda { get; init; } = string.Empty;
    public EstadoCita? Estado { get; init; }
    public int Pagina { get; init; } = 1;
    public int Paginas { get; init; } = 1;
    public IReadOnlyList<CitaResumen> Citas { get; init; } = [];
}
