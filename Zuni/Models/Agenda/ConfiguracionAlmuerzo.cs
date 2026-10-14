using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.Agenda;
public sealed class ConfiguracionAlmuerzo
{
    public string PsicologoUsuarioId { get; set; } = "";
    public bool Habilitado { get; set; }
    public TimeOnly Inicio { get; set; } = new(13,0);
    public TimeOnly Fin { get; set; } = new(14,0);
    public Guid Revision { get; set; } = Guid.NewGuid();
}
public sealed class AlmuerzoInput
{
    public bool Habilitado { get; set; }
    [Required] public TimeOnly? Inicio { get; set; } = new(13,0);
    [Required] public TimeOnly? Fin { get; set; } = new(14,0);
    public Guid Revision { get; set; }
}
