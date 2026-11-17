# ZUNI 1.5.3 — plataforma .NET 10 LTS

Se corrige SEG-03 de la auditoría de Denis Corp: aplicación y tres suites offline
pasan a `net10.0`; Identity/EF Core y Pkcs a 10.0.15, Npgsql a 10.0.3. El contenedor
utiliza `mcr.microsoft.com/dotnet/aspnet:10.0-noble`, cuyo manifiesto ofrece Linux
ARM64. La versión 1.5.3 y sus notas ES/EN aparecen en la campanita. La ficha de Fulleo
refleja su actualización local 0.8.1. No se han desplegado estas versiones en zuni.lat.

## Desarrollo

Instalar SDK .NET 10 LTS. `global.json` permite SDK estables 10.0 de bandas posteriores,
sin elegir accidentalmente .NET 8/9. Visual Studio necesita poder resolver ese SDK;
el runtime anterior por sí solo no basta.

En esta máquina se descargó el SDK oficial 10.0.404 en una carpeta aislada, con
SHA512 contrastado con el manifiesto oficial. No se sustituyó el SDK global ni
se desinstalaron runtimes de otros proyectos. El iniciador busca ese SDK o una
instalación .NET 10, y permite `DOTNET10_ROOT` como ruta explícita:

```powershell
./tools/dotnet10.ps1 run --project Zuni --launch-profile https
./tools/dotnet10.ps1 run --project tests/Seguridad.Offline -c Release
```

La configuración y claves privadas de desarrollo conservan sus ubicaciones previas.
Los cambios de privacidad que ya estaban pendientes no forman parte de este commit.

## Verificación

- Compilación Release sin advertencias ni errores de ZUNI y la suite general.
- Suite general de seguridad aprobada, con 176 comprobaciones del núcleo y sus grupos.
- 116 comprobaciones PIN/recuperación/guías, 56 de 2FA/ausencias y 112 HTTP de portal.
- Ensayo PostgreSQL 18 temporal: esquema y datos ficticios creados con EF 8, dump,
  restauración en otra base y lectura/escritura con EF 10; 10 comprobaciones de
  identidades, Unicode, hash de contraseña, cifrado, firma y Data Protection anterior.
- Publicación cruzada Linux ARM64 y consulta NuGet sin vulnerabilidades reportadas
  en las dependencias de la aplicación. Esto no es ejecución en ARM64 ni escaneo de
  los paquetes de sistema de la imagen Docker.

No se cambia ni aplica automáticamente una migración de base, no se abren bases
productivas y no se crean cuentas ni claves para sustituir las existentes. El ensayo
de esquema no sustituye una restauración de una copia productiva anonimizada ni la
aceptación en el servidor real.

## Antes del despliegue

Instalar runtime ASP.NET Core 10 en el host o preparar la imagen .NET 10; conservar
base, claves clínicas, Data Protection y auditoría; restaurar una copia aislada,
ejecutar comprobaciones con el nuevo runtime y ensayar reversa. No copiar carpetas
bin/obj ni el SDK al servidor. No ejecutar los scripts históricos de actualización
1.3/1.4 con una publicación nueva sin adaptar y revisar sus comprobaciones de versión.

El resto de los hallazgos de NAS, URL, CSP y capacidad siguen pendientes. El informe
corporativo distingue corrección local de aprobación o despliegue productivo.
