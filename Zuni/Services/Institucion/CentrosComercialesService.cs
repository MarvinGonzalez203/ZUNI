using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Comercial;
using Zuni.Services.Privacidad;
using Zuni.Services.Agenda;
namespace Zuni.Services.Institucion;
public static class CentrosComercialesService
{
    public static async Task<CentrosComercialesViewModel> Listar(ApplicationDbContext db,string? actor,int pagina,TimeProvider reloj,CancellationToken ct)
    {
        if (!await TarifasService.EsSuperAdmin(db,actor,ct)) throw new UnauthorizedAccessException();
        var hoy=AgendaReglas.Hoy(reloj.GetUtcNow().UtcDateTime);
        var q=db.CentrosComerciales.AsNoTracking(); var total=await q.CountAsync(ct);
        var paginas=Math.Max(1,(total+11)/12); pagina=Math.Clamp(pagina,1,paginas);
        var vigentes=await q.CountAsync(x=>x.Estado==EstadoContrato.Activo&&x.Inicio<=hoy&&x.Fin>=hoy,ct);
        var limite=hoy.AddDays(30);
        var proximos=await q.CountAsync(x=>x.Estado==EstadoContrato.Activo&&x.Inicio<=hoy&&x.Fin>=hoy&&x.Fin<=limite,ct);
        return new(await q.OrderBy(x=>x.Fin).ThenBy(x=>x.Id).Skip((pagina-1)*12).Take(12).ToListAsync(ct),pagina,paginas,total,vigentes,proximos);
    }
    public static async Task<Guid> Guardar(ApplicationDbContext db,System.Security.Claims.ClaimsPrincipal actor,EditarCentroInput input,TimeProvider reloj,CancellationToken ct)
    {
        PrivacidadService.Validar(input);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var id=await PrivacidadService.UsuarioAsync(db,actor,ct);
        if (!await TarifasService.EsSuperAdmin(db,id,ct)) throw new UnauthorizedAccessException();
        var c=input.Id is {} centro?await db.CentrosComerciales.SingleOrDefaultAsync(x=>x.Id==centro,ct)??throw new DatosOperacionException("El centro educativo ya no está disponible."):new CentroComercial();
        if ((input.Id is null && input.Revision!=Guid.Empty)||(input.Id is not null&&input.Revision!=c.Revision)) throw new DbUpdateConcurrencyException();
        var anterior=input.Id is null?null:JsonSerializer.Serialize(c);
        var estadoAnterior=c.Estado;
        if (input.Id is null) db.CentrosComerciales.Add(c);
        c.Nombre=input.Nombre.Trim();c.CorreoContacto=input.CorreoContacto?.Trim();c.MesesPlan=input.MesesPlan;c.Inicio=input.Inicio!.Value;c.Fin=input.Fin!.Value;
        c.Estado=input.Estado;c.ImporteGtq=input.ImporteGtq;c.NotaComercial=input.NotaComercial?.Trim();c.EsDemostracion=input.EsDemostracion;
        c.ActualizadoPorId=id;c.ActualizadoUtc=reloj.GetUtcNow().UtcDateTime;c.Revision=Guid.NewGuid();
        var accion=input.Id is null?"CENTRO_COMERCIAL_CREADO":c.Estado==EstadoContrato.Cancelado&&estadoAnterior!=c.Estado?"CENTRO_COMERCIAL_CANCELADO":"CENTRO_COMERCIAL_ACTUALIZADO";
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario{UsuarioAfectadoId=id,AdministradorId=id,Accion=accion,FechaUtc=c.ActualizadoUtc,DatosAnteriores=anterior,DatosNuevos=JsonSerializer.Serialize(c)});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return c.Id;
    }
}
