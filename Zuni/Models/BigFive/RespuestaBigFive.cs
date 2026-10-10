namespace Zuni.Models.BigFive;

public sealed class RespuestaBigFive
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParticipacionBigFiveId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Valor { get; set; }
    public DateTime FechaActualizacionUtc { get; set; }
}
