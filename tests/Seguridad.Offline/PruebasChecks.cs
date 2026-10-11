using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Pruebas;

internal static class PruebasChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> opciones,
        string clave, Guid perfil, Guid perfil2)
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Pruebas: " + label); checks++; }
        OfflineDb Nuevo() => new(opciones);
        using var director = HostPrueba.Cliente(app);
        await HostPrueba.Login(director, "director@miumg.edu.gt", clave);
        foreach (var route in new[] { "/PruebasDirector", "/PruebasDirector/Crear", "/Director" })
            Check((await director.GetAsync(route)).StatusCode == HttpStatusCode.OK, "Render " + route);
        using var anonimo = HostPrueba.Cliente(app);
        Check((await anonimo.GetAsync("/PruebasDirector")).StatusCode == HttpStatusCode.Redirect, "Sin sesión");
        using var estudiante = HostPrueba.Cliente(app);
        await HostPrueba.Login(estudiante, "est@miumg.edu.gt", clave);
        Check((await estudiante.GetAsync("/PruebasDirector")).StatusCode == HttpStatusCode.Redirect, "Rol estudiante rechazado");
        Check((await director.PostAsync("/PruebasDirector/Guardar", new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "Antiforgery");

        (string, string)[] Campos(Guid? id = null, Guid? revision = null, string nombre = "Instrumento de prueba") =>
        [ ("Id", id?.ToString() ?? ""), ("Revision", (revision ?? Guid.Empty).ToString()),
          ("Nombre", nombre), ("Descripcion", "Uso académico"), ("Instrucciones", "Selecciona una opción"),
          ("Fuente", "Referencia profesional pendiente de revisión"), ("Idioma", "Español"),
          ("Dimensiones[0].Nombre", "Bienestar"), ("Preguntas[0].Texto", "Pregunta de ejemplo"),
          ("Preguntas[0].Dimension", "Bienestar"), ("Preguntas[0].Opciones[0].Texto", "Nunca"),
          ("Preguntas[0].Opciones[0].Valor", "0"), ("Preguntas[0].Opciones[1].Texto", "A veces"),
          ("Preguntas[0].Opciones[1].Valor", "1") ];
        var creado = await HostPrueba.Post(director, "/PruebasDirector/Crear", "/PruebasDirector/Guardar", Campos());
        Check(creado.StatusCode == HttpStatusCode.Redirect, "Crear");
        Guid id;
        await using (var db = Nuevo())
        {
            var p = await db.PruebasDirector.SingleAsync(); id = p.Id;
            Check(!p.Activa, "Borrador inactivo");
        }
        async Task<Prueba> Leer()
        {
            await using var db = Nuevo();
            return await db.PruebasDirector.AsNoTracking().Include(p => p.Preguntas).ThenInclude(q => q.Opciones).SingleAsync(p => p.Id == id);
        }
        var detalle = "/PruebasDirector/Detalle/" + id;
        var editar = "/PruebasDirector/Editar/" + id;
        var asignar = "/PruebasDirector/Asignar/" + id;
        var antes = await Leer();
        Check((await director.GetAsync(editar)).StatusCode == HttpStatusCode.OK, "Editor existente");
        var editado = await HostPrueba.Post(director, editar, "/PruebasDirector/Guardar", Campos(id, antes.Revision, "Instrumento editado"));
        Check(editado.StatusCode == HttpStatusCode.Redirect && (await Leer()).Nombre == "Instrumento editado", "Editar con reemplazo de estructura");
        await HostPrueba.Post(director, editar, "/PruebasDirector/Guardar", Campos(id, antes.Revision, "Edición obsoleta"));
        Check((await Leer()).Nombre == "Instrumento editado", "Conflicto conserva contenido");
        Check((await director.GetAsync(detalle)).StatusCode == HttpStatusCode.OK, "Vista detalle");
        Check((await director.GetAsync(asignar)).StatusCode == HttpStatusCode.OK, "Vista asignar inactiva");
        await HostPrueba.Post(director, detalle, "/PruebasDirector/Asignar", ("Id", id.ToString()), ("Revision", (await Leer()).Revision.ToString()), ("PerfilEstudianteId", perfil.ToString()));
        await using (var db = Nuevo()) Check(!await db.AsignacionesPrueba.AnyAsync(), "Inactiva no asignable");
        async Task Estado(bool activar) => await HostPrueba.Post(director, detalle, "/PruebasDirector/Estado",
            ("id", id.ToString()), ("revision", (await Leer()).Revision.ToString()), ("activar", activar.ToString()));
        await Estado(true);
        Check((await Leer()).Activa, "Activación");
        Check((await director.GetAsync(editar)).StatusCode == HttpStatusCode.Redirect, "Activa no editable");
        var lista = await director.GetStringAsync(asignar);
        Check(lista.Contains(perfil.ToString(), StringComparison.OrdinalIgnoreCase) && lista.Contains(perfil2.ToString(), StringComparison.OrdinalIgnoreCase), "Estudiantes elegibles");
        await HostPrueba.Post(director, asignar, "/PruebasDirector/Asignar", ("Id", id.ToString()), ("Revision", (await Leer()).Revision.ToString()), ("PerfilEstudianteId", perfil.ToString()));
        await using (var db = Nuevo()) Check(await db.AsignacionesPrueba.CountAsync() == 1, "Asignación persistida");
        // Una pestaña vieja no debe asignar tras un cambio concurrente del instrumento.
        await HostPrueba.Post(director, asignar, "/PruebasDirector/Asignar", ("Id", id.ToString()), ("Revision", antes.Revision.ToString()), ("PerfilEstudianteId", perfil2.ToString()));
        await using (var db = Nuevo()) Check(await db.AsignacionesPrueba.CountAsync() == 1, "Asignación con revisión obsoleta rechazada");
        await HostPrueba.Post(director, asignar, "/PruebasDirector/Asignar", ("Id", id.ToString()), ("Revision", (await Leer()).Revision.ToString()), ("PerfilEstudianteId", perfil.ToString()));
        await using (var db = Nuevo()) Check(await db.AsignacionesPrueba.CountAsync() == 1, "Sin duplicados");
        lista = await director.GetStringAsync(asignar);
        Check(!lista.Contains("value=\"" + perfil + "\"", StringComparison.OrdinalIgnoreCase), "Asignado excluido de selector");
        await Estado(false);
        Check(!(await Leer()).Activa, "Desactivación");
        Check((await director.GetAsync(editar)).StatusCode == HttpStatusCode.Redirect, "Asignada conserva contenido aunque inactiva");
        var bad = Campos(nombre: "Inválida").Select(x => x.Item1 == "Preguntas[0].Dimension" ? (x.Item1, "Ajena") : x).ToArray();
        var invalida = await HostPrueba.Post(director, "/PruebasDirector/Crear", "/PruebasDirector/Guardar", bad);
        Check(invalida.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(invalida)).Contains("Cada pregunta debe pertenecer"), "Dimensión ajena rechazada con editor");
        await using (var db = Nuevo()) Check(await db.PruebasDirector.CountAsync() == 1, "Inválida no guardada");
        var sinOpciones = Campos(nombre: "Sin opciones").Where(x => !x.Item1.Contains("Opciones")).ToArray();
        var opcionesInvalidas = await HostPrueba.Post(director, "/PruebasDirector/Crear", "/PruebasDirector/Guardar", sinOpciones);
        Check(opcionesInvalidas.StatusCode == HttpStatusCode.OK && (await HostPrueba.Texto(opcionesInvalidas)).Contains("al menos dos opciones"), "Opciones obligatorias por pregunta");
        Check(!(await director.GetStringAsync(detalle)).Contains("MOTIVO-CLINICO-PRIVADO"), "Sin datos clínicos");
        var borrador = Campos(nombre: "Borrador sin preguntas").Where(x => !x.Item1.StartsWith("Preguntas") && !x.Item1.StartsWith("Dimensiones")).ToArray();
        Check((await HostPrueba.Post(director, "/PruebasDirector/Crear", "/PruebasDirector/Guardar", borrador)).StatusCode == HttpStatusCode.Redirect, "Borrador vacío permitido");
        await using (var db = Nuevo())
        {
            var vacio = await db.PruebasDirector.AsNoTracking().SingleAsync(x => x.Nombre == "Borrador sin preguntas");
            await HostPrueba.Post(director, "/PruebasDirector/Detalle/" + vacio.Id, "/PruebasDirector/Estado", ("id", vacio.Id.ToString()), ("revision", vacio.Revision.ToString()), ("activar", "true"));
            Check(!await db.PruebasDirector.AsNoTracking().Where(x => x.Id == vacio.Id).Select(x => x.Activa).SingleAsync(), "Borrador vacío no activable");
        }
        Console.WriteLine($"OK: {checks} comprobaciones del módulo Director.");
    }
}
