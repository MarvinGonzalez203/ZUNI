using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Services.Privacidad;
namespace Zuni.Services.Agenda;
public sealed partial class AgendaCitasService
{
    private static string DireccionValida(string? valor)
    {
        var lugar=valor?.Trim();
        if(string.IsNullOrWhiteSpace(lugar)||lugar.Length>500)throw new AgendaOperacionException("Indica una dirección o lugar de atención presencial de hasta 500 caracteres antes de confirmar.");
        return lugar;
    }
    public async Task<LugarCitaInput> PrepararLugar(ClaimsPrincipal actor,Guid id,CancellationToken ct)
    {
        var psy=await Psicologo(actor,ct);
        var cita=await db.Citas.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==id&&c.PsicologoUsuarioId==psy,ct)??throw new AgendaAccesoException();
        await ValidarParticipantes(cita,ct);
        if(cita.Modalidad!=ModalidadCita.Presencial||!AgendaReglas.Activa(cita.Estado)||cita.FechaHoraInicioUtc<=Ahora)throw new AgendaOperacionException("Solo se actualiza el lugar de citas presenciales activas futuras.");
        return new(){CitaId=cita.Id,Revision=cita.Revision,DireccionAtencion=cita.DireccionAtencion??"",Estado=cita.Estado,InicioUtc=cita.FechaHoraInicioUtc,Estudiante=await db.PerfilesEstudiante.Where(p=>p.Id==cita.PerfilEstudianteId).Select(p=>p.Usuario.FullName).SingleAsync(ct)};
    }
    private async Task NotificarLugar(Cita cita,string psy,CancellationToken ct)
    {
        var user=await db.PerfilesEstudiante.Where(p=>p.Id==cita.PerfilEstudianteId).Select(p=>p.UsuarioId).SingleAsync(ct);
        db.NotificacionesCita.Add(new(){CitaId=cita.Id,UsuarioId=user,AutorUsuarioId=psy,AutorRol="Psicologo",Tipo="Ubicación",FechaUtc=Ahora,Texto=$"El psicólogo indicó el lugar de tu cita presencial.\nFecha: {AgendaReglas.Local(cita.FechaHoraInicioUtc):dd/MM/yyyy HH:mm} (Guatemala).\nLugar: {cita.DireccionAtencion}"});
        PrivacidadService.Auditar(db,user,psy,"CITA_UBICACION_ACTUALIZADA",reloj,cita.Id);
    }
    public Task GuardarLugar(ClaimsPrincipal actor,LugarCitaInput input,CancellationToken ct)=>Escribir(async()=>{
        await PrepararLugar(actor,input.CitaId,ct);
        var psy=await Psicologo(actor,ct);var cita=await db.Citas.SingleAsync(c=>c.Id==input.CitaId,ct);
        if(cita.Revision!=input.Revision)throw new AgendaOperacionException("La cita cambió. Abre nuevamente su ubicación antes de guardar.");
        cita.DireccionAtencion=DireccionValida(input.DireccionAtencion);cita.Revision=Guid.NewGuid();
        if(input.ConfirmarCita){await ValidarConfirmacionProgramada(cita,ct);await ValidarAlmuerzo(psy,cita.FechaHoraInicioUtc,cita.FechaHoraFinUtc,ct);AplicarCambio(cita,EstadoCita.Confirmada,null);await NotificarRespuestaDirector(cita,ct);}
        await NotificarLugar(cita,psy,ct);
    },ct);
}
