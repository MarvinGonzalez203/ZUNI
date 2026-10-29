using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Comercial;
using Zuni.Models.Privacidad;
using Zuni.Resources;

internal static class ActualizacionV1Checks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave,Reloj reloj)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception("Actualización 1.0.0: "+text);checks++;}
        using var super=HostPrueba.Cliente(app);using var admin=HostPrueba.Cliente(app);using var anon=HostPrueba.Cliente(app);
        await HostPrueba.Login(super,"proveedor-ficticio@example.com",clave);await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);
        Check((await anon.GetAsync("/SuperAdmin/Centros")).StatusCode==HttpStatusCode.Redirect,"contratos requieren sesión");
        foreach(var url in new[]{"/SuperAdmin/Centros","/SuperAdmin/EditarCentro","/SuperAdmin/Guias"})Check((await admin.GetAsync(url)).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"Administrador no obtiene control comercial: "+url);
        await using(var db=new OfflineDb(options)){var actor=await db.Users.Where(u=>u.Email=="proveedor-ficticio@example.com").Select(u=>u.Id).SingleAsync();db.LecturasVersion.Add(new(){UsuarioId=actor,Version="1.0.0",FechaUtc=reloj.Ahora.UtcDateTime});await db.SaveChangesAsync();}
        var home=await super.GetStringAsync("/SuperAdmin");
        Check(home.Contains("release-dot"),"una versión nueva aparece pendiente aunque la anterior esté leída");
        Check(home.Contains("commercial-grid")&&home.Contains("/SuperAdmin/Centros")&&home.Contains("Colegio Huellitas"),"tablero comercial y ejemplo visible");
        Check(home.Contains("system-version\">ZUNI · v"+VersionSistema.Actual)&&home.Contains("release-control"),"versión y campana disponibles");
        var versiones=Regex.Matches(home,"data-release-version=\"([^\"]+)\"").Select(m=>m.Groups[1].Value).ToArray();
        Check(versiones.SequenceEqual(new[]{VersionSistema.Actual,"1.1.0","1.0.0"}),"campana muestra las tres versiones reales en orden");
        Check(VersionSistema.Historial.Count==3&&VersionSistema.Historial[0].Numero==VersionSistema.Actual,"metadatos del historial y versión actual coinciden");
        await using(var db=new OfflineDb(options))Check(!await db.CentrosComerciales.AnyAsync(),"GET no registra el ejemplo automáticamente");
        var example=await super.GetStringAsync("/SuperAdmin/EditarCentro?ejemplo=true");
        Check(example.Contains("2026-10-28")&&example.Contains("2027-10-27"),"ejemplo con fechas acordadas");
        var umg=await super.GetStringAsync("/SuperAdmin/EditarCentro?nombreInicial=UMG&inicioInicial=2026-10-01&mesesInicial=12");
        Check(umg.Contains("value=\"UMG\"")&&umg.Contains("2027-09-30"),"formulario UMG preparado con vigencia anual, sin guardar");
        Check((await super.GetAsync("/SuperAdmin/EditarCentro?inicioInicial=9999-12-31")).StatusCode==HttpStatusCode.BadRequest,"prefill rechaza fechas no válidas antes de sumar meses");
        Check((await super.PostAsync("/SuperAdmin/EditarCentro",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"guardar exige antiforgery");
        Task<HttpResponseMessage> Save(Guid? id,Guid revision,string inicio="2026-10-28",string fin="2027-10-27",string estado="Activo",string importe="2030",string nombre="Colegio Huellitas")=>HostPrueba.Post(super,"/SuperAdmin/EditarCentro?ejemplo=true","/SuperAdmin/EditarCentro",("Id",id?.ToString()??""),("Revision",revision.ToString()),("Nombre",nombre),("MesesPlan","12"),("Inicio",inicio),("Fin",fin),("Estado",estado),("ImporteGtq",importe),("EsDemostracion","true"));
        Check((await Save(null,Guid.Empty,fin:"2026-10-01")).StatusCode==HttpStatusCode.OK,"fecha inversa se rechaza");
        await using(var db=new OfflineDb(options))Check(!await db.CentrosComerciales.AnyAsync(),"rechazo no guarda datos");
        Check((await Save(null,Guid.Empty)).StatusCode==HttpStatusCode.Redirect,"creación comercial válida");
        CentroComercial saved;await using(var db=new OfflineDb(options))saved=await db.CentrosComerciales.AsNoTracking().SingleAsync();
        Check(saved.EsDemostracion&&saved.MesesPlan==12&&saved.ImporteGtq==2030,"datos comerciales persistidos");
        Check((await Save(saved.Id,saved.Revision,fin:"2028-10-27",estado:"Cancelado")).StatusCode==HttpStatusCode.Redirect,"ampliación y cancelación guardadas");
        Check((await Save(saved.Id,saved.Revision,fin:"2029-10-27")).StatusCode==HttpStatusCode.Conflict,"revisión vieja no sobrescribe");
        Check((await Save(null,Guid.Empty,importe:"100.123")).StatusCode==HttpStatusCode.OK,"se rechazan importes con más de dos decimales");
        Check((await Save(null,Guid.Empty,nombre:"Centro <script>alert(1)</script>")).StatusCode==HttpStatusCode.Redirect,"nombre tratado como dato");
        var listing=await super.GetStringAsync("/SuperAdmin/Centros");Check(!listing.Contains("<script>alert(1)</script>")&&listing.Contains("&lt;script&gt;"),"HTML de centros está codificado");
        await using(var db=new OfflineDb(options)){
            var current=await db.CentrosComerciales.AsNoTracking().SingleAsync(x=>x.Id==saved.Id);
            Check(current.Estado==EstadoContrato.Cancelado&&current.Fin==new DateOnly(2028,10,27),"cancelación conserva el registro y la nueva fecha");
            Check(await db.AuditoriaUsuarios.CountAsync(a=>a.Accion.StartsWith("CENTRO_COMERCIAL_"))==3,"cambios comerciales auditados");
        }
        var privacy=await super.GetStringAsync("/Privacidad");
        Check(privacy.Contains("transparencia-titulo")&&!privacy.Contains("name=\"Nueva.Detalle\"")&&!privacy.Contains("/Privacidad/Solicitar"),"proveedor ve transparencia sin petición institucional");
        Check((await HostPrueba.Post(super,"/MiCuenta","/Privacidad/Solicitar",("Nueva.Tipo","0"),("Nueva.Detalle","Dato ficticio"))).StatusCode==HttpStatusCode.Forbidden,"proveedor no crea solicitudes institucionales por POST");
        var update=await HostPrueba.Post(super,"/MiCuenta","/Actualizaciones/Leida",("UsuarioId","admin"));Check(update.StatusCode==HttpStatusCode.NoContent,"lectura de actualización guardada");
        Check((await HostPrueba.Post(super,"/MiCuenta","/Actualizaciones/Leida")).StatusCode==HttpStatusCode.NoContent,"lectura idempotente");
        Check((await super.PostAsync("/Actualizaciones/Leida",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"lectura exige antiforgery");
        await using(var db=new OfflineDb(options)){
            var id=await db.Users.Where(u=>u.Email=="proveedor-ficticio@example.com").Select(u=>u.Id).SingleAsync();
            Check(await db.LecturasVersion.CountAsync(x=>x.UsuarioId==id&&x.Version==VersionSistema.Actual)==1&&!await db.LecturasVersion.AnyAsync(x=>x.UsuarioId=="admin"),"lectura pertenece al usuario de sesión, no al ID enviado");
            db.SolicitudesDerechosDatos.Add(new SolicitudDerechoDatos{UsuarioId="admin",Tipo=TipoDerechoDatos.Correccion,Detalle="Solicitud ficticia para comprobar el aviso.",FechaSolicitudUtc=reloj.Ahora.UtcDateTime});await db.SaveChangesAsync();
        }
        var leido=await super.GetStringAsync("/MiCuenta");
        Check(!leido.Contains("class=\"release-dot\""),"indicador leído desaparece");
        Check(Regex.Matches(leido,"data-release-version=").Count==3,"marcar como leída no borra el historial");
        Check((await admin.GetStringAsync("/MiCuenta")).Contains("/Privacidad/Solicitudes"),"Administrador recibe enlace a peticiones pendientes");
        foreach(var tema in new[]{"seguridad","apariencia","avisos"})Check((await anon.GetAsync("/Home/Guia?tema="+tema)).StatusCode==HttpStatusCode.OK,"guía pública extra: "+tema);
        var search=await super.GetStringAsync("/SuperAdmin/Guias?q=renovar&rol=SuperAdmin");Check(search.Contains("Ampliar o renovar un contrato")&&!search.Contains("<h2>Responder un cuestionario indicado</h2>"),"búsqueda por tema y rol");
        var login=await anon.GetStringAsync("/Cuenta/IniciarSesion");Check(!login.Contains("auth-mobile-logo")&&login.Contains("auth-brand d-flex"),"entrada móvil conserva título y elimina logo repetido");
        Check(login.Contains("favicon-96.png")&&login.Contains("page-scroll-top"),"favicon y desplazamiento compartidos");
        Check(VersionSistema.Actual==typeof(VersionSistema).Assembly.GetName().Version!.ToString(3),"versión visual y ensamblado coinciden");
        // Each guide endpoint computes its audience from the authenticated role, never the query string.
        foreach(var role in new[]{"Administrador","Director","Psicologo","Estudiante"}){
            var uid="guias-aisladas-"+role;var mail=uid+"@example.com";
            await using(var db=new OfflineDb(options)){
                var u=new Zuni.Models.ApplicationUser{Id=uid,FullName="Persona ficticia de guías",Email=mail,NormalizedEmail=mail.ToUpperInvariant(),UserName=mail,NormalizedUserName=mail.ToUpperInvariant(),IsActive=true,SecurityStamp=Guid.NewGuid().ToString()};
                u.PasswordHash=new Microsoft.AspNetCore.Identity.PasswordHasher<Zuni.Models.ApplicationUser>().HashPassword(u,clave);db.Users.Add(u);
                db.UserRoles.Add(new(){UserId=uid,RoleId=await db.Roles.Where(r=>r.NormalizedName==role.ToUpperInvariant()).Select(r=>r.Id).SingleAsync()});await db.SaveChangesAsync();
            }
            using var audience=HostPrueba.Cliente(app);await HostPrueba.Login(audience,mail,clave);
            var guideHtml=await audience.GetStringAsync("/Guias?rol=SuperAdmin");
            Check(guideHtml.Contains("data-guide-role=\""+role+"\""),"guías propias: "+role);
            Check(!guideHtml.Contains("data-guide-role=\"SuperAdmin\""),"parámetro no amplía audiencia: "+role);
            Check(!guideHtml.Contains("name=\"rol\""),"sin selector de otros roles: "+role);
            Check(guideHtml.Contains("sidebar-collapse")&&guideHtml.Contains("sidebar-scroll-cue")&&guideHtml.Contains("sidebar-guides"),"navegación y ayuda comunes: "+role);
            var fixture=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(fixture))await File.WriteAllTextAsync(Path.Combine(fixture,"guias-"+role+".html"),guideHtml);
        }
        var filtered=await super.GetStringAsync("/SuperAdmin/Guias?rol=Estudiante");
        Check(filtered.Contains("data-guide-role=\"Estudiante\"")&&!filtered.Contains("data-guide-role=\"SuperAdmin\""),"filtro de SuperAdmin reduce contenido en servidor");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(preview)){
            await File.WriteAllTextAsync(Path.Combine(preview,"v1-super-unread-es.html"),home);
            async Task SavePage(HttpClient c,string url,string name)=>await File.WriteAllTextAsync(Path.Combine(preview,name+".html"),await c.GetStringAsync(url));
            foreach(var lang in new[]{"es","en"}){
                await HostPrueba.Post(anon,"/Cuenta/IniciarSesion","/Idioma/Cambiar",("idioma",lang),("volver","/"));
                await HostPrueba.Post(super,"/MiCuenta","/Idioma/Cambiar",("idioma",lang),("volver","/SuperAdmin"));
                await SavePage(anon,"/Cuenta/IniciarSesion","v1-login-"+lang);await SavePage(anon,"/Home/Guia?tema=privacidad","v1-guia-"+lang);
                await SavePage(anon,"/Home/Guia?tema=cita","v1-cita-"+lang);await SavePage(anon,"/","v1-home-"+lang);
                await SavePage(super,"/SuperAdmin","v1-super-"+lang);await SavePage(super,"/SuperAdmin/Guias","v1-guias-"+lang);
                foreach(var filtro in new[]{"General","SuperAdmin","Administrador","Director","Psicologo","Estudiante"})await SavePage(super,"/SuperAdmin/Guias?rol="+filtro,"guias-filtro-"+filtro+"-"+lang);
                await SavePage(super,"/SuperAdmin/EditarCentro?ejemplo=true","v1-contrato-"+lang);await SavePage(super,"/Privacidad","v1-proteccion-"+lang);
            }
        }
        await HostPrueba.Post(super,"/MiCuenta","/Idioma/Cambiar",("idioma","en"),("volver","/SuperAdmin/Guias"));
        Check((await super.GetStringAsync("/SuperAdmin/Guias?q=renew&rol=SuperAdmin")).Contains("Extend or renew a contract"),"búsqueda también reconoce traducciones");
        Console.WriteLine($"OK: {checks} comprobaciones de experiencia móvil, centros comerciales, privacidad y avisos de versión.");
    }
}
