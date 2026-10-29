# Publicación de ZUNI 1.2.0

- Fecha de finalización: 29/10/2026, 18:28:31 UTC.
- Sitio: https://zuni.lat/.
- Código publicado: `e70aa3f2cc18a5b1db3597797144eb04e2739dad`, rama `feature/director-V2`.
- Versión anterior identificada: `1.0.0+61d65ad6a5e10e1a8145d81f7c74c8529998aad1`.
- Servicio: `zuni-demo`, activo después de publicar.

El procedimiento verificó las sumas SHA-256 del paquete, detuvo brevemente
la aplicación y respaldó la base, persistencia, configuración y binarios.
Restauró el respaldo en una base temporal aislada y comprobó allí la migración
antes de aplicarla a la base de producción. La migración agregó únicamente
`CentrosComerciales` y `LecturasVersion`, con permisos específicos para runtime.
La base temporal se eliminó después de la comprobación.

Se conservaron la configuración y las claves originales de producción; no se
restauró una base local sobre la base publicada. Las verificaciones de seguridad,
integridad de claves y conservación del número de usuarios finalizaron sin error.
Estas comprobaciones no equivalen a una auditoría integral de seguridad ni a una
prueba manual de cada flujo autenticado.

Comprobación posterior independiente mediante SSH: el enlace de publicación
apunta a `/opt/zuni/demo/releases/1.2.0-e70aa3f-pZLhkB`, el manifiesto identifica
el commit indicado y el servicio está activo. Inicio, acceso y favicon devolvieron
HTTP 200 por HTTPS.

Respaldo privado conservado en el servidor:
`/var/backups/zuni/demo/20261029T182809Z-pre-1.2.0-gRDjzF`.
No se incluyen respaldos, contraseñas ni claves en este repositorio.

Pendientes: revisión manual de los flujos autenticados y documento legal
institucional. Se mantiene la política existente hasta recibir ese documento.
