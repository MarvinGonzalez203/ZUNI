using System.Text.Json;

namespace Zuni.Models.Administrador;

public sealed class HistorialUsuarioViewModel
{
    public string UsuarioId { get; init; } = string.Empty;
    public string NombreUsuario { get; init; } = string.Empty;
    public string? Email { get; init; }
    public IReadOnlyList<EventoAuditoriaUsuarioViewModel> Eventos { get; init; } =
        Array.Empty<EventoAuditoriaUsuarioViewModel>();
}

public sealed class EventoAuditoriaUsuarioViewModel
{
    public string Accion { get; init; } = string.Empty;
    public DateTime FechaUtc { get; init; }
    public string? Motivo { get; init; }
    public string? DatosAnteriores { get; init; }
    public string? DatosNuevos { get; init; }

    public string AccionVisible => Accion switch
    {
        "ROL_CAMBIADO" => "Cambio de rol",
        "USUARIO_CREADO" => "Usuario creado",
        "USUARIO_DESACTIVADO" => "Usuario desactivado",
        "USUARIO_REACTIVADO" => "Usuario reactivado",
        "REGISTRO_PENDIENTE_ACTIVACION" => "Autorregistro pendiente de activación",
        "CONTRASENA_TEMPORAL_ASIGNADA" => "Contraseña temporal asignada",
        "PRIMER_ADMIN_CREADO" => "Primer administrador creado",
        "SOLICITUD_ASIGNADA" => "Solicitud de atención asignada",
        "SOLICITUD_REASIGNADA" => "Solicitud de atención reasignada",
        "ASIGNACION_FINALIZADA" => "Asignación finalizada",
        "SOLICITUD_CERRADA" => "Solicitud de atención cerrada",
        _ => "Acción no disponible"
    };

    public string EstadoAnterior => Accion is "USUARIO_CREADO" or "REGISTRO_PENDIENTE_ACTIVACION" or "CONTRASENA_TEMPORAL_ASIGNADA" or "PRIMER_ADMIN_CREADO"
        && DatosAnteriores is null ? "—" : InterpretarDatos(DatosAnteriores);
    public string EstadoNuevo => InterpretarDatos(DatosNuevos);

    private string InterpretarDatos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "No disponible";

        try
        {
            using var document = JsonDocument.Parse(json);
            var raiz = document.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object)
                return "No disponible";

            if ((Accion == "ROL_CAMBIADO" || Accion == "USUARIO_CREADO") && raiz.TryGetProperty("Roles", out var roles) &&
                roles.ValueKind == JsonValueKind.Array)
            {
                var nombres = new List<string>();
                foreach (var rol in roles.EnumerateArray())
                {
                    if (rol.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(rol.GetString()))
                        return "No disponible";

                    nombres.Add(rol.GetString() switch
                    {
                        "Psicologo" => "Psicólogo",
                        "Catedratico" => "Catedrático",
                        var nombre => nombre!
                    });
                }
                var textoRoles = nombres.Count == 0 ? "Sin rol" : string.Join(", ", nombres);
                if (Accion == "USUARIO_CREADO")
                {
                    if (!raiz.TryGetProperty("IsActive", out var estado) ||
                        (estado.ValueKind != JsonValueKind.True && estado.ValueKind != JsonValueKind.False))
                        return "No disponible";
                    return textoRoles + (estado.GetBoolean() ? " · Activo" : " · Inactivo");
                }
                return textoRoles;
            }

            if (Accion is "SOLICITUD_ASIGNADA" or "SOLICITUD_REASIGNADA" or "ASIGNACION_FINALIZADA" or "SOLICITUD_CERRADA")
            {
                // Solo metadatos administrativos: estado de la solicitud y si hay psicólogo.
                var texto = raiz.TryGetProperty("Estado", out var estadoSolicitud) && estadoSolicitud.ValueKind == JsonValueKind.String
                    ? "Solicitud " + estadoSolicitud.GetString()!.ToLowerInvariant() : "Solicitud";
                return raiz.TryGetProperty("PsicologoUsuarioId", out var psicologo) && psicologo.ValueKind == JsonValueKind.String
                    ? texto + " · con psicólogo" : texto;
            }

            if (Accion == "CONTRASENA_TEMPORAL_ASIGNADA")
                return "Debe cambiar la contraseña al iniciar sesión";

            if ((Accion == "USUARIO_DESACTIVADO" || Accion == "USUARIO_REACTIVADO" ||
                 Accion == "REGISTRO_PENDIENTE_ACTIVACION" || Accion == "PRIMER_ADMIN_CREADO") &&
                raiz.TryGetProperty("IsActive", out var activo))
            {
                return activo.ValueKind switch
                {
                    JsonValueKind.True => "Activo",
                    JsonValueKind.False => "Inactivo",
                    _ => "No disponible"
                };
            }
        }
        catch (JsonException)
        {
            // Un registro mal formado no impide consultar el resto del historial.
        }

        return "No disponible";
    }
}
