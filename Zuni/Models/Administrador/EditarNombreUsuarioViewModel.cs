using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.Administrador;
public sealed class EditarNombreUsuarioViewModel
{
    [Required] public string UsuarioId { get; set; } = "";
    [Required] public string Revision { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 2)] public string NombreCompleto { get; set; } = "";
}
