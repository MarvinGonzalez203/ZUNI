# Coordinación educativa y propuesta comercial

## Funciones revisadas y completadas

- Citas virtuales: ya existían recomendaciones antes de entrar y enlaces individuales de Google Meet. Ahora los consejos también se presentan como tres tarjetas: privacidad, interrupciones y acceso desde la cita propia. El estudiante puede consultarlos desde que existe la cita, antes de que se habilite el ingreso. Las notificaciones de programación y preparación del enlace también incluyen estos consejos, sin exponer el enlace de Meet en el aviso.
- Los enlaces se validan como URLs individuales HTTPS de Meet, con huella única entre citas y revisión de controles de acceso por el psicólogo. Se conservan las restricciones de participante, estado y horario. La admisión efectiva se configura en Google Meet: ZUNI no modifica los permisos de una reunión externa.
- Citas presenciales: el psicólogo puede definir y actualizar dirección, edificio o consultorio. Confirmar una cita presencial futura exige lugar. Se muestra al estudiante en sus citas/calendario y se notifica al actualizarlo. La creación desde disponibilidad sin lugar queda pendiente de confirmación; los registros históricos no se rellenan con direcciones ficticias. Cambiar a virtual descarta la dirección presencial.
- Informe educativo: el profesional indica si amerita analizar el rendimiento. Cuando corresponde, separa el relato del estudiante de su valoración contrastada y confirma la revisión antes de solicitar autorización. Los dos textos se incluyen en la versión autorizada y en su PDF, sin copiar las notas privadas. El sistema no determina causas clínicas ni valida automáticamente la veracidad de un relato.
- La edición del texto exige volver a marcar el contraste en la interfaz. Una versión nueva restablece esa confirmación. Los informes históricos conservan su contenido; no se les inventa un análisis retroactivo.
- Director retirado: guardar y emitir ya verificaban el rol vigente y la cuenta activa. Ahora también lo exige cada nueva descarga del PDF, incluida la que intenta hacer su psicólogo autor. Si se retira al destinatario durante la preparación, el autor puede crear otra versión; la anterior queda sustituida sin inventar una decisión del estudiante. El nuevo destinatario requiere otra autorización.
- Administración debe registrar el cese desactivando la cuenta o retirando el rol Director. El sistema no conoce cambios laborales externos que la institución no registre. Se conserva el historial y no se recuperan archivos descargados fuera de ZUNI.
- Home: dos tarjetas de misión y visión con orientación a psicología educativa, privacidad y continuidad del acompañamiento. ZUNI es apoyo al profesional, no terapia autónoma ni sustituto del psicólogo.

## Propuesta de precios en GTQ

Son estimaciones propias para validar con instituciones piloto, no precios de mercado comprobados ni suscripciones activas. Se muestran en **Administrador → Tarifas y periodos**, sin acciones de cobro. Los administradores institucionales no pueden modificar el catálogo del proveedor.

| Periodo | Total anticipado | Equivalente mensual | Ahorro frente a Q199/mes |
| --- | ---: | ---: | ---: |
| 1 mes | Q199.00 | Q199.00 | Q0.00 |
| 3 meses | Q567.00 | Q189.00 | Q30.00 |
| 6 meses | Q1,075.00 | Q179.17 | Q119.00 |
| 12 meses | Q2,030.00 | Q169.17 | Q358.00 |

Todos los periodos proponen el mismo alcance funcional por institución: cuentas y roles, agenda, instrumentos orientados por el profesional, reportes e informes autorizados. Los descuentos son por pago anticipado del periodo completo, no por una reducción de funciones. No se afirman límites de alumnos, concurrencia ni disponibilidad que todavía no se hayan medido o contratado.

Base de trabajo para Q199/mes: Q50 de soporte básico, Q60 de mantenimiento, Q30 de reserva operativa y Q59 de margen antes de gastos adicionales. Estos importes son hipótesis internas; deben contrastarse con horas reales, volumen de uso y soporte requerido. Infraestructura, dominio, respaldos, impuestos, instalación, capacitación y servicios de psicología se cotizan por separado.

Para el presupuesto de servidor que mencionó el usuario, USD12 equivalen aproximadamente a Q91.70 usando **Q7.64140/USD del 15/10/2026**. Esa conversión no es una cotización bancaria de compra ni incluye comisiones. [Tipo de cambio de referencia, Banco de Guatemala](https://www.banguat.gob.gt/tipo_cambio/).

La infraestructura se calcula por separado: Akamai publica facturación por uso con tope mensual y distingue cómputo, almacenamiento y respaldos. El tamaño adecuado para .NET y PostgreSQL requiere medir la carga de ZUNI. [Precios oficiales de Akamai Cloud](https://www.akamai.com/cloud/pricing).

## SuperAdmin: sí procede para una plataforma central

El Administrador actual gestiona una institución. Un SuperAdmin del proveedor tendría una función distinta: alta y estado de instituciones, catálogo comercial, periodos contratados, renovaciones y auditoría operativa. Ese rol no debe conceder por defecto acceso a expedientes, respuestas ni notas de estudiantes.

Hoy el proyecto mantiene configuración, cuentas y datos separados por instalación. Eso permite operar instalaciones independientes, pero no incorpora una consola central que coordine sus suscripciones. La separación entre administración global y operación de cada institución coincide con la distinción entre plano de control y plano de datos de una solución SaaS. [Consideraciones de arquitectura para planos de control, Microsoft](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/control-planes).

La siguiente ampliación debería crear esa consola y definir aislamiento, alta de instituciones, permisos, contratos y facturación. Agregar solamente un nombre de rol a la base institucional no implementa esas funciones. En esta entrega no se crean cuentas SuperAdmin ni una segunda autenticación; las tarjetas son una propuesta de precios, sin simular contratos o cobros.

## Migración y verificación

Migración aditiva `20261015184809_AddLugaresYAnalisisEducativo`: una dirección opcional en citas y cuatro campos de análisis en versiones de informes. Se respaldó la base local en `zuni-antes-coordinacion-20261015.dump` antes de aplicarla. No se registraron atenciones, consentimientos ni decisiones clínicas en la base real.

Se prueban direcciones obligatorias, cambios obsoletos, permisos, datos codificados, aviso al estudiante, análisis pertinente/no pertinente y descargas tras retirar rol o desactivar al destinatario. Los PDF ficticios de prueba se inspeccionan en versiones breve y extensa; no incluyen notas privadas. La verificación de concurrencia real de PostgreSQL sigue siendo distinta de los ensayos con SQLite.
