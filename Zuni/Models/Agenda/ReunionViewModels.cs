using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zuni.Models.Agenda;

public sealed class GuardarReunionInput
{
    public Guid CitaId { get; set; }
    public Guid Revision { get; set; }
    [Required, StringLength(120), Display(Name = "Enlace individual de Google Meet")]
    public string Enlace { get; set; } = "";
    [Display(Name = "Revisé el acceso restringido, los invitados y la admisión por el anfitrión")]
    public bool AccesoRestringidoRevisado { get; set; }
    public bool ConfirmarCita { get; set; }
    [BindNever] public string Estudiante { get; set; } = "";
    [BindNever] public DateTime InicioUtc { get; set; }
    [BindNever] public EstadoCita Estado { get; set; }
}
public sealed record AccesoReunionViewModel(Guid CitaId, DateTime InicioUtc, DateTime FinUtc, bool EnlacePreparado, bool PuedeEntrar);
