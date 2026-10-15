using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.Institucion;
public sealed class PreferenciaVista
{
    [MaxLength(450)] public string UsuarioId { get; set; }="";
    [MaxLength(30)] public string Rol { get; set; }="";
    [Required,MaxLength(7)] public string Color { get; set; }="#cea2fd";
    [ConcurrencyCheck] public Guid Revision { get; set; }
}
public sealed class ColorPersonalInput
{
    public string Rol { get; set; }="";
    public Guid Revision { get; set; }
    [Required,RegularExpression("^#[a-fA-F0-9]{6}$"),Display(Name="Color de botones y navegación")]
    public string ColorPrincipal { get; set; }="#cea2fd";
}
