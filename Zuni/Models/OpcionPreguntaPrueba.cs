using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class OpcionPreguntaPrueba
{
    public Guid Id { get; set; }

    public Guid PreguntaPruebaId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Texto { get; set; } = string.Empty;

    [Range(1, 999)]
    public int Orden { get; set; }

    public bool Activa { get; set; } = true;

    public PreguntaPrueba Pregunta { get; set; } = null!;
}
