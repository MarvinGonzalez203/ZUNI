using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Services.Agenda;

internal static class AgendaMejorasChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj, Guid perfil)
    {
        int checks = 0; void Check(bool ok, string label) { if (!ok) throw new Exception("Mejoras agenda: " + label); checks++; }
        using var anon = HostPrueba.Cliente(app); using var psy = HostPrueba.Cliente(app); using var student = HostPrueba.Cliente(app); using var other = HostPrueba.Cliente(app); using var admin = HostPrueba.Cliente(app); using var director = HostPrueba.Cliente(app);
        await HostPrueba.Login(psy,"psi3@miumg.edu.gt",clave); await HostPrueba.Login(student,"est2@miumg.edu.gt",clave); await HostPrueba.Login(other,"est@miumg.edu.gt",clave); await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave); await HostPrueba.Login(director,"director@miumg.edu.gt",clave);
        foreach(var tema in new[]{"cita","seguimiento","bienestar","privacidad"}) Check((await anon.GetAsync("/Home/Guia?tema="+tema)).StatusCode == HttpStatusCode.OK,"guía pública sin login");
        Check((await anon.GetAsync("/Home/Guia?tema=desconocido")).StatusCode == HttpStatusCode.NotFound,"tema desconocido rechazado");
        var hoy = AgendaReglas.Hoy(reloj.Ahora.UtcDateTime); var fecha = hoy.AddDays(-4);
        var normal = new HorarioDiaInput { Fecha = hoy.AddDays(2), HoraInicio = new(9,0), DuracionMinutos = 50 };
        AgendaReglas.ValidarHorarioDia(normal,reloj.Ahora.UtcDateTime); Check(normal.HoraFin == new TimeOnly(9,50),"fin omitido calculado con 50 minutos");
        var viejo = new HorarioDiaInput { Fecha = hoy, HoraInicio = new(8,0), HoraFin = new(17,0), DuracionMinutos = 50 };
        try { AgendaReglas.ValidarHorarioDia(viejo,AgendaReglas.Utc(hoy,new(10,0))); throw new Exception("hora pasada admitida"); } catch(AgendaOperacionException) { checks++; }
        var operacion=Guid.NewGuid();
        async Task<HttpResponseMessage> Save(Guid perfilId, DateOnly dia, string inicio="09:00", Guid? op=null) => await HostPrueba.Post(psy,"/Atenciones/Imprevista?fecha="+fecha.ToString("yyyy-MM-dd"),"/Atenciones/Imprevista",("OperacionId",(op??Guid.NewGuid()).ToString()),("PerfilEstudianteId",perfilId.ToString()),("Fecha",dia.ToString("yyyy-MM-dd")),("HoraInicio",inicio),("Modalidad","Presencial"));
        Check((await psy.PostAsync("/Atenciones/Imprevista",new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest,"imprevista antiforgery");
        Check((await student.GetAsync("/Atenciones/Imprevista")).StatusCode == HttpStatusCode.Redirect,"estudiante no registra emergencia");
        Check((await Save(perfil,hoy.AddDays(1))).StatusCode == HttpStatusCode.Conflict,"imprevista futura rechazada");
        Check((await Save(Guid.NewGuid(),fecha)).StatusCode == HttpStatusCode.Redirect,"estudiante ajeno denegado");
        Check((await Save(perfil,fecha,op:operacion)).StatusCode == HttpStatusCode.Redirect,"actividad pasada sin disponibilidad previa");
        Check((await Save(perfil,fecha,op:operacion)).StatusCode == HttpStatusCode.Conflict,"doble envío no duplica");
        Check((await Save(perfil,fecha,"09:15")).StatusCode == HttpStatusCode.Conflict,"solapamiento no crea duplicados");
        await using(var db = new OfflineDb(options)) {
            var cita=await db.Citas.SingleAsync(c=>c.Id==operacion);
            var audit = await db.AuditoriaUsuarios.SingleAsync(a=>a.Accion=="ATENCION_IMPREVISTA_REGISTRADA");
            using var json = System.Text.Json.JsonDocument.Parse(audit.DatosNuevos!);
            Check(json.RootElement.GetGuid()==operacion,"referencia de auditoría válida para jsonb de PostgreSQL");
            Check(cita.EsImprevista && cita.FinAproximado && cita.DisponibilidadPsicologoId is null && cita.FechaHoraFinUtc == cita.FechaHoraInicioUtc.AddMinutes(50),"registro con fin aproximado y sin horario artificial");
            var solicitud = await db.SolicitudesAtencion.Where(s=>s.PerfilEstudianteId==perfil && db.ConsentimientosAtencion.Any(c=>c.SolicitudAtencionId==s.Id&&c.Aceptado)).OrderByDescending(s=>s.FechaSolicitudUtc).FirstAsync();
            cita.Estado=EstadoCita.Completada;
            db.RegistrosAtencionCita.Add(new RegistroAtencionCita{CitaId=cita.Id,SolicitudAtencionId=solicitud.Id,Asistio=true,ResultadoProfesionalPrivado="SECRETO-NO-IMPRIMIR-110CT",ResultadoBreveEstudiante="RESULTADO-COMPARTIDO-110CT",ResenaRecomendacionesEstudiante="RECOMENDACIONES-COMPARTIDAS-110CT",FechaCierreUtc=reloj.Ahora.UtcDateTime});
            await db.SaveChangesAsync();
        }
        var agenda=await psy.GetStringAsync("/Psicologo/Agenda?fecha="+fecha.ToString("yyyy-MM-dd"));
        Check(agenda.Contains("Ver reporte compartido y reimprimir") && agenda.Contains(operacion.ToString()),"cita pasada en ventana del día con reimpresión");
        var anterior=await psy.GetAsync("/Psicologo/Agenda?fecha="+hoy.AddMonths(-1).ToString("yyyy-MM-dd")); Check(anterior.StatusCode==HttpStatusCode.OK,"navegación a mes pasado");
        var url="/Atenciones/Reporte?citaId="+operacion;
        var html=await student.GetStringAsync(url); Check(html.Contains("RESULTADO-COMPARTIDO-110CT")&&!html.Contains("SECRETO-NO-IMPRIMIR-110CT"),"reporte estudiante excluye notas privadas");
        Check((await psy.GetStringAsync(url)).Contains("RESULTADO-COMPARTIDO-110CT"),"profesional autorizado reimprime");
        foreach(var c in new[]{other,admin,director,anon}) Check((await c.GetAsync(url)).StatusCode==HttpStatusCode.Redirect,"reporte privado ante otros roles y estudiantes");
        var pdf=await psy.GetAsync("/Atenciones/Pdf?citaId="+operacion); var bytes=await pdf.Content.ReadAsByteArrayAsync();
        Check(pdf.StatusCode==HttpStatusCode.OK&&pdf.Content.Headers.ContentType?.MediaType=="application/pdf"&&System.Text.Encoding.ASCII.GetString(bytes,0,4)=="%PDF","reporte PDF generado");
        Check(typeof(ReporteCompartido).GetProperties().All(p=>p.Name!="ResultadoProfesionalPrivado"),"DTO imprimible no admite notas privadas");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(preview)) { await File.WriteAllTextAsync(Path.Combine(preview,"agenda-mejoras.html"),agenda); await File.WriteAllTextAsync(Path.Combine(preview,"reporte-compartido.html"),html); await File.WriteAllBytesAsync(Path.Combine(preview,"reporte-compartido.pdf"),bytes); await File.WriteAllTextAsync(Path.Combine(preview,"guia-publica.html"),await anon.GetStringAsync("/Home/Guia?tema=cita")); }
        Console.WriteLine($"OK: {checks} comprobaciones de guías, imprevistas, agenda histórica y reporte compartido.");
    }
}
