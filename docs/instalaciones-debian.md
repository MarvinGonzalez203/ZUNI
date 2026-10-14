# Una instalación ZUNI por institución

La suscripción puede cubrir licencia, soporte y mantenimiento de una instalación. Cada institución conserva su propia aplicación, base, credenciales, claves y respaldos. Este cambio no agrega un sistema de cobros ni mezcla instituciones en una base compartida.

## Preparación de Debian/Linode

Usa Debian 12 de 64 bits para el primer despliegue y un servidor independiente por institución. Los archivos incluidos también permiten aislar servicios locales para ensayos. La creación del servidor Linode está prevista para el 17; este trabajo no crea recursos de pago ni cambia el DNS.

1. Instala ASP.NET Core Runtime 8, PostgreSQL, Nginx y Certbot siguiendo las instrucciones oficiales. Publica desde el proyecto con `dotnet publish Zuni/Zuni.csproj -c Release -o <salida>`. No transfieras el código completo, secretos ni claves de tu PC.
2. Elige un identificador como `institucion-a`. Crea el usuario de servicio `zuni-institucion-a` sin inicio de sesión y las carpetas `/opt/zuni/institucion-a/current`, `/var/lib/zuni/institucion-a/keys` y `/etc/zuni`. El usuario del servicio necesita leer la publicación y escribir solamente sus datos persistentes. El archivo de entorno pertenece a root, modo 600.
3. Crea el rol PostgreSQL `zuni_institucion_a` sin permisos de superusuario, creación de roles ni bases. Configura su contraseña de forma interactiva mediante `\password zuni_institucion_a` en psql. Crea `zuni_institucion_a` con ese rol como propietario. Revoca `CONNECT` de PUBLIC y concede acceso únicamente al rol de esa instalación. No publiques el puerto 5432; usa loopback o socket local.
4. Genera y revisa el script de migraciones indicado en [deployment.md](deployment.md). Para una base nueva aplica todas las migraciones con su propietario. Para una base existente, respalda y usa un script idempotente revisado. No ejecutes migraciones de otra aplicación sobre ella.
5. Copia `deploy/debian/institucion.env.example` como `/etc/zuni/institucion-a.env` y reemplaza todos los valores. `Zuni__InstallationId`, base, usuario, carpeta de claves, dominio y puerto deben ser propios de la instalación. `SSL Mode=Disable` del ejemplo corresponde solamente a PostgreSQL en loopback; una base remota requiere verificación TLS.
6. Copia `deploy/debian/zuni@.service` a `/etc/systemd/system/`. Ejecuta `systemctl daemon-reload` y `systemctl enable --now zuni@institucion-a`. Comprueba `systemctl status zuni@institucion-a` y los registros sin compartir secretos. El proceso funciona sin root y con restricciones de escritura.
7. Usa el ejemplo de Nginx con el dominio y puerto de esta institución. Ejecuta `nginx -t` antes de recargar. Cuando el servidor exista, en Namecheap configura el registro A de `@` hacia su IPv4; opcionalmente un CNAME de `www` hacia `zuni.lat` y agrega ese nombre a Nginx y AllowedHosts. Elimina registros de estacionamiento que ocupen esos mismos nombres; conserva los registros de correo. No agregues AAAA sin IPv6 funcional.
8. Con el DNS resuelto, usa Certbot para instalar el certificado de `zuni.lat` y habilitar la redirección HTTPS. Comprueba la renovación y el acceso HTTPS antes de habilitar usuarios. Mantén abiertos solo SSH y 80/443 según la política del servidor; Kestrel queda en loopback.
9. Ejecuta interactivamente el comando de primer administrador de [deployment.md](deployment.md), usando el mismo entorno y usuario del servicio. No lo ejecutes como servicio ni guardes su contraseña temporal en registros. Cambia la contraseña y configura institución, identidad visual y política de privacidad desde Administración.

Una segunda institución requiere repetir el procedimiento con otro servidor, dominio, base, usuario y claves. No copies cuentas ni expedientes de otra institución. `Zuni__InstallationId` separa los propósitos criptográficos y el nombre de la cookie; las carpetas persistentes y bases separadas siguen siendo obligatorias. Si una instalación ya opera con sesiones, cambiar su identificador requiere volver a iniciar sesión.

## Respaldo y restauración

El operador configura `PGHOST`, `PGPORT`, `PGUSER`, `PGDATABASE` y `PGPASSFILE` con permisos 600; no pasa contraseñas en argumentos. Ejecuta:

```bash
bash backup.sh institucion-a /var/backups/zuni/institucion-a
bash restore-test.sh /ruta/respaldo.dump zuni_restore_institucion_a_ensayo
```

El respaldo verifica que el archivo pueda leerse. La restauración crea una base descartable nueva, falla si ya existe y no borra ni reemplaza bases. Después compara cantidades, versiones de migración y los flujos de consulta de la copia. Programa el respaldo con el operador de la instalación, cifra las copias antes de enviarlas a almacenamiento independiente y registra fecha, resultado y plazo de conservación conforme a la política institucional. Conserva también las claves persistentes bajo acceso restringido, separadas por instalación. Mantener una copia solamente en el VPS no cubre la pérdida del servidor.

Para programarlo con systemd, instala `backup.sh` en `/usr/local/libexec/zuni/`, las unidades `zuni-backup@.service` y `zuni-backup@.timer` en `/etc/systemd/system/`, y configura `/etc/zuni/institucion-a-backup.env` a partir del ejemplo. Crea `/var/backups/zuni/institucion-a` con propietario `zuni-institucion-a` y modo 700; el archivo `backup.pgpass` pertenece a ese usuario y tiene modo 600. Tras `systemctl daemon-reload`, prueba `systemctl start zuni-backup@institucion-a`, revisa el archivo y ejecuta `systemctl enable --now zuni-backup@institucion-a.timer`. El timer realiza la copia a las 03:00, hora del servidor, con hasta 15 minutos de demora aleatoria. Comprueba `systemctl list-timers` y una restauración. El cifrado, almacenamiento externo y retención se configuran con la institución; el script no elimina respaldos automáticamente.

## Ensayo reproducible local

`tests/instalaciones-debian.ps1` usa PostgreSQL y ASP.NET Core en imágenes Debian Bookworm, dos bases con propietarios diferentes y dos aplicaciones sin root. Comprueba que las credenciales de una institución no entren a la otra, muestra identidad diferente, verifica claves separadas y su persistencia al reiniciar, crea un respaldo y lo restaura en una tercera base. Todos los datos son ficticios. Retira al finalizar solamente sus contenedores y red temporal.

1. Publica en una carpeta de ensayo que contenga `publish/`.
2. Construye `docker build -f deploy/runtime.Dockerfile -t zuni-r08-check:20261014 <carpeta-de-ensayo>`.
3. Genera el script completo de migraciones en un archivo fuera del repositorio.
4. Ejecuta `./tests/instalaciones-debian.ps1 -MigrationSql <archivo.sql>`.

La prueba no instala Nginx/systemd en Linode ni obtiene un certificado público. La publicación final y la verificación de DNS, HTTPS, renovación y respaldo externo quedan para el servidor del día 17; no deben confundirse con el ensayo local.

Ensayo del 14 de octubre de 2026: 29 comprobaciones aprobadas en Debian/PostgreSQL, incluidas la ejecución de los scripts entregados y la negativa a restaurar sobre una base existente. Se retiraron sus contenedores y red temporal. La imagen se construyó solamente desde la publicación, sin `App_Data`, secretos ni claves de desarrollo. Se aplicó además la migración de identidad a `ZuniDb` local con respaldo previo, manteniendo los usuarios y sus roles.

Verificación conjunta de esta entrega: compilación Release sin errores ni advertencias; 420 comprobaciones de seguridad y módulos asociados, 314 de agenda, 48 de formularios en navegador y 29 del ensayo Debian. Total: 811. El aislamiento de bases se probó realmente en PostgreSQL; esto no sustituye una prueba de solicitudes concurrentes de agenda en PostgreSQL.

Referencias: [Microsoft: ASP.NET Core con Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-8.0), [PostgreSQL: pg_dump](https://www.postgresql.org/docs/current/app-pgdump.html), [PostgreSQL: pg_restore](https://www.postgresql.org/docs/current/app-pgrestore.html).
