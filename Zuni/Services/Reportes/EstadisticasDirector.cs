using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Models.Reportes;
using Zuni.Services.Agenda;

namespace Zuni.Services.Reportes;

// Solo conteos. Las consultas proyectan estados, fechas y carrera; nunca identidades de estudiantes.
// Protección (docs/propuestas-director-privacidad.md):
//  - indicadores de personas (solicitudes, estudiantes, carreras, tendencia) hasta el cierre del mes
//    anterior: no cambian con cada cita, lo que evita deducir por diferencia antes y después de una fecha;
//  - supresión primaria y complementaria de cifras pequeñas (SupresionEstadistica);
//  - completadas e inasistencias se publican juntas como "concluidas" (la asistencia es privada);
//  - el endpoint no admite filtros, por lo que no hay combinaciones de filtros que comparar.
public static class EstadisticasDirector
{
    private static readonly EstadoCita[] Concluidas = [EstadoCita.Completada, EstadoCita.NoAsistio];

    public static async Task<EstadisticasDirectorViewModel> CalcularAsync(ApplicationDbContext db, TimeProvider reloj, CancellationToken ct)
    {
        var hoy = AgendaReglas.Hoy(reloj.GetUtcNow().UtcDateTime);
        var mesActual = new DateOnly(hoy.Year, hoy.Month, 1);
        var corte = AgendaReglas.Utc(mesActual, TimeOnly.MinValue);

        // --- Personas, hasta el cierre del mes anterior.
        var solicitudes = await db.SolicitudesAtencion.AsNoTracking().Where(s => s.FechaSolicitudUtc < corte).GroupBy(s => s.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToListAsync(ct);
        string EtiquetaSolicitud(EstadoSolicitudAtencion e) => e switch
        {
            EstadoSolicitudAtencion.Pendiente => "Pendientes",
            EstadoSolicitudAtencion.Asignada => "Asignadas",
            EstadoSolicitudAtencion.Cerrada => "Cerradas",
            _ => "Otros estados"
        };
        var concluidas = db.Citas.AsNoTracking().Where(c => Concluidas.Contains(c.Estado) && c.FechaHoraInicioUtc < corte);
        var personas = concluidas.Select(c => c.PerfilEstudianteId).Distinct();
        var totalPersonas = await personas.CountAsync(ct);
        var porCarrera = await db.PerfilesEstudiante.AsNoTracking().Where(p => personas.Contains(p.Id))
            .GroupBy(p => p.Carrera).Select(g => new { Carrera = g.Key, Cantidad = g.Count() }).ToListAsync(ct);
        var desgloseCarrera = SupresionEstadistica.Proteger(porCarrera
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Carrera) ? "Sin carrera registrada" : c.Carrera.Trim())
            .Select(g => (g.Key, g.Sum(c => c.Cantidad))));

        var primerMes = mesActual.AddMonths(-6);
        var desde = AgendaReglas.Utc(primerMes, TimeOnly.MinValue);
        var fechasConcluidas = await concluidas.Where(c => c.FechaHoraInicioUtc >= desde).Select(c => c.FechaHoraInicioUtc).ToListAsync(ct);
        var cultura = System.Globalization.CultureInfo.CurrentCulture;

        // --- Agenda vigente (citas, no personas).
        var citas = await db.Citas.AsNoTracking().GroupBy(c => c.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() }).ToListAsync(ct);
        int Citas(params EstadoCita[] estados) => citas.Where(c => estados.Contains(c.Estado)).Sum(c => c.Cantidad);
        var psicologos = await db.Users.AsNoTracking().Where(u => u.IsActive &&
                db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "PSICOLOGO")))
            .OrderBy(u => u.FullName).Select(u => new { u.Id, u.FullName }).ToListAsync(ct);
        var cargas = await db.Citas.AsNoTracking().GroupBy(c => c.PsicologoUsuarioId).Select(g => new
        {
            Psicologo = g.Key,
            Activas = g.Count(c => c.Estado == EstadoCita.Reservada || c.Estado == EstadoCita.Solicitada || c.Estado == EstadoCita.Confirmada),
            Concluidas = g.Count(c => c.Estado == EstadoCita.Completada || c.Estado == EstadoCita.NoAsistio)
        }).ToListAsync(ct);

        return new EstadisticasDirectorViewModel
        {
            Corte = mesActual,
            SolicitudesTotal = SupresionEstadistica.Cifra(solicitudes.Sum(s => s.Cantidad)),
            SolicitudesPorEstado = SupresionEstadistica.Proteger(solicitudes.Select(s => (EtiquetaSolicitud(s.Estado), s.Cantidad))),
            EstudiantesConCitasConcluidas = SupresionEstadistica.Cifra(totalPersonas),
            PorCarrera = desgloseCarrera,
            TendenciaMensual = Enumerable.Range(0, 6).Select(i => primerMes.AddMonths(i)).Select(mes =>
                new ConteoAgregado(mes.ToString("MMMM yyyy", cultura),
                    fechasConcluidas.Count(f => { var local = AgendaReglas.Hoy(f); return local.Year == mes.Year && local.Month == mes.Month; }))).ToList(),
            CitasProgramadas = Citas(EstadoCita.Reservada, EstadoCita.Confirmada),
            CitasPendientes = Citas(EstadoCita.Solicitada),
            CitasConcluidas = Citas(Concluidas),
            CitasCanceladas = Citas(EstadoCita.Cancelada, EstadoCita.Rechazada),
            CargaPorPsicologo = psicologos.Select(p =>
            {
                var carga = cargas.SingleOrDefault(c => c.Psicologo == p.Id);
                return new CargaPsicologoAgregada(p.FullName, carga?.Activas ?? 0, carga?.Concluidas ?? 0);
            }).ToList()
        };
    }
}
