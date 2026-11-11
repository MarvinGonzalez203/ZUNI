using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Zuni.Models.Administrador;
using Zuni.Models.Atencion;

namespace Zuni.Services.Administracion;

public sealed partial class GestionSolicitudesService
{
    private async Task<bool> FichaPsicologo(string usuarioId, CancellationToken ct)
    {
        var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == usuarioId select r.NormalizedName).ToListAsync(ct);
        if (roles.Any(r => r is "SUPERADMIN" or "ADMINISTRADOR" or "DIRECTOR" or "CATEDRATICO"))
            throw new GestionSolicitudesAccesoException();
        if (roles.Contains("PSICOLOGO")) { await ValidarPsicologo(usuarioId, ct); return true; }
        if (!roles.Contains("ESTUDIANTE")) throw new GestionSolicitudesAccesoException();
        return false;
    }

    public async Task<AsignacionesFichaViewModel> LeerFichaAsignaciones(string? administradorId, string usuarioId,
        string? buscar, int pagina, CancellationToken ct)
    {
        if (!await AdministradorActivo(administradorId, ct)) throw new GestionSolicitudesAccesoException();
        var desdePsicologo = await FichaPsicologo(usuarioId, ct);
        var nombre = await db.Users.Where(u => u.Id == usuarioId && u.IsActive).Select(u => u.FullName).SingleOrDefaultAsync(ct)
            ?? throw new GestionSolicitudesException("La cuenta debe estar activa para gestionar asignaciones.");
        var texto = (buscar ?? "").Trim();
        if (texto.Length > 80) throw new GestionSolicitudesException("La búsqueda admite hasta 80 caracteres.");
        var consulta = from p in db.PerfilesEstudiante.AsNoTracking()
            join u in db.Users on p.UsuarioId equals u.Id
            where p.Activo && u.IsActive && (desdePsicologo || u.Id == usuarioId) &&
                db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")) &&
                !db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId &&
                    (r.NormalizedName == "SUPERADMIN" || r.NormalizedName == "ADMINISTRADOR" || r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO")))
            select new { p.Id, p.UsuarioId, u.FullName, p.Carne };
        if (texto.Length > 0)
        {
            var q = texto.ToLower(); var carne = texto.Replace("-", "");
            consulta = consulta.Where(p => p.FullName.ToLower().Contains(q) || p.Carne.Contains(carne));
        }
        var paginas = Math.Max(1, (await consulta.CountAsync(ct) + TamanoPagina - 1) / TamanoPagina);
        pagina = Math.Clamp(pagina, 1, paginas);
        var alumnos = await consulta.OrderBy(p => p.FullName).ThenBy(p => p.Id).Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToListAsync(ct);
        var ids = alumnos.Select(p => p.Id).ToArray();
        var vigentes = await (from a in db.AsignacionesEstudiantePsicologo.AsNoTracking()
            join u in db.Users on a.PsicologoUsuarioId equals u.Id
            where ids.Contains(a.PerfilEstudianteId) && a.FechaFinalizacionUtc == null
            select new { a.Id, a.PerfilEstudianteId, a.PsicologoUsuarioId, u.FullName }).ToDictionaryAsync(a => a.PerfilEstudianteId, ct);
        return new AsignacionesFichaViewModel {
            UsuarioId = usuarioId, Nombre = nombre, DesdePsicologo = desdePsicologo, Buscar = texto, Pagina = pagina, Paginas = paginas,
            Psicologos = desdePsicologo ? [] : await PsicologosElegibles().OrderBy(u => u.FullName)
                .Select(u => new SelectListItem(u.FullName, u.Id)).ToListAsync(ct),
            Estudiantes = alumnos.Select(p => { vigentes.TryGetValue(p.Id, out var a); return new FilaAsignacionFicha(p.Id, p.UsuarioId,
                p.FullName, p.Carne, a?.Id, a?.PsicologoUsuarioId, a?.FullName); }).ToArray()
        };
    }

    public Task AsignarDesdeFicha(string? administradorId, AsignacionFichaInput input, CancellationToken ct) =>
        Escribir(administradorId, async adminId =>
        {
            var desdePsicologo = await FichaPsicologo(input.UsuarioId, ct);
            await ValidarEstudiante(input.PerfilEstudianteId, ct);
            var usuarioEstudiante = await db.PerfilesEstudiante.Where(p => p.Id == input.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
            if ((desdePsicologo && input.UsuarioId != input.PsicologoUsuarioId) || (!desdePsicologo && input.UsuarioId != usuarioEstudiante))
                throw new GestionSolicitudesAccesoException();
            // Tampoco admitir estudiantes con roles institucionales en un POST manipulado.
            if (await FichaPsicologo(usuarioEstudiante, ct)) throw new GestionSolicitudesAccesoException();
            var psicologo = await ValidarPsicologo(input.PsicologoUsuarioId, ct);
            var anterior = await Vigente(input.PerfilEstudianteId, ct);
            if (anterior?.Id != input.AsignacionActualId)
                throw new GestionSolicitudesException("La asignación cambió. Recarga la página antes de volver a guardar.");
            if (anterior?.PsicologoUsuarioId == psicologo)
                throw new GestionSolicitudesException("El estudiante ya está asignado a este psicólogo.");
            if (anterior is not null)
            {
                if (string.IsNullOrWhiteSpace(input.Motivo)) throw new GestionSolicitudesException("Indica un motivo administrativo para reasignar.");
                await ValidarSinCitasActivas(input.PerfilEstudianteId, "reasignar", ct);
                Finalizar(anterior);
                await db.SaveChangesAsync(ct); // liberar índice único dentro de la misma transacción
            }
            var nueva = new AsignacionEstudiantePsicologo { PerfilEstudianteId = input.PerfilEstudianteId,
                PsicologoUsuarioId = psicologo, FechaAsignacionUtc = Ahora };
            db.AsignacionesEstudiantePsicologo.Add(nueva);
            var solicitud = await db.SolicitudesAtencion.SingleOrDefaultAsync(s => s.PerfilEstudianteId == input.PerfilEstudianteId &&
                (s.Estado == EstadoSolicitudAtencion.Pendiente || s.Estado == EstadoSolicitudAtencion.Asignada), ct);
            if (solicitud is not null) solicitud.Estado = EstadoSolicitudAtencion.Asignada;
            // Vincular cuentas no crea una solicitud, consentimiento ni una verificación de edad.
            Auditar(adminId, usuarioEstudiante, anterior is null ? "ASIGNACION_DESDE_FICHA" : "REASIGNACION_DESDE_FICHA", input.Motivo,
                new { AsignacionId = anterior?.Id, PsicologoUsuarioId = anterior?.PsicologoUsuarioId },
                new { AsignacionId = nueva.Id, PsicologoUsuarioId = psicologo, SolicitudId = solicitud?.Id });
        }, ct);

    public Task FinalizarDesdeFicha(string? administradorId, AsignacionFichaInput input, CancellationToken ct) =>
        Escribir(administradorId, async adminId =>
        {
            var desdePsicologo = await FichaPsicologo(input.UsuarioId, ct);
            var estudiante = await db.PerfilesEstudiante.Where(p => p.Id == input.PerfilEstudianteId).Select(p => p.UsuarioId).SingleOrDefaultAsync(ct)
                ?? throw new GestionSolicitudesException("El estudiante no existe.");
            var actual = await Vigente(input.PerfilEstudianteId, ct);
            if (actual is null || actual.Id != input.AsignacionActualId)
                throw new GestionSolicitudesException("La asignación cambió. Recarga la página antes de volver a guardar.");
            if ((desdePsicologo && actual.PsicologoUsuarioId != input.UsuarioId) || (!desdePsicologo && estudiante != input.UsuarioId))
                throw new GestionSolicitudesAccesoException();
            if (string.IsNullOrWhiteSpace(input.Motivo)) throw new GestionSolicitudesException("Indica un motivo administrativo para finalizar la asignación.");
            await ValidarSinCitasActivas(input.PerfilEstudianteId, "finalizar la asignación", ct);
            Finalizar(actual);
            var solicitud = await db.SolicitudesAtencion.SingleOrDefaultAsync(s => s.PerfilEstudianteId == input.PerfilEstudianteId && s.Estado == EstadoSolicitudAtencion.Asignada, ct);
            if (solicitud is not null) solicitud.Estado = EstadoSolicitudAtencion.Pendiente;
            Auditar(adminId, estudiante, AccionesAuditoria.AsignacionFinalizada, input.Motivo,
                new { AsignacionId = actual.Id, PsicologoUsuarioId = actual.PsicologoUsuarioId }, new { Finalizada = true });
        }, ct);
}
