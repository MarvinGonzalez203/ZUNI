using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zuni.Models.Privacidad;

public sealed class PoliticaPrivacidad
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Version { get; set; }
    public string Institucion { get; set; } = "";
    public string Responsable { get; set; } = "";
    public string Contacto { get; set; } = "";
    public string Finalidades { get; set; } = "";
    public string DatosYAccesos { get; set; } = "";
    public string Conservacion { get; set; } = "";
    public string ProcedimientoDerechos { get; set; } = "";
    public string PublicadaPorId { get; set; } = "";
    public DateTime FechaPublicacionUtc { get; set; }
}
public sealed class AceptacionPrivacidad
{
    public Guid PoliticaId { get; set; }
    public string UsuarioId { get; set; } = "";
    public DateTime FechaUtc { get; set; }
}
public enum TipoDerechoDatos { Acceso, Correccion, Supresion, RetiroConsentimiento }
public enum EstadoDerechoDatos { Pendiente, Respondida }
public sealed class SolicitudDerechoDatos
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioId { get; set; } = "";
    public TipoDerechoDatos Tipo { get; set; }
    public string Detalle { get; set; } = "";
    public DateTime FechaSolicitudUtc { get; set; }
    public EstadoDerechoDatos Estado { get; set; }
    public string? Respuesta { get; set; }
    public string? ResponsableId { get; set; }
    public DateTime? FechaRespuestaUtc { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
}
public sealed class PublicarPoliticaInput
{
    public Guid? VersionAnteriorId { get; set; }
    [Required, StringLength(150), Display(Name = "Responsable institucional del tratamiento de datos")] public string Responsable { get; set; } = "";
    [Required, EmailAddress, StringLength(254), Display(Name = "Correo para ejercer derechos")] public string Contacto { get; set; } = "";
    [Required, StringLength(4000), Display(Name = "Finalidades del tratamiento")] public string Finalidades { get; set; } = "";
    [Required, StringLength(4000), Display(Name = "Datos necesarios y personas autorizadas")] public string DatosYAccesos { get; set; } = "";
    [Required, StringLength(4000), Display(Name = "Plazos y reglas de conservación y eliminación")] public string Conservacion { get; set; } = "";
    [Required, StringLength(4000), Display(Name = "Procedimiento de acceso, corrección, supresión e incidentes")] public string ProcedimientoDerechos { get; set; } = "";
    public bool Confirmada { get; set; }
}
public sealed class SolicitarDerechoInput
{
    [EnumDataType(typeof(TipoDerechoDatos)), Display(Name = "Tipo de solicitud")] public TipoDerechoDatos Tipo { get; set; }
    [Required, StringLength(1000), Display(Name = "Qué necesitas revisar")] public string Detalle { get; set; } = "";
}
public sealed class ResponderDerechoInput
{
    public Guid Id { get; set; }
    public Guid Revision { get; set; }
    [Required, StringLength(2000), Display(Name = "Respuesta y acciones realizadas o procedimiento acordado")] public string Respuesta { get; set; } = "";
    [BindNever] public SolicitudDerechoDatos? Solicitud { get; set; }
    [BindNever] public string Solicitante { get; set; } = "";
    [BindNever] public string Correo { get; set; } = "";
}
public sealed class PrivacidadViewModel
{
    public PoliticaPrivacidad? Politica { get; set; }
    public bool Aceptada { get; set; }
    public bool EsEstudiante { get; set; }
    public IReadOnlyList<SolicitudDerechoDatos> Solicitudes { get; set; } = [];
    public SolicitarDerechoInput Nueva { get; set; } = new();
}
