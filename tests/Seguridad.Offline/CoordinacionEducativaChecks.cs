using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Models.Institucion;
using Zuni.Services.Agenda;

internal static class CoordinacionEducativaChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave,Reloj reloj)
    {
        int checks=0;void Check(bool ok,string msg){if(!ok)throw new Exception("Coordinación educativa: "+msg);checks++;}
        using var admin=HostPrueba.Cliente(app);using var psy=HostPrueba.Cliente(app);using var student=HostPrueba.Cliente(app);using var director=HostPrueba.Cliente(app);using var other=HostPrueba.Cliente(app);using var anon=HostPrueba.Cliente(app);
        await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);await HostPrueba.Login(psy,"meal-psy@miumg.edu.gt",clave);await HostPrueba.Login(student,"meal-est@miumg.edu.gt",clave);await HostPrueba.Login(director,"director@miumg.edu.gt",clave);await HostPrueba.Login(other,"psi2@miumg.edu.gt",clave);
        Guid perfil;await using(var db=new OfflineDb(options))perfil=await db.PerfilesEstudiante.Where(p=>p.UsuarioId=="meal-est").Select(p=>p.Id).SingleAsync();
        Check((await admin.GetAsync("/Tarifas")).StatusCode==HttpStatusCode.OK,"Administrador consulta propuesta");
        foreach(var c in new[]{anon,student,psy,director})Check((await c.GetAsync("/Tarifas")).StatusCode==HttpStatusCode.Redirect,"tarifas restringidas al Administrador");
        Check(TarifaSaas.Propuesta.Select(t=>t.Meses).SequenceEqual(new[]{1,3,6,12}) && TarifaSaas.Propuesta.Select(t=>t.Total).SequenceEqual(new[]{199m,567m,1075m,2030m}),"periodos y totales GTQ");
        Check(TarifaSaas.Propuesta.All(t=>t.Ahorro>=0)&&TarifaSaas.Propuesta[3].MensualEquivalente<TarifaSaas.Propuesta[0].MensualEquivalente,"ahorro y equivalente mensual calculados");
        var fecha=AgendaReglas.Hoy(reloj.Ahora.UtcDateTime).AddDays(60);var id=Guid.NewGuid();
        Task<HttpResponseMessage> Programa(string? lugar,string modalidad="Presencial",Guid? op=null,DateOnly? day=null,HttpClient? actor=null)=>HostPrueba.Post(actor??psy,"/CitasProgramadas/Editar","/CitasProgramadas/Guardar",("OperacionId",(op??id).ToString()),("PerfilEstudianteId",perfil.ToString()),("Fecha",(day??fecha).ToString("yyyy-MM-dd")),("HoraInicio","09:00"),("DuracionMinutos","50"),("Modalidad",modalidad),("DireccionAtencion",lugar??""));
        Check((await Programa(null)).StatusCode==HttpStatusCode.Conflict,"no confirma presencial sin lugar");
        await using(var db=new OfflineDb(options))Check(!await db.Citas.AnyAsync(c=>c.Id==id),"rechazo revierte creación incompleta");
        Check((await Programa("  Campus <script>, consultorio A  ")).StatusCode==HttpStatusCode.Redirect,"programa presencial con dirección");
        Cita cita;await using(var db=new OfflineDb(options)){cita=await db.Citas.AsNoTracking().SingleAsync(c=>c.Id==id);Check(cita.Estado==EstadoCita.Confirmada&&cita.DireccionAtencion=="Campus <script>, consultorio A","lugar recortado y persistido");}
        string url="/Ubicaciones/Editar?citaId="+id;
        foreach(var c in new[]{student,director,other})Check((await c.GetAsync(url)).StatusCode==HttpStatusCode.Redirect,"solo profesional asignado edita ubicación");
        Check((await psy.PostAsync("/Ubicaciones/Editar",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"ubicación exige antiforgery");
        var raw=await student.GetStringAsync("/Estudiante/Citas?fecha="+fecha.ToString("yyyy-MM-dd"));
        Check(raw.Contains("Campus &lt;script&gt;")&&!raw.Contains("Campus <script>"),"dirección propia visible y codificada");
        Check((await HostPrueba.Post(psy,url,"/Ubicaciones/Editar",("CitaId",id.ToString()),("Revision",Guid.NewGuid().ToString()),("DireccionAtencion","Cambio obsoleto"))).StatusCode==HttpStatusCode.Conflict,"no sobrescribe revisión obsoleta");
        Check((await HostPrueba.Post(psy,url,"/Ubicaciones/Editar",("CitaId",id.ToString()),("Revision",cita.Revision.ToString()),("DireccionAtencion","Edificio B, oficina de orientación"))).StatusCode==HttpStatusCode.Redirect,"actualización de ubicación");
        Check((await student.GetStringAsync("/CitasProgramadas/Notificaciones")).Contains("Edificio B, oficina de orientaci"),"estudiante recibe lugar actualizado");
        var propuesta=Guid.NewGuid();
        Check((await Programa("El Director no fija el lugar del profesional","Presencial",propuesta,fecha.AddDays(3),director)).StatusCode==HttpStatusCode.Redirect,"Director propone sin imponer ubicación");
        Cita pendiente;await using(var db=new OfflineDb(options)){pendiente=await db.Citas.AsNoTracking().SingleAsync(c=>c.Id==propuesta);Check(pendiente.Estado==EstadoCita.Solicitada&&pendiente.DireccionAtencion is null,"propuesta requiere lugar del psicólogo");}
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/GestionarCita",("CitaId",propuesta.ToString()),("Revision",pendiente.Revision.ToString()),("Estado","Confirmada"))).StatusCode==HttpStatusCode.Conflict,"confirmación genérica tampoco omite ubicación");
        Check((await HostPrueba.Post(psy,"/Ubicaciones/Editar?citaId="+propuesta,"/Ubicaciones/Editar",("CitaId",propuesta.ToString()),("Revision",pendiente.Revision.ToString()),("DireccionAtencion","Oficina educativa, edificio C"),("ConfirmarCita","true"))).StatusCode==HttpStatusCode.Redirect,"guardar lugar y confirmar propuesta");
        await using(var db=new OfflineDb(options))Check((await db.Citas.FindAsync(propuesta))!.Estado==EstadoCita.Confirmada,"lugar validado habilita confirmación");
        var virtualId=Guid.NewGuid();Check((await Programa("Lugar que no debe conservarse","Virtual",virtualId,fecha.AddDays(1))).StatusCode==HttpStatusCode.Redirect,"virtual queda pendiente de enlace individual");
        await using(var db=new OfflineDb(options))Check((await db.Citas.FindAsync(virtualId))!.DireccionAtencion is null,"virtual no conserva dirección presencial");
        Check(WebUtility.HtmlDecode(await student.GetStringAsync("/CitasProgramadas/Notificaciones")).Contains("Antes de la cita virtual: procura estar a solas"),"notificación de creación virtual incluye preparación");
        var consejos=WebUtility.HtmlDecode(await student.GetStringAsync("/Reuniones/Detalle?citaId="+virtualId));
        Check(consejos.Contains("Procura estar a solas")&&consejos.Contains("sin ruido")&&consejos.Contains("No compartas el enlace"),"consejos antes de que se habilite el ingreso");
        var home=WebUtility.HtmlDecode(await anon.GetStringAsync("/"));
        Check(home.Contains("Nuestra misión")&&home.Contains("Nuestra visión")&&home.Contains("No sustituye al profesional"),"misión y visión de apoyo educativo");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){
            await File.WriteAllTextAsync(Path.Combine(preview,"tarifas.html"),await admin.GetStringAsync("/Tarifas"));
            await File.WriteAllTextAsync(Path.Combine(preview,"ubicacion.html"),await psy.GetStringAsync(url));
            await File.WriteAllTextAsync(Path.Combine(preview,"consejos-virtuales.html"),await student.GetStringAsync("/Reuniones/Detalle?citaId="+virtualId));
        }
        Console.WriteLine($"OK: {checks} comprobaciones de coordinación, lugares y tarifas.");
    }
}
