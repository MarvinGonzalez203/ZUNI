namespace Zuni.Resources;
public sealed record NovedadVersion(string Numero, IReadOnlyList<string> Cambios);
public static class VersionSistema
{
    public const string Actual="1.2.0";
    // Only published local releases are listed; do not invent intermediate version numbers.
    public static readonly IReadOnlyList<NovedadVersion> Historial=[
        new(Actual,["Contratos imprimibles en PDF con membrete de ZUNI.","Submenús con tabulación y desplazamiento lateral sin arrastrar la página.","Selector de psicólogos visible y propuestas reunidas en una sola entrada."]),
        new("1.1.1",["Botones con forma, altura y tipografía consistentes en los paneles.","Submenús con sangría y conectores, conservando la alineación de los iconos al comprimir.","La campana conserva las últimas tres versiones y sus cambios."]),
        new("1.1.0",["Menú compacto con iconos propios y posición estable.","Guías por perfil, pasos ampliados y filtros sin recargar la página.","Contraste nocturno, avisos por rol y propuestas del Director revisados.","Control comercial de centros, periodos y vigencias."]),
        new("1.0.0",["Acceso móvil y navegación mejorados.","Cambio de idioma sin recargar formularios.","Centro de transparencia y guías de uso."])
    ];
    public static IReadOnlyList<string> Novedades => Historial[0].Cambios;
}
