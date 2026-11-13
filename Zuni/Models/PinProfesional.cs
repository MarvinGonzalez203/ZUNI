using System.ComponentModel.DataAnnotations;
namespace Zuni.Models;

public sealed class PinProfesional
{
    [Key,MaxLength(450)] public string UsuarioId { get; set; } = "";
    [MaxLength(2000)] public string? HashProtegido { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHastaUtc { get; set; }
    [MaxLength(64)] public string? RecuperacionHash { get; set; }
    public DateTime? RecuperacionExpiraUtc { get; set; }
    [MaxLength(64)] public string? RecuperacionSello { get; set; }
    public DateTime? UltimoEnvioUtc { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
}
public sealed class ConfigurarPinInput
{
    [Required,DataType(DataType.Password),Display(Name="Contraseña actual")] public string Contrasena { get; set; } = "";
    [Required,RegularExpression("^[0-9]{4}$"),DataType(DataType.Password),Display(Name="PIN de 4 dígitos")] public string Pin { get; set; } = "";
    [Compare(nameof(Pin)),DataType(DataType.Password),Display(Name="Confirmar PIN")] public string Confirmacion { get; set; } = "";
}
public sealed class RecuperarPinInput
{
    [Required,StringLength(450)] public string Usuario { get; set; } = "";
    [Required,StringLength(100)] public string Token { get; set; } = "";
    [Required,RegularExpression("^[0-9]{4}$"),DataType(DataType.Password),Display(Name="Nuevo PIN de 4 dígitos")] public string Pin { get; set; } = "";
    [Compare(nameof(Pin)),DataType(DataType.Password),Display(Name="Confirmar PIN")] public string Confirmacion { get; set; } = "";
}
