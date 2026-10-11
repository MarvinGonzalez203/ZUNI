using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Administrador;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;

namespace Zuni.Services.Administracion;

public sealed class GestionSolicitudesAccesoException : Exception;
public sealed class GestionSolicitudesException(string mensaje) : Exception(mensaje);

// Resolución administrativa de solicitudes de atención (A1). No lee ni devuelve contenido clínico:
// solo estado de la solicitud, asignaciones y conteo de citas activas.
//
// Decisiones:
// - Las asignaciones son por perfil de estudiante (no por solicitud). Solo puede existir una vigente
//   (índice UX_AsignacionesEstudiantePsicologo_Vigente); nunca se borran, solo se finalizan.
// - Finalizar una asignación sin cerrar la solicitud la devuelve a Pendiente.
// - No se reasigna, finaliza ni cierra mientras el estudiante tenga citas activas (Reservada,
//   Solicitada o Confirmada): primero deben cancelarse o registrarse. Así ninguna cita queda
//   en manos de un psicólogo que ya perdió el acceso clínico.
public sealed class GestionSolicitudesService(ApplicationDbContext db, TimeProvider reloj)
{
    public const int TamanoPagina = 25;
    private static readonly EstadoCita[] EstadosCitaActivos = [EstadoCita.Reservada, EstadoCita.Solicitada, EstadoCita.Confirmada];
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;

    // ------------------------------------------------------------------ lectura

    public async Task<SolicitudesAtencionAdminViewModel> Listar(string? administradorId, EstadoSolicitudAtencion? estado,
        string? buscar, int pagina, CancellationToken ct)
    {
        if (!await AdministradorActivo(administradorId, ct)) throw new GestionSolicitudesAccesoException();
        if (estado.HasValue && !Enum.IsDefined(estado.Value)) throw new GestionSolicitudesException("Estado no válido.");
        var texto = (buscar ?? string.Empty).Trim();
        if (texto.Length > 100) throw new GestionSolicitudesException("La búsqueda admite hasta 100 caracteres.");

        var consulta = Filas();
        if (estado.HasValue) consulta = consulta.Where(f => f.Estado == estado.Value);
        if (texto.Length > 0)
        {
            var minusculas = texto.ToLower();
            var carne = texto.Replace("-", string.Empty);
            consulta = consulta.Where(f => f.Estudiante.ToLower().Contains(minusculas) || f.Carne.Contains(carne));
        }
        var conteos = await db.SolicitudesAtencion.AsNoTracking().GroupBy(s => s.Estado)
            .Select(g => new { g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Total, ct);
        var paginas = Math.Max(1, (await consulta.CountAsync(ct) + TamanoPagina - 1) / TamanoPagina);
        pagina = Math.Clamp(pagina, 1, paginas);
        return new SolicitudesAtencionAdminViewModel
        {
            Estado = estado, Buscar = texto, Pagina = pagina, Paginas = paginas,
            Pendientes = conteos.GetValueOrDefault(EstadoSolicitudAtencion.Pendiente),
            Asignadas = conteos.GetValueOrDefault(EstadoSolicitudAtencion.Asignada),
            Cerradas = conteos.GetValueOrDefault(EstadoSolicitudAtencion.Cerrada),
            Solicitudes = await consulta.OrderBy(f => f.Estado).ThenByDescending(f => f.FechaSolicitudUtc).ThenBy(f => f.SolicitudId)
                .Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToListAsync(ct)
        };
    }

    public async Task<DetalleSolicitudAdminViewModel> Detalle(string? administradorId, Guid solicitudId, CancellationToken ct)
    {
        if (!await AdministradorActivo(administradorId, ct)) throw new GestionSolicitudesAccesoException();
        var fila = await Filas().SingleOrDefaultAsync(f => f.SolicitudId == solicitudId, ct)
            ?? throw new GestionSolicitudesException("La solicitud no existe.");
        var perfilId = await db.SolicitudesAtencion.AsNoTracking().Where(s => s.Id == solicitudId)
            .Select(s => s.PerfilEstudianteId).SingleAsync(ct);
        var historial = await (from a in db.AsignacionesEstudiantePsicologo.AsNoTracking()
                               join u in db.Users.AsNoTracking() on a.PsicologoUsuarioId equals u.Id
                               where a.PerfilEstudianteId == perfilId
                               orderby a.FechaAsignacionUtc descending, a.Id
                               select new AsignacionHistorial(u.FullName, a.FechaAsignacionUtc, a.FechaFinalizacionUtc)).ToListAsync(ct);
        var actual = await db.AsignacionesEstudiantePsicologo.AsNoTracking()
            .Where(a => a.PerfilEstudianteId == perfilId && a.FechaFinalizacionUtc == null)
            .Select(a => a.PsicologoUsuarioId).SingleOrDefaultAsync(ct);
        return new DetalleSolicitudAdminViewModel
        {
            Solicitud = fila,
            PsicologoActualId = fila.Estado == EstadoSolicitudAtencion.Cerrada ? null : actual,
            CitasActivas = await db.Citas.AsNoTracking().CountAsync(c => c.PerfilEstudianteId == perfilId && EstadosCitaActivos.Contains(c.Estado), ct),
            Historial = historial,
            Elegibles = await PsicologosElegibles().OrderBy(u => u.FullName).ThenBy(u => u.Id)
                .Select(u => new PsicologoElegible(u.Id, u.FullName, u.Email, u.DebeCambiarContrasena)).ToListAsync(ct)
        };
    }

    private IQueryable<SolicitudAdminFila> Filas() =>
        from s in db.SolicitudesAtencion.AsNoTracking()
        join p in db.PerfilesEstudiante.AsNoTracking() on s.PerfilEstudianteId equals p.Id
        join u in db.Users.AsNoTracking() on p.UsuarioId equals u.Id
        select new SolicitudAdminFila
        {
            SolicitudId = s.Id, Estudiante = u.FullName, Carne = p.Carne, Carrera = p.Carrera,
            FechaSolicitudUtc = s.FechaSolicitudUtc, Estado = s.Estado,
            // Una solicitud cerrada ya no tiene psicólogo vigente (una nueva solicitud del mismo
            // estudiante podría tenerlo, pero no pertenece a esta fila).
            Psicologo = s.Estado == EstadoSolicitudAtencion.Cerrada ? null :
                (from a in db.AsignacionesEstudiantePsicologo
                 join pu in db.Users on a.PsicologoUsuarioId equals pu.Id
                 where a.PerfilEstudianteId == p.Id && a.FechaFinalizacionUtc == null
                 select pu.FullName).FirstOrDefault(),
            PsicologoActivo = s.Estado == EstadoSolicitudAtencion.Cerrada ? null :
                (from a in db.AsignacionesEstudiantePsicologo
                 join pu in db.Users on a.PsicologoUsuarioId equals pu.Id
                 where a.PerfilEstudianteId == p.Id && a.FechaFinalizacionUtc == null
                 select (bool?)pu.IsActive).FirstOrDefault(),
            FechaAsignacionUtc = s.Estado == EstadoSolicitudAtencion.Cerrada ? null :
                db.AsignacionesEstudiantePsicologo
                    .Where(a => a.PerfilEstudianteId == p.Id && a.FechaFinalizacionUtc == null)
                    .Select(a => (DateTime?)a.FechaAsignacionUtc).FirstOrDefault()
        };

    // Misma política que el acceso clínico: rol real Psicologo, cuenta activa y sin roles
    // institucionales. Se incluyen cuentas con cambio de contraseña pendiente (igual que la
    // asignación automática); la vista lo indica porque no podrán atender hasta cambiarla.
    private IQueryable<ApplicationUser> PsicologosElegibles() =>
        db.Users.AsNoTracking().Where(u => u.IsActive &&
            db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "PSICOLOGO")) &&
            !db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId &&
                (r.NormalizedName == "ADMINISTRADOR" || r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO"))));

    // ------------------------------------------------------------------ escritura

    public Task Asignar(string? administradorId, Guid solicitudId, string? psicologoId, string? motivo, CancellationToken ct) =>
        Escribir(administradorId, async adminId =>
        {
            var (solicitud, usuarioEstudiante) = await Solicitud(solicitudId, ct);
            if (solicitud.Estado != EstadoSolicitudAtencion.Pendiente)
                throw new GestionSolicitudesException("Solo se puede asignar una solicitud pendiente. Si ya está asignada, usa Reasignar.");
            await ValidarEstudiante(solicitud.PerfilEstudianteId, ct);
            if (await Vigente(solicitud.PerfilEstudianteId, ct) is not null)
                throw new GestionSolicitudesException("El estudiante ya tiene una asignación vigente. Finalízala o usa Reasignar.");
            var psicologo = await ValidarPsicologo(psicologoId, ct);
            var nueva = new AsignacionEstudiantePsicologo
            {
                PerfilEstudianteId = solicitud.PerfilEstudianteId, PsicologoUsuarioId = psicologo, FechaAsignacionUtc = Ahora
            };
            db.AsignacionesEstudiantePsicologo.Add(nueva);
            solicitud.Estado = EstadoSolicitudAtencion.Asignada;
            Auditar(adminId, usuarioEstudiante, AccionesAuditoria.SolicitudAsignada, motivo,
                new { SolicitudId = solicitud.Id, Estado = "Pendiente" },
                new { SolicitudId = solicitud.Id, Estado = "Asignada", PsicologoUsuarioId = psicologo, AsignacionId = nueva.Id });
        }, ct);

    public Task Reasignar(string? administradorId, Guid solicitudId, string? psicologoId, string? motivo, CancellationToken ct) =>
        Escribir(administradorId, async adminId =>
        {
            var (solicitud, usuarioEstudiante) = await Solicitud(solicitudId, ct);
            if (solicitud.Estado != EstadoSolicitudAtencion.Asignada)
                throw new GestionSolicitudesException("Solo se puede reasignar una solicitud asignada.");
            var anterior = await Vigente(solicitud.PerfilEstudianteId, ct)
                ?? throw new GestionSolicitudesException("La solicitud no tiene una asignación vigente. Asígnala desde una solicitud pendiente.");
            await ValidarEstudiante(solicitud.PerfilEstudianteId, ct);
            var psicologo = await ValidarPsicologo(psicologoId, ct);
            if (psicologo == anterior.PsicologoUsuarioId)
                throw new GestionSolicitudesException("Selecciona un psicólogo distinto al actual.");
            await ValidarSinCitasActivas(solicitud.PerfilEstudianteId, "reasignar", ct);
            Finalizar(anterior);
            // Guardar antes de insertar: el índice filtrado admite una sola asignación vigente.
            await db.SaveChangesAsync(ct);
            var nueva = new AsignacionEstudiantePsicologo
            {
                PerfilEstudianteId = solicitud.PerfilEstudianteId, PsicologoUsuarioId = psicologo, FechaAsignacionUtc = Ahora
            };
            db.AsignacionesEstudiantePsicologo.Add(nueva);
            Auditar(adminId, usuarioEstudiante, AccionesAuditoria.SolicitudReasignada, motivo,
                new { SolicitudId = solicitud.Id, Estado = "Asignada", PsicologoUsuarioId = anterior.PsicologoUsuarioId, AsignacionId = anterior.Id },
                new { SolicitudId = solicitud.Id, Estado = "Asignada", PsicologoUsuarioId = psicologo, AsignacionId = nueva.Id });
        }, ct);

    public Task FinalizarAsignacion(string? administradorId, Guid solicitudId, string? motivo, CancellationToken ct) =>
        Escribir(administradorId, async adminId =>
        {
            var (solicitud, usuarioEstudiante) = await Solicitud(solicitudId, ct);
            if (solicitud.Estado != EstadoSolicitudAtencion.Asignada)
                throw new GestionSolicitudesException("Solo se puede finalizar la asignación de una solicitud asignada.");
            var anterior = await Vigente(solicitud.PerfilEstudianteId, ct)
                ?? throw new GestionSolicitudesException("La solicitud no tiene una asignación vigente.");
            await ValidarSinCitasActivas(solicitud.PerfilEstudianteId, "finalizar la asignación", ct);
            Finalizar(anterior);
            // La solicitud sigue abierta: vuelve a Pendiente para asignar a otro psicólogo.
            solicitud.Estado = EstadoSolicitudAtencion.Pendiente;
            Auditar(adminId, usuarioEstudiante, AccionesAuditoria.AsignacionFinalizada, motivo,
                new { SolicitudId = solicitud.Id, Estado = "Asignada", PsicologoUsuarioId = anterior.PsicologoUsuarioId, AsignacionId = anterior.Id },
                new { SolicitudId = solicitud.Id, Estado = "Pendiente" });
        }, ct);

    public Task Cerrar(string? administradorId, Guid solicitudId, string? motivo, bool confirmar, CancellationToken ct) =>
        Escribir(administradorId, async adminId =>
        {
            if (!confirmar) throw new GestionSolicitudesException("Confirma que deseas cerrar la solicitud.");
            var (solicitud, usuarioEstudiante) = await Solicitud(solicitudId, ct);
            if (solicitud.Estado == EstadoSolicitudAtencion.Cerrada)
                throw new GestionSolicitudesException("La solicitud ya está cerrada.");
            await ValidarSinCitasActivas(solicitud.PerfilEstudianteId, "cerrar la solicitud", ct);
            var estadoAnterior = solicitud.Estado.ToString();
            var anterior = await Vigente(solicitud.PerfilEstudianteId, ct);
            if (anterior is not null) Finalizar(anterior);
            solicitud.Estado = EstadoSolicitudAtencion.Cerrada;
            Auditar(adminId, usuarioEstudiante, AccionesAuditoria.SolicitudCerrada, motivo,
                new { SolicitudId = solicitud.Id, Estado = estadoAnterior, AsignacionId = anterior?.Id },
                new { SolicitudId = solicitud.Id, Estado = "Cerrada" });
        }, ct);

    // ------------------------------------------------------------------ apoyo

    private async Task Escribir(string? administradorId, Func<string, Task> operacion, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // Se revalida dentro de la transacción: nunca confiar solo en el atributo [Authorize].
            if (!await AdministradorActivo(administradorId, ct)) throw new GestionSolicitudesAccesoException();
            await operacion(administradorId!);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (Conflicto(ex))
        {
            await tx.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw new GestionSolicitudesException("La solicitud cambió mientras guardabas. Actualiza la página y revisa antes de volver a intentarlo.");
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private static bool Conflicto(Exception ex)
    {
        var pg = ex as PostgresException ?? ex.InnerException as PostgresException;
        return pg?.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation;
    }

    private Task<bool> AdministradorActivo(string? id, CancellationToken ct) =>
        string.IsNullOrEmpty(id) ? Task.FromResult(false) :
        (from u in db.Users.AsNoTracking()
         join ur in db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
         join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
         where u.Id == id && u.IsActive && r.NormalizedName == "ADMINISTRADOR"
         select u.Id).AnyAsync(ct);

    private async Task<(SolicitudAtencion Solicitud, string UsuarioEstudianteId)> Solicitud(Guid id, CancellationToken ct)
    {
        var solicitud = await db.SolicitudesAtencion.SingleOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new GestionSolicitudesException("La solicitud no existe.");
        var usuario = await db.PerfilesEstudiante.AsNoTracking().Where(p => p.Id == solicitud.PerfilEstudianteId)
            .Select(p => p.UsuarioId).SingleAsync(ct);
        return (solicitud, usuario);
    }

    private Task<AsignacionEstudiantePsicologo?> Vigente(Guid perfilId, CancellationToken ct) =>
        db.AsignacionesEstudiantePsicologo.SingleOrDefaultAsync(a => a.PerfilEstudianteId == perfilId && a.FechaFinalizacionUtc == null, ct);

    private async Task ValidarEstudiante(Guid perfilId, CancellationToken ct)
    {
        var elegible = await db.PerfilesEstudiante.AsNoTracking().AnyAsync(p => p.Id == perfilId && p.Activo && p.Usuario.IsActive &&
            db.UserRoles.Any(ur => ur.UserId == p.UsuarioId && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")), ct);
        if (!elegible)
            throw new GestionSolicitudesException("El estudiante no está activo o ya no tiene el rol Estudiante.");
    }

    private async Task<string> ValidarPsicologo(string? psicologoId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(psicologoId) || !await PsicologosElegibles().AnyAsync(u => u.Id == psicologoId, ct))
            throw new GestionSolicitudesException("Selecciona un psicólogo elegible: cuenta activa, rol Psicólogo y sin roles institucionales.");
        return psicologoId;
    }

    private async Task ValidarSinCitasActivas(Guid perfilId, string operacion, CancellationToken ct)
    {
        if (await db.Citas.AnyAsync(c => c.PerfilEstudianteId == perfilId && EstadosCitaActivos.Contains(c.Estado), ct))
            throw new GestionSolicitudesException($"El estudiante tiene citas activas (solicitadas o confirmadas). Deben cancelarse o registrarse su atención antes de {operacion}.");
    }

    private void Finalizar(AsignacionEstudiantePsicologo asignacion)
    {
        // Respeta CK_AsignacionesEstudiantePsicologo_Fechas aunque el reloj del servidor retroceda.
        var ahora = Ahora;
        asignacion.FechaFinalizacionUtc = ahora < asignacion.FechaAsignacionUtc ? asignacion.FechaAsignacionUtc : ahora;
    }

    private void Auditar(string administradorId, string usuarioEstudianteId, string accion, string? motivo, object anteriores, object nuevos)
    {
        if (motivo?.Trim().Length > 500)
            throw new GestionSolicitudesException("El motivo no puede superar los 500 caracteres.");
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario
        {
            UsuarioAfectadoId = usuarioEstudianteId, AdministradorId = administradorId, Accion = accion,
            DatosAnteriores = JsonSerializer.Serialize(anteriores), DatosNuevos = JsonSerializer.Serialize(nuevos),
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(), FechaUtc = Ahora
        });
    }
}
