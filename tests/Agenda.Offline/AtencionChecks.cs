using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;

internal static class AtencionChecks
{
    public static async Task<int> Run(OfflineDb db, AgendaCitasService service, ClaimsPrincipal estudiante,
        ClaimsPrincipal otroEstudiante, ClaimsPrincipal psicologo, ClaimsPrincipal ajeno, Reloj reloj, CancellationToken ct)
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Atención: " + label); checks++; }
        async Task Denegado<T>(Func<Task> action, string label) where T : Exception
        { try { await action(); } catch (T) { checks++; return; } throw new Exception("Atención: " + label); }
        var perfil = await db.PerfilesEstudiante.AsNoTracking().SingleAsync(p => p.UsuarioId == "est", ct);
        var horario = await db.DisponibilidadesPsicologo.AsNoTracking().FirstAsync(d => d.PsicologoUsuarioId == "otro-psi", ct);
        var secuencia = 0;
        async Task<Guid> Crear(EstadoCita estado = EstadoCita.Confirmada, bool futura = false)
        {
            var inicio = reloj.Ahora.UtcDateTime.AddDays(futura ? 10 : -10).AddHours(++secuencia);
            var cita = new Cita { PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "otro-psi", DisponibilidadPsicologoId = horario.Id,
                FechaHoraInicioUtc = inicio, FechaHoraFinUtc = inicio.AddMinutes(50), Estado = estado, FechaCreacionUtc = inicio.AddDays(-1) };
            db.Citas.Add(cita); await db.SaveChangesAsync(ct); db.ChangeTracker.Clear(); return cita.Id;
        }
        RegistrarAtencionInput Input(Guid id) => new() { CitaId = id, Asistio = true,
            ResultadoProfesionalPrivado = "  PRIVADO-NUNCA-ESTUDIANTE  ", ResultadoBreveEstudiante = "  Resumen compartido  ",
            ResenaRecomendacionesEstudiante = "  Recomendaciones compartidas  ", RecomiendaOtraCita = true, IndicacionesProximaCita = "  Solicitar seguimiento  " };
        var id = await Crear();
        await Denegado<AgendaAccesoException>(() => service.RegistrarAtencion(ajeno, Input(id), ct), "Psicólogo ajeno");
        await Denegado<AgendaAccesoException>(() => service.RegistrarAtencion(estudiante, Input(id), ct), "Estudiante no cierra");
        foreach (var role in new[] { "Administrador", "Director", "Catedratico" })
        {
            var actor = "mixto-" + role;
            var sesion = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actor),
                new Claim(ClaimTypes.Role, "Psicologo"), new Claim("Zuni.SecurityStamp", actor + "-stamp") }, "Offline"));
            await Denegado<AgendaAccesoException>(() => service.RegistrarAtencion(sesion, Input(id), ct), "Rol institucional persistido " + role);
        }
        foreach (var texto in new[] { "", "   ", new string('x', 4001) })
        {
            var input = Input(id); input.ResultadoProfesionalPrivado = texto;
            await Denegado<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, input, ct), "Privado requerido/longitud");
        }
        foreach (var campo in new[] { "resena", "breve", "indicaciones" })
        foreach (var texto in new[] { "   ", new string('x', 2001) })
        {
            var input = Input(id);
            if (campo == "resena") input.ResenaRecomendacionesEstudiante = texto;
            if (campo == "breve") input.ResultadoBreveEstudiante = texto;
            if (campo == "indicaciones") input.IndicacionesProximaCita = texto;
            await Denegado<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, input, ct), "Compartidos/indicación requeridos y limitados");
        }
        var noSeleccionada = Input(id); noSeleccionada.Asistio = null;
        await Denegado<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, noSeleccionada, ct), "Asistencia explícita");
        await Denegado<AgendaOperacionException>(() => service.GestionarCita(psicologo, new() { CitaId = id, Estado = EstadoCita.Completada }, ct), "Vía antigua no elude cierre");
        var totalCitas = await db.Citas.CountAsync(ct);
        await service.RegistrarAtencion(psicologo, Input(id), ct); db.ChangeTracker.Clear();
        var registro = await db.RegistrosAtencionCita.AsNoTracking().SingleAsync(r => r.CitaId == id, ct);
        Check(registro.ResultadoProfesionalPrivado == "PRIVADO-NUNCA-ESTUDIANTE" && registro.ResultadoBreveEstudiante == "Resumen compartido" && registro.IndicacionesProximaCita == "Solicitar seguimiento", "Trim aplicado");
        Check(await db.Citas.CountAsync(ct) == totalCitas, "Recomendación no crea cita");
        Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == id, ct)).Estado == EstadoCita.Completada, "Asistencia completa");
        Check((await service.PrepararAtencion(psicologo, id, ct)).Cerrada, "Cerrada solo lectura");
        await Denegado<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Input(id), ct), "Cierre único");
        var duplicado = new RegistroAtencionCita { CitaId = id, SolicitudAtencionId = registro.SolicitudAtencionId,
            Asistio = false, FechaCierreUtc = reloj.Ahora.UtcDateTime };
        db.RegistrosAtencionCita.Add(duplicado);
        await Denegado<DbUpdateException>(() => db.SaveChangesAsync(ct), "Índice UNIQUE impide segunda fila");
        db.ChangeTracker.Clear();
        var resultados = await service.ResultadosAtencion(estudiante, ct);
        var json = JsonSerializer.Serialize(resultados);
        Check(json.Contains("Resumen compartido") && !json.Contains("PRIVADO") && !json.Contains("ResultadoProfesionalPrivado"), "DTO público sin propiedad ni contenido privado");
        Check(!JsonSerializer.Serialize(await service.ResultadosAtencion(otroEstudiante, ct)).Contains("Resumen compartido"), "Otro estudiante no recibe resultados");
        Check((await service.ConsultarAtencion(psicologo, id, ct)).ResultadoProfesionalPrivado == "PRIVADO-NUNCA-ESTUDIANTE", "Detalle privado autorizado");
        await Denegado<AgendaAccesoException>(() => service.ConsultarAtencion(ajeno, id, ct), "Detalle ajeno denegado");
        await Denegado<AgendaAccesoException>(() => service.ConsultarAtencion(estudiante, id, ct), "Detalle denegado a estudiante");
        var stampViejo = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "otro-psi"),
            new Claim(ClaimTypes.Role, "Psicologo"), new Claim("Zuni.SecurityStamp", "caducado") }, "Offline"));
        await Denegado<AgendaAccesoException>(() => service.ConsultarAtencion(stampViejo, id, ct), "Detalle revalida stamp");
        var profesional = await db.Users.SingleAsync(u => u.Id == "otro-psi", ct);
        profesional.IsActive = false; await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
        await Denegado<AgendaAccesoException>(() => service.ConsultarAtencion(psicologo, id, ct), "Detalle revalida cuenta activa");
        profesional = await db.Users.SingleAsync(u => u.Id == "otro-psi", ct);
        profesional.IsActive = true; await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
        var consentimiento = await db.ConsentimientosAtencion.SingleAsync(c => c.SolicitudAtencionId == registro.SolicitudAtencionId, ct);
        consentimiento.Aceptado = false;
        await Denegado<InvalidOperationException>(()=>db.SaveChangesAsync(ct),"La aceptación histórica no se puede sobrescribir");
        db.ChangeTracker.Clear();
        // Solo la fixture SQLite simula corrupción externa; en PostgreSQL el trigger también la impide.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ConsentimientosAtencion SET Aceptado=0 WHERE Id={consentimiento.Id}",ct);
        await Denegado<AgendaAccesoException>(() => service.ConsultarAtencion(psicologo, id, ct), "Detalle exige consentimiento real");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ConsentimientosAtencion SET Aceptado=1 WHERE Id={consentimiento.Id}",ct);db.ChangeTracker.Clear();
        var auditoria = await db.EventosAccesoClinico.AsNoTracking().Where(a => a.SolicitudAtencionId == registro.SolicitudAtencionId).ToListAsync(ct);
        Check(auditoria.Any(a => a.TipoAcceso == "ATENCION_REGISTRADA") && auditoria.Any(a => a.TipoAcceso == "ATENCION_CONSULTADA"), "Auditorías de cierre/consulta");
        Check(!JsonSerializer.Serialize(auditoria).Contains("PRIVADO") && !JsonSerializer.Serialize(auditoria).Contains("Resumen compartido"), "Auditoría sin texto clínico");
        var asignacion = await db.AsignacionesEstudiantePsicologo.SingleAsync(a => a.PerfilEstudianteId == perfil.Id && a.FechaFinalizacionUtc == null, ct);
        asignacion.PsicologoUsuarioId = "psi"; await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
        await Denegado<AgendaAccesoException>(() => service.ConsultarAtencion(psicologo, id, ct), "Anterior pierde autorización vigente");
        await Denegado<AgendaAccesoException>(() => service.ConsultarAtencion(ajeno, id, ct), "Nuevo asignado no hereda privado histórico");
        asignacion = await db.AsignacionesEstudiantePsicologo.SingleAsync(a => a.Id == asignacion.Id, ct);
        asignacion.PsicologoUsuarioId = "otro-psi"; await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
        var noAsistio = await Crear(EstadoCita.Reservada);
        var ausente = Input(noAsistio); ausente.Asistio = false;
        await service.RegistrarAtencion(psicologo, ausente, ct); db.ChangeTracker.Clear();
        var ausencia = await db.RegistrosAtencionCita.AsNoTracking().SingleAsync(r => r.CitaId == noAsistio, ct);
        Check(ausencia.ResultadoProfesionalPrivado is null && ausencia.ResultadoBreveEstudiante is null && !ausencia.RecomiendaOtraCita && ausencia.IndicacionesProximaCita is null, "No asistencia normaliza datos manipulados");
        Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == noAsistio, ct)).Estado == EstadoCita.NoAsistio, "Inasistencia legado");
        var sinRecomendacion = await Crear(); var normal = Input(sinRecomendacion); normal.RecomiendaOtraCita = false;
        await service.RegistrarAtencion(psicologo, normal, ct); db.ChangeTracker.Clear();
        Check((await db.RegistrosAtencionCita.AsNoTracking().SingleAsync(r => r.CitaId == sinRecomendacion, ct)).IndicacionesProximaCita is null, "Sin recomendación elimina indicaciones");
        foreach (var estado in new[] { EstadoCita.Cancelada, EstadoCita.Rechazada, EstadoCita.Solicitada, EstadoCita.Completada, EstadoCita.NoAsistio })
        {
            var invalida = await Crear(estado);
            await Denegado<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Input(invalida), ct), "Estado inválido " + estado);
        }
        var futura = await Crear(futura: true);
        await Denegado<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Input(futura), ct), "Futura denegada");
        var rollback = await Crear();
        var eventosAntes = await db.EventosAccesoClinico.CountAsync(ct);
        db.FallarDespuesDeGuardarAtencion = true;
        try { await Denegado<DbUpdateException>(() => service.RegistrarAtencion(psicologo, Input(rollback), ct), "Fallo simulado"); }
        finally { db.FallarDespuesDeGuardarAtencion = false; }
        Check(!await db.RegistrosAtencionCita.AnyAsync(r => r.CitaId == rollback, ct) &&
            (await db.Citas.AsNoTracking().SingleAsync(c => c.Id == rollback, ct)).Estado == EstadoCita.Confirmada &&
            await db.EventosAccesoClinico.CountAsync(ct) == eventosAntes, "Rollback real de estado, registro y auditoría");
        db.FallarAuditoria = true;
        try { await Denegado<DbUpdateException>(() => service.ConsultarAtencion(psicologo, id, ct), "Fallo auditoría no devuelve detalle"); }
        finally { db.FallarAuditoria = false; }
        Check(await db.EventosAccesoClinico.CountAsync(ct) == eventosAntes, "Auditoría fallida revertida");
        return checks;
    }
}
