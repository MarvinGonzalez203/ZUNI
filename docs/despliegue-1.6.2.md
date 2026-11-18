# ZUNI 1.6.2 — paquete para Linode / Debian 13

Preparación del 18/11/2026 para **ZUNI y la portada de Denis Corp**, alcance confirmado por el propietario. No activa otros productos ni modifica POS. El dominio público respondió HTTP 200 con título «Inicio - ZUNI» durante la preparación; eso no confirma versión, esquema ni configuración del servidor.

## Preparación reproducible

Desde PowerShell en el repositorio, con SDK .NET 10:

```powershell
./deploy/debian/Preparar-Paquete-1.6.2.ps1
```

La herramienta publica desde un **commit Git**, nunca desde los archivos locales modificados. Los tres cambios previos de privacidad se conservan pendientes y quedan fuera de la publicación. Genera dos publicaciones framework-dependent portables, SQL idempotente sin conexión, tar, comprobador, instrucciones y ZIP. El resultado local y los SHA-256 quedan en `.visual-check/despliegue-1.6.2/ultimo-paquete.json`. Esa carpeta ignorada no se confirma ni se sube a GitHub.

El servidor requiere ASP.NET Core Runtime 10 con su arquitectura y dependencias, según la [guía oficial de Microsoft para Debian 13](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian?tabs=dotnet10). Se conserva el runtime previo para poder volver al código anterior. El actualizador no instala paquetes APT ni cambia Nginx, DNS, correo o identificador de instalación.

## MobaXterm

Copiar la carpeta `actualizacion-1.6.2` completa al SFTP de Linode, en `/home/marlon/zuni-subida/`. Después:

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.6.2/actualizar.sh --comprobar
```

Solo al obtener `PREPARADO`, ejecutar durante mantenimiento:

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.6.2/actualizar.sh --aplicar
```

[Guía completa de Moba, requisitos y reversión](../deploy/debian/LEEME-MOBA-1.6.2.md).

## Controles del actualizador

- Perfil conservador de la instalación documentada: `zuni-demo`, `/etc/zuni/demo.env`, persistencia `/var/lib/zuni/demo`, enlace `/opt/zuni/demo/current`; PostgreSQL 18 local, puerto 5433, roles separados. Los valores privados se leen mediante systemd; el script no evalúa el archivo de entorno como código shell ni imprime credenciales. Se detiene si el perfil difiere.
- Comprobación de Debian 13, ASP.NET 10 y runtime anterior, integridad y rutas del paquete, unidad/ExecStart, esquema y espacio antes de parar. La comprobación crea una copia del paquete controlada por root, sin cambiar el servicio ni la base.
- Respaldo privado y coherente de base, datos persistentes, claves, entorno, servicio y binarios tras detener la aplicación y exportar/verificar la auditoría. Se exige que no queden conexiones de otro escritor con el rol de ejecución.
- Restauración en una base temporal nueva, permisos restringidos y migración; copia privada de claves y auditoría; arranque de la versión candidata solo en loopback con correo deshabilitado. Verificación de integridad y ocho rutas antes de aplicar SQL al esquema vivo. Al terminar se retira únicamente el clon creado por esa ejecución.
- SQL de PIN, 2FA y ausencias idempotente, sin transacciones internas: `psql --single-transaction` aplica la unidad completa. Permisos nuevos `SELECT/INSERT/UPDATE`, sin abrir auditorías ni ampliar permisos del esquema vivo.
- Se comprueban conteos previos a la activación y hashes de las claves originales; después enlace atómico, servicio activo, rutas HTTPS 200 y portada Denis Corp. Los accesos existentes de ZUNI conservan sus direcciones.
- No hay restauración automática de la base. Una reversión de código a 1.3.3/1.4.0 se bloquea si existen estados nuevos de PIN, 2FA o ausencias; nunca se elude el segundo factor para reiniciar código antiguo. La reversión manual valida el respaldo y candidato activo y conserva base, claves y entorno.

## Pruebas de preparación

El comprobador incluye autopruebas de rutas peligrosas, enlaces, archivos privados, duplicados y compatibilidad de versión. Un ensayo PostgreSQL 18 local con datos ficticios aprobó **17 comprobaciones**: actualización desde el esquema 1.3.3, segunda aplicación idempotente, conservación de usuarios/perfil/cita previa, derechos mínimos nuevos, FK, estado nuevo y aborto transaccional que conserva el esquema/historial anterior tras un error inyectado. Las dos bases y roles descartables se retiraron, y se detuvo el laboratorio.

La sintaxis Bash y PowerShell se revisa localmente. No se ejecutaron systemd/Nginx ni el arranque Linux en Linode desde esta preparación: esa comprobación se hace con el paquete allí y el ensayo automático antes de la migración viva. No declarar la versión desplegada por haber generado o copiado el ZIP.

Los respaldos de mantenimiento permanecen privados en el VPS; la custodia cifrada externa, retención y prueba física siguen pendientes conforme a DAT-01. UI-03 y la revisión de todos los roles/correos/PDF continúan documentados. Brevo necesita clave privada y remitente verificado para recuperar el PIN por correo. No se incluyen esas credenciales en el paquete.
