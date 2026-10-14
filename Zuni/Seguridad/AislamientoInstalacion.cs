using System.Text.RegularExpressions;
namespace Zuni.Seguridad;

public static class AislamientoInstalacion
{
    public static string Nombre(IConfiguration config)
    {
        var id = config["Zuni:InstallationId"];
        if (string.IsNullOrEmpty(id)) return "Zuni";
        if (!Regex.IsMatch(id, "^[a-z][a-z0-9-]{1,39}$")) throw new InvalidOperationException("Zuni:InstallationId debe tener entre 2 y 40 letras minúsculas, números o guiones y comenzar con letra.");
        return "Zuni." + id;
    }
    public static string Cookie(IConfiguration config) => Nombre(config) == "Zuni" ? ".AspNetCore.Cookies" : "." + Nombre(config) + ".Sesion";
}
