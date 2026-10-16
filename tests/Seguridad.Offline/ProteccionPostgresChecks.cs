using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Seguridad;

// Integración optativa. Rechaza cualquier base que no sea un clon local nombrado expresamente para pruebas.
internal static class ProteccionPostgresChecks
{
    public static async Task Run(bool sembrar)
    {
        var cs=Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")??throw new Exception("Falta conexión de prueba explícita.");
        var cfg=new NpgsqlConnectionStringBuilder(cs);
        if(cfg.Host is not ("localhost" or "127.0.0.1" or "::1")||cfg.Database?.StartsWith("zuni_seguridad_prueba_",StringComparison.Ordinal)!=true)
            throw new Exception("Esta prueba solo admite un clon PostgreSQL local.");
        var proteccion=ProteccionPersistencia.Cargar(Environment.GetEnvironmentVariable("ZUNI_TEST_KEYS_PATH")??throw new Exception("Falta ruta explícita de claves de prueba."),"Zuni");
        await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(cs).Options,proteccion);
        if(sembrar){
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO "AspNetUsers" ("Id","FullName","CreatedAtUtc","IsActive","DebeCambiarContrasena","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount") VALUES ('fixture-cifrado','Persona ficticia',now(),true,false,false,false,false,false,0);
                INSERT INTO "PerfilesEstudiante" ("Id","UsuarioId","Carne","FechaCreacionUtc","Activo") VALUES ('11111111-1111-1111-1111-111111111111','fixture-cifrado','9998-26-123456',now(),true);
                INSERT INTO "SolicitudesAtencion" ("Id","PerfilEstudianteId","FechaSolicitudUtc","Estado","TipoIngreso") VALUES ('22222222-2222-2222-2222-222222222222','11111111-1111-1111-1111-111111111111',now(),0,0);
                INSERT INTO "ExpedientesIniciales" ("Id","SolicitudAtencionId","Edad","EsMayorEdad","Sexo","Direccion","IdiomaPreferido","MotivoConsulta","ConsideracionesAtencion","FechaCreacionUtc") VALUES ('33333333-3333-3333-3333-333333333333','22222222-2222-2222-2222-222222222222',20,true,'No binario','Dirección ficticia','Español','Motivo confidencial de prueba','Consideración de prueba',now());
                INSERT INTO "ContactosEmergenciaExpediente" ("Id","ExpedienteInicialId","Nombre","Relacion","Telefono","Orden") VALUES ('44444444-4444-4444-4444-444444444444','33333333-3333-3333-3333-333333333333','Contacto ficticio','Familiar','12345678',1);
                """);
            Console.WriteLine("Fixture con textos antiguos creada solo en el clon.");return;
        }
        int checks=0;void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks++;}
        var expediente=await db.ExpedientesIniciales.SingleAsync(e=>e.Id==Guid.Parse("33333333-3333-3333-3333-333333333333"));
        Check(expediente.MotivoConsulta=="Motivo confidencial de prueba"&&expediente.Direccion=="Dirección ficticia","Conversión clínica conserva Unicode y contenido antiguo");
        Check((await db.ContactosEmergenciaExpediente.SingleAsync(c=>c.Id==Guid.Parse("44444444-4444-4444-4444-444444444444"))).Telefono=="12345678","Teléfono cifrado compatible con restricciones");
        await db.Database.OpenConnectionAsync();
        await using(var cmd=db.Database.GetDbConnection().CreateCommand()){
            cmd.CommandText="SELECT \"MotivoConsulta\" FROM \"ExpedientesIniciales\" WHERE \"Id\"='33333333-3333-3333-3333-333333333333'";
            Check(((string)(await cmd.ExecuteScalarAsync())!).StartsWith("zenc1:"),"PostgreSQL almacena sobres cifrados");
        }
        await db.Database.CloseConnectionAsync();
        var cita=new Cita{PerfilEstudianteId=Guid.Parse("11111111-1111-1111-1111-111111111111"),PsicologoUsuarioId="fixture-cifrado",EsImprevista=true,
            FechaHoraInicioUtc=DateTime.UtcNow.AddDays(-2),FechaHoraFinUtc=DateTime.UtcNow.AddDays(-2).AddMinutes(50),Estado=EstadoCita.Completada};
        db.Citas.Add(cita);var nota=new RegistroAtencionCita{CitaId=cita.Id,SolicitudAtencionId=expediente.SolicitudAtencionId,Asistio=true,ResultadoProfesionalPrivado="Nota privada ficticia",ResultadoBreveEstudiante="Resultado ficticio",ResenaRecomendacionesEstudiante="Apoyo ficticio",FechaCierreUtc=DateTime.UtcNow};
        db.RegistrosAtencionCita.Add(nota);db.AuditoriaUsuarios.Add(new(){Accion="PRUEBA_POSTGRES",UsuarioAfectadoId="fixture-cifrado",AdministradorId="fixture-cifrado",DatosNuevos="{\"referencia\":\"ficticia\"}"});await db.SaveChangesAsync();db.ChangeTracker.Clear();
        Check((await db.RegistrosAtencionCita.SingleAsync(r=>r.Id==nota.Id)).ResultadoProfesionalPrivado=="Nota privada ficticia","Nueva atención se cifra y recupera");
        foreach(var sql in new[]{"UPDATE \"AuditoriaUsuarios\" SET \"Accion\"=\"Accion\" WHERE false","DELETE FROM \"EventosAccesoClinico\" WHERE false","TRUNCATE \"RegistrosSeguridad\""}){
            bool rechazo=false;try{await db.Database.ExecuteSqlRawAsync(sql);}catch(PostgresException e)when(e.SqlState=="42501"){rechazo=true;}
            Check(rechazo,"Trigger impide alterar o truncar auditoría");
        }
        var bitacora=new BitacoraExterna(new("sin-uso",Environment.GetEnvironmentVariable("SeguridadClinica__BitacoraPath")??throw new Exception("Falta ruta de bitácora de prueba.")),proteccion);
        while(await db.RegistrosSeguridad.AnyAsync(r=>!db.Set<ExportacionSeguridad>().Any(a=>a.EventoId==r.EventoId))){await bitacora.Exportar(db);db.ChangeTracker.Clear();}
        await bitacora.VerificarContraBase(db);Check(true,"Firmas sobreviven precisión temporal de PostgreSQL");
        Console.WriteLine($"OK: {checks} comprobaciones PostgreSQL de conversión, cifrado y triggers en clon aislado.");
    }
}
