# ZUNI 1.6.2 — revisión de despliegue r1

18/11/2026. Incidencia observada en Linode / Debian 13 durante el ensayo aislado: `permisos-clon.sql: Permission denied`. El actualizador abrió `psql` como `postgres`, pero el directorio temporal es privado de root y zuni-demo. El script original restauró el servicio anterior 1.3.3 antes de aplicar las migraciones a la base viva, según el registro compartido por el usuario.

Corrección: root abre el SQL y lo entrega por entrada estándar a PostgreSQL. Se conserva el directorio privado, el rol de migración y una única transacción para migraciones y permisos. No se agregan permisos globales ni se cambia la configuración del servidor. Se corrigen tanto el ensayo como la migración final.

Esta es una revisión del instalador, no una nueva versión funcional de ZUNI: la aplicación conserva 1.6.2. La prueba PostgreSQL usa ahora el mismo transporte por entrada estándar y mantiene las comprobaciones de idempotencia, conservación de datos ficticios y reversión atómica ante error.

Entrega: extraer el ZIP corregido y reemplazar la carpeta completa `actualizacion-1.6.2` en `/home/marlon/zuni-subida/` mediante SFTP. No editar manualmente el manifiesto SHA ni abrir permisos del directorio temporal. Repetir primero `--comprobar` y luego `--aplicar`. Los respaldos del intento previo se conservan.

El paquete corregido queda preparado localmente; su activación y la comprobación final de HTTPS dependen de la siguiente ejecución en Linode.

Validación r1: sintaxis Bash correcta; 17 comprobaciones PostgreSQL 18 aprobadas con SQL por stdin; laboratorio detenido y objetos ficticios retirados; 26 controles del comprobador aprobados en publicación limpia desde Git.

- Commit de origen: `02c0399ebc6c4f1c414b2791270c60abd975fdde`.
- ZIP: `.visual-check/despliegue-1.6.2/20261118-174355-13b37e/zuni-1.6.2-mobaxterm.zip`.
- SHA-256 ZIP: `3d91d5c5ce0763dbef38824a15a79a2b69838f8460c2202956b2def1da1bbc7b`.
- SHA-256 manifiesto: `93ed68ab3b844f3ab9af3730b0d84a7e70a22c9bed4bd84ee2cb012ddf10ef66`.
- Esta entrega reemplaza el ZIP anterior de commit `9386dc1`; no reutilizar el instalador anterior.
