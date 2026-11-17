using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Seguridad;
using Zuni.Services;
using Zuni.Services.Agenda;

internal static class Mejoras15Checks
{
    internal static async Task Run()
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Mejoras 1.5: " + label); checks++; }
        var claveRfc = TotpZuni.Codificar(Encoding.ASCII.GetBytes("12345678901234567890"));
        foreach(var (segundos,esperado) in new (long,string)[]{(59,"287082"),(1111111109,"081804"),(1111111111,"050471"),(1234567890,"005924"),(2000000000,"279037"),(20000000000,"353130")})
            Check(TotpZuni.Codigo(claveRfc,segundos/30)==esperado,"vector RFC6238 " + segundos);
        Check(TotpZuni.Verificar(claveRfc,"287082",DateTimeOffset.FromUnixTimeSeconds(59),1) is null,"rechaza repetir el mismo paso");
        Check(TotpZuni.Verificar(claveRfc,"28708x",DateTimeOffset.FromUnixTimeSeconds(59),-1) is null,"rechaza código con letras");

        await using var conexion = new SqliteConnection("Data Source=:memory:"); await conexion.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conexion).Options;
        var reloj = new Reloj { Ahora = new DateTimeOffset(2026,11,16,15,0,0,TimeSpan.Zero) };
        var hasher = new HasherContador(); var password = "Ficticia-"+Guid.NewGuid().ToString("N")+"!";
        var perfil = new PerfilEstudiante { UsuarioId="mfa-est",Carne="9999268811",Carrera="Ejemplo",Telefono="12345678" };
        var cita = new Cita { PerfilEstudianteId=perfil.Id,PsicologoUsuarioId="mfa-psy",ProgramadaDirectamente=true,Estado=EstadoCita.Confirmada,
            FechaHoraInicioUtc=reloj.Ahora.UtcDateTime.AddDays(1),FechaHoraFinUtc=reloj.Ahora.UtcDateTime.AddDays(1).AddHours(1),DireccionAtencion="Oficina ficticia" };
        await using(var db = new OfflineDb(options))
        {
            await db.Database.EnsureCreatedAsync();
            foreach(var (id,rol) in new[]{("mfa-psy","Psicologo"),("mfa-otro","Psicologo"),("mfa-est","Estudiante")})
            {
                if(!db.Roles.Local.Any(r=>r.Id==rol)) db.Roles.Add(new IdentityRole(rol){Id=rol,NormalizedName=rol.ToUpperInvariant()});
                var u = new ApplicationUser { Id=id,FullName="Persona ficticia",Email=id+"@example.com",NormalizedEmail=(id+"@example.com").ToUpperInvariant(),UserName=id,NormalizedUserName=id.ToUpperInvariant(),SecurityStamp=id+"-stamp",IsActive=true };
                u.PasswordHash=hasher.HashPassword(u,password); db.Users.Add(u); db.UserRoles.Add(new(){UserId=id,RoleId=rol});
            }
            db.PerfilesEstudiante.Add(perfil); db.AsignacionesEstudiantePsicologo.Add(new(){PerfilEstudianteId=perfil.Id,PsicologoUsuarioId="mfa-psy"});
            db.Citas.Add(cita); await db.SaveChangesAsync();
        }
        await using var app = await HostPrueba.Crear(options,reloj,hasher,1000);
        using var anon = HostPrueba.Cliente(app); using var psy = HostPrueba.Cliente(app); using var otro = HostPrueba.Cliente(app); using var est = HostPrueba.Cliente(app);
        var home = await anon.GetStringAsync("/");
        Check(home.Contains("Denis Corp") && home.Contains("/deniscorp/programas/academy/") && home.Contains("/deniscorp/programas/fulleo/") && home.Contains("/deniscorp/zuni/"),"portada y tarjetas de programas");
        Check((await anon.GetAsync("/deniscorp/zuni/")).StatusCode==HttpStatusCode.OK,"portada de ZUNI conserva ruta propia");
        foreach(var ruta in new[]{"/deniscorp/programas/academy/","/deniscorp/programas/fulleo/"}) Check((await anon.GetAsync(ruta)).StatusCode==HttpStatusCode.OK,"presentación " + ruta);
        Check((await anon.GetAsync("/DobleFactor")).StatusCode==HttpStatusCode.Redirect,"configuración requiere sesión");
        await HostPrueba.Login(psy,"mfa-psy@example.com",password);
        using var sesionAnterior = HostPrueba.Cliente(app); await HostPrueba.Login(sesionAnterior,"mfa-psy@example.com",password);
        var sinClave = await HostPrueba.Post(psy,"/DobleFactor","/DobleFactor/Gestionar",("Contrasena","incorrecta"),("operacion","Preparar"));
        Check(!Regex.IsMatch(await sinClave.Content.ReadAsStringAsync(),"<code[^>]*>[A-Z2-7]{32}</code>"),"no prepara autenticador sin contraseña actual");
        var preparado = await HostPrueba.Post(psy,"/DobleFactor","/DobleFactor/Gestionar",("Contrasena",password),("operacion","Preparar"));
        var html = await preparado.Content.ReadAsStringAsync();
        var clave = Regex.Match(html,"<code[^>]*>([A-Z2-7]{32})</code>").Groups[1].Value;
        Check(clave.Length==32,"prepara clave manual sin habilitar todavía");
        await using(var db=new OfflineDb(options)) Check(!await db.Users.Where(u=>u.Id=="mfa-psy").Select(u=>u.TwoFactorEnabled).SingleAsync(),"pendiente no activa 2FA");
        var activado = await HostPrueba.Post(psy,"/DobleFactor","/DobleFactor/Gestionar",("Contrasena",password),("Codigo",TotpZuni.Codigo(clave,reloj.Ahora.ToUnixTimeSeconds()/30)),("operacion","Activar"));
        var codigos = Regex.Matches(await activado.Content.ReadAsStringAsync(),"<code>([A-F0-9]{20})</code>").Select(m=>m.Groups[1].Value).ToArray();
        Check(codigos.Length==10 && codigos.Distinct().Count()==10,"diez códigos de recuperación una sola vez");
        Check((await sesionAnterior.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"activar invalida sesiones anteriores");
        Check((await psy.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"activar cierra sesión actual");
        var login = await HostPrueba.Login(psy,"mfa-psy@example.com",password);
        Check(login.Headers.Location?.ToString().Contains("VerificarDobleFactor")==true,"contraseña correcta pide segundo factor");
        Check((await psy.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"reto pendiente no autoriza la cuenta");
        Check((await psy.PostAsync("/Cuenta/VerificarDobleFactor",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("codigo",codigos[0])}))).StatusCode==HttpStatusCode.BadRequest,"segundo factor exige antiforgery");
        reloj.Ahora = reloj.Ahora.AddSeconds(30);
        var verificado = await HostPrueba.Post(psy,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo",TotpZuni.Codigo(clave,reloj.Ahora.ToUnixTimeSeconds()/30)));
        Check(verificado.StatusCode==HttpStatusCode.Redirect && (await psy.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.OK,"TOTP abre sesión completa");
        using var mismoTotp = HostPrueba.Cliente(app); await HostPrueba.Login(mismoTotp,"mfa-psy@example.com",password);
        await HostPrueba.Post(mismoTotp,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo",TotpZuni.Codigo(clave,reloj.Ahora.ToUnixTimeSeconds()/30)));
        Check((await mismoTotp.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"TOTP usado no permite otra sesión");
        using var recuperacion = HostPrueba.Cliente(app); await HostPrueba.Login(recuperacion,"mfa-psy@example.com",password);
        var respaldo = await HostPrueba.Post(recuperacion,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo",codigos[0]));
        Check(respaldo.StatusCode==HttpStatusCode.Redirect,"recuperación abre sesión");
        using var replay = HostPrueba.Cliente(app); await HostPrueba.Login(replay,"mfa-psy@example.com",password);
        await HostPrueba.Post(replay,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo",codigos[0]));
        Check((await replay.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"recuperación no se reutiliza");
        reloj.Ahora = reloj.Ahora.AddMinutes(6);
        await HostPrueba.Post(replay,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo",codigos[1]));
        Check((await replay.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"reto vencido no abre sesión");
        await HostPrueba.Login(replay,"mfa-psy@example.com",password);
        for(var i=0;i<5;i++) await HostPrueba.Post(replay,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo","incorrecto"));
        var bloqueado = await HostPrueba.Login(replay,"mfa-psy@example.com",password);
        Check(bloqueado.StatusCode==HttpStatusCode.OK,"contraseña correcta no borra bloqueo de 2FA");
        await using(var db=new OfflineDb(options))
        {
            var c=await db.Set<DobleFactor>().SingleAsync();
            Check(c.BloqueadoHastaUtc>reloj.Ahora.UtcDateTime && !c.SecretoProtegido!.Contains(clave) && !c.CodigosHash.Contains(codigos[1]),"bloqueo durable y secretos protegidos");
        }
        reloj.Ahora = reloj.Ahora.AddMinutes(16);
        // El psicólogo ya autenticado conserva su sesión durante el bloqueo de nuevos accesos.
        await HostPrueba.Login(otro,"mfa-otro@example.com",password); await HostPrueba.Login(est,"mfa-est@example.com",password);
        var ausencia = "/Ausencias/Avisar?citaId="+cita.Id;
        Check((await otro.GetAsync(ausencia)).Headers.Location?.ToString().Contains("AccesoDenegado")==true,"otro profesional no consulta la cita");
        Check((await est.GetAsync(ausencia)).StatusCode==HttpStatusCode.Redirect,"estudiante no avisa ausencia profesional");
        Check((await psy.PostAsync("/Ausencias/Confirmar",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("citaId",cita.Id.ToString())}))).StatusCode==HttpStatusCode.BadRequest,"ausencia exige antiforgery");
        await HostPrueba.Post(psy,ausencia,"/Ausencias/Confirmar",("citaId",cita.Id.ToString()),("revision",Guid.NewGuid().ToString()));
        await using(var db=new OfflineDb(options)) Check((await db.Citas.SingleAsync()).Estado==EstadoCita.Confirmada&&!await db.NotificacionesCita.AnyAsync(),"versión obsoleta no notifica ni modifica");
        var aviso = await HostPrueba.Post(psy,ausencia,"/Ausencias/Confirmar",("citaId",cita.Id.ToString()),("revision",cita.Revision.ToString()));
        Check(aviso.StatusCode==HttpStatusCode.Redirect,"psicólogo avisa ausencia");
        await using(var db=new OfflineDb(options))
        {
            Check((await db.Citas.SingleAsync()).Estado==EstadoCita.AusenciaProfesional,"estado distinto de inasistencia estudiantil");
            var n=await db.NotificacionesCita.SingleAsync(); Check(n.UsuarioId=="mfa-est"&&n.AutorUsuarioId=="mfa-psy"&&!n.Leida,"aviso al alumno asignado sin leer");
            Check(!await db.RegistrosAtencionCita.AnyAsync(),"no crea atención clínica ficticia");
        }
        var notificaciones = await est.GetStringAsync("/CitasProgramadas/Notificaciones");
        Check((await est.GetStringAsync("/CitasProgramadas/Pendientes")).Contains("\"cantidad\":1"),"contador del alumno incluye la ausencia sin recargar el listado");
        Check((await otro.GetStringAsync("/CitasProgramadas/Pendientes")).Contains("\"cantidad\":0"),"contador de otro profesional no revela avisos ajenos");
        Check(notificaciones.Contains("Tu psicólogo avisó")||WebUtility.HtmlDecode(notificaciones).Contains("Tu psicólogo avisó"),"alumno consulta aviso desde sus notificaciones");
        await HostPrueba.Post(psy,ausencia,"/Ausencias/Confirmar",("citaId",cita.Id.ToString()),("revision",cita.Revision.ToString()));
        await using(var db=new OfflineDb(options)) Check(await db.NotificacionesCita.CountAsync()==1,"doble envío no duplica aviso");
        var htmlAusencia=await psy.GetStringAsync(ausencia); Check(htmlAusencia.Contains("Programar nueva fecha"),"acceso directo a nueva fecha tras ausencia");
        using(var scope=app.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user=await db.Users.SingleAsync(u=>u.Id=="mfa-psy");
            var principal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,user.Id),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,"Psicologo"),new System.Security.Claims.Claim("Zuni.SecurityStamp",user.SecurityStamp!)},"prueba"));
            var agenda=scope.ServiceProvider.GetRequiredService<AgendaCitasService>();
            var input=await agenda.PrepararProgramacion(principal,cita.Id,null,default);
            Check(input.CitaId is null&&input.SustituyeCitaId==cita.Id&&input.PerfilEstudianteId==perfil.Id,"nueva fecha conserva origen y alumno");
            input.Fecha=AgendaReglas.Hoy(cita.FechaHoraInicioUtc);input.HoraInicio=TimeOnly.FromDateTime(AgendaReglas.Local(cita.FechaHoraInicioUtc));input.DuracionMinutos=50;
            try {await agenda.GuardarProgramacion(principal,input,default);throw new Exception("se permitió reservar el horario ausente");} catch(AgendaOperacionException){checks++;}
            input.Fecha=input.Fecha.Value.AddDays(1);input.OperacionId=Guid.NewGuid();
            var nuevo=await agenda.GuardarProgramacion(principal,input,default);
            Check(await db.Citas.AnyAsync(c=>c.Id==nuevo&&c.SustituyeCitaId==cita.Id),"crea nueva cita vinculada");
            Check(await db.Citas.AnyAsync(c=>c.Id==cita.Id&&c.Estado==EstadoCita.AusenciaProfesional),"original conserva fecha e historial");
            Check(await db.NotificacionesCita.CountAsync()==2,"nueva fecha envía otro aviso");
            try{await agenda.PrepararProgramacion(principal,cita.Id,null,default);throw new Exception("se permitió segunda sustitución");}catch(AgendaOperacionException){checks++;}
        }
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(preview))
        {
            Directory.CreateDirectory(preview);
            await File.WriteAllTextAsync(Path.Combine(preview,"deniscorp.html"),home);
            await File.WriteAllTextAsync(Path.Combine(preview,"academy.html"),await anon.GetStringAsync("/deniscorp/programas/academy/"));
            await File.WriteAllTextAsync(Path.Combine(preview,"ausencia.html"),htmlAusencia);
            await File.WriteAllTextAsync(Path.Combine(preview,"2fa.html"),await psy.GetStringAsync("/DobleFactor"));
            await File.WriteAllTextAsync(Path.Combine(preview,"2fa-preparado.html"),html);
        }
        var renovar = await HostPrueba.Post(psy,"/DobleFactor","/DobleFactor/Gestionar",("Contrasena",password),("Codigo",codigos[1]),("operacion","Renovar"));
        var nuevos = Regex.Matches(await renovar.Content.ReadAsStringAsync(),"<code>([A-F0-9]{20})</code>").Select(m=>m.Groups[1].Value).ToArray();
        Check(nuevos.Length==10&&!nuevos.Intersect(codigos).Any(),"renovar emite nuevos códigos");
        Check((await recuperacion.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"renovar invalida otras sesiones");
        var loginExterno=await HostPrueba.Post(psy,"/Cuenta/IniciarSesion","/Cuenta/IniciarSesion",("Email","mfa-psy@example.com"),("Password",password),("ReturnUrl","https://ejemplo-malicioso.test/"));
        Check(loginExterno.Headers.Location?.ToString().Contains("VerificarDobleFactor")==true,"renovar conserva exigencia de 2FA");
        await HostPrueba.Post(psy,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo",codigos[2]));
        Check((await psy.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"códigos antiguos invalidados al renovar");
        var conNuevo=await HostPrueba.Post(psy,"/Cuenta/VerificarDobleFactor","/Cuenta/VerificarDobleFactor",("codigo",nuevos[0]));
        Check(conNuevo.StatusCode==HttpStatusCode.Redirect&&!conNuevo.Headers.Location!.ToString().Contains("ejemplo-malicioso"),"nuevo respaldo funciona sin redirección externa");
        var sinSegundo=await HostPrueba.Post(psy,"/DobleFactor","/DobleFactor/Gestionar",("Contrasena",password),("Codigo","incorrecto"),("operacion","Desactivar"));
        await using(var db=new OfflineDb(options)) Check(await db.Users.Where(u=>u.Id=="mfa-psy").Select(u=>u.TwoFactorEnabled).SingleAsync(),"desactivar exige segundo factor válido");
        await HostPrueba.Post(psy,"/DobleFactor","/DobleFactor/Gestionar",("Contrasena",password),("Codigo",nuevos[1]),("operacion","Desactivar"));
        var sinMfa=await HostPrueba.Login(psy,"mfa-psy@example.com",password);
        Check(sinMfa.Headers.Location?.ToString().Contains("VerificarDobleFactor")!=true&&(await psy.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.OK,"desactivación autenticada vuelve a acceso con contraseña");
        using var contadorCliente=HostPrueba.Cliente(app);
        await HostPrueba.Login(contadorCliente,"mfa-est@example.com",password);
        reloj.Ahora=reloj.Ahora.AddMinutes(16);
        var conteo=await contadorCliente.GetAsync("/CitasProgramadas/Pendientes");
        Check(conteo.StatusCode==HttpStatusCode.OK&&!conteo.Headers.Contains("Set-Cookie"),"consulta automática no renueva la cookie al superar media vida");
        await app.StopAsync(); Console.WriteLine($"OK: {checks} comprobaciones de 2FA, ausencias, notificaciones y Denis Corp.");
    }
}
