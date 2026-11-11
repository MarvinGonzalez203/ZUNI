using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zuni.Models.Administrador;

public sealed class AsignacionFichaInput
{
    [Required] public string UsuarioId { get; set; } = "";
    public Guid PerfilEstudianteId { get; set; }
    [Required] public string PsicologoUsuarioId { get; set; } = "";
    public Guid? AsignacionActualId { get; set; }
    [StringLength(500)] public string? Motivo { get; set; }
}
public sealed record FilaAsignacionFicha(Guid PerfilId, string UsuarioId, string Nombre, string Carne,
    Guid? AsignacionId, string? PsicologoId, string? Psicologo);
public sealed class AsignacionesFichaViewModel
{
    public string UsuarioId { get; init; } = "";
    public string Nombre { get; init; } = "";
    public bool DesdePsicologo { get; init; }
    public string? Buscar { get; init; }
    public int Pagina { get; init; }
    public int Paginas { get; init; }
    public IReadOnlyList<FilaAsignacionFicha> Estudiantes { get; init; } = [];
    public IReadOnlyList<SelectListItem> Psicologos { get; init; } = [];
}
