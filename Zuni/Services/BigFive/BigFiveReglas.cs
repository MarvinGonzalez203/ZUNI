using Zuni.Models.BigFive;
using Zuni.Resources;
namespace Zuni.Services.BigFive;

public sealed class BigFiveOperacionException(string mensaje) : Exception(mensaje);
public sealed class BigFiveAccesoException : Exception;

public static class BigFiveReglas
{
    public static void ValidarConsentimiento(bool acepto, bool mayorDeEdad)
    {
        if (!acepto || !mayorDeEdad)
            throw new BigFiveOperacionException("Debes aceptar el consentimiento específico y confirmar que tienes 18 años o más.");
    }

    public static void Editable(ParticipacionBigFive p, int revision)
    {
        if (p.VersionInstrumento != Ipip50.Version || p.VersionConsentimiento != ConsentimientoBigFive.Version ||
            string.IsNullOrWhiteSpace(p.TextoConsentimiento) || p.FechaConsentimientoUtc == default ||
            p.FechaRetiroConsentimientoUtc != null || p.FechaFinalizacionUtc != null)
            throw new BigFiveOperacionException("Esta participación no admite cambios.");
        if (p.Revision != revision)
            throw new BigFiveOperacionException("El cuestionario cambió en otra pestaña. Recarga antes de continuar.");
    }

    public static void ValidarRespuestas(IReadOnlyDictionary<string, int?> respuestas)
    {
        var ids = Ipip50.Items.Select(i => i.Id.ToString("D")).ToHashSet(StringComparer.Ordinal);
        if (respuestas.Count > 50 || respuestas.Any(r => !ids.Contains(r.Key) || r.Value is < 1 or > 5))
            throw new BigFiveOperacionException("Las respuestas deben pertenecer a esta versión y contener valores entre 1 y 5.");
    }

    public static void Finalizar(ParticipacionBigFive p, int revision, bool confirmado,
        IEnumerable<RespuestaBigFive> respuestas, DateTime now)
    {
        Editable(p, revision);
        if (!confirmado) throw new BigFiveOperacionException("Confirma que deseas finalizar las respuestas guardadas.");
        var medias = Ipip50.Calcular(respuestas);
        p.Apertura = medias['O']; p.Responsabilidad = medias['C']; p.Extraversion = medias['E'];
        p.Amabilidad = medias['A']; p.Neuroticismo = medias['N'];
        p.FechaFinalizacionUtc = now;
        p.Revision++;
    }

    public static void Retirar(ParticipacionBigFive p, DateTime now)
    {
        if (p.FechaRetiroConsentimientoUtc != null) return;
        p.FechaRetiroConsentimientoUtc = now;
        p.Revision++;
    }
}
