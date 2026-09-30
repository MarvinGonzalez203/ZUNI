using System.ComponentModel.DataAnnotations;

namespace Zuni.Models;

public enum ModalidadPrueba
{
    [Display(Name = "Rápida (15 preguntas)")]
    Rapida,

    [Display(Name = "Media (40 preguntas)")]
    Media,

    [Display(Name = "Avanzada (60 o más preguntas)")]
    Avanzada
}

/// <summary>
/// Assigns an exploratory questionnaire to a student without storing answers
/// or clinical interpretations. Response delivery belongs to the student flow.
/// </summary>
public sealed class AsignacionPrueba
{
    public Guid Id { get; set; }

    public Guid PruebaId { get; set; }

    [Required]
    [MaxLength(450)]
    public string EstudianteId { get; set; } = string.Empty;

    [Required]
    [MaxLength(450)]
    public string AsignadaPorId { get; set; } = string.Empty;

    public ModalidadPrueba Modalidad { get; set; }

    public DateTime FechaAsignacionUtc { get; set; }

    public bool Cancelada { get; set; }
}
