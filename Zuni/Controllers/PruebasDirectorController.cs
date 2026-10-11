using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Pruebas;

namespace Zuni.Controllers;

[Authorize(Roles = "Director")]
[AutoValidateAntiforgeryToken]
public sealed class PruebasDirectorController(ApplicationDbContext db) : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewData["DashboardName"] = await db.Users.AsNoTracking().Where(x => x.Id == usuarioId)
            .Select(x => x.FullName).SingleOrDefaultAsync() ?? "Director";
        await next();
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await db.PruebasDirector.AsNoTracking()
        .OrderByDescending(x => x.CreadaUtc).ToListAsync());

    [HttpGet]
    public IActionResult Crear() => View("Editar", new EditarPruebaViewModel());

    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id)
    {
        var prueba = await Cargar(id, false);
        return prueba is null ? NotFound() : View(new DetallePruebaViewModel(prueba,
            await db.AsignacionesPrueba.CountAsync(x => x.PruebaId == id)));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id)
    {
        var p = await Cargar(id, false);
        if (p is null) return NotFound();
        if (p.Activa || await db.AsignacionesPrueba.AnyAsync(x => x.PruebaId == id))
            return Aviso(id, "Solo se pueden editar pruebas inactivas sin asignaciones. Para un instrumento ya asignado, crea una nueva prueba.");
        return View(new EditarPruebaViewModel
        {
            Id = p.Id, Revision = p.Revision, Nombre = p.Nombre, Descripcion = p.Descripcion,
            Instrucciones = p.Instrucciones, Fuente = p.Fuente, Idioma = p.Idioma,
            Dimensiones = p.Dimensiones.Select(x => new DimensionInput { Nombre = x.Nombre }).ToList(),
            Preguntas = p.Preguntas.OrderBy(x => x.Orden).Select(x => new PreguntaInput
            {
                Texto = x.Texto, Dimension = p.Dimensiones.Single(d => d.Id == x.DimensionId).Nombre,
                Opciones = x.Opciones.OrderBy(o => o.Orden).Select(o => new OpcionInput { Texto = o.Texto, Valor = o.Valor }).ToList()
            }).ToList()
        });
    }

    [HttpPost]
    [RequestFormLimits(ValueCountLimit = 10000)]
    public async Task<IActionResult> Guardar(EditarPruebaViewModel model)
    {
        Validar(model);
        if (!ModelState.IsValid) return View("Editar", model);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var p = model.Id.HasValue ? await Cargar(model.Id.Value, true) : new Prueba();
        if (p is null) return NotFound();
        if (model.Id.HasValue)
        {
            if (p.Activa || await db.AsignacionesPrueba.AnyAsync(x => x.PruebaId == p.Id))
                return Aviso(p.Id, "La prueba está activa o ya fue asignada; su contenido no se puede modificar.");
            db.Entry(p).Property(x => x.Revision).OriginalValue = model.Revision;
            db.RemoveRange(p.Preguntas);
            db.RemoveRange(p.Dimensiones);
            p.Preguntas = [];
            p.Dimensiones = [];
        }
        else db.PruebasDirector.Add(p);
        p.Nombre = model.Nombre.Trim(); p.Descripcion = model.Descripcion.Trim();
        p.Instrucciones = model.Instrucciones.Trim(); p.Fuente = model.Fuente.Trim(); p.Idioma = model.Idioma.Trim();
        p.Revision = Guid.NewGuid();
        try
        {
            // Eliminar la estructura anterior antes de reutilizar nombres y posiciones únicos.
            await db.SaveChangesAsync();
            p.Dimensiones = model.Dimensiones.Select(x => new DimensionPrueba { PruebaId = p.Id, Nombre = x.Nombre.Trim() }).ToList();
            p.Preguntas = model.Preguntas.Select((x, i) => new PreguntaPrueba
            {
                PruebaId = p.Id, Orden = i + 1, Texto = x.Texto.Trim(),
                DimensionId = p.Dimensiones.Single(d => string.Equals(d.Nombre, x.Dimension.Trim(), StringComparison.OrdinalIgnoreCase)).Id,
                Opciones = x.Opciones.Select((o, j) => new OpcionPrueba { Orden = j + 1, Texto = o.Texto.Trim(), Valor = o.Valor }).ToList()
            }).ToList();
            db.AddRange(p.Dimensiones);
            db.AddRange(p.Preguntas);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Aviso(p.Id, "Prueba guardada como borrador.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Aviso(p.Id, "Otra persona modificó esta prueba. Vuelve a abrirla antes de guardar.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Estado(Guid id, Guid revision, bool activar)
    {
        var p = await Cargar(id, true);
        if (p is null) return NotFound();
        if (activar && (p.Preguntas.Count == 0 || p.Preguntas.Any(x => x.Opciones.Count < 2)))
            return Aviso(id, "Para activar, agrega al menos una pregunta y dos opciones por pregunta.");
        db.Entry(p).Property(x => x.Revision).OriginalValue = revision;
        p.Activa = activar; p.Revision = Guid.NewGuid();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Aviso(id, "La prueba cambió. Recarga su detalle."); }
        return Aviso(id, activar ? "Prueba activada para asignación." : "Prueba desactivada; no admite nuevas asignaciones.");
    }

    [HttpGet]
    public async Task<IActionResult> Asignar(Guid id)
    {
        var p = await db.PruebasDirector.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var model = new AsignarPruebaViewModel { Id = id, Revision = p.Revision, Nombre = p.Nombre };
        await PrepararAsignacion(model);
        ViewData["Activa"] = p.Activa;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Asignar(AsignarPruebaViewModel model)
    {
        var p = await db.PruebasDirector.SingleOrDefaultAsync(x => x.Id == model.Id);
        if (p is null) return NotFound();
        if (!p.Activa) return Aviso(p.Id, "Activa la prueba antes de asignarla.");
        if (!await EstudiantesElegibles().AnyAsync(x => x.Id == model.PerfilEstudianteId))
            ModelState.AddModelError(nameof(model.PerfilEstudianteId), "Selecciona un estudiante activo sin esta asignación.");
        if (!ModelState.IsValid)
        {
            model.Nombre = p.Nombre;
            await PrepararAsignacion(model);
            ViewData["Activa"] = p.Activa;
            return View(model);
        }
        // La revisión compartida evita asignar simultáneamente con una edición o desactivación.
        db.Entry(p).Property(x => x.Revision).OriginalValue = model.Revision;
        p.Revision = Guid.NewGuid();
        db.AsignacionesPrueba.Add(new AsignacionPrueba
        {
            PruebaId = p.Id, PerfilEstudianteId = model.PerfilEstudianteId!.Value,
            DirectorUsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            TempData["PruebasMensaje"] = "No se pudo registrar la asignación; recarga y verifica si la prueba o el estudiante cambiaron.";
            return RedirectToAction(nameof(Asignar), new { id = p.Id });
        }
        TempData["PruebasMensaje"] = "Asignación registrada. La aplicación y las respuestas del instrumento se integrarán en el módulo del estudiante.";
        return RedirectToAction(nameof(Asignar), new { id = p.Id });

        IQueryable<Zuni.Models.PerfilEstudiante> EstudiantesElegibles() => Elegibles(model.Id);
    }

    private IQueryable<Zuni.Models.PerfilEstudiante> Elegibles(Guid pruebaId) => db.PerfilesEstudiante
        .Where(x => x.Activo && x.Usuario.IsActive
            && db.UserRoles.Any(ur => ur.UserId == x.UsuarioId && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Estudiante"))
            && !db.AsignacionesPrueba.Any(a => a.PruebaId == pruebaId && a.PerfilEstudianteId == x.Id));

    private async Task PrepararAsignacion(AsignarPruebaViewModel model)
    {
        model.Estudiantes = await Elegibles(model.Id).OrderBy(x => x.Usuario.FullName)
            .Select(x => new SelectListItem(x.Usuario.FullName + " · " + x.Carne, x.Id.ToString())).ToListAsync();
        model.Asignaciones = await (from a in db.AsignacionesPrueba.AsNoTracking()
            join e in db.PerfilesEstudiante on a.PerfilEstudianteId equals e.Id
            where a.PruebaId == model.Id orderby a.AsignadaUtc descending
            select new AsignacionResumen(e.Usuario.FullName, e.Carne, a.AsignadaUtc)).ToListAsync();
    }

    private Task<Prueba?> Cargar(Guid id, bool tracking)
    {
        IQueryable<Prueba> query = db.PruebasDirector.Include(x => x.Dimensiones).Include(x => x.Preguntas).ThenInclude(x => x.Opciones).AsSplitQuery();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id);
    }
    private IActionResult Aviso(Guid id, string mensaje)
    {
        TempData["PruebasMensaje"] = mensaje;
        return RedirectToAction(nameof(Detalle), new { id });
    }
    private void Validar(EditarPruebaViewModel model)
    {
        model.Dimensiones ??= []; model.Preguntas ??= [];
        foreach (var pregunta in model.Preguntas) pregunta.Opciones ??= [];
        if (model.Dimensiones.Count > 30 || model.Preguntas.Count > 200 || model.Preguntas.Any(x => x.Opciones.Count > 20))
            ModelState.AddModelError("", "Máximo: 30 dimensiones, 200 preguntas y 20 opciones por pregunta.");
        var nombres = model.Dimensiones.Select(x => x.Nombre?.Trim() ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (nombres.Count != model.Dimensiones.Count) ModelState.AddModelError("", "Los nombres de las dimensiones no deben repetirse.");
        foreach (var pregunta in model.Preguntas)
        {
            if (!nombres.Contains(pregunta.Dimension?.Trim() ?? "")) ModelState.AddModelError("", "Cada pregunta debe pertenecer a una dimensión de esta prueba.");
            if (pregunta.Opciones.Count < 2) ModelState.AddModelError("", "Cada pregunta necesita al menos dos opciones.");
            if (pregunta.Opciones.Select(x => x.Texto?.Trim() ?? "").Distinct(StringComparer.OrdinalIgnoreCase).Count() != pregunta.Opciones.Count)
                ModelState.AddModelError("", "Las opciones de una pregunta no deben repetirse.");
        }
    }
}
