# Integración del panel del psicólogo

El panel `/Psicologo` es un prototipo visual restringido al rol `Psicologo`. La vista está en `Zuni/Views/Psicologo/Index.cshtml`; `Zuni/wwwroot/js/psicologo.js` evita envíos y limpia los formularios al cerrar. No hay persistencia, peticiones, datos ficticios precargados ni cambios al esquema de datos.

## Puntos de conexión

| Sección | Datos que deberá proporcionar el módulo existente |
| --- | --- |
| Estudiantes | Identificador, nombre, código y asignación o referencia al profesional autenticado. |
| Agenda | Cita, estudiante, profesional, fecha/hora con zona horaria, modalidad y estado. Los filtros deben aplicarse a las citas autorizadas. |
| Disponibilidad | Espacios del profesional y duración; el estudiante elige entre espacios disponibles. Validar conflictos y reglas institucionales en servidor. |
| Resultados | Identificador y versión del cuestionario, fecha, dimensiones, puntuación, escala y descripción aprobadas. No introducir umbrales ni diagnósticos automáticos. |
| Respuestas | Pregunta, dimensión y respuesta, únicamente tras verificar autorización vigente y alcance permitido. |
| Revisión | Resultado, interpretación, recomendaciones, estado revisado, profesional y fecha de revisión. |
| Atención | Cita completada, reseña, interpretación, recomendaciones, acuerdos y seguimiento. Mantener independiente de la revisión del cuestionario. |
| Historial | Atenciones autorizadas por estudiante, ordenadas por fecha, con profesional, modalidad y detalle de la reseña. |

## Al conectar

Reemplazar los estados vacíos por un ViewModel tipado y conectar los botones a acciones del servidor. Activar guardado y cambios de estado solo cuando existan las entidades correspondientes; retirar los avisos de vista previa y el reinicio de formularios cuando se implemente el flujo real de edición. Los filtros y la búsqueda actuales son únicamente controles visuales.

Comprobar permisos por estudiante, resultado, cita y nota en el servidor en cada lectura y escritura; el rol por sí solo no autoriza consultar cualquier estudiante. No enviar respuestas ocultas al navegador. Verificar consentimiento vigente, profesional asignado y política institucional antes de exponer información individual. Proteger escrituras con antifalsificación, validar estados de cita y registrar accesos relevantes. Las notas de atención requieren permisos propios y no deben incluirse en indicadores de Dirección.

No se han definido endpoints ni reglas de cancelación, reprogramación, conservación o acceso: acordarlos con los responsables de los módulos y la institución antes de habilitar datos reales. La vista no modifica el consentimiento del estudiante ni lo sustituye.
