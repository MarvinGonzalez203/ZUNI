using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zuni.Models.MayoriaEdad;

public enum EstadoMayoriaEdad { Pendiente, Verificada, Rechazada }
public enum DocumentoEdad { Dpi, Pasaporte, DocumentoExtranjero }

public sealed class VerificacionMayoriaEdad
{
    public Guid PerfilEstudianteId { get; set; }
    public int EdadDeclarada { get; set; }
    public DateTime FechaDeclaracionUtc { get; set; }
    public EstadoMayoriaEdad Estado { get; set; }
    public DocumentoEdad? DocumentoRevisado { get; set; }
    public int? EdadVerificada { get; set; }
    public string? ResponsableUsuarioId { get; set; }
    public DateTime? FechaRevisionUtc { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}

public sealed class DeclararEdadViewModel
{
    [Required(ErrorMessage = "Indica tu fecha de nacimiento."), DataType(DataType.Date), Display(Name = "Fecha de nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }
    public Guid Revision { get; set; }
    [BindNever] public VerificacionMayoriaEdad? Constancia { get; set; }
}

public sealed class RevisarEdadViewModel
{
    public Guid PerfilEstudianteId { get; set; }
    public Guid Revision { get; set; }
    [Required(ErrorMessage = "Selecciona el resultado de la revisión."), Display(Name = "Resultado de la revisión")]
    public bool? Aprobada { get; set; }
    [EnumDataType(typeof(DocumentoEdad)), Display(Name = "Documento revisado")]
    public DocumentoEdad? Documento { get; set; }
    [DataType(DataType.Date), Display(Name = "Fecha de nacimiento indicada en el documento")]
    public DateOnly? FechaNacimiento { get; set; }
    [Display(Name = "Revisé el documento y comprobé que corresponde al estudiante")]
    public bool DocumentoComprobado { get; set; }
    // "ficha" cuando la revisión se abre desde Administrador → Gestionar. Solo decide el regreso.
    public string? Origen { get; set; }
    [BindNever] public string UsuarioId { get; set; } = "";
    [BindNever] public string Estudiante { get; set; } = "";
    [BindNever] public string Correo { get; set; } = "";
    [BindNever] public string Carne { get; set; } = "";
    [BindNever] public VerificacionMayoriaEdad? Constancia { get; set; }
}

public sealed record RevisionEdadResumen(Guid PerfilEstudianteId, string Estudiante, string Correo,
    EstadoMayoriaEdad Estado, DateTime FechaDeclaracionUtc);
