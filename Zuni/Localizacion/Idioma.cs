using System.Globalization;
using System.Text.Json;

namespace Zuni.Localizacion;

public static class Idioma
{
    private static readonly IReadOnlyDictionary<string,string> Ingles = Cargar();
    public static bool EsIngles => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
    public static string Codigo => EsIngles ? "en" : "es";
    public static string Roles(string? roles)=>string.Join(", ",(roles??"").Split(',',StringSplitOptions.TrimEntries).Select(r=>Texto(r)));
    private static IReadOnlyDictionary<string,string> Cargar()
    {
        using var stream=typeof(Idioma).Assembly.GetManifestResourceStream("Zuni.Localizacion.en.json")!;
        var result=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var pair in JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!)result.TryAdd(pair.Key,pair.Value);
        return result;
    }
    public static IEnumerable<KeyValuePair<string,string>> Recursos => Ingles;
    public static string Texto(Enum? valor)=>Texto(valor?.ToString());
    public static string Texto(string? texto, params object[] valores)
    {
        var original=texto ?? "";
        var traducido=original;
        if(EsIngles)
        {
            if(Ingles.TryGetValue(original,out var ingles))traducido=ingles;
            else if(original.Any(char.IsLetter)&&original.Where(char.IsLetter).All(char.IsUpper)&&Ingles.TryGetValue(CultureInfo.GetCultureInfo("es").TextInfo.ToTitleCase(original.ToLowerInvariant()),out ingles))traducido=ingles;
        }
        if(EsIngles&&original.Any(char.IsLetter)&&original.Where(char.IsLetter).All(char.IsUpper))traducido=traducido.ToUpperInvariant();
        return valores.Length==0 ? traducido : string.Format(CultureInfo.CurrentCulture,traducido,valores);
    }
}
