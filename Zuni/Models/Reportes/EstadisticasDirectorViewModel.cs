namespace Zuni.Models.Reportes;

// Indicadores agregados para el Director. Ningún elemento contiene nombre, carné, correo,
// Id de perfil, solicitud o cita. Los conteos pequeños por carrera se ocultan (Cantidad = null).
public sealed record ConteoAgregado(string Etiqueta, int? Cantidad);
public sealed record CargaPsicologoAgregada(string Psicologo, int Activas, int Completadas);

public sealed class EstadisticasDirectorViewModel
{
    public const int UmbralMinimo = 5;
    public int SolicitudesTotal { get; init; }
    public int SolicitudesPendientes { get; init; }
    public int SolicitudesAsignadas { get; init; }
    public int SolicitudesCerradas { get; init; }
    public int EstudiantesAtendidos { get; init; }
    public int CitasProgramadas { get; init; }
    public int CitasPendientes { get; init; }
    public int CitasCompletadas { get; init; }
    public int CitasCanceladas { get; init; }
    public int Inasistencias { get; init; }
    public IReadOnlyList<CargaPsicologoAgregada> CargaPorPsicologo { get; init; } = [];
    public IReadOnlyList<ConteoAgregado> TendenciaMensual { get; init; } = [];
    public IReadOnlyList<ConteoAgregado> AtendidosPorCarrera { get; init; } = [];
}
