# Estudiantes y seguimiento

El menú del psicólogo reúne Estudiantes, Resultados y Atención en **Estudiantes y seguimiento**. Agenda conserva la configuración de disponibilidad. Las rutas antiguas de resultados y atención redirigen al listado. **Historial** conserva una vista general independiente; la ficha mantiene el historial individual. Ambas vistas utilizarán los mismos registros de atención cuando se implementen las citas terminadas.

El listado usa vínculos vigentes de la cuenta autenticada y verifica en PostgreSQL los roles y estados activos del profesional y del estudiante. Muestra nombre, carné, carrera y disponibilidad del resultado Big Five. No requiere conocer un código para encontrar al estudiante: aparece automáticamente al quedar vinculado. Incluye búsqueda opcional por nombre, filtros de resultado disponible y paginación de 24 filas.

Abrir el nombre o **Abrir ficha** muestra una ventana con:

- **Big Five:** cinco dimensiones, fecha, motivo y referencia. Solo se consulta el resultado del estudiante seleccionado, con consentimiento, roles y vínculo vigentes. La lectura se registra en AccesosBigFive; listar y filtrar no descarga resultados. Un perfil ajeno devuelve 404.
- **Citas:** estructura visual para próxima cita, modalidad, estado e historial. Todavía no hay registros de reservas ni operaciones de reprogramación/cancelación.
- **Atención e historial:** formato visual de una cita finalizada, notas privadas y recomendaciones publicables. Está deshabilitado y no guarda datos hasta implementar atención y citas.

Los filtros de citas agendadas, pendientes/programadas, reprogramadas, canceladas y terminadas están visibles como opciones en preparación, sin resultados ficticios. El orden por próxima cita también está preparado visualmente. Mientras no existan reservas, se ordena por nombre. Al implementar reservas, se deberá ordenar por la fecha de la próxima cita pendiente y poner al final a quienes no tengan ninguna, manteniendo filtros y permisos en servidor.

Retirar el consentimiento oculta el resultado y el motivo en la ficha, pero conserva al estudiante en la lista si su vínculo sigue activo. “Sin resultado disponible” no afirma que nunca haya completado el cuestionario: también incluye resultados inaccesibles por consentimiento o modo de aplicación.

Esta entrega no añade tablas ni migraciones, ni modifica la base de trabajo. Reiniciar la aplicación para cargar las vistas y controladores nuevos.

## Verificación

Compilación sin errores ni advertencias. `tests/BigFive.Checks`: 155 comprobaciones con PostgreSQL temporal y HTTP real. Incluye listado limitado a vinculados, filtros/búsqueda, lectura individual, auditoría, rechazo de ficha ajena y ocultación tras retirar consentimiento, además de las comprobaciones anteriores del cuestionario y calendario. La base temporal se elimina al terminar. Las vistas de prueba se revisaron en el navegador con datos ficticios, incluida la navegación entre las tres pestañas de la ficha.
