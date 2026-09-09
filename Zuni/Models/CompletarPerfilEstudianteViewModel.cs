using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class CompletarPerfilEstudianteViewModel
{
    [StringLength(30, ErrorMessage = "El carné no puede superar los 30 caracteres.")]
    [Display(Name = "Carné")]
    public string? Carne { get; set; }

    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [Display(Name = "Teléfono personal")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Ingresa tu carrera.")]
    [StringLength(150, ErrorMessage = "La carrera no puede superar los 150 caracteres.")]
    [Display(Name = "Carrera")]
    public string Carrera { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "El semestre no puede superar los 50 caracteres.")]
    [Display(Name = "Semestre")]
    public string? Semestre { get; set; }

    [StringLength(30, ErrorMessage = "El ciclo académico no puede superar los 30 caracteres.")]
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
