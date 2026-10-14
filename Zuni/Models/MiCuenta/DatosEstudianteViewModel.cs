using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.MiCuenta;

public sealed class DatosEstudianteViewModel
{
    [BindNever] public string? Carne { get; set; }
    [Required(ErrorMessage = "Ingresa tu teléfono personal.")]
    [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El teléfono debe contener 8 dígitos.")]
    public string? Telefono { get; set; }
    [Required(ErrorMessage = "Ingresa tu carrera."), StringLength(150)] public string? Carrera { get; set; }
    [RegularExpression(@"^(?:[1-9]|10)$", ErrorMessage = "Selecciona un semestre válido.")]
    public string? Semestre { get; set; }
    [RegularExpression(@"^[12]$", ErrorMessage = "Selecciona un ciclo académico válido.")]
    public string? CicloAcademico { get; set; }
    [Required(ErrorMessage = "Ingresa el nombre del contacto de emergencia."), StringLength(150)] public string? NombreContactoEmergencia { get; set; }
    [Required(ErrorMessage = "Ingresa el teléfono de emergencia.")]
    [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El teléfono de emergencia debe contener 8 dígitos.")]
    public string? TelefonoContactoEmergencia { get; set; }
    [Required(ErrorMessage = "Indica tu relación con el contacto de emergencia."), StringLength(60)] public string? RelacionContactoEmergencia { get; set; }
}
