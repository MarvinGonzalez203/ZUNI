using Zuni.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Zuni.Models.Administrador;

public sealed class AgregarUsuarioViewModel : IValidatableObject
{
    public static IReadOnlyList<string> RolesPermitidos => CambiarRolViewModel.RolesPermitidos;

    [Required(ErrorMessage = "Ingresa el nombre completo.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo electrónico.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    public string Rol { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contraseña temporal.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Usa entre 8 y 100 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña temporal")]
    public string ContrasenaTemporal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña temporal.")]
    [Compare(nameof(ContrasenaTemporal), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña temporal")]
    public string ConfirmarContrasenaTemporal { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string? Motivo { get; set; }

    public string? CarneParte1 { get; set; }
    public string? CarneParte2 { get; set; }
    public string? CarneParte3 { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var correo = Correo?.Trim() ?? string.Empty;
        var separador = correo.LastIndexOf('@');
        if (separador <= 0 || correo.IndexOf('@') != separador ||
            !correo[(separador + 1)..].Equals("miumg.edu.gt", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("Debes utilizar tu correo institucional @miumg.edu.gt.", new[] { nameof(Correo) });

        if (!RolesPermitidos.Contains(Rol))
            yield return new ValidationResult("Selecciona un rol permitido.", new[] { nameof(Rol) });

        if ((NombreCompleto?.Trim().Length ?? 0) < 2)
            yield return new ValidationResult("El nombre debe tener entre 2 y 100 caracteres.", new[] { nameof(NombreCompleto) });

        if (Rol == "Estudiante" &&
            !CarneHelper.TryConstruir(CarneParte1, CarneParte2, CarneParte3, out _))
            yield return new ValidationResult(CarneHelper.MensajeFormato, new[] { nameof(CarneParte1) });
    }
}
