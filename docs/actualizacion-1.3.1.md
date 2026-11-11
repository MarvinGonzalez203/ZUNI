# ZUNI 1.3.1 — avisos al registrar atención imprevista

El selector ofrecía estudiantes asignados con contraseña temporal pendiente, pero
al enviar el formulario se utilizaba la validación de acceso a reuniones virtuales.
Esta lanzaba una excepción de autorización y mostraba «No tienes permisos» al
psicólogo, sin explicar el requisito pendiente del estudiante.

La elegibilidad del estudiante se comparte entre el selector y el POST. El selector
identifica el cambio de contraseña pendiente y muestra un aviso al seleccionar la
cuenta. El envío directo al servidor mantiene el requisito: responde con un aviso
específico que indica que no se guardó la actividad y cómo resolverlo. Conserva el
estudiante, fecha, horas y modalidad escritos. Las cuentas ajenas o institucionales
siguen rechazándose por autorización.

Después de un guardado válido se abre el día y la actividad concreta en la agenda.
Si falta la solicitud consentida al intentar completar el registro profesional,
se explica que el horario está guardado y que no debe volver a registrarse.
No se crea consentimiento, verificación de edad ni registro profesional ficticio.
Las reglas de acceso a reuniones virtuales permanecen separadas.

La versión del ensamblado, menú y campana pasa a 1.3.1. Es una entrega local;
no se publica en zuni.lat.

Regresión: `dotnet run --project tests/Seguridad.Offline -c Release --no-restore -- --imprevistas`
y `node tests/Seguridad.Offline/imprevista.checks.cjs`. La prueba HTTP reproduce el
aviso y la ausencia de escritura con contraseña pendiente, simula que el alumno
completa su cambio y comprueba guardado, redirección y consulta de agenda, además
de errores de horario, doble envío y separación de roles. Se usan cuentas ficticias
en SQLite en memoria, sin modificar PostgreSQL.
