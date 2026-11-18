# Preparar y subir ZUNI 1.6.2 a Linode / Debian 13

Paquete para **ZUNI y la portada de Denis Corp**. No inicia Academy, Fulleo, Colegios, Logística ni POS. Las fichas conservan sus avances y límites. La raíz mostrará Denis Corp y ZUNI seguirá disponible en `/deniscorp/zuni`; se conservan `/Cuenta/...` y las direcciones actuales de la aplicación. No se cambia DNS, Nginx, correo ni el identificador de cifrado.

## 1. Copiar desde MobaXterm

Extrae el ZIP local y arrastra la carpeta `actualizacion-1.6.2` al panel SFTP de tu sesión Linode, dentro de:

```text
/home/marlon/zuni-subida/
```

Debe quedar `/home/marlon/zuni-subida/actualizacion-1.6.2/actualizar.sh`. Copia la carpeta completa, incluido `comprobador`, los SQL, el tar y `SHA256SUMS`. No copies bases locales, `.env`, claves o archivos del NAS.

## 2. Comprobar el runtime

La aplicación usa **ASP.NET Core 10**, no basta el runtime de consola ni el 8 anterior. Debian 13 es compatible según [Microsoft](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian?tabs=dotnet10). Instala el parche 10 ofrecido por el repositorio oficial, conservando el runtime anterior para reversión.

```bash
dotnet --list-runtimes
```

Si no aparece `Microsoft.AspNetCore.App 10.0...`, y el repositorio Microsoft ya está configurado:

```bash
sudo apt-get update
sudo apt-get install aspnetcore-runtime-10.0
```

Si APT no encuentra ese paquete, configura primero el repositorio oficial de Debian 13 siguiendo el enlace de Microsoft. No instalar paquetes de Debian 12 ni sustituir el sistema operativo. El actualizador no ejecuta APT por su cuenta.

## 3. Ejecutar la comprobación

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.6.2/actualizar.sh --comprobar
```

No detiene el servicio ni cambia la base. Verifica sumas, contenido del paquete, Debian, runtime, instalación previa, espacio, migraciones y que las rutas privadas/conexión coincidan con este perfil. Guarda una copia del paquete bajo control de root para evitar usar archivos cambiantes durante la actualización.

Este perfil corresponde a la instalación documentada: servicio `zuni-demo`, usuario `zuni-demo`, entorno privado `/etc/zuni/demo.env`, enlace `/opt/zuni/demo/current`, persistencia `/var/lib/zuni/demo`, PostgreSQL 18 local por socket en el puerto 5433, base `zuni`, roles `zuni_migrador` y `zuni_app`. Requiere una versión previa documentada 1.3.3–1.6.2 y el historial esperado. **Si difiere, se detiene para ajustar el perfil; no crea una instalación nueva sobre la existente.** No pegues contraseñas ni el contenido de `demo.env` en el chat.

## 4. Activar en una ventana de mantenimiento

Solo después de obtener `PREPARADO`:

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.6.2/actualizar.sh --aplicar
```

El comando detiene ZUNI, comprueba que no queda otro proceso con su rol de ejecución, exporta y verifica la auditoría, respalda PostgreSQL, persistencia, claves, entorno, servicio y binarios. Conserva los respaldos privados bajo `/var/backups/zuni/demo/`.

Restaura en una base temporal aislada, aplica allí el SQL idempotente y los permisos y arranca el candidato **solo en loopback**, con copias de las claves/auditoría y correo deshabilitado. Comprueba la integridad y ocho rutas antes de tocar el esquema vivo. Después aplica la misma migración en una transacción a producción, verifica conteos y claves, cambia el enlace y comprueba HTTPS. Los puertos públicos y el proxy existente se mantienen.

La migración incluye el PIN si estaba pendiente y 2FA/ausencias (`20261117015815_AgregarDobleFactorYAusencias`). No vuelve a crear cuentas, no importa estudiantes locales, no reinicializa claves ni elimina datos. Los scripts anteriores 1.3.3/1.4.0 no sirven para esta entrega.

Espera el estado `COMPLETADO`, conserva las rutas de respaldo y registro y revisa en navegador la portada, acceso, PIN, 2FA, guías, agenda y notificaciones con las cuentas autorizadas. Las comprobaciones automáticas no equivalen a una revisión autenticada de todos los roles. La recuperación por Brevo sigue pendiente de la clave privada y del remitente verificado si todavía no se configuraron.

## 5. Si falla

El actualizador conserva el respaldo y nunca restaura ni borra la base viva. Intenta iniciar el código previo cuando es compatible. Si ya existen cuentas con 2FA o citas con estados nuevos y el código anterior no los entiende, conserva la versión nueva detenida para revisión; no evade el segundo factor para recuperar disponibilidad.

Para una reversión manual **solo del código**, usando la ruta exacta del respaldo impresa al terminar:

```bash
sudo bash /home/marlon/zuni-subida/actualizacion-1.6.2/revertir.sh /var/backups/zuni/demo/REEMPLAZAR-POR-RESPALDO-REAL --aplicar
```

Se verifican respaldo, versión actual, rutas y compatibilidad antes de parar. La base, claves y entorno no se restauran ni se sustituyen. La copia completa de base solo se usaría en una recuperación planificada, pues restaurarla podría perder operaciones posteriores. La custodia cifrada externa y retención siguen siendo tareas del operador; guardar un respaldo solo en el VPS no cubre la pérdida del servidor.

## Alcance de validación

El paquete se construye desde un commit Git, excluyendo los cambios locales pendientes de privacidad. Contiene manifiesto con versión/commit y sumas SHA-256. El diagnóstico de la instalación real y el ensayo Debian se ejecutan allí: la preparación local no declara la actualización publicada. UI-03 y las condiciones de infraestructura previas continúan documentadas; no se certifica 100% bilingüe ni se activan servicios adicionales.
