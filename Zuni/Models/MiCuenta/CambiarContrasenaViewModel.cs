using System.ComponentModel.DataAnnotations;

namespace Zuni.Models.MiCuenta;

public sealed class CambiarContrasenaViewModel
{
    [Required(ErrorMessage = "Ingresa tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contraseña nueva.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Usa entre 8 y 100 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña nueva.")]
    [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    public string ConfirmarNuevaContrasena { get; set; } = string.Empty;
}
