using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Evaluaciones;
namespace Zuni.Services;

public sealed class SeguimientoEstudiantesService(ApplicationDbContext db,BigFiveService bigFive)
{
    public async Task<SeguimientoEstudiantesViewModel> Leer(string psychologist,string? busqueda,string? filtro,int pagina,Guid? seleccionado,CancellationToken ct)
    {
        var search=(busqueda??"").Trim();if(search.Length>80)search=search[..80];
        var filter=filtro is "completado" or "sinresultado"?filtro:"todos";
        var enabled=bigFive.Disponible;
        var query=db.AsignacionesEstudiantePsicologo.AsNoTracking()
            .Where(v=>v.PsicologoUsuarioId==psychologist&&v.FechaFinalizacionUtc==null&&v.PsicologoUsuario.IsActive&&
                db.UserRoles.Any(ur=>ur.UserId==psychologist&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO"))&&
                v.PerfilEstudiante.Activo&&v.PerfilEstudiante.Usuario.IsActive&&
                db.UserRoles.Any(ur=>ur.UserId==v.PerfilEstudiante.UsuarioId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="ESTUDIANTE")))
            .Select(v=>new {Id=v.PerfilEstudianteId,Student=v.PerfilEstudiante.UsuarioId,Nombre=v.PerfilEstudiante.Usuario.FullName,
                v.PerfilEstudiante.Carne,v.PerfilEstudiante.Carrera,
                Completo=enabled&&bigFive.Vigentes().Any(p=>p.Asignacion.EstudianteId==v.PerfilEstudiante.UsuarioId&&p.PsicologoId==psychologist&&
                    p.Asignacion.Estado==EstadoEvaluacion.Finalizada&&p.Apertura!=null)});
        var total=await query.CountAsync(ct);var completed=await query.CountAsync(v=>v.Completo,ct);
        EstudianteSeguimiento? selected=null;ResumenBigFive? summary=null;
        if(seleccionado is not null){
            var row=await query.SingleOrDefaultAsync(v=>v.Id==seleccionado,ct);
            if(row is not null){
                selected=new(row.Id,row.Nombre,row.Carne,row.Carrera,row.Completo);
                // Fetch and audit only the selected student's result, never the whole roster's profiles.
                if(row.Completo)summary=(await bigFive.Resumenes(psychologist,ct,row.Student)).SingleOrDefault();
            }
        }
        if(search.Length>0)query=query.Where(v=>v.Nombre.ToLower().Contains(search.ToLower()));
        if(filter=="completado")query=query.Where(v=>v.Completo);
        if(filter=="sinresultado")query=query.Where(v=>!v.Completo);
        var pages=Math.Max(1,(int)Math.Ceiling(await query.CountAsync(ct)/24d));var page=Math.Clamp(pagina,1,pages);
        // Booking dates will supply the primary sort once appointment persistence is implemented.
        var rows=await query.OrderBy(v=>v.Nombre).ThenBy(v=>v.Id).Skip((page-1)*24).Take(24).ToListAsync(ct);
        return new(rows.Select(v=>new EstudianteSeguimiento(v.Id,v.Nombre,v.Carne,v.Carrera,v.Completo)).ToArray(),total,completed,search,filter,page,pages,selected,summary);
    }
}
