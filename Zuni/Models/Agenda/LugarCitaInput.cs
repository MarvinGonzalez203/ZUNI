using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.Agenda;
public sealed class LugarCitaInput
{
    public Guid CitaId { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(500),Display(Name="Dirección o lugar de atención presencial")] public string DireccionAtencion { get; set; }="";
    public bool ConfirmarCita { get; set; }
    [BindNever] public string Estudiante { get; set; }="";
    [BindNever] public DateTime InicioUtc { get; set; }
    [BindNever] public EstadoCita Estado { get; set; }
}
