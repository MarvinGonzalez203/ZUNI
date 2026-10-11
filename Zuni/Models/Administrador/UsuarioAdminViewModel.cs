namespace Zuni.Models.Administrador;

public sealed class UsuarioAdminViewModel
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    // Autorregistro aún no activado por un Administrador (derivado de la auditoría, sin columna nueva).
    public bool PendienteActivacion { get; init; }
    public string Rol { get; init; } = "Sin rol";
}
