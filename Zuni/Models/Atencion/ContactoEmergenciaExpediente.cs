namespace Zuni.Models.Atencion;
public sealed class ContactoEmergenciaExpediente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpedienteInicialId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Relacion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int Orden { get; set; }
}
