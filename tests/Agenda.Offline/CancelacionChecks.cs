using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;

internal static class CancelacionChecks
{
    public static async Task<int> Run(ApplicationDbContext db, AgendaCitasService service,
        ClaimsPrincipal estudiante, ClaimsPrincipal ajeno, ClaimsPrincipal psicologo, ClaimsPrincipal psicologoAjeno,
        TimeProvider reloj, CancellationToken ct)
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Cancelación: " + label); checks++; }
        var fecha = AgendaReglas.Hoy(reloj.GetUtcNow().UtcDateTime).AddMonths(1);
        await service.AgregarHorarioDia(psicologo, new HorarioDiaInput
        {
            Fecha = fecha, HoraInicio = new(8, 0), HoraFin = new(10, 0), DuracionMinutos = 30,
            Modalidad = ModalidadDisponibilidad.Presencial
        }, ct);
        async Task<Cita> Nueva(int minuto = 0)
        {
            var inicio = AgendaReglas.Utc(fecha, new TimeOnly(8, 0).AddMinutes(minuto));
            await service.Reservar(estudiante, new() { Inicio = new DateTimeOffset(inicio), Modalidad = ModalidadCita.Presencial }, ct);
            return await db.Citas.AsNoTracking().SingleAsync(c => c.FechaHoraInicioUtc == inicio && c.Estado == EstadoCita.Solicitada);
        }
        var cita = await Nueva();
        foreach (var actor in new[] { "estudiante", "psicologo" })
        {
            foreach (var motivo in new string?[] { null, "", " \t\r\n ", new string('x', 301) })
            {
                var esperado = motivo?.Length > 300
                    ? "El motivo de cancelación no puede superar los 300 caracteres."
                    : "Debes indicar un motivo de cancelación.";
                try
                {
                    if (actor == "estudiante") await service.CancelarEstudiante(estudiante, cita.Id, motivo, ct);
                    else await service.GestionarCita(psicologo, new() { CitaId = cita.Id, Estado = EstadoCita.Cancelada, MotivoCancelacion = motivo }, ct);
                    throw new Exception("Cancelación inválida aceptada");
                }
                catch (AgendaOperacionException ex) { Check(ex.Message == esperado, "Mensaje exacto " + actor); }
                var actual = await db.Citas.AsNoTracking().SingleAsync(c => c.Id == cita.Id);
                Check(actual.Estado == EstadoCita.Solicitada && actual.MotivoCancelacion == null, "Rechazo conserva cita y motivo " + actor);
            }
        }
        foreach (var actor in new[] { "estudiante", "psicologo" })
        {
            try
            {
                if (actor == "estudiante") await service.CancelarEstudiante(ajeno, cita.Id, "Motivo válido", ct);
                else await service.GestionarCita(psicologoAjeno, new() { CitaId = cita.Id, Estado = EstadoCita.Cancelada, MotivoCancelacion = "Motivo válido" }, ct);
                throw new Exception("Se permitió cancelar cita ajena");
            }
            catch (AgendaAccesoException) { checks++; }
        }
        const string texto = "Necesito cambiar mi horario <script>alert(1)</script>";
        await service.CancelarEstudiante(estudiante, cita.Id, "  " + texto + "  ", ct);
        var cancelada = await db.Citas.AsNoTracking().SingleAsync(c => c.Id == cita.Id);
        Check(cancelada.Estado == EstadoCita.Cancelada && cancelada.MotivoCancelacion == texto, "Estudiante guarda motivo y Trim");
        Check((await service.Historial(psicologo, null, EstadoCita.Cancelada, 1, ct)).Citas.Any(c => c.Id == cita.Id && c.MotivoCancelacion == texto), "Motivo proyectado en historial propio");
        Check((await service.LeerCitas(estudiante, fecha, 1, ct)).Citas.Any(c => c.Id == cita.Id && c.MotivoCancelacion == texto), "Motivo de propia cita visible");
        Check(!(await service.LeerCitas(ajeno, fecha, 1, ct)).Citas.Any(c => c.Id == cita.Id || c.MotivoCancelacion == texto), "Otro estudiante no ve cita ni motivo");
        Check(!JsonSerializer.Serialize((await service.LeerCitas(estudiante, fecha, 1, ct)).Calendario).Contains("MotivoCancelacion"), "Calendario sin motivo");

        var segunda = await Nueva(30);
        await service.GestionarCita(psicologo, new() { CitaId = segunda.Id, Estado = EstadoCita.Confirmada }, ct);
        Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == segunda.Id)).Estado == EstadoCita.Confirmada, "Confirmar no requiere motivo");
        await service.GestionarCita(psicologo, new() { CitaId = segunda.Id, Estado = EstadoCita.Cancelada, MotivoCancelacion = "  Cambio de disponibilidad.  " }, ct);
        Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == segunda.Id)).MotivoCancelacion == "Cambio de disponibilidad.", "Psicólogo guarda Trim");
        var tercera = await Nueva(60);
        await service.CancelarEstudiante(estudiante, tercera.Id, new string('a', 300), ct);
        Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == tercera.Id)).MotivoCancelacion?.Length == 300, "Límite 300 aceptado");
        var cuarta = await Nueva(90);
        await service.GestionarCita(psicologo, new() { CitaId = cuarta.Id, Estado = EstadoCita.Rechazada }, ct);
        Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == cuarta.Id)).MotivoCancelacion == null, "Rechazo no exige ni almacena motivo de cancelación");

        // Registro legado solo en SQLite en memoria; ninguna transformación de datos reales.
        var antigua = await db.Citas.SingleAsync(c => c.Id == tercera.Id);
        antigua.MotivoCancelacion = null;
        await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
        Check((await service.Historial(psicologo, null, null, 1, ct)).Citas.Any(c => c.Id == antigua.Id && c.MotivoCancelacion == null), "Historial admite registro legado sin motivo");
        return checks;
    }
}
