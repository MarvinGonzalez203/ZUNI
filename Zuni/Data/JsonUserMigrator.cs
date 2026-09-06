using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;

namespace Zuni.Data
{
    public static class JsonUserMigrator
    {
        public static async Task<int> MigrateAsync(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            // Construir la ruta hacia users.json
            var jsonPath = Path.Combine(
                environment.ContentRootPath,
                "App_Data",
                "users.json");

            // Mensajes temporales para diagnóstico
            Debug.WriteLine($"Ruta JSON: {jsonPath}");
            Debug.WriteLine($"Existe users.json: {File.Exists(jsonPath)}");

            // Si el archivo no existe, no intentar migrar
            if (!File.Exists(jsonPath))
            {
                Debug.WriteLine("No se encontró users.json.");
                return 0;
            }

            // Leer el contenido del archivo JSON
            var json = await File.ReadAllTextAsync(jsonPath);

            // Convertir el JSON a una lista de AppUser
            var oldUsers = JsonSerializer.Deserialize<List<AppUser>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<AppUser>();

            Debug.WriteLine(
                $"Usuarios encontrados en JSON: {oldUsers.Count}");

            // Buscar el rol Estudiante
            var estudianteRole = await context.Roles
                .FirstAsync(r => r.NormalizedName == "ESTUDIANTE");

            int migratedUsers = 0;

            foreach (var oldUser in oldUsers)
            {
                // El Id antiguo es Guid y Identity utiliza string
                var oldUserId = oldUser.Id.ToString();

                // Normalizar correo
                var email = oldUser.Email.Trim();
                var normalizedEmail = email.ToUpperInvariant();

                // Comprobar que el usuario todavía no exista
                var alreadyExists = await context.Users.AnyAsync(u =>
                    u.Id == oldUserId ||
                    u.NormalizedEmail == normalizedEmail);

                if (alreadyExists)
                {
                    Debug.WriteLine(
                        $"Usuario omitido porque ya existe. Id: {oldUserId}");

                    continue;
                }

                // Crear el nuevo usuario de Identity
                var newUser = new ApplicationUser
                {
                    Id = oldUserId,

                    FullName = oldUser.FullName,

                    Email = email,
                    NormalizedEmail = normalizedEmail,

                    UserName = email,
                    NormalizedUserName = normalizedEmail,

                    // Conservar el hash de contraseña existente
                    PasswordHash = oldUser.PasswordHash,

                    CreatedAtUtc = oldUser.CreatedAtUtc,
                    IsActive = true,

                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),

                    EmailConfirmed = false
                };

                // Agregar el usuario
                context.Users.Add(newUser);

                // Asignar inicialmente el rol Estudiante
                context.UserRoles.Add(new IdentityUserRole<string>
                {
                    UserId = newUser.Id,
                    RoleId = estudianteRole.Id
                });

                migratedUsers++;
            }

            // Guardar todos los cambios
            await context.SaveChangesAsync();

            Debug.WriteLine(
                $"Usuarios migrados desde JSON: {migratedUsers}");

            return migratedUsers;
        }
    }
}