using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Seguridad;

// La copia externa no contiene notas ni IP en claro. Los acuses se guardan aparte
// del evento inmutable: no se usa un cursor numérico que perdería commits fuera de orden.
public sealed class BitacoraExterna(RutasSeguridad rutas,ProteccionPersistencia proteccion)
{
    public sealed record Linea(string Anterior,RegistroSeguridad Registro,string Sello);
    private readonly SemaphoreSlim mutex=new(1);
    private long longitudVerificada=-1;
    private string ultimoSello="INICIO";
    private static string ContenidoLinea(string anterior,RegistroSeguridad r)=>JsonSerializer.Serialize(new{Anterior=anterior,Registro=r});
    public async Task<int> Exportar(ApplicationDbContext db,CancellationToken ct=default)
    {
        await mutex.WaitAsync(ct);
        try
        {
            // Un exportador por BD. Para múltiples réplicas, todas deben usar el mismo archivo externo.
            if(db.Database.IsNpgsql())
            {
                await db.Database.OpenConnectionAsync(ct);
                await using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.CommandText="SELECT pg_try_advisory_lock(19191927)";
                if(!Equals(await cmd.ExecuteScalarAsync(ct),true)){await db.Database.CloseConnectionAsync();return 0;}
            }
            try
            {
                var pendientes=await db.RegistrosSeguridad.AsNoTracking().Where(r=>!db.Set<ExportacionSeguridad>().Any(a=>a.EventoId==r.EventoId)).OrderBy(r=>r.Id).Take(200).ToListAsync(ct);
                if(pendientes.Count==0)return 0;
                ProteccionPersistencia.PrepararDirectorio(Path.GetDirectoryName(rutas.Bitacora)!);
                using var archivo=new FileStream(rutas.Bitacora,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.Read);
                ProteccionPersistencia.RestringirArchivo(rutas.Bitacora);
                var anterior=archivo.Length==longitudVerificada?ultimoSello:LeerYVerificar(archivo,null);
                archivo.Seek(0,SeekOrigin.End);
                foreach(var r in pendientes)
                {
                    if(!proteccion.Verificar(ApplicationDbContext.ContenidoFirma(r),r.Firma))throw new CryptographicException("Firma de auditoría inválida.");
                    var sello=proteccion.Firmar(ContenidoLinea(anterior,r));
                    var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Linea(anterior,r,sello))+"\n");
                    await archivo.WriteAsync(bytes,ct);anterior=sello;
                    db.Set<ExportacionSeguridad>().Add(new(){EventoId=r.EventoId,FechaUtc=DateTime.UtcNow});
                }
                archivo.Flush(true); // Antes del acuse; tras una caída puede haber duplicados idénticos, nunca omisiones por cursor.
                longitudVerificada=archivo.Length;ultimoSello=anterior;
                await db.SaveChangesAsync(ct);
                return pendientes.Count;
            }
            finally
            {
                if(db.Database.IsNpgsql()){
                    await using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.CommandText="SELECT pg_advisory_unlock(19191927)";
                    await cmd.ExecuteScalarAsync(CancellationToken.None);await db.Database.CloseConnectionAsync();
                }
            }
        }
        finally{mutex.Release();}
    }
    private string LeerYVerificar(Stream archivo,Action<RegistroSeguridad>? visitar)
    {
        string anterior="INICIO";
        using var lector=new StreamReader(archivo,Encoding.UTF8,false,4096,true);
        while(lector.ReadLine() is string texto)
        {
            var l=JsonSerializer.Deserialize<Linea>(texto)??throw new CryptographicException("Bitácora externa ilegible.");
            if(l.Anterior!=anterior||!proteccion.Verificar(ContenidoLinea(anterior,l.Registro),l.Sello)||!proteccion.Verificar(ApplicationDbContext.ContenidoFirma(l.Registro),l.Registro.Firma))
                throw new CryptographicException("La bitácora externa no conserva su integridad.");
            anterior=l.Sello;visitar?.Invoke(l.Registro);
        }
        return anterior;
    }
    public async Task VerificarContraBase(ApplicationDbContext db,CancellationToken ct=default)
    {
        var vistos=new Dictionary<Guid,string>();
        if(File.Exists(rutas.Bitacora)){
            using var archivo=File.OpenRead(rutas.Bitacora);
            LeerYVerificar(archivo,r=>{if(vistos.TryGetValue(r.EventoId,out var firma)&&firma!=r.Firma)throw new CryptographicException("Evento duplicado diferente.");vistos[r.EventoId]=r.Firma;});
        }
        long ultimoId=0;
        while(true){
          var lote=await db.RegistrosSeguridad.AsNoTracking().Where(r=>r.Id>ultimoId).OrderBy(r=>r.Id).Take(200).ToListAsync(ct);
          if(lote.Count==0)break;
          foreach(var r in lote){
            if(!proteccion.Verificar(ApplicationDbContext.ContenidoFirma(r),r.Firma)||!vistos.Remove(r.EventoId,out var firma)||firma!=r.Firma)
                throw new CryptographicException("Falta una copia externa o una firma no coincide. Exporta los pendientes y revisa la integridad.");
            if(r.Tipo is "AUDITORIA_USUARIO" or "ACCESO_CLINICO"){
                object? fuente=r.Tipo=="AUDITORIA_USUARIO"?await db.AuditoriaUsuarios.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==Guid.Parse(r.Referencia),ct):await db.EventosAccesoClinico.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==Guid.Parse(r.Referencia),ct);
                using var contenido=JsonDocument.Parse(proteccion.Descifrar(r.ContenidoCifrado,"RegistroSeguridad.Contenido"));
                if(fuente is null||JsonSeguridad.Serializar(fuente)!=contenido.RootElement.GetProperty("Registro").GetRawText())throw new CryptographicException("El registro original fue alterado o eliminado.");
            }
          }
          ultimoId=lote[^1].Id;
        }
        if(vistos.Count!=0)throw new CryptographicException("La copia externa contiene eventos eliminados de la base de datos.");
        if(await db.AuditoriaUsuarios.AnyAsync(a=>!db.RegistrosSeguridad.Any(r=>r.Tipo=="AUDITORIA_USUARIO"&&r.Referencia==a.Id.ToString().ToLower()),ct)
            ||await db.EventosAccesoClinico.AnyAsync(a=>!db.RegistrosSeguridad.Any(r=>r.Tipo=="ACCESO_CLINICO"&&r.Referencia==a.Id.ToString().ToLower()),ct))
            throw new CryptographicException("Existen eventos originales sin evidencia firmada.");
    }
}

public sealed class ExportadorSeguridad(IServiceScopeFactory scopes,BitacoraExterna bitacora,ILogger<ExportadorSeguridad> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            var pausa=TimeSpan.FromSeconds(5);
            try { using var scope=scopes.CreateScope();if(await bitacora.Exportar(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),stoppingToken)>=200)pausa=TimeSpan.FromMilliseconds(100); }
            catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch { logger.LogCritical("No se pudo confirmar la copia externa de auditoría. Los eventos pendientes se conservan en la base; requiere revisión del operador."); }
            try{await Task.Delay(pausa,stoppingToken);}catch(OperationCanceledException){break;}
        }
    }
}
