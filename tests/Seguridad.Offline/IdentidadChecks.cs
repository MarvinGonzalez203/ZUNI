using System.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zuni.Data;
using Zuni.Models.Institucion;
using Zuni.Services.Institucion;
using Zuni.Seguridad;

internal static class IdentidadChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave)
    {
        int checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Identidad: " + label); checks++; }
        foreach (var color in new[] { "red", "#fff", "#000000;}</style><script>", "#gggggg" })
        {
            var vm = new ConfiguracionInstitucionViewModel { Nombre = "Colegio", ColorPrincipal = color, PoliticaCorreo = PoliticaCorreoInstitucion.CualquierCorreo };
            Check(!Validator.TryValidateObject(vm, new(vm), new List<ValidationResult>(), true), "color no válido rechazado");
        }
        Check(IdentidadInstitucion.Texto("#cea2fd") == "#000000" && IdentidadInstitucion.Texto("#000000") == "#ffffff", "contraste del texto adaptado");
        var a = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Zuni:InstallationId"] = "colegio-a" }).Build();
        var b = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Zuni:InstallationId"] = "colegio-b" }).Build();
        Check(AislamientoInstalacion.Nombre(a) != AislamientoInstalacion.Nombre(b) && AislamientoInstalacion.Cookie(a) != AislamientoInstalacion.Cookie(b), "claves y cookies separadas por instalación");
        Check(AislamientoInstalacion.Nombre(new ConfigurationBuilder().Build()) == "Zuni", "instalación existente conserva identidad criptográfica");
        using var admin = HostPrueba.Cliente(app); using var director = HostPrueba.Cliente(app); using var psy = HostPrueba.Cliente(app); using var anon = HostPrueba.Cliente(app);
        await HostPrueba.Login(admin, "admin@miumg.edu.gt", clave); await HostPrueba.Login(director, "director@miumg.edu.gt", clave); await HostPrueba.Login(psy, "psi3@miumg.edu.gt", clave);
        async Task<HttpResponseMessage> Save(string color, byte[]? file = null, bool remove = false)
        {
            await using var db = new OfflineDb(options); var current = await ConfiguracionInstitucionService.ObtenerAsync(db);
            using var data = new MultipartFormDataContent();
            foreach (var pair in new Dictionary<string,string> { ["Nombre"] = current.Nombre, ["Lema"] = "Acompañamiento <educativo>", ["ColorPrincipal"] = color, ["PoliticaCorreo"] = ((int)current.PoliticaCorreo).ToString(), ["DominiosAutorizados"] = current.DominiosAutorizados, ["Revision"] = current.Revision.ToString(), ["QuitarLogotipo"] = remove.ToString(), ["__RequestVerificationToken"] = HostPrueba.Token(await admin.GetStringAsync("/Institucion")) }) data.Add(new StringContent(pair.Value), pair.Key);
            if (file is not null) data.Add(new ByteArrayContent(file), "Logotipo", "archivo.png");
            return await admin.PostAsync("/Institucion", data);
        }
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        Check((await Save("#cea2fd", png)).StatusCode == HttpStatusCode.Redirect, "logotipo PNG válido guardado");
        var response = await anon.GetAsync("/Institucion/Logotipo");
        Check(response.StatusCode == HttpStatusCode.OK && response.Content.Headers.ContentType?.MediaType == "image/png" && (await response.Content.ReadAsByteArrayAsync()).SequenceEqual(png), "logo público sirve solamente imagen PNG");
        foreach (var bad in new[] { new byte[131073], System.Text.Encoding.UTF8.GetBytes("<svg><script>alert(1)</script></svg>"), png[..^4] })
            Check((await Save("#cea2fd", bad)).StatusCode == HttpStatusCode.OK, "archivo inválido no se guarda");
        Check((await Save("#000000")).StatusCode == HttpStatusCode.Redirect, "cambio de color sin eliminar logo");
        await using (var db = new OfflineDb(options)) Check((await ConfiguracionInstitucionService.ObtenerAsync(db)).LogotipoPng!.SequenceEqual(png), "logo conservado sin archivo nuevo");
        var directorHtml = await director.GetStringAsync("/PruebasDirector"); var psyHtml = await psy.GetStringAsync("/Psicologo/Agenda");
        Check(directorHtml.Contains("--institucion-color: #000000") && psyHtml.Contains("--institucion-color: #000000") && directorHtml.Contains("--institucion-texto: #ffffff"), "ambos paneles usan la misma identidad visual");
        Check(directorHtml.Contains("Acompa&#xF1;amiento &lt;educativo&gt;") || directorHtml.Contains("Acompañamiento &lt;educativo&gt;"), "lema escapado");
        Check((await Save("#cea2fd", remove: true)).StatusCode == HttpStatusCode.Redirect && (await anon.GetAsync("/Institucion/Logotipo")).StatusCode == HttpStatusCode.NotFound, "retiro explícito del logo");
        var preview = Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if (!string.IsNullOrEmpty(preview)) { await File.WriteAllTextAsync(Path.Combine(preview, "director-color.html"), await director.GetStringAsync("/PruebasDirector")); await File.WriteAllTextAsync(Path.Combine(preview, "psicologo-color.html"), await psy.GetStringAsync("/Psicologo/Agenda")); }
        Console.WriteLine($"OK: {checks} comprobaciones de identidad y aislamiento.");
    }
}
