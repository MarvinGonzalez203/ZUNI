using Zuni.Models.Agenda;

namespace Zuni.Services.Agenda;

public sealed class AgendaAccesoException : Exception { }
public sealed class AgendaOperacionException(string mensaje) : Exception(mensaje) { }

public static class AgendaReglas
{
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
            inicio.Ticks % TimeSpan.TicksPerMinute != 0 || fin.Ticks % TimeSpan.TicksPerMinute != 0 ||
            input.DuracionMinutos is < 15 or > 120 || input.DuracionMinutos % 5 != 0 ||
            (fin - inicio).TotalMinutes < input.DuracionMinutos)
            throw new AgendaOperacionException("Revisa el día, las horas y la duración (15 a 120 minutos, en múltiplos de 5). El horario no puede cruzar medianoche.");
    }

    public static DateOnly FechaPermitida(DateOnly? fecha, DateTime ahoraUtc)
    {
        var hoy = Hoy(ahoraUtc);
        var valor = fecha ?? hoy;
        if (valor < hoy || valor > hoy.AddDays(90))
            throw new AgendaOperacionException("Selecciona una fecha entre hoy y los próximos 90 días.");
        return valor;
    }

    public static bool Encaja(DisponibilidadPsicologo d, DateTime inicioUtc)
    {
        var local = Local(inicioUtc);
        var hora = TimeOnly.FromDateTime(local);
        var minutos = (hora - d.HoraInicio).TotalMinutes;
        return d.Activo && d.DiaSemana == local.DayOfWeek && hora >= d.HoraInicio &&
            minutos % d.DuracionMinutos == 0 &&
            minutos + d.DuracionMinutos <= (d.HoraFin - d.HoraInicio).TotalMinutes;
    }

    public static void ValidarCambio(Cita cita, EstadoCita nuevo, DateTime ahoraUtc)
    {
        if (cita.Estado != EstadoCita.Reservada || nuevo is not
            (EstadoCita.Cancelada or EstadoCita.Completada or EstadoCita.NoAsistio))
            throw new AgendaOperacionException("La cita ya cambió o la transición no está permitida.");
        if (nuevo == EstadoCita.Cancelada && cita.FechaHoraInicioUtc <= ahoraUtc)
            throw new AgendaOperacionException("Solo se pueden cancelar citas futuras.");
        if (nuevo is EstadoCita.Completada or EstadoCita.NoAsistio && cita.FechaHoraFinUtc > ahoraUtc)
            throw new AgendaOperacionException("Registra la asistencia después de finalizar el horario de la cita.");
    }
}
