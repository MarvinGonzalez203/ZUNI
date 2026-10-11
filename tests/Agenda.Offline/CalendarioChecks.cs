using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;

internal static class CalendarioChecks
{
    public static async Task<int> Run(ApplicationDbContext db, AgendaCitasService service,
        ClaimsPrincipal estudiante, ClaimsPrincipal segundo, ClaimsPrincipal psicologo, ClaimsPrincipal otroPsicologo,
        TimeProvider reloj, CancellationToken ct)
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Calendario: " + label); checks++; }
        async Task Rechaza(Func<Task> op, string label)
        {
            try { await op(); } catch (AgendaOperacionException) { checks++; return; }
            throw new Exception("Calendario: " + label);
        }
        var hoy = AgendaReglas.Hoy(reloj.GetUtcNow().UtcDateTime);
        var fecha = hoy.AddDays(1); // martes sin base semanal
        HorarioDiaInput Horario(DateOnly? dia = null, int duracion = 30, int inicio = 8, int fin = 10) => new()
        {
            Fecha = dia ?? fecha, HoraInicio = new(inicio, 0), HoraFin = new(fin, 0),
            DuracionMinutos = duracion, Modalidad = ModalidadDisponibilidad.Ambas
        };
        ReservaCitaInput Reserva(DateOnly dia, int hora, ModalidadCita modalidad = ModalidadCita.Virtual) => new()
        { Inicio = new DateTimeOffset(AgendaReglas.Utc(dia, new(hora, 0))), Modalidad = modalidad };
        async Task<DiaAgendaViewModel> Dia(DateOnly dia, bool alumno = false) => alumno
            ? (await service.LeerCitas(estudiante, dia, 1, ct)).Calendario.Dias.Single(d => d.Fecha == dia)
            : (await service.LeerAgenda(otroPsicologo, dia, ct)).Calendario.Dias.Single(d => d.Fecha == dia);
        async Task<Cita> Cita(DateOnly dia) => await db.Citas.AsNoTracking().SingleAsync(c =>
            c.PsicologoUsuarioId == "otro-psi" && c.FechaHoraInicioUtc >= AgendaReglas.Utc(dia, TimeOnly.MinValue) &&
            c.FechaHoraInicioUtc < AgendaReglas.Utc(dia.AddDays(1), TimeOnly.MinValue));

        Check((int)EstadoCita.Reservada == 0 && (int)EstadoCita.Cancelada == 1 && (int)EstadoCita.Completada == 2 &&
            (int)EstadoCita.NoAsistio == 3 && (int)EstadoCita.Solicitada == 4 && (int)EstadoCita.Confirmada == 5 && (int)EstadoCita.Rechazada == 6, "Enums persistidos estables");
        var calendario = (await service.LeerAgenda(otroPsicologo, null, ct)).Calendario;
        Check(calendario.Mes == new DateOnly(2030, 1, 1) && calendario.Dias.Count == 31, "Mes inicial completo");
        Check(calendario.Dias.Where(d => d.Fecha < hoy).All(d => !d.Editable && d.Estado == "Pasado"), "Pasado deshabilitado");
        var ultimo = (await service.LeerAgenda(otroPsicologo, hoy.AddMonths(12), ct)).Calendario;
        Check(ultimo.Dias.Where(d => d.Fecha > hoy.AddMonths(12)).All(d => !d.Editable), "Límite diario dentro del último mes");
        await Rechaza(() => service.LeerAgenda(otroPsicologo, hoy.AddMonths(13), ct), "Mes fuera de plazo");
        foreach (var input in new[] { Horario(hoy.AddDays(-1)), Horario(hoy.AddMonths(12).AddDays(1)),
            Horario(inicio: 7), Horario(fin: 19), Horario(inicio: 10, fin: 9), Horario(duracion: 20) })
            await Rechaza(() => service.AgregarHorarioDia(otroPsicologo, input, ct), "Parámetro diario inválido");
        var minutos = Horario(); minutos.HoraInicio = new(8, 1);
        await Rechaza(() => service.AgregarHorarioDia(otroPsicologo, minutos, ct), "Minutos no múltiplos de cinco");
        minutos.HoraInicio = new TimeOnly(8, 0).Add(TimeSpan.FromTicks(1));
        await Rechaza(() => service.AgregarHorarioDia(otroPsicologo, minutos, ct), "Fracciones de segundo inválidas");
        var corto = Horario(duracion: 50); corto.HoraFin = new(8, 30);
        await Rechaza(() => service.AgregarHorarioDia(otroPsicologo, corto, ct), "Duración no cabe");
        var invalido = Horario(); invalido.Modalidad = (ModalidadDisponibilidad)9;
        await Rechaza(() => service.AgregarHorarioDia(otroPsicologo, invalido, ct), "Modalidad inválida");
        var pasadoHoy = Horario(hoy); pasadoHoy.HoraInicio = new(8, 0); pasadoHoy.HoraFin = new(9, 0);
        try { AgendaReglas.ValidarHorarioDia(pasadoHoy, AgendaReglas.Utc(hoy, new(8, 45))); throw new Exception("Horario sin cita futura completa aceptado"); }
        catch (AgendaOperacionException) { checks++; }
        pasadoHoy.HoraFin = new(10, 0);
        AgendaReglas.ValidarHorarioDia(pasadoHoy, AgendaReglas.Utc(hoy, new(8, 45))); checks++;
        await service.AgregarHorarioDia(otroPsicologo, Horario(), ct);
        await service.AgregarHorarioDia(otroPsicologo, Horario(inicio: 14, fin: 17, duracion: 50), ct);
        Check((await Dia(fecha)).Intervalos.Count == 2 && (await Dia(fecha)).Estado == "Disponible", "Múltiples intervalos reales");
        await Rechaza(() => service.AgregarHorarioDia(otroPsicologo, Horario(), ct), "Superposición diaria");
        await service.CambiarDia(otroPsicologo, fecha, "bloquear", null, ct);
        Check((await Dia(fecha, true)).Estado == "Ocupado" && (await Dia(fecha, true)).Franjas.Count == 0, "Bloqueo oculta franjas");
        await Rechaza(() => service.Reservar(estudiante, Reserva(fecha, 8), ct), "Servidor rechaza día bloqueado");
        await service.CambiarDia(otroPsicologo, fecha, "habilitar", null, ct);
        Check((await Dia(fecha)).Intervalos.Count == 2 && (await Dia(fecha, true)).Franjas.Count == 7, "Habilitar conserva configuración");
        await service.Reservar(estudiante, Reserva(fecha, 8), ct);
        Check((await Cita(fecha)).Estado == EstadoCita.Solicitada, "Nueva solicitud");
        var compartida = await Dia(fecha, true);
        Check(compartida.Franjas.Count(f => f.Ocupada) == 1 && compartida.CitaPropiaActiva, "Franja ocupada y aviso propio");
        Check(typeof(FranjaAgenda).GetProperties().Select(p => p.Name).Order().SequenceEqual(
            new[] { "InicioUtc", "FinUtc", "Modalidad", "Ocupada", "Variable" }.Order()), "DTO sin identidades de terceros");
        Check(compartida.Intervalos.Count == 0, "Alumno no recibe IDs de configuración ni datos de gestión");
        await Rechaza(() => service.Reservar(estudiante, Reserva(fecha, 14), ct), "Una cita diaria incluso sin solapamiento");
        var franja = (await Dia(fecha)).Intervalos.First();
        await Rechaza(() => service.CambiarDia(otroPsicologo, fecha, "bloquear", null, ct), "Solicitud protege bloqueo");
        await Rechaza(() => service.CambiarDia(otroPsicologo, fecha, "limpiar", null, ct), "Solicitud protege limpieza");
        await Rechaza(() => service.CambiarDia(otroPsicologo, fecha, "quitar", franja.Id, ct), "Solicitud protege intervalo");
        var libre = (await Dia(fecha)).Intervalos.Last();
        await Rechaza(() => service.CambiarDia(otroPsicologo, fecha, "bloquear", libre.Id, ct), "ID manipulado no reduce ámbito de bloqueo");
        await service.CambiarDia(otroPsicologo, fecha, "quitar", libre.Id, ct);
        Check((await Dia(fecha)).Intervalos.Count == 1, "Puede quitar intervalo sin citas de un día con otras citas");
        var cita = await Cita(fecha);
        await service.GestionarCita(otroPsicologo, new() { CitaId = cita.Id, Estado = EstadoCita.Rechazada }, ct);
        Check((await Cita(fecha)).Estado == EstadoCita.Rechazada && (await Dia(fecha, true)).Franjas.All(f => !f.Ocupada), "Rechazo libera franja");
        Check((await service.Historial(otroPsicologo, null, EstadoCita.Rechazada, 1, ct)).Citas.Any(c => c.Id == cita.Id), "Rechazada en historial");
        await Rechaza(() => service.GestionarCita(otroPsicologo, new() { CitaId = cita.Id, Estado = EstadoCita.Confirmada }, ct), "Final no se reactiva");
        await service.CambiarDia(otroPsicologo, fecha, "limpiar", null, ct);
        Check((await Dia(fecha)).Estado == "Sin horarios" && await db.Citas.AnyAsync(c => c.Id == cita.Id), "Limpieza conserva citas históricas");

        var variable = fecha.AddDays(1);
        await service.AgregarHorarioDia(otroPsicologo, Horario(variable, duracion: 0), ct);
        Check((await Dia(variable, true)).Franjas.Count == 1 && (await Dia(variable, true)).Franjas.Single().Variable, "Variable: un único hueco libre");
        await Rechaza(() => service.Reservar(estudiante, Reserva(variable, 9), ct), "Variable exige elegir la duración de la cita");
        ReservaCitaInput ReservaVariable(int hora, int minuto, int duracion, ModalidadCita modalidad = ModalidadCita.Presencial) => new()
        { Inicio = new DateTimeOffset(AgendaReglas.Utc(variable, new(hora, minuto))), Modalidad = modalidad, DuracionMinutos = duracion };
        await Rechaza(() => service.Reservar(estudiante, ReservaVariable(8, 10, 30), ct), "Variable: inicio fuera de la cuadrícula de 15 minutos");
        await Rechaza(() => service.Reservar(estudiante, ReservaVariable(9, 0, 75), ct), "Variable: la cita no puede pasar del fin del rango");
        await Rechaza(() => service.Reservar(estudiante, ReservaVariable(8, 0, 20), ct), "Variable: duración no permitida");
        await service.Reservar(estudiante, ReservaVariable(8, 0, 120), ct);
        cita = await Cita(variable);
        Check((cita.FechaHoraFinUtc - cita.FechaHoraInicioUtc).TotalMinutes == 120, "Variable: el fin es inicio + duración elegida");
        Check((await Dia(variable, true)).Estado == "Ocupado", "Todas las franjas ocupadas");
        await service.GestionarCita(otroPsicologo, new() { CitaId = cita.Id, Estado = EstadoCita.Confirmada }, ct);
        Check(!(await service.Historial(otroPsicologo, null, null, 1, ct)).Citas.Any(c => c.Id == cita.Id), "Confirmada fuera del historial");
        await Rechaza(() => service.CambiarDia(otroPsicologo, variable, "bloquear", null, ct), "Confirmada protege horario");
        await service.CancelarEstudiante(estudiante, cita.Id, "Cambio de horario.", ct);
        var soloPresencial = fecha.AddDays(2);
        var presencial = Horario(soloPresencial); presencial.Modalidad = ModalidadDisponibilidad.Presencial;
        await service.AgregarHorarioDia(otroPsicologo, presencial, ct);
        await Rechaza(() => service.Reservar(estudiante, Reserva(soloPresencial, 8), ct), "Modalidad manipulada rechazada");
        await service.Reservar(estudiante, Reserva(soloPresencial, 8, ModalidadCita.Presencial), ct);
        cita = await Cita(soloPresencial);
        // Simular fila histórica de Kevin; no hay transformación masiva de datos reales.
        var legacy = await db.Citas.SingleAsync(c => c.Id == cita.Id); legacy.Estado = EstadoCita.Reservada;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await Rechaza(() => service.CambiarDia(otroPsicologo, soloPresencial, "bloquear", null, ct), "Reservada legado protege horario");
        await Rechaza(() => service.Reservar(estudiante, Reserva(soloPresencial, 9, ModalidadCita.Presencial), ct), "Legado aplica límite diario");
        Check((await Dia(soloPresencial, true)).Franjas.First().Ocupada, "Legado ocupa franja");
        Check((await service.LeerAgenda(otroPsicologo, hoy.AddMonths(1), ct)).Citas.Any(c => c.Id == legacy.Id), "Legado activo permanece accesible al cambiar de mes");
        AgendaReglas.ValidarCambio(legacy, EstadoCita.Completada, legacy.FechaHoraFinUtc.AddMinutes(1)); checks++;
        await service.CancelarEstudiante(estudiante, legacy.Id, "Cambio de horario.", ct);

        var lunes = hoy.AddDays(7);
        var baseAntes = (await Dia(lunes)).Intervalos.Single();
        await service.CambiarDia(otroPsicologo, lunes, "quitar", baseAntes.Id, ct);
        Check((await Dia(lunes)).Estado == "Sin horarios" && (await Dia(lunes.AddDays(7))).Estado == "Disponible", "Quitar base solo afecta fecha elegida");
        Check(await db.DisponibilidadesPsicologo.AnyAsync(d => d.Id == baseAntes.Id && d.Activo && d.ConfiguracionAgendaDiaId == null), "Fila semanal original conservada");
        var siguiente = lunes.AddDays(7);
        await service.AgregarHorarioDia(otroPsicologo, Horario(siguiente, inicio: 14, fin: 17), ct);
        Check((await Dia(siguiente)).Intervalos.Count == 2 && (await Dia(siguiente.AddDays(7))).Intervalos.Count == 1, "Base materializada una vez al particularizar");
        var count = await db.ConfiguracionesAgendaDia.CountAsync();
        var ajeno = (await Dia(siguiente)).Intervalos.First().Id;
        await Rechaza(() => service.CambiarDia(psicologo, siguiente, "quitar", ajeno, ct), "Horario ajeno no alterable");
        Check(await db.ConfiguracionesAgendaDia.CountAsync() == count, "Rollback no deja configuración fantasma");
        Check(!await db.ParticipacionesBigFive.AnyAsync(), "Ninguna participación Big Five requerida");
        return checks;
    }
}
