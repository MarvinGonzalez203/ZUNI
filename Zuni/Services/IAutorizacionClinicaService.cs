using System.Security.Claims;
namespace Zuni.Services;
public interface IAutorizacionClinicaService
{
    Task<string?> ObtenerPsicologoAutorizadoAsync(ClaimsPrincipal sesion, CancellationToken ct = default);
    Task<bool> PuedeConsultarExpedienteAsync(ClaimsPrincipal sesion, Guid solicitudId, CancellationToken ct = default);
}
