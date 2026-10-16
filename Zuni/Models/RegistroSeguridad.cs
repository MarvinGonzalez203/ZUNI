namespace Zuni.Models;

// Evidencia inmutable y bandeja de salida transaccional. El contenido contiene el evento,
// nunca se envía al navegador ni se incluye en logs técnicos.
public sealed class RegistroSeguridad
{
    public long Id { get; set; }
    public Guid EventoId { get; set; } = Guid.NewGuid();
    public string Tipo { get; set; } = "";
    public string Referencia { get; set; } = "";
    public DateTime FechaUtc { get; set; }
    public string ContenidoCifrado { get; set; } = "";
    public string Firma { get; set; } = "";
}

public sealed class ExportacionSeguridad
{
    public Guid EventoId { get; set; }
    public DateTime FechaUtc { get; set; }
}
