using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Helpers;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Models.MayoriaEdad;
using Zuni.Services.Agenda;

// Propuestas de cita de Dirección (docs/propuestas-director-privacidad.md). Distingue:
//  - permitido: propuesta administrativa, consulta de sus propias propuestas y avisos administrativos;
//  - prohibido: confirmar, reprogramar, marcar asistencia, cerrar atención y cualquier acceso clínico.
// No usa PostgreSQL.
internal static class PropuestasDirectorChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj)
    {
        int checks = 0; void Check(bool ok, string label) { if (!ok) throw new Exception("Propuestas Director: " + label); checks++; }
        // CarneHelper.Formatear admite y puede devolver null; con los carnés de prueba debe producir el formato visible.
        static string CarneVisible(string carne)
        {
            var formateado = CarneHelper.Formatear(carne);
            return string.IsNullOrEmpty(formateado)
                ? throw new Exception("Propuestas Director: CarneHelper.Formatear no devolvió un formato visible para el carné de prueba " + carne)
                : formateado;
        }
        const string nombreA = "Propuesta Alfa Estudiante", nombreB = "Propuesta Beta Estudiante", nombreC = "Propuesta Gama Estudiante", nombreM = "Propuesta Menor Pendiente";
        const string carneA = "7777260001", carneB = "7777260002", carneC = "7777260003", carneM = "7777260009";
        const string psy = "prop-psy", psy2 = "prop-psy2", nombrePsy = "Psicóloga Propuestas Uno", clinico = "MOTIVO-PROPUESTA-PRIVADO";
        var hoy = AgendaReglas.Hoy(reloj.Ahora.UtcDateTime);
        var d1 = hoy.AddDays(40); var d2 = d1.AddDays(1); var d3 = d1.AddDays(2); var d4 = d1.AddDays(3); var d5 = d1.AddDays(4); var d6 = d1.AddDays(5);
        var d7 = d1.AddDays(6); var d8 = d1.AddDays(7); var pasado = hoy.AddDays(-3);
        const string nombreD = "Propuesta Delta Estudiante", carneD = "7777260004";

        // ------------------------------------------------------------ datos
        var hasher = new PasswordHasher<ApplicationUser>();
        Guid perfilA, perfilB, perfilC, perfilM, perfilD, solicitudA, solicitudD, vencida;
        await using (var db = new OfflineDb(options))
        {
            void Usuario(string id, string nombre, bool conClave, params string[] roles)
            {
                var correo = id + "@miumg.edu.gt";
                var u = new ApplicationUser { Id = id, FullName = nombre, Email = correo, NormalizedEmail = correo.ToUpperInvariant(), UserName = correo,
                    NormalizedUserName = correo.ToUpperInvariant(), IsActive = true, SecurityStamp = id + "-stamp" };
                if (conClave) u.PasswordHash = hasher.HashPassword(u, clave);
                db.Users.Add(u);
                foreach (var r in roles) db.UserRoles.Add(new() { UserId = id, RoleId = r });
            }
            Guid Perfil(string usuario, string carne, EstadoMayoriaEdad edad)
            {
                var p = new PerfilEstudiante { UsuarioId = usuario, Carne = carne, Carrera = "Carrera propuestas", Telefono = "12345678" };
                db.PerfilesEstudiante.Add(p);
                db.VerificacionesMayoriaEdad.Add(new()
                {
                    PerfilEstudianteId = p.Id, Estado = edad, EdadDeclarada = 21, FechaDeclaracionUtc = reloj.Ahora.UtcDateTime,
                    EdadVerificada = edad == EstadoMayoriaEdad.Verificada ? 21 : null, ResponsableUsuarioId = edad == EstadoMayoriaEdad.Verificada ? "admin" : null,
                    DocumentoRevisado = edad == EstadoMayoriaEdad.Verificada ? DocumentoEdad.Dpi : null
                });
                return p.Id;
            }
            Usuario(psy, nombrePsy, true, "Psicologo");
            Usuario(psy2, "Psicólogo Propuestas Dos", true, "Psicologo");
            Usuario("prop-dir2", "Directora Propuestas Dos", true, "Director");
            Usuario("prop-est-a", nombreA, true, "Estudiante");
            Usuario("prop-est-b", nombreB, true, "Estudiante");
            Usuario("prop-est-c", nombreC, true, "Estudiante");
            Usuario("prop-est-m", nombreM, true, "Estudiante");
            perfilA = Perfil("prop-est-a", carneA, EstadoMayoriaEdad.Verificada);
            perfilB = Perfil("prop-est-b", carneB, EstadoMayoriaEdad.Verificada);
            perfilC = Perfil("prop-est-c", carneC, EstadoMayoriaEdad.Verificada);
            perfilM = Perfil("prop-est-m", carneM, EstadoMayoriaEdad.Pendiente);
            Usuario("prop-est-d", nombreD, true, "Estudiante");
            perfilD = Perfil("prop-est-d", carneD, EstadoMayoriaEdad.Verificada);
            // D registró su solicitud de atención (con consentimiento), pero Administración aún no la asigna.
            var solicitudPendiente = new SolicitudAtencion { PerfilEstudianteId = perfilD, FechaSolicitudUtc = reloj.Ahora.UtcDateTime };
            solicitudD = solicitudPendiente.Id;
            db.SolicitudesAtencion.Add(solicitudPendiente);
            db.ConsentimientosAtencion.Add(new ConsentimientoAtencion { SolicitudAtencionId = solicitudPendiente.Id, Aceptado = true, VersionConsentimiento = "v1" });
            db.ExpedientesIniciales.Add(new ExpedienteInicial { SolicitudAtencionId = solicitudPendiente.Id, Edad = 22, EsMayorEdad = true, Direccion = "DIRECCION-D-PRIVADA",
                IdiomaPreferido = "Español", MotivoConsulta = clinico });
            // Propuesta de Dirección pendiente cuya fecha ya pasó (dato histórico).
            vencida = Guid.NewGuid();
            db.Citas.Add(new Cita { Id = vencida, PerfilEstudianteId = perfilD, PsicologoUsuarioId = psy, FechaHoraInicioUtc = AgendaReglas.Utc(pasado, new(10, 0)),
                FechaHoraFinUtc = AgendaReglas.Utc(pasado, new(10, 50)), FinAproximado = true, Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.Solicitada,
                ProgramadaDirectamente = true, ProgramadorUsuarioId = "director", ProgramadorRol = "Director", FechaCreacionUtc = reloj.Ahora.UtcDateTime.AddDays(-5) });
            // Horario semanal del psicólogo para comprobar la reserva normal del estudiante.
            foreach (var dia in Enum.GetValues<DayOfWeek>())
                db.DisponibilidadesPsicologo.Add(new DisponibilidadPsicologo { PsicologoUsuarioId = psy, DiaSemana = dia, HoraInicio = new(8, 0), HoraFin = new(18, 0),
                    DuracionMinutos = 60, Modalidad = ModalidadDisponibilidad.Presencial });
            for (var i = 1; i <= 12; i++) { Usuario($"masivo-{i:00}", $"Directorio Masivo {i:00}", false, "Estudiante"); Perfil($"masivo-{i:00}", $"55552600{i:00}", EstadoMayoriaEdad.Verificada); }
            // A recibe atención (solicitud, consentimiento, ficha y asignación); B no. El directorio no debe distinguirlos.
            var solicitud = new SolicitudAtencion { PerfilEstudianteId = perfilA, FechaSolicitudUtc = reloj.Ahora.UtcDateTime };
            solicitudA = solicitud.Id;
            db.SolicitudesAtencion.Add(solicitud);
            db.ConsentimientosAtencion.Add(new ConsentimientoAtencion { SolicitudAtencionId = solicitud.Id, Aceptado = true, VersionConsentimiento = "v1" });
            db.ExpedientesIniciales.Add(new ExpedienteInicial { SolicitudAtencionId = solicitud.Id, Edad = 21, EsMayorEdad = true, Direccion = "DIRECCION-PROPUESTA-PRIVADA",
                IdiomaPreferido = "Español", MotivoConsulta = clinico });
            db.AsignacionesEstudiantePsicologo.Add(new AsignacionEstudiantePsicologo { PerfilEstudianteId = perfilA, PsicologoUsuarioId = psy });
            db.AsignacionesEstudiantePsicologo.Add(new AsignacionEstudiantePsicologo { PerfilEstudianteId = perfilC, PsicologoUsuarioId = psy2 });
            // C ya tiene una cita confirmada con otro psicólogo en d2 a las 10:00.
            db.Citas.Add(new Cita { PerfilEstudianteId = perfilC, PsicologoUsuarioId = psy2, FechaHoraInicioUtc = AgendaReglas.Utc(d2, new(10, 0)),
                FechaHoraFinUtc = AgendaReglas.Utc(d2, new(10, 50)), Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.Confirmada,
                DireccionAtencion = "Consultorio C", ProgramadaDirectamente = true, ProgramadorUsuarioId = psy2, ProgramadorRol = "Psicologo", FechaCreacionUtc = reloj.Ahora.UtcDateTime });
            await db.SaveChangesAsync();
        }

        using var director = HostPrueba.Cliente(app); using var director2 = HostPrueba.Cliente(app); using var psicologo = HostPrueba.Cliente(app);
        using var estA = HostPrueba.Cliente(app); using var estB = HostPrueba.Cliente(app); using var estC = HostPrueba.Cliente(app); using var anon = HostPrueba.Cliente(app);
        await HostPrueba.Login(director, "director@miumg.edu.gt", clave); await HostPrueba.Login(director2, "prop-dir2@miumg.edu.gt", clave);
        await HostPrueba.Login(psicologo, psy + "@miumg.edu.gt", clave); await HostPrueba.Login(estA, "prop-est-a@miumg.edu.gt", clave);
        await HostPrueba.Login(estB, "prop-est-b@miumg.edu.gt", clave); await HostPrueba.Login(estC, "prop-est-c@miumg.edu.gt", clave);

        static bool Denegado(HttpResponseMessage r) => r.StatusCode == HttpStatusCode.Redirect && r.Headers.Location?.OriginalString.Contains("/Cuenta/AccesoDenegado") == true;
        async Task<string> Html(HttpClient c, string ruta) { var r = await c.GetAsync(ruta); Check(r.StatusCode == HttpStatusCode.OK, $"{ruta} abre (200); se obtuvo {(int)r.StatusCode}"); return await HostPrueba.Texto(r); }
        Task<HttpResponseMessage> Proponer(HttpClient c, Guid perfil, string psicologoId, DateOnly dia, string hora, Guid? operacion = null, (string, string)[]? extra = null) =>
            HostPrueba.Post(c, "/Director/ProponerCita?perfilId=" + perfil, "/Director/ProponerCita",
                new[] { ("Input.OperacionId", (operacion ?? Guid.NewGuid()).ToString()), ("Input.PerfilEstudianteId", perfil.ToString()), ("Input.PsicologoId", psicologoId),
                    ("Input.Fecha", dia.ToString("yyyy-MM-dd")), ("Input.HoraInicio", hora), ("Input.DuracionMinutos", "50"), ("Input.Modalidad", "Presencial") }
                .Concat(extra ?? Array.Empty<(string, string)>()).ToArray());
        static Guid IdDesde(HttpResponseMessage r) => Guid.Parse(r.Headers.Location!.OriginalString.Split('/').Last());
        // Fila de una propuesta concreta en Mis propuestas (otras pruebas también dejan propuestas históricas de "director").
        static string FilaPropuesta(string html, Guid id) => Regex.Match(html, "<tr data-propuesta=\"" + id + "\">.*?</tr>", RegexOptions.Singleline).Value;
        static string Fecha(DateOnly d) => d.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        static string Alerta(string html) => Regex.Match(html, "<p role=\"alert\">(.*?)</p>", RegexOptions.Singleline).Groups[1].Value.Trim();
        async Task<Cita> Leer(Guid id) { await using var db = new OfflineDb(options); return await db.Citas.AsNoTracking().SingleAsync(c => c.Id == id); }
        Task<HttpResponseMessage> Responder(Guid cita, Guid revision, string estado, string? motivo = null, string? lugar = null) =>
            HostPrueba.Post(psicologo, "/Psicologo/Agenda", "/Psicologo/GestionarCita", new[] { ("CitaId", cita.ToString()), ("Revision", revision.ToString()), ("Estado", estado) }
                .Concat(motivo is null ? Array.Empty<(string, string)>() : new[] { ("MotivoCancelacion", motivo) })
                .Concat(lugar is null ? Array.Empty<(string, string)>() : new[] { ("DireccionAtencion", lugar) }).ToArray());

        // ------------------------------------------------------------ acceso a la pantalla
        foreach (var c in new[] { estA, psicologo, anon })
            Check((await c.GetAsync("/Director/ProponerCita")).StatusCode == HttpStatusCode.Redirect, "solo Dirección abre Proponer cita");
        var inicio = await Html(director, "/Director/ProponerCita");
        Check(inicio.Contains("id=\"directorio-estudiantes\"") && inicio.Contains("Selecciona primero un estudiante"), "pantalla Proponer cita con buscador");
        var panel = await Html(director, "/Director");
        Check(panel.Contains("/Director/ProponerCita") && panel.Contains("/Director/Propuestas") && panel.Contains("id=\"aviso-propuestas-director\""), "menú y aviso de Mis propuestas");
        Check((await director.PostAsync("/Director/ProponerCita", new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "envío exige antiforgery");

        // ------------------------------------------------------------ 4. directorio
        Check((await Html(director, "/Director/ProponerCita?q=Pr")).Contains("Escribe al menos 3 caracteres"), "búsqueda mínima de 3 caracteres");
        var busqueda = await Html(director, "/Director/ProponerCita?q=Propuesta");
        var tabla = Regex.Match(busqueda, "<table data-directorio-resultados>.*?</table>", RegexOptions.Singleline).Value;
        Check(tabla.Contains(nombreA) && tabla.Contains(nombreB) && tabla.Contains(nombreC) && tabla.Contains(nombreM), "directorio encuentra por nombre (con o sin atención y con edad pendiente)");
        string Fila(string nombre, string carne, Guid perfil) => Regex.Matches(tabla, "<tr>.*?</tr>", RegexOptions.Singleline).Select(m => m.Value).Single(f => f.Contains(nombre))
            .Replace(nombre, "NOMBRE").Replace(CarneVisible(carne), "CARNE").Replace(perfil.ToString(), "PERFIL");
        Check(Fila(nombreA, carneA, perfilA) == Fila(nombreB, carneB, perfilB) && Fila(nombreA, carneA, perfilA) == Fila(nombreM, carneM, perfilM),
            "la fila de quien recibe atención es idéntica a la de quien no (sin indicadores)");
        foreach (var prohibido in new[] { nombrePsy, "Psicólogo Propuestas Dos", clinico, solicitudA.ToString(), "DIRECCION-PROPUESTA-PRIVADA", "Asignad", "Big Five", "Solicitud" })
            Check(!tabla.Contains(prohibido, StringComparison.OrdinalIgnoreCase), "directorio sin datos de atención: " + prohibido);
        var porCarne = await Html(director, "/Director/ProponerCita?q=" + Uri.EscapeDataString(CarneVisible(carneA)));
        Check(porCarne.Contains(nombreA) && !porCarne.Contains(nombreB), "directorio encuentra por carné");
        var masivo = await Html(director, "/Director/ProponerCita?q=" + Uri.EscapeDataString("Directorio Masivo"));
        Check(Regex.Matches(Regex.Match(masivo, "<table data-directorio-resultados>.*?</table>", RegexOptions.Singleline).Value, "perfilId=").Count == AgendaCitasService.ResultadosDirectorio
              && masivo.Contains("Hay más coincidencias"), "resultados limitados a 10");
        var seleccion = await Html(director, "/Director/ProponerCita?perfilId=" + perfilA);
        Check(seleccion.Contains("id=\"estudiante-seleccionado\"") && seleccion.Contains(nombrePsy) && !seleccion.Contains("Persona psi-director") && !seleccion.Contains(clinico),
            "formulario con psicólogos elegibles y sin datos clínicos");

        // ------------------------------------------------------------ 1, 2, 16. propuesta válida; POST manipulado
        var opA = Guid.NewGuid();
        var creada = await Proponer(director, perfilA, psy, d1, "09:00", opA, [("Estado", "Confirmada"), ("Input.Estado", "Confirmada"), ("CitaId", Guid.NewGuid().ToString()),
            ("Input.DireccionAtencion", "LUGAR-INYECTADO"), ("DireccionAtencion", "LUGAR-INYECTADO"), ("Input.Revision", Guid.NewGuid().ToString())]);
        Check(creada.StatusCode == HttpStatusCode.Redirect && creada.Headers.Location!.OriginalString.Contains("/Director/Propuesta/"), "Director crea una propuesta válida");
        var pA = IdDesde(creada);
        var citaA = await Leer(pA);
        Check(pA == opA && citaA.Estado == EstadoCita.Solicitada && citaA.ProgramadorRol == "Director" && citaA.ProgramadorUsuarioId == "director" && citaA.PsicologoUsuarioId == psy
              && citaA.DireccionAtencion is null && citaA.EnlaceReunion is null, "propuesta queda pendiente: campos manipulados no confirman ni fijan lugar");
        Check(Denegado(await HostPrueba.Post(director, "/Director", "/Psicologo/GestionarCita", ("CitaId", pA.ToString()), ("Revision", citaA.Revision.ToString()), ("Estado", "Confirmada"),
            ("DireccionAtencion", "Consultorio"))), "Director no confirma unilateralmente");
        Check(Denegado(await HostPrueba.Post(director, "/Director", "/CitasProgramadas/Guardar", ("CitaId", pA.ToString()), ("Revision", citaA.Revision.ToString()),
            ("OperacionId", Guid.NewGuid().ToString()), ("PerfilEstudianteId", perfilA.ToString()), ("Fecha", d2.ToString("yyyy-MM-dd")), ("HoraInicio", "10:00"),
            ("DuracionMinutos", "50"), ("Modalidad", "Presencial"))), "Director no reprograma");
        Check((await Leer(pA)).Revision == citaA.Revision, "intentos del Director no cambian la propuesta");

        // ------------------------------------------------------------ 17, 18. duplicados y cruces
        Check((await Proponer(director, perfilA, psy, d1, "09:00", opA)).StatusCode == HttpStatusCode.Conflict, "operación repetida no duplica");
        var pendiente = await Proponer(director, perfilA, psy, d3, "09:00");
        Check(pendiente.StatusCode == HttpStatusCode.Conflict && (await HostPrueba.Texto(pendiente)).Contains("Ya enviaste una propuesta pendiente"), "no duplica propuesta pendiente del mismo estudiante");
        var ocupado = await Proponer(director, perfilB, psy, d1, "09:00");
        Check(ocupado.StatusCode == HttpStatusCode.Conflict && (await HostPrueba.Texto(ocupado)).Contains("El psicólogo ya tiene ocupado ese horario"), "no se cruza con citas del psicólogo");
        Check((await Proponer(director, perfilB, psy, d1, "07:00")).StatusCode == HttpStatusCode.Conflict, "fuera del horario permitido");
        Check((await Proponer(director, perfilB, "psi-director", d1, "11:00")).StatusCode == HttpStatusCode.Conflict
              && (await Proponer(director, perfilB, "prop-est-b", d1, "11:00")).StatusCode == HttpStatusCode.Conflict, "solo psicólogos elegibles");
        await using (var db = new OfflineDb(options))
            Check(await db.Citas.CountAsync(c => c.ProgramadorRol == "Director" && (c.PerfilEstudianteId == perfilA || c.PerfilEstudianteId == perfilB)) == 1, "rechazos no crean citas");

        // ------------------------------------------------------------ 21 + privacidad: mensaje genérico idéntico
        var cruceEstudiante = await HostPrueba.Texto(await Proponer(director, perfilC, psy, d2, "10:00"));
        var menor = await Proponer(director, perfilM, psy, d2, "14:00");
        var textoMenor = await HostPrueba.Texto(menor);
        Check(menor.StatusCode == HttpStatusCode.Conflict && Alerta(textoMenor) == AgendaCitasService.MensajeEstudianteNoDisponible && Alerta(cruceEstudiante) == Alerta(textoMenor),
            "mayoría de edad pendiente y cita del estudiante dan el mismo mensaje genérico");
        Check(!cruceEstudiante.Contains("Psicólogo Propuestas Dos") && !cruceEstudiante.Contains("Consultorio C"), "el mensaje no revela la cita existente");
        await using (var db = new OfflineDb(options))
            Check(!await db.Citas.AnyAsync(c => c.PerfilEstudianteId == perfilM) && await db.Citas.CountAsync(c => c.PerfilEstudianteId == perfilC) == 1, "restricción de edad vigente: sin cita");

        // Otra cita del estudiante ese mismo día ya no cambia la respuesta: se registra igual que para quien no
        // tiene citas (no revela agenda ni atención) y la regla se valida cuando el psicólogo acepta.
        var mismoDia = await Proponer(director, perfilC, psy, d2, "14:00");
        Check(mismoDia.StatusCode == HttpStatusCode.Redirect && mismoDia.Headers.Location?.OriginalString.Contains("/Director/Propuesta/") == true,
            "proponer no revela que el estudiante tiene otra cita ese día");
        var pMismoDia = IdDesde(mismoDia);
        await using (var db = new OfflineDb(options))
        {
            var existente = await db.Citas.AsNoTracking().SingleAsync(c => c.PerfilEstudianteId == perfilC && c.PsicologoUsuarioId == psy2);
            Check(existente.Estado == EstadoCita.Confirmada && existente.FechaHoraInicioUtc == AgendaReglas.Utc(d2, new(10, 0)) && existente.DireccionAtencion == "Consultorio C",
                "la cita existente del estudiante queda intacta");
        }
        Check((await Responder(pMismoDia, (await Leer(pMismoDia)).Revision, "Rechazada", MotivosPropuestaDirector.OtraFecha)).StatusCode == HttpStatusCode.Redirect,
            "el psicólogo resuelve la propuesta que coincide con otra cita");

        // ------------------------------------------------------------ 3. el psicólogo recibe la propuesta
        var avisosPsy = await Html(psicologo, "/CitasProgramadas/Notificaciones");
        Check(avisosPsy.Contains("Nueva propuesta de Dirección") && avisosPsy.Contains(nombreA) && avisosPsy.Contains("Aceptar, Reprogramar o Rechazar"), "psicólogo recibe la propuesta");
        var agendaA = await Html(psicologo, "/Psicologo/Agenda?citaId=" + pA);
        Check(agendaA.Contains("Propuesta de Dirección") && agendaA.Contains("Aceptar cita") && agendaA.Contains("Rechazar propuesta") && agendaA.Contains("Reprogramar"),
            "agenda identifica la propuesta de Dirección y ofrece aceptar, reprogramar o rechazar");
        Check((await Html(estA, "/CitasProgramadas/Notificaciones")).Contains("Dirección propuso una cita"), "estudiante recibe aviso de la propuesta");

        // ------------------------------------------------------------ 8. estado inicial para Dirección
        var mias = await Html(director, "/Director/Propuestas");
        Check(FilaPropuesta(mias, pA).Contains(nombreA) && FilaPropuesta(mias, pA).Contains("Pendiente de revisión") && FilaPropuesta(mias, pA).Contains(nombrePsy) && !mias.Contains(clinico),
            "Director consulta sus propuestas");

        // ------------------------------------------------------------ 4. aceptar
        Check((await Responder(pA, citaA.Revision, "Confirmada", lugar: "Consultorio propuestas, edificio D")).StatusCode == HttpStatusCode.Redirect, "psicólogo acepta");
        Check((await Leer(pA)).Estado == EstadoCita.Confirmada, "propuesta confirmada por el psicólogo");
        var filaAceptada = FilaPropuesta(await Html(director, "/Director/Propuestas"), pA);
        Check(filaAceptada.Contains("Aceptada · confirmada") && filaAceptada.Contains("Novedad"), "Mis propuestas marca la novedad");
        var detalleA = await Html(director, "/Director/Propuesta/" + pA);
        Check(detalleA.Contains("Aceptada · confirmada") && detalleA.Contains("Fecha y hora definitivas") && detalleA.Contains("Propuesta aceptada") && detalleA.Contains(nombrePsy),
            "detalle con fecha definitiva y aviso de aceptación");
        Check(!detalleA.Contains("Consultorio propuestas") && !detalleA.Contains(clinico) && !detalleA.Contains("DIRECCION-PROPUESTA-PRIVADA"), "detalle sin lugar ni datos clínicos");
        await using (var db = new OfflineDb(options))
            Check(await db.NotificacionesCita.AnyAsync(n => n.UsuarioId == "director" && n.CitaId == pA && n.Tipo == AgendaCitasService.AvisosDirector.Aceptada)
                  && !await db.NotificacionesCita.AnyAsync(n => n.UsuarioId == "director" && n.CitaId == pA && !n.Leida), "aviso de aceptación leído al abrir el detalle");
        Check((await Html(estA, "/CitasProgramadas/Notificaciones")).Contains("aceptó la cita propuesta"), "estudiante recibe la aceptación");

        // ------------------------------------------------------------ 5. reprogramar (sin crear otra cita activa)
        var segunda = await Proponer(director, perfilA, psy, d3, "10:00");
        Check(segunda.StatusCode == HttpStatusCode.Redirect, "nueva propuesta para otra fecha");
        var pA2 = IdDesde(segunda); var antes = await Leer(pA2);
        var reprogramada = await HostPrueba.Post(psicologo, "/CitasProgramadas/Editar?citaId=" + pA2, "/CitasProgramadas/Guardar", ("CitaId", pA2.ToString()), ("Revision", antes.Revision.ToString()),
            ("OperacionId", Guid.NewGuid().ToString()), ("PerfilEstudianteId", perfilA.ToString()), ("Fecha", d4.ToString("yyyy-MM-dd")), ("HoraInicio", "11:00"), ("DuracionMinutos", "50"),
            ("Modalidad", "Presencial"), ("DireccionAtencion", "Consultorio propuestas, edificio D"));
        Check(reprogramada.StatusCode == HttpStatusCode.Redirect, "psicólogo reprograma la propuesta");
        var despues = await Leer(pA2);
        Check(despues.FechaHoraInicioUtc == AgendaReglas.Utc(d4, new(11, 0)) && despues.ProgramadorRol == "Director" && despues.ProgramadorUsuarioId == "director" && despues.Estado == EstadoCita.Confirmada,
            "reprogramación conserva el origen de Dirección");
        await using (var db = new OfflineDb(options))
            Check(await db.Citas.CountAsync(c => c.PerfilEstudianteId == perfilA && (c.Estado == EstadoCita.Solicitada || c.Estado == EstadoCita.Confirmada || c.Estado == EstadoCita.Reservada)) == 2,
                "la reprogramación no crea una segunda cita activa");
        var detalleA2 = await Html(director, "/Director/Propuesta/" + pA2);
        Check(detalleA2.Contains("Reprogramada · confirmada") && detalleA2.Contains("Fecha anterior: " + Fecha(d3)) && detalleA2.Contains("Nueva fecha: " + Fecha(d4))
              && detalleA2.Contains("Fecha y hora definitivas"), "Dirección ve el nuevo horario y la traza de la fecha original");
        Check((await Html(estA, "/CitasProgramadas/Notificaciones")).Contains("Cita reprogramada"), "estudiante recibe la reprogramación");

        // Al aceptar se aplican las reglas de agenda del estudiante (solo las ve el psicólogo asignado).
        var mismoDiaA = await Proponer(director, perfilA, psy, d1, "14:00");
        Check(mismoDiaA.StatusCode == HttpStatusCode.Redirect, "propuesta en un día en que el estudiante ya tiene cita");
        var pA3 = IdDesde(mismoDiaA);
        var aceptarCruce = await Responder(pA3, (await Leer(pA3)).Revision, "Confirmada", lugar: "Consultorio propuestas, edificio D");
        Check(aceptarCruce.StatusCode == HttpStatusCode.Conflict && (await HostPrueba.Texto(aceptarCruce)).Contains("otra cita activa ese día"), "al aceptar se respeta una cita activa por día");
        var original = await Leer(pA);
        Check(original.Estado == EstadoCita.Confirmada && original.FechaHoraInicioUtc == AgendaReglas.Utc(d1, new(9, 0)), "la cita confirmada existente no se modifica");
        Check((await Responder(pA3, (await Leer(pA3)).Revision, "Rechazada", MotivosPropuestaDirector.OtraFecha)).StatusCode == HttpStatusCode.Redirect,
            "el psicólogo rechaza la propuesta que no puede aceptar");

        // ------------------------------------------------------------ 6. rechazar (estudiante sin asignación)
        var propuestaB = await Proponer(director, perfilB, psy, d5, "09:00");
        Check(propuestaB.StatusCode == HttpStatusCode.Redirect, "propuesta para estudiante sin asignación (no revela atención)");
        var pB = IdDesde(propuestaB); var citaB = await Leer(pB);
        var agendaB = await Html(psicologo, "/Psicologo/Agenda?citaId=" + pB);
        Check(agendaB.Contains("data-propuesta-sin-asignacion"), "agenda advierte que falta la asignación para aceptar");
        Check(Denegado(await Responder(pB, citaB.Revision, "Confirmada", lugar: "Consultorio")), "aceptar sigue exigiendo asignación vigente");
        Check((await Responder(pB, citaB.Revision, "Rechazada", "MOTIVO-CLINICO-LIBRE")).StatusCode == HttpStatusCode.Conflict, "rechazo exige motivo de la lista");
        Check((await Responder(pB, citaB.Revision, "Rechazada", MotivosPropuestaDirector.Requisitos)).StatusCode == HttpStatusCode.Redirect, "psicólogo rechaza la propuesta");
        Check((await Leer(pB)).Estado == EstadoCita.Rechazada, "propuesta rechazada");
        Check((await Responder(pB, citaB.Revision, "Rechazada", MotivosPropuestaDirector.Duplicada)).StatusCode == HttpStatusCode.Conflict
              && (await Leer(pB)).MotivoCancelacion == MotivosPropuestaDirector.Requisitos, "respuesta tardía o duplicada no altera el estado");
        var detalleB = await Html(director, "/Director/Propuesta/" + pB);
        Check(detalleB.Contains("data-estado-propuesta>Rechazada") && detalleB.Contains(MotivosPropuestaDirector.NoConfirmable) && !detalleB.Contains(MotivosPropuestaDirector.Requisitos)
              && !detalleB.Contains("MOTIVO-CLINICO-LIBRE"), "Dirección ve el rechazo con un motivo que no revela solicitud ni asignación");
        var avisosB = await Html(estB, "/CitasProgramadas/Notificaciones");
        Check(avisosB.Contains("rechazó la cita propuesta") && avisosB.Contains(MotivosPropuestaDirector.Requisitos) && avisosB.Contains("Solicitar atención"),
            "estudiante recibe el rechazo y cómo continuar");

        // ------------------------------------------------------------ 7. el estudiante cancela: Dirección ve "cancelada" sin motivo
        var propuestaC = await Proponer(director, perfilC, psy, d6, "09:00");
        Check(propuestaC.StatusCode == HttpStatusCode.Redirect, "propuesta para otro estudiante");
        var pC = IdDesde(propuestaC);
        Check((await Html(estC, "/Estudiante/Citas?fecha=" + d6.ToString("yyyy-MM-dd"))).Contains("data-propuesta-direccion"), "estudiante ve la propuesta de Dirección en sus citas");
        Check((await HostPrueba.Post(estC, "/Estudiante/Citas", "/Estudiante/Citas/Cancelar", ("citaId", pC.ToString()), ("motivoCancelacion", "MOTIVO-PERSONAL-PRIVADO"))).StatusCode == HttpStatusCode.Redirect,
            "estudiante conserva su flujo de cancelación");
        var detalleC = await Html(director, "/Director/Propuesta/" + pC);
        Check(detalleC.Contains("data-estado-propuesta>Cancelada") && !detalleC.Contains("MOTIVO-PERSONAL-PRIVADO"), "Dirección ve la cancelación sin el motivo del estudiante");

        // ------------------------------------------------------------ propuesta sin asignación → Administración asigna → aceptar
        using var admin = HostPrueba.Cliente(app); using var estD = HostPrueba.Cliente(app);
        await HostPrueba.Login(admin, "admin@miumg.edu.gt", clave); await HostPrueba.Login(estD, "prop-est-d@miumg.edu.gt", clave);
        var propuestaD = await Proponer(director, perfilD, psy, d7, "10:00");
        Check(propuestaD.StatusCode == HttpStatusCode.Redirect, "propuesta para estudiante con solicitud aún sin asignar");
        var pD = IdDesde(propuestaD);
        Check((await Html(psicologo, "/Psicologo/Agenda?citaId=" + pD)).Contains("data-propuesta-sin-asignacion"), "el psicólogo ve la indicación administrativa");
        Check(Denegado(await Responder(pD, (await Leer(pD)).Revision, "Confirmada", lugar: "Consultorio")), "sin asignación vigente no puede aceptar");
        Check((await Html(estD, "/CitasProgramadas/Notificaciones")).Contains("necesitas una solicitud de atención vigente"), "el estudiante recibe cómo continuar");
        var detalleSolicitud = await Html(admin, "/Administrador/SolicitudesAtencion/" + solicitudD);
        Check(detalleSolicitud.Contains("id=\"propuestas-direccion-pendientes\"") && detalleSolicitud.Contains(nombrePsy) && detalleSolicitud.Contains("data-citas-activas>0<"),
            "Administración ve el psicólogo propuesto y la propuesta pendiente no bloquea");
        await HostPrueba.Post(admin, "/Administrador/SolicitudesAtencion/" + solicitudD, "/Administrador/SolicitudesAtencion/" + solicitudD + "/Asignar",
            ("SolicitudId", solicitudD.ToString()), ("PsicologoUsuarioId", psy));
        await using (var db = new OfflineDb(options))
            Check(await db.AsignacionesEstudiantePsicologo.AnyAsync(a => a.PerfilEstudianteId == perfilD && a.PsicologoUsuarioId == psy && a.FechaFinalizacionUtc == null),
                "Administración asigna con el módulo existente (Dirección no crea asignaciones)");
        var agendaD = await Html(psicologo, "/Psicologo/Agenda?citaId=" + pD);
        Check(!agendaD.Contains("data-propuesta-sin-asignacion") && agendaD.Contains("Aceptar cita"), "con asignación vigente se habilita Aceptar");
        Check((await Responder(pD, (await Leer(pD)).Revision, "Confirmada", lugar: "Consultorio propuestas, edificio D")).StatusCode == HttpStatusCode.Redirect
              && (await Leer(pD)).Estado == EstadoCita.Confirmada, "la propuesta continúa y el psicólogo la acepta");
        var detalleD = await Html(director, "/Director/Propuesta/" + pD);
        Check(detalleD.Contains("Aceptada · confirmada") && !detalleD.Contains(solicitudD.ToString()) && !detalleD.Contains("DIRECCION-D-PRIVADA") && !detalleD.Contains(clinico),
            "Dirección ve la aceptación sin datos de la solicitud ni de la asignación");

        // Propuesta pendiente con la fecha vencida: estado derivado y el psicólogo aún puede cerrarla.
        Check(FilaPropuesta(await Html(director, "/Director/Propuestas"), vencida).Contains("Sin respuesta · fecha vencida"), "propuesta sin respuesta y fecha pasada se muestra vencida");
        Check((await Responder(vencida, (await Leer(vencida)).Revision, "Rechazada", MotivosPropuestaDirector.OtraFecha)).StatusCode == HttpStatusCode.Redirect
              && (await Leer(vencida)).Estado == EstadoCita.Rechazada, "una propuesta vencida no queda bloqueada");

        // Una propuesta pendiente de Dirección no bloquea la reserva normal del estudiante.
        Check((await Proponer(director, perfilA, psy2, d8, "10:00")).StatusCode == HttpStatusCode.Redirect, "propuesta pendiente con otro psicólogo");
        var reserva = await HostPrueba.Post(estA, "/Estudiante/Citas", "/Estudiante/Citas/Reservar", ("Inicio", AgendaReglas.Utc(d8, new(15, 0)).ToString("O")),
            ("Modalidad", "Presencial"), ("DuracionMinutos", ""));
        await using (var db = new OfflineDb(options))
            Check(reserva.StatusCode == HttpStatusCode.Redirect && await db.Citas.AnyAsync(c => c.PerfilEstudianteId == perfilA && c.FechaHoraInicioUtc == AgendaReglas.Utc(d8, new(15, 0))
                  && c.ProgramadorRol == null && c.Estado == EstadoCita.Solicitada), "la reserva del estudiante no se bloquea por una propuesta pendiente");

        // ------------------------------------------------------------ 9. propuestas ajenas
        Check(Denegado(await director2.GetAsync("/Director/Propuesta/" + pA)), "otra cuenta de Dirección no consulta propuestas ajenas");
        var mias2 = await Html(director2, "/Director/Propuestas");
        Check(!mias2.Contains(nombreA) && !mias2.Contains(nombreB) && mias2.Contains("Todavía no has enviado propuestas"), "Mis propuestas solo lista las propias");

        // ------------------------------------------------------------ 10–14. acceso clínico prohibido
        foreach (var ruta in new[] { "/Psicologo/Expediente/" + solicitudA, "/Psicologo/Expediente/" + solicitudA + "/BigFive", "/PruebasAplicadas/Resumen/" + Guid.NewGuid(),
                     "/Psicologo/VerAtencion?citaId=" + pA, "/Psicologo/Historial", "/Psicologo/RegistrarAtencion?citaId=" + pA, "/Psicologo/Solicitudes?expedientes=true",
                     "/Psicologo/Estudiantes", "/CitasProgramadas/Notificaciones" })
            Check(Denegado(await director.GetAsync(ruta)), "Director sin acceso clínico: " + ruta);
        Check(Denegado(await HostPrueba.Post(director, "/Director", "/Psicologo/RegistrarAtencion", ("CitaId", pA.ToString()), ("Asistio", "true"))), "Director no marca asistencia");
        Check(Denegado(await HostPrueba.Post(director, "/Director", "/Psicologo/GestionarCita", ("CitaId", pA.ToString()), ("Revision", Guid.NewGuid().ToString()), ("Estado", "Completada"))),
            "Director no cierra una atención");
        await using (var db = new OfflineDb(options))
            Check(!await db.RegistrosAtencionCita.AnyAsync(r => r.CitaId == pA) && (await db.Citas.AsNoTracking().SingleAsync(c => c.Id == pA)).Estado == EstadoCita.Confirmada, "sin registro de atención por Dirección");
        foreach (var html in new[] { mias, detalleA, detalleA2, detalleB, detalleC, await Html(director, "/Director/Propuestas") })
            Check(!html.Contains(clinico) && !html.Contains("DIRECCION-PROPUESTA-PRIVADA") && !html.Contains("Consultorio propuestas"), "pantallas de Dirección sin datos clínicos ni lugar");

        // ------------------------------------------------------------ 15. calendario general anonimizado
        var carga = await Html(director, "/Director/Calendario?psicologoId=" + psy + "&fecha=" + d1.ToString("yyyy-MM-dd"));
        Check(carga.Contains("data-director-bloques") && new[] { nombreA, nombreB, nombreC, carneA, CarneVisible(carneA), pA.ToString(), pB.ToString() }.All(x => !carga.Contains(x)),
            "calendario general sin identidades");

        // ------------------------------------------------------------ 19, 20. flujos del psicólogo y del estudiante
        Check((await psicologo.GetAsync("/CitasProgramadas")).StatusCode == HttpStatusCode.OK && (await psicologo.GetAsync("/CitasProgramadas/Editar")).StatusCode == HttpStatusCode.OK,
            "programación del psicólogo intacta");
        var citasA = await Html(estA, "/Estudiante/Citas?fecha=" + d1.ToString("yyyy-MM-dd"));
        Check(citasA.Contains("agenda-grid") && citasA.Contains("data-propuesta-direccion"), "flujo normal del estudiante funcional");

        // ------------------------------------------------------------ 22. auditoría sin contenido clínico
        await using (var db = new OfflineDb(options))
        {
            var referencias = new[] { pA, pA2, pB, pC }.Select(id => JsonSerializer.Serialize(id)).ToArray();
            // DatosNuevos está cifrado en reposo: se filtra por acción en SQL y por referencia ya descifrada.
            var auditoria = (await db.AuditoriaUsuarios.AsNoTracking().Where(a => a.Accion.StartsWith("PROPUESTA_DIRECTOR_")).ToListAsync())
                .Where(a => a.DatosNuevos is not null && referencias.Contains(a.DatosNuevos)).ToList();
            int Cuenta(string accion) => auditoria.Count(a => a.Accion == accion);
            Check(Cuenta("PROPUESTA_DIRECTOR_CREADA") == 4 && Cuenta("PROPUESTA_DIRECTOR_ACEPTADA") == 1 && Cuenta("PROPUESTA_DIRECTOR_REPROGRAMADA") == 1
                  && Cuenta("PROPUESTA_DIRECTOR_RECHAZADA") == 1 && Cuenta("PROPUESTA_DIRECTOR_CANCELADA") == 1, "auditoría de crear, aceptar, reprogramar, rechazar y cancelar");
            Check(auditoria.Where(a => a.Accion == "PROPUESTA_DIRECTOR_CREADA").All(a => a.AdministradorId == "director")
                  && auditoria.Single(a => a.Accion == "PROPUESTA_DIRECTOR_ACEPTADA").AdministradorId == psy
                  && auditoria.Single(a => a.Accion == "PROPUESTA_DIRECTOR_CANCELADA").AdministradorId == "prop-est-c", "auditoría registra al actor");
            Check(auditoria.All(a => a.DatosAnteriores is null && a.Motivo is null && !(a.DatosNuevos ?? "").Contains("MOTIVO")), "auditoría solo guarda metadatos");
        }
        Console.WriteLine($"OK: {checks} comprobaciones de propuestas de Dirección.");
    }
}
