using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Atencion;
using Zuni.Models.Privacidad;
using Zuni.Models.BigFive;
using Zuni.Models.Informes;
using Zuni.Seguridad;

namespace Zuni.Data;

public partial class ApplicationDbContext
{
    public ProteccionPersistencia Proteccion { get; }
    private readonly IHttpContextAccessor? Peticion;
    private readonly TimeProvider RelojSeguridad;
    public DbSet<RegistroSeguridad> RegistrosSeguridad => Set<RegistroSeguridad>();

    public static string ContenidoFirma(RegistroSeguridad r) => JsonSerializer.Serialize(new[] {
        r.EventoId.ToString("D"),r.Tipo,r.Referencia,JsonSeguridad.UtcMicrosegundos(r.FechaUtc).ToString("O"),r.ContenidoCifrado });
    public void RegistrarSeguridad(string tipo,string referencia,object registro,bool historico=false)
    {
        var http=Peticion?.HttpContext;
        var contenido=JsonSeguridad.Serializar(new {
            Registro=registro,
            Captura=new {Origen=historico?"HISTORICO_MIGRADO":http is null?"OPERADOR_SIN_PETICION":"HTTP",UsuarioSesion=http?.User.FindFirstValue(ClaimTypes.NameIdentifier),SesionAutenticada=http?.User.Identity?.IsAuthenticated??false,
                Ip=http?.Connection.RemoteIpAddress?.ToString(),Agente=http?.Request.Headers.UserAgent.ToString()[..Math.Min(http.Request.Headers.UserAgent.ToString().Length,512)],
                Idioma=System.Globalization.CultureInfo.CurrentUICulture.Name,
                FechaCapturaUtc=RelojSeguridad.GetUtcNow().UtcDateTime}
        });
        var r=new RegistroSeguridad{Tipo=tipo,Referencia=referencia,FechaUtc=JsonSeguridad.UtcMicrosegundos(RelojSeguridad.GetUtcNow().UtcDateTime),
            ContenidoCifrado=Proteccion.Cifrar(contenido,"RegistroSeguridad.Contenido")};
        r.Firma=Proteccion.Firmar(ContenidoFirma(r)); RegistrosSeguridad.Add(r);
    }
    public void RegistrarConsentimiento(string tipo,string referencia,string usuario,bool? decision,string version,string texto) =>
        RegistrarSeguridad("CONSENTIMIENTO_"+tipo,referencia,new {UsuarioId=usuario,Decision=decision,Version=version,Texto=texto,AvisoRegistro=Zuni.Resources.EvidenciaConsentimiento.Aviso});

    private async Task<string> UsuarioPerfil(Guid perfil,CancellationToken ct)=>
        PerfilesEstudiante.Local.FirstOrDefault(p=>p.Id==perfil)?.UsuarioId
        ??await PerfilesEstudiante.Where(p=>p.Id==perfil).Select(p=>p.UsuarioId).SingleAsync(ct);

    private async Task PrepararSeguridad(CancellationToken ct)
    {
        ChangeTracker.DetectChanges();
        var entries=ChangeTracker.Entries().ToArray();
        if(entries.Any(e=>(e.Entity is AuditoriaUsuario or EventoAccesoClinico or RegistroSeguridad or ConsentimientoAtencion or AceptacionPrivacidad or ExportacionSeguridad or EstadoProteccion)
            && e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("La auditoría y las aceptaciones son inmutables; registra un evento nuevo.");
        foreach(var e in entries.Where(e=>e.State==EntityState.Added))
        {
            var (tipo,id)=e.Entity switch {
                AuditoriaUsuario a => ("AUDITORIA_USUARIO",a.Id.ToString()),
                EventoAccesoClinico a => ("ACCESO_CLINICO",a.Id.ToString()),
                _ => ("","")};
            if(tipo!="" && !RegistrosSeguridad.Local.Any(r=>r.Tipo==tipo&&r.Referencia==id)) RegistrarSeguridad(tipo,id,e.Entity);
            switch(e.Entity)
            {
                case ConsentimientoAtencion a when !RegistrosSeguridad.Local.Any(r=>r.Tipo=="CONSENTIMIENTO_ATENCION"&&r.Referencia==a.Id.ToString()):
                    var solicitud=SolicitudesAtencion.Local.FirstOrDefault(s=>s.Id==a.SolicitudAtencionId)??await SolicitudesAtencion.AsNoTracking().SingleAsync(s=>s.Id==a.SolicitudAtencionId,ct);
                    var usuario=await UsuarioPerfil(solicitud.PerfilEstudianteId,ct);
                    RegistrarConsentimiento("ATENCION",a.Id.ToString(),usuario,a.Aceptado,a.VersionConsentimiento,
                        a.VersionConsentimiento==Zuni.Resources.ConsentimientoInformado.Version?Zuni.Resources.ConsentimientoInformado.Texto:"Texto histórico no disponible");
                    break;
                case AceptacionPrivacidad a when !RegistrosSeguridad.Local.Any(r=>r.Tipo=="CONSENTIMIENTO_PRIVACIDAD"&&r.Referencia==a.PoliticaId+":"+a.UsuarioId):
                    var politica=PoliticasPrivacidad.Local.FirstOrDefault(p=>p.Id==a.PoliticaId)??await PoliticasPrivacidad.AsNoTracking().SingleAsync(p=>p.Id==a.PoliticaId,ct);
                    RegistrarConsentimiento("PRIVACIDAD",a.PoliticaId+":"+a.UsuarioId,a.UsuarioId,true,politica.Version.ToString(),JsonSerializer.Serialize(politica));break;
                case ParticipacionBigFive a when !RegistrosSeguridad.Local.Any(r=>r.Tipo=="CONSENTIMIENTO_BIGFIVE"&&r.Referencia==a.Id.ToString()):
                    RegistrarConsentimiento("BIGFIVE",a.Id.ToString(),await UsuarioPerfil(a.PerfilEstudianteId,ct),true,a.VersionConsentimiento,a.TextoConsentimiento);break;
            }
        }
        foreach(var e in entries.Where(e=>e.State==EntityState.Modified&&e.Entity is AutorizacionInforme))
        {
            var a=(AutorizacionInforme)e.Entity;
            if(e.Property(nameof(a.Aceptada)).IsModified || e.Property(nameof(a.FechaRevocacionUtc)).IsModified)
            {
                var tipo=a.FechaRevocacionUtc.HasValue?"INFORME_REVOCADO":"INFORME";
                if(!RegistrosSeguridad.Local.Any(r=>r.Tipo=="CONSENTIMIENTO_"+tipo&&r.Referencia==a.VersionInformeId.ToString()))
                    RegistrarConsentimiento(tipo,a.VersionInformeId.ToString(),a.EstudianteUsuarioId,a.FechaRevocacionUtc.HasValue?false:a.Aceptada,a.VersionConsentimiento,a.TextoConsentimiento);
            }
        }
        foreach(var e in entries.Where(e=>e.State==EntityState.Modified&&e.Entity is ParticipacionBigFive))
        {
            var a=(ParticipacionBigFive)e.Entity;
            if(e.Property(nameof(a.FechaRetiroConsentimientoUtc)).IsModified&&a.FechaRetiroConsentimientoUtc.HasValue
                &&!RegistrosSeguridad.Local.Any(r=>r.Tipo=="CONSENTIMIENTO_BIGFIVE_RETIRO"&&r.Referencia==a.Id.ToString()))
                RegistrarConsentimiento("BIGFIVE_RETIRO",a.Id.ToString(),await UsuarioPerfil(a.PerfilEstudianteId,ct),false,a.VersionConsentimiento,a.TextoConsentimiento);
        }
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    { PrepararSeguridad(CancellationToken.None).GetAwaiter().GetResult();return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken cancellationToken=default)
    { await PrepararSeguridad(cancellationToken);return await base.SaveChangesAsync(acceptAllChangesOnSuccess,cancellationToken); }
}
