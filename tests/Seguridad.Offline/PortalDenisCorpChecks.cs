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
        Check(PortalDenisCorp.Productos.Select(p=>p.Id).SequenceEqual(new[]{"zuni","academy","fulleo","pos","colegios","logistica-transporte"}),"cinco programas y orden estable");
        Check(PortalDenisCorp.Productos.Select(p=>p.Icono).Distinct().Count()==6,"identidades propias incluyendo ZUNI");
        foreach(var idioma in new[]{"es","en"})
        {
            var cambio=await HostPrueba.Post(anon,"/","/Idioma/Cambiar",("idioma",idioma),("volver","/deniscorp/programas/fulleo/#avances"));
            Check(cambio.StatusCode==HttpStatusCode.Redirect&&cambio.Headers.Location?.OriginalString=="/deniscorp/programas/fulleo/#avances","idioma conserva producto y sección "+idioma);
            foreach(var (nombre,ruta) in new[]{("deniscorp","/"),("academy","/deniscorp/programas/academy/"),("fulleo","/deniscorp/programas/fulleo/"),("pos","/deniscorp/pos/"),("colegios","/deniscorp/programas/colegios/")})
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
                    Check(Regex.Matches(html,"data-product=\"").Count==6,"seis tarjetas "+idioma);
                    Check(html.Contains("brand-glass")&&html.Contains("/images/zuni-logo-transparent.png"),"logo ZUNI sobre cristal "+idioma);
                    Check(Regex.Matches(html,"class=\"([^\"]*)\"").Count(m=>m.Groups[1].Value.Split(' ').Contains("brand-glass"))==8,"cristal en las seis tarjetas, cabecera y hero "+idioma);
                    Check(!texto.Contains("LSR")&&!html.Contains("colegios.png"),"el cliente LSR no representa al SaaS "+idioma);
                    Check(html.Contains("/deniscorp/blog/")&&html.Contains("/deniscorp/contacto/"),"blog y contacto en navegación "+idioma);
                    Check(texto.Contains(idioma=="es"?"Colegios y gestión escolar":"Schools and administration"),"catálogo traducido "+idioma);
                }
                else
                {
                    Check(texto.Contains(nombre=="colegios"?(idioma=="es"?"Propuesta para colegios":"School service offering"):(idioma=="es"?"Avances del programa":"Product progress"))&&texto.Contains(PortalDenisCorp.Obtener(nombre).Version),"avances con versión documentada "+nombre+idioma);
                    Check(texto.Contains(idioma=="es"?"En preparación":"In preparation"),"pendientes distinguidos de lo implementado "+nombre+idioma);
                }
                if(nombre=="fulleo")Check(texto.Contains("0.9.1")&&texto.Contains("19")&&texto.Contains("88")&&texto.Contains(idioma=="es"?"datos históricos":"historical test data"),"avances de Fulleo no simulan precios actuales "+idioma);
                if(salida is not null)await File.WriteAllTextAsync(Path.Combine(salida,nombre+"-"+idioma+".html"),html);
            }
            foreach(var nombre in new[]{"blog","contacto"})
            {
                var html=await anon.GetStringAsync("/deniscorp/"+nombre+"/");var texto=WebUtility.HtmlDecode(html);
                Check(html.Contains("lang=\""+idioma+"\"")&&html.Contains("data-view-scope=\"denis-corp\""),"nuevas páginas bilingües y con tema "+nombre+idioma);
                Check(!texto.Contains("LSR")&&!html.Contains("localhost:3000"),"páginas propias sin identidad de cliente ni dependencia de otro localhost "+nombre+idioma);
                if(nombre=="blog")
                {
                    Check(Regex.Matches(html,"data-blog-article").Count==8&&html.Contains("<details>"),"biblioteca de ocho guías desplegables "+idioma);
                    var filtrado=await anon.GetStringAsync("/deniscorp/blog/?tema=fulleo");
                    Check(Regex.Matches(filtrado,"data-blog-article[^>]*hidden").Count==7,"filtro por programa también funciona en servidor "+idioma);
                    var buscado=await anon.GetStringAsync("/deniscorp/blog/?q="+(idioma=="es"?"recuperacion":"recovery"));
                    Check(Regex.Matches(buscado,"data-blog-article[^>]*hidden").Count==7,"búsqueda tolera acentos "+idioma);
                    Check((await anon.GetAsync("/deniscorp/blog/?q="+Uri.EscapeDataString(new string('a',79)+"😀"))).StatusCode==HttpStatusCode.OK,"búsqueda larga con Unicode no produce error "+idioma);
                    var inyeccion=await anon.GetStringAsync("/deniscorp/blog/?q="+Uri.EscapeDataString("<script>alert(1)</script>"));
                    Check(!inyeccion.Contains("<script>alert(1)</script>"),"consulta no se interpreta como HTML "+idioma);
                }
                else
                {
                    Check(texto.Contains("+502 3039 0175")&&texto.Contains("corporation.denis@gmail.com"),"canales corporativos de la referencia "+idioma);
                    Check(!html.Contains("name=\"contact-message\"")&&!html.Contains("name=\"contact-name\""),"el borrador no se serializa al servidor "+idioma);
                    var escolar=await anon.GetStringAsync("/deniscorp/contacto/?programa=colegios&tema=contratacion");
                    Check(Regex.IsMatch(escolar,"value=\"colegios\" selected")&&Regex.IsMatch(escolar,"value=\"contratacion\" selected"),"consulta de colegio conserva programa y tema "+idioma);
                    Check((await anon.PostAsync("/deniscorp/contacto/",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.MethodNotAllowed,"no existe un envío silencioso al servidor "+idioma);
                }
                if(salida is not null)await File.WriteAllTextAsync(Path.Combine(salida,nombre+"-"+idioma+".html"),html);
            }
        }
        Check((await anon.GetAsync("/deniscorp/")).StatusCode==HttpStatusCode.OK,"alias del portal conservado");
        var zuni=await anon.GetStringAsync("/deniscorp/zuni/");
        if(salida is not null)await File.WriteAllTextAsync(Path.Combine(salida,"zuni.html"),zuni);
        Check(Regex.IsMatch(zuni,"rel=\"icon\"[^>]*href=\"/favicon-96.png"),"ZUNI mantiene su favicon: "+string.Join(";",Regex.Matches(zuni,"<link[^>]*rel=\"icon\"[^>]*>").Select(m=>m.Value)));
        await app.StopAsync();Console.WriteLine($"OK: {checks} comprobaciones HTTP de portal, idiomas, avances e identidades.");
    }
}
