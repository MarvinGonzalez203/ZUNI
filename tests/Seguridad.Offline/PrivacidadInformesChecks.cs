using System.Net;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Helpers;
using Zuni.Models.Atencion;
using Zuni.Models.Informes;
using Zuni.Models.Privacidad;

internal static class PrivacidadInformesChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj, Guid perfil)
    {
        var checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception("Privacidad/informes: " + text); checks++; }
        OfflineDb Db() => new(options);
        foreach (var ultimo in new[] { "25", "025", "0025", "00025", "125678" })
        { Check(CarneHelper.TryConstruir("1590", "18", ultimo, out var carne) && CarneHelper.Formatear(carne) == "1590-18-" + ultimo, "último bloque de " + ultimo.Length + " dígitos y ceros iniciales"); }
        foreach (var ultimo in new[] { "2", "1234567", "25a", "２５" }) Check(!CarneHelper.TryConstruir("1590", "18", ultimo, out _), "rechazar carné fuera de rango o no numérico");
        var model = new SolicitarAtencionViewModel();
        foreach (var value in new[] { "Mujer", "Hombre", "No binario", "Otros", "Prefiero no responder" })
        { model.Sexo = value; Check(Validator.TryValidateProperty(value, new(model) { MemberName = "Sexo" }, []), "género válido: " + value); }
        using var admin = HostPrueba.Cliente(app); using var psi = HostPrueba.Cliente(app); using var student = HostPrueba.Cliente(app);
        using var director = HostPrueba.Cliente(app); using var otro = HostPrueba.Cliente(app); using var anon = HostPrueba.Cliente(app); using var otroDirector = HostPrueba.Cliente(app);
        await HostPrueba.Login(admin, "admin@miumg.edu.gt", clave); await HostPrueba.Login(psi, "psi3@miumg.edu.gt", clave);
        await HostPrueba.Login(student, "est2@miumg.edu.gt", clave); await HostPrueba.Login(director, "director@miumg.edu.gt", clave); await HostPrueba.Login(otro, "est@miumg.edu.gt", clave);
        await HostPrueba.Login(otroDirector, "psi-director@miumg.edu.gt", clave);
        Check((await student.GetAsync("/Privacidad/Configurar")).StatusCode == HttpStatusCode.Redirect, "estudiante no publica política");
        Check((await director.GetAsync("/Privacidad/Solicitudes")).StatusCode == HttpStatusCode.Redirect, "Director no recibe trámite administrativo de otras personas");
        Check((await admin.PostAsync("/Privacidad/Configurar", new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "antiforgery de política");
        Guid anterior;
        await using (var db = Db()) anterior = (await db.PoliticasPrivacidad.SingleAsync()).Id;
        (string, string)[] Policy(bool confirm, Guid? old) => [ ("VersionAnteriorId", old?.ToString() ?? ""), ("Responsable", "Responsable de prueba"), ("Contacto", "datos@offline.test"),
            ("Finalidades", "Atención educativa e informes autorizados <script>"), ("DatosYAccesos", "Acceso por rol y destinatario. Sin notas privadas para administración."),
            ("Conservacion", "Plazo de prueba; revisión institucional antes de eliminar."), ("ProcedimientoDerechos", "Verificar identidad, responder y entregar por un canal seguro."), ("Confirmada", confirm.ToString()) ];
        await HostPrueba.Post(admin, "/Privacidad/Configurar", "/Privacidad/Configurar", Policy(false, anterior));
        await using (var db = Db()) Check(await db.PoliticasPrivacidad.CountAsync() == 1, "publicación exige aprobación institucional");
        await HostPrueba.Post(admin, "/Privacidad/Configurar", "/Privacidad/Configurar", Policy(true, anterior));
        Guid current;
        await using (var db = Db()) { var p = await db.PoliticasPrivacidad.OrderByDescending(x => x.Version).FirstAsync(); current = p.Id; Check(p.Version == 2 && await db.PoliticasPrivacidad.CountAsync() == 2, "versiones de política inmutables"); }
        var policyRaw = await anon.GetStringAsync("/Privacidad/Politica");
        Check(policyRaw.Contains("&lt;script&gt;") && !policyRaw.Contains("autorizados <script>"), "política pública codificada contra XSS");
        await HostPrueba.Post(admin, "/Privacidad/Configurar", "/Privacidad/Configurar", Policy(true, anterior));
        await using (var db = Db()) Check(await db.PoliticasPrivacidad.CountAsync() == 2, "versión obsoleta no sobrescribe política");
        await HostPrueba.Post(student, "/Privacidad", "/Privacidad/Aceptar", ("politicaId", anterior.ToString()), ("confirmada", "true"));
        await using (var db = Db()) Check(!await db.AceptacionesPrivacidad.AnyAsync(a => a.PoliticaId == current && a.UsuarioId == "est2"), "aceptación anterior no sustituye nueva política");
        await HostPrueba.Post(student, "/Privacidad", "/Privacidad/Aceptar", ("politicaId", current.ToString()), ("confirmada", "true"));
        await using (var db = Db()) Check(await db.AceptacionesPrivacidad.AnyAsync(a => a.PoliticaId == current && a.UsuarioId == "est2"), "aceptación vigente con fecha");
        await HostPrueba.Post(student, "/Privacidad", "/Privacidad/Solicitar", ("Nueva.Tipo", "1"), ("Nueva.Detalle", "CORRECCION-PRIVADA-DE-PRUEBA"));
        Guid derecho; Guid revDerecho;
        await using (var db = Db()) { var s = await db.SolicitudesDerechosDatos.SingleAsync(); derecho = s.Id; revDerecho = s.Revision; }
        Check(!(await otro.GetStringAsync("/Privacidad")).Contains("CORRECCION-PRIVADA-DE-PRUEBA"), "solicitudes personales aisladas entre cuentas");
        Check((await student.GetAsync("/Privacidad/Responder/" + derecho)).StatusCode == HttpStatusCode.Redirect, "estudiante no responde como administrador");
        await HostPrueba.Post(admin, "/Privacidad/Responder/" + derecho, "/Privacidad/Responder", ("Id", derecho.ToString()), ("Revision", revDerecho.ToString()), ("Respuesta", "Procedimiento y canal seguro acordados con el solicitante."));
        Check((await student.GetStringAsync("/Privacidad")).Contains("canal seguro acordados"), "respuesta visible al solicitante");
        await using (var db = Db()) Check((await db.SolicitudesDerechosDatos.SingleAsync()).Estado == EstadoDerechoDatos.Respondida, "respuesta, responsable y revisión registrados");
        await HostPrueba.Post(admin, "/Privacidad/Responder/" + derecho, "/Privacidad/Responder", ("Id", derecho.ToString()), ("Revision", revDerecho.ToString()), ("Respuesta", "Sobrescritura"));
        await using (var db = Db()) Check(!(await db.SolicitudesDerechosDatos.SingleAsync()).Respuesta!.Contains("Sobrescritura"), "respuesta obsoleta no modifica historia");

        using var publico = HostPrueba.Cliente(app);
        foreach (var ultimo in new[] { "25", "125", "1225", "12225", "125678" })
        {
            var response = await HostPrueba.Post(publico, "/Cuenta/Registro", "/Cuenta/Registro", ("FullName", "Carné de prueba"), ("Email", "carne" + ultimo + "@miumg.edu.gt"),
                ("CarneParte1", "7490"), ("CarneParte2", "26"), ("CarneParte3", ultimo), ("Telefono", "12345678"), ("Password", clave), ("ConfirmPassword", clave));
            Check(response.StatusCode == HttpStatusCode.Redirect, "registro real admite último bloque de " + ultimo.Length);
        }
        var rechazo = await HostPrueba.Post(publico, "/Cuenta/Registro", "/Cuenta/Registro", ("FullName", "Sin consentimiento"), ("Email", "noprivacidad@miumg.edu.gt"),
            ("CarneParte1", "7490"), ("CarneParte2", "26"), ("CarneParte3", "99"), ("Telefono", "12345678"), ("Password", clave), ("ConfirmPassword", clave), ("AceptaPrivacidad", "false"));
        Check(rechazo.StatusCode == HttpStatusCode.OK, "registro exige aceptación explícita");
        Guid solicitud;
        await using (var db = Db()) solicitud = await db.SolicitudesAtencion.Where(s => s.PerfilEstudianteId == perfil).Select(s => s.Id).SingleAsync();
        var crear = "/InformesEducativos/Crear?solicitudId=" + solicitud;
        Check((await psi.GetAsync(crear)).StatusCode == HttpStatusCode.OK, "editor del psicólogo autorizado");
        Check((await director.GetAsync(crear)).StatusCode == HttpStatusCode.Redirect && (await admin.GetAsync(crear)).StatusCode == HttpStatusCode.Redirect, "roles institucionales no redactan ni abren contenido clínico");
        Check((await psi.PostAsync("/InformesEducativos/Guardar", new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "antiforgery de informes");
        var editorPreview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(editorPreview))await File.WriteAllTextAsync(Path.Combine(editorPreview,"informe-editor-rendimiento.html"),await psi.GetStringAsync(crear));
        var hoy = Zuni.Services.MayoriaEdad.MayoriaEdadService.Hoy(reloj);
        (string, string)[] Content(Guid? id = null, Guid? revision = null, bool request = false) => [ ("Id", id?.ToString() ?? ""), ("Revision", (revision ?? Guid.Empty).ToString()), ("SolicitudAtencionId", solicitud.ToString()),
            ("Objetivo", "Coordinar apoyo educativo <script>"), ("PeriodoInicio", hoy.AddDays(-10).ToString("yyyy-MM-dd")), ("PeriodoFin", hoy.ToString("yyyy-MM-dd")),
            ("IncluyeAnalisisRendimiento","false"),("Fuentes", "Entrevista y observaciones autorizadas."), ("Observaciones", "Dificultad observada para organizar las tareas académicas."), ("Necesidades", "Acompañamiento en la planificación."),
            ("Recomendaciones", "Acordar un calendario de entregas y revisar su avance."), ("ResponsablesAcciones", "Autoridad educativa designada y estudiante."),
            ("FechaSeguimiento", hoy.AddDays(28).ToString("yyyy-MM-dd")), ("DestinatarioUsuarioId", "director"), ("solicitar", request.ToString()) ];
        await HostPrueba.Post(psi, crear, "/InformesEducativos/Guardar", Content());
        Guid version;
        await using (var db = Db()) { var v = await db.VersionesInformesEducativos.SingleAsync(); version = v.Id; Check(v.Estado == EstadoInforme.Borrador, "borrador privado"); }
        var detalle = "/InformesEducativos/Detalle/" + version;
        var editor = "/InformesEducativos/Editar/" + version;
        Check((await director.GetAsync(detalle)).StatusCode == HttpStatusCode.Redirect && (await student.GetAsync(detalle)).StatusCode == HttpStatusCode.Redirect, "borrador invisible al Director y estudiante");
        async Task<VersionInformeEducativo> Version(Guid id) { await using var db = Db(); return await db.VersionesInformesEducativos.AsNoTracking().SingleAsync(v => v.Id == id); }
        var v1 = await Version(version);
        (string,string)[] Analisis(Guid id,Guid revision,bool confirmado=true)=>Content(id,revision,true).Where(x=>x.Item1!="IncluyeAnalisisRendimiento").Concat(new[]{("IncluyeAnalisisRendimiento","true"),("RelatoRendimiento","El estudiante refiere dificultad para organizar entregas."),("ValoracionRendimiento","Se contrastó el relato con observaciones educativas autorizadas; se propone apoyo en planificación, sin establecer causalidad clínica."),("ContrasteRendimientoConfirmado",confirmado.ToString())}).ToArray();
        var incompleto=await HostPrueba.Post(psi,editor,"/InformesEducativos/Guardar",Analisis(version,v1.Revision,false));
        Check(WebUtility.HtmlDecode(await incompleto.Content.ReadAsStringAsync()).Contains("confirmación del contraste") && (await Version(version)).Estado==EstadoInforme.Borrador,"análisis sin revisión profesional no solicita autorización");
        await HostPrueba.Post(psi,editor,"/InformesEducativos/Guardar",Content(version,v1.Revision,true).Where(x=>x.Item1!="IncluyeAnalisisRendimiento").ToArray());
        Check((await Version(version)).Estado==EstadoInforme.Borrador,"solicitar autorización exige decidir si amerita análisis");
        await HostPrueba.Post(psi, editor, "/InformesEducativos/Guardar", Analisis(version, v1.Revision));
        v1 = await Version(version);
        Check(v1.Estado == EstadoInforme.PendienteAutorizacion, "texto cerrado pendiente de autorización");
        Check((await director.GetAsync(detalle)).StatusCode == HttpStatusCode.Redirect, "Director no recibe informes pendientes");
        Check((await otro.GetAsync(detalle)).StatusCode == HttpStatusCode.Redirect, "otro estudiante no abre autorización");
        var raw = await student.GetStringAsync(detalle); Check(raw.Contains("&lt;script&gt;") && !raw.Contains("educativo <script>"), "informe codificado contra XSS");
        Check(!raw.Contains("MOTIVO-CLINICO-PRIVADO") && !raw.Contains("CONTACTO-PRIVADO") && !raw.Contains("DIRECCION-PRIVADA"), "informe no copia expediente automáticamente");
        await HostPrueba.Post(psi, detalle, "/InformesEducativos/Emitir", ("id", version.ToString()), ("revision", v1.Revision.ToString()), ("confirmada", "true"));
        Check((await Version(version)).Estado == EstadoInforme.PendienteAutorizacion, "no emite sin consentimiento");
        var editForjado = await HostPrueba.Post(psi, detalle, "/InformesEducativos/Guardar", Content(version, v1.Revision));
        Check(editForjado.StatusCode == HttpStatusCode.OK && (await Version(version)).Revision == v1.Revision, "versión solicitada no editable");
        await HostPrueba.Post(student, detalle, "/InformesEducativos/Decidir", ("id", version.ToString()), ("revision", v1.Revision.ToString()), ("aceptada", "true"), ("confirmada", "true"));
        v1 = await Version(version); Check(v1.Estado == EstadoInforme.Autorizada, "estudiante autoriza versión y destinatario");
        Check((await director.GetAsync("/InformesEducativos/Pdf/" + version)).StatusCode == HttpStatusCode.Redirect, "autorizada pero no emitida no se descarga");
        await HostPrueba.Post(psi, detalle, "/InformesEducativos/Emitir", ("id", version.ToString()), ("revision", v1.Revision.ToString()), ("confirmada", "false"));
        Check((await Version(version)).Estado == EstadoInforme.Autorizada, "emisión exige revisión profesional confirmada");
        await HostPrueba.Post(psi, detalle, "/InformesEducativos/Emitir", ("id", version.ToString()), ("revision", v1.Revision.ToString()), ("confirmada", "true"));
        v1 = await Version(version); Check(v1.Estado == EstadoInforme.Emitida && v1.FechaEmisionUtc.HasValue, "emisión registrada");
        var pdf = await director.GetAsync("/InformesEducativos/Pdf/" + version);
        Check(pdf.StatusCode == HttpStatusCode.OK && pdf.Content.Headers.ContentType?.MediaType == "application/pdf" && pdf.Headers.CacheControl?.NoStore == true, "PDF privado y sin caché para destinatario exacto");
        var bytes = await pdf.Content.ReadAsByteArrayAsync(); Check(bytes.Length > 1000 && System.Text.Encoding.ASCII.GetString(bytes[..5]) == "%PDF-", "PDF generado con fuente incluida");
        foreach (var cliente in new[] { otro, admin, anon, otroDirector }) Check((await cliente.GetAsync("/InformesEducativos/Pdf/" + version)).StatusCode == HttpStatusCode.Redirect, "PDF denegado a cuenta no autorizada");
        Check(v1.IncluyeAnalisisRendimiento==true && v1.ContrasteRendimientoConfirmado && v1.RelatoRendimiento.Contains("El estudiante refiere") && v1.ValoracionRendimiento.Contains("Se contrastó"),"versión autorizada conserva relato y valoración por separado");
        var previewAnalisis=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(previewAnalisis)){
            await File.WriteAllBytesAsync(Path.Combine(previewAnalisis,"informe-rendimiento-prueba.pdf"),bytes);
            var extenso=await Version(version);extenso.RelatoRendimiento=string.Join(' ',Enumerable.Repeat("El estudiante refiere dificultades de planificación de tareas académicas.",35))[..2000];extenso.ValoracionRendimiento=string.Join(' ',Enumerable.Repeat("El profesional contrasta la información educativa disponible y reconoce sus límites, sin afirmar causas clínicas.",25))[..2000];
            await File.WriteAllBytesAsync(Path.Combine(previewAnalisis,"informe-rendimiento-largo.pdf"),Zuni.Services.Informes.InformePdf.Crear(extenso));
            await File.WriteAllTextAsync(Path.Combine(previewAnalisis,"informe-rendimiento.html"),await psi.GetStringAsync(detalle));
        }
        var referenciaJson = System.Text.Json.JsonSerializer.Serialize(version);
        await using (var db = Db()) { var eventos=await db.AuditoriaUsuarios.AsNoTracking().ToListAsync(); Check(eventos.Any(a => a.Accion == "INFORME_PDF_DESCARGADO" && a.DatosNuevos == referenciaJson) && !eventos.Any(a => a.DatosNuevos != null && a.DatosNuevos.Contains("Dificultad observada")), "descarga auditada sin contenido educativo o clínico"); }
        await HostPrueba.Post(psi, detalle, "/InformesEducativos/NuevaVersion", ("id", version.ToString()), ("revision", v1.Revision.ToString()));
        Guid segunda;
        await using (var db = Db()) segunda = await db.VersionesInformesEducativos.Where(v => v.Numero == 2).Select(v => v.Id).SingleAsync();
        var d2 = "/InformesEducativos/Detalle/" + segunda;
        var v2 = await Version(segunda);
        Check(!v2.ContrasteRendimientoConfirmado,"una nueva versión requiere reafirmar el contraste");
        await HostPrueba.Post(psi, "/InformesEducativos/Editar/" + segunda, "/InformesEducativos/Guardar", Content(segunda, v2.Revision, true));
        v2 = await Version(segunda);
        await HostPrueba.Post(student, d2, "/InformesEducativos/Decidir", ("id", segunda.ToString()), ("revision", v2.Revision.ToString()), ("aceptada", "true"), ("confirmada", "true"));
        v2 = await Version(segunda);
        await HostPrueba.Post(psi, d2, "/InformesEducativos/Emitir", ("id", segunda.ToString()), ("revision", v2.Revision.ToString()), ("confirmada", "true"));
        v2 = await Version(segunda);
        Check(v2.Estado == EstadoInforme.Emitida && v2.SustituyeNumero == 1 && (await Version(version)).Estado == EstadoInforme.Sustituida, "nueva emisión sustituye la anterior sin sobrescribir contenido");
        Check((await director.GetAsync("/InformesEducativos/Pdf/" + version)).StatusCode == HttpStatusCode.Redirect, "versión sustituida no se entrega como vigente");
        string directorRole;
        await using(var db=Db()){directorRole=await db.Roles.Where(r=>r.NormalizedName=="DIRECTOR").Select(r=>r.Id).SingleAsync();var role=await db.UserRoles.SingleAsync(r=>r.UserId=="director"&&r.RoleId==directorRole);db.UserRoles.Remove(role);await db.SaveChangesAsync();}
        Check((await psi.GetAsync("/InformesEducativos/Pdf/"+segunda)).StatusCode==HttpStatusCode.Redirect,"psicólogo no descarga para un destinatario que perdió el rol Director");
        Check(WebUtility.HtmlDecode(await psi.GetStringAsync(d2)).Contains("El destinatario ya no está disponible"),"historial explica retiro del destinatario");
        await using(var db=Db()){db.UserRoles.Add(new(){UserId="director",RoleId=directorRole});var u=await db.Users.FindAsync("director");u!.IsActive=false;await db.SaveChangesAsync();}
        Check((await psi.GetAsync("/InformesEducativos/Pdf/"+segunda)).StatusCode==HttpStatusCode.Redirect,"cuenta Directora inactiva tampoco admite nuevas descargas");
        await using(var db=Db()){(await db.Users.FindAsync("director"))!.IsActive=true;await db.SaveChangesAsync();}
        var preview = Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if (!string.IsNullOrEmpty(preview))
        {
            Directory.CreateDirectory(preview); var actual = await director.GetAsync("/InformesEducativos/Pdf/" + segunda);
            await File.WriteAllBytesAsync(Path.Combine(preview, "informe-educativo-prueba.pdf"), await actual.Content.ReadAsByteArrayAsync());
            await File.WriteAllTextAsync(Path.Combine(preview, "informe.html"), await director.GetStringAsync(d2));
            await File.WriteAllTextAsync(Path.Combine(preview, "privacidad.html"), await student.GetStringAsync("/Privacidad"));
            var largo = await Version(segunda);
            largo.Observaciones = string.Join(' ', Enumerable.Repeat("Observación educativa: organización, participación y planificación de tareas.", 35));
            largo.Recomendaciones = string.Join(' ', Enumerable.Repeat("Acordar actividades y revisar el avance con respeto a la privacidad del estudiante.", 35));
            await File.WriteAllBytesAsync(Path.Combine(preview, "informe-educativo-largo.pdf"), Zuni.Services.Informes.InformePdf.Crear(largo));
        }
        await HostPrueba.Post(student, d2, "/InformesEducativos/Revocar", ("id", segunda.ToString()), ("revision", v2.Revision.ToString()), ("confirmada", "true"));
        Check((await Version(segunda)).Estado == EstadoInforme.Revocada && (await director.GetAsync("/InformesEducativos/Pdf/" + segunda)).StatusCode == HttpStatusCode.Redirect, "revocación impide futuras descargas");
        Check((await director.GetStringAsync("/InformesEducativos")).Contains("No hay informes disponibles"), "Director no lista informes revocados");
        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:"))
        {
            await connection.OpenAsync();
            var otraDb = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            await using (var db = new OfflineDb(otraDb)) await db.Database.EnsureCreatedAsync();
            await using var otroHost = await HostPrueba.Crear(otraDb, reloj, new HasherContador(), loginPorMinuto: 1000);
            var cookies = new CookieContainer();
            using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = cookies };
            using var origen = new HttpClient(handler) { BaseAddress = new Uri(app.Urls.Single()) };
            await HostPrueba.Login(origen, "director@miumg.edu.gt", clave);
            using var destino = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(otroHost.Urls.Single()) };
            destino.DefaultRequestHeaders.Add("Cookie", cookies.GetCookieHeader(origen.BaseAddress));
            var cross = await destino.GetAsync("/InformesEducativos/Pdf/" + segunda);
            Check(cross.StatusCode == HttpStatusCode.Redirect && cross.Headers.Location!.OriginalString.Contains("IniciarSesion"), "otra instalación no acepta la sesión ni entrega un PDF de origen");
            await using var aislada = new OfflineDb(otraDb);
            Check(!await aislada.InformesEducativos.AnyAsync() && !await aislada.PoliticasPrivacidad.AnyAsync(), "otra instalación no comparte documentos ni políticas");
        }
        await HostPrueba.Post(psi, d2, "/InformesEducativos/NuevaVersion", ("id", segunda.ToString()), ("revision", (await Version(segunda)).Revision.ToString()));
        Guid tercera; await using (var db = Db()) tercera = await db.VersionesInformesEducativos.Where(v => v.Numero == 3).Select(v => v.Id).SingleAsync();
        var v3 = await Version(tercera); var d3 = "/InformesEducativos/Detalle/" + tercera;
        await HostPrueba.Post(psi, "/InformesEducativos/Editar/" + tercera, "/InformesEducativos/Guardar", Content(tercera, v3.Revision, true));
        v3 = await Version(tercera);
        await HostPrueba.Post(student, d3, "/InformesEducativos/Decidir", ("id", tercera.ToString()), ("revision", v3.Revision.ToString()), ("aceptada", "false"), ("confirmada", "true"));
        Check((await Version(tercera)).Estado == EstadoInforme.Rechazada, "rechazo registrado");
        await using (var db = Db()) Check(await db.SolicitudesAtencion.AnyAsync(s => s.Id == solicitud && s.Estado == EstadoSolicitudAtencion.Asignada), "rechazar informe no cancela atención");
        await HostPrueba.Post(psi,d3,"/InformesEducativos/NuevaVersion",("id",tercera.ToString()),("revision",(await Version(tercera)).Revision.ToString()));
        Guid cuarta;await using(var db=Db())cuarta=await db.VersionesInformesEducativos.Where(v=>v.Numero==4).Select(v=>v.Id).SingleAsync();
        await HostPrueba.Post(psi,"/InformesEducativos/Editar/"+cuarta,"/InformesEducativos/Guardar",Content(cuarta,(await Version(cuarta)).Revision,true));
        await using(var db=Db()){db.UserRoles.Remove(await db.UserRoles.SingleAsync(r=>r.UserId=="director"&&r.RoleId==directorRole));await db.SaveChangesAsync();}
        Check((await psi.GetStringAsync("/InformesEducativos/Detalle/"+cuarta)).Contains("Preparar una nueva"),"puede reemplazar preparación destinada a Director retirado");
        await HostPrueba.Post(psi,"/InformesEducativos/Detalle/"+cuarta,"/InformesEducativos/NuevaVersion",("id",cuarta.ToString()),("revision",(await Version(cuarta)).Revision.ToString()));
        await using(var db=Db()){Check((await db.VersionesInformesEducativos.FindAsync(cuarta))!.Estado==EstadoInforme.Sustituida && await db.VersionesInformesEducativos.AnyAsync(v=>v.Numero==5&&v.Estado==EstadoInforme.Borrador),"retarget crea borrador separado y conserva autorización anterior sin falsificar decisión");db.UserRoles.Add(new(){UserId="director",RoleId=directorRole});await db.SaveChangesAsync();}
        Console.WriteLine($"OK: {checks} comprobaciones de privacidad, informes, carné y género.");
    }
}
