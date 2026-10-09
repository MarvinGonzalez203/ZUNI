namespace Zuni.Models.MiCuenta;

public sealed class MiCuentaViewModel
{
    public string NombreCompleto { get; init; } = string.Empty;
    public string? Email { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public bool Activo { get; init; }
    public bool EsEstudiante { get; init; }
    public bool PerfilCompleto { get; init; }
    public DatosEstudianteViewModel? Estudiante { get; init; }
}
