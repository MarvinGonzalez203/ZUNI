using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class CompletarPerfilEstudianteViewModel
{
    [Required(ErrorMessage = "Ingresa tu carné.")]
    [StringLength(30, ErrorMessage = "El carné no puede superar los 30 caracteres.")]
    [Display(Name = "Carné")]
    public string Carne { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu carrera.")]
    [StringLength(150, ErrorMessage = "La carrera no puede superar los 150 caracteres.")]
    [Display(Name = "Carrera")]
    public string Carrera { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "El semestre no puede superar los 50 caracteres.")]
    [Display(Name = "Semestre")]
    public string? Semestre { get; set; }

    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
}
