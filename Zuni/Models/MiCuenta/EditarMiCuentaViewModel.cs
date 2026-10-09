using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.MiCuenta;

public sealed class EditarMiCuentaViewModel
{
    [Required(ErrorMessage = "Ingresa tu nombre completo.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;
    public DatosEstudianteViewModel? Estudiante { get; set; }
    // Revisión protegida por el servidor: no contiene identificadores editables.
    [Required] public string Revision { get; set; } = string.Empty;
    [BindNever] public bool PuedeEditarEstudiante { get; set; }
    [BindNever] public bool SolicitarCarne { get; set; }
    public string? CarneParte1 { get; set; }
    public string? CarneParte2 { get; set; }
    public string? CarneParte3 { get; set; }
}
