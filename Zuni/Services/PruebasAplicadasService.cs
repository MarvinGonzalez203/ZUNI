using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Pruebas;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
using Zuni.Services.MayoriaEdad;
using Zuni.Services.Privacidad;
namespace Zuni.Services;
public sealed class PruebasAplicadasService(ApplicationDbContext db,IAutorizacionClinicaService autorizacion,TimeProvider reloj)
{
    private async Task<string> Psicologo(ClaimsPrincipal actor,CancellationToken ct)=>await autorizacion.ObtenerPsicologoAutorizadoAsync(actor,ct)??throw new AgendaAccesoException();
    private async Task<Zuni.Models.PerfilEstudiante> Propio(ClaimsPrincipal actor,CancellationToken ct)=>await MayoriaEdadService.PropioAsync(db,actor,ct)??throw new AgendaAccesoException();
    private IQueryable<AsignacionPrueba> Vigentes(string psy)=>db.AsignacionesPrueba.Where(a=>a.RetiradaUtc==null&&db.PerfilesEstudiante.Any(p=>p.Id==a.PerfilEstudianteId&&p.Activo&&p.Usuario.IsActive)&&db.AsignacionesEstudiantePsicologo.Any(v=>v.PerfilEstudianteId==a.PerfilEstudianteId&&v.PsicologoUsuarioId==psy&&v.FechaFinalizacionUtc==null));
    private async Task<Prueba> Instrumento(Guid id,CancellationToken ct)=>await db.PruebasDirector.AsNoTracking().Include(p=>p.Dimensiones).Include(p=>p.Preguntas).ThenInclude(q=>q.Opciones).AsSplitQuery().SingleOrDefaultAsync(p=>p.Id==id,ct)??throw new AgendaAccesoException();
    private static bool Likert(Prueba p)=>p.Preguntas.Count>0&&p.Preguntas.All(q=>q.Opciones.Count==10&&q.Opciones.Select(o=>o.Valor).Order().SequenceEqual(Enumerable.Range(1,10)));
    public async Task<HabilitarPruebasViewModel> Preparar(ClaimsPrincipal actor,CancellationToken ct)
    {
        var psy=await Psicologo(actor,ct);
        var estudiantes=await (from a in db.AsignacionesEstudiantePsicologo join p in db.PerfilesEstudiante on a.PerfilEstudianteId equals p.Id
            where a.PsicologoUsuarioId==psy&&a.FechaFinalizacionUtc==null&&p.Activo&&p.Usuario.IsActive
            orderby p.Usuario.FullName select new SelectListItem(p.Usuario.FullName+" · "+p.Carne,p.Id.ToString())).ToListAsync(ct);
        var pruebas=await db.PruebasDirector.AsNoTracking().Where(p=>p.Activa).Include(p=>p.Preguntas).ThenInclude(q=>q.Opciones).OrderBy(p=>p.Nivel).ToListAsync(ct);
        return new(estudiantes,pruebas.Where(Likert).ToList());
    }
    public async Task Habilitar(ClaimsPrincipal actor,HabilitarPruebaInput input,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var psy=await Psicologo(actor,ct);
        if(!input.InduccionRealizada)throw new AgendaOperacionException("Primero explica al estudiante las instrucciones y la escala de respuesta.");
        if(!await db.AsignacionesEstudiantePsicologo.AnyAsync(a=>a.PerfilEstudianteId==input.PerfilEstudianteId&&a.PsicologoUsuarioId==psy&&a.FechaFinalizacionUtc==null,ct))throw new AgendaAccesoException();
        var perfil=await db.PerfilesEstudiante.Include(p=>p.Usuario).SingleAsync(p=>p.Id==input.PerfilEstudianteId,ct);
        if(!perfil.Activo||!perfil.Usuario.IsActive||perfil.Usuario.DebeCambiarContrasena||!perfil.EstaCompleto||!await MayoriaEdadService.VerificadaAsync(db,perfil.Id,ct))throw new AgendaOperacionException("El estudiante debe preparar su perfil, cuenta y verificación de edad.");
        if(!await db.UserRoles.AnyAsync(ur=>ur.UserId==perfil.UsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="ESTUDIANTE"),ct)||await db.UserRoles.AnyAsync(ur=>ur.UserId==perfil.UsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&(r.NormalizedName=="ADMINISTRADOR"||r.NormalizedName=="DIRECTOR"||r.NormalizedName=="CATEDRATICO")),ct))throw new AgendaAccesoException();
        var cita=await db.Citas.Where(c=>c.PerfilEstudianteId==perfil.Id&&c.PsicologoUsuarioId==psy&&c.Estado==EstadoCita.Completada&&c.FechaHoraFinUtc<=reloj.GetUtcNow().UtcDateTime&&db.RegistrosAtencionCita.Any(r=>r.CitaId==c.Id&&r.Asistio)).OrderBy(c=>c.FechaHoraInicioUtc).FirstOrDefaultAsync(ct)
            ??throw new AgendaOperacionException("Se necesita al menos una cita atendida y registrada con este psicólogo antes de habilitar la prueba.");
        var registro=await db.RegistrosAtencionCita.SingleAsync(r=>r.CitaId==cita.Id,ct);
        if(!await autorizacion.PuedeConsultarExpedienteAsync(actor,registro.SolicitudAtencionId,ct))throw new AgendaAccesoException();
        var prueba=await Instrumento(input.PruebaId,ct);if(!prueba.Activa||!Likert(prueba))throw new AgendaOperacionException("Activa una prueba con escala completa del 1 al 10 en todas sus preguntas.");
        var a=await db.AsignacionesPrueba.SingleOrDefaultAsync(a=>a.PruebaId==prueba.Id&&a.PerfilEstudianteId==perfil.Id,ct);
        if(a is null){a=new(){PruebaId=prueba.Id,PerfilEstudianteId=perfil.Id,DirectorUsuarioId=psy};db.AsignacionesPrueba.Add(a);}
        if(a.InduccionUtc is not null||a.RetiradaUtc is not null)throw new AgendaOperacionException("Esta prueba ya fue habilitada o retirada. Revisa la participación actual.");
        // Compartir el token con activación/edición impide habilitar contra una versión obsoleta.
        var actual=await db.PruebasDirector.SingleAsync(p=>p.Id==prueba.Id,ct);actual.Revision=Guid.NewGuid();
        a.PsicologoInductorId=psy;a.CitaInduccionId=cita.Id;a.InduccionUtc=reloj.GetUtcNow().UtcDateTime;a.Revision=Guid.NewGuid();
        PrivacidadService.Auditar(db,perfil.UsuarioId,psy,"PRUEBA_INDUCCION_REGISTRADA",reloj,a.Id);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<PruebaAplicacionResumen>> MisPruebas(ClaimsPrincipal actor,CancellationToken ct)
    {
        var p=await Propio(actor,ct);
        return await (from a in db.AsignacionesPrueba.AsNoTracking() join prueba in db.PruebasDirector on a.PruebaId equals prueba.Id where a.PerfilEstudianteId==p.Id
            orderby a.AsignadaUtc descending select new PruebaAplicacionResumen(a.Id,prueba.Nombre,prueba.Subtitulo,prueba.Preguntas.Count,a.InduccionUtc,a.FinalizadaUtc,a.RetiradaUtc){PruebaId=prueba.Id}).ToListAsync(ct);
    }
    public async Task<CuestionarioAplicado> Cuestionario(ClaimsPrincipal actor,Guid id,CancellationToken ct)
    {
        var p=await Propio(actor,ct);var a=await db.AsignacionesPrueba.SingleOrDefaultAsync(a=>a.Id==id&&a.PerfilEstudianteId==p.Id,ct)??throw new AgendaAccesoException();
        if(a.InduccionUtc is null||a.PsicologoInductorId is null||a.RetiradaUtc is not null)throw new AgendaOperacionException("Tu psicólogo debe indicar y explicar esta prueba después de la primera cita.");
        if(!await db.AsignacionesEstudiantePsicologo.AnyAsync(v=>v.PerfilEstudianteId==p.Id&&v.PsicologoUsuarioId==a.PsicologoInductorId&&v.FechaFinalizacionUtc==null,ct))throw new AgendaAccesoException();
        return new(a,await Instrumento(a.PruebaId,ct),await db.Set<RespuestaPrueba>().AsNoTracking().Where(r=>r.AsignacionPruebaId==id).ToListAsync(ct));
    }
    public async Task Responder(ClaimsPrincipal actor,ContestarPruebaInput input,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var c=await Cuestionario(actor,input.Id,ct);var p=await Propio(actor,ct);
        if(c.Asignacion.FinalizadaUtc is not null||c.Asignacion.Revision!=input.Revision)throw new AgendaOperacionException("La prueba ya terminó o cambió. Recarga antes de continuar.");
        if(!p.EstaCompleto||!await MayoriaEdadService.VerificadaAsync(db,p.Id,ct)||!await PrivacidadService.AceptadaAsync(db,p.UsuarioId,ct))throw new AgendaOperacionException("Completa tu perfil, la verificación de edad y la política de privacidad antes de responder.");
        if(!input.Consentimiento)throw new AgendaOperacionException("Confirma tu participación voluntaria y que comprendiste las instrucciones.");
        if(input.Respuestas.Count>200||input.Respuestas.Select(r=>r.PreguntaId).Distinct().Count()!=input.Respuestas.Count)throw new AgendaOperacionException("Las preguntas no pueden repetirse.");
        foreach(var r in input.Respuestas) {
            var q=c.Prueba.Preguntas.SingleOrDefault(q=>q.Id==r.PreguntaId)??throw new AgendaOperacionException("Pregunta ajena al instrumento.");
            if(r.OpcionId is not null&&!q.Opciones.Any(o=>o.Id==r.OpcionId&&o.Valor>=1&&o.Valor<=10))throw new AgendaOperacionException("Selecciona una opción del 1 al 10 de esta pregunta.");
        }
        if(input.Finalizar&&(input.Respuestas.Count!=c.Prueba.Preguntas.Count||input.Respuestas.Any(r=>r.OpcionId is null)))throw new AgendaOperacionException("Responde todas las preguntas antes de finalizar; puedes guardar un avance.");
        var anteriores=await db.Set<RespuestaPrueba>().Where(r=>r.AsignacionPruebaId==input.Id).ToListAsync(ct);
        foreach(var r in input.Respuestas) {
            var anterior=anteriores.SingleOrDefault(a=>a.PreguntaPruebaId==r.PreguntaId);
            if(r.OpcionId is null){if(anterior is not null)db.Remove(anterior);continue;}
            if(anterior is null)db.Add(new RespuestaPrueba{AsignacionPruebaId=input.Id,PreguntaPruebaId=r.PreguntaId,OpcionPruebaId=r.OpcionId.Value});else anterior.OpcionPruebaId=r.OpcionId.Value;
        }
        c.Asignacion.Revision=Guid.NewGuid();if(input.Finalizar)c.Asignacion.FinalizadaUtc=reloj.GetUtcNow().UtcDateTime;
        PrivacidadService.Auditar(db,p.UsuarioId,p.UsuarioId,input.Finalizar?"PRUEBA_FINALIZADA":"PRUEBA_AVANCE_GUARDADO",reloj,input.Id);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<(Guid Id,string Estudiante,string Prueba,DateTime? Fin)>> ListarResultados(ClaimsPrincipal actor,CancellationToken ct)
    {
        var psy=await Psicologo(actor,ct);var rows=await (from a in Vigentes(psy).AsNoTracking() join p in db.PerfilesEstudiante on a.PerfilEstudianteId equals p.Id join prueba in db.PruebasDirector on a.PruebaId equals prueba.Id
            where a.InduccionUtc!=null orderby a.AsignadaUtc descending select new {a.Id,Estudiante=p.Usuario.FullName,Prueba=prueba.Nombre,Fin=a.FinalizadaUtc}).ToListAsync(ct);
        return rows.Select(r=>(r.Id,r.Estudiante,r.Prueba,r.Fin)).ToList();
    }
    public async Task<ResumenAplicado> Resumen(ClaimsPrincipal actor,Guid id,CancellationToken ct)
    {
        var psy=await Psicologo(actor,ct);var a=await Vigentes(psy).AsNoTracking().SingleOrDefaultAsync(a=>a.Id==id&&a.FinalizadaUtc!=null,ct)??throw new AgendaAccesoException();
        var solicitud=await db.RegistrosAtencionCita.Where(r=>r.CitaId==a.CitaInduccionId).Select(r=>r.SolicitudAtencionId).SingleAsync(ct);
        if(!await autorizacion.PuedeConsultarExpedienteAsync(actor,solicitud,ct))throw new AgendaAccesoException();
        var prueba=await Instrumento(a.PruebaId,ct);var respuestas=await db.Set<RespuestaPrueba>().AsNoTracking().Where(r=>r.AsignacionPruebaId==id).ToListAsync(ct);
        var valores=respuestas.ToDictionary(r=>r.PreguntaPruebaId,r=>prueba.Preguntas.Single(q=>q.Id==r.PreguntaPruebaId).Opciones.Single(o=>o.Id==r.OpcionPruebaId).Valor);
        var dimensiones=prueba.Dimensiones.Select(d=>{var preguntas=prueba.Preguntas.Where(q=>q.DimensionId==d.Id).ToList();var v=preguntas.Where(q=>valores.ContainsKey(q.Id)).Select(q=>valores[q.Id]).ToArray();return new DimensionResumen(d.Nombre,v.Length,preguntas.Count,v.Length==0?null:Math.Round(v.Average(),2));}).ToList();
        var nombre=await db.PerfilesEstudiante.Where(p=>p.Id==a.PerfilEstudianteId).Select(p=>p.Usuario.FullName).SingleAsync(ct);
        PrivacidadService.Auditar(db,await db.PerfilesEstudiante.Where(p=>p.Id==a.PerfilEstudianteId).Select(p=>p.UsuarioId).SingleAsync(ct),psy,"PRUEBA_RESUMEN_CONSULTADO",reloj,id);await db.SaveChangesAsync(ct);
        return new(nombre,prueba,a.FinalizadaUtc,dimensiones,prueba.Preguntas.OrderBy(q=>q.Orden).Select(q=>(q.Texto,valores[q.Id])).ToList());
    }
    public async Task Retirar(ClaimsPrincipal actor,Guid id,CancellationToken ct) {
        var p=await Propio(actor,ct);var a=await db.AsignacionesPrueba.SingleOrDefaultAsync(a=>a.Id==id&&a.PerfilEstudianteId==p.Id,ct)??throw new AgendaAccesoException();
        a.RetiradaUtc=reloj.GetUtcNow().UtcDateTime;a.Revision=Guid.NewGuid();PrivacidadService.Auditar(db,p.UsuarioId,p.UsuarioId,"PRUEBA_PARTICIPACION_RETIRADA",reloj,id);await db.SaveChangesAsync(ct);
    }
}
