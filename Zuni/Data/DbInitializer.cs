using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Zuni.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            string[] roles =
            {
                "Administrador",
                "Director",
                "Psicologo",
                "Catedratico",
                "Estudiante"
            };

            foreach (var roleName in roles)
            {
                var normalizedName = roleName.ToUpperInvariant();

                var exists = await context.Roles
                    .AnyAsync(r => r.NormalizedName == normalizedName);

                if (!exists)
                {
                    context.Roles.Add(new IdentityRole
                    {
                        Name = roleName,
                        NormalizedName = normalizedName
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}