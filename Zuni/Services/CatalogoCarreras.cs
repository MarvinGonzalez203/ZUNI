using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
namespace Zuni.Services;
public static class CatalogoCarreras
{
    public static string Normalizar(string nombre) => nombre.Trim().Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
    public static List<(string Area,string Nombre)> Leer(string texto)
    {
        if(texto.Length>100000) throw new ArgumentException("El listado es demasiado grande.");
        var items=new List<(string,string)>();var area="General";
        foreach(var line in texto.Replace("\r", "").Split('\n')) {
            var l=line.Trim().TrimStart('\uFEFF');if(l.Length==0)continue;
            if(l.EndsWith(':')) {area=l[..^1].Trim();continue;}
            l=l.TrimStart('-', '•', ' ').Trim();var partes=l.Split(';',2);
            var nombre=partes.Length==2?partes[1].Trim():l;var categoria=partes.Length==2?partes[0].Trim():area;
            if(nombre.Length is <2 or >150 || categoria.Length is <1 or >100 || nombre.Any(char.IsControl)) throw new ArgumentException("Cada carrera requiere un nombre de 2 a 150 caracteres y un área de hasta 100.");
            items.Add((categoria,nombre));if(items.Count>500)throw new ArgumentException("Máximo 500 carreras por listado.");
        }
        if(items.Count==0)throw new ArgumentException("Agrega al menos una carrera.");
        return items.DistinctBy(x=>Normalizar(x.Item2)).ToList();
    }
    public static async Task<IReadOnlyList<SelectListItem>> Opciones(ApplicationDbContext db,string? actual,CancellationToken ct)
    {
        var rows=await db.CarrerasInstitucionales.AsNoTracking().Where(c=>c.Activa).OrderBy(c=>c.Area).ThenBy(c=>c.Nombre).ToListAsync(ct);
        var grupos=rows.Select(c=>c.Area).Distinct().ToDictionary(a=>a,a=>new SelectListGroup{Name=a});
        var items=rows.Select(c=>new SelectListItem(c.Nombre,c.Nombre){Group=grupos[c.Area]}).ToList();
        if(!string.IsNullOrWhiteSpace(actual)&&!items.Any(x=>x.Value==actual))items.Add(new(actual+" (registro anterior)",actual));
        return items;
    }
}
