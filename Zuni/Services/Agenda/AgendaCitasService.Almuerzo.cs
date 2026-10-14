using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
namespace Zuni.Services.Agenda;
public sealed partial class AgendaCitasService
{
    private Task<ConfiguracionAlmuerzo?> Almuerzo(string? psy,CancellationToken ct)=>db.ConfiguracionesAlmuerzo.AsNoTracking().SingleOrDefaultAsync(a=>a.PsicologoUsuarioId==psy,ct);
    public static bool CruzaAlmuerzo(ConfiguracionAlmuerzo? a,DateTime inicio,DateTime fin)
    {
        if(a?.Habilitado!=true)return false;
        for(var fecha=AgendaReglas.Hoy(inicio);fecha<=AgendaReglas.Hoy(fin);fecha=fecha.AddDays(1))
            if(AgendaReglas.Cruza(inicio,fin,AgendaReglas.Utc(fecha,a.Inicio),AgendaReglas.Utc(fecha,a.Fin)))return true;
        return false;
    }
    private async Task ValidarAlmuerzo(string psy,DateTime inicio,DateTime fin,CancellationToken ct)
    {
        var a=await Almuerzo(psy,ct);
        if(CruzaAlmuerzo(a,inicio,fin))throw new AgendaOperacionException($"La cita se cruza con el almuerzo del psicólogo ({a!.Inicio:HH:mm}–{a.Fin:HH:mm}, Guatemala). Elige otra hora.");
    }
    public Task GuardarAlmuerzo(ClaimsPrincipal actor,AlmuerzoInput input,CancellationToken ct)=>Escribir(async()=>{
        var psy=await Psicologo(actor,ct);
        if(input.Inicio is not {}inicio||input.Fin is not {}fin||inicio<new TimeOnly(8,0)||fin>new TimeOnly(18,0)||fin<=inicio||inicio.Ticks%(5*TimeSpan.TicksPerMinute)!=0||fin.Ticks%(5*TimeSpan.TicksPerMinute)!=0)
            throw new AgendaOperacionException("Define el almuerzo entre 08:00 y 18:00, con fin posterior al inicio y en múltiplos de cinco minutos.");
        var actual=await db.ConfiguracionesAlmuerzo.SingleOrDefaultAsync(a=>a.PsicologoUsuarioId==psy,ct);
        if((actual?.Revision??Guid.Empty)!=input.Revision)throw new AgendaOperacionException("La configuración de almuerzo cambió. Recarga la agenda antes de guardar.");
        var propuesta=new ConfiguracionAlmuerzo{PsicologoUsuarioId=psy,Habilitado=input.Habilitado,Inicio=inicio,Fin=fin};
        if(input.Habilitado){
            var citas=await db.Citas.AsNoTracking().Where(c=>c.PsicologoUsuarioId==psy&&EstadosActivos.Contains(c.Estado)&&c.FechaHoraFinUtc>Ahora).Select(c=>new{c.FechaHoraInicioUtc,c.FechaHoraFinUtc}).ToListAsync(ct);
            if(citas.Any(c=>CruzaAlmuerzo(propuesta,c.FechaHoraInicioUtc,c.FechaHoraFinUtc)))throw new AgendaOperacionException("Ya hay citas activas en ese horario. Reprográmalas o elige otro horario de almuerzo antes de guardar.");
        }
        if(actual is null){actual=propuesta;db.ConfiguracionesAlmuerzo.Add(actual);}else{actual.Habilitado=propuesta.Habilitado;actual.Inicio=inicio;actual.Fin=fin;actual.Revision=Guid.NewGuid();}
    },ct);
    public async Task<(string PsicologoId,DateOnly Fecha)> UbicarCita(ClaimsPrincipal actor,Guid id,CancellationToken ct)
    {
        var a=await Programador(actor,ct);
        var c=await db.Citas.AsNoTracking().Where(c=>c.Id==id&&(a.Rol=="Director"||c.PsicologoUsuarioId==a.Id)).Select(c=>new{c.PsicologoUsuarioId,c.FechaHoraInicioUtc}).SingleOrDefaultAsync(ct)??throw new AgendaAccesoException();
        return(c.PsicologoUsuarioId,AgendaReglas.Hoy(c.FechaHoraInicioUtc));
    }
}
