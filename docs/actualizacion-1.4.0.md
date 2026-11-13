# ZUNI 1.4.0 - guías, contratos, reuniones y PIN profesional

La búsqueda de guías compara palabras sin acentos, equivalencias fonéticas
(h muda, b/v, s/z/c, k/qu, ll/y y g/j), omisiones y transposiciones. Normaliza
`xq`, `q` y `oy`. Una pregunta introductoria como `komo` muestra las funciones
disponibles para el perfil. Se conservan los límites de longitud y permisos;
la búsqueda no inventa funciones de ventas u otros sistemas.

Se interpretan hoy, ayer, anteayer, semanas, meses y años actuales o anteriores,
últimos N días, días de la semana, meses por nombre y fechas numéricas o escritas.
El resultado explica las fechas en hora de Guatemala. Sin año se usa el actual;
un día de la semana es el más reciente, incluido hoy. El acceso al calendario
abre el mes de la fecha inicial; no afirma filtrar registros por todo el rango.

Cada guía del perfil tiene un acceso directo. Los controles de apariencia,
idioma, menú y novedades se enfocan o abren en la misma pantalla. Consultar
guías de otros perfiles como SuperAdmin no concede acceso a esas funciones.
El tablero del Director separa la bienvenida de la tarjeta de propuestas.

Los contratos nuevos, cambios de plan y renovaciones calculan la vigencia desde
hoy, hasta el día anterior al aniversario de 1, 3, 6 o 12 meses. El importe viene
de la tarifa configurada. Fecha e importe son de solo lectura; el servidor los
recalcula y no acepta valores manipulados. Editar nombre, contacto o estado sin
cambiar de plan conserva la vigencia e importe históricos. Renovar el mismo plan
requiere pulsar la opción explícita de renovar desde hoy. No hay cobros automáticos.

Preparar una cita virtual ofrece un botón a `https://meet.google.com/home`, en
otra pestaña, y explica cómo copiar un enlace individual y volver a ZUNI para
guardarlo. Se mantienen las validaciones de propiedad, URL, acceso restringido
y no reutilización del enlace entre citas.

## PIN profesional

Mi cuenta permite configurar un PIN de cuatro dígitos confirmando la contraseña
actual. Las notas privadas se consultan desde el reporte o el historial después
de un POST con antiforgery y PIN. Antes de validarlo no se envían al navegador:
el efecto borroso muestra texto ficticio. El estudiante y el PDF conservan solo
el contenido compartido. No se guarda un desbloqueo en cookies ni en el servidor;
cada nueva consulta exige el PIN y revalida la propiedad actual e histórica.

La respuesta privada no se almacena en caché. El navegador la elimina al bloquear,
salir de la pestaña o después de dos minutos. El PIN se verifica con un hash
salado protegido además por Data Protection, con un propósito vinculado al usuario.
Cinco fallos bloquean la cuenta para ese PIN durante 15 minutos. Los contadores
se guardan y usan revisión de concurrencia. Los permisos clínicos y su auditoría
siguen vigentes; no se exportan las notas al PDF compartido.
Si ya existe un PIN, cambiar el correo propio también exige ese PIN. La
verificación se confirma antes de la transacción de edición y conserva los
intentos fallidos aunque el cambio de perfil se rechace. Así una sesión robada
no puede cambiar el destinatario de recuperación sin conocer el PIN.

La recuperación envía un enlace al correo registrado del psicólogo por Brevo.
Tiene 256 bits aleatorios, vence en 20 minutos, se almacena solo como hash y
se invalida al usarlo o solicitar otro. Cambiar contraseña, correo, actividad o
rol invalida su uso. El token viaja en el fragmento del enlace, se retira de la
barra del navegador y se remite solo en el POST, evitando su inclusión en las
URLs de los registros de acceso. Hay dos minutos de espera entre envíos.

## Configuración de Brevo pendiente

Usar `deploy/debian/correo-brevo.env.example` para las variables privadas del
servicio. `zuni@gmail.com` es únicamente un ejemplo, no un remitente verificado.
El operador debe verificar el remitente en Brevo, configurar una clave API
privada, indicar la URL HTTPS pública y activar `Correo__RemitenteVerificado`.
Si falta esto, la interfaz informa que el correo no está configurado; no muestra
enlaces de recuperación en pantalla ni afirma que se envió un mensaje.

Integración basada en la documentación oficial:
[Enviar un correo transaccional con Brevo](https://developers.brevo.com/docs/send-a-transactional-email).
No se enviaron mensajes reales durante las pruebas.

## Migración y publicación

`20261113061106_AgregarPinProfesional` crea solamente `PinesProfesionales`, con
clave de usuario y relación restrictiva a Identity. Para la instalación demo,
aplicar la migración con respaldo y conceder al usuario de ejecución:

```sql
GRANT SELECT, INSERT, UPDATE ON TABLE public."PinesProfesionales" TO zuni_app;
```

Conservar las claves persistentes de Data Protection y las claves clínicas.
La actualización no cambia ni migra las notas existentes. No se aplica en
producción como parte de esta entrega local; zuni.lat sigue en 1.3.3 hasta desplegar.

La campana y el proyecto identifican esta entrega como 1.4.0. Las comprobaciones
incluyen búsquedas y periodos, accesos por guía, PIN y antiforgery, ausencia de
notas en respuestas bloqueadas, bloqueo por cuenta y enlaces temporales de un
solo uso, con datos y correo ficticios.

Validación completada: suite general de seguridad offline; 321 comprobaciones
de agenda; 114 de guías, periodos, contratos, PIN, recuperación y protección del
correo; 43 comprobaciones de navegador en escritorio/móvil y ambos temas;
45 de historial y traducción. La migración se aplicó a PostgreSQL local tras un
respaldo privado verificado. Localhost responde HTTP 200 con la versión 1.4.0.
