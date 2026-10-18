# Una instalación ZUNI por institución

La suscripción puede cubrir licencia, soporte y mantenimiento de una instalación. Cada institución conserva su propia aplicación, base, credenciales, claves y respaldos. Este cambio no agrega un sistema de cobros ni mezcla instituciones en una base compartida.

## Estructura por instalación

Cada institución tiene su propio servidor (o, en ensayos, su propia instancia aislada), base, usuarios PostgreSQL, usuario de sistema, archivo de entorno y carpeta de datos. Para el identificador `institucion-a`:

| Ruta | Propietario y permisos | Contenido |
|---|---|---|
| `/opt/zuni/institucion-a/current` | root, solo lectura para el servicio | Resultado de `dotnet publish`. Nunca contiene claves ni `App_Data`. |
| `/etc/zuni/institucion-a.env` | root:root, 600 | Entorno de ejecución ([ejemplo](../deploy/debian/institucion.env.example)). |
| `/etc/zuni/institucion-a-migracion.env` | root:root, 600 | Entorno de mantenimiento con el usuario de migración ([ejemplo](../deploy/debian/institucion-migracion.env.example)). Solo existe durante el mantenimiento. |
| `/var/lib/zuni/institucion-a/` | zuni-institucion-a, 700 | Única ruta escribible por el servicio (`ReadWritePaths` de [zuni@.service](../deploy/debian/zuni@.service)). |
| `/var/lib/zuni/institucion-a/dataprotection/` | zuni-institucion-a, 700 | Claves de Data Protection (`DataProtection__KeysPath`): cookies y antiforgery. |
| `/var/lib/zuni/institucion-a/seguridad/claves.json` | zuni-institucion-a, 600 | Anillo de claves del cifrado clínico (`SeguridadClinica__ClavesPath`). |
| `/var/lib/zuni/institucion-a/auditoria/bitacora.jsonl` | zuni-institucion-a, 600 | Copia externa firmada de la auditoría (`SeguridadClinica__BitacoraPath`). |

Las rutas de seguridad deben ser absolutas y estar fuera de la publicación: ZUNI se niega a arrancar en Production si faltan o si están dentro de la carpeta de la aplicación.

## Dos usuarios PostgreSQL por instalación

| Usuario | Para qué | Permisos |
|---|---|---|
| `zuni_institucion_a_migrador` | Aplicar scripts de migraciones EF, el script de permisos y `--migrar-seguridad` | Propietario de la base y de sus tablas. Sin superusuario. Su contraseña no se guarda en el entorno del servicio. |
| `zuni_institucion_a_app` | Ejecución normal, primer Administrador, exportación y verificación de auditoría, respaldos | Sin propiedad de tablas, sin `CREATE` en el esquema, sin superusuario, `CREATEROLE`, `CREATEDB` ni `BYPASSRLS`. Solo `SELECT, INSERT` en las tablas de auditoría, consentimientos y aceptaciones, y solo `SELECT` en `EstadoProteccion`. |

En Production, ZUNI comprueba estos privilegios al iniciar y **no arranca** si el usuario de la cadena de conexión es propietario de tablas, puede crear objetos o puede modificar o borrar evidencias. Usar el usuario de migración en `institucion-a.env` impide el arranque. Es intencional.

Usa el usuario de migración únicamente en estos casos, con el servicio detenido:

1. aplicar el script de migraciones;
2. volver a aplicar [permisos-runtime.sql.example](../deploy/debian/permisos-runtime.sql.example) después de cada migración;
3. ejecutar `--migrar-seguridad`.

Todo lo demás usa el usuario de ejecución.

## Instalación nueva paso a paso

Los comandos son de referencia para el servidor de la institución. Revisa cada valor antes de ejecutarlo. No copies claves, bases ni `App_Data` desde un PC de desarrollo.

1. **Servidor.** Debian 12 con ASP.NET Core Runtime 8, PostgreSQL, Nginx y Certbot según sus instrucciones oficiales. Abre solo SSH y 80/443; PostgreSQL y Kestrel escuchan en loopback.
2. **Usuario de sistema y carpetas.** Crea `zuni-institucion-a` sin inicio de sesión, `/opt/zuni/institucion-a/current` y `/var/lib/zuni/institucion-a/{dataprotection,seguridad,auditoria}` con propietario `zuni-institucion-a` y modo 700.
3. **PostgreSQL.** Como administrador de PostgreSQL crea los dos usuarios con `LOGIN` y sin privilegios especiales, y asigna sus contraseñas de forma interactiva con `\password`. Crea la base `zuni_institucion_a` con propietario `zuni_institucion_a_migrador`. Revoca `CONNECT` de `PUBLIC` y concédelo solo a los dos usuarios.
4. **Publicación.** `dotnet publish Zuni/Zuni.csproj -c Release -o <salida>` en la máquina de compilación. Copia solo `<salida>` a `/opt/zuni/institucion-a/current`.
5. **Entornos.** Copia `institucion.env.example` a `/etc/zuni/institucion-a.env` y `institucion-migracion.env.example` a `/etc/zuni/institucion-a-migracion.env`. Reemplaza dominio, puerto, identificador y contraseñas. `Zuni__InstallationId` y las tres rutas deben ser idénticos en ambos archivos.
6. **Respaldo previo.** En una base nueva no hay datos, pero sí en una actualización: ver [Respaldo y restauración](#respaldo-y-restauración) y la sección 5 de [deployment.md](deployment.md).
7. **Migraciones EF.** Genera el script idempotente según [deployment.md](deployment.md#6-migraciones), revísalo y aplícalo con el usuario de migración (`psql -U zuni_institucion_a_migrador -d zuni_institucion_a -f zuni-migraciones.sql`).
8. **Permisos del usuario de ejecución.** Revisa y aplica `permisos-runtime.sql.example` con el usuario de migración, sustituyendo el nombre del usuario de ejecución.
9. **Inicialización de seguridad.** Crea el anillo de claves del cifrado clínico con el usuario del servicio, para que el archivo le pertenezca:
   `systemd-run --pty --wait --collect -p User=zuni-institucion-a -p EnvironmentFile=/etc/zuni/institucion-a.env -p WorkingDirectory=/opt/zuni/institucion-a/current /usr/bin/dotnet Zuni.dll --inicializar-seguridad`
   La orden no reemplaza una clave existente.
10. **Copia segura de claves.** Copia `seguridad/claves.json` cifrado a un almacenamiento separado del servidor y de los respaldos de la base, con custodia distinta del administrador de PostgreSQL. **Si se pierde, los textos clínicos cifrados no se pueden recuperar**: ni el respaldo de la base ni un nuevo archivo de claves lo resuelven. Lo mismo ocurre si cambia `Zuni__InstallationId`.
11. **Conversión de datos sensibles.** Con el servicio detenido, ejecuta `--migrar-seguridad` con el entorno de mantenimiento (usuario de migración). En una base nueva solo instala las restricciones y la marca de conversión; en una existente cifra los textos actuales:
    `systemd-run --pty --wait --collect -p User=zuni-institucion-a -p EnvironmentFile=/etc/zuni/institucion-a-migracion.env -p WorkingDirectory=/opt/zuni/institucion-a/current /usr/bin/dotnet Zuni.dll --migrar-seguridad`
12. **Exportación y verificación de seguridad.** Con el entorno de ejecución, ejecuta igual `--exportar-seguridad` y después `--verificar-seguridad`. La verificación compara firmas, copia externa y registros.
13. **Arranque normal.** Instala `zuni@.service`, ejecuta `systemctl daemon-reload` y `systemctl enable --now zuni@institucion-a`. Revisa `systemctl status zuni@institucion-a` y los registros sin copiar secretos. Si no arranca, los mensajes indican qué falta: conversión, rutas de seguridad o privilegios del usuario de ejecución.
14. **Nginx y HTTPS.** Usa [nginx.conf.example](../deploy/debian/nginx.conf.example) con el dominio y puerto de la instalación y ejecuta `nginx -t` antes de recargar. Cuando el DNS apunte al servidor, usa Certbot para el certificado y la redirección a HTTPS, y comprueba la renovación. No habilites usuarios ni datos personales antes de verificar TLS. ZUNI activa HSTS y cookies `Secure` en Production, y `Zuni__ProxyInverso=true` usa la IP y el esquema reales detrás de Nginx.
15. **Primer Administrador.** Ejecuta de forma interactiva con el entorno de ejecución:
    `systemd-run --pty --wait --collect -p User=zuni-institucion-a -p EnvironmentFile=/etc/zuni/institucion-a.env -p WorkingDirectory=/opt/zuni/institucion-a/current /usr/bin/dotnet Zuni.dll --crear-primer-admin=true --correo <correo> --nombre "<Nombre completo>"`
    La contraseña temporal aparece una sola vez. No la copies a registros.
16. **Configuración institucional.** El Administrador cambia su contraseña, configura institución, identidad visual y política de correo, y publica la política de privacidad. Sin una política publicada, el registro público permanece cerrado.
17. **Retiro del entorno de mantenimiento.** Vacía o elimina la contraseña de `/etc/zuni/institucion-a-migracion.env` hasta la próxima actualización.

### Actualización de una instalación existente

Detén el servicio, respalda la base y la carpeta `/var/lib/zuni/institucion-a/` y verifica una restauración. Después repite los pasos 7, 8, 11 y 12 (el paso 11 no vuelve a cifrar datos ya convertidos) y arranca el servicio. Nunca vuelvas a un binario anterior contra una base ya cifrada. Ver [proteccion-clinica-y-auditoria.md](proteccion-clinica-y-auditoria.md).

### Data Protection persistente

`DataProtection__KeysPath` debe apuntar a la carpeta persistente de la instalación. Si se pierde o cambia, se cierran las sesiones y fallan los formularios abiertos, pero no se pierden datos. El cifrado clínico usa un anillo distinto (`claves.json`); ese sí es imprescindible para leer los datos. Ambas carpetas se respaldan aparte y nunca se comparten entre instituciones.

Una segunda institución repite todo el procedimiento con otro servidor, dominio, base, usuarios PostgreSQL, usuario de sistema, identificador y claves. No copies cuentas, claves ni expedientes de otra institución.

## Respaldo y restauración

El operador configura `PGHOST`, `PGPORT`, `PGUSER`, `PGDATABASE` y `PGPASSFILE` con permisos 600; no pasa contraseñas en argumentos. Ejecuta:

```bash
bash backup.sh institucion-a /var/backups/zuni/institucion-a
bash restore-test.sh /ruta/respaldo.dump zuni_restore_institucion_a_ensayo
```

El respaldo verifica que el archivo pueda leerse. La restauración crea una base descartable nueva, falla si ya existe y no borra ni reemplaza bases. Después compara cantidades, versiones de migración y los flujos de consulta de la copia. Programa el respaldo con el operador de la instalación, cifra las copias antes de enviarlas a almacenamiento independiente y registra fecha, resultado y plazo de conservación conforme a la política institucional. Conserva también las claves persistentes bajo acceso restringido, separadas por instalación. Mantener una copia solamente en el VPS no cubre la pérdida del servidor.

Para programarlo con systemd, instala `backup.sh` en `/usr/local/libexec/zuni/`, las unidades `zuni-backup@.service` y `zuni-backup@.timer` en `/etc/systemd/system/`, y configura `/etc/zuni/institucion-a-backup.env` a partir del ejemplo. Crea `/var/backups/zuni/institucion-a` con propietario `zuni-institucion-a` y modo 700; el archivo `backup.pgpass` pertenece a ese usuario y tiene modo 600. Tras `systemctl daemon-reload`, prueba `systemctl start zuni-backup@institucion-a`, revisa el archivo y ejecuta `systemctl enable --now zuni-backup@institucion-a.timer`. El timer realiza la copia a las 03:00, hora del servidor, con hasta 15 minutos de demora aleatoria. Comprueba `systemctl list-timers` y una restauración. El cifrado, almacenamiento externo y retención se configuran con la institución; el script no elimina respaldos automáticamente.

## Ensayo reproducible local

> **Pendiente:** este ensayo es anterior a la protección clínica del 16 de octubre. No configura `SeguridadClinica__*`, no separa el usuario de migración del de ejecución ni ejecuta `--inicializar-seguridad`/`--migrar-seguridad`, por lo que en Production la aplicación del ensayo ya no arranca. Debe actualizarse antes de repetirlo.

`tests/instalaciones-debian.ps1` usa PostgreSQL y ASP.NET Core en imágenes Debian Bookworm, dos bases con propietarios diferentes y dos aplicaciones sin root. Comprueba que las credenciales de una institución no entren a la otra, muestra identidad diferente, verifica claves separadas y su persistencia al reiniciar, crea un respaldo y lo restaura en una tercera base. Todos los datos son ficticios. Retira al finalizar solamente sus contenedores y red temporal.

1. Publica en una carpeta de ensayo que contenga `publish/`.
2. Construye `docker build -f deploy/runtime.Dockerfile -t zuni-r08-check:20261014 <carpeta-de-ensayo>`.
3. Genera el script completo de migraciones en un archivo fuera del repositorio.
4. Ejecuta `./tests/instalaciones-debian.ps1 -MigrationSql <archivo.sql>`.

La prueba no instala Nginx/systemd en Linode ni obtiene un certificado público. La publicación final y la verificación de DNS, HTTPS, renovación y respaldo externo quedan para el servidor del día 17; no deben confundirse con el ensayo local.

Ensayo del 14 de octubre de 2026: 29 comprobaciones aprobadas en Debian/PostgreSQL, incluidas la ejecución de los scripts entregados y la negativa a restaurar sobre una base existente. Se retiraron sus contenedores y red temporal. La imagen se construyó solamente desde la publicación, sin `App_Data`, secretos ni claves de desarrollo. Se aplicó además la migración de identidad a `ZuniDb` local con respaldo previo, manteniendo los usuarios y sus roles.

Verificación conjunta de esta entrega: compilación Release sin errores ni advertencias; 420 comprobaciones de seguridad y módulos asociados, 314 de agenda, 48 de formularios en navegador y 29 del ensayo Debian. Total: 811. El aislamiento de bases se probó realmente en PostgreSQL; esto no sustituye una prueba de solicitudes concurrentes de agenda en PostgreSQL.

Referencias: [Microsoft: ASP.NET Core con Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-8.0), [PostgreSQL: pg_dump](https://www.postgresql.org/docs/current/app-pgdump.html), [PostgreSQL: pg_restore](https://www.postgresql.org/docs/current/app-pgrestore.html).
