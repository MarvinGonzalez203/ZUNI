# Panel del psicólogo: calendario, niveles e integración

Cambios sobre feature/psychologist-appointments conservando dashboard, sidebar y rutas del compañero. Prototipo sin persistencia: no modifica PostgreSQL, migraciones, autenticación ni paneles de otros roles.

## Calendario y disponibilidad

`Views/Shared/_CalendarioDisponibilidad.cshtml` se incluye desde `Views/Psicologo/_Agenda.cshtml`, únicamente en /Psicologo/Agenda. El tablero conserva la bienvenida y el enlace Ver agenda; se retiraron sus tarjetas repetidas y la agenda completa. `wwwroot/js/psicologo.js` mantiene el estado en memoria, sin peticiones ni localStorage. Se reinicia al recargar o cambiar de página.

- Verde claro: rango configurado con al menos una cita completa futura disponible.
- Rojo claro: día bloqueado por el profesional o rangos de hoy sin espacios futuros.
- Neutro: sin definir. Los días pasados están deshabilitados.
- Añadir varios rangos por día sin cruces; se permiten rangos contiguos. Bloquear retira los horarios del día. Añadir un rango lo habilita otra vez. Quitar configuración devuelve el día a neutro.
- Jornada inicial: 08:00–18:00 en America/Guatemala; propuesta operativa ajustable, no norma clínica. Modificar conjuntamente data-opening/data-closing, min/max HTML y el texto visible del parcial.
- Validaciones puras en `disponibilidad-rules.js`: fecha válida, no pasada; inicio futuro si es hoy; horas dentro de jornada; final posterior al inicio; incrementos de cinco minutos; duración permitida; modalidad válida; rango suficiente para una cita completa; sin superposición.
- Duraciones 15, 30, 45, 50 y 60 minutos; predeterminada 50. Las opciones breves son propuestas para orientación/seguimiento. Atención individual habitual 45–50 min: [UCLA](https://counseling.ucla.edu/services/short-term-counseling-and-psychotherapy/) y [University of Florida](https://counseling.ufl.edu/services/individual/), consultadas 04/10/2026.

Al conectar reservas reales, descontar citas vigentes de los espacios generados. Verde si queda espacio, rojo si está lleno/bloqueado. El rojo del prototipo no representa reservas reales. Validar nuevamente en servidor y resolver concurrencia al reservar. No retirar rangos con citas sin resolver primero las reservas. Guardar instantes en UTC, mostrar hora institucional y verificar siempre autorización del profesional.

## Tres niveles de cuestionario

`_Resultados.cshtml` contiene un selector habilitado con Nivel 1, Nivel 2 y Nivel 3, todos pendientes. Son nombres provisionales, no niveles de gravedad ni diagnósticos. Su selección solo cambia un aviso: no genera resultados, fechas, puntuaciones ni respuestas. Reemplazar nombres/ids con los instrumentos oficiales cuando se definan. Las acciones de guardado clínico siguen deshabilitadas.

## Historial: entrega al compañero de Dirección

Archivos para reutilizar:

1. `Models/HistorialAtencionViewModel.cs`: contrato `HistorialAtencionItem`.
2. `Views/Shared/_HistorialAtencion.cshtml`: único componente para tabla y detalle.
3. `Views/Psicologo/_Historial.cshtml`: ejemplo de consumo con lista vacía.

El panel del Director no se modificó. El compañero debe incluir el mismo parcial en su panel, no copiar sus tablas a otra vista:

```cshtml
<partial name="_HistorialAtencion" model="Model.Atenciones" />
```

`Model.Atenciones` debe ser una IReadOnlyList<HistorialAtencionItem>, procedente del mismo servicio/repositorio que usa Psicólogo, con permisos por registro aplicados antes de proyectar. Mismo Id para una atención en ambos paneles; nunca duplicar las atenciones por rol. Conectar /Psicologo/Historial al ViewModel y sustituir la lista vacía del prototipo.

Campos y detalle compartidos: fecha, estudiante, código, profesional, modalidad, reseña compartida, recomendaciones compartidas, acuerdos y seguimiento. Orden por fecha descendente; mostrar fechas en UTC-6 (Guatemala). Dirección y Psicólogo deben ver idénticos datos para una atención autorizada, aunque el alcance de registros puede diferir según sus permisos.

La interpretación del profesional se mantiene exclusivamente en los formularios de Psicólogo, protegidos por [Authorize(Roles = "Psicologo")]. No está en el contrato ni en el componente compartido. Al integrar, guardar y servir esa interpretación mediante una entidad/proyección privada y comprobar tanto rol como relación con la atención. No enviarla a Dirección en JSON, campos ocultos o HTML. La reseña compartida debe redactarse pensando en el alcance de Dirección, sin mezclar notas privadas.

## Integración futura

Estudiante solicita un espacio disponible → Psicólogo recibe la solicitud → confirma/reprograma → Estudiante consulta el estado → Psicólogo completa la cita y registra atención → ambos paneles autorizados consultan el mismo historial compartido. El estudiante conserva su placeholder actual; no se modifica en esta etapa.

Sin backend, los filtros de agenda/búsqueda y los formularios clínicos son visuales. No hay estudiantes, citas, atenciones ni resultados ficticios precargados. Se conservó la prevención de envíos y el reinicio de formularios clínicos al cerrar sus modales; la disponibilidad pasó a un editor de agenda, por lo que se retiró su modal anterior. Se retiró el modal de historial duplicado en favor de detalles dentro del componente común.

## Validación

`dotnet build Zuni/Zuni.csproj --no-restore`

`node --test tests/disponibilidad.test.cjs`

Probar /Psicologo/Agenda: añadir rango → verde; cruce → error; bloquear → rojo; quitar → neutro; navegar meses; comprobar móvil. Probar /Psicologo/Resultados: las tres opciones funcionan, todas siguen pendientes y sin resultados.
