# Registro de atención por cita

Rama: `fix/registro-atencion-por-cita`. Sin commit, sin push, sin migraciones ni cambios de esquema.
**No compilado ni probado en la sesión que lo escribió**: verificar con los comandos del final.

## Causa

La regla de cierre ya comparaba contra el fin de cada cita:
`AgendaReglas.ValidarCambio` exige `Cita.FechaHoraFinUtc <= ahora` para Completada/NoAsistio, y la vista
`Psicologo/Agenda.cshtml` muestra "Registrar atención" cuando `cita.FinUtc <= ahora`.
Ninguna validación de cierre usaba `DisponibilidadPsicologo.HoraFin` ni `ConfiguracionAgendaDia`.

El error estaba en **cómo se calculaba el fin de la cita al reservar** en horarios del día con
"Sin duración establecida" (`DuracionMinutos = 0`):

- `AgendaCitasService.Reservar`: `fin = AgendaReglas.Utc(dia, horario.HoraFin)`. El fin de la cita se guardaba igual al fin del intervalo de disponibilidad (por ejemplo 18:00).
- `AgendaReglas.Encaja`: con duración 0 solo aceptaba iniciar exactamente en `HoraInicio`.
- `AgendaReglas.Franjas` y el calendario: ofrecían una única franja que cubría todo el intervalo.

Resultado: en una disponibilidad 08:00–18:00 sin duración fija, una cita de las 08:00 quedaba guardada como 08:00–18:00.
Solo podía cerrarse a las 18:00 y bloqueaba el resto del día.

## Regla anterior

- Intervalo sin duración fija: una sola cita por intervalo, desde su inicio hasta su fin (`FechaHoraFinUtc` = fin de la disponibilidad).
- Registrar atención: desde `FechaHoraFinUtc` de la cita (que en este caso coincidía con el fin de la disponibilidad).

## Regla nueva

- La disponibilidad solo define el rango reservable.
- En intervalos sin duración fija, el estudiante elige:
  - la hora de inicio, cada 15 minutos desde el inicio del intervalo;
  - la duración: 15, 30, 45, 60, 75, 90, 105 o 120 minutos.
- La cita debe terminar dentro del intervalo.
- `FechaHoraFinUtc = inicio + duración` de **esa** cita (`AgendaReglas.FinCita`).
- El calendario muestra los huecos libres reales (`AgendaReglas.TramosVariables`); el resto del intervalo sigue reservable.
- Registrar atención: permitido cuando `ahora >= FechaHoraFinUtc` de la cita. Es independiente de las demás citas y del fin de la disponibilidad (regla de servidor sin cambios).
- Completada y NoAsistio no se registran otra vez (regla e índice UNIQUE existentes).
- Sin cambios:
  - intervalos con duración fija (15–120 min);
  - reglas de solapamiento;
  - una cita activa por estudiante y día;
  - autorización clínica;
  - UTC interno y presentación en hora de Guatemala.

## Archivos modificados

- `Zuni/Services/Agenda/AgendaReglas.cs`: `Encaja` con duración solicitada, `FinCita`, `TramosVariables`, constantes de paso y duraciones.
- `Zuni/Services/Agenda/AgendaCitasService.cs`: `Reservar` calcula el fin con `FinCita`.
- `Zuni/Services/Agenda/AgendaCitasService.Calendario.cs`: los intervalos sin duración fija muestran huecos libres y tramos ocupados.
- `Zuni/Models/Agenda/AgendaViewModels.cs`: `ReservaCitaInput.DuracionMinutos` (opcional, 15–120).
- `Zuni/Views/Shared/_AgendaCalendario.cshtml`: selección de hora de inicio y duración en intervalos sin duración fija; textos.
- `Zuni/Views/Psicologo/Agenda.cshtml`: el aviso indica la hora de fin de esa cita.
- `tests/Agenda.Offline/CalendarioChecks.cs`: bloque "variable" actualizado a la nueva regla.
- `tests/Agenda.Offline/AtencionPorCitaChecks.cs` (nuevo) y `tests/Agenda.Offline/Program.cs` (lo invoca).
- `docs/AgendaCalendarioRevision.md`: descripción de `DuracionMinutos = 0` actualizada.

## Pruebas

`AtencionPorCitaChecks` (escenario 08:00–18:00 sin duración fija, citas 08:00–10:00, 10:30–12:00, 13:00–14:00 y 15:30–17:00):

1. Disponibilidad 08:00–18:00.
2. Cita A 08:00–10:00 con fin propio.
3. 09:59: el servidor rechaza preparar y registrar.
4. 10:00: el registro se permite.
5. B, C y D no cambian al registrar A; la disponibilidad tampoco.
6. 11:00: B todavía no.
7. 12:00: B se registra como NoAsistio.
8. A (Completada) y B (NoAsistio) no se registran otra vez.
9. C sigue normal después de cerrar A y B, y se registra a las 14:00.
10. Huecos libres reales y rechazo de solapamiento.
11. Autorización: otro psicólogo, estudiante y SecurityStamp caducado son rechazados.

## Comportamiento colateral

- Las citas "sin duración" ya guardadas antes de este cambio conservan su `FechaHoraFinUtc` original (fin del intervalo); no se modifican datos. Esas citas siguen habilitándose al terminar ese fin guardado.
- En intervalos sin duración fija ahora pueden existir varias citas en el mismo intervalo.
- El formulario de reserva de esos intervalos pide hora de inicio y duración.

## Verificación

```
dotnet restore Zuni\Zuni.sln
dotnet build Zuni\Zuni.sln -c Release
dotnet run --project tests\Agenda.Offline -c Release
dotnet run --project tests\BigFive.Offline -c Release
dotnet run --project tests\Seguridad.Offline -c Release
dotnet list Zuni\Zuni.csproj package --vulnerable --include-transitive
```
