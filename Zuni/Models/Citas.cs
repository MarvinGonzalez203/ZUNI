using System.ComponentModel.DataAnnotations;
namespace Zuni.Models;

public enum EstadoCita { Solicitada=0, Confirmada=1, Terminada=2, Cancelada=3, Rechazada=4, NoAsistio=5, Reprogramada=6 }
public sealed class Cita
{
    public Guid Id { get; set; }=Guid.NewGuid();
    public string EstudianteId { get; set; }="";
    public ApplicationUser Estudiante { get; set; }=null!;
    public string PsicologoId { get; set; }="";
    public ApplicationUser Psicologo { get; set; }=null!;
    public Guid HorarioOriginalId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public bool DuracionVariable { get; set; }
    public bool PruebaLocal { get; set; }
    public string Modalidad { get; set; }="Presencial";
    public EstadoCita Estado { get; set; }
    public int Revision { get; set; }=1;
    public DateTime SolicitudUtc { get; set; }=DateTime.UtcNow;
    public Guid? CitaAnteriorId { get; set; }
    public Cita? CitaAnterior { get; set; }
    public bool? Asistio { get; set; }
    public DateTime? AtencionUtc { get; set; }
    public string ResultadoClinicoPrivado { get; set; }="";
    public string ResenaEstudiante { get; set; }="";
    public string ResultadoPublicable { get; set; }="";
    public bool RecomiendaProximaCita { get; set; }
    public string IndicacionesProximaCita { get; set; }="";
}
public sealed class EventoCita
{
    public Guid Id { get; set; }=Guid.NewGuid();
    public Guid CitaId { get; set; }
    public Cita Cita { get; set; }=null!;
    public string ActorId { get; set; }="";
    public ApplicationUser Actor { get; set; }=null!;
    public string Accion { get; set; }="";
    public string Motivo { get; set; }="";
    public DateTime FechaUtc { get; set; }=DateTime.UtcNow;
}
public sealed class AccesoAtencion
{
    public Guid Id { get; set; }=Guid.NewGuid();
    public Guid CitaId { get; set; }
    public Cita Cita { get; set; }=null!;
    public string PsicologoId { get; set; }="";
    public ApplicationUser Psicologo { get; set; }=null!;
    public DateTime FechaUtc { get; set; }=DateTime.UtcNow;
}
public sealed class SolicitudCitaInput
{
    public Guid HorarioId { get; set; }
    public TimeOnly Inicio { get; set; }
    [Required] public string Modalidad { get; set; }="Presencial";
    public Guid? ReprogramarId { get; set; }
    public int? RevisionAnterior { get; set; }
}
public sealed class CerrarAtencionInput
{
    public Guid Id { get; set; }
    public int Revision { get; set; }
    [Required] public bool? Asistio { get; set; }
    [StringLength(4000)] public string? ResultadoClinicoPrivado { get; set; }
    [StringLength(1500)] public string? ResenaEstudiante { get; set; }
    [StringLength(1500)] public string? ResultadoPublicable { get; set; }
    public bool RecomiendaProximaCita { get; set; }
    [StringLength(1000)] public string? IndicacionesProximaCita { get; set; }
}
public sealed record FranjaCita(Guid HorarioId,DateOnly Fecha,TimeOnly Inicio,TimeOnly Fin,bool Variable,string Modalidad,bool Reservada);
public sealed record CitaResumen(Guid Id,Guid PerfilEstudianteId,string Estudiante,string Psicologo,DateOnly Fecha,TimeOnly Inicio,TimeOnly Fin,
    EstadoCita Estado,int Revision,string Modalidad,bool Variable,DateTime InicioUtc,string Resena,string ResultadoPublicable,bool ProximaCita,string Indicaciones);
public sealed record AtencionDetalle(CitaResumen Cita,string ResultadoClinicoPrivado,IReadOnlyList<EventoCitaResumen> Eventos);
public sealed record EventoCitaResumen(DateTime FechaUtc,string Accion,string Motivo);
public sealed record HistorialCitasViewModel(IReadOnlyList<CitaResumen> Citas,string Busqueda,DateOnly? Desde,DateOnly? Hasta,string Modalidad,int Pagina,int Paginas);
public static class CitaTextos
{
    public static bool Activa(EstadoCita estado)=>estado is EstadoCita.Solicitada or EstadoCita.Confirmada;
    public static string Estado(EstadoCita estado)=>estado switch{EstadoCita.Solicitada=>"Solicitada · pendiente de aceptación",EstadoCita.Confirmada=>"Confirmada",EstadoCita.Terminada=>"Terminada",EstadoCita.Cancelada=>"Cancelada",EstadoCita.Rechazada=>"Rechazada",EstadoCita.NoAsistio=>"No asistió",EstadoCita.Reprogramada=>"Reprogramada",_=>"No disponible"};
}
