using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers;

[Authorize(Roles = "Director")]
public sealed class DirectorController(ApplicationDbContext db) : Controller
{
    private static readonly string[] RolesAsignables =
    [
        "Psicologo",
        "Catedratico",
        "Estudiante"
    ];

    private static readonly string[] RolesAsignablesNormalizados =
        RolesAsignables
            .Select(nombre => nombre.ToUpperInvariant())
            .ToArray();

    private static readonly HashSet<string> RolesProtegidos =
    [
        "ADMINISTRADOR",
        "DIRECTOR"
    ];

    public async Task<IActionResult> Index()
    {
        var estudianteIds =
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where role.NormalizedName == "ESTUDIANTE"
            select userRole.UserId;

        var estudiantesRegistrados = await estudianteIds.CountAsync();

        var perfilesCompletos = await db.PerfilesEstudiante
            .AsNoTracking()
            .Where(perfil =>
                estudianteIds.Contains(perfil.UsuarioId) &&
                !string.IsNullOrWhiteSpace(perfil.Carne) &&
                !string.IsNullOrWhiteSpace(perfil.Telefono) &&
                !string.IsNullOrWhiteSpace(perfil.Carrera))
            .CountAsync();

        var model = new DirectorDashboardViewModel
        {
            EstudiantesRegistrados = estudiantesRegistrados,
            PerfilesCompletos = perfilesCompletos,
            PerfilesIncompletos = Math.Max(
                0,
                estudiantesRegistrados - perfilesCompletos)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Pruebas(string? buscar)
    {
        var consulta = db.Pruebas.AsNoTracking();
        var termino = buscar?.Trim();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(prueba =>
                prueba.Nombre.Contains(termino) ||
                (prueba.Descripcion != null && prueba.Descripcion.Contains(termino)));
        }

        var model = new PruebasViewModel
        {
            Buscar = termino,
            Pruebas = await consulta
                .OrderByDescending(prueba => prueba.Activa)
                .ThenBy(prueba => prueba.Nombre)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult CrearPrueba() => View(new PruebaFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPrueba(PruebaFormViewModel model)
    {
        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion)
            ? null
            : model.Descripcion.Trim();

        if (!ModelState.IsValid)
            return View(model);

        var prueba = new Prueba
        {
            Id = Guid.NewGuid(),
            Nombre = model.Nombre,
            Descripcion = model.Descripcion,
            Activa = false,
            FechaCreacionUtc = DateTime.UtcNow
        };

        db.Pruebas.Add(prueba);
        await db.SaveChangesAsync();
        TempData["Success"] = "La prueba se creó como borrador. Revisa su alcance exploratorio antes de habilitarla en el catálogo.";
        return RedirectToAction(nameof(Pruebas));
    }

    [HttpGet]
    public async Task<IActionResult> EditarPrueba(Guid id)
    {
        var prueba = await db.Pruebas.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (prueba is null)
            return NotFound();

        return View(new PruebaFormViewModel
        {
            Id = prueba.Id,
            Nombre = prueba.Nombre,
            Descripcion = prueba.Descripcion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPrueba(PruebaFormViewModel model)
    {
        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion)
            ? null
            : model.Descripcion.Trim();

        if (!ModelState.IsValid)
            return View(model);

        var prueba = await db.Pruebas
            .SingleOrDefaultAsync(item => item.Id == model.Id);
        if (prueba is null)
            return NotFound();

        prueba.Nombre = model.Nombre;
        prueba.Descripcion = model.Descripcion;
        prueba.FechaActualizacionUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Success"] = "Los datos de la prueba se actualizaron.";
        return RedirectToAction(nameof(Pruebas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoPrueba(Guid id)
    {
        var prueba = await db.Pruebas
            .SingleOrDefaultAsync(item => item.Id == id);
        if (prueba is null)
            return NotFound();

        prueba.Activa = !prueba.Activa;
        prueba.FechaActualizacionUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Success"] = prueba.Activa
            ? "El cuestionario quedó habilitado para asignarse."
            : "El cuestionario se archivó y se conservó junto con su contenido.";
        return RedirectToAction(nameof(Pruebas));
    }

    [HttpGet]
    public async Task<IActionResult> AsignacionesPrueba()
    {
        var asignaciones = await (
            from asignacion in db.AsignacionesPrueba.AsNoTracking()
            join prueba in db.Pruebas.AsNoTracking()
                on asignacion.PruebaId equals prueba.Id
            join estudiante in db.Users.AsNoTracking()
                on asignacion.EstudianteId equals estudiante.Id
            orderby asignacion.Cancelada, asignacion.FechaAsignacionUtc descending
            select new AsignacionPruebaListadoItem
            {
                Id = asignacion.Id,
                PruebaNombre = prueba.Nombre,
                EstudianteNombre = estudiante.FullName,
                EstudianteCorreo = estudiante.Email ?? string.Empty,
                Modalidad = asignacion.Modalidad,
                FechaAsignacionUtc = asignacion.FechaAsignacionUtc,
                Cancelada = asignacion.Cancelada
            })
            .ToListAsync();

        return View(new AsignacionesPruebaViewModel { Asignaciones = asignaciones });
    }

    [HttpGet]
    public async Task<IActionResult> AsignarPrueba(Guid? pruebaId)
    {
        var model = await CrearModeloAsignacionAsync();
        if (pruebaId.HasValue && model.PruebasDisponibles.Any(item => item.Value == pruebaId.Value.ToString()))
            model.PruebaId = pruebaId.Value;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarPrueba(AsignarPruebaViewModel model)
    {
        if (!Enum.IsDefined(typeof(ModalidadPrueba), model.Modalidad))
            ModelState.AddModelError(nameof(model.Modalidad), "Selecciona una modalidad válida.");

        var prueba = await db.Pruebas.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == model.PruebaId && item.Activa);
        if (prueba is null)
            ModelState.AddModelError(nameof(model.PruebaId), "El cuestionario no está habilitado para asignación.");

        var estudianteEsValido = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where userRole.UserId == model.EstudianteId && role.NormalizedName == "ESTUDIANTE"
            select userRole.UserId)
            .AnyAsync();
        if (!estudianteEsValido)
            ModelState.AddModelError(nameof(model.EstudianteId), "Selecciona una cuenta con rol Estudiante.");

        var modalidadValida = Enum.IsDefined(typeof(ModalidadPrueba), model.Modalidad);
        var preguntaCount = prueba is null || !modalidadValida
            ? 0
            : await db.PreguntasPrueba.CountAsync(pregunta =>
                pregunta.Dimension.PruebaId == prueba.Id &&
                pregunta.Dimension.Activa &&
                pregunta.Activa);
        var preguntasRequeridas = model.Modalidad switch
        {
            ModalidadPrueba.Rapida => 15,
            ModalidadPrueba.Media => 40,
            ModalidadPrueba.Avanzada => 60,
            _ => int.MaxValue
        };
        if (prueba is not null && modalidadValida && preguntaCount < preguntasRequeridas)
        {
            var modalidadTexto = model.Modalidad switch
            {
                ModalidadPrueba.Rapida => "rápida (15)",
                ModalidadPrueba.Media => "media (40)",
                ModalidadPrueba.Avanzada => "avanzada (60 o más)",
                _ => "seleccionada"
            };
            ModelState.AddModelError(string.Empty,
                $"El cuestionario tiene {preguntaCount} preguntas activas. La modalidad {modalidadTexto} requiere al menos {preguntasRequeridas}.");
        }

        var duplicada = await db.AsignacionesPrueba.AnyAsync(asignacion =>
            asignacion.PruebaId == model.PruebaId &&
            asignacion.EstudianteId == model.EstudianteId &&
            !asignacion.Cancelada);
        if (duplicada)
            ModelState.AddModelError(string.Empty, "Este cuestionario ya tiene una asignación activa para ese estudiante.");

        if (!ModelState.IsValid)
        {
            model = await CompletarOpcionesAsignacionAsync(model);
            return View(model);
        }

        var asignadaPorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(asignadaPorId))
            return Forbid();

        db.AsignacionesPrueba.Add(new AsignacionPrueba
        {
            Id = Guid.NewGuid(),
            PruebaId = model.PruebaId,
            EstudianteId = model.EstudianteId,
            AsignadaPorId = asignadaPorId,
            Modalidad = model.Modalidad,
            FechaAsignacionUtc = DateTime.UtcNow,
            Cancelada = false
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "El cuestionario se asignó al estudiante. La asignación no contiene respuestas ni resultados.";
        return RedirectToAction(nameof(AsignacionesPrueba));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarAsignacionPrueba(Guid id)
    {
        var asignacion = await db.AsignacionesPrueba
            .SingleOrDefaultAsync(item => item.Id == id && !item.Cancelada);
        if (asignacion is null)
            return NotFound();

        asignacion.Cancelada = true;
        await db.SaveChangesAsync();
        TempData["Success"] = "La asignación se canceló; se conserva el registro administrativo.";
        return RedirectToAction(nameof(AsignacionesPrueba));
    }

    private async Task<AsignarPruebaViewModel> CrearModeloAsignacionAsync() =>
        await CompletarOpcionesAsignacionAsync(new AsignarPruebaViewModel());

    private async Task<AsignarPruebaViewModel> CompletarOpcionesAsignacionAsync(
        AsignarPruebaViewModel model)
    {
        model.PruebasDisponibles = await db.Pruebas.AsNoTracking()
            .Where(prueba => prueba.Activa)
            .OrderBy(prueba => prueba.Nombre)
            .Select(prueba => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = prueba.Id.ToString(),
                Text = prueba.Nombre
            })
            .ToListAsync();

        model.Estudiantes = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            join estudiante in db.Users.AsNoTracking()
                on userRole.UserId equals estudiante.Id
            where role.NormalizedName == "ESTUDIANTE"
            orderby estudiante.FullName, estudiante.Email
            select new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = estudiante.Id,
                Text = estudiante.FullName + " · " + (estudiante.Email ?? "Sin correo")
            })
            .ToListAsync();

        return model;
    }

    [HttpGet]
    public async Task<IActionResult> Dimensiones(Guid pruebaId)
    {
        var prueba = await db.Pruebas.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == pruebaId);
        if (prueba is null)
            return NotFound();

        var dimensiones = await db.DimensionesPrueba
            .AsNoTracking()
            .Include(dimension => dimension.Preguntas)
            .Where(dimension => dimension.PruebaId == pruebaId)
            .OrderBy(dimension => dimension.Orden)
            .ThenBy(dimension => dimension.Nombre)
            .ToListAsync();

        return View(new DimensionesPruebaViewModel
        {
            PruebaId = prueba.Id,
            PruebaNombre = prueba.Nombre,
            Dimensiones = dimensiones
        });
    }

    [HttpGet]
    public async Task<IActionResult> CrearDimension(Guid pruebaId)
    {
        var pruebaExiste = await db.Pruebas.AsNoTracking()
            .AnyAsync(item => item.Id == pruebaId);
        if (!pruebaExiste)
            return NotFound();

        var orden = await db.DimensionesPrueba
            .Where(dimension => dimension.PruebaId == pruebaId)
            .CountAsync() + 1;

        return View(new DimensionPruebaFormViewModel
        {
            PruebaId = pruebaId,
            Orden = orden
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearDimension(DimensionPruebaFormViewModel model)
    {
        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion)
            ? null
            : model.Descripcion.Trim();

        if (!ModelState.IsValid)
            return View(model);

        var pruebaExiste = await db.Pruebas
            .AnyAsync(item => item.Id == model.PruebaId);
        if (!pruebaExiste)
            return NotFound();

        db.DimensionesPrueba.Add(new DimensionPrueba
        {
            Id = Guid.NewGuid(),
            PruebaId = model.PruebaId,
            Nombre = model.Nombre,
            Descripcion = model.Descripcion,
            Orden = model.Orden,
            Activa = true
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "La dimensión se creó correctamente.";
        return RedirectToAction(nameof(Dimensiones), new { pruebaId = model.PruebaId });
    }

    [HttpGet]
    public async Task<IActionResult> EditarDimension(Guid id)
    {
        var dimension = await db.DimensionesPrueba.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (dimension is null)
            return NotFound();

        return View(new DimensionPruebaFormViewModel
        {
            Id = dimension.Id,
            PruebaId = dimension.PruebaId,
            Nombre = dimension.Nombre,
            Descripcion = dimension.Descripcion,
            Orden = dimension.Orden
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarDimension(DimensionPruebaFormViewModel model)
    {
        model.Nombre = model.Nombre?.Trim() ?? string.Empty;
        model.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion)
            ? null
            : model.Descripcion.Trim();

        if (!ModelState.IsValid)
            return View(model);

        var dimension = await db.DimensionesPrueba
            .SingleOrDefaultAsync(item => item.Id == model.Id);
        if (dimension is null)
            return NotFound();

        dimension.Nombre = model.Nombre;
        dimension.Descripcion = model.Descripcion;
        dimension.Orden = model.Orden;
        await db.SaveChangesAsync();

        TempData["Success"] = "La dimensión se actualizó correctamente.";
        return RedirectToAction(nameof(Dimensiones), new { pruebaId = dimension.PruebaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoDimension(Guid id)
    {
        var dimension = await db.DimensionesPrueba
            .Include(item => item.Preguntas)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (dimension is null)
            return NotFound();

        dimension.Activa = !dimension.Activa;
        if (!dimension.Activa)
        {
            foreach (var pregunta in dimension.Preguntas)
                pregunta.Activa = false;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = dimension.Activa
            ? "La dimensión quedó activa. Sus preguntas siguen en el estado que tenían."
            : "La dimensión y sus preguntas se desactivaron; su contenido se conserva.";
        return RedirectToAction(nameof(Dimensiones), new { pruebaId = dimension.PruebaId });
    }

    [HttpGet]
    public async Task<IActionResult> Preguntas(Guid dimensionId)
    {
        var dimension = await db.DimensionesPrueba.AsNoTracking()
            .Include(item => item.Prueba)
            .SingleOrDefaultAsync(item => item.Id == dimensionId);
        if (dimension is null)
            return NotFound();

        var preguntas = await db.PreguntasPrueba
            .AsNoTracking()
            .Include(pregunta => pregunta.Opciones)
            .Where(pregunta => pregunta.DimensionPruebaId == dimensionId)
            .OrderBy(pregunta => pregunta.Orden)
            .ThenBy(pregunta => pregunta.Texto)
            .ToListAsync();

        return View(new PreguntasPruebaViewModel
        {
            PruebaId = dimension.PruebaId,
            PruebaNombre = dimension.Prueba.Nombre,
            DimensionId = dimension.Id,
            DimensionNombre = dimension.Nombre,
            Preguntas = preguntas
        });
    }

    [HttpGet]
    public async Task<IActionResult> CrearPregunta(Guid dimensionId)
    {
        var dimension = await db.DimensionesPrueba.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == dimensionId);
        if (dimension is null)
            return NotFound();

        var orden = await db.PreguntasPrueba
            .Where(pregunta => pregunta.DimensionPruebaId == dimensionId)
            .CountAsync() + 1;

        return View(new PreguntaPruebaFormViewModel
        {
            DimensionId = dimensionId,
            Orden = orden
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPregunta(PreguntaPruebaFormViewModel model)
    {
        model.Texto = model.Texto?.Trim() ?? string.Empty;
        model.PropositoExploratorio = model.PropositoExploratorio?.Trim() ?? string.Empty;
        if (!ModelState.IsValid)
            return View(model);

        var dimensionExiste = await db.DimensionesPrueba
            .AnyAsync(dimension => dimension.Id == model.DimensionId);
        if (!dimensionExiste)
            return NotFound();

        var pregunta = new PreguntaPrueba
        {
            Id = Guid.NewGuid(),
            DimensionPruebaId = model.DimensionId,
            Texto = model.Texto,
            PropositoExploratorio = model.PropositoExploratorio,
            Orden = model.Orden,
            Activa = false
        };
        db.PreguntasPrueba.Add(pregunta);
        await db.SaveChangesAsync();

        TempData["Success"] = "La pregunta se creó como borrador. Añade opciones y completa su revisión antes de activarla.";
        return RedirectToAction(nameof(Opciones), new { preguntaId = pregunta.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditarPregunta(Guid id)
    {
        var pregunta = await db.PreguntasPrueba.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (pregunta is null)
            return NotFound();

        return View(new PreguntaPruebaFormViewModel
        {
            Id = pregunta.Id,
            DimensionId = pregunta.DimensionPruebaId,
            Texto = pregunta.Texto,
            PropositoExploratorio = pregunta.PropositoExploratorio ?? string.Empty,
            Orden = pregunta.Orden
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPregunta(PreguntaPruebaFormViewModel model)
    {
        model.Texto = model.Texto?.Trim() ?? string.Empty;
        model.PropositoExploratorio = model.PropositoExploratorio?.Trim() ?? string.Empty;
        if (!ModelState.IsValid)
            return View(model);

        var pregunta = await db.PreguntasPrueba
            .SingleOrDefaultAsync(item => item.Id == model.Id);
        if (pregunta is null)
            return NotFound();

        pregunta.Texto = model.Texto;
        pregunta.PropositoExploratorio = model.PropositoExploratorio;
        pregunta.Orden = model.Orden;
        await db.SaveChangesAsync();

        TempData["Success"] = "La pregunta se actualizó correctamente.";
        return RedirectToAction(nameof(Preguntas), new { dimensionId = pregunta.DimensionPruebaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoPregunta(Guid id)
    {
        var pregunta = await db.PreguntasPrueba
            .Include(item => item.Dimension)
            .Include(item => item.Opciones)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (pregunta is null)
            return NotFound();

        if (!pregunta.Activa)
        {
            var opcionesActivas = pregunta.Opciones.Count(opcion => opcion.Activa);
            if (string.IsNullOrWhiteSpace(pregunta.PropositoExploratorio))
            {
                TempData["Error"] = "Define el propósito exploratorio de la pregunta antes de activarla.";
                return RedirectToAction(nameof(EditarPregunta), new { id = pregunta.Id });
            }

            if (!pregunta.Dimension.Activa)
            {
                TempData["Error"] = "Activa primero la dimensión para habilitar internamente sus preguntas.";
                return RedirectToAction(nameof(Preguntas), new { dimensionId = pregunta.DimensionPruebaId });
            }

            if (opcionesActivas < 2)
            {
                TempData["Error"] = "Añade al menos dos opciones activas antes de activar esta pregunta.";
                return RedirectToAction(nameof(Opciones), new { preguntaId = pregunta.Id });
            }
        }

        pregunta.Activa = !pregunta.Activa;
        await db.SaveChangesAsync();
        TempData["Success"] = pregunta.Activa
            ? "La pregunta quedó habilitada en el catálogo interno; no se publicó ni asignó a estudiantes."
            : "La pregunta quedó en borrador; sus opciones se conservaron.";
        return RedirectToAction(nameof(Preguntas), new { dimensionId = pregunta.DimensionPruebaId });
    }

    [HttpGet]
    public async Task<IActionResult> Opciones(Guid preguntaId)
    {
        var pregunta = await db.PreguntasPrueba.AsNoTracking()
            .Include(item => item.Dimension)
                .ThenInclude(dimension => dimension.Prueba)
            .SingleOrDefaultAsync(item => item.Id == preguntaId);
        if (pregunta is null)
            return NotFound();

        var opciones = await db.OpcionesPreguntaPrueba
            .AsNoTracking()
            .Where(opcion => opcion.PreguntaPruebaId == preguntaId)
            .OrderBy(opcion => opcion.Orden)
            .ThenBy(opcion => opcion.Texto)
            .ToListAsync();

        return View(new OpcionesPreguntaViewModel
        {
            PruebaId = pregunta.Dimension.PruebaId,
            PruebaNombre = pregunta.Dimension.Prueba.Nombre,
            DimensionId = pregunta.DimensionPruebaId,
            DimensionNombre = pregunta.Dimension.Nombre,
            PreguntaId = pregunta.Id,
            PreguntaTexto = pregunta.Texto,
            Opciones = opciones
        });
    }

    [HttpGet]
    public async Task<IActionResult> CrearOpcion(Guid preguntaId)
    {
        var preguntaExiste = await db.PreguntasPrueba.AsNoTracking()
            .AnyAsync(item => item.Id == preguntaId);
        if (!preguntaExiste)
            return NotFound();

        var orden = await db.OpcionesPreguntaPrueba
            .Where(opcion => opcion.PreguntaPruebaId == preguntaId)
            .CountAsync() + 1;

        return View(new OpcionPreguntaFormViewModel
        {
            PreguntaId = preguntaId,
            Orden = orden
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearOpcion(OpcionPreguntaFormViewModel model)
    {
        model.Texto = model.Texto?.Trim() ?? string.Empty;
        if (!ModelState.IsValid)
            return View(model);

        var preguntaExiste = await db.PreguntasPrueba
            .AnyAsync(item => item.Id == model.PreguntaId);
        if (!preguntaExiste)
            return NotFound();

        db.OpcionesPreguntaPrueba.Add(new OpcionPreguntaPrueba
        {
            Id = Guid.NewGuid(),
            PreguntaPruebaId = model.PreguntaId,
            Texto = model.Texto,
            Orden = model.Orden,
            Activa = true
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "La opción se creó correctamente.";
        return RedirectToAction(nameof(Opciones), new { preguntaId = model.PreguntaId });
    }

    [HttpGet]
    public async Task<IActionResult> EditarOpcion(Guid id)
    {
        var opcion = await db.OpcionesPreguntaPrueba.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);
        if (opcion is null)
            return NotFound();

        return View(new OpcionPreguntaFormViewModel
        {
            Id = opcion.Id,
            PreguntaId = opcion.PreguntaPruebaId,
            Texto = opcion.Texto,
            Orden = opcion.Orden
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarOpcion(OpcionPreguntaFormViewModel model)
    {
        model.Texto = model.Texto?.Trim() ?? string.Empty;
        if (!ModelState.IsValid)
            return View(model);

        var opcion = await db.OpcionesPreguntaPrueba
            .SingleOrDefaultAsync(item => item.Id == model.Id);
        if (opcion is null)
            return NotFound();

        opcion.Texto = model.Texto;
        opcion.Orden = model.Orden;
        await db.SaveChangesAsync();

        TempData["Success"] = "La opción se actualizó correctamente.";
        return RedirectToAction(nameof(Opciones), new { preguntaId = opcion.PreguntaPruebaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstadoOpcion(Guid id)
    {
        var opcion = await db.OpcionesPreguntaPrueba
            .Include(item => item.Pregunta)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (opcion is null)
            return NotFound();

        opcion.Activa = !opcion.Activa;
        if (!opcion.Activa && opcion.Pregunta.Activa)
        {
            var activasRestantes = await db.OpcionesPreguntaPrueba
                .CountAsync(item =>
                    item.PreguntaPruebaId == opcion.PreguntaPruebaId &&
                    item.Activa &&
                    item.Id != opcion.Id);

            if (activasRestantes < 2)
            {
                opcion.Pregunta.Activa = false;
                TempData["Success"] = "La opción se desactivó y la pregunta volvió a borrador porque requiere dos opciones activas.";
            }
        }

        await db.SaveChangesAsync();
        TempData["Success"] ??= opcion.Activa
            ? "La opción quedó activa."
            : "La opción se desactivó y se conservó.";
        return RedirectToAction(nameof(Opciones), new { preguntaId = opcion.PreguntaPruebaId });
    }

    [HttpGet]
    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await db.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email
            })
            .ToListAsync();

        var asignaciones = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            select new
            {
                userRole.UserId,
                role.Name,
                role.NormalizedName
            })
            .ToListAsync();

        var rolesPorUsuario = asignaciones
            .GroupBy(asignacion => asignacion.UserId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo
                    .Select(asignacion => asignacion.Name ?? "Rol desconocido")
                    .OrderBy(nombre => nombre)
                    .ToArray());

        var rolesProtegidosPorUsuario = asignaciones
            .Where(asignacion =>
                asignacion.NormalizedName != null &&
                RolesProtegidos.Contains(asignacion.NormalizedName))
            .Select(asignacion => asignacion.UserId)
            .ToHashSet();

        var usuarioActualId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var model = new DirectorUsuariosViewModel
        {
            Usuarios = usuarios
                .Select(usuario => new DirectorUsuarioRolItem
                {
                    Id = usuario.Id,
                    Nombre = string.IsNullOrWhiteSpace(usuario.FullName)
                        ? "Sin nombre"
                        : usuario.FullName,
                    Correo = usuario.Email ?? "Sin correo",
                    Roles = rolesPorUsuario.TryGetValue(
                        usuario.Id,
                        out var roles)
                            ? roles
                            : Array.Empty<string>(),
                    PuedeEditar =
                        usuario.Id != usuarioActualId &&
                        !rolesProtegidosPorUsuario.Contains(usuario.Id)
                })
                .ToArray()
        };

        ViewBag.RolesAsignables = RolesAsignables;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(DirectorCambiarRolViewModel model)
    {
        if (!ModelState.IsValid ||
            !RolesAsignables.Contains(model.Rol, StringComparer.Ordinal))
        {
            TempData["Error"] =
                "No se pudo cambiar el tipo de usuario. Revisa la selección e inténtalo de nuevo.";
            return RedirectToAction(nameof(Usuarios));
        }

        var usuario = await db.Users
            .SingleOrDefaultAsync(user => user.Id == model.UsuarioId);

        if (usuario is null)
        {
            TempData["Error"] = "No se encontró la cuenta seleccionada.";
            return RedirectToAction(nameof(Usuarios));
        }

        var usuarioActualId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (usuario.Id == usuarioActualId)
        {
            TempData["Error"] = "No puedes cambiar tu propio tipo de usuario.";
            return RedirectToAction(nameof(Usuarios));
        }

        var roles = await db.Roles
            .Where(role =>
                role.NormalizedName != null &&
                RolesAsignablesNormalizados.Contains(role.NormalizedName))
            .ToListAsync();

        if (roles.Count != RolesAsignables.Length)
        {
            TempData["Error"] =
                "Falta configurar uno o más tipos de usuario. Contacta al administrador del sistema.";
            return RedirectToAction(nameof(Usuarios));
        }

        var rolesActuales = await (
            from userRole in db.UserRoles
            join role in db.Roles
                on userRole.RoleId equals role.Id
            where userRole.UserId == usuario.Id
            select role.NormalizedName)
            .ToListAsync();

        if (rolesActuales.Any(rol =>
                rol != null && RolesProtegidos.Contains(rol)))
        {
            TempData["Error"] =
                "Las cuentas con permisos de Director o Administrador no se pueden cambiar desde esta pantalla.";
            return RedirectToAction(nameof(Usuarios));
        }

        var idsRolesAsignables = roles
            .Select(role => role.Id)
            .ToArray();

        var asignacionesAnteriores = await db.UserRoles
            .Where(userRole =>
                userRole.UserId == usuario.Id &&
                idsRolesAsignables.Contains(userRole.RoleId))
            .ToListAsync();

        db.UserRoles.RemoveRange(asignacionesAnteriores);

        var nuevoRol = roles.Single(role =>
            role.NormalizedName == model.Rol.ToUpperInvariant());

        db.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = usuario.Id,
            RoleId = nuevoRol.Id
        });

        await db.SaveChangesAsync();

        TempData["Success"] =
            $"Se actualizó el tipo de usuario de {usuario.FullName}. El cambio de acceso se aplicará al refrescar su sesión.";

        return RedirectToAction(nameof(Usuarios));
    }
}
