using System.ComponentModel.DataAnnotations;
using Zuni.Models.Atencion;

namespace Zuni.Models.Administrador;

// Vista administrativa de solicitudes: SOLO metadatos. Nunca incluye motivo de consulta,
// expediente, contactos de emergencia, Big Five, notas ni resultados clínicos.
public sealed class SolicitudAdminFila
{
    public Guid SolicitudId { get; init; }
    public string Estudiante { get; init; } = string.Empty;
    public string Carne { get; init; } = string.Empty;
    public string? Carrera { get; init; }
    public DateTime FechaSolicitudUtc { get; init; }
    public EstadoSolicitudAtencion Estado { get; init; }
    public string? Psicologo { get; init; }
    public bool? PsicologoActivo { get; init; }
    public DateTime? FechaAsignacionUtc { get; init; }
}

public sealed class SolicitudesAtencionAdminViewModel
{
    public EstadoSolicitudAtencion? Estado { get; init; }
    public string? Buscar { get; init; }
    public int Pagina { get; init; } = 1;
    public int Paginas { get; init; } = 1;
    public int Pendientes { get; init; }
    public int Asignadas { get; init; }
    public int Cerradas { get; init; }
    public IReadOnlyList<SolicitudAdminFila> Solicitudes { get; init; } = [];
}

public sealed record PsicologoElegible(string Id, string Nombre, string? Correo, bool DebeCambiarContrasena);

public sealed record AsignacionHistorial(string Psicologo, DateTime FechaAsignacionUtc, DateTime? FechaFinalizacionUtc);

public sealed class DetalleSolicitudAdminViewModel
{
    public SolicitudAdminFila Solicitud { get; init; } = new();
    public string? PsicologoActualId { get; init; }
    public int CitasActivas { get; init; }
    public IReadOnlyList<AsignacionHistorial> Historial { get; init; } = [];
    public IReadOnlyList<PsicologoElegible> Elegibles { get; init; } = [];
}

public sealed class OperacionSolicitudInput
{
    [Required]
    public Guid SolicitudId { get; set; }

    public string? PsicologoUsuarioId { get; set; }

    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string? Motivo { get; set; }

    public bool Confirmar { get; set; }
}
