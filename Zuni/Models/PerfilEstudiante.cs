namespace Zuni.Models;

public sealed class PerfilEstudiante
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioId { get; set; } = string.Empty;
    public string Carne { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public string? Semestre { get; set; }
    public string? Telefono { get; set; }
    public string? NombreContactoEmergencia { get; set; }
    public string? TelefonoContactoEmergencia { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    public ApplicationUser Usuario { get; set; } = null!;
}
