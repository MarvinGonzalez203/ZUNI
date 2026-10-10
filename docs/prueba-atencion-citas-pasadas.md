# Probar atención sin esperar una cita futura

Abrir PostgreSQL local, seleccionar la base `zuni` y ejecutar `tools/sql/citas-pasadas-prueba.sql`. Este script no se ejecuta automáticamente por la aplicación.

Usa únicamente `estudiante.prueba@miumg.edu.gt` y `psicologo.prueba@miumg.edu.gt`, con un vínculo vigente. Crea dos citas confirmadas ficticias, ayer y anteayer, de 09:00 a 09:50 (Guatemala), marcadas como piloto local. Repetir el script conserva lo que ya se registró; no reinicia las citas ni sus resultados. Si encuentra otra cita del estudiante ese día o una reserva profesional superpuesta, cancela la transacción.

1. Entrar con el psicólogo de prueba y abrir **Estudiantes y seguimiento → ficha del estudiante → Citas**.
2. Pulsar **Registrar atención** en una cita pasada. Elegir **Sí, asistió**, escribir textos ficticios en resultado privado, reseña y resultado compartido, y guardar.
3. En la otra cita, elegir **No asistió** y guardar. Puede añadirse una reseña breve; no se genera resultado clínico.
4. Revisar **Atención e historial**, el historial general y **Mis resultados** con el estudiante. El resultado clínico privado solo aparece al profesional.

Los registros se identifican con los IDs `b7394100-0000-4000-8000-000000000001` y `b7394100-0000-4000-8000-000000000002`; el movimiento indica explícitamente DEMOSTRACIÓN LOCAL. No usar datos reales en las notas. No cambiar el reloj del equipo ni quitar la validación de fechas de la aplicación.

## Una cita por día

Las solicitudes nuevas y las reprogramaciones comprueban que el estudiante no tenga otra cita solicitada, confirmada, terminada o con inasistencia en la misma fecha. Una cita cancelada o rechazada permite solicitar nuevamente. Reprogramar la cita existente en su mismo día está permitido y sustituye la reserva anterior.

Este cambio no elimina ni modifica las citas que ya existían antes de la nueva regla. Si había varias el mismo día, se conservan y se pueden cancelar las futuras que no se utilizarán.

Validación: 229 comprobaciones Big Five/citas/PostgreSQL/HTTP aprobadas, incluida la repetición del script en una base temporal, dos solicitudes simultáneas del mismo estudiante en horas distintas y reprogramación dentro del mismo día. Compilación sin errores; persiste el aviso NU1900 por la consulta de auditoría de NuGet no disponible. La tabla y el cambio entre ficha y ventana de gestión se verificaron visualmente con datos ficticios.

## Cancelación

El estudiante dueño de una cita o su psicólogo pueden cancelarla antes de su inicio, indicando un motivo breve. El estudiante puede informar que no puede asistir; el psicólogo puede comunicar que necesita cambiar su disponibilidad. El motivo y el actor quedan registrados en los movimientos. La franja se libera. Cuando el horario ya empezó, el profesional registra asistencia o inasistencia al finalizar.
