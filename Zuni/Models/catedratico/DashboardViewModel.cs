namespace Zuni.Models.Catedratico;

public sealed class DashboardViewModel
{
    public string NombreCatedratico { get; init; } = string.Empty;

    public string PrimerNombre =>
        NombreCatedratico
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "Catedrático";
}