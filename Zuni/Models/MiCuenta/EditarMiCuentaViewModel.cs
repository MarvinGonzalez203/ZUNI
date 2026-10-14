using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.MiCuenta;

public sealed class EditarMiCuentaViewModel
{
    [BindNever] public IReadOnlyList<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Carreras { get; set; } = [];
    [Required(ErrorMessage = "Ingresa tu nombre completo.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;
    [EmailAddress, StringLength(254)] public string? Correo { get; set; }
    [RegularExpression("^[0-9]{8}$", ErrorMessage = "Ingresa un teléfono de 8 dígitos.")] public string? Telefono { get; set; }
    [BindNever] public bool PuedeEditarContacto { get; set; }
    public DatosEstudianteViewModel? Estudiante { get; set; }
    // Revisión protegida por el servidor: no contiene identificadores editables.
    [Required] public string Revision { get; set; } = string.Empty;
    [BindNever] public bool PuedeEditarEstudiante { get; set; }
    [BindNever] public bool SolicitarCarne { get; set; }
    public string? CarneParte1 { get; set; }
    public string? CarneParte2 { get; set; }
    public string? CarneParte3 { get; set; }
}
