using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.MiCuenta;

public sealed class DatosEstudianteViewModel
{
    [BindNever] public string? Carne { get; set; }
    [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El teléfono debe contener 8 dígitos.")]
    public string? Telefono { get; set; }
    [StringLength(150)] public string? Carrera { get; set; }
    [RegularExpression(@"^(?:[1-9]|10)$", ErrorMessage = "Selecciona un semestre válido.")]
    public string? Semestre { get; set; }
    [RegularExpression(@"^[12]$", ErrorMessage = "Selecciona un ciclo académico válido.")]
    public string? CicloAcademico { get; set; }
    [StringLength(150)] public string? NombreContactoEmergencia { get; set; }
    [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El teléfono de emergencia debe contener 8 dígitos.")]
    public string? TelefonoContactoEmergencia { get; set; }
    [StringLength(60)] public string? RelacionContactoEmergencia { get; set; }
}
