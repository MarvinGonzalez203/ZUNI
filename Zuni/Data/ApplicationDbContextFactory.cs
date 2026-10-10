using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Zuni.Data;
// Herramientas EF: misma configuración que Program, sin ejecutar Program ni SeedRolesAsync.
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // No inferir Production ni depender del perfil de lanzamiento de Visual Studio.
        var explicitEnvironment = args.Any(a =>
            a.Equals("--environment", StringComparison.OrdinalIgnoreCase) ||
            a.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase));
        if (!explicitEnvironment)
            throw new InvalidOperationException(
                "Indica explícitamente --environment y --contentRoot del proyecto Zuni al ejecutar herramientas EF.");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(ApplicationDbContextFactory).Assembly.GetName().Name
        });
        if (!File.Exists(Path.Combine(builder.Environment.ContentRootPath, "Zuni.csproj")))
            throw new InvalidOperationException("El contentRoot debe ser la carpeta que contiene Zuni.csproj.");

        // appsettings + appsettings del entorno + User Secrets en Development
        // + variables de entorno + argumentos: mismos proveedores y precedencia que ZUNI.
        var connection = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection para el entorno indicado.");

        // Crear opciones no abre conexiones. Solo una orden de actualización autorizada lo hará.
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection).Options);
    }
}
