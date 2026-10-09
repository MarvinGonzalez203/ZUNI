# Evaluaciones y resultados de ZUNI

## Implementación

Rama: `feature/student-dashboard-clean`. Base: `localhost:5432/ZuniDesarrollo`. No se hicieron commits, merges, rebase ni push.

Migración aditiva: `20261009043711_AddEvaluaciones`.

Tablas nuevas:
- Evaluaciones: definición e identificación de demostraciones.
- PreguntasEvaluacion: preguntas ordenadas y obligatoriedad.
- AsignacionesEvaluacion: titular, estado, fechas, versión y comprobante.
- RespuestasEvaluacion: una respuesta por pregunta y asignación.
- ResultadosEvaluacion: puntuación opcional, observaciones publicables y autorización.

Las claves foráneas compuestas impiden mezclar respuestas de evaluaciones diferentes. Hay restricciones de valores (1–5), estados, finalización y publicación, índices únicos para definición/asignación/pregunta/comprobante y control de concurrencia por versión.

Los estados son mutuamente excluyentes: Asignada → Pendiente → EnProceso → Finalizada. Solo Administrador puede habilitar, publicar, ocultar o reabrir. Reabrir conserva las respuestas y retira la publicación. Las mutaciones usan transacciones y bloquean la asignación para serializar guardado, finalización y administración. El envío repetido conserva el mismo comprobante.

Los resultados del estudiante se proyectan desde PostgreSQL filtrando por el identificador de su sesión. Si no están publicados, la consulta devuelve únicamente el título/fecha/estado, sin puntuación ni observaciones. No se crearon campos ni interfaces para notas clínicas.

## Respaldo y preservación

Archivo privado, excluido de Git:
`.visual-check/private-backups/zuni-before-evaluaciones-20261009-043222.dump`

Se creó con pg_dump, se restauró con pg_restore en una base temporal del mismo PostgreSQL local y se compararon cantidades y huellas del contenido de las 11 tablas. Coincidieron; después se eliminó la base temporal. La restauración no sobrescribió ZuniDesarrollo.

Después de migrar y crear las demostraciones, se compararon las 10 tablas de datos anteriores (excluyendo el historial de migraciones): todas seguían idénticas. Las pruebas posteriores agregaron auditoría de las operaciones administrativas ejecutadas sobre las demostraciones. No modificaron credenciales, roles, perfiles ni datos de otros estudiantes.

## Cómo probar con Evin

Reinicia la aplicación desde tu entorno habitual para cargar los archivos nuevos. Ingresa con tu cuenta actual y selecciona Estudiante → Evaluaciones.

| Categoría inicial | Demostración | Comprobación |
|---|---|---|
| Asignadas | Organización del tiempo | Ver instrucciones; permanece bloqueada hasta habilitarla desde Administración. |
| Pendientes | Hábitos de estudio | Iniciar; responder; guardar; actualizar o volver a ingresar y recuperar las respuestas guardadas. |
| En proceso | Adaptación universitaria | Ya tiene una respuesta guardada; continuar y completar las otras dos. |
| Finalizadas | Rutinas de bienestar | Consultar fecha y comprobante. No permite editar. |

Cada cuestionario contiene tres preguntas de ejemplo con escala Nunca–Siempre (1–5). Todos están marcados como demostración sin interpretación clínica.

El guardado es **manual**: «Guardar avance» o «Guardar y revisar respuestas». La página avisa si intentas salir con cambios sin guardar. Anterior/Siguiente no borran selecciones. Después de guardar y revisar, «Finalizar evaluación» abre la ventana de confirmación. Cancelar no envía ni pierde el avance ya guardado. Confirmar valida todas las preguntas obligatorias antes de finalizar.

Para probar publicación, cambia a Administrador → Evaluaciones y resultados. En Rutinas de bienestar pulsa «Autorizar publicación». Regresa a Estudiante → Resultados y verás la suma ficticia de 9 puntos, sin diagnóstico. «Ocultar resultado» la retira. «Reabrir evaluación» devuelve el cuestionario a En proceso, conserva las respuestas y oculta el resultado anterior. Cada operación agrega auditoría sin copiar respuestas.

Al finalizar las pruebas automatizadas se restauraron las cuatro demostraciones a sus estados iniciales. El resultado finalizado se dejó **pendiente de publicación** para probar el control de autorización. Repetir el creador de demostraciones no reinicia avances ni duplica evaluaciones: primera ejecución 4, segunda 0.

## Pruebas ejecutadas

- Respaldo y restauración real: 11 tablas comparadas, idénticas.
- Migración aplicada y cuatro estados consultados en PostgreSQL.
- Carga idempotente: 4 creadas; segunda ejecución 0.
- 67 comprobaciones HTTP/PostgreSQL aprobadas: estados, habilitación, inicio, guardado parcial, recuperación en nueva sesión, versión obsoleta, obligatorias, revisión, persistencia al volver, doble envío, comprobante, bloqueo tras finalizar, resultados ocultos/publicados, aislamiento entre estudiantes, antifalsificación, reapertura y rutas de módulos existentes.
- 8 pruebas JavaScript aprobadas: 4 nuevas de navegación, cancelación del diálogo, doble envío y aviso de cambios; 4 existentes del perfil.
- La primera ejecución de integración detectó un error de enlace de diccionario en formularios manipulados. Se corrigió y la ejecución completa posterior aprobó los 67 controles.
- Compilación correcta. La compilación informó una advertencia NU1900 por indisponibilidad del índice de auditoría de NuGet; no es un error de compilación.

Las pruebas HTTP utilizan sesiones firmadas localmente con el ID y sello actual de cada usuario; no prueban escribir la contraseña en el formulario de login. No hubo inspección visual automatizada en un navegador real: el diálogo y la navegación se verificaron ejecutando su JavaScript con un DOM simulado, además del HTML servido por la aplicación.

Reproducción local: compilar `Zuni/Zuni.csproj` con salida `.visual-check/eval-runtime`; iniciar esa compilación en `http://127.0.0.1:7111` con entorno Development y raíz de contenido Zuni; ejecutar `dotnet run --project tests/Evaluaciones.Integration/Evaluaciones.Integration.csproj`. El test solo opera sobre las cuatro demostraciones de Evin y restaura sus estados en finally, conservando auditoría. Ejecutar `node --test tests/evaluaciones-ui.test.cjs tests/student-profile.test.cjs` para JavaScript.

## Archivos agregados en esta implementación

- Zuni/Models/Evaluaciones/EntidadesEvaluacion.cs
- Zuni/Models/Evaluaciones/ResultadoEstudianteViewModel.cs
- Zuni/Data/EvaluacionesModelConfiguration.cs
- Zuni/Migrations/20261009043711_AddEvaluaciones.cs
- Zuni/Migrations/20261009043711_AddEvaluaciones.Designer.cs
- Zuni/Services/EvaluacionesService.cs
- Zuni/Services/EvaluacionesDemostracion.cs
- Zuni/Controllers/EvaluacionesController.cs
- Zuni/Controllers/AdministradorEvaluaciones.cs
- Zuni/Views/Evaluaciones/_ViewStart.cshtml
- Zuni/Views/Evaluaciones/Detalle.cshtml
- Zuni/Views/Evaluaciones/Revisar.cshtml
- Zuni/Views/Administrador/Evaluaciones.cshtml
- Zuni/wwwroot/css/evaluaciones.css
- Zuni/wwwroot/js/evaluaciones.js
- tests/Evaluaciones.Integration/Evaluaciones.Integration.csproj
- tests/Evaluaciones.Integration/Program.cs
- tests/evaluaciones-ui.test.cjs
- docs/evaluaciones-local.md

## Archivos existentes modificados en esta implementación

- Zuni/Data/ApplicationDbContext.cs
- Zuni/Migrations/ApplicationDbContextModelSnapshot.cs
- Zuni/Program.cs
- Zuni/Controllers/EstudianteController.cs
- Zuni/Views/Estudiante/Evaluaciones.cshtml
- Zuni/Views/Estudiante/Resultados.cshtml
- Zuni/Views/Estudiante/_Navegacion.cshtml
- Zuni/Views/Estudiante/Index.cshtml (etiquetas de disponibilidad y enlace de consulta).
- Zuni/Views/Administrador/_Navegacion.cshtml

Los cambios previos del panel Administrador permanecen en el árbol de trabajo. Las herramientas de respaldo, generación y aplicación están en `.visual-check` y excluidas de Git. Los artefactos bin/obj generados por las pruebas son temporales, no código de la implementación.
