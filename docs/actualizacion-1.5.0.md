# ZUNI 1.5.0 — acceso, ausencias profesionales y Denis Corp

## Portada de Denis Corp

La raíz `/` y `/deniscorp/` presentan a Denis Corp y tres tarjetas de programas.
ZUNI conserva sus rutas de acceso, registro, paneles, guías y documentos; su
presentación anterior se encuentra en `/deniscorp/zuni/`. Los enlaces de inicio
del sitio público de ZUNI llevan a esa presentación. El inicio del panel conserva
el acceso a la portada corporativa.

`/deniscorp/academy/` presenta Denis Academy como aprendizaje de inglés y español.
`/deniscorp/fulleo/` presenta Fulleo como consulta de precios de gasolina,
según lo confirmado por el propietario y su proyecto en `C:\U\10mo\Proyect G II`.
Ambas páginas indican que el acceso público está en preparación.
Estos enlaces no publican las otras aplicaciones ni comparten sus cuentas con
ZUNI. La portada corporativa no consulta perfiles ni preferencias institucionales.

Son rutas HTTP del servidor existente: esta implementación no cambia DNS,
Namecheap ni certificados. Para alojar Academy bajo esa ruta hace falta preparar
su compilación y sus rutas de recursos/service worker para el prefijo, además de
configurar el servidor web. No se copiaron código ni datos de otros proyectos.

## Verificación en dos pasos

Disponible de forma voluntaria desde **Mi cuenta → Configurar verificación en
dos pasos (2FA)** para los perfiles que pueden iniciar sesión. Usa códigos TOTP
de seis dígitos, HMAC-SHA1 y periodos de 30 segundos, con tolerancia de un periodo
a cada lado. Se comprueban los vectores oficiales del RFC 6238.

Se confirma la contraseña actual para preparar una clave manual en la aplicación
autenticadora. La preparación vence en diez minutos y solo queda activada después
de comprobar el primer código. No requiere un proveedor de correo ni envía claves
a un servicio externo de QR. Es independiente del PIN profesional.

Una contraseña correcta crea un reto de cinco minutos; no emite una sesión
autenticada. La cookie del reto es HttpOnly, SameSite=Strict, Secure fuera de
desarrollo y restringida a `/Cuenta`. El servidor almacena únicamente la huella
del token aleatorio, vinculada al SecurityStamp de la cuenta. Un reto nuevo
reemplaza el anterior. Se consume al verificarlo correctamente. Los códigos TOTP
ya aceptados no pueden reutilizarse.

Se entregan diez códigos de recuperación de 80 bits, mostrados una sola vez y
almacenados como huellas SHA-256. Cada código se consume de manera atómica.
La revisión de concurrencia impide dos consumos simultáneos válidos. El secreto
del autenticador está cifrado con Data Protection y un propósito por usuario.
Los respaldos deben conservar la base y las claves persistentes de Data Protection.

Cinco errores bloquean durante quince minutos, de forma durable e independiente
del contador de contraseña. Volver a introducir la contraseña correcta no elimina
ese bloqueo. Preparar o modificar el 2FA también comprueba la contraseña; para
desactivar o renovar códigos exige además TOTP o un código de recuperación.
Activar, renovar y desactivar rotan el SecurityStamp y cierran las sesiones.
Los cambios de configuración se auditan sin guardar secretos ni códigos usados.
Los formularios usan antiforgery y límites de acceso; las respuestas son no-store.

Restablecer la contraseña no desactiva el 2FA. La recuperación automática del
acceso se realiza con los códigos de respaldo. No existe un botón administrativo
que permita retirar esta protección sin verificar la identidad; una recuperación
excepcional si se pierden ambos factores requiere definir ese procedimiento.
Brevo continúa pendiente de credenciales y remitente verificado para el PIN
profesional; esta actualización no activa el remitente de ejemplo.

## Ausencias del profesional

En la agenda y en cada día del calendario, una cita programada pendiente de
atención ofrece **No podré asistir**. El profesional revisa la cita y confirma.
También permite documentar una ausencia imprevista después del horario, siempre
que no exista ya un registro de atención. No se usa para atenciones imprevistas
ya realizadas ni para citas terminadas o canceladas.

Se registra el estado **Ausencia del profesional** (valor persistido 7), separado
de **No asistió**, que corresponde al estudiante. En la misma transacción se
guarda una notificación sin leer para el alumno propietario y la auditoría.
El contador de avisos se consulta cada 45 segundos mientras la pestaña esté
visible, sin cargar datos personales y sin renovar la sesión por esa consulta.
No se solicita ni almacena información médica ni personal del psicólogo. Las
propuestas históricas de Dirección reciben únicamente el aviso administrativo
de cancelación. No se generan registros clínicos ni consentimientos ficticios.

La hora original continúa ocupada para el profesional, para impedir que otro
alumno reserve ese intervalo. Libera al estudiante. Los enlaces virtuales de la
cita dejan de estar disponibles. Para varios estudiantes afectados, se avisa en
cada cita; bloquear el resto del día no cancela ni notifica las citas existentes.

**Consultar aviso y nueva fecha → Programar nueva fecha y notificar** prepara
una cita nueva, con la asignación vigente y las reglas habituales. Conserva
`SustituyeCitaId`, un vínculo único con la cita original, que mantiene su fecha
y estado en el historial. La nueva fecha genera otro aviso. No se reutiliza
un enlace virtual anterior. Una revisión obsoleta o un doble envío no duplica
avisos ni citas. Otro psicólogo o estudiante no puede operar estas acciones.

## Migración y publicación

Migración EF: `20261117015815_AgregarDobleFactorYAusencias`. Añade la tabla
`DobleFactor`, amplía el CHECK de estados de citas y agrega el vínculo de sustitución
con índice único y FK restrictiva. No cambia roles ni asignaciones existentes.

Antes de publicar, respaldar base y claves, probar el SQL en una restauración
aislada y conceder al usuario de ejecución SELECT/INSERT/UPDATE sobre `DobleFactor`.
El usuario de migraciones conserva la propiedad del esquema. Si el servidor sigue
en 1.3.3, incluir también la migración de PIN de 1.4.0 y sus permisos. No basta
con sustituir los binarios. Para revertir después de usar el estado 7 o activar
2FA, restaurar un respaldo compatible; no volver a los binarios antiguos, que
desconocen el segundo factor y el nuevo estado.

La versión de proyecto y la campana se actualizan a 1.5.0, con las últimas tres
entregas visibles. Las guías incluyen 2FA y ausencias con preguntas y accesos.
Los commits se documentan con título, alcance, validación y estado de publicación;
GitHub Desktop los muestra en el historial de `feature/director-V2`.

Esta entrega no afirma estar publicada en zuni.lat. El paquete 1.4.0 preparado
anteriormente no contiene estas funciones; requiere una publicación posterior
de 1.5.0 con migraciones y comprobaciones.

## Verificación realizada

- Compilación Release sin errores ni advertencias.
- Suite completa `Seguridad.Offline`: módulos de roles, privacidad, agenda,
  contratos, PIN previo, sesión y versión, más 176 comprobaciones del núcleo.
- `Seguridad.Offline --mejoras-15`: 56 comprobaciones específicas de TOTP,
  sesión pendiente, códigos de recuperación, bloqueo, renovación, desactivación,
  permisos de ausencia, historial, notificaciones y contador sin renovación de sesión.
- `Agenda.Offline`: 321 comprobaciones con SQLite ficticio.
- `Seguridad.Offline --mejoras-14`: 116 comprobaciones de guías, contratos,
  PIN profesional, bloqueo y recuperación con correo ficticio.
- `mejoras-15.browser.cjs`: 74 comprobaciones, anchos de 360, 390, 768 y 1366 px,
  tarjetas, formularios, foco, ausencia de desbordamiento y avisos sin recargar.
- `historial-estilo.browser.cjs`: 45 comprobaciones de campana, tamaños, temas,
  lectura por versión y traducción al inglés.
- Migración aplicada solo en PostgreSQL de localhost después de verificar un
  respaldo privado. Se conservaron las claves originales.

Las pruebas HTTP usan usuarios ficticios y SQLite en memoria. No se enviaron
correos, no se modificó producción y no se afirma haber validado concurrencia real
de PostgreSQL con usuarios simultáneos. Las capturas locales usan datos ficticios.
