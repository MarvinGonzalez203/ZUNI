namespace Zuni.Models.Agenda;

public sealed class ConfiguracionAgendaDia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PsicologoUsuarioId { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public bool Bloqueado { get; set; }
    public bool UsaBaseSemanal { get; set; } = true;
}
