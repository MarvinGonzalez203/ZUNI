# Propuestas de cita desde Dirección y privacidad del estudiante

Rama `fix/pkcs-security-update`. La primera versión es del 18/10/2026 y la revisión cruzada del 20/10/2026.

Este documento describe cómo Dirección propone citas sin poder identificar quién recibe atención psicológica. Se hizo sin cambios de esquema: no hay migraciones, columnas, estados nuevos en `EstadoCita`, cambios a `ApplicationDbContext` ni al snapshot.

## 1. Alcance real del Director

Dirección **coordina y propone citas nuevas** en un contexto institucional. Por ejemplo, un padre, madre o encargado puede pedir a Dirección que gestione una cita de orientación para un estudiante. Decidir si la cita se realiza le corresponde siempre al psicólogo.

**Dirección puede:**
- buscar estudiantes en el directorio institucional (nombre, carné y carrera o grado);
- elegir a un estudiante, a un psicólogo elegible y la fecha, hora, duración y modalidad;
- enviar la propuesta;
- consultar **solo sus propias propuestas**, con su estado administrativo y su historial de avisos;
- consultar la carga anonimizada de los psicólogos y las estadísticas protegidas.

**Dirección no puede:**
- consultar fichas, motivos de atención, notas, Big Five, cuestionarios, registros de atención, asistencia, contactos de emergencia ni historiales;
- confirmar, reprogramar, cancelar ni modificar ninguna cita, ni siquiera sus propias propuestas;
- marcar asistencia ni registrar una atención;
- crear asignaciones;
- ver la agenda identificable de los estudiantes atendidos;
- deducir información clínica por los mensajes de error (ver §5).

Las rutas `/Psicologo/*`, `/CitasProgramadas/*`, `/Reuniones/*`, `/Ubicaciones/*`, `/PruebasAplicadas/Resumen/*` y `/Psicologo/Expediente/*` le responden con acceso denegado.

## 2. Proponer no significa interferir en citas existentes

Una propuesta es **una cita nueva** en estado `Solicitada`, con `ProgramadorRol = "Director"`. Nunca modifica, cancela, sustituye, mueve ni elimina otra cita:

- El formulario (`PropuestaDirectorInput`) no admite Id de cita, revisión, estado, lugar ni enlace. Esos campos se ignoran aunque se manipule el POST.
- La franja del psicólogo se respeta: si se cruza con otra cita activa suya, con su almuerzo o con un día bloqueado, la propuesta se rechaza. Dirección no puede forzar una reserva.
- Solo el psicólogo destinatario decide: acepta, reprograma (con su pantalla y sus permisos) o rechaza.
- Una propuesta pendiente **no bloquea** la reserva normal del estudiante ni la programación de su psicólogo. Solo ocupa la franja del psicólogo destinatario. Si luego se cruza con otra cita del estudiante, el psicólogo no podrá aceptarla y deberá reprogramarla o rechazarla.

## 3. Flujo

```
Director propone → Psicólogo revisa → Acepta / Reprograma / Rechaza → aviso al estudiante y a Dirección
```

| Paso | Quién | Dónde | Resultado |
|---|---|---|---|
| 1 | Director | Agenda y citas → **Proponer cita** | Cita `Solicitada` de origen Dirección; avisos al psicólogo, al estudiante y registro propio |
| 2a | Psicólogo | Agenda → **Aceptar cita** (o confirmar con lugar o enlace) | `Confirmada`, si hay asignación vigente y se cumplen las reglas |
| 2b | Psicólogo | Agenda → **Reprogramar cita** | Nueva fecha; sigue siendo propuesta de Dirección |
| 2c | Psicólogo | Agenda → **Rechazar propuesta** (motivo de la lista) | `Rechazada` |
| 3 | Estudiante | Mis citas y Notificaciones | Ve la propuesta y puede cancelarla (flujo existente) |
| 4 | Director | **Mis propuestas** y el detalle de cada una | Estado administrativo e historial |

**Estados que ve Dirección** (derivados de `Cita.Estado`, sin valores nuevos):

| `Cita.Estado` | Lo que ve Dirección |
|---|---|
| `Solicitada`, fecha futura | Pendiente de revisión (o «Reprogramada · pendiente de confirmación») |
| `Solicitada`, fecha pasada | Sin respuesta · fecha vencida |
| `Confirmada` / `Reservada` | Aceptada · confirmada (o «Reprogramada · confirmada») |
| `Rechazada` | Rechazada, con un motivo administrativo neutral |
| `Cancelada` | Cancelada, sin motivo |
| `Completada` / `NoAsistio` | Fecha concluida |

Una respuesta duplicada o tardía se rechaza por la revisión obligatoria y por `AgendaReglas.ValidarCambio`. Un doble envío no crea dos propuestas, porque `OperacionId` es el Id de la cita.

## 4. Protección estadística

Toda la protección se aplica en el servidor (`Services/Reportes/SupresionEstadistica.cs` y `EstadisticasDirector.cs`). La vista recibe solo lo que puede publicarse.

### 4.1 Supresión primaria

Ninguna cifra de personas entre 1 y 4 se publica: se muestra «menos de 5». Esto aplica al total de solicitudes, al de estudiantes con citas concluidas y a cada celda de un desglose.

### 4.2 Supresión complementaria

Si solo se ocultara la celda pequeña, podría calcularse restándola del total. Por eso:

1. Las celdas ocultas se agrupan en «Otras carreras (agrupadas)» u «Otros estados».
2. Si el grupo suma menos de 5 o tiene una sola celda, se le añade la celda visible más pequeña, y se repite hasta que el grupo llegue al umbral.
3. Si no queda ninguna celda visible, el desglose no se publica («Desglose no disponible») y solo se muestra el total.

Con esto, la suma de lo publicado (celdas visibles más el grupo) es igual al total. Nada pequeño queda por diferencia y no se altera ni se inventa ninguna cifra.

**Ejemplos (probados en `EstadisticasPrivacidadChecks`):**

| Datos | Publicado |
|---|---|
| 11 = 10 + 1 | Solo el total 11; desglose no disponible |
| 20 = 16 + 4 | Solo el total 20 |
| 10, 6, 2, 2, 1 | 10 y 6 visibles; «Otras» = 5 (tres carreras) |
| 12, 7, 3 | 12 visible; «Otras» = 10 (7 se suprime para proteger el 3) |
| Una sola carrera con 9 | 9 visible |
| Una sola carrera con 3 | Total «menos de 5»; sin desglose |

### 4.3 Periodos y filtros

- **Indicadores de personas hasta el cierre del mes anterior.** Las solicitudes, los estudiantes con citas concluidas, el desglose por carrera y la tendencia mensual se calculan con datos hasta el cierre del mes anterior. No cambian con cada cita del mes en curso, así que no sirven para comparar antes y después de una fecha concreta, por ejemplo la de una propuesta.
- **Asistencia no publicada.** Completadas e inasistencias se publican **juntas como «concluidas»** en Estadísticas, en la carga por psicólogo y en las métricas del calendario. Antes, comparar «Inasistencias» o «Completadas» antes y después de la fecha de una propuesta revelaba si ese estudiante asistió.
- **Sin filtros.** El endpoint no admite filtros (carrera, periodo, psicólogo ni estado). Los parámetros en la URL se ignoran, así que no hay combinaciones de filtros que comparar.

## 5. Inferencias sobre el estudiante

**Antes de esta revisión:** al proponer se comprobaba si el estudiante tenía otra cita ese día o que se cruzara. El mensaje era genérico, pero la diferencia entre éxito (302 y detalle) y rechazo (409) permitía sondear la agenda del estudiante, fecha por fecha.

**Ahora:**

- **Al proponer no se consulta la agenda del estudiante.** Solo se revisa:
  - que el estudiante sea una cuenta activa del directorio;
  - que tenga la mayoría de edad verificada (regla vigente);
  - el **choque exacto de hora** con otra cita activa suya. Esta comprobación la impone el índice único `(estudiante, inicio)` y no puede quitarse sin migración.

  Cualquiera de estos tres produce el mismo mensaje genérico y el mismo código 409.
- **Las reglas de agenda del estudiante se aplican al aceptar** (`ValidarConfirmacionProgramada`): una cita activa por día y sin cruces. Ese error solo lo ve el psicólogo asignado.
- **Motivo de rechazo neutral.** «El estudiante debe completar requisitos administrativos en ZUNI» se le muestra a Dirección como «No es posible confirmarla por ahora», para no revelar si falta una solicitud o una asignación. El estudiante y el psicólogo ven el motivo completo.
- **Sin rastros en otras pantallas.** El directorio, el calendario y las estadísticas no consultan ni muestran atención, asignaciones ni citas por estudiante.
- **Límite de solicitudes:** se mantiene `director-propuestas`, con 60 peticiones por IP cada 10 minutos.

**Inferencias residuales** (no eliminables con el modelo actual):

1. **Choque exacto de hora.** Una propuesta con exactamente la misma hora de inicio que otra cita activa del estudiante falla con el mensaje genérico. Probando horas exactas podría deducirse que hay una cita a esa hora, aunque no con quién. Eliminarlo requiere cambiar el índice único: es una migración.
2. **Aceptación.** Que una propuesta se acepte implica que el estudiante quedó asignado a ese psicólogo. Es inherente: aceptar la cita es el propósito.
3. **Mayoría de edad.** El mensaje genérico incluye «mayoría de edad no verificada». En estudiantes antiguos, que declararon su edad al solicitar atención, la verificación puede correlacionar con haber pedido atención.
4. **Diferencias entre meses cerrados.** Comparar un mes cerrado con el siguiente puede revelar variaciones pequeñas en totales acumulados. La supresión reduce ese riesgo, pero no lo elimina sin añadir ruido, y añadir ruido sería alterar las cifras.

## 6. Propuestas sin asignación vigente

Crear una propuesta no exige asignación previa: exigirla revelaría a Dirección quién está en atención. **Aceptarla sí la exige**, y la autorización clínica no se omite.

**Recorrido administrativo:**

1. Dirección propone y el psicólogo recibe la propuesta.
   - Si el estudiante no tiene una asignación vigente con él, la agenda **oculta Aceptar** y muestra: «Falta una asignación vigente: el estudiante debe registrar su solicitud de atención en ZUNI y Administración debe asignártela en Solicitudes de atención…».
   - No hay enlace al módulo de Administración, porque el psicólogo no tiene acceso a él. Solo se dan instrucciones.
2. El estudiante recibe un aviso que le indica registrar su solicitud desde **Solicitar atención**, con consentimiento y mayoría de edad. Puede indicar que la registra por referencia de Dirección.
3. Administración ve en **Solicitudes de atención → Gestionar solicitud** qué psicólogo y qué fecha propuso Dirección. Asigna la solicitud con el módulo existente.
   - Las propuestas pendientes de Dirección **no bloquean** Asignar, Reasignar, Finalizar ni Cerrar. Sin asignación vigente, su psicólogo no puede aceptarlas; solo rechazarlas.
4. Con la asignación vigente, el psicólogo puede **aceptar** desde su agenda si la fecha sigue vigente. Si no, puede reprogramar o rechazar.

**Nunca quedan bloqueadas:**
- el psicólogo destinatario siempre puede rechazar la propuesta, incluso con la fecha vencida;
- Dirección ve «Sin respuesta · fecha vencida» y puede proponer otra fecha.

**Lo que no se hace:** Dirección no crea asignaciones; no se activan cuentas, no se crean asignaciones ficticias y no se omite ninguna verificación de edad, privacidad ni consentimiento.

### Responsabilidades

- **Psicólogo:** revisa, acepta, reprograma o rechaza con un motivo administrativo de la lista; nunca escribe texto libre que Dirección pueda ver.
- **Administrador:** asigna, reasigna o finaliza solicitudes en el módulo existente; ve solo metadatos.
- **Dirección:** propone y consulta el estado administrativo de sus propuestas.

## 7. Notificaciones y auditoría

Se reutiliza `NotificacionesCita`, sin correo, WhatsApp ni SMS.

- **Psicólogo:** «Nueva propuesta de Dirección», con estudiante, fecha, modalidad y duración. También indica cómo continuar si falta la asignación.
- **Estudiante:** recibe un aviso al crearse la propuesta (con el paso «Solicitar atención») y otro cuando se acepta, se rechaza (con el motivo completo) o se reprograma.
- **Dirección:** propuesta enviada, aceptada, reprogramada (con fecha anterior y nueva), rechazada (con motivo neutral) o cancelada (sin motivo).

**Auditoría** en `AuditoriaUsuarios`, que guarda solo el estudiante afectado, el actor, la acción y el Id de la cita (cifrado en reposo):
- `PROPUESTA_DIRECTOR_CREADA`
- `PROPUESTA_DIRECTOR_ACEPTADA`
- `PROPUESTA_DIRECTOR_REPROGRAMADA`
- `PROPUESTA_DIRECTOR_RECHAZADA`
- `PROPUESTA_DIRECTOR_CANCELADA`

## 8. Mayoría de edad y menores

**Restricciones actuales (sin cambios):**
- proponer exige mayoría de edad verificada;
- aceptar exige, además, el perfil completo;
- reservar sigue exigiendo la mayoría de edad;
- la solicitud de atención exige consentimiento.

ZUNI no atiende menores de edad.

**Pendiente para instituciones con menores** (no implementado; requiere tablas nuevas y revisión legal):
- identificación del padre, madre o encargado y su relación con el estudiante, verificada por la institución, sin almacenar DPI ni imágenes;
- consentimiento del responsable, versionado y revocable, y asentimiento del estudiante;
- políticas de privacidad específicas y reglas por institución sobre quién solicita, qué se informa al responsable y qué se reserva al profesional;
- autorización explícita para atención psicológica educativa, con registro de quién la otorgó.

## 9. Pendientes

- **Debian/PostgreSQL:**
  - verificar la concurrencia `SERIALIZABLE` de las propuestas y la aceptación;
  - verificar el índice único con el choque exacto de hora;
  - comprobar los permisos del usuario de ejecución sobre `AuditoriaUsuarios` y `NotificacionesCita`;
  - medir el rendimiento de las consultas de estadísticas con datos reales.
- **Dirección no puede retirar una propuesta pendiente.** Hacerlo no requiere esquema; no se agregó porque no estaba en el alcance pedido.
- **Scripts de navegador:** `*.browser.cjs`, entre ellos `coordinacion.browser.cjs`, no se actualizaron.
