namespace Zuni.Models;

public sealed class MiPerfilViewModel
{
    public string Nombre { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Carne { get; init; }
    public string? Telefono { get; init; }
    public string? Carrera { get; init; }
    public string? Semestre { get; init; }
    public string? CicloAcademico { get; init; }
    public string? NombreContactoEmergencia { get; init; }
    public string? TelefonoContactoEmergencia { get; init; }
    public string? RelacionContactoEmergencia { get; init; }
    public bool Completo { get; init; }

    public string? CarneFormateado => Carne is { Length: 10 }
        ? $"{Carne[..4]}-{Carne[4..6]}-{Carne[6..]}"
        : Carne;
}
