using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zuni.Models.Administrador;

public sealed class CambiarRolViewModel
{
    public static IReadOnlyList<string> RolesPermitidos { get; } =
        Array.AsReadOnly(new[] { "Administrador", "Director", "Psicologo", "Catedratico", "Estudiante" });

    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [Display(Name = "Nuevo rol")]
    public string NuevoRol { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string? Motivo { get; set; }

    [BindNever]
    public string NombreUsuario { get; set; } = string.Empty;

    [BindNever]
    public string RolesActuales { get; set; } = string.Empty;
}
