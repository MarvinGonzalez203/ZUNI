# Citas virtuales y retirada de Catedrático

R06 y R07, implementados en `feature/director-V2` el 14 de octubre de 2026.

## Preparar una cita virtual

1. El psicólogo abre **Mi agenda → Preparar y confirmar cita virtual**.
2. Crea una reunión individual de Google Meet en su cuenta institucional e invita únicamente al estudiante de esa cita. Configura el acceso restringido y la admisión por el anfitrión; cuando su cuenta lo permita, desactiva solicitudes de personas no invitadas. Comprueba el resultado con una cuenta externa.
3. Pega el enlace `https://meet.google.com/abc-defg-hij`, marca la revisión de acceso y pulsa **Guardar y confirmar cita virtual**. **Guardar enlace** permite preparar la reunión sin confirmar todavía.
4. El estudiante abre **Mis citas → Consultar acceso virtual**. El ingreso se habilita 15 minutos antes del inicio y termina al finalizar el horario. También puede ingresar el psicólogo asignado. Se recomienda un lugar privado y sin ruido, audífonos y no compartir el enlace.

El formato admite solamente enlaces individuales HTTPS de Google Meet, sin parámetros, fragmentos o dominios alternativos. Un enlace usado no puede asignarse a otra cita, incluso si fue sustituido o la cita terminó. Las citas presenciales siguen su flujo habitual.

ZUNI comprueba la sesión vigente, los permisos persistidos, el estudiante correspondiente y su asignación actual al psicólogo. Rechaza entradas de otras cuentas y de citas canceladas, rechazadas, sin enlace o fuera del horario. Los enlaces no aparecen en los listados ni en el registro de auditoría; el psicólogo puede consultar y editar el enlace de su propia cita. La entrada autorizada redirige a Google, por lo que ambos participantes pueden conocer la URL.

ZUNI no crea reuniones mediante la API de Google ni modifica los permisos de Meet. La confirmación del formulario registra la revisión declarada por el psicólogo; no certifica los permisos externos. Cancelar en ZUNI cierra su acceso desde la aplicación, pero el anfitrión debe administrar también la reunión externa. Consulta la [guía oficial de controles de Meet](https://support.google.com/a/users/answer/11989526?hl=es), cuyas opciones dependen de la cuenta institucional. No se habilitan grabación ni transcripción desde ZUNI.

## Revisar las cuentas de Catedrático

**Administración → Gestión → Revisar roles retirados** conserva el listado de cuentas históricas. El rol Catedrático ya no se ofrece en altas o cambios, ni se crea al inicializar una instalación nueva. Las rutas del antiguo panel devuelven un aviso de función retirada, incluso con una sesión anterior.

Las cuentas y sus datos no se eliminan ni reciben un rol nuevo automáticamente. Una cuenta que conserva únicamente el rol retirado recibe el aviso al ingresar y puede acceder a **Mi cuenta** y al centro de derechos. Administración revisa cada caso y, si corresponde, realiza un cambio explícito a un rol vigente con su auditoría habitual. Una cuenta con Administración o Director conserva ese panel; los controles existentes impiden el acceso clínico de roles incompatibles.

## Instalación y verificación

La migración `20261014145344_AddReunionesPrivadas` agrega enlace, fecha de preparación y revisión de concurrencia a las citas, junto con el registro de huellas para impedir reutilización. No modifica tablas Identity, cuentas o roles existentes. Las citas antiguas quedan sin enlace hasta que su profesional lo prepare; no se inventan reuniones.

Se aplicó a `ZuniDb` local después de guardar y verificar el respaldo `zuni-antes-reuniones-retiro-20261014.dump`. Se conservaron los conteos de usuarios, asignaciones de rol, citas, perfiles y solicitudes. Para cargar el cambio en una instancia abierta desde Visual Studio, reinicia la aplicación.

Las pruebas offline utilizan datos ficticios y cubren enlaces maliciosos, propietarios y roles, confirmación obligatoria, horario de entrada, cancelación, edición simultánea, reutilización de enlaces actuales y sustituidos, auditoría sin URL, cierre de rutas retiradas y cambio institucional explícito. La verificación en navegador utiliza las vistas reales renderizadas. No se realizan reuniones externas ni se cambian permisos de Google mediante estas pruebas.

Verificación final: compilación Release sin errores ni advertencias; 403 comprobaciones en Seguridad.Offline (51 específicas de estos bloques), 314 en Agenda.Offline y 45 en navegador. Total: 762. La concurrencia PostgreSQL con solicitudes simultáneas sigue pendiente de ensayo en un entorno de pruebas; se verificaron el modelo, el control de revisión obsoleta y la migración local.
