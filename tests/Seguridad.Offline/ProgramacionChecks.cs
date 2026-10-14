using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
internal static class ProgramacionChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave,Reloj reloj,Guid perfil)
    {
        int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception("Programación: "+label);checks++;}
        using var psy=HostPrueba.Cliente(app);using var director=HostPrueba.Cliente(app);using var student=HostPrueba.Cliente(app);using var other=HostPrueba.Cliente(app);using var otherPsy=HostPrueba.Cliente(app);using var admin=HostPrueba.Cliente(app);using var anon=HostPrueba.Cliente(app);
        await HostPrueba.Login(psy,"psi3@miumg.edu.gt",clave);await HostPrueba.Login(director,"director@miumg.edu.gt",clave);await HostPrueba.Login(student,"est2@miumg.edu.gt",clave);await HostPrueba.Login(other,"est@miumg.edu.gt",clave);await HostPrueba.Login(otherPsy,"psi2@miumg.edu.gt",clave);await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);
        var fecha=AgendaReglas.Hoy(reloj.Ahora.UtcDateTime).AddDays(15);var id=Guid.NewGuid();
        string Edit(Guid? c=null)=>c is null?"/CitasProgramadas/Editar":"/CitasProgramadas/Editar?citaId="+c;
        async Task<HttpResponseMessage> Save(HttpClient client,DateOnly dia,Guid? cita=null,Guid? revision=null,Guid? profile=null,Guid? op=null,string modalidad="Presencial")=>await HostPrueba.Post(client,Edit(cita),"/CitasProgramadas/Guardar",("CitaId",cita?.ToString()??""),("Revision",(revision??Guid.Empty).ToString()),("OperacionId",(op??id).ToString()),("PerfilEstudianteId",(profile??perfil).ToString()),("Fecha",dia.ToString("yyyy-MM-dd")),("HoraInicio","10:00"),("DuracionMinutos","50"),("Modalidad",modalidad));
        async Task<Cita> Leer(){await using var db=new OfflineDb(options);return await db.Citas.AsNoTracking().SingleAsync(c=>c.Id==id);}
        foreach(var c in new[]{student,admin,anon})Check((await c.GetAsync(Edit())).StatusCode==HttpStatusCode.Redirect,"roles ajenos no programan");
        Check((await psy.GetAsync(Edit())).StatusCode==HttpStatusCode.OK&&(await director.GetAsync(Edit())).StatusCode==HttpStatusCode.OK,"pantallas profesional y Director");
        Check((await psy.PostAsync("/CitasProgramadas/Guardar",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"antiforgery");
        Check((await Save(psy,fecha,profile:Guid.NewGuid())).StatusCode==HttpStatusCode.Conflict,"sin vínculo no programa");
        Check((await Save(psy,fecha)).StatusCode==HttpStatusCode.Redirect,"programación directa");
        var cita=await Leer();Check(cita.ProgramadaDirectamente&&cita.DisponibilidadPsicologoId is null&&cita.ProgramadorRol=="Psicologo"&&cita.Estado==EstadoCita.Confirmada,"fecha vinculada y confirmada para perfil preparado");
        Check((await student.GetStringAsync("/CitasProgramadas/Notificaciones")).Contains("Cita programada por Psicologo"),"estudiante notificado con autor");
        Check(!(await other.GetStringAsync("/CitasProgramadas/Notificaciones")).Contains("Cita programada por Psicologo"),"notificaciones no visibles a otro estudiante");
        Guid notificacion;await using(var db=new OfflineDb(options)){notificacion=(await db.NotificacionesCita.FirstAsync(n=>n.CitaId==id)).Id;}
        Check((await HostPrueba.Post(other,"/CitasProgramadas/Notificaciones","/CitasProgramadas/Leida",("id",notificacion.ToString()))).StatusCode==HttpStatusCode.Redirect,"lectura de notificación ajena denegada");
        await using(var db=new OfflineDb(options))Check(!(await db.NotificacionesCita.SingleAsync(n=>n.Id==notificacion)).Leida,"IDOR no modifica estado");
        Check((await HostPrueba.Post(student,"/CitasProgramadas/Notificaciones","/CitasProgramadas/Leida",("id",notificacion.ToString()))).StatusCode==HttpStatusCode.Redirect,"marca propia leída");
        Check((await otherPsy.GetAsync(Edit(id))).StatusCode==HttpStatusCode.Redirect,"otro psicólogo no reprograma");
        Check((await Save(director,fecha.AddDays(1),id,cita.Revision)).StatusCode==HttpStatusCode.Redirect,"Director reprograma sin expediente");
        var moved=await Leer();Check(moved.Id==id&&moved.FechaHoraInicioUtc==AgendaReglas.Utc(fecha.AddDays(1),new(10,0))&&moved.Revision!=cita.Revision&&moved.ProgramadorRol=="Director"&&moved.Estado==EstadoCita.Solicitada,"misma cita y revisión nueva; Director deja pendiente confirmación");
        Check((await psy.GetStringAsync("/CitasProgramadas/Notificaciones")).Contains("Cita reprogramada por Director"),"profesional informado del cambio institucional");
        Check((await Save(psy,fecha.AddDays(2),id,cita.Revision)).StatusCode==HttpStatusCode.Conflict,"edición obsoleta rechazada");
        Check((await Save(psy,fecha.AddDays(1),op:Guid.NewGuid())).StatusCode==HttpStatusCode.Conflict,"no duplica citas del mismo día");
        Check((await Save(psy,fecha)).StatusCode==HttpStatusCode.Conflict,"operación ya guardada no duplica");
        Check((await Save(psy,fecha.AddDays(-100),op:Guid.NewGuid())).StatusCode==HttpStatusCode.Conflict,"cita normal pasada rechazada");
        await using(var db=new OfflineDb(options)){var v=await db.VerificacionesMayoriaEdad.SingleAsync(v=>v.PerfilEstudianteId==perfil);v.Estado=Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Pendiente;await db.SaveChangesAsync();}
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/GestionarCita",("CitaId",id.ToString()),("Revision",moved.Revision.ToString()),("Estado","Confirmada"))).StatusCode==HttpStatusCode.Conflict,"propuesta no permite confirmar sin verificación de edad");
        await using(var db=new OfflineDb(options)){var v=await db.VerificacionesMayoriaEdad.SingleAsync(v=>v.PerfilEstudianteId==perfil);v.Estado=Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Verificada;var c=await db.Citas.SingleAsync(c=>c.Id==id);c.Modalidad=ModalidadCita.Virtual;c.Estado=EstadoCita.Confirmada;c.EnlaceReunion="https://meet.google.com/tuv-wxyz-abc";c.FechaEnlaceUtc=reloj.Ahora.UtcDateTime;await db.SaveChangesAsync();}
        Check((await Save(psy,fecha.AddDays(2),id,moved.Revision,modalidad:"Virtual")).StatusCode==HttpStatusCode.Redirect,"reprogramación virtual");
        var virtualMovida=await Leer();Check(virtualMovida.EnlaceReunion is null&&virtualMovida.FechaEnlaceUtc is null&&virtualMovida.Estado==EstadoCita.Solicitada,"cambio virtual requiere preparar y confirmar de nuevo el enlace");
        var hfecha=fecha.AddDays(3);
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/CambiarDia",("fecha",hfecha.ToString("yyyy-MM-dd")),("operacion","limpiar"))).StatusCode==HttpStatusCode.Redirect,"prepara una fecha sin base semanal para el nuevo horario");
        var horario=await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/AgregarHorarioDia",("Fecha",hfecha.ToString("yyyy-MM-dd")),("HoraInicio","09:00"),("DuracionMinutos","50"),("Modalidad","Presencial"),("PerfilEstudianteId",perfil.ToString()));
        Check(horario.StatusCode==HttpStatusCode.Redirect,"disponibilidad con estudiante opcional programa");
        await using(var db=new OfflineDb(options)){var c=await db.Citas.SingleAsync(c=>c.PerfilEstudianteId==perfil&&c.FechaHoraInicioUtc==AgendaReglas.Utc(hfecha,new(9,0)));Check(c.DisponibilidadPsicologoId is not null&&await db.NotificacionesCita.AnyAsync(n=>n.CitaId==c.Id),"cita vinculada al horario y notificación persistida");}
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){await File.WriteAllTextAsync(Path.Combine(preview,"programar-cita.html"),await psy.GetStringAsync(Edit()));await File.WriteAllTextAsync(Path.Combine(preview,"notificaciones.html"),await student.GetStringAsync("/CitasProgramadas/Notificaciones"));foreach(var tema in new[]{"cita","seguimiento","bienestar","privacidad"})await File.WriteAllTextAsync(Path.Combine(preview,"guia-"+tema+".html"),await anon.GetStringAsync("/Home/Guia?tema="+tema));await File.WriteAllTextAsync(Path.Combine(preview,"home-footer.html"),await anon.GetStringAsync("/"));}
        Console.WriteLine($"OK: {checks} comprobaciones de programación, reprogramación y notificaciones.");
    }
}
