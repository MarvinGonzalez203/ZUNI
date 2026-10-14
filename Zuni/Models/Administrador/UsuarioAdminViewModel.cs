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
    // Datos administrativos opcionales según la vista (nunca información clínica).
    public string? Carne { get; init; }
    public string? Carrera { get; init; }
    public int AsignacionesVigentes { get; init; }
}

public sealed class UsuariosPorRolViewModel
{
    // Nombre del rol en Identity; null = usuarios sin rol.
    public string? Rol { get; init; }
    // Acción del controlador que generó la vista (para filtros, paginación y menú).
    public string Accion { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string? Buscar { get; init; }
    public string? Estado { get; init; }
    public int Pagina { get; init; } = 1;
    public int Paginas { get; init; } = 1;
    public int Total { get; init; }
    public IReadOnlyList<UsuarioAdminViewModel> Usuarios { get; init; } = [];
    public bool EsEstudiantes => Rol == "Estudiante";
    public bool EsPsicologos => Rol == "Psicologo";
}

public sealed class GestionarUsuarioViewModel
{
    public string Revision { get; init; } = string.Empty;
    public bool? MayorEdadDeclarada { get; init; }
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public bool PendienteActivacion { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public bool DebeCambiarContrasena { get; init; }
    public bool BloqueadoTemporalmente { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public string? Carne { get; init; }
    public string? Carrera { get; init; }
    public int AsignacionesVigentes { get; init; }
    public bool EsUnoMismo { get; init; }

    // Vista por rol a la que vuelve el enlace "Volver" (primer rol en el orden del menú).
    public string AccionVolver => Roles.Contains("Administrador") ? "Administradores"
        : Roles.Contains("Director") ? "Directores"
        : Roles.Contains("Psicologo") ? "Psicologos"
        : Roles.Contains("Catedratico") ? "Catedraticos"
        : Roles.Contains("Estudiante") ? "Estudiantes"
        : "SinRol";
}
