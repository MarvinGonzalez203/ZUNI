# Asignación manual y consulta de disponibilidad

El registro público crea cuentas de Estudiante. Un Administrador puede crear una
cuenta de Psicólogo en **Usuarios → Agregar usuario → Rol: Psicólogo**, o cambiar
una cuenta existente en **Usuarios → Estudiantes → Gestionar → Cambiar rol**.
La cuenta debe estar activa. Cambiar el rol invalida sus sesiones anteriores.

La relación se gestiona en **Usuarios → Estudiantes → Gestionar → Acciones →
Asignar psicólogo**, o en **Usuarios → Psicólogos → Gestionar → Acciones →
Asignar estudiantes**. Desde la ficha del psicólogo se busca por nombre o carné y
se muestran 25 estudiantes por página. Las cuentas y perfiles deben estar activos.
También se conserva el flujo de Solicitudes de atención para las solicitudes existentes.

Se permite vincular la cuenta antes de enviar una solicitud. Esa acción no fabrica
solicitudes, consentimientos ni verificaciones de edad. Para reservar o confirmar
atención se exige solicitud consentida, además de los requisitos habituales.
Una solicitud posterior reconoce el profesional previamente elegido por el Administrador.
Para cambiar o quitar el vínculo se utilizan Reasignar psicólogo o Finalizar asignación
en la misma pantalla. Se exige motivo administrativo, revisión de la asignación vigente,
auditoría e historial; las citas activas bloquean el cambio.

Se retiró el servicio de asignación automática y su llamada al enviar la solicitud.
 Incluso con un solo psicólogo, una cuenta sin vínculo previo permanece pendiente
de elección administrativa. No se modifican automáticamente asignaciones históricas.

**Guías y funciones** conserva su nombre, ubicación y catálogo. Se agregó una
explicación y preguntas desplegables dentro de la página con pasos y enlaces a las
funciones autorizadas para el rol actual. La búsqueda incluye las preguntas y
encuentra «asignar», «reasignar» y las instrucciones para crear psicólogos.

La disponibilidad pertenece al psicólogo y se consulta por su asignación vigente,
sin copiar horarios por estudiante. Una cita pendiente no oculta las otras franjas
libres del calendario del estudiante. Las cuentas sin asignación no ven horarios
ajenos. Las citas de otros estudiantes solo ocupan franjas sin revelar su identidad.

El calendario del estudiante consulta los cambios al volver a la pestaña y cada
30 segundos mientras está visible. No reemplaza ventanas abiertas de reserva ni
sus formularios. La reserva sigue validando en el servidor la disponibilidad real.

Validación: `dotnet run --project tests/Agenda.Offline -c Release --no-restore` y
`dotnet run --project tests/Seguridad.Offline -c Release --no-restore`.
La actualización automática se verifica con
`node tests/Seguridad.Offline/calendario-estudiante.checks.cjs`.
Las pruebas usan SQLite en memoria y un servidor HTTP local, sin PostgreSQL real.
Los nuevos casos cubren horarios de hoy compartidos por dos estudiantes,
privacidad de cuentas sin asignación, ocupación y liberación de franjas, solicitud
con un único psicólogo activo y búsqueda de guías por preguntas y por «asignar».
