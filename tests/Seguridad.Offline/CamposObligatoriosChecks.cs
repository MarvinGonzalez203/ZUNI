using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;

internal static class CamposObligatoriosChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Guid perfil)
    {
        var checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception("Campos obligatorios: " + text); checks++; }
        string Label(string html, string campo) => Regex.Match(html, "<label\\b[^>]*for=\"" + Regex.Escape(campo) + "\"[^>]*>[\\s\\S]*?</label>").Value;
        bool Marca(string html, string campo) => Label(html, campo).Contains("class=\"required-marker\"");
        async Task<string> Read(HttpClient client, string route, string name)
        {
            var html = await client.GetStringAsync(route);
            Check(html.Contains("required-fields-help") && html.Contains("Campo obligatorio"), "leyenda en " + name);
            var path = Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
            if (!string.IsNullOrEmpty(path)) { Directory.CreateDirectory(path); await File.WriteAllTextAsync(Path.Combine(path, name + ".html"), html); }
            return html;
        }
        using var anonimo = HostPrueba.Cliente(app);
        var login = await Read(anonimo, "/Cuenta/IniciarSesion", "login");
        Check(Marca(login, "Email") && Marca(login, "Password"), "correo y contraseña marcados");
        Check(!Marca(login, "RememberMe"), "recordar sesión sigue opcional");
        var registro = await Read(anonimo, "/Cuenta/Registro", "registro");
        Check(Marca(registro, "FullName") && Marca(registro, "Telefono") && registro.Contains("Carné<span class=\"required-marker\""), "datos obligatorios y carné agrupado");
        Check(!registro.Contains("zuni-required="), "atributo de servidor no se filtra al HTML");
        using var admin = HostPrueba.Cliente(app);
        await HostPrueba.Login(admin, "admin@miumg.edu.gt", clave);
        var institucion = await Read(admin, "/Institucion", "institucion");
        Check(Marca(institucion, "Nombre") && Marca(institucion, "PoliticaCorreo") && Marca(institucion, "DominiosAutorizados"), "modo restringido exige dominios");
        Check(!Marca(institucion, "CorreoSoporte"), "soporte sigue opcional");
        var agregar = await Read(admin, "/Administrador/AgregarUsuario", "agregar");
        Check(Marca(agregar, "NombreCompleto") && Marca(agregar, "ContrasenaTemporal") && !Marca(agregar, "Motivo"), "alta administrativa distingue motivo opcional");
        using var director = HostPrueba.Cliente(app);
        await HostPrueba.Login(director, "director@miumg.edu.gt", clave);
        var editor = await Read(director, "/PruebasDirector/Crear", "editor");
        Check(Marca(editor, "Nombre") && editor.Contains("Nombre de dimensión<span class=\"required-marker\""), "plantillas dinámicas ya contienen indicadores");

        // Cuenta de prueba aislada para no alterar solicitudes usadas por los otros checks.
        await using (var db = new OfflineDb(options))
        {
            var user = new ApplicationUser { Id = "campos", Email = "campos@miumg.edu.gt", NormalizedEmail = "CAMPOS@MIUMG.EDU.GT", UserName = "campos@miumg.edu.gt", FullName = "Campos Formulario", IsActive = true, SecurityStamp = Guid.NewGuid().ToString() };
            user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, clave);
            db.Users.Add(user);
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = "Estudiante" });
            db.PerfilesEstudiante.Add(new PerfilEstudiante { UsuarioId = user.Id, Carne = "20990199", Telefono = "12345678" });
            await db.SaveChangesAsync();
        }
        using var estudiante = HostPrueba.Cliente(app);
        await HostPrueba.Login(estudiante, "campos@miumg.edu.gt", clave);
        var solicitud = await Read(estudiante, "/Estudiante/SolicitarAtencion", "solicitud");
        Check(Marca(solicitud, "Edad") && Marca(solicitud, "AceptaConsentimiento") && Marca(solicitud, "EsMayorEdad"), "edad y confirmaciones obligatorias");
        Check(!Marca(solicitud, "Sexo") && !Marca(solicitud, "NombreReferente") && !Marca(solicitud, "Contactos_0__Nombre"), "inicio sin referencia y contactos vacíos no se marcan individualmente");
        var invalid = await HostPrueba.Post(estudiante, "/Estudiante/SolicitarAtencion", "/Estudiante/SolicitarAtencion",
            ("Edad", "21"), ("EsMayorEdad", "true"), ("AceptaConsentimiento", "true"), ("Direccion", "Zona 1"),
            ("IdiomaPreferido", "Español"), ("MotivoConsulta", "Consulta"), ("TipoIngreso", "1"),
            ("Contactos[0].Nombre", ""), ("Contactos[0].Relacion", ""), ("Contactos[0].Telefono", ""), ("Contactos[1].Nombre", "Contacto"));
        var invalidHtml = await invalid.Content.ReadAsStringAsync();
        Check(invalid.StatusCode == HttpStatusCode.OK && Marca(invalidHtml, "NombreReferente") && Marca(invalidHtml, "MotivoReferencia"), "referencia enviada se marca tras error sin JavaScript");
        Check(Marca(invalidHtml, "Contactos_1__Nombre") && Marca(invalidHtml, "Contactos_1__Relacion") && !Marca(invalidHtml, "Contactos_0__Nombre"), "segundo contacto iniciado exige sus datos, primero vacío sigue permitido");

        var citaId = Guid.NewGuid();
        await using (var db = new OfflineDb(options))
        {
            var horario = new DisponibilidadPsicologo { PsicologoUsuarioId = "psi3", HoraInicio = new(8, 0), HoraFin = new(12, 0), DuracionMinutos = 50 };
            db.DisponibilidadesPsicologo.Add(horario);
            db.Citas.Add(new Cita { Id = citaId, PerfilEstudianteId = perfil, PsicologoUsuarioId = "psi3", DisponibilidadPsicologoId = horario.Id,
                FechaHoraInicioUtc = DateTime.UtcNow.AddHours(-2), FechaHoraFinUtc = DateTime.UtcNow.AddHours(-1), Estado = EstadoCita.Confirmada });
            await db.SaveChangesAsync();
        }
        using var psicologo = HostPrueba.Cliente(app);
        await HostPrueba.Login(psicologo, "psi3@miumg.edu.gt", clave);
        var atencion = await Read(psicologo, "/Psicologo/RegistrarAtencion?citaId=" + citaId, "atencion");
        Check(Marca(atencion, "Asistio") && !Marca(atencion, "ResenaRecomendacionesEstudiante"), "asistencia aún no seleccionada no exige resultados");
        var agenda = await Read(psicologo, "/Psicologo/Agenda", "agenda");
        Check(Marca(agenda, "inicio") && Marca(agenda, "fin"), "horas obligatorias en agenda");
        Console.WriteLine($"OK: {checks} comprobaciones de campos obligatorios.");
    }
}
