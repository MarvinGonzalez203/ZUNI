using System.ComponentModel.DataAnnotations;
namespace Zuni.Models;

public sealed class DiaAgendaPsicologo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PsicologoId { get; set; } = "";
    public ApplicationUser Psicologo { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public bool Ocupado { get; set; }
    public List<HorarioAgendaPsicologo> Horarios { get; set; } = [];
}
public sealed class HorarioAgendaPsicologo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiaId { get; set; }
    public DiaAgendaPsicologo Dia { get; set; } = null!;
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }
    public int DuracionMinutos { get; set; }
    public string Modalidad { get; set; } = "Presencial";
}
public sealed class HorarioAgendaInput
{
    public DateOnly Fecha { get; set; }
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }
    public int DuracionMinutos { get; set; } = 50;
    [Required] public string Modalidad { get; set; } = "Presencial";
}
public sealed record AgendaCalendario(DateOnly Mes, string NombrePsicologo, bool Habilitado, bool Editor, IReadOnlyList<DiaAgendaPsicologo> Dias)
{
    public IReadOnlyList<FranjaCita> Franjas { get; init; }=[];
    public IReadOnlyList<CitaResumen> Citas { get; init; }=[];
    public CitaResumen? Reprogramar { get; init; }
}
