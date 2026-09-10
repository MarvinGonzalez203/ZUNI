using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class CompletarPerfilEstudianteViewModel
{
    [RegularExpression(@"^\d{4}$", ErrorMessage = "El carné debe contener 10 dígitos en el formato 0000-00-0000.")]
    public string? CarneParte1 { get; set; }

    [RegularExpression(@"^\d{2}$", ErrorMessage = "El carné debe contener 10 dígitos en el formato 0000-00-0000.")]
    public string? CarneParte2 { get; set; }

    [RegularExpression(@"^\d{4}$", ErrorMessage = "El carné debe contener 10 dígitos en el formato 0000-00-0000.")]
    public string? CarneParte3 { get; set; }

    [RegularExpression(@"^\d{8}$", ErrorMessage = "El teléfono debe contener 8 dígitos.")]
    [Display(Name = "Teléfono personal")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Ingresa tu carrera.")]
    [StringLength(150, ErrorMessage = "La carrera no puede superar los 150 caracteres.")]
    [Display(Name = "Carrera")]
    public string Carrera { get; set; } = string.Empty;

    [RegularExpression(@"^(?:[1-9]|10)$", ErrorMessage = "Selecciona un semestre válido.")]
    [Display(Name = "Semestre")]
    public string? Semestre { get; set; }

    [RegularExpression(@"^[12]$", ErrorMessage = "Selecciona un ciclo académico válido.")]
    [Display(Name = "Ciclo académico")]
    public string? CicloAcademico { get; set; }

    [StringLength(150, ErrorMessage = "El nombre del contacto no puede superar los 150 caracteres.")]
    [Display(Name = "Nombre del contacto de emergencia")]
    public string? NombreContactoEmergencia { get; set; }

    [RegularExpression(
        @"^\d{8}$",
        ErrorMessage = "El teléfono de emergencia debe contener 8 dígitos.")]
    [Display(Name = "Teléfono de emergencia")]
    public string? TelefonoContactoEmergencia { get; set; }

    [StringLength(60, ErrorMessage = "La relación no puede superar los 60 caracteres.")]
    [Display(Name = "Relación con el estudiante")]
    public string? RelacionContactoEmergencia { get; set; }

    public bool SolicitarCarne { get; set; }
    public bool SolicitarTelefono { get; set; }
}
