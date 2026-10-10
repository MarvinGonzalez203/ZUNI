using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Zuni.Controllers;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Atencion;
using Zuni.Models.BigFive;
using Zuni.Resources;
using Zuni.Services;
using Zuni.Services.BigFive;

var checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
void Rechaza(Action a, string label)
{
    try { a(); } catch (BigFiveOperacionException) { checks++; return; }
    throw new Exception(label);
}
async Task RechazaAsync<T>(Func<Task> a, string label) where T : Exception
{
    try { await a(); } catch (T) { checks++; return; }
    throw new Exception(label);
}
ClaimsPrincipal Sesion(string id, params string[] roles) => new(new ClaimsIdentity(
    new[] { new Claim(ClaimTypes.NameIdentifier, id), new Claim("Zuni.SecurityStamp", id + "-stamp") }
        .Concat(roles.Select(r => new Claim(ClaimTypes.Role, r))), "Offline"));
List<RespuestaBigFive> Respuestas(Func<Ipip50.Item, int> valor) => Ipip50.Items.Select(i =>
    new RespuestaBigFive { ItemId = i.Id.ToString("D"), Valor = valor(i) }).ToList();

Check(Ipip50.Items.Count == 50 && Ipip50.Items.Select(i => i.Id).Distinct().Count() == 50, "50 IDs únicos");
Check(Ipip50.Items.GroupBy(i => i.Rasgo).All(g => g.Count() == 10), "10 por dimensión");
var ordenId = Guid.NewGuid();
Check(Ipip50.Orden(ordenId).SequenceEqual(Ipip50.Orden(ordenId)), "Orden estable");
Check(Ipip50.Orden(ordenId).Chunk(10).All(b => b.GroupBy(i => i.Rasgo).All(g => g.Count() == 2)), "Bloques equilibrados");
foreach (var v in new[] { 1, 3, 5 })
    Check(Ipip50.Calcular(Respuestas(i => i.Inversa ? 6 - v : v)).Values.All(x => x == v), "Inversión y límites");
var unas = Ipip50.Calcular(Respuestas(_ => 1));
Check(unas['E'] == 3 && unas['A'] == 2.6m && unas['C'] == 2.6m && unas['N'] == 1.8m && unas['O'] == 2.2m, "Claves esperadas");
Rechaza(() => Ipip50.Calcular(Respuestas(_ => 3).Take(49)), "Faltantes");
var duplicadas = Respuestas(_ => 3); duplicadas[49].ItemId = duplicadas[0].ItemId;
Rechaza(() => Ipip50.Calcular(duplicadas), "Duplicados");
var ajenas = Respuestas(_ => 3); ajenas[0].ItemId = Guid.NewGuid().ToString();
Rechaza(() => Ipip50.Calcular(ajenas), "Item ajeno");
foreach (var v in new[] { 0, 6 }) Rechaza(() => Ipip50.Calcular(Respuestas(_ => v)), "Fuera de rango");
foreach (var flags in new[] { (false, false), (true, false), (false, true) })
    Rechaza(() => BigFiveReglas.ValidarConsentimiento(flags.Item1, flags.Item2), "Consentimiento obligatorio");
BigFiveReglas.ValidarConsentimiento(true, true);
checks++;
var borradorRetirado = new ParticipacionBigFive { VersionInstrumento = Ipip50.Version,
    VersionConsentimiento = ConsentimientoBigFive.Version, TextoConsentimiento = ConsentimientoBigFive.Texto,
    FechaConsentimientoUtc = DateTime.UtcNow };
BigFiveReglas.Retirar(borradorRetirado, DateTime.UtcNow);
Rechaza(() => BigFiveReglas.Editable(borradorRetirado, borradorRetirado.Revision), "Retiro bloquea borrador no finalizado");
var revisionRetirada = borradorRetirado.Revision;
BigFiveReglas.Retirar(borradorRetirado, DateTime.UtcNow);
Check(borradorRetirado.Revision == revisionRetirada, "Retiro idempotente");

// Base efímera SQLite. No se lee configuración ni secretos ni se conecta a PostgreSQL.
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
connection.CreateFunction<string, string>("btrim", value => value.Trim());
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
await using var db = new OfflineDb(options);
await db.Database.EnsureCreatedAsync();
var roles = new[] { "Estudiante", "Psicologo", "Administrador", "Director", "Catedratico" };
foreach (var role in roles) db.Roles.Add(new IdentityRole(role) { Id = role, NormalizedName = role.ToUpperInvariant() });
void Usuario(string id, string[] userRoles)
{
    db.Users.Add(new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(),
        FullName = "Prueba offline", IsActive = true, SecurityStamp = id + "-stamp" });
    foreach (var role in userRoles) db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = role });
}
Usuario("est", ["Estudiante"]); Usuario("otro-est", ["Estudiante"]); Usuario("psi", ["Psicologo"]);
Usuario("otro-psi", ["Psicologo"]);
foreach (var role in new[] { "Administrador", "Director", "Catedratico" })
{
    Usuario(role, [role]); Usuario("mixto-" + role, [role, "Psicologo"]);
}
var perfil = new PerfilEstudiante { Id = Guid.NewGuid(), UsuarioId = "est", Carne = "0000000001" };
var otroPerfil = new PerfilEstudiante { Id = Guid.NewGuid(), UsuarioId = "otro-est", Carne = "0000000002" };
db.PerfilesEstudiante.AddRange(perfil, otroPerfil);
var solicitud = new SolicitudAtencion { PerfilEstudianteId = perfil.Id };
db.SolicitudesAtencion.Add(solicitud);
db.ConsentimientosAtencion.Add(new ConsentimientoAtencion { SolicitudAtencionId = solicitud.Id, Aceptado = true, VersionConsentimiento = "ZUNI-CONSENT-1" });
db.ExpedientesIniciales.Add(new ExpedienteInicial { SolicitudAtencionId = solicitud.Id, Edad = 20, EsMayorEdad = true,
    Direccion = "Solo memoria", IdiomaPreferido = "Español", MotivoConsulta = "Prueba offline" });
await db.SaveChangesAsync();
db.ChangeTracker.Clear();
var auth = new AutorizacionClinicaService(db);
var service = new BigFiveService(db, auth);
var estudiante = Sesion("est", "Estudiante");
var psicologo = Sesion("psi", "Psicologo");
var ct = CancellationToken.None;
Check((await service.Estado(estudiante, ct)).TieneSolicitudActiva, "Solicitud activa");
db.UserRoles.Add(new IdentityUserRole<string> { UserId = "est", RoleId = "Administrador" }); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<BigFiveAccesoException>(() => service.Estado(estudiante, ct), "Rol institucional real impide acceso propio aunque la claim sea antigua");
db.UserRoles.Remove(await db.UserRoles.SingleAsync(r => r.UserId == "est" && r.RoleId == "Administrador")); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<BigFiveAccesoException>(() => service.Estado(Sesion("est", "Administrador"), ct), "Rol falso");
await RechazaAsync<BigFiveOperacionException>(() => service.Aceptar(Sesion("otro-est", "Estudiante"), new() { Acepto = true, MayorDeEdad = true }, ct), "Requiere solicitud");
db.ChangeTracker.Clear();
await service.Aceptar(estudiante, new() { Acepto = true, MayorDeEdad = true }, ct);
await service.Aceptar(estudiante, new() { Acepto = true, MayorDeEdad = true }, ct);
Check(await db.ParticipacionesBigFive.CountAsync() == 1, "Aceptación idempotente");
Check(await db.AsignacionesEstudiantePsicologo.CountAsync() == 0, "No crea asignación");
var p = await db.ParticipacionesBigFive.AsNoTracking().SingleAsync();
Check(p.VersionInstrumento == Ipip50.Version && p.TextoConsentimiento == ConsentimientoBigFive.Texto, "Versión y texto persistidos");
await RechazaAsync<BigFiveAccesoException>(() => service.Cuestionario(Sesion("otro-est", "Estudiante"), p.Id, ct), "Respuestas privadas");
await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(psicologo, solicitud.Id, ct), "Sin asignación no hay resumen");
var parcial = Ipip50.Items.Take(10).ToDictionary(i => i.Id.ToString("D"), _ => (int?)3);
db.ChangeTracker.Clear();
await service.Guardar(estudiante, new() { Id = p.Id, Revision = 0, Respuestas = parcial }, ct);
var q = await service.Cuestionario(estudiante, p.Id, ct);
Check(q.Respuestas.Count == 10 && q.Revision == 1, "Guardar y recuperar avance");
await RechazaAsync<BigFiveOperacionException>(() => service.Guardar(estudiante, new() { Id = p.Id, Revision = 0, Respuestas = parcial }, ct), "Revisión obsoleta");
await RechazaAsync<BigFiveOperacionException>(() => service.Finalizar(estudiante, new() { Id = p.Id, Revision = 1, Confirmado = true }, ct), "No finalizar parcial");
db.ChangeTracker.Clear();
await service.Guardar(estudiante, new() { Id = p.Id, Revision = 1, Respuestas = Ipip50.Items.ToDictionary(i => i.Id.ToString("D"), i => (int?)(i.Inversa ? 2 : 4)) }, ct);
await RechazaAsync<BigFiveOperacionException>(() => service.Finalizar(estudiante, new() { Id = p.Id, Revision = 2, Confirmado = false }, ct), "Confirmación final");
db.ChangeTracker.Clear();
db.FallarFinalizacion = true;
await RechazaAsync<DbUpdateException>(() => service.Finalizar(estudiante, new() { Id = p.Id, Revision = 2, Confirmado = true }, ct), "Fallo de persistencia final");
db.FallarFinalizacion = false; db.ChangeTracker.Clear();
Check(await db.ParticipacionesBigFive.AllAsync(x => x.FechaFinalizacionUtc == null && x.Apertura == null && x.Revision == 2), "Rollback finalización");
await service.Finalizar(estudiante, new() { Id = p.Id, Revision = 2, Confirmado = true }, ct);
p = await db.ParticipacionesBigFive.AsNoTracking().SingleAsync();
Check(p.FechaFinalizacionUtc != null && Ipip50.Dimensiones(p).All(d => d.Media == 4), "Finaliza cinco medias sin asignación");
await RechazaAsync<BigFiveOperacionException>(() => service.Guardar(estudiante, new() { Id = p.Id, Revision = 3, Respuestas = parcial }, ct), "Inmutable finalizada");
await RechazaAsync<BigFiveOperacionException>(() => service.Cuestionario(estudiante, p.Id, ct), "No devuelve respuestas después de finalizar");
Check(typeof(EstadoBigFiveViewModel).GetProperties().All(x => x.Name != "Apertura"), "Estado estudiantil sin puntuaciones");
db.ChangeTracker.Clear();
var link = new AsignacionEstudiantePsicologo { PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "psi" };
db.AsignacionesEstudiantePsicologo.Add(link);
await db.SaveChangesAsync();
db.ChangeTracker.Clear();
foreach (var role in new[] { "Administrador", "Director", "Catedratico" })
{
    await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(Sesion(role, role), solicitud.Id, ct), "Rol institucional");
    // Dar asignación al actor mixto para demostrar que la denegación es por roles, no por falta de vínculo.
    var current = await db.AsignacionesEstudiantePsicologo.SingleAsync();
    current.PsicologoUsuarioId = "mixto-" + role; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(Sesion("mixto-" + role, role, "Psicologo"), solicitud.Id, ct), "Rol combinado");
}
var restore = await db.AsignacionesEstudiantePsicologo.SingleAsync(); restore.PsicologoUsuarioId = "psi";
await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(Sesion("otro-psi", "Psicologo"), solicitud.Id, ct), "Otro psicólogo");
await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(psicologo, Guid.NewGuid(), ct), "Solicitud ajena/ausente");
var sinStamp = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "psi"), new Claim(ClaimTypes.Role, "Psicologo") }, "Offline"));
await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(sinStamp, solicitud.Id, ct), "Stamp obligatorio");
var psi = await db.Users.SingleAsync(u => u.Id == "psi"); psi.IsActive = false; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(psicologo, solicitud.Id, ct), "Actor inactivo");
psi = await db.Users.SingleAsync(u => u.Id == "psi"); psi.IsActive = true; psi.SecurityStamp = "nuevo"; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(psicologo, solicitud.Id, ct), "Stamp antiguo");
psi = await db.Users.SingleAsync(u => u.Id == "psi"); psi.SecurityStamp = "psi-stamp"; psi.DebeCambiarContrasena = true; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<BigFiveAccesoException>(() => service.Resumen(psicologo, solicitud.Id, ct), "Cambio obligatorio");
psi = await db.Users.SingleAsync(u => u.Id == "psi"); psi.DebeCambiarContrasena = false; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
var resumen = await service.Resumen(psicologo, solicitud.Id, ct);
Check(resumen?.Dimensiones.Count == 5, "Resumen autorizado");
Check(await db.EventosAccesoClinico.CountAsync(e => e.TipoAcceso == "BIGFIVE_CONSULTADO") == 1, "Auditoría exitosa");
db.ChangeTracker.Clear();
db.FallarAuditoria = true;
var controller = new BigFivePsicologoController(service) { ControllerContext = new ControllerContext
    { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = psicologo } } };
Check(await controller.Resumen(solicitud.Id, ct) is ObjectResult { StatusCode: 503 }, "Fallo de auditoría no devuelve vista");
db.FallarAuditoria = false; db.ChangeTracker.Clear();
Check(await db.EventosAccesoClinico.CountAsync() == 1, "Rollback auditoría");
await service.Retirar(estudiante, p.Id, ct);
db.ChangeTracker.Clear();
Check(await service.Resumen(psicologo, solicitud.Id, ct) == null, "Retiro impide resumen");
await RechazaAsync<BigFiveOperacionException>(() => service.Cuestionario(estudiante, p.Id, ct), "Retiro impide responder");
Check(await db.SolicitudesAtencion.CountAsync() == 1 && await db.ConsentimientosAtencion.CountAsync() == 1 &&
    await db.AsignacionesEstudiantePsicologo.CountAsync() == 1 && await db.RespuestasBigFive.CountAsync() == 50, "Retiro no borra atención ni respuestas");
await service.Aceptar(estudiante, new() { Acepto = true, MayorDeEdad = true }, ct);
Check(await db.ParticipacionesBigFive.CountAsync() == 1 && (await service.Estado(estudiante, ct)).FechaRetiroConsentimientoUtc != null, "No reaplica tras retiro");
Check(await db.EventosAccesoClinico.CountAsync() == 1, "Sin auditorías para consultas sin resultado");
db.ChangeTracker.Clear();
db.RespuestasBigFive.Add(new RespuestaBigFive { ParticipacionBigFiveId = p.Id, ItemId = Ipip50.Items[0].Id.ToString("D"), Valor = 3, FechaActualizacionUtc = DateTime.UtcNow });
await RechazaAsync<DbUpdateException>(() => db.SaveChangesAsync(), "BD rechaza respuesta duplicada válida");
db.ChangeTracker.Clear();
var respuestaFueraRango = await db.RespuestasBigFive.FirstAsync();
respuestaFueraRango.Valor = 6;
await RechazaAsync<DbUpdateException>(() => db.SaveChangesAsync(), "BD rechaza valor fuera de rango");
db.ChangeTracker.Clear();
db.RespuestasBigFive.Add(new RespuestaBigFive { ParticipacionBigFiveId = p.Id, ItemId = Ipip50.Items[0].Id.ToString("D"), Valor = 6, FechaActualizacionUtc = DateTime.UtcNow });
await RechazaAsync<DbUpdateException>(() => db.SaveChangesAsync(), "BD rechaza respuesta inválida/duplicada");
db.ChangeTracker.Clear();
var respuestaExistente = await db.RespuestasBigFive.FirstAsync();
respuestaExistente.ItemId = Guid.NewGuid().ToString("D");
await RechazaAsync<DbUpdateException>(() => db.SaveChangesAsync(), "BD rechaza ítem fuera del catálogo");
db.ChangeTracker.Clear();
var participacionExistente = await db.ParticipacionesBigFive.SingleAsync();
participacionExistente.Apertura = null;
await RechazaAsync<DbUpdateException>(() => db.SaveChangesAsync(), "BD rechaza finalización sin cinco medias");
db.ChangeTracker.Clear();
var fueraRango = await db.ParticipacionesBigFive.SingleAsync();
fueraRango.Apertura = 6;
await RechazaAsync<DbUpdateException>(() => db.SaveChangesAsync(), "BD rechaza media fuera de rango");
db.ChangeTracker.Clear();
foreach (var method in typeof(BigFiveController).GetMethods().Where(m => m.GetCustomAttributes(typeof(HttpPostAttribute), true).Length != 0))
    Check(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).Length == 1, "Antiforgery en POST");

// Proveedor Npgsql: solo construir metadatos y SQL, nunca abrir conexión.
await using var pg = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=127.0.0.1;Port=1;Database=OfflineNeverConnect;Username=offline").Options);
var assembly = pg.GetService<IMigrationsAssembly>();
var migration = assembly.CreateMigration(assembly.Migrations.Single(m => m.Key.EndsWith("_AddBigFiveApoyo")).Value, pg.Database.ProviderName!);
var allowed = new[] { "ParticipacionesBigFive", "RespuestasBigFive" };
Check(migration.UpOperations.OfType<CreateTableOperation>().Select(t => t.Name).Order().SequenceEqual(allowed.Order()), "Solo dos tablas nuevas");
Check(migration.UpOperations.All(o => o is CreateTableOperation || o is CreateIndexOperation i && allowed.Contains(i.Table)), "No altera tablas existentes");
Check(migration.DownOperations.Count == 2 && migration.DownOperations.All(o => o is DropTableOperation t && allowed.Contains(t.Name)), "Down solo tablas Big Five");
Check(migration.UpOperations.OfType<CreateTableOperation>().SelectMany(t => t.ForeignKeys).All(f => f.OnDelete == ReferentialAction.Restrict), "FK Restrict");
var design = pg.GetService<IDesignTimeModel>().Model;
var snapshot = pg.GetService<IModelRuntimeInitializer>().Initialize(assembly.ModelSnapshot!.Model, designTime: true);
Check(!pg.GetService<IMigrationsModelDiffer>().HasDifferences(snapshot.GetRelationalModel(), design.GetRelationalModel()), "Snapshot sincronizado");
var tabla = migration.UpOperations.OfType<CreateTableOperation>().Single(t => t.Name == "ParticipacionesBigFive");
Check(tabla.CheckConstraints.Any(c => c.Name == "CK_BigFive_Finalizacion" && c.Sql.Contains("IS NOT NULL")), "Constraint puntuaciones completas");
Check(migration.UpOperations.OfType<CreateIndexOperation>().Count(i => i.IsUnique) == 2, "Unicidad participación y respuestas");
Check(migration.UpOperations.OfType<CreateTableOperation>().Single(t => t.Name == "RespuestasBigFive").CheckConstraints.Count == 2, "Restricciones ítem y rango");
Console.WriteLine($"OK: {checks} comprobaciones offline. PostgreSQL no fue abierto ni modificado.");

sealed class OfflineDb(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    public bool FallarAuditoria { get; set; }
    public bool FallarFinalizacion { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // SQLite no interpreta el operador regex PostgreSQL de tablas ajenas.
        // Conservamos las restricciones Big Five. Los metadatos Npgsql se comprueban por separado.
        foreach (var entity in builder.Model.GetEntityTypes())
            foreach (var constraint in entity.GetCheckConstraints().Where(c => c.Name != null && !c.Name.Contains("BigFive")).ToArray())
                entity.RemoveCheckConstraint(constraint.Name!);
    }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (FallarAuditoria && ChangeTracker.Entries<EventoAccesoClinico>().Any(e => e.State == EntityState.Added))
            throw new DbUpdateException("Fallo simulado offline.");
        if (FallarFinalizacion && ChangeTracker.Entries<ParticipacionBigFive>().Any(e => e.Entity.FechaFinalizacionUtc != null))
            throw new DbUpdateException("Fallo simulado de finalización.");
        return base.SaveChangesAsync(ct);
    }
}
