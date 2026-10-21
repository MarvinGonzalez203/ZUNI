namespace Zuni.Models.Reportes;

// Indicadores agregados para el Director. Ningún elemento contiene nombre, carné, correo,
// Id de perfil, solicitud o cita. Las cifras de personas pasan por supresión primaria y
// complementaria en el servidor (Services/Reportes/SupresionEstadistica.cs).
public sealed record ConteoAgregado(string Etiqueta, int? Cantidad);
// Concluidas = completadas + inasistencias: Dirección no distingue asistencia (registro privado de atención).
public sealed record CargaPsicologoAgregada(string Psicologo, int Activas, int Concluidas);
// Desglose publicable: celdas visibles (todas >= umbral), grupo "Otras" (>= umbral, al menos dos celdas)
// o, si no puede protegerse, Disponible = false y solo se publica el total.
public sealed record DesgloseProtegido(bool Disponible, IReadOnlyList<ConteoAgregado> Visibles, int? Agrupado, int CeldasAgrupadas, int Total);
public sealed record DesgloseVista(string Id, DesgloseProtegido Desglose, string EtiquetaGrupo);

public sealed class EstadisticasDirectorViewModel
{
    public const int UmbralMinimo = 5;
    // Los indicadores de personas usan datos hasta el cierre del mes anterior (fecha de corte exclusiva).
    public DateOnly Corte { get; init; }
    public int? SolicitudesTotal { get; init; }
    public DesgloseProtegido SolicitudesPorEstado { get; init; } = new(true, [], null, 0, 0);
    public int? EstudiantesConCitasConcluidas { get; init; }
    public DesgloseProtegido PorCarrera { get; init; } = new(true, [], null, 0, 0);
    public IReadOnlyList<ConteoAgregado> TendenciaMensual { get; init; } = [];
    // Agenda vigente (citas, no personas): sin distinguir asistencia de inasistencia.
    public int CitasProgramadas { get; init; }
    public int CitasPendientes { get; init; }
    public int CitasConcluidas { get; init; }
    public int CitasCanceladas { get; init; }
    public IReadOnlyList<CargaPsicologoAgregada> CargaPorPsicologo { get; init; } = [];
}
