using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zuni.Controllers;
using Zuni.Data;
using Zuni.Helpers;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Seguridad;
using Zuni.Services;
using Zuni.Services.Administracion;

// Comprobaciones offline del endurecimiento previo al despliegue.
// SQLite en memoria + host HTTP efímero en 127.0.0.1. No abre ni modifica PostgreSQL.
var checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception("FALLA: " + label); checks++; }
async Task Rechaza<T>(Func<Task> accion, string label) where T : Exception
{
    try { await accion(); } catch (T) { checks++; return; }
    throw new Exception("FALLA (se esperaba " + typeof(T).Name + "): " + label);
}

await using var conexion = new SqliteConnection("Data Source=:memory:");
await conexion.OpenAsync();
var opciones = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conexion).Options;
OfflineDb Nuevo() => new(opciones);
var reloj = new Reloj();
var hasher = new HasherContador();
var ct = CancellationToken.None;
// Contraseña de prueba generada en cada ejecución: no es un secreto real ni se guarda.
var clave = "Prueba-" + Guid.NewGuid().ToString("N")[..10] + "!";

await using (var db = Nuevo())
{
    await db.Database.EnsureCreatedAsync();
    foreach (var rol in new[] { "Administrador", "Director", "Psicologo", "Catedratico", "Estudiante" })
        db.Roles.Add(new IdentityRole(rol) { Id = rol, NormalizedName = rol.ToUpperInvariant() });
    await db.SaveChangesAsync();
}

async Task<string> Usuario(string id, bool activo, bool debeCambiar, params string[] roles)
{
    await using var db = Nuevo();
    var correo = id + "@miumg.edu.gt";
    var u = new ApplicationUser
    {
        Id = id, FullName = "Persona " + id, Email = correo, NormalizedEmail = correo.ToUpperInvariant(),
        UserName = correo, NormalizedUserName = correo.ToUpperInvariant(), IsActive = activo,
        DebeCambiarContrasena = debeCambiar, SecurityStamp = id + "-stamp", ConcurrencyStamp = Guid.NewGuid().ToString()
    };
    u.PasswordHash = hasher.HashPassword(u, clave);
    db.Users.Add(u);
    foreach (var rol in roles) db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = rol });
    await db.SaveChangesAsync();
    return correo;
}

async Task<Guid> Perfil(string usuarioId, string carne)
{
    await using var db = Nuevo();
    var p = new PerfilEstudiante { UsuarioId = usuarioId, Carne = carne, Carrera = "Ingeniería", Telefono = "12345678" };
    db.PerfilesEstudiante.Add(p);
    await db.SaveChangesAsync();
    return p.Id;
}

async Task<Guid> Solicitud(Guid perfilId)
{
    await using var db = Nuevo();
    var s = new SolicitudAtencion { PerfilEstudianteId = perfilId, FechaSolicitudUtc = reloj.Ahora.UtcDateTime };
    var e = new ExpedienteInicial
    {
        SolicitudAtencionId = s.Id, Edad = 21, EsMayorEdad = true, Direccion = "DIRECCION-PRIVADA",
        IdiomaPreferido = "Español", MotivoConsulta = "MOTIVO-CLINICO-PRIVADO"
    };
    db.SolicitudesAtencion.Add(s);
    db.ConsentimientosAtencion.Add(new ConsentimientoAtencion { SolicitudAtencionId = s.Id, Aceptado = true, VersionConsentimiento = "v1" });
    db.ExpedientesIniciales.Add(e);
    db.ContactosEmergenciaExpediente.Add(new ContactoEmergenciaExpediente
    { ExpedienteInicialId = e.Id, Nombre = "CONTACTO-PRIVADO", Relacion = "Madre", Telefono = "87654321", Orden = 1 });
    await db.SaveChangesAsync();
    return s.Id;
}

async Task<(EstadoSolicitudAtencion Estado, int Vigentes, int Total, string? PsicologoVigente)> Estado(Guid solicitudId)
{
    await using var db = Nuevo();
    var s = await db.SolicitudesAtencion.AsNoTracking().SingleAsync(x => x.Id == solicitudId);
    var asignaciones = await db.AsignacionesEstudiantePsicologo.AsNoTracking().Where(a => a.PerfilEstudianteId == s.PerfilEstudianteId).ToListAsync();
    return (s.Estado, asignaciones.Count(a => a.FechaFinalizacionUtc == null), asignaciones.Count,
        asignaciones.SingleOrDefault(a => a.FechaFinalizacionUtc == null)?.PsicologoUsuarioId);
}

// ------------------------------------------------------------------ datos
var correoAdmin = await Usuario("admin", true, false, "Administrador");
await Usuario("director", true, false, "Director");
await Usuario("admin-inactivo", false, false, "Administrador");
await Usuario("psi1", true, false, "Psicologo");
await Usuario("psi2", true, false, "Psicologo");
await Usuario("psi3", true, false, "Psicologo");
await Usuario("psi-inactivo", false, false, "Psicologo");
await Usuario("psi-director", true, false, "Psicologo", "Director");
var correoEst = await Usuario("est", true, false, "Estudiante");
await Usuario("est2", true, false, "Estudiante");
var perfil = await Perfil("est", "202001001");
var perfil2 = await Perfil("est2", "202001002");
var solicitud = await Solicitud(perfil);
var solicitud2 = await Solicitud(perfil2);

// ================================================================== A1: asignaciones
await using (var db = Nuevo())
{
    // Con más de un psicólogo elegible la asignación automática no decide.
    var automatico = await new AsignacionPsicologoService(db).IntentarAsignarAsync(perfil, ct);
    Check(automatico == ResultadoAsignacion.PendienteMultiplesPsicologos, "Con >1 psicólogo la automática deja la solicitud pendiente");
}
Check((await Estado(solicitud)).Estado == EstadoSolicitudAtencion.Pendiente, "Solicitud sigue Pendiente");

GestionSolicitudesService Gestion(ApplicationDbContext db) => new(db, reloj);

await using (var db = Nuevo())
{
    var lista = await Gestion(db).Listar("admin", EstadoSolicitudAtencion.Pendiente, null, 1, ct);
    Check(lista.Solicitudes.Any(s => s.SolicitudId == solicitud) && lista.Pendientes == 2, "Listado de pendientes");
    var porCarne = await Gestion(db).Listar("admin", null, "2020-01-001", 1, ct);
    Check(porCarne.Solicitudes.Count == 1 && porCarne.Solicitudes[0].SolicitudId == solicitud, "Búsqueda por carné con guiones");
    var detalle = await Gestion(db).Detalle("admin", solicitud, ct);
    Check(detalle.Elegibles.Select(e => e.Id).Order().SequenceEqual(new[] { "psi1", "psi2", "psi3" }),
        "Elegibles: activos, rol Psicologo y sin roles institucionales (excluye inactivo y Psicologo+Director)");
    var propiedades = typeof(Zuni.Models.Administrador.SolicitudAdminFila).GetProperties().Select(p => p.Name)
        .Concat(typeof(Zuni.Models.Administrador.DetalleSolicitudAdminViewModel).GetProperties().Select(p => p.Name)).ToArray();
    Check(!propiedades.Any(p => p.Contains("Motivo") || p.Contains("Expediente") || p.Contains("Contacto") || p.Contains("Direccion")),
        "Modelos administrativos sin campos clínicos");
}

await Rechaza<GestionSolicitudesAccesoException>(async () => { await using var db = Nuevo(); await Gestion(db).Listar("psi1", null, null, 1, ct); }, "No Administrador no lista");
await Rechaza<GestionSolicitudesAccesoException>(async () => { await using var db = Nuevo(); await Gestion(db).Asignar("admin-inactivo", solicitud, "psi1", null, ct); }, "Administrador inactivo no asigna");
foreach (var noElegible in new[] { "psi-inactivo", "psi-director", "est2", "no-existe", "" })
    await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Asignar("admin", solicitud, noElegible, null, ct); }, "Psicólogo no elegible rechazado: " + noElegible);
Check((await Estado(solicitud)).Total == 0, "Rechazos no crearon asignaciones");

await using (var db = Nuevo()) await Gestion(db).Asignar("admin", solicitud, "psi1", "Asignación manual", ct);
var e1 = await Estado(solicitud);
Check(e1.Estado == EstadoSolicitudAtencion.Asignada && e1.Vigentes == 1 && e1.PsicologoVigente == "psi1", "Asignación manual: vigente y solicitud Asignada");
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Asignar("admin", solicitud, "psi2", null, ct); }, "No se asigna dos veces una solicitud asignada");

// El índice filtrado impide dos asignaciones vigentes aunque se intente sin el servicio.
await Rechaza<DbUpdateException>(async () =>
{
    await using var db = Nuevo();
    db.AsignacionesEstudiantePsicologo.Add(new AsignacionEstudiantePsicologo { PerfilEstudianteId = perfil, PsicologoUsuarioId = "psi2" });
    await db.SaveChangesAsync();
}, "Índice único impide duplicar asignación vigente");

await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Reasignar("admin", solicitud, "psi1", null, ct); }, "Reasignar al mismo psicólogo rechazado");
reloj.Ahora = reloj.Ahora.AddMinutes(1);
await using (var db = Nuevo()) await Gestion(db).Reasignar("admin", solicitud, "psi2", null, ct);
var e2 = await Estado(solicitud);
Check(e2.Estado == EstadoSolicitudAtencion.Asignada && e2.Vigentes == 1 && e2.Total == 2 && e2.PsicologoVigente == "psi2", "Reasignación: anterior finalizada, nueva vigente");

// Citas activas bloquean reasignar, finalizar y cerrar.
Guid citaId;
await using (var db = Nuevo())
{
    var disponibilidad = new DisponibilidadPsicologo
    {
        PsicologoUsuarioId = "psi2", DiaSemana = DayOfWeek.Monday, HoraInicio = new(9, 0), HoraFin = new(12, 0), DuracionMinutos = 30
    };
    var cita = new Cita
    {
        PerfilEstudianteId = perfil, PsicologoUsuarioId = "psi2", DisponibilidadPsicologoId = disponibilidad.Id,
        FechaHoraInicioUtc = reloj.Ahora.UtcDateTime.AddDays(2), FechaHoraFinUtc = reloj.Ahora.UtcDateTime.AddDays(2).AddMinutes(30),
        Estado = EstadoCita.Confirmada
    };
    db.DisponibilidadesPsicologo.Add(disponibilidad);
    db.Citas.Add(cita);
    await db.SaveChangesAsync();
    citaId = cita.Id;
}
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Reasignar("admin", solicitud, "psi3", null, ct); }, "Cita activa bloquea reasignar");
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).FinalizarAsignacion("admin", solicitud, null, ct); }, "Cita activa bloquea finalizar");
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Cerrar("admin", solicitud, null, true, ct); }, "Cita activa bloquea cerrar");
Check((await Estado(solicitud)).PsicologoVigente == "psi2", "Operaciones bloqueadas no cambiaron la asignación (rollback)");
await using (var db = Nuevo())
{
    var cita = await db.Citas.SingleAsync(c => c.Id == citaId);
    cita.Estado = EstadoCita.Cancelada;
    cita.MotivoCancelacion = "Prueba";
    await db.SaveChangesAsync();
}

reloj.Ahora = reloj.Ahora.AddMinutes(1);
await using (var db = Nuevo()) await Gestion(db).FinalizarAsignacion("admin", solicitud, null, ct);
var e3 = await Estado(solicitud);
Check(e3.Estado == EstadoSolicitudAtencion.Pendiente && e3.Vigentes == 0 && e3.Total == 2, "Finalizar asignación: vuelve a Pendiente y conserva historial");

await using (var db = Nuevo())
{
    var detalle = await Gestion(db).Detalle("admin", solicitud, ct);
    Check(detalle.Historial.Count == 2 && detalle.Historial.All(h => h.FechaFinalizacionUtc != null), "Historial de asignaciones visible y completo");
}

await using (var db = Nuevo()) await Gestion(db).Asignar("admin", solicitud, "psi1", null, ct);
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Cerrar("admin", solicitud, null, false, ct); }, "Cerrar exige confirmación");
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Cerrar("admin", solicitud, new string('x', 501), true, ct); }, "Motivo > 500 rechazado");
Check((await Estado(solicitud)).Estado == EstadoSolicitudAtencion.Asignada, "Cierre inválido no cambió el estado");
reloj.Ahora = reloj.Ahora.AddMinutes(1);
await using (var db = Nuevo()) await Gestion(db).Cerrar("admin", solicitud, "Atención concluida", true, ct);
var e4 = await Estado(solicitud);
Check(e4.Estado == EstadoSolicitudAtencion.Cerrada && e4.Vigentes == 0 && e4.Total == 3, "Cerrar: finaliza asignación vigente y queda Cerrada");

await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Asignar("admin", solicitud, "psi1", null, ct); }, "Cerrada no se asigna");
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Reasignar("admin", solicitud, "psi1", null, ct); }, "Cerrada no se reasigna");
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).FinalizarAsignacion("admin", solicitud, null, ct); }, "Cerrada no vuelve a Pendiente");
await Rechaza<GestionSolicitudesException>(async () => { await using var db = Nuevo(); await Gestion(db).Cerrar("admin", solicitud, null, true, ct); }, "Cerrada no se cierra dos veces");
Check((await Estado(solicitud)).Estado == EstadoSolicitudAtencion.Cerrada, "Solicitud cerrada permanece cerrada");

await using (var db = Nuevo())
{
    var auditoria = await db.AuditoriaUsuarios.AsNoTracking().Where(a => a.UsuarioAfectadoId == "est").ToListAsync();
    var acciones = auditoria.Select(a => a.Accion).ToArray();
    Check(acciones.Count(a => a == AccionesAuditoria.SolicitudAsignada) == 2 && acciones.Contains(AccionesAuditoria.SolicitudReasignada) &&
          acciones.Contains(AccionesAuditoria.AsignacionFinalizada) && acciones.Contains(AccionesAuditoria.SolicitudCerrada) &&
          auditoria.All(a => a.AdministradorId == "admin"), "Auditoría administrativa de asignar/reasignar/finalizar/cerrar");
    var texto = string.Join("|", auditoria.Select(a => a.DatosAnteriores + a.DatosNuevos + a.Motivo));
    Check(!texto.Contains("MOTIVO-CLINICO-PRIVADO") && !texto.Contains("CONTACTO-PRIVADO") && !texto.Contains("DIRECCION-PRIVADA"), "Auditoría sin contenido clínico");
    var cerrada = (await Gestion(db).Listar("admin", EstadoSolicitudAtencion.Cerrada, null, 1, ct)).Solicitudes.Single(s => s.SolicitudId == solicitud);
    Check(cerrada.Psicologo is null && cerrada.Estado == EstadoSolicitudAtencion.Cerrada, "Cerrada visible en el historial administrativo");
}

// ================================================================== HTTP
await using var app = await HostPrueba.Crear(opciones, reloj, hasher, loginPorMinuto: 1000);

// --- Cabeceras de seguridad
using (var anonimo = HostPrueba.Cliente(app))
{
    var r = await anonimo.GetAsync("/Cuenta/IniciarSesion");
    Check(r.StatusCode == HttpStatusCode.OK, "GET login responde");
    Check(r.Headers.TryGetValues("X-Content-Type-Options", out var nosniff) && nosniff.Single() == "nosniff", "nosniff");
    Check(r.Headers.TryGetValues("Referrer-Policy", out var referrer) && referrer.Single() == "strict-origin-when-cross-origin", "Referrer-Policy");
    var csp = r.Headers.TryGetValues("Content-Security-Policy", out var cspValores) ? cspValores.Single() : "";
    Check(csp.Contains("frame-ancestors 'self'") && csp.Contains("object-src 'none'") && csp.Contains("base-uri 'self'"), "CSP mínima");
    Check(!csp.Contains("script-src") && !csp.Contains("style-src"), "CSP no rompe scripts/estilos inline actuales");
    Check(r.Headers.TryGetValues("X-Frame-Options", out var xfo) && xfo.Single() == "SAMEORIGIN", "X-Frame-Options compatible con frame-ancestors");
    var mantener = await anonimo.GetAsync("/Cuenta/MantenerSesion");
    Check(mantener.StatusCode == HttpStatusCode.Redirect && mantener.Headers.Location!.OriginalString.Contains("/Cuenta/IniciarSesion"), "MantenerSesion exige sesión");
}

// --- Sesión: configuración
var opcionesCookie = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
Check(opcionesCookie.SlidingExpiration && opcionesCookie.ExpireTimeSpan == TimeSpan.FromMinutes(30), "Sliding expiration de 30 minutos");
Check(opcionesCookie.Cookie.HttpOnly && opcionesCookie.Cookie.SameSite == Microsoft.AspNetCore.Http.SameSiteMode.Lax, "Cookie HttpOnly y SameSite=Lax");
Check(opcionesCookie.Events.OnValidatePrincipal.Method.Name == nameof(SesionZuni.ValidarPrincipalAsync) &&
      opcionesCookie.Events.OnValidatePrincipal.Method.DeclaringType == typeof(SesionZuni), "Revalidación de SecurityStamp conectada");
var produccion = new CookieAuthenticationOptions();
SesionZuni.Configurar(produccion, desarrollo: false);
Check(produccion.Cookie.SecurePolicy == Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, "Cookie Secure en Production");
var inicio = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { SesionZuni.CrearClaimInicio(reloj.Ahora) }, "x"));
Check(!SesionZuni.ExcedeMaximo(inicio, reloj.Ahora.AddHours(7)) && SesionZuni.ExcedeMaximo(inicio, reloj.Ahora.AddHours(8).AddMinutes(1)), "Máximo absoluto de 8 horas");
Check(SesionZuni.ExcedeMaximo(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()), reloj.Ahora), "Cookie sin inicio de sesión se rechaza");

// --- Login: mensajes genéricos, hash ficticio y bloqueo
async Task<ApplicationUser> Leer(string id) { await using var db = Nuevo(); return await db.Users.AsNoTracking().SingleAsync(u => u.Id == id); }
await Usuario("bloqueo", true, false, "Estudiante");
await Perfil("bloqueo", "202001003");
using (var c = HostPrueba.Cliente(app))
{
    var antes = hasher.Verificaciones;
    var inexistente = await HostPrueba.Login(c, "nadie@miumg.edu.gt", clave);
    var textoInexistente = await HostPrueba.Texto(inexistente);
    Check(inexistente.StatusCode == HttpStatusCode.OK && textoInexistente.Contains(CuentaController.MensajeCredencialesInvalidas), "Correo inexistente: mensaje genérico");
    Check(hasher.Verificaciones == antes + 1, "Correo inexistente usa un hash ficticio (misma verificación)");

    var incorrecta = await HostPrueba.Login(c, "bloqueo@miumg.edu.gt", "incorrecta-1");
    Check((await HostPrueba.Texto(incorrecta)).Contains(CuentaController.MensajeCredencialesInvalidas), "Contraseña incorrecta: mismo mensaje");
    Check((await Leer("bloqueo")).AccessFailedCount == 1, "Fallo incrementa el contador");
    await HostPrueba.Login(c, "bloqueo@miumg.edu.gt", "incorrecta-2");
    Check((await Leer("bloqueo")).AccessFailedCount == 2, "Segundo fallo");
}
using (var c = HostPrueba.Cliente(app))
{
    var correcto = await HostPrueba.Login(c, "bloqueo@miumg.edu.gt", clave);
    Check(correcto.StatusCode == HttpStatusCode.Redirect, "Login correcto");
    Check((await Leer("bloqueo")).AccessFailedCount == 0, "Login correcto reinicia el contador");
    var setCookie = correcto.Headers.TryGetValues("Set-Cookie", out var cookies) ? string.Join(";", cookies).ToLowerInvariant() : "";
    Check(setCookie.Contains("httponly") && setCookie.Contains("samesite=lax"), "Cookie de sesión emitida con HttpOnly y SameSite");
    Check((await c.GetAsync("/Cuenta/MantenerSesion")).StatusCode == HttpStatusCode.NoContent, "MantenerSesion responde 204 con sesión");
}
using (var c = HostPrueba.Cliente(app))
{
    for (var i = 1; i <= CuentaController.MaximoIntentosFallidos; i++)
        await HostPrueba.Login(c, "bloqueo@miumg.edu.gt", "incorrecta-" + i);
    var bloqueado = await Leer("bloqueo");
    Check(bloqueado.LockoutEnd is { } fin && fin == reloj.Ahora.Add(CuentaController.DuracionBloqueo) && bloqueado.AccessFailedCount == 0,
        "Quinto fallo bloquea 15 minutos");
    var antes = hasher.Verificaciones;
    var duranteBloqueo = await HostPrueba.Login(c, "bloqueo@miumg.edu.gt", clave);
    Check(duranteBloqueo.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(duranteBloqueo)).Contains(CuentaController.MensajeCredencialesInvalidas),
        "Bloqueada: contraseña correcta no entra y el mensaje es genérico");
    Check(hasher.Verificaciones == antes + 1, "Bloqueada: solo verificación ficticia (no se comprueba la real)");
    var original = reloj.Ahora;
    reloj.Ahora = original.AddMinutes(14).AddSeconds(59);
    Check((await HostPrueba.Login(c, "bloqueo@miumg.edu.gt", clave)).StatusCode == HttpStatusCode.OK, "Sigue bloqueada a los 14:59");
    reloj.Ahora = original.AddMinutes(15).AddSeconds(1);
    Check((await HostPrueba.Login(c, "bloqueo@miumg.edu.gt", clave)).StatusCode == HttpStatusCode.Redirect, "Desbloqueada después de 15 minutos");
    var desbloqueado = await Leer("bloqueo");
    Check(desbloqueado.LockoutEnd is null && desbloqueado.AccessFailedCount == 0, "Login correcto limpia el bloqueo");
}

// --- Autorregistro pendiente de activación
using var admin = HostPrueba.Cliente(app);
Check((await HostPrueba.Login(admin, correoAdmin, clave)).StatusCode == HttpStatusCode.Redirect, "Administrador inicia sesión");
using (var c = HostPrueba.Cliente(app))
{
    var registro = await HostPrueba.Post(c, "/Cuenta/Registro", "/Cuenta/Registro",
        ("FullName", "Estudiante Nuevo"), ("Email", "nuevo@miumg.edu.gt"), ("CarneParte1", "2021"), ("CarneParte2", "01"),
        ("CarneParte3", "0099"), ("Telefono", "11223344"), ("Password", clave), ("ConfirmPassword", clave));
    Check(registro.StatusCode == HttpStatusCode.Redirect && registro.Headers.Location!.OriginalString.Contains("/Cuenta/IniciarSesion"), "Autorregistro redirige a iniciar sesión");
    string nuevoId;
    await using (var db = Nuevo())
    {
        var nuevo = await db.Users.AsNoTracking().SingleAsync(u => u.NormalizedEmail == "NUEVO@MIUMG.EDU.GT");
        nuevoId = nuevo.Id;
        Check(!nuevo.IsActive && await EstadoCuenta.EsRegistroPendienteAsync(db, nuevo.Id), "Autorregistro queda inactivo y pendiente");
    }
    Check((await c.GetAsync("/MiCuenta")).StatusCode == HttpStatusCode.Redirect, "Autorregistro no queda con sesión iniciada");
    var incorrecta = await HostPrueba.Texto(await HostPrueba.Login(c, "nuevo@miumg.edu.gt", "incorrecta"));
    Check(incorrecta.Contains(CuentaController.MensajeCredencialesInvalidas) && !incorrecta.Contains(CuentaController.MensajePendienteActivacion),
        "Pendiente + contraseña incorrecta: no revela el estado");
    var pendiente = await HostPrueba.Login(c, "nuevo@miumg.edu.gt", clave);
    Check(pendiente.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(pendiente)).Contains(CuentaController.MensajePendienteActivacion),
        "Pendiente + contraseña correcta: aviso de activación pendiente");
    Check((await c.GetAsync("/MiCuenta")).StatusCode == HttpStatusCode.Redirect, "Pendiente no puede entrar");

    var listado = await HostPrueba.Texto(await admin.GetAsync("/Administrador/Estudiantes?estado=Pendientes"));
    Check(listado.Contains(">Estudiante Nuevo<") && listado.Contains("Pendiente de activación") && !listado.Contains(">Persona est2<"),
        "Vista Estudiantes filtra pendientes de activación");
    var fichaPendiente = await HostPrueba.Texto(await admin.GetAsync($"/Administrador/Gestionar/{nuevoId}"));
    Check(fichaPendiente.Contains("Pendiente de activación") && fichaPendiente.Contains("Activar registro"), "Gestionar identifica la cuenta pendiente");
    var tableroPendientes = await HostPrueba.Texto(await admin.GetAsync("/Administrador"));
    Check(tableroPendientes.Contains("Pendientes de activación") && tableroPendientes.Contains("/Administrador/Estudiantes?estado=Pendientes"),
        "Tablero resume pendientes de activación");
    var confirmar = await HostPrueba.Texto(await admin.GetAsync($"/Administrador/ConfirmarEstado?id={nuevoId}&activar=true"));
    Check(confirmar.Contains("Activar registro"), "Confirmación muestra Activar registro");
    var activar = await HostPrueba.Post(admin, "/Administrador", "/Administrador/Reactivar", ("UsuarioId", nuevoId));
    Check(activar.StatusCode == HttpStatusCode.Redirect, "Administrador activa el registro");
    await using (var db = Nuevo())
        Check(!await EstadoCuenta.EsRegistroPendienteAsync(db, nuevoId), "Ya no figura como pendiente");
    Check((await HostPrueba.Login(c, "nuevo@miumg.edu.gt", clave)).StatusCode == HttpStatusCode.Redirect, "Activación permite iniciar sesión");
}

// Desactivada por un Administrador (no autorregistro): no se muestra como pendiente.
await Usuario("desactivado", true, false, "Catedratico");
var desactivar = await HostPrueba.Post(admin, "/Administrador", "/Administrador/Desactivar", ("UsuarioId", "desactivado"));
Check(desactivar.StatusCode == HttpStatusCode.Redirect, "Administrador desactiva una cuenta");
using (var c = HostPrueba.Cliente(app))
{
    var texto = await HostPrueba.Texto(await HostPrueba.Login(c, "desactivado@miumg.edu.gt", clave));
    Check(texto.Contains(CuentaController.MensajeCredencialesInvalidas) && !texto.Contains(CuentaController.MensajePendienteActivacion),
        "Cuenta desactivada no se presenta como pendiente");
}

// Cuenta creada por el Administrador: activa y con cambio obligatorio (comportamiento previo).
var alta = await HostPrueba.Post(admin, "/Administrador/AgregarUsuario", "/Administrador/AgregarUsuario",
    ("NombreCompleto", "Psicologa Alta"), ("Correo", "alta@miumg.edu.gt"), ("Rol", "Psicologo"),
    ("ContrasenaTemporal", clave), ("ConfirmarContrasenaTemporal", clave));
Check(alta.StatusCode == HttpStatusCode.Redirect, "Alta por Administrador");
await using (var db = Nuevo())
{
    var creado = await db.Users.AsNoTracking().SingleAsync(u => u.NormalizedEmail == "ALTA@MIUMG.EDU.GT");
    Check(creado.IsActive && creado.DebeCambiarContrasena && !await EstadoCuenta.EsRegistroPendienteAsync(db, creado.Id), "Alta por Administrador conserva activa + cambio obligatorio");
}
using (var c = HostPrueba.Cliente(app))
{
    var login = await HostPrueba.Login(c, "alta@miumg.edu.gt", clave);
    Check(login.StatusCode == HttpStatusCode.Redirect && login.Headers.Location!.OriginalString.Contains("CambiarContrasenaObligatoria"), "Alta por Administrador entra con cambio obligatorio");
}

// --- A3: contraseña temporal por Administrador
using (var sesionAnterior = HostPrueba.Cliente(app))
{
    Check((await HostPrueba.Login(sesionAnterior, correoEst, clave)).StatusCode == HttpStatusCode.Redirect, "Estudiante con sesión abierta");
    Check((await sesionAnterior.GetAsync("/MiCuenta")).StatusCode == HttpStatusCode.OK, "Sesión previa válida");
    ApplicationUser previo;
    await using (var db = Nuevo())
    {
        db.PasswordResetTokens.Add(new PasswordResetToken { UserId = "est", TokenHash = new string('A', 64), ExpiresAtUtc = reloj.Ahora.UtcDateTime.AddMinutes(30) });
        await db.SaveChangesAsync();
        previo = await db.Users.AsNoTracking().SingleAsync(u => u.Id == "est");
    }
    var confirmacion = await admin.GetAsync("/Administrador/ContrasenaTemporal/est");
    Check(confirmacion.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(confirmacion)).Contains("una sola vez"), "Confirmación de contraseña temporal");
    var generada = await HostPrueba.Post(admin, "/Administrador", "/Administrador/ContrasenaTemporal", ("UsuarioId", "est"), ("Motivo", "Olvidó su contraseña"));
    var html = await HostPrueba.Texto(generada);
    var temporal = System.Text.RegularExpressions.Regex.Match(html, "id=\"contrasena-temporal\"[^>]*>([^<]+)<").Groups[1].Value;
    Check(generada.StatusCode == HttpStatusCode.OK && temporal.Length == ContrasenaTemporalHelper.Longitud, "Contraseña temporal mostrada una vez");
    Check(generada.Headers.CacheControl?.NoStore == true, "Respuesta con la contraseña no se almacena en caché");
    await using (var db = Nuevo())
    {
        var actual = await db.Users.AsNoTracking().SingleAsync(u => u.Id == "est");
        Check(actual.PasswordHash != previo.PasswordHash, "Cambia el hash");
        Check(actual.DebeCambiarContrasena && actual.IsActive == previo.IsActive, "DebeCambiarContrasena = true y estado intacto");
        Check(actual.SecurityStamp != previo.SecurityStamp, "SecurityStamp cambia");
        Check(await db.PasswordResetTokens.AllAsync(t => t.UserId != "est" || t.UsedAtUtc != null), "Tokens pendientes invalidados");
        var eventos = await db.AuditoriaUsuarios.AsNoTracking().Where(a => a.Accion == AccionesAuditoria.ContrasenaTemporalAsignada).ToListAsync();
        Check(eventos.Count == 1 && eventos[0].UsuarioAfectadoId == "est" && eventos[0].AdministradorId == "admin", "Auditoría de contraseña temporal");
        var todo = string.Join("|", await db.AuditoriaUsuarios.AsNoTracking().Select(a => a.DatosAnteriores + a.DatosNuevos + a.Motivo).ToListAsync());
        Check(!todo.Contains(temporal) && !todo.Contains(actual.PasswordHash!), "La contraseña temporal no queda en la auditoría");
    }
    var anterior = await sesionAnterior.GetAsync("/MiCuenta");
    Check(anterior.StatusCode == HttpStatusCode.Redirect && anterior.Headers.Location!.OriginalString.Contains("/Cuenta/IniciarSesion"), "SecurityStamp invalida la sesión anterior");
    Check((await HostPrueba.Login(sesionAnterior, correoEst, clave)).StatusCode == HttpStatusCode.OK, "La contraseña anterior deja de funcionar");
}
using (var c = HostPrueba.Cliente(app))
{
    // Recupera la contraseña temporal del Administrador por un nuevo ciclo controlado.
    var generada = await HostPrueba.Post(admin, "/Administrador", "/Administrador/ContrasenaTemporal", ("UsuarioId", "est"));
    var temporal = System.Text.RegularExpressions.Regex.Match(await HostPrueba.Texto(generada), "id=\"contrasena-temporal\"[^>]*>([^<]+)<").Groups[1].Value;
    var login = await HostPrueba.Login(c, correoEst, temporal);
    Check(login.StatusCode == HttpStatusCode.Redirect && login.Headers.Location!.OriginalString.Contains("CambiarContrasenaObligatoria"), "Temporal exige cambio obligatorio");
    var bloqueada = await c.GetAsync("/MiCuenta");
    Check(bloqueada.StatusCode == HttpStatusCode.Redirect && bloqueada.Headers.Location!.OriginalString.Contains("CambiarContrasenaObligatoria"), "Cambio obligatorio sigue funcionando");
    var cambio = await HostPrueba.Post(c, "/Cuenta/CambiarContrasenaObligatoria", "/Cuenta/CambiarContrasenaObligatoria",
        ("PasswordActual", temporal), ("Password", clave), ("ConfirmPassword", clave));
    // La contraseña nueva no puede ser igual a la temporal, pero sí a la original (no hay historial).
    Check(cambio.StatusCode == HttpStatusCode.Redirect, "Cambio obligatorio completado");
    Check((await HostPrueba.Login(c, correoEst, clave)).StatusCode == HttpStatusCode.Redirect, "Inicia sesión con la contraseña nueva");
}
var propia = await admin.GetAsync("/Administrador/ContrasenaTemporal/admin");
Check(propia.StatusCode == HttpStatusCode.Redirect, "El Administrador no se asigna contraseña temporal a sí mismo");

// --- A1 por HTTP: pantalla sin datos clínicos, asignación y cambio de rol
var pantalla = await HostPrueba.Texto(await admin.GetAsync("/Administrador/SolicitudesAtencion"));
Check(pantalla.Contains("Persona est2") && pantalla.Contains("Solicitudes de atención"), "Pantalla de solicitudes renderiza");
Check(!pantalla.Contains("MOTIVO-CLINICO-PRIVADO") && !pantalla.Contains("CONTACTO-PRIVADO") && !pantalla.Contains("DIRECCION-PRIVADA"), "Listado sin información clínica");
var detalleHtml = await HostPrueba.Texto(await admin.GetAsync($"/Administrador/SolicitudesAtencion/{solicitud2}"));
Check(detalleHtml.Contains("Asignar psicólogo") && !detalleHtml.Contains("MOTIVO-CLINICO-PRIVADO") && !detalleHtml.Contains("CONTACTO-PRIVADO"), "Detalle sin información clínica");
var manipulada = await HostPrueba.Post(admin, "/Administrador", $"/Administrador/SolicitudesAtencion/{solicitud2}/Asignar",
    ("SolicitudId", solicitud.ToString()), ("PsicologoUsuarioId", "psi3"));
Check(manipulada.StatusCode == HttpStatusCode.BadRequest, "SolicitudId manipulado rechazado");
var asignada = await HostPrueba.Post(admin, "/Administrador", $"/Administrador/SolicitudesAtencion/{solicitud2}/Asignar",
    ("SolicitudId", solicitud2.ToString()), ("PsicologoUsuarioId", "psi3"));
Check(asignada.StatusCode == HttpStatusCode.Redirect && (await Estado(solicitud2)).PsicologoVigente == "psi3", "Asignación manual por HTTP");
using (var estudiante = HostPrueba.Cliente(app))
{
    await HostPrueba.Login(estudiante, "est2@miumg.edu.gt", clave);
    var prohibido = await estudiante.GetAsync("/Administrador/SolicitudesAtencion");
    Check(prohibido.StatusCode == HttpStatusCode.Redirect && prohibido.Headers.Location!.OriginalString.Contains("AccesoDenegado"), "Solo Administrador accede a solicitudes");
}

var cambioRol = await HostPrueba.Post(admin, "/Administrador", "/Administrador/CambiarRol", ("UsuarioId", "psi3"), ("NuevoRol", "Catedratico"));
Check(cambioRol.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(cambioRol)).Contains(AdministradorController.MensajePsicologoConAsignaciones),
    "No se quita el rol Psicologo con asignaciones vigentes");
await using (var db = Nuevo())
    Check(await db.UserRoles.AnyAsync(r => r.UserId == "psi3" && r.RoleId == "Psicologo") && !await db.UserRoles.AnyAsync(r => r.UserId == "psi3" && r.RoleId == "Catedratico"),
        "El rol no cambió");
var cambioPermitido = await HostPrueba.Post(admin, "/Administrador", "/Administrador/CambiarRol", ("UsuarioId", "psi1"), ("NuevoRol", "Catedratico"));
await using (var db = Nuevo())
    Check(cambioPermitido.StatusCode == HttpStatusCode.Redirect && await db.UserRoles.AnyAsync(r => r.UserId == "psi1" && r.RoleId == "Catedratico"),
        "Psicólogo sin asignaciones vigentes sí puede cambiar de rol");

Check(cambioPermitido.Headers.Location!.OriginalString.Contains("/Administrador/Gestionar/psi1"), "Tras cambiar el rol vuelve a la ficha del usuario");

// --- Panel del Administrador reorganizado: tablero sin tabla general y vistas por rol
bool Fila(string html, string nombre) => html.Contains(">" + nombre + "<");
async Task<string> Vista(string ruta) => await HostPrueba.Texto(await admin.GetAsync(ruta));
var tablero = await Vista("/Administrador");
Check(!tablero.Contains("<table") && !Fila(tablero, "Persona est2") && !tablero.Contains("est2@miumg.edu.gt") && !Fila(tablero, "Persona psi2"),
    "El tablero no mezcla usuarios en una tabla");
foreach (var accionRol in new[] { "Administradores", "Directores", "Psicologos", "Catedraticos", "Estudiantes" })
    Check(tablero.Contains("href=\"/Administrador/" + accionRol + "\""), "Tablero y menú enlazan a " + accionRol);
Check(System.Text.RegularExpressions.Regex.Matches(tablero, "Ver usuarios").Count >= 5, "Cada tarjeta de rol tiene Ver usuarios");
Check(tablero.Contains("Solicitudes pendientes"), "Tablero resume solicitudes pendientes");

var vistaAdministradores = await Vista("/Administrador/Administradores");
Check(Fila(vistaAdministradores, "Persona admin") && Fila(vistaAdministradores, "Persona admin-inactivo") &&
      !Fila(vistaAdministradores, "Persona psi2") && !Fila(vistaAdministradores, "Persona est2"), "Administradores: solo rol Administrador");
var vistaDirectores = await Vista("/Administrador/Directores");
Check(Fila(vistaDirectores, "Persona psi-director") && !Fila(vistaDirectores, "Persona psi2") && !Fila(vistaDirectores, "Persona admin"),
    "Directores: solo rol Director");
var vistaPsicologos = await Vista("/Administrador/Psicologos");
Check(Fila(vistaPsicologos, "Persona psi2") && Fila(vistaPsicologos, "Persona psi3") && Fila(vistaPsicologos, "Persona psi-director") &&
      !Fila(vistaPsicologos, "Persona psi1") && !Fila(vistaPsicologos, "Persona est2") && !Fila(vistaPsicologos, "Persona admin"),
    "Psicólogos: solo rol Psicologo (psi1 ya es Catedrático)");
var asignacionesPsi3 = System.Text.RegularExpressions.Regex.Match(vistaPsicologos, "data-usuario=\"psi3\"[\\s\\S]*?data-asignaciones=\"(\\d+)\"").Groups[1].Value;
Check(asignacionesPsi3 == "1", "Psicólogos: asignaciones vigentes por psicólogo");
Check(!vistaPsicologos.Contains("MOTIVO-CLINICO-PRIVADO") && !vistaPsicologos.Contains("CONTACTO-PRIVADO"), "Psicólogos: sin información clínica");
var vistaCatedraticos = await Vista("/Administrador/Catedraticos");
Check(Fila(vistaCatedraticos, "Persona psi1") && Fila(vistaCatedraticos, "Persona desactivado") && !Fila(vistaCatedraticos, "Persona psi2"),
    "Catedráticos: solo rol Catedratico");
Check(!Fila(await Vista("/Administrador/Catedraticos?estado=Activos"), "Persona desactivado"), "Filtro de estado por rol");
var vistaEstudiantes = await Vista("/Administrador/Estudiantes");
Check(Fila(vistaEstudiantes, "Persona est") && Fila(vistaEstudiantes, "Persona est2") && Fila(vistaEstudiantes, "Estudiante Nuevo") &&
      !Fila(vistaEstudiantes, "Persona psi2") && !Fila(vistaEstudiantes, "Persona admin"), "Estudiantes: solo rol Estudiante");
Check(vistaEstudiantes.Contains("2020-01-001") && vistaEstudiantes.Contains("Ingeniería"), "Estudiantes: carné y carrera");
Check(!vistaEstudiantes.Contains("MOTIVO-CLINICO-PRIVADO") && !vistaEstudiantes.Contains("CONTACTO-PRIVADO") && !vistaEstudiantes.Contains("DIRECCION-PRIVADA"),
    "Estudiantes: sin información clínica");
Check(Fila(await Vista("/Administrador/Estudiantes?buscar=2020-01-002"), "Persona est2") &&
      !Fila(await Vista("/Administrador/Estudiantes?buscar=2020-01-002"), "Persona est"), "Estudiantes: búsqueda por carné");
Check((await admin.GetAsync("/Administrador/Estudiantes?estado=Otro")).StatusCode == HttpStatusCode.BadRequest, "Filtro inválido rechazado");
Check(!vistaPsicologos.Contains("Cambiar rol") && vistaPsicologos.Contains("Gestionar"), "Tabla por rol solo muestra Gestionar");

var fichaPsi3 = await Vista("/Administrador/Gestionar/psi3");
Check(fichaPsi3.Contains("id=\"asignaciones-vigentes\">1<") && fichaPsi3.Contains("/Administrador/CambiarRol/psi3") &&
      fichaPsi3.Contains("/Administrador/ConfirmarEstado?id=psi3") && fichaPsi3.Contains("/Administrador/ContrasenaTemporal/psi3") &&
      fichaPsi3.Contains("/Administrador/Historial/psi3"), "Gestionar reúne datos, rol, estado y todas las acciones");
Check((await admin.GetAsync("/Administrador/Gestionar/no-existe")).StatusCode == HttpStatusCode.NotFound, "Gestionar usuario inexistente: 404");
var fichaPropia = await Vista("/Administrador/Gestionar/admin");
Check(!fichaPropia.Contains("/Administrador/ContrasenaTemporal/admin") && !fichaPropia.Contains(">Desactivar<"), "Ficha propia sin acciones peligrosas");
using (var noAdmin = HostPrueba.Cliente(app))
{
    await HostPrueba.Login(noAdmin, "est2@miumg.edu.gt", clave);
    foreach (var ruta in new[] { "/Administrador", "/Administrador/Psicologos", "/Administrador/Estudiantes", "/Administrador/Gestionar/psi3", "/Administrador/SinRol" })
    {
        var r = await noAdmin.GetAsync(ruta);
        Check(r.StatusCode == HttpStatusCode.Redirect && r.Headers.Location!.OriginalString.Contains("AccesoDenegado"), "Solo Administrador accede a " + ruta);
    }
}

// --- Después de cerrar, el estudiante puede iniciar una nueva solicitud
using (var estudiante = HostPrueba.Cliente(app))
{
    await HostPrueba.Login(estudiante, correoEst, clave);
    var formulario = await HostPrueba.Texto(await estudiante.GetAsync("/Estudiante/SolicitarAtencion"));
    Check(formulario.Contains("MotivoConsulta"), "Con la solicitud cerrada vuelve a mostrarse el formulario");
    var nueva = await HostPrueba.Post(estudiante, "/Estudiante/SolicitarAtencion", "/Estudiante/SolicitarAtencion",
        ("EsMayorEdad", "true"), ("Edad", "21"), ("Direccion", "Zona 1"), ("IdiomaPreferido", "Español"), ("MotivoConsulta", "Nueva consulta"),
        ("TipoIngreso", "IniciativaPropia"), ("AceptaConsentimiento", "true"),
        ("Contactos[0].Nombre", "Contacto"), ("Contactos[0].Relacion", "Padre"), ("Contactos[0].Telefono", "12341234"));
    Check(nueva.StatusCode == HttpStatusCode.Redirect, "Nueva solicitud enviada");
    await using var db = Nuevo();
    var activas = await db.SolicitudesAtencion.AsNoTracking().Where(s => s.PerfilEstudianteId == perfil && s.Estado != EstadoSolicitudAtencion.Cerrada).ToListAsync();
    Check(activas.Count == 1 && activas[0].Estado == EstadoSolicitudAtencion.Pendiente, "Nueva solicitud queda pendiente para revisión administrativa");
}

// --- Máximo absoluto de sesión (se ejecuta al final porque adelanta el reloj)
await PruebasChecks.Run(app, opciones, clave, perfil, perfil2);

using (var c = HostPrueba.Cliente(app))
{
    await HostPrueba.Login(c, "est2@miumg.edu.gt", clave);
    Check((await c.GetAsync("/MiCuenta")).StatusCode == HttpStatusCode.OK, "Sesión vigente");
    reloj.Ahora = reloj.Ahora.AddHours(8).AddMinutes(1);
    var vencida = await c.GetAsync("/MiCuenta");
    Check(vencida.StatusCode == HttpStatusCode.Redirect && vencida.Headers.Location!.OriginalString.Contains("/Cuenta/IniciarSesion"), "Sesión vence al superar 8 horas");
}

// --- Rate limiting: host separado con límite bajo
await using (var limitado = await HostPrueba.Crear(opciones, reloj, hasher, loginPorMinuto: 3))
{
    using var c = HostPrueba.Cliente(limitado);
    var html = await c.GetStringAsync("/Cuenta/IniciarSesion");
    var token = HostPrueba.Token(html);
    HttpResponseMessage? ultima = null;
    for (var i = 0; i < 4; i++)
        ultima = await c.PostAsync("/Cuenta/IniciarSesion", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = "nadie@miumg.edu.gt", ["Password"] = "x", ["__RequestVerificationToken"] = token }));
    Check((int)ultima!.StatusCode == 429, "Rate limiter responde 429 al exceder el límite");
    var cuerpo = await ultima.Content.ReadAsStringAsync();
    Check(cuerpo.Contains("Demasiados intentos") && !cuerpo.Contains("127.0.0.1") && !cuerpo.Contains(PoliticasAcceso.Login), "429 sin detalles internos");
    Check(ultima.Headers.TryGetValues("X-Content-Type-Options", out _), "429 también lleva cabeceras de seguridad");
    for (var i = 0; i < 5; i++)
        Check((await c.GetAsync("/Cuenta/IniciarSesion")).StatusCode == HttpStatusCode.OK, "GET de login no se limita");
    await limitado.StopAsync();
}

await app.StopAsync();
Console.WriteLine($"OK: {checks} comprobaciones de seguridad offline. PostgreSQL no fue abierto ni modificado.");
