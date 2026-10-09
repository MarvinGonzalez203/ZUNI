namespace Zuni.Models.Evaluaciones;
public sealed class ResultadoEstudianteViewModel
{
    public Guid AsignacionId { get; set; }
    public string Titulo { get; set; } = "";
    public bool EsDemostracion { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public bool Publicado { get; set; }
    public decimal? Puntuacion { get; set; }
    public string? Observaciones { get; set; }
}
