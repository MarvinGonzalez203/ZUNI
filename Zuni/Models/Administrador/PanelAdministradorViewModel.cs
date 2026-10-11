namespace Zuni.Models.Administrador;

public sealed class PanelAdministradorViewModel
{
    public string? Buscar { get; set; }
    public string? Rol { get; set; }
    public string? Estado { get; set; }
    public int Total { get; set; }
    public int Activos { get; set; }
    public int Inactivos => Total - Activos;
    public Dictionary<string, int> UsuariosPorRol { get; set; } = [];
    public IReadOnlyList<UsuarioAdminViewModel> Usuarios { get; set; } = [];
}
