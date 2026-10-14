using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.Agenda;

public sealed class ProgramarCitaInput
{
    public Guid? CitaId { get; set; }
    public Guid Revision { get; set; }
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    [Required, Display(Name="Estudiante vinculado")] public Guid? PerfilEstudianteId { get; set; }
    [Required] public DateOnly? Fecha { get; set; }
    [Required, Display(Name="Hora de inicio")] public TimeOnly? HoraInicio { get; set; }
    [Display(Name="Fin aproximado (opcional)")] public TimeOnly? HoraFin { get; set; }
    [Range(15,120), Display(Name="Duración prevista")] public int DuracionMinutos { get; set; } = 50;
    [EnumDataType(typeof(ModalidadCita))] public ModalidadCita Modalidad { get; set; }
    [BindNever] public IReadOnlyList<EstudianteProgramable> Estudiantes { get; set; } = [];
}
public sealed record EstudianteProgramable(Guid Id,string Nombre,string Psicologo,bool EdadVerificada=false);
public sealed record CitaProgramadaResumen(Guid Id,Guid Revision,string Estudiante,string Psicologo,DateTime InicioUtc,DateTime FinUtc,EstadoCita Estado,bool PuedeReprogramar);
public sealed record ProgramacionListado(IReadOnlyList<CitaProgramadaResumen> Citas)
{
    public CalendarioDirectorViewModel? CalendarioDirector { get; init; }
}
public sealed class NotificacionCita
{
    public string? AutorUsuarioId { get; set; }
    public string? AutorRol { get; set; }
    public string? Tipo { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioId { get; set; } = "";
    public Guid CitaId { get; set; }
    public string Texto { get; set; } = "";
    public DateTime FechaUtc { get; set; }
    public bool Leida { get; set; }
}
public sealed record NotificacionResumen(Guid Id,string Texto,DateTime FechaUtc,bool Leida,Guid CitaId,DateTime InicioUtc)
{
    public string Estudiante { get; init; }="";
    public string Carne { get; init; }="";
    public string Psicologo { get; init; }="";
    public string PsicologoId { get; init; }="";
    public string? Autor { get; init; }
    public string? AutorRol { get; init; }
    public bool AutorOriginal { get; init; }
    public string Tipo { get; init; }="Aviso";
    public EstadoCita EstadoActual { get; init; }
    public ModalidadCita Modalidad { get; init; }
}
