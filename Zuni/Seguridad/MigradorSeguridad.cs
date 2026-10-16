using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Seguridad;

// Los identificadores interpolados proceden exclusivamente de la lista fija CifradoClinico y del modelo EF.
// Los valores y claves de registros se pasan mediante parámetros DbParameter.
#pragma warning disable EF1002

// Se ejecuta en mantenimiento, con respaldo previo y el rol propietario de las tablas.
// No usa el conversor EF para leer texto antiguo. Toda la conversión es una transacción.
public static class MigradorSeguridad
{
    public static async Task ExigirConversion(ApplicationDbContext db)
    {
        if(!await db.Set<EstadoProteccion>().AnyAsync(e=>e.Id==1))
            throw new InvalidOperationException("Primero respalda PostgreSQL y las claves y ejecuta --migrar-seguridad en mantenimiento.");
    }
    public static async Task Ejecutar(ApplicationDbContext db,CancellationToken ct=default)
    {
        if(await db.Set<EstadoProteccion>().AnyAsync(e=>e.Id==1,ct))return;
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(19191928)",ct);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"AuditoriaUsuarios\" DISABLE TRIGGER seguridad_inmutable",ct);
        foreach(var (tipo,campos) in CifradoClinico.Campos)
        {
            var tabla=db.Model.FindEntityType(tipo)!.GetTableName()!;
            var pk=db.Model.FindEntityType(tipo)!.FindPrimaryKey()!.Properties.Single().Name;
            await db.Database.ExecuteSqlRawAsync($"LOCK TABLE \"{tabla}\" IN ACCESS EXCLUSIVE MODE",ct);
            // El catálogo contiene solo identificadores del modelo, nunca datos del usuario.
            Guid? ultimo=null;
            while(true)
            {
                var filas=new List<(Guid Id,string?[] Valores)>();
                await using(var cmd=db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.Transaction=tx.GetDbTransaction();
                    cmd.CommandText=$"SELECT \"{pk}\",{string.Join(',',campos.Select(c=>$"\"{c}\""))} FROM \"{tabla}\" {(ultimo.HasValue?$"WHERE \"{pk}\">@ultimo":"")} ORDER BY \"{pk}\" LIMIT 200";
                    if(ultimo.HasValue){var p=cmd.CreateParameter();p.ParameterName="ultimo";p.Value=ultimo.Value;cmd.Parameters.Add(p);}
                    await using var reader=await cmd.ExecuteReaderAsync(ct);
                    while(await reader.ReadAsync(ct))filas.Add((reader.GetGuid(0),campos.Select((_,i)=>reader.IsDBNull(i+1)?null:reader.GetString(i+1)).ToArray()));
                }
                if(filas.Count==0)break;
                foreach(var fila in filas)
                {
                    await using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.Transaction=tx.GetDbTransaction();
                    cmd.CommandText=$"UPDATE \"{tabla}\" SET {string.Join(',',campos.Select((c,i)=>$"\"{c}\"=@p{i}"))} WHERE \"{pk}\"=@id";
                    var id=cmd.CreateParameter();id.ParameterName="id";id.Value=fila.Id;cmd.Parameters.Add(id);
                    for(var i=0;i<campos.Length;i++)
                    {
                        var p=cmd.CreateParameter();p.ParameterName="p"+i;p.DbType=DbType.String;
                        var valor=fila.Valores[i];
                        if(valor is not null){
                            var proposito=CifradoClinico.Proposito(tipo,campos[i]);
                            // Una segunda ejecución no cifra dos veces; sobres dañados abortan todo.
                            if(valor.StartsWith("zenc1:",StringComparison.Ordinal))db.Proteccion.Descifrar(valor,proposito);
                            else { var cifrado=db.Proteccion.Cifrar(valor,proposito);if(db.Proteccion.Descifrar(cifrado,proposito)!=valor)throw new InvalidOperationException("La verificación de conversión falló.");valor=cifrado; }
                        }
                        p.Value=(object?)valor??DBNull.Value;cmd.Parameters.Add(p);
                    }
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                ultimo=filas[^1].Id;
            }
            foreach(var campo in campos)
                await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{tabla}\" ADD CONSTRAINT \"ENC_{tabla}_{campo}\" CHECK (\"{campo}\" IS NULL OR \"{campo}\" LIKE 'zenc1:%')",ct);
        }
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"AuditoriaUsuarios\" ENABLE ALWAYS TRIGGER seguridad_inmutable",ct);
        // La fecha original se conserva dentro del evento. La firma solo acredita su estado al migrar.
        for(int skip=0;;skip+=200){var rows=await db.AuditoriaUsuarios.AsNoTracking().OrderBy(a=>a.Id).Skip(skip).Take(200).ToListAsync(ct);if(rows.Count==0)break;foreach(var a in rows)db.RegistrarSeguridad("AUDITORIA_USUARIO",a.Id.ToString(),a,true);await db.SaveChangesAsync(ct);db.ChangeTracker.Clear();}
        for(int skip=0;;skip+=200){var rows=await db.EventosAccesoClinico.AsNoTracking().OrderBy(a=>a.Id).Skip(skip).Take(200).ToListAsync(ct);if(rows.Count==0)break;foreach(var a in rows)db.RegistrarSeguridad("ACCESO_CLINICO",a.Id.ToString(),a,true);await db.SaveChangesAsync(ct);db.ChangeTracker.Clear();}
        db.Set<EstadoProteccion>().Add(new(){ConversionUtc=DateTime.UtcNow});
        db.RegistrarSeguridad("CONVERSION_CLINICA","1",new {Version=1,HistoricosSinIp=true},true);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
