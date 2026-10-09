using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Ingresa tu nombre.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    public string CarneParte1 { get; set; } = string.Empty;

    [Required(ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    [RegularExpression(@"^\d{2}$", ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    public string CarneParte2 { get; set; } = string.Empty;

    [Required(ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    [RegularExpression(@"^[0-9]{4,5}$", ErrorMessage = "El último bloque debe contener 4 o 5 dígitos.")]
    public string CarneParte3 { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu teléfono.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe contener 8 dígitos.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "Crea una contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Usa al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contraseña nueva.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Usa al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
