using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Models.Reportes;
using Zuni.Services.Agenda;

namespace Zuni.Services.Reportes;

// Solo conteos. Las consultas proyectan estados, fechas y carrera; nunca identidades de estudiantes.
public static class EstadisticasDirector
{
    public static async Task<EstadisticasDirectorViewModel> CalcularAsync(ApplicationDbContext db, TimeProvider reloj, CancellationToken ct)
    {
        var solicitudes = await db.SolicitudesAtencion.AsNoTracking().GroupBy(s => s.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToListAsync(ct);
        var citas = await db.Citas.AsNoTracking().GroupBy(c => c.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToListAsync(ct);
        int Citas(params EstadoCita[] estados) => citas.Where(c => estados.Contains(c.Estado)).Sum(c => c.Cantidad);
        int Solicitudes(EstadoSolicitudAtencion estado) => solicitudes.Where(s => s.Estado == estado).Sum(s => s.Cantidad);

        var atendidos = db.Citas.AsNoTracking().Where(c => c.Estado == EstadoCita.Completada).Select(c => c.PerfilEstudianteId).Distinct();
        var totalAtendidos = await atendidos.CountAsync(ct);
        var porCarrera = await db.PerfilesEstudiante.AsNoTracking().Where(p => atendidos.Contains(p.Id))
            .GroupBy(p => p.Carrera).Select(g => new { Carrera = g.Key, Cantidad = g.Count() }).ToListAsync(ct);

        var psicologos = await db.Users.AsNoTracking().Where(u => u.IsActive &&
                db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "PSICOLOGO")))
            .OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName }).ToListAsync(ct);
        var cargas = await db.Citas.AsNoTracking().GroupBy(c => c.PsicologoUsuarioId).Select(g => new
        {
            Psicologo = g.Key,
            Activas = g.Count(c => c.Estado == EstadoCita.Reservada || c.Estado == EstadoCita.Solicitada || c.Estado == EstadoCita.Confirmada),
            Completadas = g.Count(c => c.Estado == EstadoCita.Completada)
        }).ToListAsync(ct);

        var hoy = AgendaReglas.Hoy(reloj.GetUtcNow().UtcDateTime);
        var primerMes = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-5);
        var desde = AgendaReglas.Utc(primerMes, TimeOnly.MinValue);
        var completadas = await db.Citas.AsNoTracking().Where(c => c.Estado == EstadoCita.Completada && c.FechaHoraInicioUtc >= desde)
            .Select(c => c.FechaHoraInicioUtc).ToListAsync(ct);
        var cultura = System.Globalization.CultureInfo.CurrentCulture;

        return new EstadisticasDirectorViewModel
        {
            SolicitudesTotal = solicitudes.Sum(s => s.Cantidad),
            SolicitudesPendientes = Solicitudes(EstadoSolicitudAtencion.Pendiente),
            SolicitudesAsignadas = Solicitudes(EstadoSolicitudAtencion.Asignada),
            SolicitudesCerradas = Solicitudes(EstadoSolicitudAtencion.Cerrada),
            EstudiantesAtendidos = totalAtendidos,
            CitasProgramadas = Citas(EstadoCita.Reservada, EstadoCita.Confirmada),
            CitasPendientes = Citas(EstadoCita.Solicitada),
            CitasCompletadas = Citas(EstadoCita.Completada),
            CitasCanceladas = Citas(EstadoCita.Cancelada, EstadoCita.Rechazada),
            Inasistencias = Citas(EstadoCita.NoAsistio),
            CargaPorPsicologo = psicologos.Select(p =>
            {
                var carga = cargas.SingleOrDefault(c => c.Psicologo == p.Id);
                return new CargaPsicologoAgregada(p.FullName, carga?.Activas ?? 0, carga?.Completadas ?? 0);
            }).ToList(),
            TendenciaMensual = Enumerable.Range(0, 6).Select(i => primerMes.AddMonths(i)).Select(mes =>
                new ConteoAgregado(mes.ToString("MMMM yyyy", cultura),
                    completadas.Count(f => { var local = AgendaReglas.Hoy(f); return local.Year == mes.Year && local.Month == mes.Month; }))).ToList(),
            // Supresión de celdas pequeñas: una carrera con 1 a 4 estudiantes atendidos podría identificarlos.
            AtendidosPorCarrera = porCarrera.OrderBy(c => c.Carrera ?? "~")
                .Select(c => new ConteoAgregado(string.IsNullOrWhiteSpace(c.Carrera) ? "Sin carrera registrada" : c.Carrera!,
                    c.Cantidad < EstadisticasDirectorViewModel.UmbralMinimo ? null : c.Cantidad)).ToList()
        };
    }
}
