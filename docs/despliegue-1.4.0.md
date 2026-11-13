# Publicación de ZUNI 1.4.0

Estado: paquete subido y verificado; pendiente de activación por el operador
mediante `sudo` en MobaXterm. No declarar publicado hasta comprobar la salida
`COMPLETADO`, el servicio, el manifiesto activo y HTTPS.

- Código del producto: `5287a0d4a12f37d641869e745f72bd0649403bb8`, rama `feature/director-V2`.
- Instalación previa confirmada por SSH: `1.3.3-20fcb9e-zKqQ2g`, servicio `zuni-demo` activo.
- Publicación limpia y portable de ASP.NET Core 8, creada desde un archivo Git
  del commit probado, con el identificador de ese commit en el ensamblado.
- Se excluyeron la configuración de desarrollo, claves, persistencia, símbolos
  propios de depuración y los cambios locales pendientes de privacidad.
- Carga privada: `/home/marlon/zuni-subida/actualizacion-1.4.0`.
- SHA-256 de `zuni-publicacion.tar.gz`:
  `cfc3a9f644a600fa701983746a9404967fa18abf2086c30208721a11d995eb8c`.

El servidor verificó las cuatro sumas SHA-256, la sintaxis Bash, el manifiesto
1.4.0 y la ausencia de rutas privadas en el archivo. La preparación no detuvo
ni modificó la aplicación activa.

## Comando de activación

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.4.0/actualizar.sh
```

El actualizador versionado es `deploy/debian/actualizar-demo-1.4.0.sh`. Respalda
base, persistencia, configuración y binarios con el servicio detenido. Restaura
el respaldo en una base temporal aislada, aplica allí el SQL idempotente del PIN
y verifica la tabla y los tres permisos de ejecución. Solo después aplica esos
cambios a producción y cambia el enlace a los nuevos binarios. Conserva claves,
usuarios, perfiles y atenciones; no restaura una base local sobre producción.

Migración: `20261113061106_AgregarPinProfesional`. Crea únicamente
`PinesProfesionales` y registra la migración en el historial EF. Los permisos
del nuevo objeto están en `deploy/debian/permisos-pin-profesional.sql`; no se
amplían permisos sobre notas ni evidencias. El paquete incluye copias de ambos
SQL, el script y sus sumas SHA-256.

Ante un fallo se conserva el respaldo y se intenta arrancar la versión anterior
sin borrar ni restaurar la base viva. Las copias privadas quedan bajo
`/var/backups/zuni/demo/`; la ruta concreta se guarda en `respaldo-ruta.txt`.
La prueba elimina solamente su base temporal identificada con `zuni_verif_140_`.

## Brevo pendiente

La publicación no configura ni envía correos reales. El operador todavía debe
añadir la clave API privada y un remitente verificado al entorno del servicio,
siguiendo `deploy/debian/correo-brevo.env.example`, y reiniciarlo. El correo
`zuni@gmail.com` es un ejemplo solicitado, no un remitente habilitado. Hasta
entonces la recuperación muestra un aviso claro; el PIN puede configurarse con
la contraseña actual y proteger las notas sin enviar correo.

## Comprobaciones después de activar

- Servicio activo y manifiesto 1.4.0 con el commit indicado.
- Estado `COMPLETADO`, respaldo conservado y permisos del PIN comprobados.
- HTTPS 200 en inicio, acceso, script del PIN y página de recuperación.
- Revisión manual autenticada de guías, planes, cita virtual y PIN.

Pruebas previas completadas: suite de seguridad offline, 321 comprobaciones de
agenda, 114 específicas de esta versión, 43 de navegador y 45 de historial.
Localhost responde 200 con 1.4.0 y la migración local tiene respaldo verificado.
No se hizo push a GitHub. Los cambios pendientes de privacidad siguen excluidos.
