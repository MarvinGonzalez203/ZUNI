using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Resources;

internal static class PortalDenisCorpChecks
{
    internal static async Task Run()
    {
        var checks=0;void Check(bool ok,string label){if(!ok)throw new Exception("Portal Denis Corp: "+label);checks++;}
        await using var conexion=new SqliteConnection("Data Source=:memory:");await conexion.OpenAsync();
        var options=new DbContextOptionsBuilder<Zuni.Data.ApplicationDbContext>().UseSqlite(conexion).Options;
        await using(var db=new OfflineDb(options))await db.Database.EnsureCreatedAsync();
        await using var app=await HostPrueba.Crear(options,new Reloj(),new HasherContador(),100);
        using var anon=HostPrueba.Cliente(app);
        var salida=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(salida is not null)Directory.CreateDirectory(salida);
        Check(PortalDenisCorp.Productos.Select(p=>p.Id).SequenceEqual(new[]{"zuni","academy","fulleo","pos","colegios"}),"cinco programas y orden estable");
        Check(PortalDenisCorp.Productos.Select(p=>p.Icono).Distinct().Count()==5,"identidades propias incluyendo ZUNI");
        foreach(var idioma in new[]{"es","en"})
        {
            var cambio=await HostPrueba.Post(anon,"/","/Idioma/Cambiar",("idioma",idioma),("volver","/deniscorp/fulleo/#avances"));
            Check(cambio.StatusCode==HttpStatusCode.Redirect&&cambio.Headers.Location?.OriginalString=="/deniscorp/fulleo/#avances","idioma conserva producto y sección "+idioma);
            foreach(var (nombre,ruta) in new[]{("deniscorp","/"),("academy","/deniscorp/academy/"),("fulleo","/deniscorp/fulleo/"),("pos","/deniscorp/pos/"),("colegios","/deniscorp/colegios/")})
            {
                var r=await anon.GetAsync(ruta);var html=await r.Content.ReadAsStringAsync();var texto=WebUtility.HtmlDecode(html);
                Check(r.StatusCode==HttpStatusCode.OK&&html.Contains("lang=\""+idioma+"\""),"ruta pública en idioma "+nombre+idioma);
                Check(html.Contains("zuni-language-control")&&html.Contains("zuni-theme-control")&&html.Contains("corp-scroll-control"),"controles visibles y accesibles "+nombre+idioma);
                Check(html.Contains("data-view-scope=\"denis-corp\""),"tema de Denis Corp separado de preferencias de ZUNI "+nombre+idioma);
                var icono=nombre=="deniscorp"?"/images/productos/deniscorp-32.png":PortalDenisCorp.Obtener(nombre).Icono;
                Check(Regex.IsMatch(html,"rel=\"icon\"[^>]*href=\""+Regex.Escape(icono)),"favicon correcto "+nombre+idioma);
                Check(!html.Contains("C:\\")&&!html.Contains("SourcePath"),"no exponer rutas internas en fichas públicas "+nombre+idioma);
                if(nombre=="deniscorp")
                {
                    Check(Regex.Matches(html,"data-product=\"").Count==5,"cinco tarjetas "+idioma);
                    Check(html.Contains("brand-glass")&&html.Contains("/images/zuni-logo-transparent.png"),"logo ZUNI sobre cristal "+idioma);
                    Check(texto.Contains(idioma=="es"?"Colegios y gestión escolar":"Schools and administration"),"catálogo traducido "+idioma);
                }
                else
                {
                    Check(texto.Contains(idioma=="es"?"Avances del programa":"Product progress")&&texto.Contains(PortalDenisCorp.Obtener(nombre).Version),"avances con versión documentada "+nombre+idioma);
                    Check(texto.Contains(idioma=="es"?"En preparación":"In preparation"),"pendientes distinguidos de lo implementado "+nombre+idioma);
                }
                if(nombre=="fulleo")Check(texto.Contains("0.8.0")&&texto.Contains("19")&&texto.Contains("88")&&texto.Contains(idioma=="es"?"datos históricos":"historical test data"),"avances de Fulleo no simulan precios actuales "+idioma);
                if(salida is not null)await File.WriteAllTextAsync(Path.Combine(salida,nombre+"-"+idioma+".html"),html);
            }
        }
        Check((await anon.GetAsync("/deniscorp/")).StatusCode==HttpStatusCode.OK,"alias del portal conservado");
        var zuni=await anon.GetStringAsync("/deniscorp/zuni/");Check(zuni.Contains("favicon-96.png")&&!zuni.Contains("deniscorp-32.png"),"ZUNI mantiene sus propios favicons");
        await app.StopAsync();Console.WriteLine($"OK: {checks} comprobaciones HTTP de portal, idiomas, avances e identidades.");
    }
}
