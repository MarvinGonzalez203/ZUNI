using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Services.MayoriaEdad;
using Zuni.Services.Privacidad;
namespace Zuni.Services.Agenda;

public sealed partial class AgendaCitasService
{
    private async Task<(string Id,string Rol)> Programador(ClaimsPrincipal actor,CancellationToken ct)
    {
        // Privacidad: el Director ya no programa, reprograma ni consulta citas individuales. Solo el
        // psicólogo asignado programa. Las ramas "Director" restantes solo describen citas históricas.
        return (await Psicologo(actor,ct),"Psicologo");
    }
    private async Task<string> ProfesionalProgramacion((string Id,string Rol) actor,Guid perfil,CancellationToken ct)
    {
        var psy=await Asignado(perfil,ct)??throw new AgendaOperacionException("El estudiante necesita un psicólogo vinculado y disponible.");
        if(actor.Rol=="Psicologo"&&actor.Id!=psy)throw new AgendaAccesoException();
        if(!await db.PerfilesEstudiante.AnyAsync(p=>p.Id==perfil&&p.Activo&&p.Usuario.IsActive&&db.UserRoles.Any(ur=>ur.UserId==p.UsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="ESTUDIANTE"))&&!db.UserRoles.Any(ur=>ur.UserId==p.UsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&(r.NormalizedName=="DIRECTOR"||r.NormalizedName=="ADMINISTRADOR"||r.NormalizedName=="CATEDRATICO"))),ct))throw new AgendaAccesoException();
        return psy;
    }
    private async Task<IReadOnlyList<EstudianteProgramable>> SeleccionProgramables((string Id,string Rol) actor,CancellationToken ct) =>
        await (from a in db.AsignacionesEstudiantePsicologo.AsNoTracking() join p in db.PerfilesEstudiante.AsNoTracking() on a.PerfilEstudianteId equals p.Id join u in db.Users.AsNoTracking() on a.PsicologoUsuarioId equals u.Id
            where a.FechaFinalizacionUtc==null&&p.Activo&&p.Usuario.IsActive&&u.IsActive&&(actor.Rol=="Director"||a.PsicologoUsuarioId==actor.Id)
            && db.UserRoles.Any(ur=>ur.UserId==p.UsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="ESTUDIANTE"))
            orderby p.Usuario.FullName select new EstudianteProgramable(p.Id,p.Usuario.FullName,u.FullName,db.VerificacionesMayoriaEdad.Any(v=>v.PerfilEstudianteId==p.Id&&v.Estado==Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Verificada))).ToListAsync(ct);
    public async Task<ProgramacionListado> ListarProgramacion(ClaimsPrincipal actor,CancellationToken ct)
    {
        var a=await Programador(actor,ct);var ahora=Ahora;
        return new(await (from c in db.Citas.AsNoTracking() join p in db.PerfilesEstudiante.AsNoTracking() on c.PerfilEstudianteId equals p.Id join u in db.Users.AsNoTracking() on c.PsicologoUsuarioId equals u.Id
            where EstadosActivos.Contains(c.Estado)&&(a.Rol=="Director"||c.PsicologoUsuarioId==a.Id)
            orderby c.FechaHoraInicioUtc select new CitaProgramadaResumen(c.Id,c.Revision,p.Usuario.FullName,u.FullName,c.FechaHoraInicioUtc,c.FechaHoraFinUtc,c.Estado,!c.EsImprevista&&c.FechaHoraInicioUtc>ahora)).Take(200).ToListAsync(ct));
    }
    public async Task<ProgramarCitaInput> PrepararProgramacion(ClaimsPrincipal actor,Guid? id,DateOnly? fecha,CancellationToken ct,string? psicologoId=null)
    {
        var a=await Programador(actor,ct);var input=new ProgramarCitaInput{Fecha=fecha??AgendaReglas.Hoy(Ahora).AddDays(1),Estudiantes=await SeleccionProgramables(a,ct)};
        if(a.Rol == "Director" && psicologoId is not null) {
            var ids = await db.AsignacionesEstudiantePsicologo.Where(x => x.PsicologoUsuarioId == psicologoId && x.FechaFinalizacionUtc == null).Select(x => x.PerfilEstudianteId).ToListAsync(ct);
            input.Estudiantes = input.Estudiantes.Where(x => ids.Contains(x.Id)).ToList();
        }
        if(id is null)return input;
        var c=await db.Citas.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==id&&(a.Rol=="Director"||c.PsicologoUsuarioId==a.Id),ct)??throw new AgendaAccesoException();
        if(c.EsImprevista||!AgendaReglas.Activa(c.Estado)||c.FechaHoraInicioUtc<=Ahora)throw new AgendaOperacionException("Solo se reprograman citas activas futuras; las atenciones pasadas conservan su historial.");
        await ProfesionalProgramacion(a,c.PerfilEstudianteId,ct);
        input.DireccionAtencion=c.DireccionAtencion;input.CitaId=c.Id;input.Revision=c.Revision;input.PerfilEstudianteId=c.PerfilEstudianteId;input.Fecha=AgendaReglas.Hoy(c.FechaHoraInicioUtc);input.HoraInicio=TimeOnly.FromDateTime(AgendaReglas.Local(c.FechaHoraInicioUtc));input.HoraFin=TimeOnly.FromDateTime(AgendaReglas.Local(c.FechaHoraFinUtc));input.DuracionMinutos=(int)Math.Clamp((c.FechaHoraFinUtc-c.FechaHoraInicioUtc).TotalMinutes,15,120);input.Modalidad=c.Modalidad;return input;
    }
    private async Task ValidarFranjaProgramada(Guid perfil,string psy,DateTime inicio,DateTime fin,Guid? except,CancellationToken ct)
    {
        await ValidarAlmuerzo(psy,inicio,fin,ct);
        var fecha=AgendaReglas.FechaPermitida(AgendaReglas.Hoy(inicio),Ahora);
        var desde=AgendaReglas.Utc(fecha,TimeOnly.MinValue);var hasta=AgendaReglas.Utc(fecha.AddDays(1),TimeOnly.MinValue);
        if(inicio<=Ahora||fin<=inicio||AgendaReglas.Hoy(fin)!=fecha||TimeOnly.FromDateTime(AgendaReglas.Local(inicio))<new TimeOnly(8,0)||TimeOnly.FromDateTime(AgendaReglas.Local(fin))>new TimeOnly(18,0))throw new AgendaOperacionException("Usa una franja futura entre las 08:00 y 18:00 del mismo día.");
        if(await db.ConfiguracionesAgendaDia.AnyAsync(d=>d.PsicologoUsuarioId==psy&&d.Fecha==fecha&&d.Bloqueado,ct))throw new AgendaOperacionException("El psicólogo tiene ese día bloqueado.");
        if(await db.Citas.AnyAsync(c=>c.Id!=except&&EstadosActivos.Contains(c.Estado)&&((c.PerfilEstudianteId==perfil&&c.FechaHoraInicioUtc>=desde&&c.FechaHoraInicioUtc<hasta)||((c.PsicologoUsuarioId==psy||c.PerfilEstudianteId==perfil)&&c.FechaHoraInicioUtc<fin&&c.FechaHoraFinUtc>inicio)),ct))throw new AgendaOperacionException("Hay una cita activa o superpuesta para el estudiante o profesional. Elige otra franja.");
    }
    private async Task NotificarProgramacion(Cita c,(string Id,string Rol) actor,bool cambio,CancellationToken ct)
    {
        var user=await db.PerfilesEstudiante.Where(p=>p.Id==c.PerfilEstudianteId).Select(p=>p.UsuarioId).SingleAsync(ct);
        var nombre=await db.Users.Where(u=>u.Id==actor.Id).Select(u=>u.FullName).SingleAsync(ct);
        var estudiante=await db.PerfilesEstudiante.Where(p=>p.Id==c.PerfilEstudianteId).Select(p=>p.Usuario.FullName).SingleAsync(ct);
        var texto=$"Cita {(cambio?"reprogramada":"programada")} por {actor.Rol}: {nombre}.\nEstudiante: {estudiante}.\nFecha: {AgendaReglas.Local(c.FechaHoraInicioUtc):dd/MM/yyyy HH:mm} (Guatemala).\nModalidad: {c.Modalidad}.\nEstado: {c.Estado}.";
        if(c.Modalidad==ModalidadCita.Virtual)texto+="\nAntes de la cita virtual: procura estar a solas, en un lugar privado y sin ruido. Consulta los consejos y el acceso individual desde tu cita en ZUNI.";
        db.NotificacionesCita.Add(new(){UsuarioId=user,CitaId=c.Id,Texto=texto,FechaUtc=Ahora,AutorUsuarioId=actor.Id,AutorRol=actor.Rol,Tipo=cambio?"Reprogramación":"Programación"});
        if(actor.Rol=="Director")db.NotificacionesCita.Add(new(){UsuarioId=c.PsicologoUsuarioId,CitaId=c.Id,Texto=texto,FechaUtc=Ahora,AutorUsuarioId=actor.Id,AutorRol=actor.Rol,Tipo=cambio?"Reprogramación":"Programación"});
        PrivacidadService.Auditar(db,user,actor.Id,cambio?"CITA_REPROGRAMADA":"CITA_PROGRAMADA_PROFESIONAL",reloj,c.Id);
    }
    private async Task<EstadoCita> EstadoProgramacion((string Id,string Rol) actor,Guid perfil,ModalidadCita modalidad,CancellationToken ct,string? direccion=null)
    {
        // Propuesta administrativa no sustituye la verificación clínica del estudiante.
        var p=await db.PerfilesEstudiante.Include(p=>p.Usuario).SingleAsync(p=>p.Id==perfil,ct);
        return actor.Rol=="Psicologo"&&modalidad==ModalidadCita.Presencial&&!string.IsNullOrWhiteSpace(direccion)&&!p.Usuario.DebeCambiarContrasena&&p.EstaCompleto&&await MayoriaEdadService.VerificadaAsync(db,perfil,ct)?EstadoCita.Confirmada:EstadoCita.Solicitada;
    }
    private async Task ValidarConfirmacionProgramada(Cita c,CancellationToken ct)
    {
        if(c.ProgramadorUsuarioId is null)return;
        var p=await db.PerfilesEstudiante.AsNoTracking().SingleAsync(p=>p.Id==c.PerfilEstudianteId,ct);
        if(!p.EstaCompleto||!await MayoriaEdadService.VerificadaAsync(db,p.Id,ct))throw new AgendaOperacionException("La cita propuesta requiere que el estudiante complete su perfil y la institución verifique su mayoría de edad antes de confirmarla.");
    }
    public async Task<Guid> GuardarProgramacion(ClaimsPrincipal actor,ProgramarCitaInput input,CancellationToken ct)
    {
        Guid resultado=Guid.Empty;
        await Escribir(async()=>{
            var a=await Programador(actor,ct);
            if(input.PerfilEstudianteId is null||input.Fecha is null||input.HoraInicio is null||!Enum.IsDefined(input.Modalidad)||input.DuracionMinutos is <15 or >120||input.DuracionMinutos%5!=0)throw new AgendaOperacionException("Completa estudiante, fecha, inicio, duración y modalidad.");
            var psy=await ProfesionalProgramacion(a,input.PerfilEstudianteId.Value,ct);var inicio=AgendaReglas.Utc(input.Fecha.Value,input.HoraInicio.Value);
            var fin=input.HoraFin is {}h?AgendaReglas.Utc(input.Fecha.Value,h):inicio.AddMinutes(input.DuracionMinutos);
            if(inicio.Ticks%(5*TimeSpan.TicksPerMinute)!=0||fin.Ticks%(5*TimeSpan.TicksPerMinute)!=0)throw new AgendaOperacionException("Usa horas en múltiplos de cinco minutos.");
            Cita c;
            if(input.CitaId is {}id){c=await db.Citas.SingleOrDefaultAsync(c=>c.Id==id&&(a.Rol=="Director"||c.PsicologoUsuarioId==a.Id),ct)??throw new AgendaAccesoException();if(c.PerfilEstudianteId!=input.PerfilEstudianteId||c.PsicologoUsuarioId!=psy)throw new AgendaAccesoException();if(c.EsImprevista||!AgendaReglas.Activa(c.Estado)||c.FechaHoraInicioUtc<=Ahora||c.Revision!=input.Revision)throw new AgendaOperacionException("La cita cambió o ya no se puede reprogramar. Abre su versión actual.");}
            else{if(input.OperacionId==Guid.Empty||await db.Citas.AnyAsync(c=>c.Id==input.OperacionId,ct))throw new AgendaOperacionException("Esta programación ya se guardó. Consulta tu agenda.");c=new(){Id=input.OperacionId,PerfilEstudianteId=input.PerfilEstudianteId.Value,PsicologoUsuarioId=psy,FechaCreacionUtc=Ahora};db.Citas.Add(c);}
            await ValidarFranjaProgramada(c.PerfilEstudianteId,psy,inicio,fin,input.CitaId,ct);
            if(a.Rol=="Psicologo"&&input.Modalidad==ModalidadCita.Presencial)c.DireccionAtencion=DireccionValida(input.DireccionAtencion??c.DireccionAtencion);
            if(input.Modalidad==ModalidadCita.Virtual)c.DireccionAtencion=null;
            c.FechaHoraInicioUtc=inicio;c.FechaHoraFinUtc=fin;c.FinAproximado=true;c.Modalidad=input.Modalidad;c.ProgramadaDirectamente=true;c.ProgramadorUsuarioId=a.Id;c.ProgramadorRol=a.Rol;c.DisponibilidadPsicologoId=null;c.Estado=await EstadoProgramacion(a,c.PerfilEstudianteId,c.Modalidad,ct,c.DireccionAtencion);c.Revision=Guid.NewGuid();c.EnlaceReunion=null;c.FechaEnlaceUtc=null;
            await NotificarProgramacion(c,a,input.CitaId is not null,ct);resultado=c.Id;
        },ct);return resultado;
    }
    // Las notificaciones de cita identifican al estudiante. Una cuenta institucional sin rol de atención
    // (Director sin rol Psicólogo/Estudiante) no las consulta, aunque existan avisos históricos.
    public static bool SinAccesoANotificaciones(ClaimsPrincipal actor) =>
        actor.IsInRole("Director") && !actor.IsInRole("Psicologo") && !actor.IsInRole("Estudiante");
    public async Task<IReadOnlyList<NotificacionResumen>> Notificaciones(ClaimsPrincipal actor,CancellationToken ct)
    {
        if(SinAccesoANotificaciones(actor))throw new AgendaAccesoException();
        var id=await PrivacidadService.UsuarioAsync(db,actor,ct);
        return await (from n in db.NotificacionesCita.AsNoTracking() join c in db.Citas.AsNoTracking() on n.CitaId equals c.Id
            join p in db.PerfilesEstudiante on c.PerfilEstudianteId equals p.Id join psy in db.Users on c.PsicologoUsuarioId equals psy.Id
            where n.UsuarioId==id orderby n.FechaUtc descending
            select new NotificacionResumen(n.Id,n.Texto,n.FechaUtc,n.Leida,n.CitaId,c.FechaHoraInicioUtc){Estudiante=p.Usuario.FullName,Carne=p.Carne,Psicologo=psy.FullName,PsicologoId=psy.Id,
                Autor=db.Users.Where(u=>u.Id==(n.AutorUsuarioId??c.ProgramadorUsuarioId)).Select(u=>u.FullName).FirstOrDefault(),AutorRol=n.AutorRol??c.ProgramadorRol,
                AutorOriginal=n.AutorUsuarioId!=null,Tipo=n.Tipo??"Aviso",EstadoActual=c.Estado,Modalidad=c.Modalidad}).Take(100).ToListAsync(ct);
    }
    public async Task LeerNotificacion(ClaimsPrincipal actor,Guid id,CancellationToken ct)
    {if(SinAccesoANotificaciones(actor))throw new AgendaAccesoException();var user=await PrivacidadService.UsuarioAsync(db,actor,ct);var n=await db.NotificacionesCita.SingleOrDefaultAsync(n=>n.Id==id&&n.UsuarioId==user,ct)??throw new AgendaAccesoException();n.Leida=true;await db.SaveChangesAsync(ct);}
}
