namespace Zuni.Models;

// Modelo de presentación: no se registra como entidad de Entity Framework.
public sealed class EstudianteDashboardViewModel
{
    public string Nombre { get; init; } = string.Empty;
    public bool PerfilCompleto { get; init; }
}
