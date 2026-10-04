namespace Zuni.Models;

// Contrato compartido. La interpretación clínica nunca forma parte de este modelo.
public sealed record HistorialAtencionItem(
    Guid Id,
    DateTimeOffset Fecha,
    string Estudiante,
    string Codigo,
    string Profesional,
    string Modalidad,
    string Resena,
    string Recomendaciones,
    string Seguimiento);
