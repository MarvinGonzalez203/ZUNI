namespace Zuni.Models.Atencion;
// Sin navegación inversa desde ApplicationUser/PerfilEstudiante: no cargar contenido clínico en administración.
public sealed class SolicitudAtencion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilEstudianteId { get; set; }
    public DateTime FechaSolicitudUtc { get; set; } = DateTime.UtcNow;
    public EstadoSolicitudAtencion Estado { get; set; } = EstadoSolicitudAtencion.Pendiente;
    public TipoIngresoAtencion TipoIngreso { get; set; }
    public string? UsuarioReferenteId { get; set; }
}
