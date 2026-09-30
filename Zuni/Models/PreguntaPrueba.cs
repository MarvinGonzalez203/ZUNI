using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class PreguntaPrueba
{
    public Guid Id { get; set; }

    public Guid DimensionPruebaId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Texto { get; set; } = string.Empty;

    // Justificación para recoger esta respuesta; no funciona como regla clínica.
    [MaxLength(500)]
    public string? PropositoExploratorio { get; set; }

    [Range(1, 999)]
    public int Orden { get; set; }

    // Se crea como borrador hasta que tenga por lo menos dos opciones activas.
    public bool Activa { get; set; }

    public DimensionPrueba Dimension { get; set; } = null!;

    public ICollection<OpcionPreguntaPrueba> Opciones { get; set; } = new List<OpcionPreguntaPrueba>();
}
