using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Seguridad;

internal static class ProteccionClinicaChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> opciones)
    {
        int checks=0;void Check(bool ok,string texto){if(!ok)throw new Exception("Protección clínica: "+texto);checks++;}
        async Task Rechaza(Func<Task> f,string texto){try{await f();}catch(Exception e)when(e is CryptographicException or InvalidOperationException){checks++;return;}throw new Exception("No rechazó: "+texto);}
        var cripto=SeguridadPrueba.Proteccion;
        var secreto="Nota clínica ficticia: áéíóú 🔐";var a=cripto.Cifrar(secreto,"campo");var b=cripto.Cifrar(secreto,"campo");
        Check(a!=b,"nonce aleatorio por escritura");Check(cripto.Descifrar(a,"campo")==secreto,"Unicode íntegro");
        await Rechaza(()=>Task.FromResult(cripto.Descifrar(a,"otro campo")),"intercambio entre campos");
        await Rechaza(()=>Task.FromResult(ProteccionPersistencia.Efimera().Descifrar(a,"campo")),"clave distinta");
        await Rechaza(()=>Task.FromResult(cripto.Descifrar(secreto,"campo")),"texto plano rechazado");
        var partes=a.Split(':');var bytes=Convert.FromBase64String(partes[4]);bytes[0]^=1;partes[4]=Convert.ToBase64String(bytes);
        await Rechaza(()=>Task.FromResult(cripto.Descifrar(string.Join(':',partes),"campo")),"cifrado alterado");
        using(var fuente=new OfflineDb(opciones)){
            var eventos=await fuente.RegistrosSeguridad.Where(r=>r.Tipo=="CONSENTIMIENTO_ATENCION").ToListAsync();
            var http=eventos.Select(r=>JsonDocument.Parse(cripto.Descifrar(r.ContenidoCifrado,"RegistroSeguridad.Contenido")))
                .FirstOrDefault(j=>j.RootElement.GetProperty("Captura").GetProperty("Origen").GetString()=="HTTP");
            Check(http is not null,"consentimiento capturado en petición real");
            Check(http!.RootElement.GetProperty("Captura").GetProperty("Ip").GetString()=="127.0.0.1","IP del transporte, no de formulario");
            Check(http.RootElement.GetProperty("Registro").GetProperty("Texto").GetString()==Zuni.Resources.ConsentimientoInformado.Texto,"texto exacto de aceptación archivado");
        }
        await using var conexion=new SqliteConnection("Data Source=:memory:");await conexion.OpenAsync();
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(conexion).Options;
        await using var db=new OfflineDb(options);await db.Database.EnsureCreatedAsync();
        var audit=new AuditoriaUsuario{UsuarioAfectadoId="ficticio",AdministradorId="operador",Accion="PRUEBA",DatosNuevos="{\"nota\":\"confidencial\"}",Motivo=secreto};
        db.AuditoriaUsuarios.Add(audit);await db.SaveChangesAsync();
        using(var cmd=conexion.CreateCommand()){cmd.CommandText="SELECT Motivo FROM AuditoriaUsuarios";var raw=(string)(await cmd.ExecuteScalarAsync())!;Check(raw.StartsWith("zenc1:")&&!raw.Contains("clínica"),"columna almacenada cifrada");}
        db.ChangeTracker.Clear();Check((await db.AuditoriaUsuarios.SingleAsync()).Motivo==secreto,"lectura autorizada conserva contenido");
        var r=await db.RegistrosSeguridad.SingleAsync();Check(cripto.Verificar(ApplicationDbContext.ContenidoFirma(r),r.Firma),"auditoría firmada transaccionalmente");
        audit=await db.AuditoriaUsuarios.SingleAsync();audit.Accion="CAMBIO";await Rechaza(()=>db.SaveChangesAsync(),"auditoría inmutable desde EF");db.ChangeTracker.Clear();
        await using(var tx=await db.Database.BeginTransactionAsync()){
            db.AuditoriaUsuarios.Add(new(){Accion="REVERTIDO"});await db.SaveChangesAsync();await tx.RollbackAsync();
        }
        db.ChangeTracker.Clear();Check(await db.AuditoriaUsuarios.CountAsync()==1&&await db.RegistrosSeguridad.CountAsync()==1,"rollback incluye evidencia");
        var carpeta=Path.Combine(Path.GetTempPath(),"zuni-seguridad-"+Guid.NewGuid());Directory.CreateDirectory(carpeta);
        var ruta=Path.Combine(carpeta,"bitacora.jsonl");var bitacora=new BitacoraExterna(new(Path.Combine(carpeta,"sin-claves"),ruta),cripto);
        await bitacora.Exportar(db);await bitacora.VerificarContraBase(db);Check(true,"exportación y verificación íntegra");
        Check(!File.ReadAllText(ruta).Contains(secreto),"copia externa no expone texto clínico");
        var original=File.ReadAllText(ruta);await File.WriteAllTextAsync(ruta,original.Replace("AUDITORIA_USUARIO","AUDITORIA_ALTERADA"));
        await Rechaza(()=>bitacora.VerificarContraBase(db),"alteración del archivo externo");await File.WriteAllTextAsync(ruta,original);
        await db.Database.ExecuteSqlRawAsync("UPDATE AuditoriaUsuarios SET Accion='ALTERADO'");db.ChangeTracker.Clear();
        await Rechaza(()=>bitacora.VerificarContraBase(db),"cambio directo de base de datos");
        await db.Database.ExecuteSqlRawAsync("UPDATE AuditoriaUsuarios SET Accion='PRUEBA'");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM RegistrosSeguridad");
        await Rechaza(()=>bitacora.VerificarContraBase(db),"borrado de evento firmado");
        Console.WriteLine($"OK: {checks} comprobaciones de cifrado, evidencia del consentimiento e integridad externa.");
    }
}
