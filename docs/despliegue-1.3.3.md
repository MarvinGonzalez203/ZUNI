# Publicación de ZUNI 1.3.3

Estado: paquete subido y verificado; activación pendiente del comando con `sudo`
en la sesión SSH del operador. No marcar como publicado hasta comprobar el
manifiesto activo, el servicio y las respuestas HTTPS.

- Sitio: https://zuni.lat/.
- Código del paquete: `20fcb9e27813ca7b70a615fb21d886a8a5ced494`, rama `feature/director-V2`.
- Instalación previa comprobada por SSH: `1.2.0-e70aa3f-pZLhkB`, servicio `zuni-demo` activo.
- Paquete creado desde un archivo de Git del commit; se excluyeron cambios locales
  de privacidad sin commit, claves, persistencia y configuración de desarrollo.
- Publicación portable de ASP.NET Core 8, dependiente del runtime del servidor.
- Carpeta privada de carga: `/home/marlon/zuni-subida/actualizacion-1.3.3`.
- SHA-256 del archivo de publicación:
  `dc603f3f050821365efc0eb1eff6db839352ed78272e2cea3279c3bfbda0d6c0`.

El servidor verificó las tres sumas de `SHA256SUMS`, la sintaxis Bash del
actualizador, el manifiesto y la ausencia de rutas privadas en el archivo.
La compilación del paquete terminó correctamente con acceso a NuGet.

## Activación

En MobaXterm, conectado a la instalación como `marlon`:

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.3.3/actualizar.sh
```

El script versionado está en `deploy/debian/actualizar-demo-1.3.3.sh`. No cambia
usuarios PostgreSQL ni permisos existentes. Comprueba el esquema esperado,
respalda base, persistencia, configuración y binarios, restaura el respaldo en
una base temporal aislada y prueba allí la migración. Solo después aplica la
migración a producción y activa los nuevos binarios con un cambio de enlace.
Conserva las claves e identificadores de la instalación y verifica su integridad.

La única migración desde 1.2.0 es
`20261111063900_PermitirAtencionProfesionalSinSolicitud`: elimina el `NOT NULL`
de `RegistrosAtencionCita.SolicitudAtencionId` y registra su aplicación en el
historial EF. El SQL idempotente se generó con EF; no borra ni sustituye datos.

Durante la actualización se detiene brevemente la aplicación para obtener una
copia consistente. Ante un fallo, se conserva el respaldo y se intenta arrancar
el servicio anterior sin restaurar ni borrar la base viva. La base temporal se
elimina al terminar la prueba. Los respaldos quedan privados en
`/var/backups/zuni/demo/` y la ruta concreta se registra en `respaldo-ruta.txt`.

## Verificación pendiente tras activar

- Manifiesto activo con versión 1.3.3 y el commit del paquete.
- Servicio `zuni-demo` activo y migración comprobada por el script.
- HTTPS 200 en inicio, acceso y archivos estáticos actualizados.
- Aviso `COMPLETADO` del actualizador y ruta del respaldo conservado.
- Revisión manual de los flujos autenticados por el operador.

La preparación y carga no hicieron push a GitHub ni modificaron la aplicación
activa. La política publicada conserva su versión anterior; los cambios locales
de privacidad requieren una revisión y publicación independientes.
