using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Zuni.Models.Institucion;

namespace Zuni.Services.Institucion;

public static class PoliticaCorreoReglas
{
    public static bool EsCorreoValido(string? email)
    {
        var value = email?.Trim() ?? "";
        if (value.Length is 0 or > 254 || value.Count(c => c == '@') != 1 || value.Any(char.IsWhiteSpace)) return false;
        return MailAddress.TryCreate(value, out var address) && address.Address == value &&
               NormalizarDominio(value[(value.LastIndexOf('@') + 1)..]) is not null;
    }

    private static string? NormalizarDominio(string value)
    {
        try
        {
            var ascii = new IdnMapping().GetAscii(value.Trim()).ToLowerInvariant();
            if (ascii.Length > 253 || !ascii.Contains('.')) return null;
            var labels = ascii.Split('.');
            return labels.All(l => Regex.IsMatch(l, @"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\z")) &&
                   labels[^1].Any(char.IsLetter) ? ascii : null;
        }
        catch (ArgumentException) { return null; }
    }

    public static bool TryNormalizarDominios(string? text, out string[] dominios)
    {
        dominios = [];
        if ((text?.Length ?? 0) > 2000) return false;
        var values = (text ?? "").Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length > 20) return false;
        var normalized = values.Select(NormalizarDominio).ToArray();
        if (normalized.Any(x => x is null)) return false;
        dominios = normalized.Select(x => x!).Distinct(StringComparer.Ordinal).Order().ToArray();
        return true;
    }

    public static string? ValidarCorreo(ConfiguracionInstitucion config, string? email, bool registroPublico)
    {
        if (registroPublico && config.PoliticaCorreo == PoliticaCorreoInstitucion.SoloAltasAdministradas)
            return "El registro público está cerrado. Solicita tu cuenta al Administrador de la institución.";
        if (!EsCorreoValido(email)) return "Ingresa un correo electrónico válido.";
        if (!Enum.IsDefined(config.PoliticaCorreo)) return "La política de correo necesita revisión del Administrador.";
        if (config.PoliticaCorreo != PoliticaCorreoInstitucion.DominiosAutorizados) return null;
        var domain = NormalizarDominio(email!.Trim()[(email.Trim().LastIndexOf('@') + 1)..]);
        return TryNormalizarDominios(config.DominiosAutorizados, out var allowed) && allowed.Contains(domain)
            ? null : "El correo debe pertenecer a uno de los dominios autorizados por la institución.";
    }

    public static string Descripcion(ConfiguracionInstitucion config) => config.PoliticaCorreo switch
    {
        PoliticaCorreoInstitucion.DominiosAutorizados => "Dominios admitidos: " + config.DominiosAutorizados.Replace("\n", ", ") + ".",
        PoliticaCorreoInstitucion.CualquierCorreo => "Puedes utilizar cualquier correo electrónico válido. El Administrador debe activar tu cuenta.",
        _ => "Las cuentas son creadas por el Administrador de la institución."
    };
}
