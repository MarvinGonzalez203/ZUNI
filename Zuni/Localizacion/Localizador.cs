using Microsoft.Extensions.Localization;
namespace Zuni.Localizacion;

public sealed class Localizador : IStringLocalizer
{
    public LocalizedString this[string name] => new(name,Idioma.Texto(name));
    public LocalizedString this[string name,params object[] arguments] => new(name,Idioma.Texto(name,arguments));
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Idioma.Recursos.Select(x=>new LocalizedString(x.Key,Idioma.Texto(x.Key)));
}
