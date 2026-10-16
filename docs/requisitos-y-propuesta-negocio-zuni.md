# ZUNI — Requisitos y propuesta de negocio

Fecha: 13 de octubre de 2026. Estado: propuesta para revisar con el equipo y el ingeniero; funcionalidades pendientes de implementación.

Este documento transforma las anotaciones de la asesoría en requisitos verificables y una propuesta comercial. No cambia el sistema, la base de datos ni la infraestructura. La demostración en Debian con Linode y el dominio `zuni.lat` se mantiene prevista para el 17 de octubre; no se han contratado recursos nuevos.

## 1. Propósito y alcance del producto

ZUNI facilita la atención de psicología educativa y la coordinación del apoyo al estudiante. Su entregable principal será un **informe de apoyo educativo en PDF**, elaborado y autorizado por el psicólogo para un destinatario institucional definido. La autoridad recibe recomendaciones educativas útiles, sin obtener acceso al expediente clínico ni a las respuestas individuales de los cuestionarios.

El producto debe servir a distintas instituciones, no exclusivamente a UMG. Para la primera versión comercial se propone una instalación independiente por institución, con su propia base de datos, cuentas, configuración, documentos y respaldos.

Las afirmaciones sobre rendimiento deben apoyarse en observaciones y fuentes identificadas. El sistema no determina automáticamente las causas de un bajo rendimiento, la inteligencia o la motivación del estudiante. Expresiones como «haragán» se traducen en conductas observables —por ejemplo, dificultades para organizar tareas— y necesidades de apoyo, sin etiquetas ni juicios personales.

El MVP propuesto atiende únicamente a mayores de edad. La atención de menores requiere otro flujo de consentimiento y responsabilidades que debe definirse antes de ampliar el alcance a todos los alumnos de un colegio.

## 2. Estado actual revisado

| Aspecto | Estado actual | Cambio propuesto |
|---|---|---|
| Registro | El registro personal y las altas administrativas exigen `@miumg.edu.gt`. | Política de correo configurable por institución. |
| Mayoría de edad | Edad declarada y casillas de confirmación; no hay verificación documental. | Fecha de nacimiento y constancia de revisión del documento. |
| Privacidad | Hay autorizaciones clínicas, separación de textos privados y compartidos, y auditoría. | Política institucional, gestión de derechos y autorización específica de los informes educativos. |
| Reportes | Existe registro de atención y contenido compartido con el estudiante. | Nuevo informe educativo independiente y descarga en PDF. |
| Citas virtuales | Existe modalidad Virtual; la cita no guarda un enlace de reunión. | Enlace por cita y controles de acceso y privacidad. |
| Roles | Administrador, Director, Psicólogo, Catedrático y Estudiante. | Retirar Catedrático del alcance operativo de la siguiente versión. |
| Infraestructura | Una aplicación ASP.NET Core y PostgreSQL. | Mantener el mismo producto, con una instalación separada por institución. |
| Suscripciones | No hay gestión de planes ni cobros. | Definir contratación y límites antes de automatizar pagos. |

El dominio escrito en las anotaciones como `@miumg.edut.gt` parece un error de escritura: el sistema utiliza `@miumg.edu.gt`. Al generalizar el producto, ningún dominio debe quedar fijado en el código.

## 3. Requisitos funcionales y criterios de aceptación

### R01 — Configuración de la institución y del correo

El Administrador configura nombre de la institución, identidad visual, contacto de soporte y política de correo. Las opciones propuestas son:

- **Dominios autorizados:** permitir uno o varios dominios completos.
- **Cualquier correo válido:** aceptar correos personales, conservando la activación administrativa.
- **Altas administradas:** deshabilitar el registro público y crear cuentas desde Administración.

Aceptar un correo no acredita pertenencia a la institución. La institución debe revisar o verificar a la persona antes de activar su cuenta. Verificar la propiedad del correo mediante un enlace requeriría configurar un proveedor de envío; no se presume implementado.

**Aceptación:** registro personal y altas administrativas usan la misma política; cambiarla queda auditado; cuentas existentes no se eliminan ni pierden acceso por cambiar el dominio autorizado; un dominio parecido no se acepta como si fuera el autorizado.

### R02 — Campos obligatorios visibles

Los campos obligatorios llevan asterisco rojo y una indicación textual «* Campo obligatorio». Los opcionales se identifican como tales. Los campos que pasan a ser obligatorios por una selección —por ejemplo, datos de referencia— actualizan su indicación.

**Aceptación:** la marca coincide con las validaciones del servidor y con las reglas condicionales. La obligatoriedad también es comprensible para lectores de pantalla y no depende únicamente del color.

### R03 — Verificación de mayoría de edad

La fecha de nacimiento permite calcular la edad exacta, considerando el cumpleaños. La institución revisa el documento de identidad para comprobarla. El DPI es una referencia adecuada para Guatemala; escribir un CUI con formato válido no equivale a verificar identidad o edad. RENAP describe el DPI como documento oficial personal y explica su entrega a partir de los 18 años. [Emisión del DPI](https://www.renap.gob.gt/servicios/emision-del-dpi), [Entrega del DPI](https://renap.gob.gt/noticias/formas-de-entrega-del-dpi).

Se propone una **verificación documental manual**, registrando resultado, fecha, responsable y tipo de documento revisado. No conservar fotografía, número completo de DPI ni biometría por defecto. La fecha de nacimiento se mantiene solo si la institución justifica su necesidad; de otro modo se conserva la constancia de mayoría de edad validada.

El responsable de verificar debe tener un permiso administrativo limitado: revisar identidad no le concede acceso a motivos de consulta o notas privadas. No se promete integración con RENAP ni verificación automática de documentos.

**Aceptación:** una declaración pendiente aparece como «Declarada, pendiente de verificación», no como «Verificada». No se permite iniciar la atención del MVP hasta validar la mayoría de edad. Se rechazan fechas futuras y se prueba el día anterior y el día exacto del cumpleaños 18. Para extranjeros se define el documento equivalente autorizado.

### R04 — Protección de datos y derechos de las personas

Antes de operar con información real, la institución define finalidad, datos necesarios, personas autorizadas, conservación, canal de solicitudes de acceso o corrección y tratamiento de incidentes. Debe diferenciar consentimiento para atención, participación en cuestionarios y entrega de un informe a autoridades educativas.

La Ley de Acceso a la Información Pública de Guatemala contempla datos sensibles, incluyendo salud física y psíquica; sus artículos 30–32 regulan protección y difusión de datos en su ámbito. Sus sujetos obligados se delimitan en los artículos 2 y 6, por lo que no debe afirmarse que resuelve por sí sola todas las obligaciones de cualquier colegio privado. La institución deberá validar el marco aplicable, el secreto profesional y las bases de cada comunicación. [Decreto 57-2008, texto publicado por el TSE](https://www.tse.org.gt/images/UECFFPP/leyes/ley_acceso_informacion_decreto_57-2008.pdf).

**Requisitos del producto:**

- Recoger solo datos necesarios para cada finalidad.
- Mantener el expediente y las notas profesionales fuera del panel administrativo y de los reportes educativos.
- Auditar accesos, cambios, autorizaciones y descargas sin copiar contenido clínico en los registros de auditoría.
- Permitir solicitudes de acceso y corrección mediante un procedimiento institucional verificable.
- Definir conservación y eliminación con la institución; retirar un consentimiento no implica borrar automáticamente todo el expediente.
- Proteger comunicaciones con HTTPS y respaldos con cifrado y acceso restringido.
- Evitar datos reales en demostraciones, capturas públicas y ambientes de prueba.

**Aceptación:** se prueban intentos de acceso con otro usuario y otra instalación; Administración no puede consultar notas privadas; un PDF no se sirve mediante una dirección pública; los consentimientos conservan versión, propósito, decisión y fecha.

### R05 — Informe de apoyo educativo y PDF

Debe ser un documento nuevo, separado del registro profesional privado. No se construye copiando automáticamente las notas clínicas ni el texto ya compartido con el estudiante: ambos podrían contener información inapropiada para el destinatario educativo.

**Contenido propuesto:**

1. Institución, identificador del informe, fecha y versión.
2. Identificación mínima necesaria del estudiante y destinatario institucional autorizado.
3. Objetivo del informe y periodo de observación.
4. Fuentes de información: entrevista, observaciones y datos académicos disponibles y autorizados.
5. Descripción prudente de barreras para el aprendizaje y su posible relación con el desempeño.
6. Necesidades y recomendaciones de apoyo educativo.
7. Responsable institucional de las acciones y fecha propuesta de seguimiento.
8. Nombre del psicólogo y constancia de aprobación. La presentación del nombre no se anuncia como firma electrónica certificada.
9. Nota sobre propósito, destinatarios y uso confidencial del documento.

No incluir diagnósticos, narraciones íntimas, DPI, contactos de emergencia, respuestas de pruebas ni información familiar innecesaria. Si una recomendación permite inferir información sensible, también requiere revisión antes de compartir.

**Ejemplo de redacción, únicamente ilustrativo:** «Durante el periodo observado se identificaron dificultades para organizar las actividades académicas. Se recomienda acordar un calendario de entregas y revisar su avance dentro de cuatro semanas». Una afirmación así debe proceder de la valoración profesional del caso; no es texto que el sistema deba adjudicar automáticamente a un alumno.

**Flujo:** Borrador → Revisión del psicólogo → Autorización de finalidad y destinatario → Emitido → Consulta o descarga auditada. Si hay una corrección posterior, se emite una nueva versión y se marca cuál sustituye a la anterior. Una autorización rechazada no impide continuar la atención psicológica.

En el flujo ordinario propuesto se registra el consentimiento específico del estudiante para compartirlo. Cualquier excepción necesita una base institucional y legal definida; no se incorpora como permiso general para ver expedientes.

**Aceptación:** el PDF corresponde exactamente al contenido aprobado; solo el profesional autorizado lo redacta y emite; el destinatario designado consulta únicamente el informe autorizado. Un Director no obtiene acceso a todo el caso por recibir el PDF. Revocar acceso en ZUNI no borra copias que ya se descargaron: debe explicarse antes de compartir.

### R06 — Citas virtuales con privacidad

Cada cita virtual confirmada tendrá su enlace de reunión, visible únicamente para el estudiante y el profesional autorizados. No habrá un enlace general reutilizado por todos los estudiantes.

Para el primer alcance, el psicólogo crea la reunión en su cuenta autorizada y registra el enlace al confirmar la cita. Automatizar la creación con Google requiere una integración posterior y permisos de la cuenta organizadora; guardar una URL no configura por sí mismo los permisos de Meet.

Google distingue acceso abierto, de confianza y restringido; en modo restringido solo los invitados pueden entrar sin solicitar acceso. Los controles disponibles dependen de la cuenta y de la configuración institucional. Se debe probar además el rechazo de personas no invitadas. [Referencia de Google Meet](https://developers.google.com/workspace/meet/api/reference/rest/v2/spaces), [Controles de acceso](https://support.google.com/a/users/answer/11989526?hl=en).

**Aceptación:**

- El enlace es HTTPS y pertenece al proveedor permitido por la institución.
- Otra cuenta de estudiante no puede verlo ni cambiarlo.
- Una cita presencial no exige enlace; una virtual no se presenta como lista para ingresar sin enlace confirmado.
- Cambiar el enlace queda auditado. Cancelar impide ofrecer el acceso desde ZUNI, aunque cerrar la reunión externa también requiere actuar en el proveedor.
- Se muestran indicaciones de buscar un espacio privado y silencioso, usar audífonos si es posible y evitar compartir el enlace.
- Una prueba con una cuenta externa verifica que no puede ingresar sin autorización del anfitrión.
- Grabación y transcripción no se activan automáticamente.

### R07 — Retirar Catedrático del alcance actual

Los roles operativos del MVP serán Administrador, Director, Psicólogo y Estudiante. Se ocultará Catedrático en navegación y nuevas altas, y se definirá qué hacer con cuentas ya existentes antes de modificar sus permisos.

**Aceptación:** no se eliminan usuarios, datos históricos ni tablas Identity. Ninguna cuenta recibe otro rol automáticamente. La retirada conserva auditoría y cierra accesos incompatibles con la decisión adoptada.

### R08 — Instalaciones independientes

Cada institución tiene una instalación y una base de datos separadas, configuradas desde el mismo producto. También quedan separados las claves de sesión, documentos, copias de seguridad, credenciales y configuración de correo. Ser instalador o proveedor de soporte no concede acceso permanente a expedientes.

La institución puede aportar un servidor Debian propio o contratar una instancia dedicada en su cuenta de Linode. Si ZUNI opera la infraestructura por encargo, el contrato identifica expresamente quién administra, quién autoriza el soporte y cómo se devuelve la información.

**Aceptación:** dos instalaciones de prueba no comparten cuentas ni datos; una restauración se realiza únicamente en el destino correspondiente; las actualizaciones incluyen respaldo y verificación previa. La primera demostración será una única instalación, sin portal comercial de múltiples instituciones.

## 4. Modelo comercial propuesto

| Modelo | Forma de cobro | Encaje con la asesoría |
|---|---|---|
| SaaS central con instituciones en una aplicación compartida | Suscripción por institución o profesional; proveedor aloja los datos. | Posible a futuro, pero requiere aislamiento adicional y no coincide con instalaciones propias separadas. |
| Servicio con instalación dedicada por institución | Suscripción de software y operación; una instalación para cada cliente. | Compatible si la institución acepta delegar la operación y se delimita el tratamiento de sus datos. |
| Licencia instalada en infraestructura de la institución, con mantenimiento por suscripción | Instalación inicial y suscripción mensual o anual de actualizaciones y soporte. | **Recomendación inicial:** coincide con la infraestructura propia solicitada. |
| Licencia perpetua con mantenimiento opcional | Pago inicial mayor y renovación de mantenimiento. | Alternativa; ingresos menos previsibles y mayor riesgo de instalaciones sin actualizar. |

No es necesario llamar SaaS a todos los modelos. Una instalación operada por el cliente encaja mejor como **software institucional con licencia y mantenimiento por suscripción**. Una instalación dedicada operada por ZUNI puede ofrecerse como servicio administrado.

**Cliente que paga:** la institución educativa. **Usuarios:** estudiantes, psicólogos y autoridades autorizadas. No se propone cobrar al estudiante por solicitar apoyo ni monetizar información psicológica.

**Propuesta de valor:** organizar el proceso de atención y convertir la valoración profesional en recomendaciones educativas documentadas, con destinatarios controlados y seguimiento. No prometer mejoras de notas, diagnósticos automáticos ni sustitución del psicólogo.

## 5. Referencia de mercado y diferenciación

Cliniko cobra por cantidad de profesionales: publica US$45 mensuales para uno y US$95 para dos a cinco; además anuncia acceso gratuito para instituciones educativas o entidades benéficas registradas, sujeto a contacto y elegibilidad. Es una referencia de gestión clínica, no una equivalencia funcional de ZUNI. Esto obliga a validar por qué una institución pagaría por nuestro enfoque de psicología educativa, informes y despliegue propio. [Precios oficiales de Cliniko](https://www.cliniko.com/pricing/).

La diferenciación a validar es el informe educativo con privacidad, el trabajo en español, la configuración institucional y la posibilidad de conservar la infraestructura y la información en el entorno del cliente. No afirmar ventajas frente a competidores sin probarlas.

Antes de fijar precios, entrevistar a cinco instituciones y mostrar el flujo de informe: quién lo necesita, quién aprueba la compra, qué herramienta utilizan, qué presupuesto tienen y qué costo les supone el proceso actual. Registrar objeciones y disposición de pago; no confundir interés en una demostración con una venta.

## 6. Planes para validar con clientes

Los siguientes importes y límites son **hipótesis comerciales propias**, no tarifas contratadas ni capacidades verificadas del servidor. La infraestructura se paga aparte por la institución.

| Concepto | Inicial | Institucional | Ampliado |
|---|---|---|---|
| Suscripción propuesta | US$29/mes | US$59/mes | US$99/mes |
| Psicólogos activos incluidos | 1 | Hasta 3 | Hasta 6 |
| Estudiantes con cuenta activa incluidos | Hasta 250 | Hasta 1,000 | Hasta 3,000 |
| Instituciones por licencia | 1 | 1 | 1 |
| Instalación inicial propuesta | US$75 | US$150 | US$250 |
| Soporte humano incluido | Hasta 30 min/mes | Hasta 1 h/mes | Hasta 2 h/mes |
| Servicios adicionales | Cotización | Cotización | Cotización |

Todos los planes incluyen privacidad, autorizaciones, agenda, registro de atención e informe educativo PDF. Las medidas de seguridad y los derechos de los estudiantes no se venden como funciones exclusivas del plan más caro. Los límites de cuentas no representan cantidad de usuarios simultáneos; la concurrencia debe probarse por separado.

Definir contractualmente:

- «Psicólogo activo» y «estudiante activo» como cuentas habilitadas, no como número de visitas mensuales.
- Conservación y consulta de registros históricos aunque una cuenta se desactive.
- Aviso previo al alcanzar límites; no borrar información ni interrumpir una sesión de atención por exceder una cuota.
- Crecimiento de plan con autorización de la institución, sin cargos sorpresa.
- Cancelación, exportación, transición y periodo de conservación acordado. No bloquear la entrega de datos del cliente por una disputa comercial.
- Qué soporte comprende instalación, aplicación y actualizaciones, y qué tareas corresponden al administrador del servidor.
- Migraciones de datos, personalizaciones, capacitación adicional y atención fuera de horario como servicios cotizados.
- Impuestos, moneda de cobro y condiciones de facturación, pendientes de definir con la operación comercial local.

Para el piloto se propone contratación y activación manual de la licencia. Un módulo de cobros automáticos, pasarela de pagos y portal de contratación se deja para después de validar el producto y el modelo.

## 7. Costos y presupuesto de la demostración

El presupuesto indicado —menos de US$12 mensuales— corresponde al alojamiento de la demostración, no al precio de venta del software ni al costo total de operar un servicio comercial.

Akamai/Linode publica Nanode 1 GB a US$5/mes, 2 GB a US$12 y 4 GB a US$24 en su tabla de Shared CPU consultada. El primero entra en el presupuesto base, pero deberá probarse si admite la aplicación y PostgreSQL con la carga prevista. Extras y región pueden modificar el total. [Precios oficiales](https://www.akamai.com/cloud/pricing/cpu).

Para el piloto: publicar el resultado compilado, instalar una única instancia en Debian, medir memoria y carga y mantener PostgreSQL fuera de acceso público. Reservar presupuesto para respaldos y recuperación; no dar por incluidos servicios adicionales.

La facturación de estos planes depende del tiempo que el recurso está provisionado; una VM apagada también se factura. Eliminar la instancia detiene sus cargos futuros, pero no elimina consumos anteriores ni otros recursos que permanezcan activos. Esto coincide con esperar al 17 para crear la instancia. [Facturación de Akamai](https://techdocs.akamai.com/cloud-computing/docs/understanding-how-billing-works), [Preguntas de facturación y precios](https://www.akamai.com/cloud/pricing/cpu).

Para evaluar la viabilidad de un plan comercial:

**Ingreso mensual por institución − soporte − operación técnica − infraestructura asumida − comisiones de cobro − provisión para incidencias = contribución antes de impuestos.**

Ejemplo de planificación, no costo contratado: una licencia de US$29 con US$10 de trabajo de soporte y US$5 de operación deja US$14 antes de comisiones e impuestos si el cliente paga el servidor. Si el proveedor absorbe también US$5 de servidor, quedan US$9. El piloto debe medir el tiempo real de soporte y los costos de respaldo antes de confirmar la tarifa.

## 8. Responsabilidades

| Actor | Responsabilidad propuesta |
|---|---|
| Institución | Contratar, definir finalidades, destinatarios y conservación; habilitar usuarios; nombrar responsable de privacidad. |
| Administrador de ZUNI | Gestionar cuentas, configuración y verificaciones autorizadas sin consultar contenido clínico. |
| Psicólogo | Valorar el caso, registrar atención, redactar y aprobar el informe educativo. |
| Director o autoridad designada | Consultar únicamente los informes educativos expresamente autorizados y coordinar apoyos. |
| Estudiante | Aportar información, participar voluntariamente y conocer los usos de sus datos. |
| Equipo proveedor | Mantener el software y prestar soporte con acceso acotado, autorizado y registrado. |
| Administrador de infraestructura | Administrar Debian, disponibilidad, secretos, respaldos y restauraciones según el contrato. |

Instalar los datos en el servidor de una institución no elimina los riesgos de acceso indebido. La separación física se complementa con permisos, procedimientos y responsabilidad profesional.

## 9. Orden de implementación propuesto

1. **Definir alcance y permisos:** aceptar el MVP de mayores de edad, la instalación separada y los destinatarios del informe; establecer privacidad y verificación de identidad.
2. **Generalizar el producto:** institución y correo configurables, marcas de obligatoriedad y retirada controlada de Catedrático.
3. **Implementar el entregable principal:** editor de informe educativo, autorización, versiones, PDF y descarga auditada.
4. **Completar atención virtual:** enlace individual por cita, instrucciones y prueba de acceso externo.
5. **Preparar y probar Debian/Linode:** publicación, HTTPS, PostgreSQL, respaldos y recuperación; conexión del dominio después de conocer la IP.
6. **Validar con una institución piloto:** ajustes profesionales, utilidad del informe y costos reales.
7. **Iniciar contratación manual:** contrato, instalación y suscripción; automatizar planes y pagos cuando haya demanda comprobada.

La creación del servidor el 17 no implica que todas las funcionalidades estén listas para esa fecha. La demostración debe indicar qué está implementado y qué sigue como propuesta.

## 10. Demostración y criterios de éxito

Usar una institución y personas ficticias. El recorrido objetivo será: configurar la institución → activar estudiante → verificar mayoría de edad → solicitar atención → asignar psicólogo → confirmar cita → registrar atención → redactar y autorizar informe → descargar PDF → consultar seguimiento.

Comprobar que una autoridad puede actuar sobre recomendaciones educativas sin leer el expediente, y que una persona no autorizada no puede acceder al PDF ni al enlace de otra cita. Restaurar un respaldo en un ambiente de prueba y verificar la separación de instalaciones.

Medir en el piloto: solicitudes atendidas, tiempos de asignación, inasistencias, informes emitidos con autorización y seguimiento de recomendaciones. No usar estas métricas para asegurar una mejora causal de rendimiento sin un estudio adecuado. Los informes globales deben evitar identificar personas por grupos demasiado pequeños.

## 11. Decisiones pendientes antes de programar

- Confirmar la política de correo inicial y quién valida la pertenencia institucional.
- Confirmar responsable de verificación de edad, documentos admitidos y datos mínimos conservados.
- Definir con el psicólogo la plantilla del PDF y con la institución sus destinatarios y autorización.
- Confirmar qué cuentas existentes tienen Catedrático y cómo se tratarán.
- Elegir cuenta de videollamadas y comprobar sus controles antes de prometer creación automática.
- Definir si el cliente operará su servidor o delegará su administración al proveedor.
- Validar precio, límites, soporte, contratación y salida con clientes potenciales.

Estas decisiones quedan registradas como pendientes. Ninguna de las funcionalidades descritas en este documento se presenta como ya implementada.
