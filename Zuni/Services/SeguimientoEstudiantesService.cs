using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Evaluaciones;
namespace Zuni.Services;

public sealed class SeguimientoEstudiantesService(ApplicationDbContext db,BigFiveService bigFive)
{
    private static string EstadoAgenda(bool pending,EstadoCita? last)=>pending?"Pendiente de aprobación":last switch{EstadoCita.Cancelada=>"Cancelada",EstadoCita.Rechazada=>"Rechazada",EstadoCita.Reprogramada=>"Reprogramada",EstadoCita.Terminada=>"Atención terminada",EstadoCita.NoAsistio=>"No asistió",EstadoCita.Confirmada=>"Por registrar atención",EstadoCita.Solicitada=>"Solicitud vencida",_=>"Sin cita futura"};
    public async Task<SeguimientoEstudiantesViewModel> Leer(string psychologist,string? busqueda,string? filtro,int pagina,Guid? seleccionado,CancellationToken ct)
    {
        var search=(busqueda??"").Trim();if(search.Length>80)search=search[..80];
        var filter=filtro is "completado" or "sinresultado" or "solicitada" or "agendada" or "pendiente" or "reprogramada" or "cancelada" or "terminada" or "noasistio"?filtro:"todos";
        var enabled=bigFive.Disponible;
        var now=DateTime.UtcNow;
        var appointments=db.Set<Cita>().AsNoTracking().Where(c=>c.PsicologoId==psychologist&&c.PruebaLocal==bigFive.PruebaLocal);
        var query=db.AsignacionesEstudiantePsicologo.AsNoTracking()
            .Where(v=>v.PsicologoUsuarioId==psychologist&&v.FechaFinalizacionUtc==null&&v.PsicologoUsuario.IsActive&&
                db.UserRoles.Any(ur=>ur.UserId==psychologist&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO"))&&
                v.PerfilEstudiante.Activo&&v.PerfilEstudiante.Usuario.IsActive&&
                db.UserRoles.Any(ur=>ur.UserId==v.PerfilEstudiante.UsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="ESTUDIANTE")))
            .Select(v=>new {Id=v.PerfilEstudianteId,Student=v.PerfilEstudiante.UsuarioId,Nombre=v.PerfilEstudiante.Usuario.FullName,
                v.PerfilEstudiante.Carne,v.PerfilEstudiante.Carrera,
                Proxima=appointments.Where(c=>c.EstudianteId==v.PerfilEstudiante.UsuarioId&&c.Estado==EstadoCita.Confirmada&&c.InicioUtc>now).Select(c=>(DateTime?)c.InicioUtc).Min(),
                Pendiente=appointments.Any(c=>c.EstudianteId==v.PerfilEstudiante.UsuarioId&&c.Estado==EstadoCita.Solicitada&&c.InicioUtc>now),
                Ultimo=appointments.Where(c=>c.EstudianteId==v.PerfilEstudiante.UsuarioId).OrderByDescending(c=>c.SolicitudUtc).ThenByDescending(c=>c.Id).Select(c=>(EstadoCita?)c.Estado).FirstOrDefault(),
                Completo=enabled&&bigFive.Vigentes().Any(p=>p.Asignacion.EstudianteId==v.PerfilEstudiante.UsuarioId&&p.PsicologoId==psychologist&&
                    p.Asignacion.Estado==EstadoEvaluacion.Finalizada&&p.Apertura!=null)});
        var total=await query.CountAsync(ct);var completed=await query.CountAsync(v=>v.Completo,ct);
        EstudianteSeguimiento? selected=null;ResumenBigFive? summary=null;IReadOnlyList<CitaResumen> selectedAppointments=[];
        var upcoming=await query.CountAsync(v=>v.Proxima!=null,ct);
        if(seleccionado is not null){
            var row=await query.SingleOrDefaultAsync(v=>v.Id==seleccionado,ct);
            if(row is not null){
                selected=new(row.Id,row.Nombre,row.Carne,row.Carrera,row.Completo,row.Proxima,EstadoAgenda(row.Pendiente,row.Ultimo));
                selectedAppointments=await new CitasService(db,bigFive,TimeProvider.System).Listar(psychologist,true,ct,row.Student);
                // Fetch and audit only the selected student's result, never the whole roster's profiles.
                if(row.Completo)summary=(await bigFive.Resumenes(psychologist,ct,row.Student)).SingleOrDefault();
            }
        }
        if(search.Length>0)query=query.Where(v=>v.Nombre.ToLower().Contains(search.ToLower()));
        if(filter=="completado")query=query.Where(v=>v.Completo);
        if(filter=="sinresultado")query=query.Where(v=>!v.Completo);
        if(filter=="pendiente")query=query.Where(v=>appointments.Any(c=>c.EstudianteId==v.Student&&(c.Estado==EstadoCita.Solicitada||c.Estado==EstadoCita.Confirmada)));
        var state=filter switch{"solicitada"=>EstadoCita.Solicitada,"agendada"=>EstadoCita.Confirmada,"reprogramada"=>EstadoCita.Reprogramada,"cancelada"=>EstadoCita.Cancelada,"terminada"=>EstadoCita.Terminada,"noasistio"=>EstadoCita.NoAsistio,_=>(EstadoCita?)null};
        if(state is not null)query=query.Where(v=>appointments.Any(c=>c.EstudianteId==v.Student&&c.Estado==state));
        var pages=Math.Max(1,(int)Math.Ceiling(await query.CountAsync(ct)/24d));var page=Math.Clamp(pagina,1,pages);
        var rows=await query.OrderBy(v=>v.Proxima==null).ThenBy(v=>v.Proxima).ThenBy(v=>v.Nombre).ThenBy(v=>v.Id).Skip((page-1)*24).Take(24).ToListAsync(ct);
        return new(rows.Select(v=>new EstudianteSeguimiento(v.Id,v.Nombre,v.Carne,v.Carrera,v.Completo,v.Proxima,EstadoAgenda(v.Pendiente,v.Ultimo))).ToArray(),total,completed,search,filter,page,pages,selected,summary){Citas=selectedAppointments,Proximas=upcoming};
    }
}
