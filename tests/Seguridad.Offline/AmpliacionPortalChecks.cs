using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Models.Pruebas;
using Zuni.Services;
using Zuni.Services.Agenda;

internal static class AmpliacionPortalChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave,Reloj reloj,Guid perfil)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception("Ampliación: "+text);checks++;}
        using var admin=HostPrueba.Cliente(app);using var director=HostPrueba.Cliente(app);using var psy=HostPrueba.Cliente(app);using var estudiante=HostPrueba.Cliente(app);using var otro=HostPrueba.Cliente(app);using var psyOtro=HostPrueba.Cliente(app);
        await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);await HostPrueba.Login(director,"director@miumg.edu.gt",clave);await HostPrueba.Login(psy,"psi3@miumg.edu.gt",clave);await HostPrueba.Login(estudiante,"est2@miumg.edu.gt",clave);await HostPrueba.Login(otro,"est@miumg.edu.gt",clave);await HostPrueba.Login(psyOtro,"psi2@miumg.edu.gt",clave);
        Check(CatalogoCarreras.Leer("SALUD:\n- Carrera A\n- carrera a\nÁrea;Carrera B").Count==2,"parser de áreas y duplicados");
        await using(var db=new OfflineDb(options))Check(await db.CarrerasInstitucionales.CountAsync()==9,"nueve carreras iniciales");
        Check((await estudiante.GetAsync("/CarrerasAdmin")).StatusCode==HttpStatusCode.Redirect,"estudiante no gestiona catálogo");
        Check((await admin.PostAsync("/CarrerasAdmin/Importar",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"catálogo antiforgery");
        Check((await HostPrueba.Post(admin,"/CarrerasAdmin","/CarrerasAdmin/Importar",("Listado","Área de prueba;Carrera de ejemplo"))).StatusCode==HttpStatusCode.Redirect,"importación de catálogo");
        await HostPrueba.Post(admin,"/CarrerasAdmin","/CarrerasAdmin/Importar",("Listado","Área de prueba;Carrera de ejemplo"));
        await using(var db=new OfflineDb(options))Check(await db.CarrerasInstitucionales.CountAsync()==10,"importación repetida no duplica");
        var cuenta=await estudiante.GetStringAsync("/MiCuenta/Editar");Check(cuenta.Contains("<select")&&cuenta.Contains("Licenciatura en Enfermer")&&cuenta.Contains("registro anterior"),"selector académico conserva valor anterior");
        await using(var db=new OfflineDb(options)){var v=await db.VerificacionesMayoriaEdad.SingleAsync(v=>v.PerfilEstudianteId==perfil);v.Estado=Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Pendiente;await db.SaveChangesAsync();}
        var citas=await estudiante.GetStringAsync("/Estudiante/Citas");Check(citas.Contains("agenda-grid")&&!citas.Contains(">Solicitar cita</button>"),"calendario visible sin permiso de reserva");
        var alerta=await admin.GetStringAsync("/Administrador");Check(alerta.Contains("Edad pendiente")&&alerta.Contains("/MayoriaEdad/Revisiones"),"alerta superior del administrador");
        Check(!(await director.GetStringAsync("/Director")).Contains("Edad pendiente"),"alerta de edad exclusiva de administrador");
        await using(var db=new OfflineDb(options)){var v=await db.VerificacionesMayoriaEdad.SingleAsync(v=>v.PerfilEstudianteId==perfil);v.Estado=Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Verificada;await db.SaveChangesAsync();}
        var hoy=AgendaReglas.Hoy(reloj.Ahora.UtcDateTime);
        var cal=await director.GetStringAsync("/Director/Calendario?psicologoId=psi3");Check(cal.Contains("agenda-grid")&&!cal.Contains("Proponer cita a un estudiante vinculado")&&!cal.Contains("/CitasProgramadas/Editar")&&!cal.Contains("MOTIVO-CLINICO-PRIVADO")&&!cal.Contains("SECRETO-NO-IMPRIMIR"),"carga del Director con calendario compartido, sin clínica ni programación");
        Check((await director.GetAsync("/CitasProgramadas")).StatusCode==HttpStatusCode.Redirect,"Director ya no abre la programación individual");
        Check((await director.GetAsync("/Director/Calendario?psicologoId=admin")).StatusCode==HttpStatusCode.Redirect,"calendario no acepta un administrador como profesional");
        Check((await estudiante.GetAsync("/Director/Calendario")).StatusCode==HttpStatusCode.Redirect,"calendario institucional restringido");
        Check((await estudiante.GetStringAsync("/Estudiante/Citas?fecha="+hoy.AddMonths(-1).ToString("yyyy-MM-dd"))).Contains("agenda-grid"),"histórico del estudiante accesible");
        Check((await HostPrueba.Post(director,"/Director","/CitasProgramadas/Guardar",("OperacionId",Guid.NewGuid().ToString()),("PerfilEstudianteId",perfil.ToString()),("Fecha",hoy.AddDays(32).ToString("yyyy-MM-dd")),("HoraInicio","11:00"),("DuracionMinutos","50"),("Modalidad","Presencial"))).StatusCode==HttpStatusCode.Redirect,"Director no usa la programación individual del psicólogo");
        // Propuestas históricas del Director: el psicólogo aún las responde y el estudiante recibe la respuesta.
        var cita=await LegadoDirector.CrearAsync(options,perfil,"psi3",AgendaReglas.Utc(hoy.AddDays(32),new(11,0)),reloj.Ahora.UtcDateTime);
        Guid version;await using(var db=new OfflineDb(options))version=await db.Citas.Where(c=>c.Id==cita).Select(c=>c.Revision).SingleAsync();
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/GestionarCita",("CitaId",cita.ToString()),("Revision",Guid.NewGuid().ToString()),("DireccionAtencion","Consultorio de prueba, edificio A"),("Estado","Confirmada"))).StatusCode==HttpStatusCode.Conflict,"no acepta una propuesta con versión obsoleta");
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/GestionarCita",("CitaId",cita.ToString()),("Revision",version.ToString()),("Estado","Rechazada"))).StatusCode==HttpStatusCode.Conflict,"rechazo institucional exige motivo");
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/GestionarCita",("CitaId",cita.ToString()),("Revision",version.ToString()),("Estado","Rechazada"),("MotivoCancelacion","Conflicto de horario"))).StatusCode==HttpStatusCode.Conflict,"propuesta de Dirección no acepta motivo libre");
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/GestionarCita",("CitaId",cita.ToString()),("Revision",version.ToString()),("Estado","Rechazada"),("MotivoCancelacion",MotivosPropuestaDirector.HorarioNoDisponible))).StatusCode==HttpStatusCode.Redirect,"rechazo con motivo de la lista guardado");
        Check((await director.GetAsync("/CitasProgramadas/Notificaciones")).StatusCode==HttpStatusCode.Redirect,"Director no consulta notificaciones que identifican estudiantes");
        Check((await estudiante.GetStringAsync("/CitasProgramadas/Notificaciones")).Contains(MotivosPropuestaDirector.HorarioNoDisponible),"estudiante recibe respuesta");
        var aceptada=await LegadoDirector.CrearAsync(options,perfil,"psi3",AgendaReglas.Utc(hoy.AddDays(33),new(11,0)),reloj.Ahora.UtcDateTime);
        await using(var db=new OfflineDb(options))version=await db.Citas.Where(c=>c.Id==aceptada).Select(c=>c.Revision).SingleAsync();
        Check((await HostPrueba.Post(psy,"/Psicologo/Agenda","/Psicologo/GestionarCita",("CitaId",aceptada.ToString()),("Revision",version.ToString()),("DireccionAtencion","Consultorio de prueba, edificio A"),("Estado","Confirmada"))).StatusCode==HttpStatusCode.Redirect,"profesional acepta propuesta institucional histórica");
        Check(WebUtility.HtmlDecode(await estudiante.GetStringAsync("/CitasProgramadas/Notificaciones")).Contains("aceptó la cita propuesta"),"estudiante recibe aceptación");
        // Dirección recibe solo avisos administrativos de sus propuestas (sin texto libre ni datos clínicos).
        await using(var db=new OfflineDb(options)){var avisos=await db.NotificacionesCita.AsNoTracking().Where(n=>n.UsuarioId=="director"&&(n.CitaId==cita||n.CitaId==aceptada)).ToListAsync();
            Check(avisos.Count==2&&avisos.All(n=>AgendaCitasService.AvisosDirector.Todos.Contains(n.Tipo))&&!avisos.Any(n=>n.Texto.Contains("Conflicto de horario")||n.Texto.Contains("MOTIVO-CLINICO-PRIVADO")),"el Director recibe solo avisos administrativos");}
        Guid prueba;
        await using(var db=new OfflineDb(options)) {
            var niveles=await db.PruebasDirector.Include(p=>p.Preguntas).ThenInclude(q=>q.Opciones).Where(p=>p.Nivel!=null).OrderBy(p=>p.Nivel).ToListAsync();
            Check(niveles.Select(p=>p.Preguntas.Count).SequenceEqual(new[]{15,35,54}),"tres niveles y cantidades exactas");
            Check(niveles.All(p=>!p.Activa&&p.Preguntas.All(q=>q.Opciones.Select(o=>o.Valor).Order().SequenceEqual(Enumerable.Range(1,10)))),"borradores con escala completa 1–10");
            foreach(var nivel in niveles) {
                Check(nivel.Preguntas.OrderBy(q=>q.Orden).Select(q=>q.Orden).SequenceEqual(Enumerable.Range(1,nivel.Preguntas.Count)),"numeración consecutiva nivel "+nivel.Nivel);
                var detalleNivel=WebUtility.HtmlDecode(await director.GetStringAsync("/PruebasDirector/Detalle/"+nivel.Id));
                var anterior=-1;
                foreach(var preguntaNivel in nivel.Preguntas.OrderBy(q=>q.Orden)) {
                    var posicion=detalleNivel.IndexOf(preguntaNivel.Texto,StringComparison.Ordinal);
                    Check(posicion>anterior,"orden visible de preguntas nivel "+nivel.Nivel+" pregunta "+preguntaNivel.Orden);
                    anterior=posicion;
                }
            }
            Check(!niveles[2].Preguntas.Any(q=>q.Id==Guid.Parse("eeeeeeee-0003-0003-0000-000000000043")) && niveles[2].Preguntas.Any(q=>q.Id==Guid.Parse("eeeeeeee-0003-0003-0000-000000000055")),"se retira ítem redundante y se conserva la pregunta final original");
            prueba=niveles[0].Id;niveles[0].Activa=true;await db.SaveChangesAsync();
        }
        foreach(var c in new[]{admin,director,estudiante})Check((await c.GetAsync("/PruebasAplicadas/Habilitar")).StatusCode==HttpStatusCode.Redirect,"solo psicólogo indica pruebas");
        Check((await psy.PostAsync("/PruebasAplicadas/Habilitar",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"inducción antiforgery");
        Check((await HostPrueba.Post(psy,"/PruebasAplicadas/Habilitar","/PruebasAplicadas/Habilitar",("PerfilEstudianteId",perfil.ToString()),("PruebaId",prueba.ToString()))).StatusCode==HttpStatusCode.OK,"sin inducción no habilita");
        Check((await HostPrueba.Post(psyOtro,"/PruebasAplicadas/Habilitar","/PruebasAplicadas/Habilitar",("PerfilEstudianteId",perfil.ToString()),("PruebaId",prueba.ToString()),("InduccionRealizada","true"))).StatusCode==HttpStatusCode.Redirect,"otro psicólogo no habilita");
        List<(Guid Id,EstadoCita Estado)> estados;
        await using(var db=new OfflineDb(options)){var atendidas=await db.Citas.Where(c=>c.PerfilEstudianteId==perfil&&c.PsicologoUsuarioId=="psi3"&&c.Estado==EstadoCita.Completada).ToListAsync();estados=atendidas.Select(c=>(c.Id,c.Estado)).ToList();foreach(var c in atendidas)c.Estado=EstadoCita.NoAsistio;await db.SaveChangesAsync();}
        Check((await HostPrueba.Post(psy,"/PruebasAplicadas/Habilitar","/PruebasAplicadas/Habilitar",("PerfilEstudianteId",perfil.ToString()),("PruebaId",prueba.ToString()),("InduccionRealizada","true"))).StatusCode==HttpStatusCode.Conflict,"sin primera cita atendida no habilita aunque se marque inducción");
        await using(var db=new OfflineDb(options)){foreach(var e in estados)(await db.Citas.SingleAsync(c=>c.Id==e.Id)).Estado=e.Estado;await db.SaveChangesAsync();}
        Guid revisionPrueba;await using(var db=new OfflineDb(options))revisionPrueba=await db.PruebasDirector.Where(p=>p.Id==prueba).Select(p=>p.Revision).SingleAsync();
        await HostPrueba.Post(director,"/PruebasDirector/Asignar/"+prueba,"/PruebasDirector/Asignar",("Id",prueba.ToString()),("Revision",revisionPrueba.ToString()),("PerfilEstudianteId",perfil.ToString()));
        Guid pendiente;await using(var db=new OfflineDb(options))pendiente=await db.AsignacionesPrueba.Where(a=>a.PruebaId==prueba&&a.PerfilEstudianteId==perfil).Select(a=>a.Id).SingleAsync();
        Check((await estudiante.GetAsync("/PruebasAplicadas/Contestar/"+pendiente)).StatusCode==HttpStatusCode.Conflict,"asignación del Director no sustituye inducción");
        Check((await HostPrueba.Post(psy,"/PruebasAplicadas/Habilitar","/PruebasAplicadas/Habilitar",("PerfilEstudianteId",perfil.ToString()),("PruebaId",prueba.ToString()),("InduccionRealizada","true"))).StatusCode==HttpStatusCode.Redirect,"primera atención e inducción habilitan");
        AsignacionPrueba a;Prueba instrumento;await using(var db=new OfflineDb(options)){a=await db.AsignacionesPrueba.AsNoTracking().SingleAsync(a=>a.PruebaId==prueba&&a.PerfilEstudianteId==perfil);instrumento=await db.PruebasDirector.AsNoTracking().Include(p=>p.Preguntas).ThenInclude(q=>q.Opciones).SingleAsync(p=>p.Id==prueba);Check(a.CitaInduccionId!=null&&a.InduccionUtc!=null&&a.PsicologoInductorId=="psi3","inducción enlazada a cita atendida");}
        string url="/PruebasAplicadas/Contestar/"+a.Id;
        Check((await otro.GetAsync(url)).StatusCode==HttpStatusCode.Redirect,"cuestionario ajeno denegado");
        var cuestionarioHtml=await estudiante.GetStringAsync(url);
        Check(cuestionarioHtml.Contains("No existen respuestas correctas"),"consigna presente");
        Check(cuestionarioHtml.Contains("data-questionnaire") && cuestionarioHtml.Contains("likert-bar"),"cuestionario con escala táctil y paginación");
        var previewCuestionario=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(previewCuestionario))await File.WriteAllTextAsync(Path.Combine(previewCuestionario,"cuestionario-aplicado.html"),cuestionarioHtml);
        if(!string.IsNullOrEmpty(previewCuestionario)){await File.WriteAllTextAsync(Path.Combine(previewCuestionario,"catalogo-contraste.html"),await director.GetStringAsync("/PruebasDirector"));await File.WriteAllTextAsync(Path.Combine(previewCuestionario,"informes-contraste.html"),await director.GetStringAsync("/InformesEducativos"));}
        var q=instrumento.Preguntas.First();var otraQ=instrumento.Preguntas.Last();
        Check((await HostPrueba.Post(estudiante,url,"/PruebasAplicadas/Contestar",("Id",a.Id.ToString()),("Revision",a.Revision.ToString()),("Consentimiento","true"),("Respuestas[0].PreguntaId",q.Id.ToString()),("Respuestas[0].OpcionId",otraQ.Opciones.First().Id.ToString()))).StatusCode==HttpStatusCode.Conflict,"opción de otra pregunta rechazada");
        Check((await HostPrueba.Post(estudiante,url,"/PruebasAplicadas/Contestar",("Id",a.Id.ToString()),("Revision",a.Revision.ToString()),("Consentimiento","true"),("Finalizar","true"))).StatusCode==HttpStatusCode.Conflict,"no finaliza incompleta");
        Check((await HostPrueba.Post(estudiante,url,"/PruebasAplicadas/Contestar",("Id",a.Id.ToString()),("Revision",a.Revision.ToString()))).StatusCode==HttpStatusCode.Conflict,"participación voluntaria requerida");
        Check((await HostPrueba.Post(estudiante,url,"/PruebasAplicadas/Contestar",("Id",a.Id.ToString()),("Revision",a.Revision.ToString()),("Consentimiento","true"),("Respuestas[0].PreguntaId",q.Id.ToString()),("Respuestas[0].OpcionId",q.Opciones.First().Id.ToString()))).StatusCode==HttpStatusCode.Redirect,"avance parcial guardado");
        await using(var db=new OfflineDb(options))a=await db.AsignacionesPrueba.AsNoTracking().SingleAsync(x=>x.Id==a.Id);
        var datos=new List<(string,string)>{("Id",a.Id.ToString()),("Revision",a.Revision.ToString()),("Consentimiento","true"),("Finalizar","true")};int i=0;
        foreach(var pregunta in instrumento.Preguntas.OrderBy(q=>q.Orden)){datos.Add( ($"Respuestas[{i}].PreguntaId",pregunta.Id.ToString()));datos.Add( ($"Respuestas[{i++}].OpcionId",pregunta.Opciones.Single(o=>o.Valor==7).Id.ToString()));}
        Check((await HostPrueba.Post(estudiante,url,"/PruebasAplicadas/Contestar",datos.ToArray())).StatusCode==HttpStatusCode.Redirect,"respuestas completas finalizan");
        await using(var db=new OfflineDb(options))Check(await db.Set<RespuestaPrueba>().CountAsync(r=>r.AsignacionPruebaId==a.Id)==15,"quince respuestas almacenadas");
        Check((await HostPrueba.Post(estudiante,url,"/PruebasAplicadas/Contestar",datos.ToArray())).StatusCode==HttpStatusCode.Conflict,"reenvío finalizado no sobrescribe");
        var resumen=await psy.GetStringAsync("/PruebasAplicadas/Resumen/"+a.Id);Check(resumen.Contains("7 / 10")&&System.Net.WebUtility.HtmlDecode(resumen).Contains("Promedio aritmético"),"resumen descriptivo por dimensión");
        foreach(var c in new[]{admin,director,otro,psyOtro})Check((await c.GetAsync("/PruebasAplicadas/Resumen/"+a.Id)).StatusCode==HttpStatusCode.Redirect,"resumen no expuesto a cuentas ajenas");
        Check((await HostPrueba.Post(estudiante,"/PruebasAplicadas","/PruebasAplicadas/Retirar",("id",a.Id.ToString()))).StatusCode==HttpStatusCode.Redirect,"retiro voluntario");
        Check((await psy.GetAsync("/PruebasAplicadas/Resumen/"+a.Id)).StatusCode==HttpStatusCode.Redirect,"retiro oculta resumen");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){await File.WriteAllTextAsync(Path.Combine(preview,"calendario-director.html"),cal);await File.WriteAllTextAsync(Path.Combine(preview,"calendario-estudiante.html"),citas);await File.WriteAllTextAsync(Path.Combine(preview,"catalogo-carreras.html"),await admin.GetStringAsync("/CarrerasAdmin"));await File.WriteAllTextAsync(Path.Combine(preview,"resumen-prueba.html"),resumen);}
        Console.WriteLine($"OK: {checks} comprobaciones de calendarios, catálogo, alertas e instrumentos con inducción.");
    }
}
