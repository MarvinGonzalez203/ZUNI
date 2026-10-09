using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Evaluaciones;
namespace Zuni.Services;

public sealed class AgendaService(ApplicationDbContext db, BigFiveService bigFive)
{
    public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala"));
    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    public static TimeOnly? InicioDisponible(HorarioAgendaPsicologo horario,DateOnly fecha,DateTime ahora)
    {
        var hoy=DateOnly.FromDateTime(ahora);
        if(fecha<hoy)return null;
        var start=horario.Inicio.ToTimeSpan();
        if(fecha==hoy && start<ahora.TimeOfDay)
            start+=TimeSpan.FromMinutes(Math.Ceiling((ahora.TimeOfDay-start).TotalMinutes/horario.DuracionMinutos)*horario.DuracionMinutos);
        return start+TimeSpan.FromMinutes(horario.DuracionMinutos)<=horario.Fin.ToTimeSpan()?TimeOnly.FromTimeSpan(start):null;
    }
    public static DateOnly Mes(DateOnly? requested)
    {
        var today=Hoy; var month=requested ?? today;
        var first=new DateOnly(month.Year,month.Month,1);
        var minimum=new DateOnly(today.Year,today.Month,1);
        return first<minimum || first>minimum.AddMonths(12) ? minimum : first;
    }
    private Task<bool> ProfesionalActivo(string id,CancellationToken ct) => db.Users.AnyAsync(u=>u.Id==id&&u.IsActive &&
        db.UserRoles.Any(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO")),ct);
    public async Task<AgendaCalendario> Leer(string id,bool editor,DateOnly? requested,CancellationToken ct)
    {
        var month=Mes(requested); string? psychologist=null;
        if(editor && await ProfesionalActivo(id,ct)) psychologist=id;
        if(!editor && bigFive.Disponible)
            psychologist=await bigFive.Vigentes().Where(p=>p.Asignacion.EstudianteId==id&&p.Asignacion.Estado==EstadoEvaluacion.Finalizada&&p.Apertura!=null)
                .Select(p=>p.PsicologoId).SingleOrDefaultAsync(ct);
        if(psychologist is null)return new(month,"",false,editor,[]);
        var name=await db.Users.Where(u=>u.Id==psychologist).Select(u=>u.FullName).SingleAsync(ct);
        var days=await db.Set<DiaAgendaPsicologo>().AsNoTracking().Include(d=>d.Horarios)
            .Where(d=>d.PsicologoId==psychologist&&d.Fecha>=month&&d.Fecha<month.AddMonths(1)).OrderBy(d=>d.Fecha).ToListAsync(ct);
        // Only availability is passed to the view; never users, answers or appointment identities.
        return new(month,name,true,editor,days);
    }
    public async Task Cambiar(string psychologist,DateOnly date,string action,HorarioAgendaInput? input,Guid? range,CancellationToken ct)
    {
        if(!await ProfesionalActivo(psychologist,ct))throw new EvaluacionOperacionException("Tu cuenta no tiene acceso a la agenda.");
        if(date<Hoy || date>Hoy.AddMonths(12))throw new EvaluacionOperacionException("Elige una fecha desde hoy y dentro de los próximos doce meses.");
        if(action is not ("agregar" or "bloquear" or "habilitar" or "quitar" or "limpiar"))throw new EvaluacionOperacionException("Operación no válida.");
        if(action=="agregar" && (input is null || input.Inicio<new TimeOnly(8,0)||input.Fin>new TimeOnly(18,0)||input.Fin<=input.Inicio ||
            input.Inicio.Second!=0||input.Fin.Second!=0||input.Inicio.Minute%5!=0||input.Fin.Minute%5!=0 ||
            !new[]{15,30,45,50,60}.Contains(input.DuracionMinutos) || (input.Fin-input.Inicio).TotalMinutes<input.DuracionMinutos ||
            input.Modalidad is not ("Presencial" or "Virtual") || date==Hoy&&input.Inicio<=TimeOnly.FromDateTime(Ahora)))
            throw new EvaluacionOperacionException("Revisa el horario: de 08:00 a 18:00, en intervalos de cinco minutos, con espacio para al menos una cita y sin horas pasadas.");
        await using var transaction=await db.Database.BeginTransactionAsync(ct);
        // A transaction-scoped lock also serializes creation when the day does not exist yet.
        var key=psychologist+":"+date.ToString("yyyy-MM-dd");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 803501502))",ct);
        var day=await db.Set<DiaAgendaPsicologo>().Include(d=>d.Horarios).SingleOrDefaultAsync(d=>d.PsicologoId==psychologist&&d.Fecha==date,ct);
        if(action=="agregar")
        {
            if(day?.Ocupado==true)throw new EvaluacionOperacionException("Habilita el día antes de añadir horarios.");
            if(day?.Horarios.Any(h=>input!.Inicio<h.Fin&&input.Fin>h.Inicio)==true)throw new EvaluacionOperacionException("Ese horario se cruza con otro ya guardado.");
            if(day is null){day=new(){PsicologoId=psychologist,Fecha=date};db.Add(day);}
            day.Horarios.Add(new(){Inicio=input!.Inicio,Fin=input.Fin,DuracionMinutos=input.DuracionMinutos,Modalidad=input.Modalidad});
        }
        else if(action is "bloquear" or "habilitar")
        {
            if(day is null){day=new(){PsicologoId=psychologist,Fecha=date};db.Add(day);}
            day.Ocupado=action=="bloquear";
        }
        else if(day is not null)
        {
            if(action=="limpiar") {db.RemoveRange(day.Horarios);await db.SaveChangesAsync(ct);db.Remove(day);}
            else {var slot=day.Horarios.SingleOrDefault(h=>h.Id==range);if(slot is null)throw new EvaluacionOperacionException("El horario ya no existe.");db.Remove(slot);}
        }
        await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
    }
}
