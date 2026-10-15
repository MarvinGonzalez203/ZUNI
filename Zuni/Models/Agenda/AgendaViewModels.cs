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
    // Solo para horarios "Sin duración establecida": duración de ESTA cita (15 a 120, múltiplos de 15).
    // En horarios con duración fija se ignora y se usa la duración del horario.
    [Range(15, 120)] public int? DuracionMinutos { get; set; }
}

public sealed class CambiarCitaInput
{
    [StringLength(500), Display(Name = "Dirección o lugar de atención presencial")] public string? DireccionAtencion { get; set; }
    public Guid Revision { get; set; }
    public string? EnlaceReunion { get; set; }
    public bool AccesoRestringidoRevisado { get; set; }
    public Guid CitaId { get; set; }
    [EnumDataType(typeof(EstadoCita))] public EstadoCita Estado { get; set; }
    // Validación condicional en el servicio: solo la transición a Cancelada exige motivo.
    public string? MotivoCancelacion { get; set; }
}

public sealed record DisponibilidadResumen(Guid Id, DayOfWeek DiaSemana, TimeOnly HoraInicio,
    TimeOnly HoraFin, int DuracionMinutos, ModalidadDisponibilidad Modalidad, bool Activo);
public sealed record HorarioDisponible(DateTime InicioUtc, DateTime FinUtc, ModalidadDisponibilidad Modalidad);
public sealed class CitaResumen
{
    public string? DireccionAtencion { get; init; }
    public Guid Revision { get; init; }
    public string? ProgramadorRol { get; init; }
    public bool EsImprevista { get; init; }
    public bool FinAproximado { get; init; }
    public bool EnlacePreparado { get; init; }
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateTime InicioUtc { get; init; }
    public DateTime FinUtc { get; init; }
    public ModalidadCita Modalidad { get; init; }
    public EstadoCita Estado { get; init; }
    public string? MotivoCancelacion { get; init; }
    public bool TieneRegistroAtencion { get; init; }
}

public sealed class AgendaPsicologoViewModel
{
    public AlmuerzoInput Almuerzo { get; init; } = new();
    public CalendarioAgendaViewModel Calendario { get; init; } = new();
    public DateOnly Fecha { get; init; }
    public IReadOnlyList<DisponibilidadResumen> Disponibilidades { get; init; } = [];
    public IReadOnlyList<CitaResumen> Citas { get; init; } = [];
}
public sealed class CitasEstudianteViewModel
{
    public bool MayoriaEdadVerificada { get; init; }
    public CalendarioAgendaViewModel Calendario { get; init; } = new();
    public DateOnly Fecha { get; init; }
    public bool PuedeReservar { get; init; }
    public string? Psicologo { get; init; }
    public IReadOnlyList<HorarioDisponible> Horarios { get; init; } = [];
    public IReadOnlyList<CitaResumen> Citas { get; init; } = [];
    public int Pagina { get; init; } = 1;
    public int Paginas { get; init; } = 1;
}

public sealed class HorarioDiaInput
{
    [StringLength(500)] public string? DireccionAtencion { get; set; }
    public Guid? PerfilEstudianteId { get; set; }
    [Required] public DateOnly? Fecha { get; set; }
    [Required] public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public int DuracionMinutos { get; set; } = 50;
    [EnumDataType(typeof(ModalidadDisponibilidad))] public ModalidadDisponibilidad Modalidad { get; set; }
}

// DTO de franjas: deliberadamente no contiene identidades de estudiantes ni citas ajenas.
public sealed record FranjaAgenda(DateTime InicioUtc, DateTime FinUtc, ModalidadDisponibilidad Modalidad,
    bool Ocupada, bool Variable);
public sealed record IntervaloAgenda(Guid Id, TimeOnly Inicio, TimeOnly Fin, int DuracionMinutos,
    ModalidadDisponibilidad Modalidad, bool TieneCitasActivas);
public sealed class DiaAgendaViewModel
{
    public int PendientesPorRevisar { get; init; }
    public bool Consultable { get; init; }
    public IReadOnlyList<CitaCalendarioResumen> Citas { get; set; } = [];
    public DateOnly Fecha { get; init; }
    public bool Editable { get; init; }
    public bool Bloqueado { get; init; }
    public bool UsaBaseSemanal { get; init; }
    public bool TieneCitasActivas { get; init; }
    public bool CitaPropiaActiva { get; init; }
    public string Estado { get; init; } = "Sin horarios";
    public IReadOnlyList<IntervaloAgenda> Intervalos { get; init; } = [];
    public IReadOnlyList<FranjaAgenda> Franjas { get; init; } = [];
}
// El calendario no admite motivo de cancelación, enlace de reunión o contenido clínico.
public sealed class CitaCalendarioResumen
{
    public string? DireccionAtencion { get; init; }
    public Guid Revision { get; init; }
    public string? ProgramadorRol { get; init; }
    public Guid Id { get; init; }
    public string Nombre { get; init; } = "";
    public DateTime InicioUtc { get; init; }
    public DateTime FinUtc { get; init; }
    public EstadoCita Estado { get; init; }
    public ModalidadCita Modalidad { get; init; }
    public bool EsImprevista { get; init; }
    public bool FinAproximado { get; init; }
    public bool TieneRegistroAtencion { get; init; }
}
public sealed class CalendarioAgendaViewModel
{
    public Guid? CitaSeleccionada { get; set; }
    public AlmuerzoResumen? Almuerzo { get; init; }
    public IReadOnlyList<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Profesionales { get; set; } = [];
    public bool Director { get; set; }
    public bool Historico { get; set; }
    public bool PuedeReservar { get; set; }
    public string? PsicologoId { get; set; }
    public IReadOnlyList<EstudianteProgramable> EstudiantesProgramables { get; init; } = [];
    public DateTime AhoraUtc { get; init; }
    public DateOnly Mes { get; init; }
    public DateOnly Hoy { get; init; }
    public bool Editor { get; init; }
    public IReadOnlyList<DiaAgendaViewModel> Dias { get; init; } = [];
}
public sealed record AlmuerzoResumen(TimeOnly Inicio,TimeOnly Fin);
public sealed record CalendarioDirectorViewModel(CalendarioAgendaViewModel Calendario, IReadOnlyList<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Psicologos);
public sealed class HistorialCitasViewModel
{
    public string Busqueda { get; init; } = string.Empty;
    public EstadoCita? Estado { get; init; }
    public int Pagina { get; init; } = 1;
    public int Paginas { get; init; } = 1;
    public IReadOnlyList<CitaResumen> Citas { get; init; } = [];
}
