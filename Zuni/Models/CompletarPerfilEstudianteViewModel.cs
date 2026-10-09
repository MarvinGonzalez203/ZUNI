using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class CompletarPerfilEstudianteViewModel
{
    [Required(ErrorMessage = "Completa el primer bloque del carné.")]
    [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    public string? CarneParte1 { get; set; }

    [Required(ErrorMessage = "Completa el segundo bloque del carné.")]
    [RegularExpression(@"^[0-9]{2}$", ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    public string? CarneParte2 { get; set; }

    [Required(ErrorMessage = "Completa el último bloque del carné.")]
    [RegularExpression(@"^[0-9]{4,5}$", ErrorMessage = "El carné debe tener 10 u 11 dígitos: 0000-00-0000 o 0000-00-00000.")]
    public string? CarneParte3 { get; set; }

    [Required(ErrorMessage = "Ingresa tu teléfono.")]
    [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El teléfono debe contener 8 dígitos.")]
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
        @"^[0-9]{8}$",
        ErrorMessage = "El teléfono de emergencia debe contener 8 dígitos.")]
    [Display(Name = "Teléfono de emergencia")]
    public string? TelefonoContactoEmergencia { get; set; }

    [StringLength(60, ErrorMessage = "La relación no puede superar los 60 caracteres.")]
    [Display(Name = "Relación con el estudiante")]
    public string? RelacionContactoEmergencia { get; set; }

    public bool SolicitarCarne { get; set; }
    public bool SolicitarTelefono { get; set; }
}
