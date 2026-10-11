using Zuni.Models.Agenda;

namespace Zuni.Services.Agenda;

public sealed class AgendaAccesoException : Exception { }
public sealed class AgendaOperacionException(string mensaje) : Exception(mensaje) { }

public static class AgendaReglas
{
    public static bool Activa(EstadoCita estado) => estado is EstadoCita.Reservada or EstadoCita.Solicitada or EstadoCita.Confirmada;
    public static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Guatemala");
    public static DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Zona);
    public static DateTime Utc(DateOnly fecha, TimeOnly hora) =>
        TimeZoneInfo.ConvertTimeToUtc(fecha.ToDateTime(hora, DateTimeKind.Unspecified), Zona);
    public static DateOnly Hoy(DateTime ahoraUtc) => DateOnly.FromDateTime(Local(ahoraUtc));
    public static bool Cruza(DateTime a, DateTime b, DateTime c, DateTime d) => a < d && c < b;
    public static bool Permite(ModalidadDisponibilidad disponible, ModalidadCita elegida) =>
        Enum.IsDefined(elegida) && (disponible == ModalidadDisponibilidad.Ambas || (int)disponible == (int)elegida);

    public static void ValidarDisponibilidad(DisponibilidadInput input)
    {
        if (!Enum.IsDefined(input.DiaSemana) || !Enum.IsDefined(input.Modalidad) ||
            input.HoraInicio is not { } inicio || input.HoraFin is not { } fin || fin <= inicio ||
            inicio < new TimeOnly(8, 0) || fin > new TimeOnly(18, 0) ||
            inicio.Ticks % (5 * TimeSpan.TicksPerMinute) != 0 || fin.Ticks % (5 * TimeSpan.TicksPerMinute) != 0 ||
            input.DuracionMinutos is < 15 or > 120 || input.DuracionMinutos % 5 != 0 ||
            (fin - inicio).TotalMinutes < input.DuracionMinutos)
            throw new AgendaOperacionException("Revisa el día, las horas y la duración (15 a 120 minutos, en múltiplos de 5). El horario no puede cruzar medianoche.");
    }

    public static DateOnly FechaPermitida(DateOnly? fecha, DateTime ahoraUtc)
    {
        var hoy = Hoy(ahoraUtc);
        var valor = fecha ?? hoy;
        if (valor < hoy || valor > hoy.AddMonths(12))
            throw new AgendaOperacionException("Selecciona una fecha entre hoy y los próximos 12 meses.");
        return valor;
    }

    public static bool Encaja(DisponibilidadPsicologo d, DateTime inicioUtc)
    {
        var local = Local(inicioUtc);
        var hora = TimeOnly.FromDateTime(local);
        var minutos = (hora - d.HoraInicio).TotalMinutes;
        return d.Activo && d.DiaSemana == local.DayOfWeek && hora >= d.HoraInicio &&
            (d.DuracionMinutos == 0 ? hora == d.HoraInicio :
            minutos % d.DuracionMinutos == 0 &&
            minutos + d.DuracionMinutos <= (d.HoraFin - d.HoraInicio).TotalMinutes);
    }

    public static void ValidarCambio(Cita cita, EstadoCita nuevo, DateTime ahoraUtc)
    {
        var permitida = nuevo switch
        {
            EstadoCita.Confirmada or EstadoCita.Rechazada => cita.Estado == EstadoCita.Solicitada,
            EstadoCita.Cancelada => Activa(cita.Estado),
            EstadoCita.Completada or EstadoCita.NoAsistio => cita.Estado is EstadoCita.Confirmada or EstadoCita.Reservada,
            _ => false
        };
        if (!permitida)
            throw new AgendaOperacionException("La cita ya cambió o la transición no está permitida.");
        if (nuevo is EstadoCita.Cancelada or EstadoCita.Confirmada && cita.FechaHoraInicioUtc <= ahoraUtc)
            throw new AgendaOperacionException("Solo se pueden cancelar o confirmar citas futuras.");
        if (nuevo is EstadoCita.Completada or EstadoCita.NoAsistio && cita.FechaHoraFinUtc > ahoraUtc)
            throw new AgendaOperacionException("Registra la asistencia después de finalizar el horario de la cita.");
    }

    public static DateOnly MesPermitido(DateOnly? fecha, DateTime ahoraUtc)
    {
        var hoy = Hoy(ahoraUtc);
        var valor = fecha ?? hoy;
        var mes = new DateOnly(valor.Year, valor.Month, 1);
        var minimo = new DateOnly(hoy.Year, hoy.Month, 1);
        if (mes < minimo || mes > minimo.AddMonths(12))
            throw new AgendaOperacionException("Consulta un mes dentro de los próximos 12 meses.");
        return mes;
    }

    public static IEnumerable<(DateTime Inicio, DateTime Fin)> Franjas(DisponibilidadPsicologo d, DateOnly fecha)
    {
        var inicio = Utc(fecha, d.HoraInicio);
        var fin = Utc(fecha, d.HoraFin);
        if (d.DuracionMinutos == 0) { yield return (inicio, fin); yield break; }
        if (d.DuracionMinutos < 0) yield break;
        for (var desde = inicio; desde.AddMinutes(d.DuracionMinutos) <= fin; desde = desde.AddMinutes(d.DuracionMinutos))
            yield return (desde, desde.AddMinutes(d.DuracionMinutos));
    }

    public static void ValidarHorarioDia(HorarioDiaInput input, DateTime ahoraUtc)
    {
        if (input.Fecha is null) throw new AgendaOperacionException("Selecciona una fecha válida.");
        var fecha = FechaPermitida(input.Fecha, ahoraUtc);
        if (input.HoraInicio is not { } inicio || input.HoraFin is not { } fin ||
            inicio < new TimeOnly(8, 0) || fin > new TimeOnly(18, 0) || fin <= inicio ||
            inicio.Ticks % (5 * TimeSpan.TicksPerMinute) != 0 || fin.Ticks % (5 * TimeSpan.TicksPerMinute) != 0 ||
            input.DuracionMinutos is not (0 or 15 or 30 or 50) || !Enum.IsDefined(input.Modalidad) ||
            (fin - inicio).TotalMinutes < input.DuracionMinutos)
            throw new AgendaOperacionException("Usa horas entre 08:00 y 18:00, múltiplos de 5, fin posterior e intervalo suficiente para la duración elegida.");
        var d = new DisponibilidadPsicologo { HoraInicio = inicio, HoraFin = fin, DuracionMinutos = input.DuracionMinutos };
        if (!Franjas(d, fecha).Any(f => f.Inicio > ahoraUtc))
            throw new AgendaOperacionException("El horario debe permitir al menos una cita futura completa.");
    }
}
