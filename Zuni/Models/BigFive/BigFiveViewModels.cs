using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.BigFive;

public sealed class ConsentimientoBigFiveInput
{
    public bool Acepto { get; set; }
    public bool MayorDeEdad { get; set; }
}
public sealed class GuardarBigFiveInput
{
    public Guid Id { get; set; }
    [Range(0, int.MaxValue)]
    public int Revision { get; set; }
    public Dictionary<string, int?> Respuestas { get; set; } = new();
}
public sealed class FinalizarBigFiveInput
{
    public Guid Id { get; set; }
    [Range(0, int.MaxValue)]
    public int Revision { get; set; }
    public bool Confirmado { get; set; }
}
public sealed class EstadoBigFiveViewModel
{
    public Guid? Id { get; set; }
    public bool TieneSolicitudActiva { get; set; }
    public bool PuedeResponder { get; set; }
    public string VersionInstrumento { get; set; } = string.Empty;
    public string TextoConsentimiento { get; set; } = string.Empty;
    public DateTime? FechaConsentimientoUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public DateTime? FechaRetiroConsentimientoUtc { get; set; }
}
public sealed class CuestionarioBigFiveViewModel
{
    public Guid Id { get; set; }
    public int Revision { get; set; }
    public Dictionary<string, int?> Respuestas { get; set; } = new();
}
public sealed record DimensionBigFive(string Nombre, decimal Media);
public sealed record ResumenBigFiveViewModel(Guid SolicitudAtencionId, string VersionInstrumento,
    DateTime FechaFinalizacionUtc, IReadOnlyList<DimensionBigFive> Dimensiones);
