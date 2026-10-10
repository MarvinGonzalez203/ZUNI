namespace Zuni.Models.Atencion;
public sealed class ExpedienteInicial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SolicitudAtencionId { get; set; }
    public int Edad { get; set; }
    public bool EsMayorEdad { get; set; }
    public string? Sexo { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string IdiomaPreferido { get; set; } = string.Empty;
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? NombreReferente { get; set; }
    public string? MotivoReferencia { get; set; }
    public string? ConsideracionesAtencion { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
}
