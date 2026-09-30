using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zuni.Models;

public sealed class AsignacionesPruebaViewModel
{
    public IReadOnlyList<AsignacionPruebaListadoItem> Asignaciones { get; init; } = [];
}

public sealed class AsignacionPruebaListadoItem
{
    public Guid Id { get; init; }
    public string PruebaNombre { get; init; } = string.Empty;
    public string EstudianteNombre { get; init; } = string.Empty;
    public string EstudianteCorreo { get; init; } = string.Empty;
    public ModalidadPrueba Modalidad { get; init; }
    public DateTime FechaAsignacionUtc { get; init; }
    public bool Cancelada { get; init; }
}

public sealed class AsignarPruebaViewModel
{
    [Required(ErrorMessage = "Selecciona un cuestionario habilitado.")]
    [Display(Name = "Cuestionario")]
    public Guid PruebaId { get; set; }

    [Required(ErrorMessage = "Selecciona un estudiante.")]
    [Display(Name = "Estudiante")]
    public string EstudianteId { get; set; } = string.Empty;

    [EnumDataType(typeof(ModalidadPrueba), ErrorMessage = "Selecciona una modalidad válida.")]
    [Display(Name = "Modalidad")]
    public ModalidadPrueba Modalidad { get; set; }

    public IReadOnlyList<SelectListItem> PruebasDisponibles { get; set; } = [];
    public IReadOnlyList<SelectListItem> Estudiantes { get; set; } = [];
}
