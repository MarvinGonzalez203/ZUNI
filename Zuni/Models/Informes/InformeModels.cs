using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zuni.Models.Informes;

public enum EstadoInforme { Borrador, PendienteAutorizacion, Autorizada, Emitida, Rechazada, Revocada, Sustituida }
public sealed class InformeEducativo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SolicitudAtencionId { get; set; }
    public string AutorUsuarioId { get; set; } = "";
    public DateTime FechaCreacionUtc { get; set; }
}
public sealed class VersionInformeEducativo
{
    public bool? IncluyeAnalisisRendimiento { get; set; }
    [MaxLength(2000)] public string RelatoRendimiento { get; set; } = "";
    [MaxLength(2000)] public string ValoracionRendimiento { get; set; } = "";
    public bool ContrasteRendimientoConfirmado { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InformeId { get; set; }
    public int Numero { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public EstadoInforme Estado { get; set; }
    public string Objetivo { get; set; } = "";
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFin { get; set; }
    public string Fuentes { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public string Necesidades { get; set; } = "";
    public string Recomendaciones { get; set; } = "";
    public string ResponsablesAcciones { get; set; } = "";
    public DateOnly FechaSeguimiento { get; set; }
    public string? DestinatarioUsuarioId { get; set; }
    public string Institucion { get; set; } = "";
    public string Estudiante { get; set; } = "";
    public string Carne { get; set; } = "";
    public string Psicologo { get; set; } = "";
    public string Destinatario { get; set; } = "";
    public DateTime? FechaEmisionUtc { get; set; }
    public int? SustituyeNumero { get; set; }
}
public sealed class AutorizacionInforme
{
    public Guid VersionInformeId { get; set; }
    public string EstudianteUsuarioId { get; set; } = "";
    public Guid PoliticaPrivacidadId { get; set; }
    public string VersionConsentimiento { get; set; } = "informe-educativo-v1";
    public string TextoConsentimiento { get; set; } = "";
    public bool? Aceptada { get; set; }
    public DateTime FechaSolicitudUtc { get; set; }
    public DateTime? FechaDecisionUtc { get; set; }
    public DateTime? FechaRevocacionUtc { get; set; }
}
public sealed class EditarInformeInput
{
    [Display(Name = "¿Corresponde analizar el rendimiento académico?")] public bool? IncluyeAnalisisRendimiento { get; set; }
    [StringLength(2000), Display(Name = "Lo que refiere el estudiante sobre su rendimiento")] public string? RelatoRendimiento { get; set; }
    [StringLength(2000), Display(Name = "Valoración educativa del psicólogo y contraste de fuentes")] public string? ValoracionRendimiento { get; set; }
    [Display(Name = "Contrasté el relato del estudiante con la información disponible y revisé esta valoración educativa")] public bool ContrasteRendimientoConfirmado { get; set; }
    public Guid? Id { get; set; }
    public Guid SolicitudAtencionId { get; set; }
    public Guid Revision { get; set; }
    [Required, StringLength(1000), Display(Name = "Objetivo y finalidad del informe")] public string Objetivo { get; set; } = "";
    [Required, DataType(DataType.Date), Display(Name = "Inicio del periodo observado")] public DateOnly? PeriodoInicio { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Fin del periodo observado")] public DateOnly? PeriodoFin { get; set; }
    [Required, StringLength(2000), Display(Name = "Fuentes de información utilizadas y autorizadas")] public string Fuentes { get; set; } = "";
    [Required, StringLength(3000), Display(Name = "Barreras observadas y posible relación con el desempeño")] public string Observaciones { get; set; } = "";
    [Required, StringLength(2000), Display(Name = "Necesidades de apoyo educativo")] public string Necesidades { get; set; } = "";
    [Required, StringLength(3000), Display(Name = "Recomendaciones educativas")] public string Recomendaciones { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Responsables de las acciones propuestas")] public string ResponsablesAcciones { get; set; } = "";
    [Required, DataType(DataType.Date), Display(Name = "Fecha propuesta de seguimiento")] public DateOnly? FechaSeguimiento { get; set; }
    [Required, Display(Name = "Director destinatario autorizado")] public string DestinatarioUsuarioId { get; set; } = "";
    [BindNever] public IReadOnlyList<SelectListItem> Destinatarios { get; set; } = [];
}
public sealed record InformeResumen(Guid Id, Guid InformeId, int Numero, string Estudiante, string Objetivo, EstadoInforme Estado);
public sealed record DetalleInforme(VersionInformeEducativo Version, AutorizacionInforme? Autorizacion, bool EsAutor, bool EsEstudiante)
{
    public bool DestinatarioVigente { get; init; }
}
