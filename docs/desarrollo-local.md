# Inicio local con Visual Studio

El perfil `https` de `Zuni/Properties/launchSettings.json` utiliza
`https://localhost:7109` y `http://localhost:5001` en Development.

Además de User Secrets, Development permite un archivo privado opcional
`.local-security/desarrollo.json` en la raíz del repositorio. Se carga después de
la configuración habitual y permite compartir las rutas locales entre Visual
Studio y la consola cuando los procesos ven almacenes User Secrets distintos.
La carpeta está excluida de Git y fuera del proyecto web; no se publica.
Esta configuración no se carga en Production.

Las rutas `SeguridadClinica:ClavesPath` y `SeguridadClinica:BitacoraPath` deben ser
absolutas y quedar fuera de la carpeta `Zuni`. Para una base existente se utilizan
sus claves y bitácora originales, conservando sus permisos. No se generan otras
claves para resolver un error de arranque.

Antes de iniciar con F5, detén cualquier instancia de consola que utilice los
mismos puertos. La advertencia genérica de Visual Studio también puede aparecer
cuando el proceso termina antes de abrir el puerto: revisar Salida y la excepción
del depurador para distinguirlo de un conflicto de puertos.
