using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Services.Correo;
using Zuni.Services.Privacidad;

namespace Zuni.Services;

public sealed class PinProfesionalService(ApplicationDbContext db,IAutorizacionClinicaService autorizacion,
    IDataProtectionProvider protection,IPasswordHasher<ApplicationUser> hasher,TimeProvider reloj,ICorreoPin correo)
{
    private DateTime Ahora=>reloj.GetUtcNow().UtcDateTime;
    private async Task<ApplicationUser> Actor(ClaimsPrincipal sesion,CancellationToken ct)
    {
        var id=await autorizacion.ObtenerPsicologoAutorizadoAsync(sesion,ct)??throw new UnauthorizedAccessException();
        if(await db.UserRoles.AnyAsync(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName!="PSICOLOGO"),ct))throw new UnauthorizedAccessException();
        return await db.Users.SingleAsync(u=>u.Id==id,ct);
    }
    public async Task<bool> Configurado(ClaimsPrincipal sesion,CancellationToken ct)
    {
        var u=await Actor(sesion,ct);return await db.PinesProfesionales.AnyAsync(p=>p.UsuarioId==u.Id&&p.HashProtegido!=null,ct);
    }
    private IDataProtector Protector(string id)=>protection.CreateProtector("ZUNI.PIN.PROFESIONAL.v1",id);
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private static void Validar(string pin){if(!Regex.IsMatch(pin??"","^[0-9]{4}$"))throw new DatosOperacionException("Ingresa exactamente 4 dígitos para el PIN.");}
    private async Task<PinProfesional> Fila(string id,CancellationToken ct)
    {
        var p=await db.PinesProfesionales.SingleOrDefaultAsync(p=>p.UsuarioId==id,ct);
        if(p is null){p=new(){UsuarioId=id};db.PinesProfesionales.Add(p);}return p;
    }
    private void Bloqueo(PinProfesional p)
    {
        if(p.BloqueadoHastaUtc>Ahora)throw new DatosOperacionException("El PIN está bloqueado temporalmente. Espera 15 minutos o utiliza la recuperación por correo.");
        if(p.BloqueadoHastaUtc is not null){p.IntentosFallidos=0;p.BloqueadoHastaUtc=null;}
    }
    private void Fallo(PinProfesional p){p.IntentosFallidos++;if(p.IntentosFallidos>=5)p.BloqueadoHastaUtc=Ahora.AddMinutes(15);p.Revision=Guid.NewGuid();}
    private void GuardarPin(PinProfesional p,ApplicationUser u,string pin)
    {
        p.HashProtegido=Protector(u.Id).Protect(hasher.HashPassword(u,pin));p.IntentosFallidos=0;p.BloqueadoHastaUtc=null;
        p.RecuperacionHash=null;p.RecuperacionExpiraUtc=null;p.Revision=Guid.NewGuid();
    }
    public async Task Configurar(ClaimsPrincipal sesion,ConfigurarPinInput input,CancellationToken ct)
    {
        PrivacidadService.Validar(input);Validar(input.Pin);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var u=await Actor(sesion,ct);var p=await Fila(u.Id,ct);Bloqueo(p);
        if(p.HashProtegido is not null)throw new DatosOperacionException("Ya tienes un PIN configurado. Para cambiarlo utiliza la recuperación por correo.");
        if(u.PasswordHash is null||hasher.VerifyHashedPassword(u,u.PasswordHash,input.Contrasena)==PasswordVerificationResult.Failed){
            Fallo(p);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);throw new DatosOperacionException("No se pudo verificar la contraseña actual.");
        }
        GuardarPin(p,u,input.Pin);PrivacidadService.Auditar(db,u.Id,u.Id,"PIN_PROFESIONAL_CONFIGURADO",reloj);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task Verificar(ClaimsPrincipal sesion,string pin,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var u=await Actor(sesion,ct);var p=await Fila(u.Id,ct);Bloqueo(p);
        if(p.HashProtegido is null)throw new DatosOperacionException("Configura primero tu PIN profesional desde Mi cuenta.");
        var valido=Regex.IsMatch(pin??"","^[0-9]{4}$")&&hasher.VerifyHashedPassword(u,Protector(u.Id).Unprotect(p.HashProtegido),pin!)!=PasswordVerificationResult.Failed;
        if(!valido){Fallo(p);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);throw new DatosOperacionException("PIN incorrecto. Después de 5 intentos fallidos se bloquea durante 15 minutos.");}
        p.IntentosFallidos=0;p.BloqueadoHastaUtc=null;p.Revision=Guid.NewGuid();await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task SolicitarRecuperacion(ClaimsPrincipal sesion,CancellationToken ct)
    {
        var u=await Actor(sesion,ct);
        if(!correo.Disponible)throw new DatosOperacionException("La recuperación por correo está pendiente de configurar el remitente verificado y la clave privada de Brevo. Contacta al responsable del sistema.");
        if(!System.Net.Mail.MailAddress.TryCreate(u.Email,out _))throw new DatosOperacionException("Tu cuenta necesita un correo válido para recuperar el PIN. Contacta al Administrador.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var p=await Fila(u.Id,ct);
        if(p.UltimoEnvioUtc>Ahora.AddMinutes(-2))throw new DatosOperacionException("Ya solicitaste un enlace. Espera dos minutos antes de solicitar otro.");
        var token=WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        p.RecuperacionHash=Hash(token);p.RecuperacionExpiraUtc=Ahora.AddMinutes(20);p.UltimoEnvioUtc=Ahora;p.Revision=Guid.NewGuid();
        p.RecuperacionSello=Hash(u.SecurityStamp+"|"+u.Email);
        // Fragment stays out of access logs and Referer headers. Only the POST receives the token.
        var enlace=correo.UrlPublica.TrimEnd('/')+"/PinProfesional/Recuperar#usuario="+Uri.EscapeDataString(u.Id)+"&token="+Uri.EscapeDataString(token);
        try{await correo.Enviar(u.Email!,enlace,ct);}catch(Exception ex)when(ex is InvalidOperationException or HttpRequestException or TaskCanceledException){throw new DatosOperacionException("No se pudo enviar el correo de recuperación. Inténtalo más tarde.");}
        PrivacidadService.Auditar(db,u.Id,u.Id,"PIN_PROFESIONAL_RECUPERACION_SOLICITADA",reloj);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task Recuperar(RecuperarPinInput input,CancellationToken ct)
    {
        PrivacidadService.Validar(input);Validar(input.Pin);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var p=await db.PinesProfesionales.SingleOrDefaultAsync(p=>p.UsuarioId==input.Usuario,ct);
        var u=await db.Users.SingleOrDefaultAsync(u=>u.Id==input.Usuario&&u.IsActive&&!u.DebeCambiarContrasena,ct);
        var rol=u is not null&&await db.UserRoles.AnyAsync(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="PSICOLOGO"),ct)&&!await db.UserRoles.AnyAsync(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName!="PSICOLOGO"),ct);
        if(p?.RecuperacionHash is null||p.RecuperacionExpiraUtc is null||p.RecuperacionExpiraUtc<=Ahora||u is null||!rol||p.RecuperacionSello!=Hash(u.SecurityStamp+"|"+u.Email)||!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(p.RecuperacionHash),Encoding.ASCII.GetBytes(Hash(input.Token))))
            throw new DatosOperacionException("El enlace de recuperación no es válido o ya venció. Solicita uno nuevo.");
        GuardarPin(p,u,input.Pin);p.RecuperacionSello=null;
        PrivacidadService.Auditar(db,u.Id,u.Id,"PIN_PROFESIONAL_RECUPERADO",reloj);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
