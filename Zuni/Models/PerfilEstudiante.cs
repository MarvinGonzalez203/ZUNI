using System.ComponentModel.DataAnnotations.Schema;

namespace Zuni.Models;

public sealed class PerfilEstudiante
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioId { get; set; } = string.Empty;
    public string Carne { get; set; } = string.Empty;
    public string? Carrera { get; set; }
    public string? Semestre { get; set; }
    public string? CicloAcademico { get; set; }
    public string? Telefono { get; set; }
    public string? NombreContactoEmergencia { get; set; }
    public string? TelefonoContactoEmergencia { get; set; }
    public string? RelacionContactoEmergencia { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    public ApplicationUser Usuario { get; set; } = null!;

    [NotMapped]
    public bool EstaCompleto =>
        !string.IsNullOrWhiteSpace(Carne) &&
        !string.IsNullOrWhiteSpace(Telefono) &&
        !string.IsNullOrWhiteSpace(Carrera);
}
