namespace Zuni.Models.Administrador;

public sealed class UsuarioAdminViewModel
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public string Rol { get; set; } = "Sin rol";
}
