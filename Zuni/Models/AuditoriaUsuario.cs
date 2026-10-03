namespace Zuni.Models;

public sealed class AuditoriaUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioAfectadoId { get; set; } = string.Empty;
    public string AdministradorId { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? DatosAnteriores { get; set; }
    public string? DatosNuevos { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}
