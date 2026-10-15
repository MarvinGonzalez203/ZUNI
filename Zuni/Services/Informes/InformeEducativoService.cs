using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Helpers;
using Zuni.Models.Informes;
using Zuni.Services.MayoriaEdad;
using Zuni.Services.Privacidad;

namespace Zuni.Services.Informes;

public static class InformeEducativoService
{
    private static async Task<string> Psicologo(ApplicationDbContext db, ClaimsPrincipal actor, CancellationToken ct) =>
        await new AutorizacionClinicaService(db).ObtenerPsicologoAutorizadoAsync(actor, ct) ?? throw new UnauthorizedAccessException();
    private static async Task<InformeEducativo> Autor(ApplicationDbContext db, ClaimsPrincipal actor, VersionInformeEducativo v, CancellationToken ct)
    {
        var id = await Psicologo(db, actor, ct);
        var root = await db.InformesEducativos.SingleOrDefaultAsync(r => r.Id == v.InformeId && r.AutorUsuarioId == id, ct) ?? throw new UnauthorizedAccessException();
        if (!await new AutorizacionClinicaService(db).PuedeConsultarExpedienteAsync(actor, root.SolicitudAtencionId, ct)) throw new UnauthorizedAccessException();
        var perfil = await db.SolicitudesAtencion.Where(s => s.Id == root.SolicitudAtencionId).Select(s => s.PerfilEstudianteId).SingleAsync(ct);
        if (!await db.PerfilesEstudiante.AnyAsync(p => p.Id == perfil && p.Activo && p.Usuario.IsActive, ct)) throw new UnauthorizedAccessException();
        return root;
    }
    private static Task<bool> DestinatarioValido(ApplicationDbContext db, string id, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id == id && u.IsActive && !u.DebeCambiarContrasena &&
            db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "DIRECTOR")), ct);

    public static async Task<IReadOnlyList<InformeResumen>> ListarAsync(ApplicationDbContext db, ClaimsPrincipal actor, CancellationToken ct)
    {
        var id = await PrivacidadService.UsuarioAsync(db, actor, ct);
        var psy = await new AutorizacionClinicaService(db).ObtenerPsicologoAutorizadoAsync(actor, ct);
        IQueryable<VersionInformeEducativo> q = db.VersionesInformesEducativos.AsNoTracking();
        if (psy is not null)
            q = q.Where(v => db.InformesEducativos.Any(r => r.Id == v.InformeId && r.AutorUsuarioId == id &&
                db.SolicitudesAtencion.Any(s => s.Id == r.SolicitudAtencionId && db.ConsentimientosAtencion.Any(c => c.SolicitudAtencionId == s.Id && c.Aceptado) &&
                    db.AsignacionesEstudiantePsicologo.Any(a => a.PerfilEstudianteId == s.PerfilEstudianteId && a.PsicologoUsuarioId == id && a.FechaFinalizacionUtc == null) &&
                    db.PerfilesEstudiante.Any(p => p.Id == s.PerfilEstudianteId && p.Activo && p.Usuario.IsActive))));
        else if (await MayoriaEdadService.ActorAsync(db, actor, "Estudiante", ct))
            q = q.Where(v => v.Estado != EstadoInforme.Borrador && db.AutorizacionesInformes.Any(a => a.VersionInformeId == v.Id && a.EstudianteUsuarioId == id));
        else if (await MayoriaEdadService.ActorAsync(db, actor, "Director", ct))
            q = q.Where(v => v.DestinatarioUsuarioId == id && v.Estado == EstadoInforme.Emitida && db.AutorizacionesInformes.Any(a => a.VersionInformeId == v.Id && a.Aceptada == true && a.FechaRevocacionUtc == null &&
                db.Users.Any(u => u.Id == a.EstudianteUsuarioId && u.IsActive)));
        else throw new UnauthorizedAccessException();
        return await q.OrderByDescending(v => v.FechaEmisionUtc).ThenByDescending(v => v.Numero).Take(200)
            .Select(v => new InformeResumen(v.Id, v.InformeId, v.Numero, v.Estudiante, v.Objetivo, v.Estado)).ToListAsync(ct);
    }
    public static async Task<DetalleInforme> DetalleAsync(ApplicationDbContext db, ClaimsPrincipal actor, Guid id, TimeProvider reloj, CancellationToken ct, bool auditar = true)
    {
        var userId = await PrivacidadService.UsuarioAsync(db, actor, ct);
        var v = await db.VersionesInformesEducativos.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new UnauthorizedAccessException();
        var a = await db.AutorizacionesInformes.AsNoTracking().SingleOrDefaultAsync(a => a.VersionInformeId == id, ct);
        var root = await db.InformesEducativos.AsNoTracking().SingleAsync(r => r.Id == v.InformeId, ct);
        var autor = false;
        if (root.AutorUsuarioId == userId && await new AutorizacionClinicaService(db).ObtenerPsicologoAutorizadoAsync(actor, ct) is not null)
        { await Autor(db, actor, v, ct); autor = true; }
        var estudiante = a?.EstudianteUsuarioId == userId && await MayoriaEdadService.ActorAsync(db, actor, "Estudiante", ct) && v.Estado != EstadoInforme.Borrador;
        var director = v.DestinatarioUsuarioId == userId && await MayoriaEdadService.ActorAsync(db, actor, "Director", ct)
            && v.Estado == EstadoInforme.Emitida && a?.Aceptada == true && a.FechaRevocacionUtc is null
            && await db.Users.AnyAsync(u => u.Id == a.EstudianteUsuarioId && u.IsActive, ct);
        if (!autor && !estudiante && !director) throw new UnauthorizedAccessException();
        if (auditar) { PrivacidadService.Auditar(db, a?.EstudianteUsuarioId ?? root.AutorUsuarioId, userId, "INFORME_EDUCATIVO_CONSULTADO", reloj, id); await db.SaveChangesAsync(ct); }
        return new(v, a, autor, estudiante) { DestinatarioVigente = await DestinatarioValido(db, v.DestinatarioUsuarioId!, ct) };
    }

    public static async Task<Guid> GuardarAsync(ApplicationDbContext db, ClaimsPrincipal actor, EditarInformeInput input, bool solicitar, TimeProvider reloj, CancellationToken ct)
    {
        PrivacidadService.Validar(input);
        if (solicitar && input.IncluyeAnalisisRendimiento is null)
            throw new DatosOperacionException("Indica si corresponde analizar el rendimiento académico antes de solicitar autorización.");
        if (solicitar && input.IncluyeAnalisisRendimiento == true &&
            (string.IsNullOrWhiteSpace(input.RelatoRendimiento) || string.IsNullOrWhiteSpace(input.ValoracionRendimiento) || !input.ContrasteRendimientoConfirmado))
            throw new DatosOperacionException("Completa el relato del estudiante, tu valoración educativa y la confirmación del contraste de información.");
        if (input.PeriodoFin < input.PeriodoInicio || input.PeriodoFin > MayoriaEdadService.Hoy(reloj) || input.FechaSeguimiento < input.PeriodoFin)
            throw new DatosOperacionException("Revisa las fechas: el periodo observado debe terminar después de su inicio y no puede ser futuro; el seguimiento no puede precederlo.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var psy = await Psicologo(db, actor, ct);
        VersionInformeEducativo v; InformeEducativo root;
        if (input.Id is { } versionId)
        {
            v = await db.VersionesInformesEducativos.SingleOrDefaultAsync(v => v.Id == versionId, ct) ?? throw new UnauthorizedAccessException();
            root = await Autor(db, actor, v, ct);
            if (v.Revision != input.Revision) throw new DbUpdateConcurrencyException();
            if (v.Estado != EstadoInforme.Borrador) throw new DatosOperacionException("Esta versión está cerrada para edición. Crea una nueva versión para corregirla.");
        }
        else
        {
            if (!await new AutorizacionClinicaService(db).PuedeConsultarExpedienteAsync(actor, input.SolicitudAtencionId, ct)) throw new UnauthorizedAccessException();
            root = new() { SolicitudAtencionId = input.SolicitudAtencionId, AutorUsuarioId = psy, FechaCreacionUtc = reloj.GetUtcNow().UtcDateTime };
            db.InformesEducativos.Add(root); v = new() { InformeId = root.Id, Numero = 1 }; db.VersionesInformesEducativos.Add(v);
        }
        var perfilId = await db.SolicitudesAtencion.Where(s => s.Id == root.SolicitudAtencionId).Select(s => s.PerfilEstudianteId).SingleAsync(ct);
        var perfil = await db.PerfilesEstudiante.Include(p => p.Usuario).SingleOrDefaultAsync(p => p.Id == perfilId && p.Activo && p.Usuario.IsActive, ct) ?? throw new UnauthorizedAccessException();
        if (!await MayoriaEdadService.VerificadaAsync(db, perfil.Id, ct)) throw new DatosOperacionException("La mayoría de edad debe estar verificada antes de preparar el informe.");
        if (!await DestinatarioValido(db, input.DestinatarioUsuarioId, ct)) throw new DatosOperacionException("Selecciona un Director activo disponible como destinatario.");
        v.Objetivo = input.Objetivo.Trim(); v.PeriodoInicio = input.PeriodoInicio!.Value; v.PeriodoFin = input.PeriodoFin!.Value;
        v.Fuentes = input.Fuentes.Trim(); v.Observaciones = input.Observaciones.Trim(); v.Necesidades = input.Necesidades.Trim();
        v.Recomendaciones = input.Recomendaciones.Trim(); v.ResponsablesAcciones = input.ResponsablesAcciones.Trim(); v.FechaSeguimiento = input.FechaSeguimiento!.Value;
        v.IncluyeAnalisisRendimiento = input.IncluyeAnalisisRendimiento;
        v.RelatoRendimiento = input.IncluyeAnalisisRendimiento == true ? (input.RelatoRendimiento ?? "").Trim() : "";
        v.ValoracionRendimiento = input.IncluyeAnalisisRendimiento == true ? (input.ValoracionRendimiento ?? "").Trim() : "";
        v.ContrasteRendimientoConfirmado = input.IncluyeAnalisisRendimiento == true && input.ContrasteRendimientoConfirmado;
        v.DestinatarioUsuarioId = input.DestinatarioUsuarioId; v.Estudiante = perfil.Usuario.FullName; v.Carne = CarneHelper.Formatear(perfil.Carne)!;
        v.Psicologo = await db.Users.Where(u => u.Id == psy).Select(u => u.FullName).SingleAsync(ct);
        v.Destinatario = await db.Users.Where(u => u.Id == v.DestinatarioUsuarioId).Select(u => u.FullName).SingleAsync(ct);
        v.Institucion = (await Zuni.Services.Institucion.ConfiguracionInstitucionService.ObtenerAsync(db, ct)).Nombre;
        v.Revision = Guid.NewGuid();
        if (solicitar)
        {
            var politica = await PrivacidadService.VigenteAsync(db, ct) ?? throw new DatosOperacionException("La institución debe publicar su política de privacidad antes de solicitar autorización.");
            v.Estado = EstadoInforme.PendienteAutorizacion;
            db.AutorizacionesInformes.Add(new() { VersionInformeId = v.Id, EstudianteUsuarioId = perfil.UsuarioId, PoliticaPrivacidadId = politica.Id,
                FechaSolicitudUtc = reloj.GetUtcNow().UtcDateTime,
                TextoConsentimiento = $"Autorizo compartir exclusivamente el informe educativo versión {v.Numero}, preparado por {v.Psicologo}, con {v.Destinatario}, para esta finalidad: {v.Objetivo}. La decisión es voluntaria; rechazarla no impide recibir atención. Puedo revocar el acceso en ZUNI, pero eso no elimina copias descargadas previamente. No autoriza acceso al expediente, diagnósticos ni respuestas de pruebas." });
        }
        PrivacidadService.Auditar(db, perfil.UsuarioId, psy, solicitar ? "INFORME_AUTORIZACION_SOLICITADA" : "INFORME_BORRADOR_GUARDADO", reloj, v.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return v.Id;
    }

    public static async Task DecidirAsync(ApplicationDbContext db, ClaimsPrincipal actor, Guid id, Guid revision, bool? aceptada, bool confirmada, bool revocar, TimeProvider reloj, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var userId = await PrivacidadService.UsuarioAsync(db, actor, ct);
        if (!await MayoriaEdadService.ActorAsync(db, actor, "Estudiante", ct)) throw new UnauthorizedAccessException();
        var a = await db.AutorizacionesInformes.SingleOrDefaultAsync(a => a.VersionInformeId == id && a.EstudianteUsuarioId == userId, ct) ?? throw new UnauthorizedAccessException();
        var v = await db.VersionesInformesEducativos.SingleAsync(v => v.Id == id, ct);
        if (v.Revision != revision) throw new DbUpdateConcurrencyException();
        if (!confirmada) throw new DatosOperacionException("Confirma tu decisión sobre esta versión del informe.");
        if (revocar)
        {
            if (a.Aceptada != true || a.FechaRevocacionUtc is not null) throw new DatosOperacionException("Esta versión no tiene una autorización vigente para revocar.");
            a.FechaRevocacionUtc = reloj.GetUtcNow().UtcDateTime; v.Estado = EstadoInforme.Revocada;
        }
        else
        {
            if (v.Estado != EstadoInforme.PendienteAutorizacion || a.Aceptada.HasValue || aceptada is null) throw new DatosOperacionException("Selecciona una decisión para una autorización pendiente.");
            a.Aceptada = aceptada; a.FechaDecisionUtc = reloj.GetUtcNow().UtcDateTime;
            v.Estado = aceptada == true ? EstadoInforme.Autorizada : EstadoInforme.Rechazada;
        }
        v.Revision = Guid.NewGuid(); PrivacidadService.Auditar(db, userId, userId, revocar ? "INFORME_AUTORIZACION_REVOCADA" : aceptada == true ? "INFORME_AUTORIZADO" : "INFORME_RECHAZADO", reloj, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public static async Task EmitirAsync(ApplicationDbContext db, ClaimsPrincipal actor, Guid id, Guid revision, bool confirmada, TimeProvider reloj, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var v = await db.VersionesInformesEducativos.SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new UnauthorizedAccessException();
        var root = await Autor(db, actor, v, ct);
        if (v.Revision != revision) throw new DbUpdateConcurrencyException();
        var a = await db.AutorizacionesInformes.SingleOrDefaultAsync(a => a.VersionInformeId == id, ct);
        if (v.IncluyeAnalisisRendimiento == true &&
            (!v.ContrasteRendimientoConfirmado || string.IsNullOrWhiteSpace(v.RelatoRendimiento) || string.IsNullOrWhiteSpace(v.ValoracionRendimiento)))
            throw new DatosOperacionException("Esta versión necesita revisión del análisis de rendimiento. Prepara una nueva versión y solicita nuevamente autorización.");
        if (!confirmada || v.Estado != EstadoInforme.Autorizada || a?.Aceptada != true || a.FechaRevocacionUtc is not null)
            throw new DatosOperacionException("Revisa el contenido y confirma la emisión de una versión autorizada por el estudiante.");
        if (!await DestinatarioValido(db, v.DestinatarioUsuarioId!, ct) || !await db.Users.AnyAsync(u => u.Id == a.EstudianteUsuarioId && u.IsActive, ct))
            throw new DatosOperacionException("El destinatario o el estudiante ya no están disponibles.");
        var perfil = await db.SolicitudesAtencion.Where(s => s.Id == root.SolicitudAtencionId).Select(s => s.PerfilEstudianteId).SingleAsync(ct);
        if (!await MayoriaEdadService.VerificadaAsync(db, perfil, ct)) throw new DatosOperacionException("La mayoría de edad del estudiante debe continuar verificada para emitir el informe.");
        var anteriores = await db.VersionesInformesEducativos.Where(x => x.InformeId == v.InformeId && x.Estado == EstadoInforme.Emitida).ToListAsync(ct);
        v.SustituyeNumero = anteriores.Select(x => (int?)x.Numero).SingleOrDefault();
        foreach (var old in anteriores) { old.Estado = EstadoInforme.Sustituida; old.Revision = Guid.NewGuid(); }
        if (anteriores.Count > 0) await db.SaveChangesAsync(ct);
        v.Estado = EstadoInforme.Emitida; v.FechaEmisionUtc = reloj.GetUtcNow().UtcDateTime; v.Revision = Guid.NewGuid();
        PrivacidadService.Auditar(db, a.EstudianteUsuarioId, root.AutorUsuarioId, "INFORME_EDUCATIVO_EMITIDO", reloj, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public static async Task<Guid> NuevaVersionAsync(ApplicationDbContext db, ClaimsPrincipal actor, Guid id, Guid revision, TimeProvider reloj, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var v = await db.VersionesInformesEducativos.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new UnauthorizedAccessException();
        var root = await Autor(db, actor, v, ct);
        if (v.Revision != revision) throw new DbUpdateConcurrencyException();
        var abiertas=await db.VersionesInformesEducativos.Where(x=>x.InformeId==root.Id && (x.Estado==EstadoInforme.Borrador || x.Estado==EstadoInforme.PendienteAutorizacion || x.Estado==EstadoInforme.Autorizada)).ToListAsync(ct);
        if(abiertas.Count>0){
            if(abiertas.Count==1 && abiertas[0].Id==v.Id && (v.Estado is EstadoInforme.PendienteAutorizacion or EstadoInforme.Autorizada) && !await DestinatarioValido(db,v.DestinatarioUsuarioId!,ct)){
                abiertas[0].Estado=EstadoInforme.Sustituida;abiertas[0].Revision=Guid.NewGuid();
                PrivacidadService.Auditar(db,root.AutorUsuarioId,root.AutorUsuarioId,"INFORME_DESTINATARIO_RETIRADO_VERSION_CERRADA",reloj,v.Id);
                await db.SaveChangesAsync(ct);
            }else throw new DatosOperacionException("Ya hay una versión en preparación; complétala o registra la decisión antes de crear otra.");
        }
        var number = await db.VersionesInformesEducativos.Where(x => x.InformeId == root.Id).MaxAsync(x => x.Numero, ct);
        if (number >= 50) throw new DatosOperacionException("Este informe alcanzó su límite de versiones. Coordina con la institución un nuevo informe.");
        v.Id = Guid.NewGuid(); v.Numero = number + 1; v.Revision = Guid.NewGuid(); v.Estado = EstadoInforme.Borrador; v.ContrasteRendimientoConfirmado = false; v.FechaEmisionUtc = null; v.SustituyeNumero = null;
        db.VersionesInformesEducativos.Add(v); PrivacidadService.Auditar(db, root.AutorUsuarioId, root.AutorUsuarioId, "INFORME_NUEVA_VERSION", reloj, v.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return v.Id;
    }
    public static async Task<VersionInformeEducativo> ParaPdfAsync(ApplicationDbContext db, ClaimsPrincipal actor, Guid id, TimeProvider reloj, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var d = await DetalleAsync(db, actor, id, reloj, ct, false);
        if (d.Version.Estado != EstadoInforme.Emitida || d.Autorizacion?.Aceptada != true || d.Autorizacion.FechaRevocacionUtc is not null || !d.DestinatarioVigente) throw new UnauthorizedAccessException();
        PrivacidadService.Auditar(db, d.Autorizacion.EstudianteUsuarioId, await PrivacidadService.UsuarioAsync(db, actor, ct), "INFORME_PDF_DESCARGADO", reloj, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return d.Version;
    }
}
