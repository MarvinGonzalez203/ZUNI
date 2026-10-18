using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;

// Simula una cita propuesta por un Director ANTES del retiro de esa función (registro histórico).
// Permite seguir probando la respuesta del psicólogo sin que el Director pueda programar.
internal static class LegadoDirector
{
    public static async Task<Guid> CrearAsync(DbContextOptions<ApplicationDbContext> options, Guid perfil, string psicologo,
        DateTime inicioUtc, DateTime ahoraUtc, int minutos = 50, bool notificarPsicologo = false)
    {
        var id = Guid.NewGuid();
        await using var db = new OfflineDb(options);
        db.Citas.Add(new Cita
        {
            Id = id, PerfilEstudianteId = perfil, PsicologoUsuarioId = psicologo,
            FechaHoraInicioUtc = inicioUtc, FechaHoraFinUtc = inicioUtc.AddMinutes(minutos), FinAproximado = true,
            Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.Solicitada, ProgramadaDirectamente = true,
            ProgramadorUsuarioId = "director", ProgramadorRol = "Director", FechaCreacionUtc = ahoraUtc
        });
        if (notificarPsicologo)
            db.NotificacionesCita.Add(new NotificacionCita
            {
                UsuarioId = psicologo, CitaId = id, Texto = "Cita programada por Director (registro histórico).", FechaUtc = ahoraUtc,
                AutorUsuarioId = "director", AutorRol = "Director", Tipo = "Programación"
            });
        await db.SaveChangesAsync();
        return id;
    }
}
