namespace Zuni.Localizacion;
// Only ZUNI's fixed catalog has curated translations; custom instruments remain as authored.
public static class TextoPrueba
{
    public static string Texto(Guid id,string? texto)=>Enumerable.Range(1,3).Any(n=>id==Guid.Parse($"eeeeeeee-0001-{n:0000}-0000-000000000000"))?Idioma.Texto(texto):texto??"";
}
