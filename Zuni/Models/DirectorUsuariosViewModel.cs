using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class DirectorUsuariosViewModel
{
    public IReadOnlyList<DirectorUsuarioRolItem> Usuarios { get; init; } =
        Array.Empty<DirectorUsuarioRolItem>();
}

public sealed class DirectorUsuarioRolItem
{
    public required string Id { get; init; }

    public required string Nombre { get; init; }

    public required string Correo { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }

    public required bool PuedeEditar { get; init; }
}

public sealed class DirectorCambiarRolViewModel
{
    [Required]
    [StringLength(450)]
    public string UsuarioId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un tipo de usuario.")]
    [RegularExpression(
        "^(Psicologo|Catedratico|Estudiante)$",
        ErrorMessage = "El tipo de usuario seleccionado no es válido.")]
    public string Rol { get; set; } = string.Empty;
}
