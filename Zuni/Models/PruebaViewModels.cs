using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class PruebasViewModel
{
    public IReadOnlyList<Prueba> Pruebas { get; init; } = [];

    public string? Buscar { get; init; }
}

public sealed class PruebaFormViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Escribe el nombre de la prueba.")]
    [StringLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")]
    [Display(Name = "Nombre de la prueba")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción admite hasta 1000 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }
}
