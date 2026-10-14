# Avance de los requisitos de la asesoría

Fecha de revisión: 14 de octubre de 2026. Referencia: `requisitos-y-propuesta-negocio-zuni.md`.

| Bloque | Estado | Pendiente para cerrar el bloque |
|---|---|---|
| R01 · Institución y correo | Parcial | Identidad visual configurable y revisión final de configuración institucional. Nombre, soporte, políticas de correo y auditoría implementados. |
| R02 · Campos obligatorios | Completado | Indicadores, validación de género y datos de contacto, y requisitos condicionales implementados. |
| R03 · Mayoría de edad | Completado | Flujo de declaración y revisión manual implementado y probado. La institución debe ejecutar las revisiones reales y establecer su procedimiento documental. |
| R04 · Datos personales y derechos | Completado | Política institucional versionada y aprobada por Administración, aceptación registrada, trámite de acceso/corrección/supresión/retiro y respuesta auditable. Conservación y actuación institucional configurables; no se ejecutan eliminaciones automáticamente. Autorización específica del informe implementada. |
| R05 · Informe educativo PDF | Completado | Editor separado del expediente, autorización del estudiante por versión y destinatario, emisión profesional, sustitución de versiones, PDF con fuente incluida y consulta/descarga auditadas. Revocar impide nuevas descargas. |
| R06 · Citas virtuales | Completado | Enlace individual de Google Meet por cita, validación de participantes y horario, prevención de reutilización e instrucciones de privacidad. El anfitrión configura y comprueba los permisos reales en Google antes de confirmar la revisión en ZUNI. |
| R07 · Retirada de Catedrático | Completado | Rol excluido de altas y cambios nuevos; rutas antiguas cerradas con aviso. Cuentas, roles históricos y datos conservados, disponibles para revisión y cambio explícito auditado por Administración. |
| R08 · Instalaciones independientes | Parcial | Publicación y comprobación de aislamiento, respaldos y restauración por instalación. Hay documentación de despliegue; falta la demostración en Debian/Linode. |

**Pendiente: 25 %.** Se implementaron 6 de 8 bloques; R01 y R08 siguen parciales. Cada bloque pesa lo mismo y uno parcial se cuenta como pendiente. Este porcentaje describe funcionalidades del producto, no horas de trabajo, avance total de ZUNI ni certificación de cumplimiento legal. La investigación comercial inicial no se cuenta como funcionalidad implementada.

La institución debe publicar y aprobar su política real, definir procedimientos y ejecutar las revisiones/documentaciones correspondientes antes de operar con datos reales. El software no sustituye esas responsabilidades.

Siguientes pendientes: completar identidad visual de R01 y despliegue/aislamiento operativo de R08. La prueba real con una cuenta externa en Google Meet corresponde al anfitrión; las pruebas automáticas verifican los permisos y enlaces de ZUNI, sin entrar a reuniones externas.
