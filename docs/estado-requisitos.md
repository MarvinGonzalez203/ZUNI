# Avance de los requisitos de la asesoría

Fecha de revisión: 14 de octubre de 2026. Referencia: `requisitos-y-propuesta-negocio-zuni.md`.

| Bloque | Estado | Pendiente para cerrar el bloque |
|---|---|---|
| R01 · Institución y correo | Completado | Nombre, soporte, políticas de correo, lema, color común de botones y logotipo PNG configurable; cambios validados y auditados. Botones del Director y Psicólogo unificados con violeta inicial #cea2fd. |
| R02 · Campos obligatorios | Completado | Indicadores, validación de género y datos de contacto, y requisitos condicionales implementados. |
| R03 · Mayoría de edad | Completado | Flujo de declaración y revisión manual implementado y probado. La institución debe ejecutar las revisiones reales y establecer su procedimiento documental. |
| R04 · Datos personales y derechos | Completado | Política institucional versionada y aprobada por Administración, aceptación registrada, trámite de acceso/corrección/supresión/retiro y respuesta auditable. Conservación y actuación institucional configurables; no se ejecutan eliminaciones automáticamente. Autorización específica del informe implementada. |
| R05 · Informe educativo PDF | Completado | Editor separado del expediente, autorización del estudiante por versión y destinatario, emisión profesional, sustitución de versiones, PDF con fuente incluida y consulta/descarga auditadas. Revocar impide nuevas descargas. |
| R06 · Citas virtuales | Completado | Enlace individual de Google Meet por cita, validación de participantes y horario, prevención de reutilización e instrucciones de privacidad. El anfitrión configura y comprueba los permisos reales en Google antes de confirmar la revisión en ZUNI. |
| R07 · Retirada de Catedrático | Completado | Rol excluido de altas y cambios nuevos; rutas antiguas cerradas con aviso. Cuentas, roles históricos y datos conservados, disponibles para revisión y cambio explícito auditado por Administración. |
| R08 · Instalaciones independientes | Parcial: implementación y ensayo completados | Configuración de Debian/systemd/Nginx, identificador y cookie por instalación, claves, bases y usuarios separados, scripts de respaldo/restauración. Ensayo real en contenedores Debian con PostgreSQL: dos aplicaciones sin root, credenciales cruzadas rechazadas y respaldo restaurado. Pendiente únicamente publicar en Linode y verificar dominio, HTTPS, renovación y respaldo externo del servidor el día 17. |

**Pendiente: 12,5 %.** Se cerraron 7 de 8 bloques. R08 conserva el estado parcial por la publicación real pendiente, aunque su implementación y ensayo local ya estén terminados. Cada bloque pesa lo mismo y uno parcial se cuenta como pendiente. Este porcentaje describe el cierre de los requisitos, no horas de trabajo, avance total de ZUNI ni certificación de cumplimiento legal. La investigación comercial inicial no se cuenta como funcionalidad implementada.

La institución debe publicar y aprobar su política real, definir procedimientos y ejecutar las revisiones/documentaciones correspondientes antes de operar con datos reales. El software no sustituye esas responsabilidades.

Siguiente pendiente: publicación y validación operativa de R08 en Linode el día 17. Ver `instalaciones-debian.md`. La prueba real con una cuenta externa en Google Meet corresponde al anfitrión; las pruebas automáticas verifican los permisos y enlaces de ZUNI, sin entrar a reuniones externas.
