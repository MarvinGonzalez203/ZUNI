using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public sealed class DimensionPrueba
{
    public Guid Id { get; set; }

    public Guid PruebaId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Range(1, 999)]
    public int Orden { get; set; }

    public bool Activa { get; set; } = true;

    public Prueba Prueba { get; set; } = null!;

    public ICollection<PreguntaPrueba> Preguntas { get; set; } = new List<PreguntaPrueba>();
}
