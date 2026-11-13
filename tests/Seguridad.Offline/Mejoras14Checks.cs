using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Resources;
using Zuni.Services.Correo;

internal static class Mejoras14Checks
{
    internal sealed class CorreoFicticio : ICorreoPin
    {
        public bool Disponible=>true;public string UrlPublica=>"https://zuni.test/";
        public string? Destinatario;public string? Enlace;public int Envios;
        public Task Enviar(string destinatario,string enlace,CancellationToken ct){Destinatario=destinatario;Enlace=enlace;Envios++;return Task.CompletedTask;}
    }
    internal static async Task Run()
    {
        var checks=0;void Check(bool ok,string mensaje){if(!ok)throw new Exception("Mejoras 1.4: "+mensaje);checks++;}
        foreach(var (a,b) in new[]{("kuanto","cuánto"),("bendi","vendí"),("aller","ayer"),("komo","cómo"),("rejistro","registro"),("avono","abono"),("psiclogo","psicólogo"),("asiganr","asignar")})Check(BusquedaGuias.Equivale(a,b),"fonética y erratas "+a);
        Check(GuiasUso.Buscar("komo",null).Count==GuiasUso.Todas.Length,"pregunta introductoria con falta no oculta todas las guías");
        Check(GuiasUso.Buscar("komo asiganr psikologo",null).Any(g=>g.Titulo=="Asignar un psicólogo a un estudiante"),"pregunta con varias erratas encuentra asignación");
        Check(GuiasUso.Buscar("xq q oy",null).Count>0,"abreviaturas y periodo reconocido");
        var hoy=new DateOnly(2026,11,13);
        foreach(var (texto,desde,hasta) in new[]{("hoy","2026-11-13","2026-11-13"),("ayer","2026-11-12","2026-11-12"),("anteayer","2026-11-11","2026-11-11"),("esta semana","2026-11-09","2026-11-15"),("la semana pasada","2026-11-02","2026-11-08"),("este mes","2026-11-01","2026-11-30"),("el mes pasado","2026-10-01","2026-10-31"),("este año","2026-01-01","2026-12-31"),("el año pasado","2025-01-01","2025-12-31"),("últimos 7 días","2026-11-07","2026-11-13"),("el lunes","2026-11-09","2026-11-09"),("en octubre","2026-10-01","2026-10-31"),("15/10","2026-10-15","2026-10-15"),("3 de noviembre","2026-11-03","2026-11-03"),("citas aller","2026-11-12","2026-11-12")}){
            var p=PeriodoGuia.Leer(texto,hoy);Check(p?.Desde==DateOnly.Parse(desde)&&p.Hasta==DateOnly.Parse(hasta),"periodo "+texto);
        }
        Check(PeriodoGuia.Leer("31/02",hoy) is null,"fecha inexistente no se inventa");
        foreach(var g in GuiasUso.Todas)Check(!string.IsNullOrEmpty(AccesosGuias.Enlace(g,g.Rol=="General"?"Psicologo":g.Rol)),"cada guía tiene acceso: "+g.Titulo);

        await using var conexion=new SqliteConnection("Data Source=:memory:");await conexion.OpenAsync();
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conexion).Options;var reloj=new Reloj();var hasher=new HasherContador();var correo=new CorreoFicticio();
        var password="Ficticia-"+Guid.NewGuid().ToString("N")+"!";var perfil=new PerfilEstudiante{UsuarioId="pin-est",Carne="9999267711",Carrera="Ejemplo",Telefono="12345678"};var citaId=Guid.NewGuid();
        await using(var db=new OfflineDb(options)){
            await db.Database.EnsureCreatedAsync();
            foreach(var rol in new[]{"Psicologo","Estudiante","Administrador"})db.Roles.Add(new IdentityRole(rol){Id=rol,NormalizedName=rol.ToUpperInvariant()});
            foreach(var (id,rol) in new[]{("pin-psy","Psicologo"),("pin-otro","Psicologo"),("pin-est","Estudiante"),("pin-admin","Administrador")}){
                var u=new ApplicationUser{Id=id,FullName="Persona ficticia",Email=id+"@example.com",UserName=id+"@example.com",NormalizedEmail=(id+"@example.com").ToUpperInvariant(),NormalizedUserName=(id+"@example.com").ToUpperInvariant(),IsActive=true,SecurityStamp=Guid.NewGuid().ToString()};u.PasswordHash=hasher.HashPassword(u,password);db.Users.Add(u);db.UserRoles.Add(new(){UserId=id,RoleId=rol});
            }
            db.PerfilesEstudiante.Add(perfil);db.AsignacionesEstudiantePsicologo.Add(new(){PerfilEstudianteId=perfil.Id,PsicologoUsuarioId="pin-psy"});
            db.Citas.Add(new(){Id=citaId,PerfilEstudianteId=perfil.Id,PsicologoUsuarioId="pin-psy",EsImprevista=true,ProgramadorRol="Psicologo",ProgramadorUsuarioId="pin-psy",Estado=EstadoCita.Completada,FechaHoraInicioUtc=reloj.Ahora.UtcDateTime.AddHours(-2),FechaHoraFinUtc=reloj.Ahora.UtcDateTime.AddHours(-1),Modalidad=ModalidadCita.Presencial});
            db.RegistrosAtencionCita.Add(new(){CitaId=citaId,Asistio=true,ResultadoProfesionalPrivado="SECRETO-PRIVADO-PIN",ResultadoBreveEstudiante="Resultado compartido ficticio",ResenaRecomendacionesEstudiante="Acuerdo ficticio",FechaCierreUtc=reloj.Ahora.UtcDateTime.AddHours(-1)});
            await db.SaveChangesAsync();
        }
        await using var app=await HostPrueba.Crear(options,reloj,hasher,100,correo);
        using var psy=HostPrueba.Cliente(app);using var otro=HostPrueba.Cliente(app);using var est=HostPrueba.Cliente(app);using var anon=HostPrueba.Cliente(app);
        await HostPrueba.Login(psy,"pin-psy@example.com",password);await HostPrueba.Login(otro,"pin-otro@example.com",password);await HostPrueba.Login(est,"pin-est@example.com",password);
        var reporte="/Atenciones/Reporte?citaId="+citaId;
        var page=await psy.GetStringAsync(reporte);Check(page.Contains("data-pin-unlock")&&!page.Contains("SECRETO-PRIVADO-PIN"),"reporte bloqueado no entrega privado al DOM");
        var detalle=await psy.GetStringAsync("/Psicologo/VerAtencion?citaId="+citaId);Check(!detalle.Contains("SECRETO-PRIVADO-PIN")&&detalle.Contains("data-pin-unlock"),"ruta antigua tampoco evita el PIN");
        Check(!(await est.GetStringAsync(reporte)).Contains("data-pin-unlock"),"estudiante solo ve contenido compartido");
        Check((await psy.PostAsync("/PinProfesional/Desbloquear",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("citaId",citaId.ToString()),new("pin","0384")}))).StatusCode==HttpStatusCode.BadRequest,"desbloquear exige antiforgery");
        async Task<HttpResponseMessage> Config(string pass,string pin="0384")=>await HostPrueba.Post(psy,"/PinProfesional","/PinProfesional/Configurar",("Contrasena",pass),("Pin",pin),("Confirmacion",pin));
        await Config("incorrecta");await using(var db=new OfflineDb(options))Check(!(await db.PinesProfesionales.SingleAsync()).HashProtegido?.Contains("0384")??true,"contraseña incorrecta no configura PIN");
        Check((await Config(password)).StatusCode==HttpStatusCode.Redirect,"configuración con contraseña actual");
        await using(var db=new OfflineDb(options)){var p=await db.PinesProfesionales.SingleAsync();Check(p.HashProtegido is not null&&!p.HashProtegido.Contains("0384")&&!p.HashProtegido.Contains("AQAAAA"),"PIN no se guarda en claro y hash protegido con Data Protection");}
        Task<HttpResponseMessage> Unlock(HttpClient c,string pin)=>HostPrueba.Post(c,reporte,"/PinProfesional/Desbloquear",("citaId",citaId.ToString()),("pin",pin));
        var unlocked=await Unlock(psy,"0384");Check(unlocked.StatusCode==HttpStatusCode.OK&&(await unlocked.Content.ReadAsStringAsync()).Contains("SECRETO-PRIVADO-PIN"),"PIN correcto devuelve únicamente la nota profesional");
        Check(unlocked.Headers.CacheControl?.NoStore==true,"respuesta privada no se almacena en caché");
        Check(!(await psy.GetStringAsync(reporte)).Contains("SECRETO-PRIVADO-PIN"),"otra petición vuelve a quedar bloqueada");
        Check((await HostPrueba.Post(otro,"/PinProfesional","/PinProfesional/Desbloquear",("citaId",citaId.ToString()),("pin","0384"))).StatusCode!=HttpStatusCode.OK,"otro psicólogo no hereda las notas ni con el mismo PIN");
        Check((await HostPrueba.Post(est,"/MiCuenta","/PinProfesional/Desbloquear",("citaId",citaId.ToString()),("pin","0384"))).StatusCode!=HttpStatusCode.OK,"estudiante no puede desbloquear por POST");
        for(var i=0;i<5;i++)Check((await Unlock(psy,"9999")).StatusCode==HttpStatusCode.Conflict,"intento erróneo persistido "+i);
        Check((await Unlock(psy,"0384")).StatusCode==HttpStatusCode.Conflict,"bloqueo por cuenta incluso con PIN correcto tras cinco fallos");
        await HostPrueba.Post(psy,"/PinProfesional","/PinProfesional/EnviarRecuperacion");
        Check(correo.Envios==1&&correo.Destinatario=="pin-psy@example.com"&&correo.Enlace!.Contains("#usuario="),"enlace enviado solo al correo registrado, token fuera de query y logs");
        var fragment=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(correo.Enlace!.Split('#')[1]);var token=fragment["token"].ToString();
        await using(var db=new OfflineDb(options)){var p=await db.PinesProfesionales.SingleAsync();Check(p.RecuperacionHash is not null&&!p.RecuperacionHash.Contains(token),"token de recuperación solo almacenado como hash");}
        await HostPrueba.Post(psy,"/PinProfesional","/PinProfesional/EnviarRecuperacion");Check(correo.Envios==1,"espera entre solicitudes de correo");
        Task<HttpResponseMessage> Reset(string tok,string pin)=>HostPrueba.Post(anon,"/PinProfesional/Recuperar","/PinProfesional/Recuperar",("Usuario","pin-psy"),("Token",tok),("Pin",pin),("Confirmacion",pin));
        var invalido=await Reset("invalido","0415");Check(!(await invalido.Content.ReadAsStringAsync()).Contains("El enlace quedó invalidado"),"token inválido no restablece el PIN");
        var reset=await Reset(token,"0415");Check((await HostPrueba.Texto(reset)).Contains("PIN actualizado"),"enlace válido restablece el PIN y el bloqueo");
        Check((await Unlock(psy,"0415")).StatusCode==HttpStatusCode.OK,"nuevo PIN permite consulta");
        await Reset(token,"7777");Check((await Unlock(psy,"0415")).StatusCode==HttpStatusCode.OK,"el enlace no se puede reutilizar");
        reloj.Ahora=reloj.Ahora.AddMinutes(3);await HostPrueba.Post(psy,"/PinProfesional","/PinProfesional/EnviarRecuperacion");
        var expired=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(correo.Enlace!.Split('#')[1])["token"].ToString();reloj.Ahora=reloj.Ahora.AddMinutes(21);
        await Reset(expired,"7777");
        await using(var db=new OfflineDb(options))Check((await db.PinesProfesionales.SingleAsync()).RecuperacionHash is not null,"enlace vencido no cambia ni consume un PIN válido");
        var relojContrato=new Reloj{Ahora=new DateTimeOffset(2028,1,31,18,0,0,TimeSpan.Zero)};
        foreach(var (meses,importe) in new[]{(1,199m),(3,567m),(6,1075m),(12,2030m)}){
            await using var db=new OfflineDb(options);var input=new Zuni.Models.Comercial.EditarCentroInput{Nombre="Centro ficticio",MesesPlan=meses,Inicio=new(2000,1,1),Fin=new(2200,12,31),ImporteGtq=1};
            await Zuni.Services.Institucion.CentrosComercialesService.AplicarPlan(db,input,relojContrato,default);
            Check(input.Inicio==new DateOnly(2028,1,31)&&input.Fin==new DateOnly(2028,1,31).AddMonths(meses).AddDays(-1)&&input.ImporteGtq==importe,"tarifa y fechas del plan calculadas por servidor "+meses);
        }
        var fixture=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(fixture)){
            Directory.CreateDirectory(fixture);await File.WriteAllTextAsync(Path.Combine(fixture,"pin-reporte.html"),page);await File.WriteAllTextAsync(Path.Combine(fixture,"pin-cuenta.html"),await psy.GetStringAsync("/PinProfesional"));await File.WriteAllTextAsync(Path.Combine(fixture,"pin-recuperar.html"),await anon.GetStringAsync("/PinProfesional/Recuperar"));await File.WriteAllTextAsync(Path.Combine(fixture,"pin-desbloqueado.html"),await unlocked.Content.ReadAsStringAsync());
        }
        await HostPrueba.Post(psy,"/PinProfesional","/PinProfesional/EnviarRecuperacion");
        var tokenAntesCorreo=Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(correo.Enlace!.Split('#')[1])["token"].ToString();
        async Task<HttpResponseMessage> CambiarCorreo(string pin){
            var html=await psy.GetStringAsync("/MiCuenta/Editar");
            var revision=WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(html,"name=\"Revision\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            return await HostPrueba.Post(psy,"/MiCuenta/Editar","/MiCuenta/Editar",("NombreCompleto","Persona ficticia"),("Correo","nuevo-pin@miumg.edu.gt"),("Telefono",""),("Revision",revision),("PinParaCorreo",pin),("TienePinProfesional","false"));
        }
        Check((await CambiarCorreo("")).StatusCode==HttpStatusCode.OK,"cambiar correo exige PIN aunque se manipule el indicador del formulario");
        await using(var db=new OfflineDb(options))Check((await db.Users.SingleAsync(u=>u.Id=="pin-psy")).Email=="pin-psy@example.com","sin PIN no se redirige la recuperación a otro correo");
        await CambiarCorreo("9999");
        await using(var db=new OfflineDb(options))Check((await db.PinesProfesionales.SingleAsync()).IntentosFallidos==2,"fallos de PIN al editar perfil persisten aunque no se guarde el perfil");
        Check((await CambiarCorreo("0415")).StatusCode==HttpStatusCode.Redirect,"PIN correcto permite cambiar correo");
        string hashAntes;await using(var db=new OfflineDb(options)){Check((await db.Users.SingleAsync(u=>u.Id=="pin-psy")).Email=="nuevo-pin@miumg.edu.gt","nuevo correo guardado solo tras confirmar PIN");hashAntes=(await db.PinesProfesionales.SingleAsync()).HashProtegido!;}
        await Reset(tokenAntesCorreo,"7777");
        await using(var db=new OfflineDb(options))Check((await db.PinesProfesionales.SingleAsync()).HashProtegido==hashAntes,"cambiar correo invalida el enlace de recuperación anterior aunque no haya vencido");
        await app.StopAsync();Console.WriteLine($"OK: {checks} comprobaciones de guías, periodos, accesos, PIN, bloqueo y recuperación sin correo real.");
    }
}
