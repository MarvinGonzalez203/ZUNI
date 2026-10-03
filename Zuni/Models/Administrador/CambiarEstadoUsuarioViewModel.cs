using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zuni.Models.Administrador;

public sealed class CambiarEstadoUsuarioViewModel
{
    [Required]
    public string UsuarioId { get; set; } = string.Empty;

    [BindNever]
    public string NombreUsuario { get; set; } = string.Empty;

    [BindNever]
    public bool EstadoActual { get; set; }

    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string? Motivo { get; set; }
}
