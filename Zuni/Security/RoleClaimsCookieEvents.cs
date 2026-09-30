using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;

namespace Zuni.Security;

public sealed class RoleClaimsCookieEvents(ApplicationDbContext db)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var identity = principal?.Identity as ClaimsIdentity;
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (identity is null || string.IsNullOrWhiteSpace(userId))
        {
            await RechazarSesionAsync(context);
            return;
        }

        var usuarioActivo = await db.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive);

        if (!usuarioActivo)
        {
            await RechazarSesionAsync(context);
            return;
        }

        var rolesActuales = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select role.Name)
            .Where(nombre => nombre != null)
            .ToListAsync();

        var rolesEnCookie = identity
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rolesEnBase = rolesActuales
            .Where(nombre => nombre != null)
            .Select(nombre => nombre!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (rolesEnCookie.SetEquals(rolesEnBase))
            return;

        var identidadActualizada = new ClaimsIdentity(identity);
        foreach (var claim in identidadActualizada
                     .FindAll(ClaimTypes.Role)
                     .ToArray())
        {
            identidadActualizada.RemoveClaim(claim);
        }

        foreach (var rol in rolesEnBase)
        {
            identidadActualizada.AddClaim(
                new Claim(ClaimTypes.Role, rol));
        }

        context.ReplacePrincipal(
            new ClaimsPrincipal(identidadActualizada));
        context.ShouldRenew = true;
    }

    private static async Task RechazarSesionAsync(
        CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
