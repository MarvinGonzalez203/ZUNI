using Microsoft.AspNetCore.DataProtection;using Microsoft.AspNetCore.Identity;using Microsoft.EntityFrameworkCore;using Npgsql;using Zuni.Data;using Zuni.Models;using Zuni.Seguridad;using System.Text.Json;
internal static class RestoreFixture
{
 public static async Task Run(bool create)
 {
  var cs=Environment.GetEnvironmentVariable("DENIS_FIXTURE_CONNECTION")!;var cfg=new NpgsqlConnectionStringBuilder(cs);
  if(cfg.Host!="127.0.0.1"||!cfg.Database!.StartsWith("zuni_seguridad_prueba_restore1418_"))throw new Exception("Disposable restore fixture only.");
  var dir=Environment.GetEnvironmentVariable("DENIS_FIXTURE_DIRECTORY")??throw new Exception("Set the private fixture directory.");
  var keyFile=Path.Combine(dir,"keys-fixture.json");if(create)ProteccionPersistencia.Inicializar(keyFile);
  var protection=ProteccionPersistencia.Cargar(keyFile,"Zuni");
  await using var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(cs).Options,protection);
  var hasher=new PasswordHasher<ApplicationUser>();const string password="Only-a-fictitious-password!123";
  var id=Guid.Parse("27a7a25b-ad60-4c1d-8035-60820f3b80f8");
  if(create)
  {
   if(!await db.Database.EnsureCreatedAsync())throw new Exception("Refusing to seed an existing database.");
   var u=new ApplicationUser{Id="recovery-fake-user",UserName="fixture@example.test",Email="fixture@example.test",FullName="Persona ficticia Álvarez",SecurityStamp=Guid.NewGuid().ToString()};u.PasswordHash=hasher.HashPassword(u,password);db.Users.Add(u);
   db.PerfilesEstudiante.Add(new PerfilEstudiante{Id=id,UsuarioId=u.Id,Carne="9999-26-123456",Carrera="Solo laboratorio",Telefono="12345678"});await db.SaveChangesAsync();
   var dp=DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(dir,"data-protection")),b=>b.SetApplicationName("Zuni"));
   File.WriteAllText(Path.Combine(dir,"protected.json"),JsonSerializer.Serialize(new[]{protection.Cifrar("Nota ficticia de recuperación","plataforma"),protection.Firmar("evidencia ficticia"),dp.CreateProtector("Denis.RestoreFixture").Protect("sesión ficticia")}));
   Console.WriteLine("Created one fictional user/profile and encrypted recovery artifacts. No real records.");return;
  }
  var checks=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
  Check(!await db.Database.EnsureCreatedAsync(),"restored schema preserved");
  Check(await db.Users.CountAsync()==1&&await db.PerfilesEstudiante.CountAsync()==1,"counts preserved");var user=await db.Users.SingleAsync();
  Check(user.Id=="recovery-fake-user"&&user.FullName=="Persona ficticia Álvarez","identity and Unicode preserved");Check(hasher.VerifyHashedPassword(user,user.PasswordHash!,password)!=PasswordVerificationResult.Failed,"login hash restored");
  var profile=await db.PerfilesEstudiante.SingleAsync();Check(profile.Id==id&&profile.UsuarioId==user.Id&&profile.Carne=="9999-26-123456","relations preserved");
  var values=JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(dir,"protected.json")))!;
  Check(protection.Descifrar(values[0],"plataforma")=="Nota ficticia de recuperación","separately restored clinical key decrypts content");Check(protection.Verificar("evidencia ficticia",values[1]),"integrity signing key restored");
  var restoredDp=DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(dir,"data-protection")),b=>b.SetApplicationName("Zuni"));
  Check(restoredDp.CreateProtector("Denis.RestoreFixture").Unprotect(values[2])=="sesión ficticia","Data Protection keys restored");
  profile.Telefono="87654321";await db.SaveChangesAsync();db.ChangeTracker.Clear();Check((await db.PerfilesEstudiante.SingleAsync()).Telefono=="87654321","restored database writable without duplicating records");
  Console.WriteLine($"OK: {checks} PostgreSQL recovery checks after encrypted dump and separate-key restore. Fictional data only.");
 }
}
