using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Zuni.Data;

/// <summary>
/// Configura el contexto cuando Entity Framework ejecuta comandos de diseño
/// desde Visual Studio o desde la CLI.
/// </summary>
public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var userSecretsId = typeof(ApplicationDbContext).Assembly
            .GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;

        if (string.IsNullOrWhiteSpace(userSecretsId))
        {
            throw new InvalidOperationException(
                "No se encontró UserSecretsId para configurar la conexión de diseño.");
        }

        var secretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "UserSecrets",
            userSecretsId,
            "secrets.json");

        string? connectionString = null;
        if (File.Exists(secretsPath))
        {
            using var secrets = JsonDocument.Parse(File.ReadAllText(secretsPath));
            var root = secrets.RootElement;

            if (root.TryGetProperty(
                    "ConnectionStrings:DefaultConnection",
                    out var flattenedConnectionString))
            {
                connectionString = flattenedConnectionString.GetString();
            }
            else if (root.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
                     connectionStrings.TryGetProperty("DefaultConnection", out var nestedConnectionString))
            {
                connectionString = nestedConnectionString.GetString();
            }
        }

        connectionString ??= Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta ConnectionStrings:DefaultConnection en User Secrets o en las variables de entorno.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
