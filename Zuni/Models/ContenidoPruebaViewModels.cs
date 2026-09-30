using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class DimensionesPruebaViewModel
{
    public Guid PruebaId { get; init; }
    public string PruebaNombre { get; init; } = string.Empty;
    public IReadOnlyList<DimensionPrueba> Dimensiones { get; init; } = [];
}

public sealed class DimensionPruebaFormViewModel
{
    public Guid Id { get; set; }
    public Guid PruebaId { get; set; }

    [Required(ErrorMessage = "Escribe el nombre de la dimensión.")]
    [StringLength(100, ErrorMessage = "El nombre admite hasta 100 caracteres.")]
    [Display(Name = "Nombre de la dimensión")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción admite hasta 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(1, 999, ErrorMessage = "El orden debe estar entre 1 y 999.")]
    [Display(Name = "Orden")]
    public int Orden { get; set; } = 1;
}

public sealed class PreguntasPruebaViewModel
{
    public Guid PruebaId { get; init; }
    public string PruebaNombre { get; init; } = string.Empty;
    public Guid DimensionId { get; init; }
    public string DimensionNombre { get; init; } = string.Empty;
    public IReadOnlyList<PreguntaPrueba> Preguntas { get; init; } = [];
}

public sealed class PreguntaPruebaFormViewModel
{
    public Guid Id { get; set; }
    public Guid DimensionId { get; set; }

    [Required(ErrorMessage = "Escribe el enunciado de la pregunta.")]
    [StringLength(1000, ErrorMessage = "La pregunta admite hasta 1000 caracteres.")]
    [Display(Name = "Enunciado")]
    public string Texto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Explica qué aspecto de gustos, preferencias o intereses busca explorar.")]
    [StringLength(500, ErrorMessage = "El propósito admite hasta 500 caracteres.")]
    [Display(Name = "Propósito exploratorio")]
    public string PropositoExploratorio { get; set; } = string.Empty;

    [Range(1, 999, ErrorMessage = "El orden debe estar entre 1 y 999.")]
    [Display(Name = "Orden dentro de la dimensión")]
    public int Orden { get; set; } = 1;
}

public sealed class OpcionesPreguntaViewModel
{
    public Guid PruebaId { get; init; }
    public string PruebaNombre { get; init; } = string.Empty;
    public Guid DimensionId { get; init; }
    public string DimensionNombre { get; init; } = string.Empty;
    public Guid PreguntaId { get; init; }
    public string PreguntaTexto { get; init; } = string.Empty;
    public IReadOnlyList<OpcionPreguntaPrueba> Opciones { get; init; } = [];
    public int OpcionesActivas => Opciones.Count(opcion => opcion.Activa);
}

public sealed class OpcionPreguntaFormViewModel
{
    public Guid Id { get; set; }
    public Guid PreguntaId { get; set; }

    [Required(ErrorMessage = "Escribe el texto de la opción.")]
    [StringLength(200, ErrorMessage = "La opción admite hasta 200 caracteres.")]
    [Display(Name = "Texto de la opción")]
    public string Texto { get; set; } = string.Empty;

    [Range(1, 999, ErrorMessage = "El orden debe estar entre 1 y 999.")]
    [Display(Name = "Orden")]
    public int Orden { get; set; } = 1;
}
