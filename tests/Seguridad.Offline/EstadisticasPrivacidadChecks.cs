using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Models.Reportes;
using Zuni.Services.Agenda;
using Zuni.Services.Reportes;

// Privacidad estadística del Director: supresión primaria y complementaria, corte al cierre del mes
// anterior, sin filtros que combinar y sin distinguir asistencia de inasistencia. No usa PostgreSQL.
internal static class EstadisticasPrivacidadChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj)
    {
        int checks = 0; void Check(bool ok, string label) { if (!ok) throw new Exception("Estadísticas Director: " + label); checks++; }
        const int U = SupresionEstadistica.Umbral;

        // ------------------------------------------------------------ función de supresión (servidor)
        void Segura(DesgloseProtegido d, int total, string caso)
        {
            Check(d.Total == total, caso + ": total exacto");
            if (!d.Disponible) { Check(d.Visibles.Count == 0 && d.Agrupado is null, caso + ": sin desglose no publica celdas"); return; }
            Check(d.Visibles.All(v => v.Cantidad is >= U), caso + ": ninguna celda visible menor que el umbral");
            Check(d.Agrupado is null || (d.Agrupado >= U && d.CeldasAgrupadas >= 2), caso + ": el grupo suma al menos el umbral y tiene dos o más celdas");
            Check(d.Visibles.Sum(v => v.Cantidad ?? 0) + (d.Agrupado ?? 0) == total, caso + ": lo publicado suma el total (nada pequeño queda por diferencia)");
        }
        DesgloseProtegido P(params (string, int)[] celdas) => SupresionEstadistica.Proteger(celdas);

        var d10y1 = P(("A", 10), ("B", 1)); Segura(d10y1, 11, "11 = 10 + 1");
        Check(!d10y1.Disponible, "11 = 10 + 1: no se publica la carrera de 10 (permitiría deducir 1)");
        var d16y4 = P(("A", 16), ("B", 4)); Segura(d16y4, 20, "20 = 16 + 4");
        Check(!d16y4.Disponible, "20 = 16 + 4: solo información general");
        var varias = P(("A", 10), ("B", 6), ("C", 2), ("D", 2), ("E", 1)); Segura(varias, 21, "varias pequeñas");
        Check(varias.Disponible && varias.Visibles.Select(v => v.Etiqueta).SequenceEqual(new[] { "A", "B" }) && varias.Agrupado == 5 && varias.CeldasAgrupadas == 3,
            "varias pequeñas se agrupan entre sí cuando suman el umbral");
        var complementaria = P(("A", 12), ("B", 7), ("C", 3)); Segura(complementaria, 22, "supresión complementaria");
        Check(complementaria.Disponible && complementaria.Visibles.Single().Etiqueta == "A" && complementaria.Agrupado == 10,
            "la celda visible más pequeña se suprime para proteger la oculta");
        var unaGrande = P(("A", 9)); Segura(unaGrande, 9, "una sola carrera grande");
        Check(unaGrande.Disponible && unaGrande.Visibles.Single().Cantidad == 9, "una sola carrera grande sigue siendo útil");
        var unaPequena = P(("A", 3)); Segura(unaPequena, 3, "una sola carrera pequeña");
        Check(!unaPequena.Disponible, "una sola carrera pequeña no se publica");
        var todasGrandes = P(("A", 20), ("B", 5), ("C", 5)); Segura(todasGrandes, 30, "todas visibles");
        Check(todasGrandes.Visibles.Count == 3 && todasGrandes.Agrupado is null, "sin celdas pequeñas no se agrupa nada");
        Segura(P(("A", 0), ("B", 7), ("C", 5)), 12, "ceros ignorados");
        var vacio = P(); Check(vacio.Disponible && vacio.Total == 0 && vacio.Visibles.Count == 0, "sin datos");
        Check(SupresionEstadistica.Cifra(0) == 0 && SupresionEstadistica.Cifra(3) is null && SupresionEstadistica.Cifra(U) == U, "cifras individuales protegidas");

        // ------------------------------------------------------------ datos: mes anterior (cerrado) y mes en curso
        var hoy = AgendaReglas.Hoy(reloj.Ahora.UtcDateTime);
        var mesActual = new DateOnly(hoy.Year, hoy.Month, 1);
        var mesAnterior = mesActual.AddDays(-10);
        var futuro = mesActual.AddMonths(2).AddDays(4); // dos días del mismo mes futuro
        const string psy = "est-psy", mayor = "Carrera Estadística Mayor", menor = "Carrera Estadística Menor";
        var hasher = new PasswordHasher<ApplicationUser>();
        var perfiles = new List<PerfilEstudiante>();
        await using (var db = new OfflineDb(options))
        {
            var u = new ApplicationUser { Id = psy, FullName = "Psicólogo Estadístico", Email = psy + "@miumg.edu.gt", NormalizedEmail = (psy + "@miumg.edu.gt").ToUpperInvariant(),
                UserName = psy + "@miumg.edu.gt", NormalizedUserName = (psy + "@miumg.edu.gt").ToUpperInvariant(), IsActive = true, SecurityStamp = psy + "-stamp" };
            u.PasswordHash = hasher.HashPassword(u, clave); db.Users.Add(u); db.UserRoles.Add(new() { UserId = psy, RoleId = "Psicologo" });
            for (var i = 1; i <= 11; i++)
            {
                var id = $"est-carrera-{i:00}";
                db.Users.Add(new ApplicationUser { Id = id, FullName = $"Estudiante Estadística {i:00}", Email = id + "@miumg.edu.gt", NormalizedEmail = (id + "@miumg.edu.gt").ToUpperInvariant(),
                    UserName = id + "@miumg.edu.gt", NormalizedUserName = (id + "@miumg.edu.gt").ToUpperInvariant(), IsActive = true, SecurityStamp = id + "-stamp" });
                db.UserRoles.Add(new() { UserId = id, RoleId = "Estudiante" });
                var p = new PerfilEstudiante { UsuarioId = id, Carne = $"66662600{i:00}", Carrera = i == 11 ? menor : mayor, Telefono = "12345678" };
                db.PerfilesEstudiante.Add(p); perfiles.Add(p);
                // Mes cerrado: una cita concluida por estudiante (una de ellas es inasistencia).
                db.Citas.Add(new Cita { PerfilEstudianteId = p.Id, PsicologoUsuarioId = psy, FechaHoraInicioUtc = AgendaReglas.Utc(mesAnterior, new(8 + i % 9, 0)),
                    FechaHoraFinUtc = AgendaReglas.Utc(mesAnterior, new(8 + i % 9, 50)), Modalidad = ModalidadCita.Presencial,
                    Estado = i == 3 ? EstadoCita.NoAsistio : EstadoCita.Completada, ProgramadaDirectamente = true, ProgramadorUsuarioId = psy, ProgramadorRol = "Psicologo",
                    FechaCreacionUtc = reloj.Ahora.UtcDateTime.AddDays(-40) });
            }
            // Métricas del calendario: una completada y una inasistencia en el mismo mes deben verse juntas.
            var perfilMetricas = perfiles[0];
            db.Citas.Add(new Cita { PerfilEstudianteId = perfilMetricas.Id, PsicologoUsuarioId = psy, FechaHoraInicioUtc = AgendaReglas.Utc(futuro, new(9, 0)), FechaHoraFinUtc = AgendaReglas.Utc(futuro, new(9, 50)),
                Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.Completada, ProgramadaDirectamente = true, ProgramadorUsuarioId = psy, ProgramadorRol = "Psicologo", FechaCreacionUtc = reloj.Ahora.UtcDateTime });
            db.Citas.Add(new Cita { PerfilEstudianteId = perfilMetricas.Id, PsicologoUsuarioId = psy, FechaHoraInicioUtc = AgendaReglas.Utc(futuro.AddDays(1), new(9, 0)), FechaHoraFinUtc = AgendaReglas.Utc(futuro.AddDays(1), new(9, 50)),
                Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.NoAsistio, ProgramadaDirectamente = true, ProgramadorUsuarioId = psy, ProgramadorRol = "Psicologo", FechaCreacionUtc = reloj.Ahora.UtcDateTime });
            await db.SaveChangesAsync();
        }

        using var director = HostPrueba.Cliente(app); using var estudiante = HostPrueba.Cliente(app);
        await HostPrueba.Login(director, "director@miumg.edu.gt", clave); await HostPrueba.Login(estudiante, "est2@miumg.edu.gt", clave);
        async Task<string> Html(string ruta) { var r = await director.GetAsync(ruta); Check(r.StatusCode == HttpStatusCode.OK, ruta + " abre (200)"); return await HostPrueba.Texto(r); }
        // Contenido del panel (sin cabecera, menú ni tokens antiforgery, que cambian en cada petición).
        static string Contenido(string html) =>
            Regex.Match(html, "<p class=\"small\" data-corte-estadisticas.*?id=\"atendidos-carrera\".*?</section>", RegexOptions.Singleline).Value;
        int? Numero(string html, string indicador)
        {
            var m = Regex.Match(html, "data-indicador=\"" + indicador + "\">([^<]+)<");
            Check(m.Success, "indicador presente: " + indicador);
            return int.TryParse(m.Groups[1].Value.Trim(), out var n) ? n : null;
        }

        var estadisticas = await Html("/Director/Estadisticas");
        Check(estadisticas.Contains("data-corte-estadisticas=\"" + mesActual.ToString("yyyy-MM-dd") + "\""), "corte al cierre del mes anterior");
        Check(!estadisticas.Contains(menor), "la carrera pequeña no aparece con nombre ni cifra");
        Check(!estadisticas.Contains("Inasistencias") && !estadisticas.Contains("data-indicador=\"completadas\""), "no se separan inasistencias de atenciones completadas");
        // Invariantes del desglose publicado por carrera (vale aunque otras pruebas hayan dejado datos).
        var bloque = Regex.Match(estadisticas, "data-desglose=\"carrera\"[^>]*>.*?(</ul>|</p>)", RegexOptions.Singleline).Value;
        Check(bloque.Length > 0, "desglose por carrera presente");
        var atendidos = Numero(estadisticas, "atendidos");
        Check(atendidos is >= 11, "estudiantes con citas concluidas incluye el mes cerrado sembrado");
        if (bloque.Contains("data-desglose-estado=\"disponible\""))
        {
            var visibles = Regex.Matches(bloque, "data-celda=\"[^\"]*\" data-cantidad=\"(\\d+)\"").Select(m => int.Parse(m.Groups[1].Value)).ToList();
            var grupo = Regex.Match(bloque, "data-celda-agrupada data-cantidad=\"(\\d+)\"");
            var total = int.Parse(Regex.Match(bloque, "data-total=\"(\\d+)\"").Groups[1].Value);
            Check(visibles.All(v => v >= U) && (!grupo.Success || int.Parse(grupo.Groups[1].Value) >= U), "ninguna cifra publicada por carrera es menor que 5");
            Check(visibles.Sum() + (grupo.Success ? int.Parse(grupo.Groups[1].Value) : 0) == total && total == atendidos, "ninguna cifra oculta se deduce restando del total");
        }
        else Check(bloque.Contains("data-desglose-estado=\"no-disponible\""), "si no puede protegerse, solo el total");

        // Filtros: el endpoint no admite filtros; parámetros arbitrarios no cambian el resultado.
        var filtrada = await Html("/Director/Estadisticas?carrera=" + Uri.EscapeDataString(menor) + "&desde=2000-01-01&hasta=2100-01-01&psicologoId=" + psy + "&estado=Pendiente&mes=" + mesAnterior.ToString("yyyy-MM"));
        Check(Contenido(filtrada).Length > 0 && Contenido(filtrada) == Contenido(estadisticas), "los filtros en la URL no generan otra vista para comparar");

        // Periodo: una cita concluida en el mes en curso no cambia los indicadores de personas hasta el cierre.
        await using (var db = new OfflineDb(options))
        {
            var p = await db.PerfilesEstudiante.FirstAsync(x => x.Carrera == menor);
            db.Citas.Add(new Cita { PerfilEstudianteId = p.Id, PsicologoUsuarioId = psy, FechaHoraInicioUtc = AgendaReglas.Utc(hoy, new(0, 5)), FechaHoraFinUtc = AgendaReglas.Utc(hoy, new(0, 55)),
                Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.Completada, ProgramadaDirectamente = true, ProgramadorUsuarioId = psy, ProgramadorRol = "Psicologo", FechaCreacionUtc = reloj.Ahora.UtcDateTime });
            await db.SaveChangesAsync();
        }
        var despues = await Html("/Director/Estadisticas");
        Check(Numero(despues, "atendidos") == atendidos && Regex.Match(despues, "data-desglose=\"carrera\"[^>]*>.*?(</ul>|</p>)", RegexOptions.Singleline).Value == bloque,
            "una atención del mes en curso no altera las cifras publicadas (sin deducción antes/después)");

        // Calendario: concluidas juntas, sin inasistencias.
        var carga = await Html("/Director/Calendario?psicologoId=" + psy + "&fecha=" + futuro.ToString("yyyy-MM-dd"));
        Check(Regex.IsMatch(carga, "data-metrica=\"concluidas\">2<") && !carga.Contains("data-metrica=\"inasistencias\"") && !carga.Contains("data-metrica=\"completadas\""),
            "el calendario une completadas e inasistencias");
        Check((await estudiante.GetAsync("/Director/Estadisticas")).StatusCode == HttpStatusCode.Redirect, "estadísticas solo para Dirección");
        foreach (var i in Enumerable.Range(1, 11))
            Check(!estadisticas.Contains($"Estudiante Estadística {i:00}") && !estadisticas.Contains($"66662600{i:00}"), "sin identidades en estadísticas");
        Console.WriteLine($"OK: {checks} comprobaciones de privacidad estadística.");
    }
}
