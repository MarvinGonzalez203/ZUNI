using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Atencion;
using Zuni.Models.MayoriaEdad;
using Zuni.Models.MiCuenta;
using Zuni.Services.MayoriaEdad;

internal static class MayoriaEdadChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj)
    {
        var checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception("Mayoría de edad: " + text); checks++; }
        var hoy = MayoriaEdadService.Hoy(reloj);
        var cumple18 = hoy.AddYears(-18);
        Check(MayoriaEdadService.ValidarNacimiento(cumple18.AddDays(1), hoy) is not null, "día anterior al cumpleaños 18");
        Check(MayoriaEdadService.ValidarNacimiento(cumple18, hoy) is null && MayoriaEdadService.Edad(cumple18, hoy) == 18, "día exacto del cumpleaños 18");
        Check(MayoriaEdadService.ValidarNacimiento(hoy.AddDays(1), hoy) is not null, "fecha futura");
        Check(MayoriaEdadService.ValidarNacimiento(hoy.AddYears(-121), hoy) is not null, "fecha fuera de rango");
        Check(MayoriaEdadService.Edad(new(2008, 2, 29), new(2026, 2, 27)) == 17 && MayoriaEdadService.Edad(new(2008, 2, 29), new(2026, 3, 1)) == 18, "nacimiento en año bisiesto");
        var datos = new DatosEstudianteViewModel();
        var errors = new List<ValidationResult>();
        Check(!Validator.TryValidateObject(datos, new(datos), errors, true) && errors.Any(e => e.MemberNames.Contains(nameof(datos.TelefonoContactoEmergencia))), "teléfono de emergencia obligatorio en servidor");
        datos.Telefono = "12345678"; datos.Carrera = "Ingeniería"; datos.NombreContactoEmergencia = "Contacto"; datos.RelacionContactoEmergencia = "Familiar"; datos.TelefonoContactoEmergencia = "87654321";
        Check(Validator.TryValidateObject(datos, new(datos), [], true), "datos personales y de emergencia completos");
        var solicitud = new SolicitarAtencionViewModel { Edad = 21, EsMayorEdad = true, AceptaConsentimiento = true, Direccion = "Dirección", IdiomaPreferido = "Español", MotivoConsulta = "Consulta", Contactos = [new() { Nombre = "Contacto", Relacion = "Familiar", Telefono = "12345678" }] };
        errors.Clear();
        Check(!Validator.TryValidateObject(solicitud, new(solicitud), errors, true) && errors.Any(e => e.MemberNames.Contains("Sexo")), "género obligatorio en servidor");
        solicitud.Sexo = "Prefiero no responder";
        Check(Validator.TryValidateObject(solicitud, new(solicitud), [], true), "opción válida sin forzar revelación de género");

        var perfil = new PerfilEstudiante { UsuarioId = "edad", Carne = "20990198", Carrera = "Ingeniería", Telefono = "12345678" };
        await using (var db = new OfflineDb(options))
        {
            var user = new ApplicationUser { Id = "edad", FullName = "Persona Edad", UserName = "edad@miumg.edu.gt", NormalizedUserName = "EDAD@MIUMG.EDU.GT", Email = "edad@miumg.edu.gt", NormalizedEmail = "EDAD@MIUMG.EDU.GT", IsActive = true, SecurityStamp = "edad-stamp" };
            user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, clave);
            db.Users.Add(user); db.UserRoles.Add(new IdentityUserRole<string> { UserId = "edad", RoleId = "Estudiante" });
            db.PerfilesEstudiante.Add(perfil); await db.SaveChangesAsync();
        }
        using var student = HostPrueba.Cliente(app);
        using var admin = HostPrueba.Cliente(app);
        using var director = HostPrueba.Cliente(app);
        await HostPrueba.Login(student, "edad@miumg.edu.gt", clave);
        await HostPrueba.Login(admin, "admin@miumg.edu.gt", clave);
        await HostPrueba.Login(director, "director@miumg.edu.gt", clave);
        var blocked = await student.GetAsync("/Estudiante/SolicitarAtencion");
        Check(blocked.StatusCode == HttpStatusCode.Redirect && blocked.Headers.Location!.OriginalString.Contains("MayoriaEdad"), "solicitud bloqueada sin verificación");
        var forced = await HostPrueba.Post(student, "/MayoriaEdad", "/Estudiante/SolicitarAtencion", ("EsMayorEdad", "true"), ("Edad", "99"));
        Check(forced.StatusCode == HttpStatusCode.Redirect && forced.Headers.Location!.OriginalString.Contains("MayoriaEdad"), "POST falsificado no evade verificación");
        Check((await student.PostAsync("/MayoriaEdad", new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "antiforgery en declaración");
        var minor = await HostPrueba.Post(student, "/MayoriaEdad", "/MayoriaEdad", ("FechaNacimiento", cumple18.AddDays(1).ToString("yyyy-MM-dd")), ("Revision", Guid.Empty.ToString()));
        Check(minor.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(minor)).Contains("18 años"), "menor rechazado por POST");
        await using (var db = new OfflineDb(options)) Check(!await db.VerificacionesMayoriaEdad.AnyAsync(v => v.PerfilEstudianteId == perfil.Id), "menor no crea constancia");
        var declare = await HostPrueba.Post(student, "/MayoriaEdad", "/MayoriaEdad", ("FechaNacimiento", cumple18.ToString("yyyy-MM-dd")), ("Revision", Guid.Empty.ToString()));
        Check(declare.StatusCode == HttpStatusCode.Redirect, "declaración válida enviada");
        Check((await HostPrueba.Texto(await student.GetAsync("/MayoriaEdad"))).Contains("Declarada, pendiente de verificación"), "declarada no se muestra como verificada");
        Check((await student.GetAsync("/Estudiante/SolicitarAtencion")).StatusCode == HttpStatusCode.Redirect, "declaración pendiente no habilita atención");
        var review = "/MayoriaEdad/Revisar/" + perfil.Id;
        Check((await student.GetAsync(review)).StatusCode == HttpStatusCode.Redirect && (await director.GetAsync(review)).StatusCode == HttpStatusCode.Redirect, "revisión limitada al Administrador");
        Guid revision;
        await using (var db = new OfflineDb(options))
        {
            var v = await db.VerificacionesMayoriaEdad.SingleAsync(v => v.PerfilEstudianteId == perfil.Id); revision = v.Revision;
            Check(v.Estado == EstadoMayoriaEdad.Pendiente && v.EdadDeclarada == 18 && v.ResponsableUsuarioId is null, "estado pendiente sin responsable inventado");
            Check(!db.Model.FindEntityType(typeof(VerificacionMayoriaEdad))!.GetProperties().Any(p => p.Name.Contains("Nacimiento") || p.Name.Contains("Cui") || p.Name.Contains("Foto")), "fecha de nacimiento y documento completo no se conservan");
        }
        async Task<HttpResponseMessage> Approve(Guid version, string birth, string confirm = "true") => await HostPrueba.Post(admin, review, "/MayoriaEdad/Revisar",
            ("PerfilEstudianteId", perfil.Id.ToString()), ("Revision", version.ToString()), ("Aprobada", "true"), ("Documento", "0"), ("FechaNacimiento", birth), ("DocumentoComprobado", confirm));
        Check((await Approve(revision, cumple18.AddDays(1).ToString("yyyy-MM-dd"))).StatusCode == HttpStatusCode.OK, "administrador no verifica menor");
        Check((await Approve(revision, cumple18.ToString("yyyy-MM-dd"), "false")).StatusCode == HttpStatusCode.OK, "aprobación exige revisión documental confirmada");
        Check((await Approve(revision, cumple18.ToString("yyyy-MM-dd"))).StatusCode == HttpStatusCode.Redirect, "aprobación válida");
        await using (var db = new OfflineDb(options))
        {
            var v = await db.VerificacionesMayoriaEdad.SingleAsync(v => v.PerfilEstudianteId == perfil.Id);
            Check(v.Estado == EstadoMayoriaEdad.Verificada && v.EdadVerificada == 18 && v.ResponsableUsuarioId == "admin" && v.FechaRevisionUtc.HasValue && v.DocumentoRevisado == DocumentoEdad.Dpi, "constancia con edad, documento, responsable y fecha");
            Check(await db.AuditoriaUsuarios.AnyAsync(a => a.UsuarioAfectadoId == "edad" && a.Accion == "MAYORIA_EDAD_VERIFICADA" && a.DatosNuevos == null), "auditoría sin fecha de nacimiento ni contenido del documento");
        }
        var necesitaPrivacidad = await student.GetAsync("/Estudiante/SolicitarAtencion");
        Check(necesitaPrivacidad.StatusCode == HttpStatusCode.Redirect && necesitaPrivacidad.Headers.Location!.OriginalString.Contains("Privacidad"), "verificación documental no sustituye aceptación de privacidad");
        await using (var db = new OfflineDb(options))
        { var policy = await Zuni.Services.Privacidad.PrivacidadService.VigenteAsync(db, default); await HostPrueba.Post(student, "/Privacidad", "/Privacidad/Aceptar", ("politicaId", policy!.Id.ToString()), ("confirmada", "true")); }
        Check((await student.GetAsync("/Estudiante/SolicitarAtencion")).StatusCode == HttpStatusCode.OK, "verificación y privacidad habilitan solicitud");
        await using (var db = new OfflineDb(options)) { var p = await db.PerfilesEstudiante.SingleAsync(p => p.Id == perfil.Id); p.Telefono = null; await db.SaveChangesAsync(); }
        var sinTelefono = await student.GetAsync("/Estudiante/SolicitarAtencion");
        Check(sinTelefono.StatusCode == HttpStatusCode.Redirect && sinTelefono.Headers.Location!.OriginalString.Contains("MiCuenta"), "solicitud exige teléfono personal válido");
        await using (var db = new OfflineDb(options)) { var p = await db.PerfilesEstudiante.SingleAsync(p => p.Id == perfil.Id); p.Telefono = "12345678"; await db.SaveChangesAsync(); }
        var nueva = await HostPrueba.Post(student, "/Estudiante/SolicitarAtencion", "/Estudiante/SolicitarAtencion",
            ("Edad", "99"), ("EsMayorEdad", "true"), ("AceptaConsentimiento", "true"), ("Sexo", "Prefiero no responder"),
            ("Direccion", "Zona 1"), ("IdiomaPreferido", "Español"), ("MotivoConsulta", "Consulta"), ("TipoIngreso", "0"),
            ("Contactos[0].Nombre", "Contacto"), ("Contactos[0].Relacion", "Familiar"), ("Contactos[0].Telefono", "12345678"));
        Check(nueva.StatusCode == HttpStatusCode.Redirect, "solicitud completa después de verificar");
        await using (var db = new OfflineDb(options)) Check(await db.ExpedientesIniciales.AnyAsync(e => e.Edad == 18 && db.SolicitudesAtencion.Any(s => s.Id == e.SolicitudAtencionId && s.PerfilEstudianteId == perfil.Id)), "edad enviada por el cliente no reemplaza la documentada");
        await HostPrueba.Post(admin, review, "/MayoriaEdad/Revisar", ("PerfilEstudianteId", perfil.Id.ToString()), ("Revision", revision.ToString()), ("Aprobada", "false"));
        await using (var db = new OfflineDb(options)) Check(await MayoriaEdadService.VerificadaAsync(db, perfil.Id, default), "revisión obsoleta no sobrescribe aprobación");
        await using (var db = new OfflineDb(options)) revision = (await db.VerificacionesMayoriaEdad.SingleAsync(v => v.PerfilEstudianteId == perfil.Id)).Revision;
        await HostPrueba.Post(admin, review, "/MayoriaEdad/Revisar", ("PerfilEstudianteId", perfil.Id.ToString()), ("Revision", revision.ToString()), ("Aprobada", "false"));
        Check((await student.GetAsync("/Estudiante/SolicitarAtencion")).StatusCode == HttpStatusCode.Redirect, "revisión rechazada vuelve a bloquear nuevas solicitudes");
        var preview = Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if (!string.IsNullOrEmpty(preview)) { Directory.CreateDirectory(preview); await File.WriteAllTextAsync(Path.Combine(preview, "edad.html"), await student.GetStringAsync("/MayoriaEdad")); await File.WriteAllTextAsync(Path.Combine(preview, "revisar-edad.html"), await admin.GetStringAsync(review)); }
        Console.WriteLine($"OK: {checks} comprobaciones de mayoría de edad y datos obligatorios.");
    }
}
