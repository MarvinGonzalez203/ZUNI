using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
internal static class AlmuerzoRevisionChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave,Reloj reloj)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception("Almuerzo y revisión: "+text);checks++;}
        var perfil=new PerfilEstudiante{UsuarioId="meal-est",Carne="9999264444",Carrera="Carrera ficticia",Telefono="12345678"};
        await using(var db=new OfflineDb(options)){
            foreach(var (id,rol) in new[]{("meal-psy","Psicologo"),("meal-est","Estudiante")}){
                var u=new ApplicationUser{Id=id,FullName=id=="meal-psy"?"Psicólogo ficticio almuerzo":"Estudiante ficticio almuerzo",Email=id+"@miumg.edu.gt",NormalizedEmail=(id+"@miumg.edu.gt").ToUpperInvariant(),UserName=id+"@miumg.edu.gt",NormalizedUserName=(id+"@miumg.edu.gt").ToUpperInvariant(),IsActive=true,SecurityStamp=id+"-stamp"};
                u.PasswordHash=new PasswordHasher<ApplicationUser>().HashPassword(u,clave);db.Users.Add(u);db.UserRoles.Add(new(){UserId=id,RoleId=rol});
            }
            db.PerfilesEstudiante.Add(perfil);db.VerificacionesMayoriaEdad.Add(new(){PerfilEstudianteId=perfil.Id,Estado=Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Verificada,EdadDeclarada=21,EdadVerificada=21,ResponsableUsuarioId="admin",DocumentoRevisado=Zuni.Models.MayoriaEdad.DocumentoEdad.Dpi,FechaDeclaracionUtc=reloj.Ahora.UtcDateTime});
            db.AsignacionesEstudiantePsicologo.Add(new(){PerfilEstudianteId=perfil.Id,PsicologoUsuarioId="meal-psy"});
            foreach(DayOfWeek d in Enum.GetValues<DayOfWeek>())db.DisponibilidadesPsicologo.Add(new(){PsicologoUsuarioId="meal-psy",DiaSemana=d,HoraInicio=new(8,0),HoraFin=new(18,0),DuracionMinutos=30,Modalidad=ModalidadDisponibilidad.Presencial});
            await db.SaveChangesAsync();
        }
        using var psy=HostPrueba.Cliente(app);using var est=HostPrueba.Cliente(app);using var dir=HostPrueba.Cliente(app);using var other=HostPrueba.Cliente(app);using var existingPsy=HostPrueba.Cliente(app);
        await HostPrueba.Login(psy,"meal-psy@miumg.edu.gt",clave);await HostPrueba.Login(est,"meal-est@miumg.edu.gt",clave);await HostPrueba.Login(dir,"director@miumg.edu.gt",clave);await HostPrueba.Login(other,"psi2@miumg.edu.gt",clave);await HostPrueba.Login(existingPsy,"psi3@miumg.edu.gt",clave);
        Check((await psy.PostAsync("/Psicologo/GuardarAlmuerzo",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"almuerzo exige antiforgery");
        async Task<HttpResponseMessage> Meal(HttpClient client,Guid revision,string inicio="13:00",string fin="14:00",bool habilitado=true)=>await HostPrueba.Post(client,client==psy?"/Psicologo/Agenda":client==dir?"/Director":"/CitasProgramadas/Notificaciones","/Psicologo/GuardarAlmuerzo",("Revision",revision.ToString()),("Inicio",inicio),("Fin",fin),("Habilitado",habilitado.ToString()));
        Check((await Meal(est,Guid.Empty)).StatusCode==HttpStatusCode.Redirect&&(await Meal(dir,Guid.Empty)).StatusCode==HttpStatusCode.Redirect,"solo el psicólogo configura su almuerzo");
        Check((await Meal(psy,Guid.Empty)).StatusCode==HttpStatusCode.Redirect,"almuerzo guardado");
        ConfiguracionAlmuerzo config;await using(var db=new OfflineDb(options)){config=await db.ConfiguracionesAlmuerzo.AsNoTracking().SingleAsync(a=>a.PsicologoUsuarioId=="meal-psy");Check(config.Habilitado&&config.Inicio==new TimeOnly(13,0)&&config.Fin==new TimeOnly(14,0),"intervalo 13–14 persistente");}
        Check((await Meal(psy,config.Revision,"14:00","13:00")).StatusCode==HttpStatusCode.Conflict,"fin anterior rechazado");
        Check((await Meal(psy,config.Revision,"13:02")).StatusCode==HttpStatusCode.Conflict,"minutos fuera de cuadrícula rechazados");
        Check((await Meal(psy,Guid.Empty,habilitado:false)).StatusCode==HttpStatusCode.Conflict,"configuración obsoleta no desactiva bloqueo");
        var fecha=AgendaReglas.Hoy(reloj.Ahora.UtcDateTime).AddDays(45);
        async Task<HttpResponseMessage> Programa(HttpClient client,DateOnly day,string hora,Guid id,Guid? cita=null,Guid? revision=null)=>await HostPrueba.Post(client,cita is null?"/CitasProgramadas/Editar":"/CitasProgramadas/Editar?citaId="+cita,"/CitasProgramadas/Guardar",("OperacionId",id.ToString()),("CitaId",cita?.ToString()??""),("Revision",(revision??Guid.Empty).ToString()),("PerfilEstudianteId",perfil.Id.ToString()),("Fecha",day.ToString("yyyy-MM-dd")),("HoraInicio",hora),("DuracionMinutos","50"),("Modalidad","Presencial"),("DireccionAtencion","Consultorio de prueba, edificio A"));
        Check((await Programa(psy,fecha,"13:00",Guid.NewGuid())).StatusCode==HttpStatusCode.Conflict,"programación profesional bloqueada en almuerzo");
        Check((await Programa(psy,fecha,"12:30",Guid.NewGuid())).StatusCode==HttpStatusCode.Conflict,"cita que empieza antes y cruza almuerzo rechazada");
        // Privacidad: el Director ya no programa citas individuales (ni dentro ni fuera del almuerzo).
        Check((await HostPrueba.Post(dir,"/Director","/CitasProgramadas/Guardar",("OperacionId",Guid.NewGuid().ToString()),("PerfilEstudianteId",perfil.Id.ToString()),("Fecha",fecha.AddDays(3).ToString("yyyy-MM-dd")),("HoraInicio","10:00"),("DuracionMinutos","50"),("Modalidad","Presencial"))).StatusCode==HttpStatusCode.Redirect,"Director no programa citas individuales");
        async Task<HttpResponseMessage> Reserva(DateOnly day,string hora,string? duracion=null)=>await HostPrueba.Post(est,"/Estudiante/Citas","/Estudiante/Citas/Reservar",("Inicio",AgendaReglas.Utc(day,TimeOnly.Parse(hora)).ToString("O")),("Modalidad","Presencial"),("DuracionMinutos",duracion??""));
        Check((await Reserva(fecha,"13:00")).StatusCode==HttpStatusCode.Conflict,"reserva de estudiante bloqueada");
        Check((await Reserva(fecha,"12:30")).StatusCode==HttpStatusCode.Redirect,"puede terminar exactamente al inicio del almuerzo");
        var cita=Guid.NewGuid();Check((await Programa(psy,fecha.AddDays(1),"14:00",cita)).StatusCode==HttpStatusCode.Redirect,"puede comenzar exactamente al terminar almuerzo");
        Guid version;await using(var db=new OfflineDb(options))version=await db.Citas.Where(c=>c.Id==cita).Select(c=>c.Revision).SingleAsync();
        Check((await Programa(psy,fecha.AddDays(1),"13:00",Guid.NewGuid(),cita,version)).StatusCode==HttpStatusCode.Conflict,"reprogramación no evade bloqueo");
        Check((await Meal(psy,config.Revision,"14:00","15:00")).StatusCode==HttpStatusCode.Conflict,"no cambia almuerzo sobre citas existentes");
        var variable=fecha.AddDays(2);await using(var db=new OfflineDb(options)){
            var d=new ConfiguracionAgendaDia{PsicologoUsuarioId="meal-psy",Fecha=variable,UsaBaseSemanal=false};db.ConfiguracionesAgendaDia.Add(d);db.DisponibilidadesPsicologo.Add(new(){PsicologoUsuarioId="meal-psy",ConfiguracionAgendaDiaId=d.Id,DiaSemana=variable.DayOfWeek,HoraInicio=new(12,0),HoraFin=new(15,0),DuracionMinutos=0});await db.SaveChangesAsync();
        }
        Check((await Reserva(variable,"12:45","30")).StatusCode==HttpStatusCode.Conflict,"duración variable no cruza almuerzo");
        await using(var db=new OfflineDb(options))Check(!await db.Citas.AnyAsync(c=>c.ProgramadorRol=="Director"&&c.PsicologoUsuarioId=="meal-psy"),"el intento del Director no crea citas");
        var propuesta=await LegadoDirector.CrearAsync(options,perfil.Id,"meal-psy",AgendaReglas.Utc(fecha.AddDays(3),new(10,0)),reloj.Ahora.UtcDateTime,notificarPsicologo:true);
        var notificaciones=await psy.GetStringAsync("/CitasProgramadas/Notificaciones");Check(WebUtility.HtmlDecode(notificaciones).Contains("Estudiante ficticio almuerzo")&&notificaciones.Contains("notification-data")&&notificaciones.Contains("citaId="+propuesta),"notificación identifica estudiante y cita concreta");
        var foco=await psy.GetStringAsync("/Psicologo/Agenda?citaId="+propuesta);Check(foco.Contains("is-pending")&&foco.Contains("data-cita-seleccionada=\""+propuesta)&&foco.Contains("Aceptar cita")&&foco.Contains("Rechazar propuesta")&&foco.Contains("data-motivos-propuesta-director"),"día pendiente y respuesta en ventana contextual");
        Check((await other.GetAsync("/Psicologo/Agenda?citaId="+propuesta)).StatusCode==HttpStatusCode.Redirect,"cita ajena no abre ventana contextual");
        Check((await dir.GetAsync("/CitasProgramadas?citaId="+propuesta)).StatusCode==HttpStatusCode.Redirect,"Director no abre citas individuales ni su contexto");
        var solicitud=await est.GetStringAsync("/Estudiante/EstadoSolicitud");Check(WebUtility.HtmlDecode(solicitud).Contains("Aún no tienes una solicitud registrada"),"sin solicitud muestra explicación funcional");
        var asignadas=await existingPsy.GetStringAsync("/Psicologo/Solicitudes");var expedientes=await existingPsy.GetStringAsync("/Psicologo/Solicitudes?expedientes=true");Check(asignadas.Contains("Solicitudes asignadas")&&WebUtility.HtmlDecode(expedientes).Contains("Fichas de acompañamiento")&&!expedientes.Contains("MOTIVO-CLINICO-PRIVADO"),"accesos del tablero conducen a listados autorizados");
        Check((await est.GetAsync("/Psicologo/Solicitudes")).StatusCode==HttpStatusCode.Redirect,"estudiante no consulta solicitudes profesionales");
        foreach(var html in new[]{notificaciones,foco,solicitud,asignadas,expedientes})Check(Regex.Matches(html,"class=\"dash-nav-link is-active\"").Count==1,"un solo submenú activo por pantalla");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){await File.WriteAllTextAsync(Path.Combine(preview,"agenda-revision.html"),foco);await File.WriteAllTextAsync(Path.Combine(preview,"notificacion-revision.html"),notificaciones);await File.WriteAllTextAsync(Path.Combine(preview,"solicitud-estudiante.html"),solicitud);await File.WriteAllTextAsync(Path.Combine(preview,"solicitudes-psicologo.html"),asignadas);await File.WriteAllTextAsync(Path.Combine(preview,"expedientes-psicologo.html"),expedientes);}
        Console.WriteLine($"OK: {checks} comprobaciones de almuerzo, revisión contextual, enlaces y submenús.");
    }
}
