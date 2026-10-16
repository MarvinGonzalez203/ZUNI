using Zuni.Data;
using Microsoft.EntityFrameworkCore;
namespace Zuni.Seguridad;

public static class PermisosPersistencia
{
    public static async Task ValidarProduccion(ApplicationDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        try {
            await using var cmd=db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText="""
                SELECT EXISTS (SELECT 1 FROM pg_roles WHERE rolname=current_user AND (rolsuper OR rolcreaterole OR rolcreatedb OR rolbypassrls))
                OR EXISTS (SELECT 1 FROM pg_tables WHERE schemaname='public' AND tablename IN ('AuditoriaUsuarios','EventosAccesoClinico','RegistrosSeguridad','ExportacionesSeguridad','ConsentimientosAtencion','AceptacionesPrivacidad','EstadoProteccion') AND tableowner=current_user)
                OR has_schema_privilege(current_user,'public','CREATE')
                OR EXISTS (SELECT 1 FROM (VALUES ('AuditoriaUsuarios'),('EventosAccesoClinico'),('RegistrosSeguridad'),('ExportacionesSeguridad'),('ConsentimientosAtencion'),('AceptacionesPrivacidad'),('EstadoProteccion')) AS t(nombre)
                    WHERE has_table_privilege(current_user,quote_ident(nombre),'UPDATE,DELETE,TRUNCATE,TRIGGER'))
                """;
            if(Equals(await cmd.ExecuteScalarAsync(),true))throw new InvalidOperationException("Producción requiere un usuario PostgreSQL de ejecución sin propiedad de tablas ni permisos para alterar o borrar auditorías. Usa un rol separado para migraciones.");
        }
        finally{await db.Database.CloseConnectionAsync();}
    }
}
