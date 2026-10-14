namespace Zuni.Models.Administrador;

// Tablero del Administrador: solo conteos agregados, sin listado de usuarios.
public sealed class PanelAdministradorViewModel
{
    public int Total { get; set; }
    public int Activos { get; set; }
    public int Inactivos => Total - Activos;
    public int PendientesActivacion { get; set; }
    public int SinRol { get; set; }
    public int SolicitudesPendientes { get; set; }
    public Dictionary<string, int> UsuariosPorRol { get; set; } = [];
}
