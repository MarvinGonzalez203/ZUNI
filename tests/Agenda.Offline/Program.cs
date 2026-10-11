using System.Data;
using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Zuni.Controllers;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Services;
using Zuni.Services.Agenda;

var checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
void Rechaza(Action action, string label)
{
    try { action(); } catch (AgendaOperacionException) { checks++; return; }
    throw new Exception(label);
}
async Task RechazaAsync<T>(Func<Task> action, string label) where T : Exception
{
    try { await action(); } catch (T) { checks++; return; }
    throw new Exception(label);
}
ClaimsPrincipal Sesion(string id, params string[] roles) => new(new ClaimsIdentity(
    new[] { new Claim(ClaimTypes.NameIdentifier, id), new Claim("Zuni.SecurityStamp", id + "-stamp") }
        .Concat(roles.Select(r => new Claim(ClaimTypes.Role, r))), "Offline"));
var ct = CancellationToken.None;
var reloj = new Reloj { Ahora = new DateTimeOffset(2030, 1, 7, 12, 0, 0, TimeSpan.Zero) };
var dia = new DateOnly(2030, 1, 7); // lunes, 06:00 Guatemala
var inicio = AgendaReglas.Utc(dia, new TimeOnly(9, 0));
Check(inicio == new DateTime(2030, 1, 7, 15, 0, 0, DateTimeKind.Utc), "Guatemala -> UTC");
Check(AgendaReglas.Local(inicio).Hour == 9, "UTC -> Guatemala");
Check(!AgendaReglas.Cruza(inicio, inicio.AddMinutes(50), inicio.AddMinutes(50), inicio.AddMinutes(100)), "Citas contiguas permitidas");
Check(AgendaReglas.Cruza(inicio, inicio.AddMinutes(50), inicio.AddMinutes(49), inicio.AddMinutes(99)), "Solapamiento parcial");
foreach (var input in new[] {
    new DisponibilidadInput { HoraInicio = new(10,0), HoraFin = new(9,0) },
    new DisponibilidadInput { HoraInicio = new(9,0), HoraFin = new(10,0), DuracionMinutos = 14 },
    new DisponibilidadInput { HoraInicio = new(9,0), HoraFin = new(10,0), DuracionMinutos = 17 },
    new DisponibilidadInput { HoraInicio = new(9,0), HoraFin = new(10,0), DuracionMinutos = 125 },
    new DisponibilidadInput { HoraInicio = new(9,0), HoraFin = new(9,30), DuracionMinutos = 50 },
    new DisponibilidadInput { HoraInicio = new(9,0,1), HoraFin = new(10,0) },
    new DisponibilidadInput { HoraInicio = new(9,0), HoraFin = new(10,0), DiaSemana = (DayOfWeek)8 },
    new DisponibilidadInput { HoraInicio = new(9,0), HoraFin = new(10,0), Modalidad = (ModalidadDisponibilidad)9 }
}) Rechaza(() => AgendaReglas.ValidarDisponibilidad(input), "Disponibilidad inválida");
Rechaza(() => AgendaReglas.FechaPermitida(dia.AddDays(91), reloj.Ahora.UtcDateTime), "Horizonte máximo");

// Datos solo en memoria: no carga appsettings, secretos ni ejecuta Program de Zuni.
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var transacciones = new Transacciones();
await using var db = new OfflineDb(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlite(connection).AddInterceptors(transacciones).Options);
await db.Database.EnsureCreatedAsync();
foreach (var role in new[] { "Estudiante", "Psicologo", "Administrador", "Director", "Catedratico" })
    db.Roles.Add(new IdentityRole(role) { Id = role, NormalizedName = role.ToUpperInvariant() });
void Usuario(string id, params string[] roles)
{
    db.Users.Add(new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(),
        FullName = id, IsActive = true, SecurityStamp = id + "-stamp" });
    foreach (var role in roles) db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = role });
}
Usuario("est", "Estudiante"); Usuario("otro-est", "Estudiante"); Usuario("sin-asignar", "Estudiante");
Usuario("psi", "Psicologo"); Usuario("otro-psi", "Psicologo");
foreach (var role in new[] { "Administrador", "Director", "Catedratico" })
{
    Usuario("mixto-" + role, "Psicologo", role);
    Usuario("est-" + role, "Estudiante", role);
}
var perfil = new PerfilEstudiante { UsuarioId = "est", Carne = "0000000001", Carrera = "Offline", Telefono = "12345678" };
var otro = new PerfilEstudiante { UsuarioId = "otro-est", Carne = "0000000002", Carrera = "Offline", Telefono = "12345678" };
var sin = new PerfilEstudiante { UsuarioId = "sin-asignar", Carne = "0000000003", Carrera = "Offline", Telefono = "12345678" };
db.PerfilesEstudiante.AddRange(perfil, otro, sin);
db.AsignacionesEstudiantePsicologo.AddRange(
    new AsignacionEstudiantePsicologo { PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "psi" },
    new AsignacionEstudiantePsicologo { PerfilEstudianteId = otro.Id, PsicologoUsuarioId = "psi" });
await db.SaveChangesAsync(); db.ChangeTracker.Clear();
var service = new AgendaCitasService(db, new AutorizacionClinicaService(db), reloj);
var estudiante = Sesion("est", "Estudiante"); var psicologo = Sesion("psi", "Psicologo");
var segundo = Sesion("otro-est", "Estudiante"); var otroPsicologo = Sesion("otro-psi", "Psicologo");
DisponibilidadInput Horario() => new() { DiaSemana = DayOfWeek.Monday, HoraInicio = new(9,0), HoraFin = new(12,0), DuracionMinutos = 50, Modalidad = ModalidadDisponibilidad.Ambas };
ReservaCitaInput Reserva(DateTime? hora = null, ModalidadCita modalidad = ModalidadCita.Virtual) => new() { Inicio = new DateTimeOffset(hora ?? inicio), Modalidad = modalidad };
await service.AgregarDisponibilidad(psicologo, Horario(), ct);
await service.AgregarDisponibilidad(otroPsicologo, Horario(), ct);
var horarioId = (await service.LeerAgenda(psicologo, dia, ct)).Disponibilidades.Single().Id;
await RechazaAsync<AgendaOperacionException>(() => service.AgregarDisponibilidad(psicologo, Horario(), ct), "Rechaza disponibilidad superpuesta");
await RechazaAsync<AgendaAccesoException>(() => service.CambiarDisponibilidad(otroPsicologo, horarioId, false, ct), "No cambia agenda ajena");
var libre = await service.LeerCitas(estudiante, dia, 1, ct);
Check(libre.Horarios.Count == 3 && libre.Psicologo == "psi", "Slots reales de 50 minutos; sobrante no reservable");
Check((await service.LeerCitas(Sesion("sin-asignar", "Estudiante"), dia, 1, ct)).Horarios.Count == 0, "Sin asignación sin slots");
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(Sesion("sin-asignar", "Estudiante"), Reserva(), ct), "Sin asignación no reserva");
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(estudiante, Reserva(inicio.AddMinutes(1)), ct), "No reserva fuera de cuadrícula");
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(estudiante, Reserva(inicio.AddSeconds(1)), ct), "Rechaza segundos manipulados");
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(estudiante, Reserva(inicio.AddHours(-10)), ct), "No reserva pasado");
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(estudiante, Reserva(inicio.AddHours(3)), ct), "No reserva fuera de ventana");
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(estudiante, Reserva(modalidad: (ModalidadCita)10), ct), "Modalidad inválida");
await service.Reservar(estudiante, Reserva(), ct); db.ChangeTracker.Clear();
Check(!await db.ParticipacionesBigFive.AnyAsync(), "Reserva sin participación Big Five");
var cita = await db.Citas.AsNoTracking().SingleAsync();
Check(cita.PsicologoUsuarioId == "psi" && cita.PerfilEstudianteId == perfil.Id && cita.FechaHoraFinUtc == inicio.AddMinutes(50), "Identidad, duración y asignación del servidor");
Check((await service.LeerCitas(estudiante, dia, 1, ct)).Horarios.Count == 2, "Slot ocupado desaparece");
Check((await service.LeerAgenda(otroPsicologo, dia, ct)).Citas.Count == 0, "Agenda no expone citas ajenas");
Check((await service.LeerCitas(segundo, dia, 1, ct)).Citas.Count == 0, "Estudiante no ve citas ajenas");
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(segundo, Reserva(), ct), "Otro estudiante no duplica psicólogo/hora");
await RechazaAsync<AgendaAccesoException>(() => service.GestionarCita(otroPsicologo, new() { CitaId = cita.Id, Estado = EstadoCita.Cancelada }, ct), "ID ajeno denegado");
await RechazaAsync<AgendaAccesoException>(() => service.CancelarEstudiante(segundo, cita.Id, ct), "Cancelación ajena denegada");
await RechazaAsync<AgendaOperacionException>(() => service.CambiarDisponibilidad(psicologo, horarioId, false, ct), "No desactiva con reservas");
await RechazaAsync<AgendaOperacionException>(() => service.GestionarCita(psicologo, new() { CitaId = cita.Id, Estado = EstadoCita.Completada }, ct), "No completa cita futura");
db.ChangeTracker.Clear();
await service.Reservar(estudiante, Reserva(inicio.AddMinutes(50)), ct); db.ChangeTracker.Clear();
Check(await db.Citas.CountAsync() == 2, "Permite citas contiguas");
await service.CancelarEstudiante(estudiante, cita.Id, ct); db.ChangeTracker.Clear();
await RechazaAsync<AgendaOperacionException>(() => service.CancelarEstudiante(estudiante, cita.Id, ct), "No repite transición final");
db.ChangeTracker.Clear();
await service.Reservar(segundo, Reserva(), ct); db.ChangeTracker.Clear();
Check(await db.Citas.CountAsync(c => c.Estado == EstadoCita.Reservada) == 2, "Cancelación libera horario");
Check((await service.Historial(psicologo, "est", EstadoCita.Cancelada, 1, ct)).Citas.Count == 1, "Historial filtra estado y estudiante");
Check((await service.Historial(otroPsicologo, null, null, 1, ct)).Citas.Count == 0, "Historial privado");
await RechazaAsync<AgendaOperacionException>(() => service.Historial(psicologo, null, EstadoCita.Reservada, 1, ct), "No acepta estado fuera de historial");

// Cada operación vuelve a consultar identidad, stamp y roles persistidos.
foreach (var role in new[] { "Administrador", "Director", "Catedratico" })
{
    var mixto = Sesion("mixto-" + role, "Psicologo"); // claim institucional omitido intencionalmente
    await RechazaAsync<AgendaAccesoException>(() => service.LeerAgenda(mixto, dia, ct), "Rol combinado denegado en lectura");
    await RechazaAsync<AgendaAccesoException>(() => service.AgregarDisponibilidad(mixto, Horario(), ct), "Rol combinado denegado en escritura");
    await RechazaAsync<AgendaAccesoException>(() => service.Historial(mixto, null, null, 1, ct), "Rol combinado denegado en historial");
    await RechazaAsync<AgendaAccesoException>(() => service.LeerCitas(Sesion("est-" + role, "Estudiante"), dia, 1, ct), "Institucional más estudiante denegado");
}
var falsoStamp = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "psi"), new Claim(ClaimTypes.Role, "Psicologo"), new Claim("Zuni.SecurityStamp", "viejo") }, "Offline"));
await RechazaAsync<AgendaAccesoException>(() => service.LeerAgenda(falsoStamp, dia, ct), "Stamp caducado");
await RechazaAsync<AgendaAccesoException>(() => service.LeerCitas(new ClaimsPrincipal(), dia, 1, ct), "Anónimo denegado");
await RechazaAsync<AgendaAccesoException>(() => service.LeerAgenda(Sesion("est", "Psicologo"), dia, ct), "Rol falsificado no sustituye BD");
foreach (var id in new[] { "psi", "est" })
{
    var u = await db.Users.SingleAsync(x => x.Id == id); u.IsActive = false; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    if (id == "psi")
    {
        await RechazaAsync<AgendaAccesoException>(() => service.LeerAgenda(psicologo, dia, ct), "Psicólogo inactivo");
        Check(!(await service.LeerCitas(estudiante, dia, 1, ct)).PuedeReservar, "Asignado inactivo no ofrece slots");
    }
    else await RechazaAsync<AgendaAccesoException>(() => service.Reservar(estudiante, Reserva(inicio.AddMinutes(100)), ct), "Estudiante inactivo");
    u = await db.Users.SingleAsync(x => x.Id == id); u.IsActive = true; u.DebeCambiarContrasena = true; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    if (id == "psi") await RechazaAsync<AgendaAccesoException>(() => service.LeerAgenda(psicologo, dia, ct), "Cambio obligatorio psicólogo");
    else await RechazaAsync<AgendaAccesoException>(() => service.LeerCitas(estudiante, dia, 1, ct), "Cambio obligatorio estudiante");
    u = await db.Users.SingleAsync(x => x.Id == id); u.DebeCambiarContrasena = false; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
}
var p = await db.PerfilesEstudiante.SingleAsync(x => x.Id == perfil.Id); p.Telefono = null; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(estudiante, Reserva(inicio.AddMinutes(100)), ct), "Perfil incompleto no reserva");
p = await db.PerfilesEstudiante.SingleAsync(x => x.Id == perfil.Id); p.Telefono = "12345678"; p.Activo = false; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<AgendaAccesoException>(() => service.LeerCitas(estudiante, dia, 1, ct), "Perfil inactivo");
p = await db.PerfilesEstudiante.SingleAsync(x => x.Id == perfil.Id); p.Activo = true; await db.SaveChangesAsync(); db.ChangeTracker.Clear();

// Cambiar de asignación no permite que el estudiante tenga dos citas simultáneas.
var asignacion = await db.AsignacionesEstudiantePsicologo.SingleAsync(x => x.PerfilEstudianteId == perfil.Id);
asignacion.PsicologoUsuarioId = "otro-psi"; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
await RechazaAsync<AgendaOperacionException>(() => service.Reservar(estudiante, Reserva(inicio.AddMinutes(50)), ct), "Evita cruce del estudiante con otro psicólogo");
var pendiente = await db.Citas.AsNoTracking().SingleAsync(x => x.PerfilEstudianteId == perfil.Id && x.Estado == EstadoCita.Reservada);
await service.CancelarEstudiante(estudiante, pendiente.Id, ct); db.ChangeTracker.Clear();
Check((await db.Citas.AsNoTracking().SingleAsync(x => x.Id == pendiente.Id)).Estado == EstadoCita.Cancelada, "Puede cancelar cita propia de asignación anterior");
await service.Reservar(estudiante, Reserva(inicio.AddMinutes(50)), ct); db.ChangeTracker.Clear();
reloj.Ahora = new DateTimeOffset(inicio.AddMinutes(151));
var realizadas = await db.Citas.AsNoTracking().Where(x => x.Estado == EstadoCita.Reservada).ToListAsync();
foreach (var c in realizadas)
    await service.GestionarCita(Sesion(c.PsicologoUsuarioId, "Psicologo"), new() { CitaId = c.Id,
        Estado = c.PsicologoUsuarioId == "psi" ? EstadoCita.Completada : EstadoCita.NoAsistio }, ct);
db.ChangeTracker.Clear();
Check(await db.Citas.CountAsync(x => x.Estado == EstadoCita.Completada) == 1 && await db.Citas.CountAsync(x => x.Estado == EstadoCita.NoAsistio) == 1, "Asistencia después del fin");
await service.CambiarDisponibilidad(psicologo, horarioId, false, ct); db.ChangeTracker.Clear();
Check(!(await service.LeerAgenda(psicologo, dia, ct)).Disponibilidades.Single().Activo, "Desactiva sin reservas pendientes");
Check(transacciones.Aislamientos.Count > 20 && transacciones.Aislamientos.All(x => x == IsolationLevel.Serializable), "Escrituras transaccionales SERIALIZABLE");

foreach (var type in new[] { typeof(EstudianteController), typeof(PsicologoController) })
    foreach (var method in type.GetMethods().Where(m => m.GetCustomAttributes(typeof(AgendaExceptionFilter), true).Length > 0 && m.GetCustomAttributes(typeof(HttpPostAttribute), true).Length > 0))
        Check(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).Length == 1, "Antiforgery POST agenda");
Check(typeof(ReservaCitaInput).GetProperties().All(x => x.Name != "PsicologoUsuarioId" && x.Name != "PerfilEstudianteId"), "Formulario no admite identidades");

// Solo metadatos y traducción SQL con Npgsql: nunca abrir conexión.
await using var pg = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql("Host=127.0.0.1;Port=1;Database=OfflineNeverConnect;Username=offline").Options);
var assembly = pg.GetService<IMigrationsAssembly>();
var migration = assembly.CreateMigration(assembly.Migrations.Single(m => m.Key.EndsWith("_AddAgendaYCitas")).Value, pg.Database.ProviderName!);
var allowed = new[] { "DisponibilidadesPsicologo", "Citas" };
Check(migration.UpOperations.OfType<CreateTableOperation>().Select(t => t.Name).Order().SequenceEqual(allowed.Order()), "Solo dos tablas nuevas");
Check(migration.UpOperations.All(o => o is CreateTableOperation || o is CreateIndexOperation i && allowed.Contains(i.Table)), "Sin cambios a tablas previas");
Check(migration.DownOperations.Count == 2 && migration.DownOperations.All(o => o is DropTableOperation t && allowed.Contains(t.Name)), "Down limitado a nuevas tablas");
Check(migration.UpOperations.OfType<CreateTableOperation>().SelectMany(t => t.ForeignKeys).All(f => f.OnDelete == ReferentialAction.Restrict), "FK sin cascadas destructivas");
Check(migration.UpOperations.OfType<CreateIndexOperation>().Count(i => i.IsUnique && i.Filter == "\"Estado\" = 0") == 2, "Unicidad filtrada reservas");
var design = pg.GetService<IDesignTimeModel>().Model;
var snapshot = pg.GetService<IModelRuntimeInitializer>().Initialize(assembly.ModelSnapshot!.Model, designTime: true);
Check(!pg.GetService<IMigrationsModelDiffer>().HasDifferences(snapshot.GetRelationalModel(), design.GetRelationalModel()), "Snapshot sincronizado");
var servicioPg = new AgendaCitasService(pg, new AutorizacionClinicaService(pg), reloj);
var resumenMethod = typeof(AgendaCitasService).GetMethod("ResumenPsicologo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
var query = (IQueryable<CitaResumen>)resumenMethod.Invoke(servicioPg, new object[] { "psi" })!;
var sql = query.Where(c => c.Estado == EstadoCita.Completada && c.Nombre.ToLower().Contains("est")).OrderByDescending(c => c.InicioUtc).Skip(20).Take(20).ToQueryString();
Check(sql.Contains("LIMIT") && sql.Contains("PsicologoUsuarioId") && sql.Contains("WHERE"), "Filtros e historial traducidos a PostgreSQL");
reloj.Ahora = new DateTimeOffset(2030, 1, 7, 12, 0, 0, TimeSpan.Zero);
checks += await HttpSmoke.Run(db, reloj);
Console.WriteLine($"OK: {checks} comprobaciones Agenda offline. PostgreSQL no fue abierto ni modificado. Concurrencia real PostgreSQL pendiente de validación en entorno de pruebas.");

sealed class Reloj : TimeProvider
{
    public DateTimeOffset Ahora { get; set; }
    public override DateTimeOffset GetUtcNow() => Ahora;
}
sealed class Transacciones : DbTransactionInterceptor
{
    public List<IsolationLevel> Aislamientos { get; } = [];
    public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData data, DbTransaction result, CancellationToken cancellationToken = default)
    { Aislamientos.Add(result.IsolationLevel); return ValueTask.FromResult(result); }
}
sealed class OfflineDb(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // SQLite no admite regex PostgreSQL en tablas previas. Se mantienen checks de Agenda.
        foreach (var entity in builder.Model.GetEntityTypes())
            foreach (var constraint in entity.GetCheckConstraints().Where(c => c.Name != null && !c.Name.StartsWith("CK_Citas_") && !c.Name.StartsWith("CK_Disponibilidad_")).ToArray())
                entity.RemoveCheckConstraint(constraint.Name!);
    }
}
