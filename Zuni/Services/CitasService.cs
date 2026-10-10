using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
namespace Zuni.Services;

public sealed class CitasService(ApplicationDbContext db,BigFiveService bigFive,TimeProvider clock)
{
    private DateTime UtcNow=>clock.GetUtcNow().UtcDateTime;
    public static DateTime Utc(DateOnly date,TimeOnly time)=>TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(time,DateTimeKind.Unspecified),TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala"));
    private Task<bool> ActorActivo(string id,string role,CancellationToken ct)=>db.Users.AnyAsync(u=>u.Id==id&&u.IsActive&&
        db.UserRoles.Any(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName==role)),ct);
    private async Task Actor(string id,string role,CancellationToken ct){if(!await ActorActivo(id,role,ct))throw new EvaluacionOperacionException("La cuenta no tiene acceso a esta operación.");}
    private static readonly Expression<Func<Cita,CitaResumen>> Proyectar=c=>new(c.Id,c.Estudiante.PerfilEstudiante!.Id,c.Estudiante.FullName,c.Psicologo.FullName,
        c.Fecha,c.Inicio,c.Fin,c.Estado,c.Revision,c.Modalidad,c.DuracionVariable,c.InicioUtc,
        c.Estado==EstadoCita.Terminada||c.Estado==EstadoCita.NoAsistio?c.ResenaEstudiante:"",
        c.Estado==EstadoCita.Terminada?c.ResultadoPublicable:"",c.Estado==EstadoCita.Terminada&&c.RecomiendaProximaCita,
        c.Estado==EstadoCita.Terminada?c.IndicacionesProximaCita:"");
    public IQueryable<Cita> PropiasProfesional(string id)=>db.Set<Cita>().AsNoTracking().Where(c=>c.PsicologoId==id&&c.PruebaLocal==bigFive.PruebaLocal&&c.Psicologo.IsActive&&
        db.UserRoles.Any(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO")));
    public async Task<IReadOnlyList<CitaResumen>> Listar(string actor,bool profesional,CancellationToken ct,string? estudiante=null)
    {
        await Actor(actor,profesional?"PSICOLOGO":"ESTUDIANTE",ct);
        var query=profesional?PropiasProfesional(actor):db.Set<Cita>().AsNoTracking().Where(c=>c.EstudianteId==actor&&c.PruebaLocal==bigFive.PruebaLocal);
        if(estudiante is not null)query=query.Where(c=>c.EstudianteId==estudiante);
        return await query.OrderBy(c=>c.Estado!=EstadoCita.Solicitada&&c.Estado!=EstadoCita.Confirmada).ThenBy(c=>c.InicioUtc).ThenBy(c=>c.Id).Select(Proyectar).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<CitaResumen>> Resultados(string student,CancellationToken ct)
    {
        await Actor(student,"ESTUDIANTE",ct);
        return await db.Set<Cita>().AsNoTracking().Where(c=>c.EstudianteId==student&&c.PruebaLocal==bigFive.PruebaLocal&&c.Estado==EstadoCita.Terminada)
            .OrderByDescending(c=>c.InicioUtc).Select(Proyectar).ToListAsync(ct);
    }
    public async Task<HistorialCitasViewModel> Historial(string psychologist,string? busqueda,DateOnly? desde,DateOnly? hasta,string? modalidad,int pagina,CancellationToken ct)
    {
        await Actor(psychologist,"PSICOLOGO",ct);var search=(busqueda??"").Trim();if(search.Length>80)search=search[..80];
        var mode=modalidad is "Presencial" or "Virtual"?modalidad:"Todas";
        var query=PropiasProfesional(psychologist).Where(c=>c.Estado==EstadoCita.Terminada||c.Estado==EstadoCita.NoAsistio);
        if(search.Length>0)query=query.Where(c=>c.Estudiante.FullName.ToLower().Contains(search.ToLower()));
        if(desde is not null)query=query.Where(c=>c.Fecha>=desde);if(hasta is not null)query=query.Where(c=>c.Fecha<=hasta);
        if(mode!="Todas")query=query.Where(c=>c.Modalidad==mode);
        var pages=Math.Max(1,(int)Math.Ceiling(await query.CountAsync(ct)/24d));var page=Math.Clamp(pagina,1,pages);
        var rows=await query.OrderByDescending(c=>c.InicioUtc).ThenBy(c=>c.Id).Skip((page-1)*24).Take(24).Select(Proyectar).ToListAsync(ct);
        return new(rows,search,desde,hasta,mode,page,pages);
    }
    public async Task<AtencionDetalle?> Detalle(string psychologist,Guid id,CancellationToken ct)
    {
        await Actor(psychologist,"PSICOLOGO",ct);
        var own=PropiasProfesional(psychologist).Where(c=>c.Id==id);
        var summary=await own.Select(Proyectar).SingleOrDefaultAsync(ct);if(summary is null)return null;
        var clinical=await own.Select(c=>c.ResultadoClinicoPrivado).SingleAsync(ct);
        var events=await db.Set<EventoCita>().AsNoTracking().Where(e=>e.CitaId==id).OrderBy(e=>e.FechaUtc).Select(e=>new EventoCitaResumen(e.FechaUtc,e.Accion,e.Motivo)).ToListAsync(ct);
        if(summary.Estado is EstadoCita.Terminada or EstadoCita.NoAsistio){db.Add(new AccesoAtencion{CitaId=id,PsicologoId=psychologist});await db.SaveChangesAsync(ct);}
        return new(summary,clinical,events);
    }
    private async Task<Cita> Bloquear(Guid id,string actor,bool psychologist,int revision,CancellationToken ct)
    {
        var cita=await db.Set<Cita>().FromSqlInterpolated($"SELECT * FROM \"Citas\" WHERE \"Id\"={id} FOR UPDATE").SingleOrDefaultAsync(ct);
        if(cita is null||cita.PruebaLocal!=bigFive.PruebaLocal||(psychologist?cita.PsicologoId:cita.EstudianteId)!=actor)throw new EvaluacionOperacionException("Cita no disponible para tu cuenta.");
        if(cita.Revision!=revision)throw new EvaluacionOperacionException("La cita cambió en otra sesión. Actualiza la página.");
        return cita;
    }
    private async Task Dia(string psychologist,DateOnly date,CancellationToken ct)
    {
        var key=psychologist+":"+date.ToString("yyyy-MM-dd");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 803501502))",ct);
    }
    private void Evento(Cita cita,string actor,string accion,string? motivo=null)=>db.Add(new EventoCita{CitaId=cita.Id,ActorId=actor,Accion=accion,Motivo=(motivo??"").Trim(),FechaUtc=UtcNow});
    public async Task<Guid> Solicitar(string student,SolicitudCitaInput input,CancellationToken ct)
    {
        await Actor(student,"ESTUDIANTE",ct);
        if(input.Modalidad is not ("Presencial" or "Virtual"))throw new EvaluacionOperacionException("Elige presencial o virtual.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        // Serialize booking with withdrawal/finalization of the prerequisite questionnaire.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT \"Id\" FROM \"AsignacionesEvaluacion\" WHERE \"EstudianteId\"={student} AND \"EvaluacionId\"={Ipip50.EvaluacionId} FOR UPDATE",ct);
        if(!await bigFive.PuedeSolicitarCita(student,ct))throw new EvaluacionOperacionException("Completa Big Five y confirma un consentimiento y vínculo vigentes antes de solicitar o reprogramar.");
        var psychologist=await bigFive.Vigentes().Where(p=>p.Asignacion.EstudianteId==student).Select(p=>p.PsicologoId).SingleAsync(ct);
        Cita? previous=null;
        if(input.ReprogramarId is not null){
            previous=await Bloquear(input.ReprogramarId.Value,student,false,input.RevisionAnterior??0,ct);
            if(!CitaTextos.Activa(previous.Estado)||previous.InicioUtc<=UtcNow||previous.PsicologoId!=psychologist)
                throw new EvaluacionOperacionException("Solo puedes reprogramar una cita futura pendiente o confirmada con tu psicólogo actual.");
        }
        var initial=await db.Set<HorarioAgendaPsicologo>().AsNoTracking().Where(h=>h.Id==input.HorarioId).Select(h=>new{h.Dia.Fecha,h.Dia.PsicologoId}).SingleOrDefaultAsync(ct);
        if(initial is null||initial.PsicologoId!=psychologist)throw new EvaluacionOperacionException("Ese horario ya no está disponible.");
        var dates=new[]{initial.Fecha,previous?.Fecha??initial.Fecha}.Distinct().OrderBy(d=>d);
        foreach(var date in dates)await Dia(psychologist,date,ct);
        var range=await db.Set<HorarioAgendaPsicologo>().AsNoTracking().Include(h=>h.Dia).SingleOrDefaultAsync(h=>h.Id==input.HorarioId,ct);
        if(range is null||range.Dia.PsicologoId!=psychologist||range.Dia.Ocupado||range.Dia.Fecha!=initial.Fecha)
            throw new EvaluacionOperacionException("El psicólogo cambió la disponibilidad. Elige otro horario.");
        var dateOf=range.Dia.Fecha;var start=input.Inicio;
        if(await db.Set<Cita>().AnyAsync(c=>c.EstudianteId==student&&c.PruebaLocal==bigFive.PruebaLocal&&c.Fecha==dateOf&&
            (previous==null||c.Id!=previous.Id)&&(c.Estado==EstadoCita.Solicitada||c.Estado==EstadoCita.Confirmada||c.Estado==EstadoCita.Terminada||c.Estado==EstadoCita.NoAsistio),ct))
            throw new EvaluacionOperacionException("Ya tienes una cita para ese día. Reprograma la existente o elige otra fecha.");
        var variable=range.DuracionMinutos==0;
        var end=variable?range.Fin:start.AddMinutes(range.DuracionMinutos);
        var aligned=variable?(start.ToTimeSpan()-range.Inicio.ToTimeSpan()).TotalMinutes%5==0:
            (start.ToTimeSpan()-range.Inicio.ToTimeSpan()).TotalMinutes%range.DuracionMinutos==0;
        if(start<range.Inicio||start>=range.Fin||end>range.Fin||end<=start||start.Second!=0||start.Ticks%TimeSpan.TicksPerMinute!=0||!aligned||Utc(dateOf,start)<=UtcNow||
            dateOf>AgendaService.Hoy.AddMonths(12)||range.Modalidad!="Ambas"&&range.Modalidad!=input.Modalidad)
            throw new EvaluacionOperacionException("El horario o modalidad seleccionados no están disponibles.");
        // A variable appointment reserves the whole published interval, not a guessed 15/30/50-minute duration.
        var overlapStart=variable?range.Inicio:start;
        if(await db.Set<Cita>().AnyAsync(c=>c.PsicologoId==psychologist&&c.PruebaLocal==bigFive.PruebaLocal&&c.Fecha==dateOf&&(c.Estado==EstadoCita.Solicitada||c.Estado==EstadoCita.Confirmada)&&
            (previous==null||c.Id!=previous.Id)&&c.Inicio<end&&c.Fin>overlapStart,ct))
            throw new EvaluacionOperacionException("Ese horario acaba de reservarse. Actualiza el calendario y elige otro.");
        if(previous is not null&&previous.Fecha==dateOf&&previous.Inicio==start&&previous.Fin==end&&previous.Modalidad==input.Modalidad)
            throw new EvaluacionOperacionException("Elige una fecha, hora o modalidad diferente para reprogramar.");
        if(previous is not null){previous.Estado=EstadoCita.Reprogramada;previous.Revision++;Evento(previous,student,"Reprogramada");await db.SaveChangesAsync(ct);}
        var cita=new Cita{EstudianteId=student,PsicologoId=psychologist,HorarioOriginalId=range.Id,Fecha=dateOf,Inicio=start,Fin=end,
            InicioUtc=Utc(dateOf,start),FinUtc=Utc(dateOf,end),DuracionVariable=variable,PruebaLocal=bigFive.PruebaLocal,Modalidad=input.Modalidad,CitaAnteriorId=previous?.Id,SolicitudUtc=UtcNow};
        db.Add(cita);Evento(cita,student,"Solicitada");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return cita.Id;
    }
    public async Task Gestionar(string actor,bool psychologist,Guid id,int revision,string accion,string? motivo,CancellationToken ct)
    {
        await Actor(actor,psychologist?"PSICOLOGO":"ESTUDIANTE",ct);
        if((motivo??"").Trim().Length>300)throw new EvaluacionOperacionException("El motivo admite hasta 300 caracteres.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);var cita=await Bloquear(id,actor,psychologist,revision,ct);
        await Dia(cita.PsicologoId,cita.Fecha,ct);
        if(!CitaTextos.Activa(cita.Estado))throw new EvaluacionOperacionException("Esta cita ya no está pendiente ni confirmada.");
        switch(accion){
            case "confirmar" when psychologist:
                if(cita.Estado!=EstadoCita.Solicitada||cita.InicioUtc<=UtcNow)throw new EvaluacionOperacionException("Solo puedes confirmar una solicitud futura.");
                if(!await ActorActivo(cita.EstudianteId,"ESTUDIANTE",ct)||!await db.AsignacionesEstudiantePsicologo.AnyAsync(v=>v.PerfilEstudiante.UsuarioId==cita.EstudianteId&&v.PerfilEstudiante.Activo&&v.PsicologoUsuarioId==actor&&v.FechaFinalizacionUtc==null,ct))
                    throw new EvaluacionOperacionException("El estudiante ya no tiene un vínculo activo contigo.");
                cita.Estado=EstadoCita.Confirmada;break;
            case "rechazar" when psychologist:
                if(cita.Estado!=EstadoCita.Solicitada)throw new EvaluacionOperacionException("Solo puedes rechazar una solicitud pendiente.");
                if(string.IsNullOrWhiteSpace(motivo))throw new EvaluacionOperacionException("Indica brevemente el motivo del rechazo.");
                cita.Estado=EstadoCita.Rechazada;break;
            case "cancelar":
                if(cita.InicioUtc<=UtcNow)throw new EvaluacionOperacionException("La cita ya comenzó. El profesional debe registrar su asistencia.");
                if(string.IsNullOrWhiteSpace(motivo))throw new EvaluacionOperacionException("Indica brevemente el motivo de cancelación.");
                cita.Estado=EstadoCita.Cancelada;break;
            default:throw new EvaluacionOperacionException("Operación no permitida.");
        }
        cita.Revision++;Evento(cita,actor,accion,motivo);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task Cerrar(string psychologist,CerrarAtencionInput input,CancellationToken ct)
    {
        await Actor(psychologist,"PSICOLOGO",ct);
        var clinical=(input.ResultadoClinicoPrivado??"").Trim();var review=(input.ResenaEstudiante??"").Trim();var published=(input.ResultadoPublicable??"").Trim();var next=(input.IndicacionesProximaCita??"").Trim();
        if(input.Asistio is null||clinical.Length>4000||review.Length>1500||published.Length>1500||next.Length>1000)
            throw new EvaluacionOperacionException("Confirma asistencia y revisa los límites de texto.");
        if(input.Asistio==true&&(clinical.Length<5||review.Length<5||published.Length<5))
            throw new EvaluacionOperacionException("Escribe el resultado clínico privado, una reseña y un resultado breve para compartir con el estudiante.");
        if(input.Asistio==true&&input.RecomiendaProximaCita&&next.Length==0)
            throw new EvaluacionOperacionException("Indica el seguimiento recomendado para la próxima cita.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);var cita=await Bloquear(input.Id,psychologist,true,input.Revision,ct);
        if(cita.Estado!=EstadoCita.Confirmada||cita.FinUtc>UtcNow)throw new EvaluacionOperacionException("Registra la atención después de terminar el horario de una cita confirmada.");
        cita.Asistio=input.Asistio;cita.AtencionUtc=UtcNow;cita.Estado=input.Asistio==true?EstadoCita.Terminada:EstadoCita.NoAsistio;
        cita.ResultadoClinicoPrivado=input.Asistio==true?clinical:"";
        cita.ResenaEstudiante=review;cita.ResultadoPublicable=input.Asistio==true?published:"";
        cita.RecomiendaProximaCita=input.Asistio==true&&input.RecomiendaProximaCita;
        cita.IndicacionesProximaCita=cita.RecomiendaProximaCita?next:"";
        cita.Revision++;Evento(cita,psychologist,input.Asistio==true?"Atención terminada":"No asistió");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<IReadOnlyList<FranjaCita>> Franjas(string psychologist,IReadOnlyList<DiaAgendaPsicologo> days,CancellationToken ct)
    {
        if(days.Count==0)return [];
        var dates=days.Select(d=>d.Fecha).ToArray();
        var reserved=await db.Set<Cita>().AsNoTracking().Where(c=>c.PsicologoId==psychologist&&c.PruebaLocal==bigFive.PruebaLocal&&dates.Contains(c.Fecha)&&(c.Estado==EstadoCita.Solicitada||c.Estado==EstadoCita.Confirmada))
            .Select(c=>new{c.Fecha,c.Inicio,c.Fin}).ToListAsync(ct);
        var rows=new List<FranjaCita>();
        foreach(var day in days){foreach(var range in day.Horarios.OrderBy(h=>h.Inicio)){
            if(range.DuracionMinutos==0){
                var start=AgendaService.InicioDisponible(range,day.Fecha,TimeZoneInfo.ConvertTimeFromUtc(UtcNow,TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala")));
                if(start is not null)rows.Add(new(range.Id,day.Fecha,start.Value,range.Fin,true,range.Modalidad,day.Ocupado||reserved.Any(c=>c.Fecha==day.Fecha&&c.Inicio<range.Fin&&c.Fin>range.Inicio)));
            }else{
                for(var start=range.Inicio.ToTimeSpan();start+TimeSpan.FromMinutes(range.DuracionMinutos)<=range.Fin.ToTimeSpan();start+=TimeSpan.FromMinutes(range.DuracionMinutos)){
                    var from=TimeOnly.FromTimeSpan(start);var until=TimeOnly.FromTimeSpan(start+TimeSpan.FromMinutes(range.DuracionMinutos));
                    if(Utc(day.Fecha,from)<=UtcNow)continue;
                    rows.Add(new(range.Id,day.Fecha,from,until,false,range.Modalidad,day.Ocupado||reserved.Any(c=>c.Fecha==day.Fecha&&c.Inicio<until&&c.Fin>from)));
                }
            }
        }}
        return rows;
    }
}
