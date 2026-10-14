using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zuni.Models.Administrador;

public sealed class ContrasenaTemporalViewModel
{
    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string? Motivo { get; set; }

    [BindNever]
    public string NombreUsuario { get; set; } = string.Empty;

    [BindNever]
    public string? Email { get; set; }

    [BindNever]
    public bool Activo { get; set; }
}

// Solo vive durante la respuesta inmediata del POST. Nunca se persiste.
public sealed class ContrasenaTemporalGeneradaViewModel
{
    public string UsuarioId { get; init; } = string.Empty;
    public string NombreUsuario { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool Activo { get; init; }
    public string ContrasenaTemporal { get; init; } = string.Empty;
}
