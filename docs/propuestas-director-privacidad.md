# Propuestas de cita desde Dirección y privacidad del estudiante (18/10/2026)

Rama `fix/pkcs-security-update`. Recupera la función de Marlon para que el Director proponga citas y la rediseña para que Dirección no pueda identificar a quién se atiende. No cambia el esquema: no hay migraciones, columnas, estados nuevos en `EstadoCita` ni cambios a `ApplicationDbContext` o al snapshot.

## 1. Por qué Dirección propone citas

En la institución, un padre, madre o encargado (o la propia Dirección) puede pedir que se gestione una cita de orientación para un estudiante. Dirección necesita una vía segura para:

- proponer la cita;
- saber si el psicólogo la aceptó, la reprogramó o la rechazó.

La decisión sobre la cita es siempre del psicólogo. ZUNI se diseña para poder llevarse a otras instituciones, incluidos colegios, por eso los textos hablan de «carrera o grado» y de «Dirección», sin suponer una universidad.

## 2. Flujo

```
Director → Proponer cita → Psicólogo revisa → Aceptar / Reprogramar / Rechazar → aviso al estudiante y a Dirección
```

| Paso | Quién | Pantalla / acción | Resultado |
|---|---|---|---|
| 1 | Director | `Director/ProponerCita` (menú **Agenda y citas → Proponer cita**) | Cita `Solicitada` con `ProgramadorRol = "Director"`; avisos al psicólogo, al estudiante y registro propio del Director |
| 2a | Psicólogo | Agenda → **Aceptar cita** (o confirmar con lugar o enlace) | `Confirmada` |
| 2b | Psicólogo | Agenda → **Reprogramar** (`CitasProgramadas/Editar`) | Nueva fecha; sigue siendo propuesta de Dirección |
| 2c | Psicólogo | Agenda → **Rechazar propuesta** con un motivo de la lista | `Rechazada` |
| 3 | Estudiante | **Mis citas** y **Notificaciones** | Ve la propuesta («Propuesta de Dirección · pendiente de revisión del psicólogo») y puede cancelarla, como cualquier cita activa |
| 4 | Director | `Director/Propuestas` y `Director/Propuesta/{id}` | Estado administrativo, fecha actual o definitiva e historial de avisos |

Dirección no puede confirmar, reprogramar, cancelar, marcar asistencia ni registrar la atención. El formulario solo acepta estudiante, psicólogo, fecha, hora, duración y modalidad (`PropuestaDirectorInput`). Las acciones de agenda del psicólogo (`/Psicologo/*`, `/CitasProgramadas/*`, `/Reuniones/*`, `/Ubicaciones/*`) siguen exigiendo el rol Psicólogo.

## 3. Datos que Dirección puede consultar

- **Directorio** (al buscar): nombre, carné y carrera o grado de estudiantes activos. La búsqueda exige al menos 3 caracteres y devuelve como máximo 10 resultados. No consulta solicitudes, asignaciones, expedientes, citas, Big Five ni cuestionarios, de modo que el resultado es igual para quien recibe atención y para quien no.
- **Sus propias propuestas** (y solo esas):
  - datos del estudiante: nombre, carné y carrera;
  - datos de la cita: psicólogo, fecha y hora (las definitivas cuando están confirmadas), duración, modalidad y estado administrativo;
  - motivo de rechazo, solo si es uno de la lista cerrada;
  - historial de avisos administrativos.
- **Carga de psicólogos**: sigue anonimizada (bloques «Ocupado» o «Pendiente de revisar»), incluidas sus propias propuestas.
- **Estadísticas**: siguen siendo agregadas.

## 4. Datos psicológicos restringidos

Dirección nunca accede a:

- ficha de acompañamiento, motivo de atención ni contactos de emergencia;
- notas profesionales privadas, registros de atención ni asistencia;
- resultados Big Five individuales, respuestas o resúmenes de cuestionarios, ni diagnósticos;
- historial psicológico;
- solicitudes de atención, propias o de otros estudiantes;
- listados de quién recibe atención;
- lugar ni enlace de la cita, ni el motivo de cancelación escrito por el estudiante o el psicólogo.

El servidor lo garantiza de cuatro formas:

- las consultas de Dirección solo proyectan los campos anteriores;
- cada propuesta se filtra por `ProgramadorUsuarioId` del Director autenticado; una propuesta ajena devuelve acceso denegado;
- la bandeja general de notificaciones sigue cerrada para Dirección;
- las rutas clínicas exigen el rol Psicólogo o la autorización clínica.

## 5. Validaciones al crear una propuesta (en el servidor)

Todo ocurre dentro de una transacción `SERIALIZABLE` (`AgendaCitasService.Escribir`), con el mismo control de concurrencia que el resto de la agenda.

1. Director activo, con sesión y sello de seguridad vigentes (`MayoriaEdadService.ActorAsync`).
2. Datos completos: duración de 15 a 120 minutos en múltiplos de cinco, modalidad válida y hora en múltiplos de cinco minutos.
3. Operación idempotente: `OperacionId` se usa como Id de la cita, así que un doble envío no crea dos propuestas.
4. Psicólogo elegible: activo, sin cambio de contraseña pendiente y sin rol institucional. Es el mismo criterio de la carga de psicólogos.
5. Franja del psicólogo (estos motivos sí se explican, porque ya se ven en la carga anonimizada):
   - fecha futura dentro de 12 meses, entre 08:00 y 18:00 del mismo día;
   - día no bloqueado;
   - sin cruzar el almuerzo;
   - sin otra cita activa que se cruce.
6. Que el Director no tenga ya una propuesta pendiente para ese estudiante.
7. Estudiante elegible. Todas estas causas producen **un único mensaje genérico**:
   - activo y con rol Estudiante;
   - **mayoría de edad verificada**;
   - sin otra cita activa ese día ni cruce de horario.

   El mensaje es: «No es posible registrar la propuesta para este estudiante en esa fecha. Elige otra fecha u horario; si persiste, coordina con el área de psicología».

**Crear la propuesta no exige una asignación previa** del estudiante a ese psicólogo. Si la exigiera, el error revelaría quién recibe atención. La asignación se exige al **aceptar**, como hoy (`GestionarCita`, `Lugares` y `Reuniones` usan `Asignado`). Si el estudiante aún no tiene una solicitud asignada a ese psicólogo, la agenda lo indica y oculta **Aceptar**. El psicólogo destinatario puede rechazar la propuesta aunque no haya asignación, para que no quede atascada.

**Límite de solicitudes:** la búsqueda y el envío usan la política `director-propuestas`, que permite 60 peticiones por IP cada 10 minutos. Se configura con `Zuni:LimitesAcceso:PropuestasDirectorPorDiezMinutos`.

## 6. Estados (sin valores nuevos en el enum)

| `Cita.Estado` | Lo que ve Dirección |
|---|---|
| `Solicitada` | Pendiente de revisión (o «Reprogramada · pendiente de confirmación») |
| `Confirmada` / `Reservada` | Aceptada · confirmada (o «Reprogramada · confirmada») |
| `Rechazada` | Rechazada (con motivo de la lista) |
| `Cancelada` | Cancelada (sin motivo) |
| `Completada` / `NoAsistio` | Fecha concluida: la asistencia es un registro privado |

Una respuesta duplicada o tardía no cambia el estado de forma inconsistente:

- `GestionarCita` exige la revisión vigente para propuestas de Dirección;
- `AgendaReglas.ValidarCambio` solo permite confirmar o rechazar desde `Solicitada` y solo citas futuras;
- `OperacionId` evita duplicados al crear.

## 7. Reprogramación

El psicólogo reprograma con la pantalla existente (`CitasProgramadas/Editar?citaId=`):

- aplica las mismas reglas de franja, almuerzo y cruces, y exige la asignación vigente;
- la cita **conserva su origen**: `ProgramadorUsuarioId` y `ProgramadorRol = "Director"`. Es la misma cita, así que no se crea una segunda cita activa;
- el estado lo decide el flujo actual: queda confirmada si es presencial con lugar y el perfil está completo y verificado; si no, queda pendiente.

Avisos:

- **Estudiante:** «Cita reprogramada por Psicologo…», como antes.
- **Dirección:** «Propuesta reprogramada por …», con la **fecha anterior** y la **nueva**. Es la traza de la propuesta original.

## 8. Notificaciones

Se reutiliza `NotificacionesCita`; no se envía correo, WhatsApp ni SMS.

- **Psicólogo:** «Nueva propuesta de Dirección. Se ha propuesto una cita para el estudiante … (carné …) el dd/MM/aaaa a las HH:mm… Acciones: Aceptar, Reprogramar o Rechazar desde tu agenda.» La agenda la marca como **Propuesta de Dirección**.
- **Estudiante:**
  - al crearse: propuesta pendiente de revisión;
  - al aceptarse o rechazarse: respuesta del psicólogo;
  - al reprogramarse: nueva fecha.
- **Dirección:** Propuesta enviada (registro propio, ya leído), aceptada, reprogramada, rechazada (con motivo de la lista) o cancelada (sin motivo). El contador del encabezado enlaza a **Mis propuestas**. Los avisos se marcan como leídos al abrir el detalle.
  - Los avisos antiguos de tipo «Respuesta», que podían llevar texto libre, no se muestran a Dirección.

**Motivos de rechazo** (lista cerrada, `MotivosPropuestaDirector`):

- Horario no disponible
- Se requiere coordinar otra fecha
- El estudiante debe completar requisitos administrativos en ZUNI
- Propuesta duplicada
- No corresponde al servicio de orientación psicológica

## 9. Auditoría

Se usa `AuditoriaUsuarios` mediante `PrivacidadService.Auditar`. Solo guarda el usuario afectado (el estudiante), el actor, la acción, la fecha y el Id de la cita, sin notas, motivos ni contenido de la ficha.

| Acción | Actor |
|---|---|
| `PROPUESTA_DIRECTOR_CREADA` | Director |
| `PROPUESTA_DIRECTOR_ACEPTADA` | Psicólogo |
| `PROPUESTA_DIRECTOR_REPROGRAMADA` | Psicólogo |
| `PROPUESTA_DIRECTOR_RECHAZADA` | Psicólogo |
| `PROPUESTA_DIRECTOR_CANCELADA` | Estudiante o psicólogo |

## 10. Restricciones actuales de mayoría de edad

No se relajan:

- proponer exige mayoría de edad verificada;
- aceptar exige, además, el perfil completo (`ValidarConfirmacionProgramada`);
- reservar sigue exigiéndola al estudiante.

Un menor no puede completar la atención.

## 11. Preparación conceptual para instituciones con menores (no implementado)

La propuesta de Dirección ya separa quién propone, quién decide y a quién se informa. Para atender a menores falta implementar, antes de relajar la verificación de edad:

- identificación del padre, madre o encargado y su relación con el estudiante (verificada por la institución);
- consentimiento informado del responsable, versionado y revocable, y asentimiento del estudiante cuando corresponda;
- políticas de privacidad específicas para menores y sus responsables;
- reglas por institución: edades, quién puede pedir una cita, qué se informa al responsable y qué se reserva al profesional;
- autorización explícita para atención psicológica educativa y registro de quién la otorgó.

Todo esto exige cambios de esquema (nuevas tablas) y revisión legal. No se implementó de forma improvisada.

## 12. Limitaciones pendientes

- **Inferencia residual:**
  - El mensaje genérico mezcla varias causas, pero un Director que pruebe muchas fechas podría deducir que el estudiante tiene alguna cita activa un día concreto (no con quién ni por qué).
  - Los estudiantes antiguos con mayoría de edad verificada pueden correlacionar con haber solicitado atención en el pasado.
  - El límite de solicitudes reduce el sondeo, pero no lo elimina.
- **Asignación:** si el estudiante no tiene una solicitud de atención asignada al psicólogo, la propuesta no puede aceptarse. El psicólogo la rechaza con «El estudiante debe completar requisitos administrativos en ZUNI» y el estudiante debe solicitar atención desde ZUNI.
- **Sin retirar propuestas:** Dirección no puede retirar una propuesta enviada. Debe esperar la respuesta o pedir al psicólogo que la rechace.
- **Sin aceptación del estudiante:** el estudiante no tiene un botón de «aceptar» la propuesta, porque el flujo actual no lo tiene. Puede cancelarla.
- **Directorio con carné:** la búsqueda por carné requiere al menos 3 dígitos. No hay paginación de resultados: hay que precisar la búsqueda.
- **Scripts de navegador sin actualizar:** `tests/Seguridad.Offline/coordinacion.browser.cjs` y otros `*.browser.cjs` no se actualizaron.
