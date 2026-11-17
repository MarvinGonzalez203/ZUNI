# ZUNI 1.6.1 — operación de una instancia y recuperación

Antes de la primera activación, detener todas las versiones anteriores y herramientas escritoras. El guard solo coordina procesos que lo respetan; no bloquea una versión vieja ni escrituras directas externas. No ejecutar dos versiones contra los mismos datos durante el cambio.

Entrega local, 17/11/2026. No se desplegó ni se migraron bases reales. La campana conserva 1.6.0 y 1.5.3; el catálogo refleja Fulleo 0.9.1 y Academy 1.2.2.

## Instancia y límites

Program adquiere una conexión PostgreSQL exclusiva de coordinación antes de seeds, comandos de escritura y exportadores. pg_try_advisory_lock usa una clave estable por base. Otra instancia se rechaza antes de escribir; la conexión no usa pool y se libera al terminar. Un monitor comprueba cada dos segundos y detiene la aplicación al perder la conexión; el middleware rechaza peticiones cuando detecta esa pérdida. Evento 1418 no registra credenciales ni conexión.

Esto controla el arranque accidental, **no implementa HA ni fencing distribuido ante particiones de red**. No añadir réplicas ni un failover automático: persisten los requisitos de límites compartidos, claves/auditoría y medición. La configuración soportada sigue siendo una instancia en un host autorizado. El comando de verificación de seguridad es solo lectura y no solicita el guard; los demás comandos escritores requieren detener la instancia antes de mantenimiento.

## Recuperación

deploy/debian/backup-encrypted.sh prepara un pg_dump consistente y lo cifra con la herramienta vault revisada de Denis Corp. Requiere Node 24+, PGDATABASE/PGUSER/PGPASSFILE privados y DENIS_VAULT_TOOL/DENIS_BACKUP_KEY_FILE. La clave de la bóveda se crea fuera del snapshot y no se envía por chat ni Git. Es un trabajo nuevo y opcional: no se sustituyó el backup.sh legado ni se reconfiguraron tareas instaladas. No desplegarlo sin custodiar/verificar primero las claves y el espacio privado de staging.

Las claves de cifrado clínico y de Data Protection se respaldan en otra bóveda, con otra clave y custodia separada del dump. Sin ellas no se recuperan los textos protegidos; la contraseña del login o el PIN no las sustituyen. Mantener también evidencia de auditoría, configuración privada y versiones del artefacto. Al restaurar se requiere revisar permisos, firmas, conteos y revocar sesiones según el procedimiento autorizado; no activar la base solo porque pg_restore terminó.

Pruebas de laboratorio: tests/Operacion.Postgres exige host loopback, nombre de base ficticio y ApplicationName de fixture; prueba guard, pérdida de conexión y el contrato escolar de staging dentro de una transacción revertida. La prueba de recuperación creó y borró únicamente dos bases desechables, cifró dump y claves por separado, restauró ambas y verificó identidad/hash/relaciones/cifrado/firma/Data Protection y escritura. No representa un RTO/RPO de producción ni recuperación en el DS223j. No se abrió la base real.
