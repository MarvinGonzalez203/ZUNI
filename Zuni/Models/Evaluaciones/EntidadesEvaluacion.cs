using Zuni.Models;
namespace Zuni.Models.Evaluaciones;
public enum EstadoEvaluacion { Asignada, Pendiente, EnProceso, Finalizada }
public sealed class Evaluacion
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Instrucciones { get; set; } = "";
    public bool EsDemostracion { get; set; }
    public ICollection<PreguntaEvaluacion> Preguntas { get; set; } = new List<PreguntaEvaluacion>();
}
public sealed class PreguntaEvaluacion
{
    public Guid Id { get; set; }
    public Guid EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;
    public int Orden { get; set; }
    public string Texto { get; set; } = "";
    public bool Obligatoria { get; set; } = true;
}
public sealed class AsignacionEvaluacion
{
    public Guid Id { get; set; }
    public Guid EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;
    public string EstudianteId { get; set; } = "";
    public ApplicationUser Estudiante { get; set; } = null!;
    public EstadoEvaluacion Estado { get; set; }
    public DateTime FechaAsignacionUtc { get; set; }
    public DateTime? FechaInicioUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public Guid? Comprobante { get; set; }
    public int Revision { get; set; }
    public ICollection<RespuestaEvaluacion> Respuestas { get; set; } = new List<RespuestaEvaluacion>();
    public ResultadoEvaluacion? Resultado { get; set; }
}
public sealed class RespuestaEvaluacion
{
    public Guid AsignacionId { get; set; }
    public Guid PreguntaId { get; set; }
    public Guid EvaluacionId { get; set; }
    public int Valor { get; set; }
    public DateTime ActualizadaUtc { get; set; }
    public AsignacionEvaluacion Asignacion { get; set; } = null!;
    public PreguntaEvaluacion Pregunta { get; set; } = null!;
}
public sealed class ResultadoEvaluacion
{
    public Guid AsignacionId { get; set; }
    public AsignacionEvaluacion Asignacion { get; set; } = null!;
    public decimal? Puntuacion { get; set; }
    public string ObservacionesPublicables { get; set; } = "";
    public bool Publicado { get; set; }
    public DateTime? PublicadoUtc { get; set; }
    public string? PublicadoPorId { get; set; }
    public ApplicationUser? PublicadoPor { get; set; }
}
