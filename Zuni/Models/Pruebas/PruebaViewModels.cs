using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zuni.Models.Pruebas;

public sealed class EditarPruebaViewModel
{
    public Guid? Id { get; set; }
    public Guid Revision { get; set; }
    [Required, StringLength(150), Display(Name = "Nombre")] public string Nombre { get; set; } = "";
    [Required, StringLength(2000), Display(Name = "Descripción y propósito")] public string Descripcion { get; set; } = "";
    [Required, StringLength(2000)] public string Instrucciones { get; set; } = "";
    [Required, StringLength(500), Display(Name = "Fuente / referencia del instrumento")] public string Fuente { get; set; } = "";
    [Required, StringLength(80)] public string Idioma { get; set; } = "Español";
    public List<DimensionInput> Dimensiones { get; set; } = [];
    public List<PreguntaInput> Preguntas { get; set; } = [];
}
public sealed class DimensionInput
{
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
}
public sealed class PreguntaInput
{
    [Required, StringLength(1000)] public string Texto { get; set; } = "";
    [Required, StringLength(100)] public string Dimension { get; set; } = "";
    public List<OpcionInput> Opciones { get; set; } = [];
}
public sealed class OpcionInput
{
    [Required, StringLength(300)] public string Texto { get; set; } = "";
    [Range(-10000, 10000)] public int Valor { get; set; }
}
public sealed class AsignarPruebaViewModel
{
    public Guid Id { get; set; }
    public Guid Revision { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public string Nombre { get; set; } = "";
    [Required, Display(Name = "Estudiante")] public Guid? PerfilEstudianteId { get; set; }
    public List<SelectListItem> Estudiantes { get; set; } = [];
    public List<AsignacionResumen> Asignaciones { get; set; } = [];
}
public sealed record AsignacionResumen(string Estudiante, string Carne, DateTime FechaUtc);
public sealed record DetallePruebaViewModel(Prueba Prueba, int Asignaciones);
