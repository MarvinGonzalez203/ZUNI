# ZUNI — Guía de despliegue en producción

Esta guía no contiene contraseñas, cadenas de conexión ni claves reales. Todos los valores
entre `<...>` son marcadores que debes reemplazar en el servidor, nunca en el repositorio.

## 1. Entorno

- Variable obligatoria: `ASPNETCORE_ENVIRONMENT=Production`.
  - Activa la cookie de sesión y la cookie antiforgery con `Secure` (solo HTTPS).
  - Activa `UseExceptionHandler` y HSTS.
  - Desactiva la generación de enlaces de recuperación de contraseña (solo existen en Development).
- Runtime: ASP.NET Core 8 (`net8.0`).
- Publicar con: `dotnet publish Zuni/Zuni.csproj -c Release -o <carpeta-de-publicacion>`.

## 2. Cadena de conexión

La aplicación lee `ConnectionStrings:DefaultConnection`. Si falta, se detiene al iniciar con un mensaje claro.

- Development: User Secrets (`dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena>" --project Zuni`).
- Production: variable de entorno
  `ConnectionStrings__DefaultConnection=Host=<host>;Port=5432;Database=<db>;Username=<usuario>;Password=<secreto>;SSL Mode=Require`
- Recomendado:
  - Usuario de base de datos propio de ZUNI con los permisos mínimos sobre su base (no `postgres`).
  - `SSL Mode=Require` (o `VerifyFull` con certificado) si la base no está en el mismo servidor.
  - Nunca escribir la cadena en `appsettings*.json`, scripts versionados ni documentación.

## 3. HTTPS

- Publicar siempre detrás de HTTPS (certificado válido).
- Si hay un proxy inverso (Nginx, Caddy, Apache) que termina TLS, configurar `Zuni__ProxyInverso=true`
  para que ZUNI use la IP y el esquema reales (`X-Forwarded-For`, `X-Forwarded-Proto`). Esto es necesario
  para que el límite de intentos por IP no trate a todos los usuarios como una sola IP.
  Si el proxy no está en la misma máquina, agregar su IP en `KnownProxies` (Program.cs).
- Con IIS (in-process) no hace falta `Zuni__ProxyInverso`.

## 4. Claves de Data Protection (cookies y antiforgery)

- Por defecto se guardan en `<contentRoot>/App_Data/keys`.
- En producción usar una carpeta persistente fuera de la carpeta de publicación:
  `DataProtection__KeysPath=<ruta-persistente>` (por ejemplo `D:\zuni-data\keys` o `/var/lib/zuni/keys`).
- Permisos: solo la cuenta que ejecuta ZUNI debe poder leer y escribir esa carpeta.
- Si la carpeta se pierde en cada despliegue, todos los usuarios quedan desconectados y los formularios abiertos fallan.
- **Antes de producción, rotar:** no copiar las claves de tu PC al servidor. Deja que ZUNI genere claves
  nuevas en la carpeta persistente del servidor (se crean solas al primer arranque).
- Las claves de desarrollo de tu PC (`Zuni/App_Data/keys/*.xml`) no deben compartirse ni subirse a Git o a ZIPs.

## 5. Copias de seguridad

- Antes de aplicar migraciones: respaldo completo, por ejemplo
  `pg_dump -Fc -h <host> -U <usuario> -d <db> -f zuni-<fecha>.dump` (la contraseña se pide de forma interactiva o por `PGPASSFILE`, nunca en la línea de comandos).
- Programar respaldos periódicos cifrados y probar al menos una restauración en una base descartable.
- Los respaldos contienen datos clínicos: guardarlos con el mismo nivel de protección que la base.

## 6. Migraciones

ZUNI **no** aplica migraciones automáticamente al iniciar.

1. Generar un script idempotente revisable (no abre la base):
   `dotnet ef migrations script --idempotent --project Zuni/Zuni.csproj --startup-project Zuni/Zuni.csproj -o zuni-migraciones.sql -- --environment Production --contentRoot <ruta>/Zuni`
   `ApplicationDbContextFactory` exige que `ConnectionStrings:DefaultConnection` exista para el entorno indicado,
   aunque generar el script no abre la conexión.
2. Revisarlo y probarlo primero en una copia de la base de producción.
3. Respaldar (sección 5) y aplicar el script en producción en una ventana de mantenimiento.
4. Nunca ejecutar `Down`/`database update <anterior>` sobre datos reales sin revisión: algunas migraciones
   antiguas (`AddFlujoAtencionInicial`, `AddBigFiveApoyo`, `AddAgendaYCitas`) eliminan tablas en su `Down`.

Este endurecimiento **no agregó migraciones** ni cambios de esquema.

## 7. Primer Administrador

ZUNI crea los roles al iniciar, pero no crea usuarios. Si la base de producción no tiene ningún Administrador activo:

```
dotnet Zuni.dll --crear-primer-admin=true --correo <correo-valido> --nombre "<Nombre completo>"
```

(o `dotnet run --project Zuni -- --crear-primer-admin=true --correo ... --nombre "..."` desde el código fuente).

- Solo funciona si no existe ningún Administrador activo; en otro caso no cambia nada.
- Muestra una contraseña temporal **una vez** en la consola y obliga a cambiarla al primer inicio de sesión.
- Ejecutarlo de forma interactiva (no como servicio) para que la contraseña no quede en registros.
- No hay credenciales en código ni en variables de entorno.
- Después, el Administrador crea el resto de cuentas desde el panel.
- Para instalaciones independientes en Debian, usar también [el procedimiento por institución](instalaciones-debian.md), los archivos de `deploy/debian` y un `Zuni__InstallationId` propio. El identificador separa cookies y propósitos de Data Protection; conservar claves y base propias por instalación.
- El primer Administrador puede usar cualquier correo válido para configurar una institución diferente.
  Después debe abrir **Configuración de la institución** y elegir la política de registro y correo.
  La configuración inicial conserva `miumg.edu.gt` para los registros y altas ordinarias.
  Ver [Configuración institucional](configuracion-institucion.md).

## 8. Qué NO subir al servidor ni a Git

- `Zuni/App_Data/` (claves de Data Protection y archivos locales).
- `.visual-check/`, `.clean-check/`, `.verification-build/`, `.build-appdata/` (contienen `users.json` con datos personales y hashes de una versión antigua).
- `bin/`, `obj/`, `.vs/`, `*.user`.
- `secrets.json`, `.env`, certificados (`*.pfx`, `*.pem`, `*.key`).
- ZIPs del proyecto que incluyan cualquiera de los anteriores.

Publicar solo el resultado de `dotnet publish`.

## 9. Límites de acceso (rate limiting)

Configurables por variables de entorno (valores por defecto entre paréntesis):

- `Zuni__LimitesAcceso__LoginPorMinuto` (30 POST de inicio de sesión por IP y minuto).
- `Zuni__LimitesAcceso__RegistroPorDiezMinutos` (20 registros por IP cada 10 minutos).
- `Zuni__LimitesAcceso__RecuperacionPorDiezMinutos` (10 por IP cada 10 minutos).

Al excederse se responde HTTP 429 sin detalles internos. Además, cada cuenta se bloquea 15 minutos tras 5 intentos fallidos consecutivos.

## 10. Lista de verificación final

- [ ] `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] `ConnectionStrings__DefaultConnection` con usuario de permisos mínimos y SSL.
- [ ] `DataProtection__KeysPath` apuntando a una carpeta persistente con permisos restringidos.
- [ ] HTTPS activo; `Zuni__ProxyInverso=true` si hay proxy inverso.
- [ ] Respaldo previo + script idempotente de migraciones aplicado y verificado.
- [ ] Primer Administrador creado y contraseña cambiada.
- [ ] Ninguna carpeta de la sección 8 en el servidor.
- [ ] Prueba manual: iniciar sesión, autorregistro + activación, asignación manual, registro de atención.
