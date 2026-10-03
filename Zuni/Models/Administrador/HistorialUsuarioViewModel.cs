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
        "USUARIO_DESACTIVADO" => "Usuario desactivado",
        "USUARIO_REACTIVADO" => "Usuario reactivado",
        _ => "Acción no disponible"
    };

    public string EstadoAnterior => InterpretarDatos(DatosAnteriores);
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

            if (Accion == "ROL_CAMBIADO" && raiz.TryGetProperty("Roles", out var roles) &&
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
                return nombres.Count == 0 ? "Sin rol" : string.Join(", ", nombres);
            }

            if ((Accion == "USUARIO_DESACTIVADO" || Accion == "USUARIO_REACTIVADO") &&
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
