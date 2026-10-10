namespace Zuni.Services;
public enum ResultadoAsignacion
{
    Asignado, YaAsignado, RequiereRevision, PendienteSinPsicologo,
    PendienteMultiplesPsicologos, PerfilNoElegible, ErrorTemporal
}
public interface IAsignacionPsicologoService
{
    Task<ResultadoAsignacion> IntentarAsignarAsync(Guid perfilEstudianteId, CancellationToken ct = default);
}
