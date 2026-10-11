namespace Zuni.Models.Atencion;
public sealed class EstudianteAsignadoViewModel
{
    public string Nombre { get; init; } = string.Empty;
    public string Carne { get; init; } = string.Empty;
    public string? Carrera { get; init; }
    public DateTime FechaAsignacionUtc { get; init; }
    public Guid? SolicitudId { get; init; }
}
public sealed class ExpedienteConsultaViewModel
{
    public string Nombre { get; init; } = string.Empty;
    public string Carne { get; init; } = string.Empty;
    public string? Carrera { get; init; }
    public DateTime FechaSolicitudUtc { get; init; }
    public TipoIngresoAtencion TipoIngreso { get; init; }
    public int Edad { get; init; }
    public bool EsMayorEdad { get; init; }
    public string? Sexo { get; init; }
    public string Direccion { get; init; } = string.Empty;
    public string IdiomaPreferido { get; init; } = string.Empty;
    public string MotivoConsulta { get; init; } = string.Empty;
    public string? NombreReferente { get; init; }
    public string? MotivoReferencia { get; init; }
    public string? ConsideracionesAtencion { get; init; }
    public List<ContactoSolicitudViewModel> Contactos { get; set; } = [];
}
