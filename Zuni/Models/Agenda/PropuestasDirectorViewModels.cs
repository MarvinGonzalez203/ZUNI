using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zuni.Models.Agenda;

// Propuestas de cita creadas por Dirección. Solo modelos de presentación y entrada:
// reutilizan la entidad Cita (ProgramadorRol = "Director", Estado = Solicitada) sin cambios de esquema.

// Entrada del formulario. No admite estado, lugar, enlace, revisión ni Id de cita: el Director
// no puede confirmar, reprogramar ni fijar datos de atención aunque manipule el POST.
public sealed class PropuestaDirectorInput
{
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    [Required(ErrorMessage = "Selecciona un estudiante del directorio.")] public Guid? PerfilEstudianteId { get; set; }
    [Required(ErrorMessage = "Selecciona un psicólogo."), Display(Name = "Psicólogo")] public string? PsicologoId { get; set; }
    [Required(ErrorMessage = "Indica la fecha propuesta."), Display(Name = "Fecha")] public DateOnly? Fecha { get; set; }
    [Required(ErrorMessage = "Indica la hora propuesta."), Display(Name = "Hora")] public TimeOnly? HoraInicio { get; set; }
    [Range(15, 120), Display(Name = "Duración (minutos)")] public int DuracionMinutos { get; set; } = 50;
    [EnumDataType(typeof(ModalidadCita)), Display(Name = "Modalidad")] public ModalidadCita Modalidad { get; set; }
}

// Fila del directorio institucional: solo datos académicos mínimos. Sin indicadores de atención,
// solicitud, asignación, expediente, Big Five, correo ni teléfono.
public sealed record EstudianteDirectorio(Guid PerfilId, string Nombre, string Carne, string? Carrera);

public sealed class ProponerCitaViewModel
{
    public string Busqueda { get; init; } = "";
    public bool BusquedaCorta { get; init; }
    public bool HayMasResultados { get; init; }
    public IReadOnlyList<EstudianteDirectorio> Resultados { get; init; } = [];
    public EstudianteDirectorio? Seleccionado { get; init; }
    public IReadOnlyList<SelectListItem> Psicologos { get; init; } = [];
    [BindNever] public PropuestaDirectorInput Input { get; init; } = new();
    public DateOnly FechaMinima { get; init; }
}

// Fila de "Mis propuestas": exclusivamente propuestas creadas por el Director autenticado.
public sealed record PropuestaDirectorResumen(Guid Id, string Estudiante, string Carne, string Psicologo,
    DateTime InicioUtc, DateTime FinUtc, string EstadoAdministrativo, bool Novedad);

public sealed class PropuestasDirectorViewModel
{
    public IReadOnlyList<PropuestaDirectorResumen> Propuestas { get; init; } = [];
    public int Pagina { get; init; } = 1;
    public int Paginas { get; init; } = 1;
}

public sealed record AvisoPropuestaDirector(DateTime FechaUtc, string Tipo, string Texto);

// Detalle administrativo de una propuesta propia. No incluye lugar, enlace, motivo de cancelación,
// asistencia, registro de atención, expediente, solicitud ni resultados.
public sealed class PropuestaDirectorDetalle
{
    public Guid Id { get; init; }
    public string Estudiante { get; init; } = "";
    public string Carne { get; init; } = "";
    public string? Carrera { get; init; }
    public string Psicologo { get; init; } = "";
    public DateTime InicioUtc { get; init; }
    public DateTime FinUtc { get; init; }
    public ModalidadCita Modalidad { get; init; }
    public string EstadoAdministrativo { get; init; } = "";
    public bool FechaDefinitiva { get; init; }
    public string? MotivoAdministrativo { get; init; }
    public IReadOnlyList<AvisoPropuestaDirector> Historial { get; init; } = [];
}

// Motivos que el psicólogo puede dar al rechazar una propuesta de Dirección. Lista cerrada para que
// Dirección nunca reciba texto libre que pueda contener información clínica.
public static class MotivosPropuestaDirector
{
    public const string HorarioNoDisponible = "Horario no disponible";
    public const string OtraFecha = "Se requiere coordinar otra fecha";
    public const string Requisitos = "El estudiante debe completar requisitos administrativos en ZUNI";
    public const string Duplicada = "Propuesta duplicada";
    public const string FueraDeAlcance = "No corresponde al servicio de orientación psicológica";
    public static readonly IReadOnlyList<string> Todos = [HorarioNoDisponible, OtraFecha, Requisitos, Duplicada, FueraDeAlcance];
    public static bool Valido(string? motivo) => motivo is not null && Todos.Contains(motivo.Trim());
}
