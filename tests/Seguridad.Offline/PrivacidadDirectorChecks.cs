using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;

// Correcciones posteriores a la revisión de cambios: privacidad del Director, avisos antes de
// cuestionarios y coherencia de la configuración de despliegue. No usa PostgreSQL.
internal static class PrivacidadDirectorChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj)
    {
        int checks = 0; void Check(bool ok, string label) { if (!ok) throw new Exception("Privacidad Director: " + label); checks++; }
        const string nombre = "Estudiante Confidencial Carga", correo = "carga-privada@miumg.edu.gt", carne = "8888267777";
        var perfil = new PerfilEstudiante { UsuarioId = "carga-est", Carne = carne, Carrera = "Carrera privada", Telefono = "12345678" };
        var hoy = AgendaReglas.Hoy(reloj.Ahora.UtcDateTime);
        var dia = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(9);
        var cita = new Cita
        {
            PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "psi3", FechaHoraInicioUtc = AgendaReglas.Utc(dia, new(15, 0)),
            FechaHoraFinUtc = AgendaReglas.Utc(dia, new(15, 50)), Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.Confirmada,
            DireccionAtencion = "DIRECCION-CITA-PRIVADA", ProgramadaDirectamente = true, ProgramadorUsuarioId = "psi3", ProgramadorRol = "Psicologo",
            FechaCreacionUtc = reloj.Ahora.UtcDateTime
        };
        await using (var db = new OfflineDb(options))
        {
            var u = new ApplicationUser { Id = "carga-est", FullName = nombre, Email = correo, NormalizedEmail = correo.ToUpperInvariant(), UserName = correo,
                NormalizedUserName = correo.ToUpperInvariant(), IsActive = true, SecurityStamp = "carga-est-stamp" };
            db.Users.Add(u); db.UserRoles.Add(new() { UserId = u.Id, RoleId = "Estudiante" });
            db.PerfilesEstudiante.Add(perfil); db.Citas.Add(cita);
            db.Citas.Add(new Cita { PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "psi3", FechaHoraInicioUtc = AgendaReglas.Utc(dia.AddDays(1), new(15, 0)),
                FechaHoraFinUtc = AgendaReglas.Utc(dia.AddDays(1), new(15, 50)), Modalidad = ModalidadCita.Virtual, Estado = EstadoCita.Cancelada,
                MotivoCancelacion = "MOTIVO-CANCELACION-PRIVADO", ProgramadaDirectamente = true, ProgramadorUsuarioId = "psi3", ProgramadorRol = "Psicologo", FechaCreacionUtc = reloj.Ahora.UtcDateTime });
            await db.SaveChangesAsync();
        }
        using var director = HostPrueba.Cliente(app); using var estudiante = HostPrueba.Cliente(app); using var psy = HostPrueba.Cliente(app);
        await HostPrueba.Login(director, "director@miumg.edu.gt", clave); await HostPrueba.Login(estudiante, "est2@miumg.edu.gt", clave);
        await HostPrueba.Login(psy, "psi3@miumg.edu.gt", clave);

        // --- Calendario del Director: bloques anónimos.
        var carga = WebUtility.HtmlDecode(await director.GetStringAsync("/Director/Calendario?psicologoId=psi3&fecha=" + dia.ToString("yyyy-MM-dd")));
        var prohibidos = new[] { nombre, correo, carne, "8888-26-7777", perfil.Id.ToString(), cita.Id.ToString(), "carga-est", "DIRECCION-CITA-PRIVADA", "MOTIVO-CANCELACION-PRIVADO", "Carrera privada" };
        Check(prohibidos.All(p => !carga.Contains(p, StringComparison.OrdinalIgnoreCase)), "calendario sin nombre, carné, correo, carrera, lugar ni identificadores");
        Check(carga.Contains("data-director-bloques") && Regex.IsMatch(carga, "15:00–15:50</strong> · Ocupado"), "el bloque ocupado sigue visible con hora y estado");
        Check(!carga.Contains("/CitasProgramadas/Editar") && !carga.Contains("Proponer cita") && !carga.Contains("Reprogramar cita"), "sin programación individual desde el calendario");
        Check(carga.Contains("id=\"metricas-carga-director\"") && Regex.IsMatch(carga, "data-metrica=\"ocupados\">[1-9]") && Regex.IsMatch(carga, "data-metrica=\"canceladas\">[1-9]"),
            "métricas agregadas del mes disponibles");
        Check((await director.GetAsync("/Director/Calendario?psicologoId=carga-est")).StatusCode == HttpStatusCode.Redirect, "un estudiante no puede usarse como profesional en la URL");

        // --- Rutas directas: ninguna devuelve citas individuales ni fichas al Director.
        // La ficha usa ruta por atributo "Psicologo/Expediente/{solicitudId:guid}" (no acepta ?solicitudId=).
        var plantillaFicha = typeof(Zuni.Controllers.PsicologoController).GetMethod("Expediente")!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), false).Cast<Microsoft.AspNetCore.Mvc.HttpGetAttribute>().Single().Template;
        Check(plantillaFicha == "Psicologo/Expediente/{solicitudId:guid}", "la prueba usa la ruta real de la ficha");
        Guid solicitudConFicha;
        await using (var db = new OfflineDb(options))
            solicitudConFicha = await db.ExpedientesIniciales.AsNoTracking().Select(e => e.SolicitudAtencionId).FirstAsync();
        foreach (var ruta in new[] { "/CitasProgramadas", "/CitasProgramadas?citaId=" + cita.Id, "/CitasProgramadas/Editar", "/CitasProgramadas/Editar?citaId=" + cita.Id,
                     "/CitasProgramadas/Notificaciones", "/Psicologo/Agenda?citaId=" + cita.Id,
                     "/Psicologo/Expediente/" + solicitudConFicha, "/Psicologo/Expediente/" + solicitudConFicha + "/BigFive" })
        {
            var respuesta = await director.GetAsync(ruta);
            // Única denegación aceptada: la de la autorización de la app (Forbid -> /Cuenta/AccesoDenegado).
            Check(respuesta.StatusCode == HttpStatusCode.Redirect
                  && respuesta.Headers.Location?.OriginalString.Contains("/Cuenta/AccesoDenegado") == true
                  && !(await respuesta.Content.ReadAsStringAsync()).Contains("MOTIVO-CLINICO-PRIVADO"), "ruta directa denegada: " + ruta);
        }
        var guardar = await HostPrueba.Post(director, "/Director", "/CitasProgramadas/Guardar", ("CitaId", cita.Id.ToString()), ("Revision", cita.Revision.ToString()),
            ("OperacionId", Guid.NewGuid().ToString()), ("PerfilEstudianteId", perfil.Id.ToString()), ("Fecha", dia.AddDays(2).ToString("yyyy-MM-dd")),
            ("HoraInicio", "10:00"), ("DuracionMinutos", "50"), ("Modalidad", "Presencial"));
        Check(guardar.StatusCode == HttpStatusCode.Redirect, "programación individual por Director bloqueada en el servidor");
        await using (var db = new OfflineDb(options))
            Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == cita.Id)).Revision == cita.Revision, "el intento del Director no cambia la cita");
        var panel = WebUtility.HtmlDecode(await director.GetStringAsync("/Director"));
        Check(!panel.Contains("/CitasProgramadas") && panel.Contains("/Director/Calendario") && panel.Contains("Carga de psicólogos"), "menú del Director sin programación ni notificaciones de citas");

        // --- Estadísticas agregadas: conteos, sin listados identificables.
        var estadisticas = WebUtility.HtmlDecode(await director.GetStringAsync("/Director/Estadisticas"));
        Check(estadisticas.Contains("id=\"estadisticas-director\"") && Regex.IsMatch(estadisticas, "data-indicador=\"solicitudes\">\\d+") && estadisticas.Contains("Carga por psicólogo"),
            "estadísticas agregadas disponibles");
        Check(prohibidos.All(p => !estadisticas.Contains(p, StringComparison.OrdinalIgnoreCase)) && !estadisticas.Contains("Persona est2"), "estadísticas sin nombres, carnés ni correos de estudiantes");
        Check(estadisticas.Contains("menos de 5") || !estadisticas.Contains("Carrera privada"), "carreras con pocos estudiantes no muestran cifras exactas");
        Check((await estudiante.GetAsync("/Director/Estadisticas")).StatusCode == HttpStatusCode.Redirect, "estadísticas solo para Director");

        // --- El psicólogo conserva su programación y sus datos.
        var agendaPsy = WebUtility.HtmlDecode(await psy.GetStringAsync("/Psicologo/Agenda?fecha=" + dia.ToString("yyyy-MM-dd")));
        Check(agendaPsy.Contains(nombre), "el psicólogo asignado sigue viendo a su estudiante en su agenda");
        Check((await psy.GetAsync("/CitasProgramadas")).StatusCode == HttpStatusCode.OK && (await psy.GetAsync("/CitasProgramadas/Editar")).StatusCode == HttpStatusCode.OK,
            "programación del psicólogo intacta");

        // --- Aviso de privacidad antes de Big Five y cuestionarios.
        // Rutas reales: BigFiveController usa [Route("Estudiante/BigFive")] + [HttpGet("")];
        // PruebasAplicadasController.Index usa la ruta convencional /PruebasAplicadas.
        foreach (var ruta in new[] { "/Estudiante/BigFive", "/PruebasAplicadas" })
        {
            var respuestaAviso = await estudiante.GetAsync(ruta);
            Check(respuestaAviso.StatusCode == HttpStatusCode.OK, $"la pantalla del estudiante abre (200) en {ruta}; se obtuvo {(int)respuestaAviso.StatusCode}");
            var html = WebUtility.HtmlDecode(await respuestaAviso.Content.ReadAsStringAsync());
            Check(html.Contains("id=\"aviso-privacidad-actividad\"") && html.Contains("lugar tranquilo, privado y sin interrupciones")
                  && html.Contains("otras personas puedan observar tus respuestas"), "aviso de lugar tranquilo y privado en " + ruta);
            Check(html.Contains("no constituye un diagnóstico") && html.Contains("herramientas de apoyo y orientación psicoeducativa"), "texto de no diagnóstico en " + ruta);
        }

        // --- Despliegue: comprobaciones estáticas de los archivos versionados (sin ejecutar nada).
        var raiz = new DirectoryInfo(AppContext.BaseDirectory);
        while (raiz is not null && !Directory.Exists(Path.Combine(raiz.FullName, "deploy", "debian"))) raiz = raiz.Parent;
        Check(raiz is not null, "raíz del repositorio localizada");
        string Leer(params string[] partes) => File.ReadAllText(Path.Combine(new[] { raiz!.FullName }.Concat(partes).ToArray())).Replace("\r\n", "\n");
        var env = Leer("deploy", "debian", "institucion.env.example");
        var envMigracion = Leer("deploy", "debian", "institucion-migracion.env.example");
        var servicio = Leer("deploy", "debian", "zuni@.service");
        foreach (var variable in new[] { "ASPNETCORE_ENVIRONMENT=Production", "ConnectionStrings__DefaultConnection=", "Zuni__InstallationId=", "SeguridadClinica__ClavesPath=", "SeguridadClinica__BitacoraPath=", "DataProtection__KeysPath=" })
            Check(env.Contains("\n" + variable) || env.StartsWith(variable), "ejemplo de entorno incluye " + variable);
        string Valor(string texto, string nombreVariable) => Regex.Match(texto, "^" + Regex.Escape(nombreVariable) + "=(.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
        foreach (var clave2 in new[] { "Zuni__InstallationId", "SeguridadClinica__ClavesPath", "SeguridadClinica__BitacoraPath", "DataProtection__KeysPath" })
            Check(Valor(env, clave2) == Valor(envMigracion, clave2), "mantenimiento usa la misma " + clave2);
        var usuarioEjecucion = Regex.Match(env, "Username=([^;\"]+)").Groups[1].Value;
        var usuarioMigracion = Regex.Match(envMigracion, "Username=([^;\"]+)").Groups[1].Value;
        Check(usuarioEjecucion.Length > 0 && usuarioMigracion.Length > 0 && usuarioEjecucion != usuarioMigracion, "usuario de ejecución distinto del usuario de migración");
        var escribible = Regex.Match(servicio, "^ReadWritePaths=(.+)$", RegexOptions.Multiline).Groups[1].Value.Trim().Replace("%i", "institucion-a");
        foreach (var clave2 in new[] { "SeguridadClinica__ClavesPath", "SeguridadClinica__BitacoraPath", "DataProtection__KeysPath" })
            Check(Valor(env, clave2).StartsWith(escribible + "/"), "systemd permite escribir " + clave2);
        var guia = Leer("docs", "instalaciones-debian.md");
        Check(guia.Contains("usuario de migración") && guia.Contains("usuario de ejecución") && guia.Contains("--inicializar-seguridad") && guia.Contains("--migrar-seguridad")
              && guia.Contains("--verificar-seguridad") && !guia.Contains("/var/lib/zuni-private") && !guia.Contains("/var/lib/zuni-audit"), "guía con roles separados, orden de seguridad y rutas coherentes");
        Check(!Leer("docs", "proteccion-clinica-y-auditoria.md").Contains("/var/lib/zuni-private"), "guía de protección sin rutas fuera de ReadWritePaths");
        Check(Leer("deploy", "debian", "permisos-runtime.sql.example").Contains("REVOKE UPDATE, DELETE, TRUNCATE, TRIGGER"), "ejemplo de permisos mínimos del usuario de ejecución");
        foreach (var archivo in new[] { env, envMigracion }) Check(!Regex.IsMatch(archivo, "Password=(?!REEMPLAZAR)[^;\"]+"), "ejemplos sin contraseñas reales");

        Console.WriteLine($"OK: {checks} comprobaciones de privacidad del Director, avisos y despliegue.");
    }
}
