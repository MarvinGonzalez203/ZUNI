using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Institucion;
using Zuni.Services.Institucion;

internal static class InstitucionChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave)
    {
        var checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception("Institución: " + text); checks++; }
        OfflineDb Db() => new(options);
        var restricted = new ConfiguracionInstitucion { DominiosAutorizados = "colegio.edu.gt" };
        Check(PoliticaCorreoReglas.ValidarCorreo(restricted, " Persona@COLEGIO.EDU.GT ", true) is null, "normalización de correo y dominio exacto");
        foreach (var email in new[] { "a@colegio.edu.gt.evil.test", "a@sub.colegio.edu.gt", "a@otro.test", "a@@colegio.edu.gt", "Nombre <a@colegio.edu.gt>" })
            Check(PoliticaCorreoReglas.ValidarCorreo(restricted, email, true) is not null, "rechazar correo no permitido: " + email);
        foreach (var domains in new[] { "@colegio.edu.gt", "*.colegio.edu.gt", "https://colegio.edu.gt", "colegio..edu.gt", "colegio_edu.gt", string.Join(',', Enumerable.Repeat("a.test", 21)) })
            Check(!PoliticaCorreoReglas.TryNormalizarDominios(domains, out _), "rechazar dominios inválidos");
        Check(PoliticaCorreoReglas.TryNormalizarDominios(" COLEGIO.EDU.GT; otro.test\ncolegio.edu.gt", out var normalized) && normalized.SequenceEqual(new[] { "colegio.edu.gt", "otro.test" }), "normalizar lista y duplicados");
        var invalid = new ConfiguracionInstitucionViewModel { Nombre = "Colegio", PoliticaCorreo = PoliticaCorreoInstitucion.DominiosAutorizados };
        Check(!Validator.TryValidateObject(invalid, new(invalid), new List<ValidationResult>(), true), "lista vacía no permitida en modo restringido");
        await using (var bootstrapConnection = new SqliteConnection("Data Source=:memory:"))
        {
            await bootstrapConnection.OpenAsync();
            var bootstrapOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(bootstrapConnection).Options;
            await using var bootstrapDb = new OfflineDb(bootstrapOptions);
            await bootstrapDb.Database.EnsureCreatedAsync();
            await DbInitializer.SeedRolesAsync(bootstrapDb);
            using var provider = new ServiceCollection().AddSingleton<ApplicationDbContext>(bootstrapDb)
                .AddSingleton<IPasswordHasher<ApplicationUser>>(new PasswordHasher<ApplicationUser>()).BuildServiceProvider();
            using var output = new StringWriter();
            var code = await PrimerAdministrador.EjecutarAsync(provider, ["--correo", "operador@otra-institucion.test", "--nombre", "Operador Institucional"], output);
            var bootstrapUser = await bootstrapDb.Users.SingleAsync();
            Check(code == 0 && bootstrapUser.IsActive && bootstrapUser.DebeCambiarContrasena, "primer administrador admite institución diferente y exige cambio de contraseña");
            Check(await (from ur in bootstrapDb.UserRoles join r in bootstrapDb.Roles on ur.RoleId equals r.Id
                where ur.UserId == bootstrapUser.Id && r.NormalizedName == "ADMINISTRADOR" select ur).AnyAsync(), "primer administrador conserva la autenticación existente");
            Check(await PrimerAdministrador.EjecutarAsync(provider, ["--correo", "segundo@otra-institucion.test", "--nombre", "Otro Operador"], output) == 1 && await bootstrapDb.Users.CountAsync() == 1,
                "aprovisionamiento no crea otro administrador si ya existe uno activo");
            Check(await PrimerAdministrador.EjecutarAsync(provider, ["--correo", "correo-invalido", "--nombre", "Otro Operador"], output) == 2, "aprovisionamiento rechaza correo inválido");
        }
        await using (var db = Db())
        {
            var initial = await ConfiguracionInstitucionService.ObtenerAsync(db);
            Check(initial.DominiosAutorizados == "miumg.edu.gt" && initial.PoliticaCorreo == PoliticaCorreoInstitucion.DominiosAutorizados, "valor inicial conserva acceso de UMG");
        }

        using var admin = HostPrueba.Cliente(app);
        await HostPrueba.Login(admin, "admin@miumg.edu.gt", clave);
        var form = await admin.GetAsync("/Institucion");
        Check(form.StatusCode == HttpStatusCode.OK, "Administrador abre configuración");
        Check(form.Headers.CacheControl?.NoStore == true, "configuración privada no se almacena en caché");
        var raw = await form.Content.ReadAsStringAsync();
        Check(raw.Contains("name=\"Revision\"") && raw.Contains("DominiosAutorizados"), "formulario renderizado con revisión y dominios");
        var noToken = await admin.PostAsync("/Institucion", new FormUrlEncodedContent(new Dictionary<string, string> { ["Nombre"] = "No guardar" }));
        Check(noToken.StatusCode == HttpStatusCode.BadRequest, "antiforgery obligatorio");
        using (var student = HostPrueba.Cliente(app))
        {
            await HostPrueba.Login(student, "est2@miumg.edu.gt", clave);
            var denied = await student.GetAsync("/Institucion");
            Check(denied.StatusCode == HttpStatusCode.Redirect && denied.Headers.Location!.OriginalString.Contains("AccesoDenegado"), "Estudiante no accede a configuración");
            var deniedPost = await HostPrueba.Post(student, "/MiCuenta", "/Institucion",
                ("Nombre", "Cambio no autorizado"), ("PoliticaCorreo", "1"), ("Revision", Guid.Empty.ToString()));
            Check(deniedPost.StatusCode == HttpStatusCode.Redirect && deniedPost.Headers.Location!.OriginalString.Contains("AccesoDenegado"), "Estudiante tampoco modifica configuración por POST");
        }

        async Task<HttpResponseMessage> Save(int mode, string domains, Guid? revision = null)
        {
            await using var db = Db();
            var current = await ConfiguracionInstitucionService.ObtenerAsync(db);
            return await HostPrueba.Post(admin, "/Institucion", "/Institucion",
                ("Nombre", "Colegio <Aprender>"), ("CorreoSoporte", "soporte@colegio.test"),
                ("PoliticaCorreo", mode.ToString()), ("DominiosAutorizados", domains), ("Revision", (revision ?? current.Revision).ToString()));
        }
        var save = await Save(0, "COLEGIO.EDU.GT\notro.test");
        Check(save.StatusCode == HttpStatusCode.Redirect, "guardar política desde Administración");
        Guid firstRevision;
        await using (var db = Db())
        {
            var current = await ConfiguracionInstitucionService.ObtenerAsync(db);
            firstRevision = current.Revision;
            Check(current.Nombre == "Colegio <Aprender>" && current.DominiosAutorizados == "colegio.edu.gt\notro.test" && current.CorreoSoporte == "soporte@colegio.test", "persistencia y normalización");
            Check(await db.AuditoriaUsuarios.AnyAsync(x => x.Accion == "CONFIGURACION_INSTITUCION_CAMBIADA" && x.AdministradorId == "admin"), "auditoría de cambios");
        }
        using var publicClient = HostPrueba.Cliente(app);
        var registroRaw = await publicClient.GetStringAsync("/Cuenta/Registro");
        Check(registroRaw.Contains("Colegio &lt;Aprender&gt;") && !registroRaw.Contains("Colegio <Aprender>"), "nombre institucional escapado");
        Check(registroRaw.Contains("colegio.edu.gt") && registroRaw.Contains("otro.test"), "dominios visibles en registro");
        async Task<HttpResponseMessage> Register(string email, string code) => await HostPrueba.Post(publicClient, "/Cuenta/Registro", "/Cuenta/Registro",
            ("FullName", "Prueba Institucion"), ("Email", email), ("Password", clave), ("ConfirmPassword", clave),
            ("Telefono", "12345678"), ("CarneParte1", "2099"), ("CarneParte2", "01"), ("CarneParte3", code));
        var rejected = await Register("no@personal.test", "01");
        Check(rejected.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(rejected)).Contains("dominios autorizados"), "POST público rechaza dominio ajeno");
        Check((await Register("aceptado@colegio.edu.gt", "02")).StatusCode == HttpStatusCode.Redirect, "POST público acepta dominio de otra institución");
        await using (var db = Db())
        {
            Check(!await db.Users.AnyAsync(x => x.Email == "no@personal.test"), "correo rechazado no crea cuenta");
            var created = await db.Users.SingleAsync(x => x.Email == "aceptado@colegio.edu.gt");
            Check(!created.IsActive, "otro dominio conserva activación administrativa");
        }
        async Task<HttpResponseMessage> Add(string email) => await HostPrueba.Post(admin, "/Administrador/AgregarUsuario", "/Administrador/AgregarUsuario",
            ("NombreCompleto", "Cuenta Institucion"), ("Correo", email), ("Rol", "Director"),
            ("ContrasenaTemporal", clave), ("ConfirmarContrasenaTemporal", clave));
        Check((await Add("admin-rechazado@personal.test")).StatusCode == HttpStatusCode.OK, "altas administrativas también restringen dominio");
        Check((await Add("director@otro.test")).StatusCode == HttpStatusCode.Redirect, "altas administrativas aceptan segundo dominio");
        Check((await Save(1, "colegio.edu.gt")).StatusCode == HttpStatusCode.Redirect, "guardar cualquier correo");
        Check((await Register("personal@correo.test", "03")).StatusCode == HttpStatusCode.Redirect, "registro con correo personal");
        Check((await Add("director-personal@correo.test")).StatusCode == HttpStatusCode.Redirect, "alta con correo personal");
        await Save(0, "colegio.edu.gt", firstRevision);
        await using (var db = Db()) Check((await ConfiguracionInstitucionService.ObtenerAsync(db)).PoliticaCorreo == PoliticaCorreoInstitucion.CualquierCorreo, "revisión obsoleta no sobrescribe configuración");
        var invalidSave = await Save(0, "*.colegio.edu.gt");
        Check(invalidSave.StatusCode == HttpStatusCode.OK, "configuración inválida muestra errores");
        var beforeClose = await publicClient.GetStringAsync("/Cuenta/Registro");
        var staleToken = HostPrueba.Token(beforeClose);
        Check((await Save(2, "")).StatusCode == HttpStatusCode.Redirect, "cerrar registro público");
        var closed = await publicClient.GetStringAsync("/Cuenta/Registro");
        Check(closed.Contains("Solicitar una cuenta") && !closed.Contains("name=\"Password\""), "GET cerrado sin formulario de alta");
        var forged = await publicClient.PostAsync("/Cuenta/Registro", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = staleToken, ["FullName"] = "Alta obsoleta", ["Email"] = "cerrado@correo.test",
            ["Password"] = clave, ["ConfirmPassword"] = clave, ["Telefono"] = "12345678",
            ["CarneParte1"] = "2099", ["CarneParte2"] = "01", ["CarneParte3"] = "04"
        }));
        Check(forged.StatusCode == HttpStatusCode.Forbidden, "POST con formulario anterior tampoco abre registro cerrado");
        var loginRaw = await publicClient.GetStringAsync("/Cuenta/IniciarSesion");
        Check(!loginRaw.Contains("href=\"/Cuenta/Registro\"") && loginRaw.Contains("soporte@colegio.test"), "login muestra contacto y no ofrece registro cerrado");
        var homeRaw = await publicClient.GetStringAsync("/");
        Check(!homeRaw.Contains("href=\"/Cuenta/Registro\"") && homeRaw.Contains("Solicita tu cuenta"), "Inicio también respeta el cierre del registro");
        Check((await Add("director-administrado@personal.test")).StatusCode == HttpStatusCode.Redirect, "Administrador crea cuentas cuando el registro público está cerrado");
        await using (var db = Db())
        {
            Check(!await db.Users.AnyAsync(x => x.Email == "cerrado@correo.test"), "POST cerrado no escribe usuario");
            Check(await db.Users.AnyAsync(x => x.Id == "admin" && x.IsActive) && await db.UserRoles.AnyAsync(x => x.UserId == "admin" && x.RoleId == "Administrador"), "política nueva conserva administrador existente");
        }
        Check((await HostPrueba.Login(publicClient, "admin@miumg.edu.gt", clave)).StatusCode == HttpStatusCode.Redirect, "cuenta existente inicia sesión pese al cambio de dominios");
        // Restablecer la política para que las pruebas de sesión posteriores sean independientes.
        await Save(0, "miumg.edu.gt");
        Console.WriteLine($"OK: {checks} comprobaciones de institución y política de correo.");
    }
}
