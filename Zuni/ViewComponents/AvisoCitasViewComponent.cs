using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
namespace Zuni.ViewComponents;
public sealed class AvisoCitasViewComponent(ApplicationDbContext db):ViewComponent
{public async Task<IViewComponentResult> InvokeAsync(){var id=HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);var count=await db.NotificacionesCita.AsNoTracking().CountAsync(n=>n.UsuarioId==id&&!n.Leida);return View(count);}}
