using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

internal static class AdministradorEdicionChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave)
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception("Edición administrativa: " + text); checks++; }
        string Hidden(string html, string name) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"" + name + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
        await using (var db = new OfflineDb(options))
        {
            var user = new ApplicationUser { Id="admin-edicion", FullName="Administrador Edición", Email="edicion@miumg.edu.gt", UserName="edicion@miumg.edu.gt", NormalizedEmail="EDICION@MIUMG.EDU.GT", NormalizedUserName="EDICION@MIUMG.EDU.GT", SecurityStamp=Guid.NewGuid().ToString(), ConcurrencyStamp=Guid.NewGuid().ToString(), IsActive=true, LockoutEnabled=true };
            user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, clave);
            db.Users.Add(user); db.UserRoles.Add(new IdentityUserRole<string> { UserId=user.Id, RoleId="Administrador" });
            db.PasswordResetTokens.Add(new PasswordResetToken { UserId=user.Id, TokenHash="solo-fixture-edicion", ExpiresAtUtc=DateTime.UtcNow.AddHours(1) });
            await db.SaveChangesAsync();
        }
        using var admin = HostPrueba.Cliente(app); using var otra = HostPrueba.Cliente(app); using var estudiante = HostPrueba.Cliente(app);
        await HostPrueba.Login(admin,"edicion@miumg.edu.gt",clave); await HostPrueba.Login(otra,"edicion@miumg.edu.gt",clave); await HostPrueba.Login(estudiante,"est2@miumg.edu.gt",clave);
        var html = await admin.GetStringAsync("/MiCuenta/Editar");
        Check(html.Contains("name=\"Correo\"") && html.Contains("name=\"Telefono\""), "formulario propio incluye contacto");
        var oldRevision = Hidden(html,"Revision");
        async Task<HttpResponseMessage> Edit(string correo, string telefono, string? revision=null) => await HostPrueba.Post(admin,"/MiCuenta/Editar","/MiCuenta/Editar",("Revision",revision ?? Hidden(await admin.GetStringAsync("/MiCuenta/Editar"),"Revision")),("NombreCompleto","Admin corregido"),("Correo",correo),("Telefono",telefono));
        Check((await Edit("admin@miumg.edu.gt","12345678")).StatusCode==HttpStatusCode.OK,"correo duplicado rechazado");
        Check((await Edit("edicion@miumg.edu.gt","abc")).StatusCode==HttpStatusCode.OK,"teléfono inválido rechazado");
        Check((await Edit("edicion@miumg.edu.gt","12345678")).StatusCode==HttpStatusCode.Redirect,"nombre y teléfono guardados");
        Check((await admin.GetStringAsync("/MiCuenta")).Contains("12345678"),"teléfono visible en cuenta");
        Check((await Edit("obsoleto@miumg.edu.gt","87654321",oldRevision)).StatusCode==HttpStatusCode.Redirect,"edición obsoleta rechazada");
        await using (var db = new OfflineDb(options)) Check((await db.Users.FindAsync("admin-edicion"))!.Email=="edicion@miumg.edu.gt","edición obsoleta no modifica correo");
        var studentHtml = await estudiante.GetStringAsync("/MiCuenta/Editar");
        Check(studentHtml.Contains("name=\"Correo\""),"estudiante puede editar su propio correo con validación");
        var ficha = await admin.GetStringAsync("/Administrador/Gestionar/est2"); var revisionNombre = Hidden(ficha,"Revision");
        var reject = await estudiante.PostAsync("/Administrador/EditarNombre",new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"]=HostPrueba.Token(studentHtml),["UsuarioId"]="est2",["Revision"]=revisionNombre,["NombreCompleto"]="No autorizado" }));
        Check(reject.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Redirect,"estudiante no edita nombres administrativamente");
        Check((await HostPrueba.Post(admin,"/Administrador/Gestionar/est2","/Administrador/EditarNombre",("UsuarioId","est2"),("Revision",revisionNombre),("NombreCompleto","Estudiante corregido"))).StatusCode==HttpStatusCode.Redirect,"admin corrige nombre de estudiante");
        await HostPrueba.Post(admin,"/Administrador/Gestionar/est2","/Administrador/EditarNombre",("UsuarioId","est2"),("Revision",revisionNombre),("NombreCompleto","Nombre obsoleto"));
        await using (var db = new OfflineDb(options)) { Check((await db.Users.FindAsync("est2"))!.FullName=="Estudiante corregido","revisión impide sobrescritura"); Check(await db.AuditoriaUsuarios.AnyAsync(a=>a.UsuarioAfectadoId=="est2"&&a.Accion=="NOMBRE_USUARIO_CORREGIDO"),"corrección auditada"); }
        Check((await Edit("nuevo-edicion@miumg.edu.gt","12345678")).StatusCode==HttpStatusCode.Redirect,"cambio de correo guardado");
        Check((await otra.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"sesión anterior invalidada");
        await using (var db = new OfflineDb(options)) { var user=(await db.Users.FindAsync("admin-edicion"))!; Check(user.NormalizedEmail=="NUEVO-EDICION@MIUMG.EDU.GT"&&user.NormalizedUserName==user.NormalizedEmail&&!user.EmailConfirmed,"Identity normalizado sin verificar correo falsamente"); Check(await db.PasswordResetTokens.Where(t=>t.UserId==user.Id).AllAsync(t=>t.UsedAtUtc!=null),"tokens anteriores revocados"); }
        Check((await HostPrueba.Login(admin,"nuevo-edicion@miumg.edu.gt",clave)).StatusCode==HttpStatusCode.Redirect && (await admin.GetAsync("/Administrador")).StatusCode==HttpStatusCode.OK,"nuevo correo permite acceso con mismo rol");
        async Task<HttpResponseMessage> Add(string? edad) { var fields=new List<(string,string)> { ("NombreCompleto","Cuenta menor ficticia"),("Correo","menor-edicion@miumg.edu.gt"),("Rol","Estudiante"),("CarneParte1","9911"),("CarneParte2","26"),("CarneParte3","99"),("ContrasenaTemporal",clave),("ConfirmarContrasenaTemporal",clave) }; if(edad!=null)fields.Add(("MayorEdadDeclarada",edad)); return await HostPrueba.Post(admin,"/Administrador/AgregarUsuario","/Administrador/AgregarUsuario",fields.ToArray()); }
        Check((await Add(null)).StatusCode==HttpStatusCode.OK,"clasificación de edad obligatoria");
        Check((await Add("false")).StatusCode==HttpStatusCode.Redirect,"permite alta administrativa de menor");
        await using (var db = new OfflineDb(options)) { var menor=await db.Users.SingleAsync(u=>u.Email=="menor-edicion@miumg.edu.gt"); Check(menor.MayorEdadDeclarada==false,"clasificación persistida"); var perfil=await db.PerfilesEstudiante.SingleAsync(p=>p.UsuarioId==menor.Id); Check(!await db.VerificacionesMayoriaEdad.AnyAsync(v=>v.PerfilEstudianteId==perfil.Id),"clasificación no crea una verificación documental"); }
        var retirados = await admin.GetStringAsync("/Administrador/Catedraticos");
        Check(Regex.IsMatch(retirados,"class=\"dash-nav-link is-active\"[^>]*href=\"/Administrador/Catedraticos\""),"rol retirado tiene selección activa");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        await HostPrueba.Login(estudiante,"est2@miumg.edu.gt",clave);
        Check((await estudiante.GetAsync("/Demostracion")).StatusCode==HttpStatusCode.NotFound,"demostración restringida a cuenta marcada");
        int citasAntes, registrosAntes, respuestasAntes, induccionesAntes; bool pruebaActiva;
        await using(var db=new OfflineDb(options)) {
            citasAntes=await db.Citas.CountAsync(); registrosAntes=await db.RegistrosAtencionCita.CountAsync(); respuestasAntes=await db.Set<Zuni.Models.Pruebas.RespuestaPrueba>().CountAsync(); induccionesAntes=await db.AsignacionesPrueba.CountAsync(a=>a.InduccionUtc!=null);
            pruebaActiva=(await db.PruebasDirector.FindAsync(Guid.Parse("eeeeeeee-0001-0001-0000-000000000000")))!.Activa;
            db.AuditoriaUsuarios.Add(new AuditoriaUsuario { UsuarioAfectadoId="est2",AdministradorId="admin-edicion",Accion=Zuni.Services.DemostracionAcceso.Accion,FechaUtc=DateTime.UtcNow }); await db.SaveChangesAsync();
            Check(!await Zuni.Services.DemostracionAcceso.Habilitada(db,"est2",false),"demostración bloqueada fuera de desarrollo");
        }
        var demoHtml=await estudiante.GetStringAsync("/Demostracion");
        Check(demoHtml.Contains("13 de octubre de 2026")&&demoHtml.Contains("14:00"),"cita ficticia con fecha y hora pedidas");
        var respuestas=Regex.Matches(demoHtml,"name=\"(Respuestas\\[[^\\]]+\\])\"").Select(m=>(m.Groups[1].Value,"5")).Distinct().ToList();
        Check(respuestas.Count==15,"nivel 1 con 15 preguntas");
        var sinConsentir=await HostPrueba.Post(estudiante,"/Demostracion","/Demostracion",respuestas.ToArray());
        Check(!(await sinConsentir.Content.ReadAsStringAsync()).Contains("Demostración terminada"),"participación voluntaria obligatoria en práctica");
        respuestas.Add(("Consentimiento","true"));
        var concluida=await HostPrueba.Post(estudiante,"/Demostracion","/Demostracion",respuestas.ToArray());
        Check(WebUtility.HtmlDecode(await concluida.Content.ReadAsStringAsync()).Contains("Demostración terminada"),"recorrido de prueba completo");
        await using(var db=new OfflineDb(options)) {
            Check(await db.Citas.CountAsync()==citasAntes&&await db.RegistrosAtencionCita.CountAsync()==registrosAntes,"demo no registra atenciones inventadas");
            Check(await db.Set<Zuni.Models.Pruebas.RespuestaPrueba>().CountAsync()==respuestasAntes&&await db.AsignacionesPrueba.CountAsync(a=>a.InduccionUtc!=null)==induccionesAntes,"demo no altera respuestas ni inducciones");
            Check((await db.PruebasDirector.FindAsync(Guid.Parse("eeeeeeee-0001-0001-0000-000000000000")))!.Activa==pruebaActiva,"demo no activa globalmente la prueba");
        }
        if(!string.IsNullOrEmpty(preview)) await File.WriteAllTextAsync(Path.Combine(preview,"demostracion-estudiante.html"),demoHtml);
        if(!string.IsNullOrEmpty(preview)) { await File.WriteAllTextAsync(Path.Combine(preview,"admin-tablero.html"),await admin.GetStringAsync("/Administrador")); await File.WriteAllTextAsync(Path.Combine(preview,"admin-retirados.html"),retirados); await File.WriteAllTextAsync(Path.Combine(preview,"admin-contacto.html"),await admin.GetStringAsync("/MiCuenta/Editar")); }
        Console.WriteLine($"OK: {checks} comprobaciones de edición administrativa.");
    }
}
