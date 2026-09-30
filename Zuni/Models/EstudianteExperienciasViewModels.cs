using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zuni.Models;

public enum ModalidadCitaPreview
{
    [Display(Name = "Presencial (vista previa)")]
    Presencial,

    [Display(Name = "Virtual (vista previa)")]
    Virtual
}

public sealed class SolicitudCitaPreviewViewModel
{
    [Required(ErrorMessage = "Selecciona una fecha preferida.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha preferida")]
    public DateTime? FechaPreferida { get; set; }

    [Required(ErrorMessage = "Selecciona un horario de ejemplo.")]
    [Display(Name = "Horario de preferencia")]
    public string FranjaHoraria { get; set; } = string.Empty;

    [Display(Name = "Modalidad")]
    public ModalidadCitaPreview Modalidad { get; set; }

    public IReadOnlyList<SelectListItem> FranjasHorarias { get; set; } = [];
}

public sealed class SeguimientoEstudiantePreviewViewModel
{
    public IReadOnlyList<SeguimientoCuestionarioPreviewItem> Cuestionarios { get; init; } = [];
}

public sealed class SeguimientoCuestionarioPreviewItem
{
    public string Nombre { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public int PreguntasTotales { get; init; }
    public int PreguntasRespondidas { get; init; }
    public ModalidadPrueba Modalidad { get; init; }
}
