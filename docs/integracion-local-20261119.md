# Denis Corp — entorno local integrado

19/11/2026. Inicio principal: http://localhost:5001/. El propietario eliminó el Linode y pidió continuar la revisión en local. No se requiere servidor de nube ni se instaló un servicio de inicio automático de Windows. POS se conserva fuera de los cambios.

| Programa | Inicio local | Versión |
|---|---|---|
| Denis Corp / ZUNI | http://localhost:5001/ y /deniscorp/zuni/ |1.6.3|
| Academy | http://localhost:5186/#/ |1.4.1|
| Fulleo | http://localhost:4180/deniscorp/fulleo/ |0.11.1|
| Colegios | http://localhost:5173/DenisCorp/sistemas |1.8.1|
| Logística | http://localhost:5105/ |0.4.2|

Las tarjetas y el menú de programas de la portada abren el inicio de cada hijo. Los enlaces propios de Inicio se conservan; by Denis Corp, al pie de cada aplicación, regresa a la portada corporativa. El pie público y el de usuarios de Colegios comparten enlace; ZUNI lo incluye también en su plantilla de paneles. Las fichas y artículos informativos siguen separados de los accesos a la aplicación. POS mantiene su ficha previa y no se inicia con este entorno.

## Abrir y volver a iniciar

Ejecutar C:/Denis Corp/Iniciar Denis Corp.cmd. El iniciador estable está en C:/Denis Corp/deniscorporation/.local/entorno-local/Start-EntornoLocal.ps1 y utiliza proyectos.local.json, ambos disponibles aunque GitHub Desktop esté en la rama POS. Levanta el PostgreSQL local existente si falta y los cinco procesos web necesarios. No crea/restaura bases, cuentas, claves clínicas ni contraseñas. ZUNI conserva su configuración privada de desarrollo y el resto conserva PostgreSQL55432; cada aplicación tiene su puerto/origen local.

Para recompilar los cambios de código y reiniciar:

```powershell
& 'C:/Denis Corp/deniscorporation/.local/entorno-local/Start-EntornoLocal.ps1' -Action Restart -Build
```

Para consultar estado o detener las aplicaciones, usar -Action Status o -Action Stop. Stop conserva PostgreSQL y POS. Los puertos ocupados por un proceso de otra carpeta se rechazan antes de reemplazarlo. Los procesos se abren ocultos; los errores se guardan en .local/entorno-local/runtime. No ejecutar F5 sobre los mismos puertos mientras ya están activos; cerrar el entorno antes de depurar ese proyecto desde Visual Studio.

Las pestañas abiertas previamente deben recargarse para cargar los archivos nuevos. Academy conserva su actualización PWA y sus datos por cuenta; no se borraron cachés, IndexedDB ni progreso para refrescar el diseño.

## Configuración de direcciones

ZUNI/portal: DenisCorp:HomeUrl y DenisCorp:ProductHomes en appsettings.Development.json. Academy/Colegios: VITE_DENIS_CORP_HOME opcional al compilar; en localhost usan por defecto http://localhost:5001/. Fulleo: DENIS_CORP_HOME opcional del servidor; Logística: DenisCorp:HomeUrl. Las direcciones no contienen credenciales; HTTPS permite otro origen futuro, HTTP queda limitado a loopback. Una publicación externa requiere sus propios DNS/TLS/orígenes y no se activa con estos valores locales.

No se proxyaron todas las aplicaciones en un mismo origen ni se mezclaron sus bases. Son puertos locales independientes conectados por navegación. El acceso desde un teléfono físico de la red no está habilitado por este iniciador; la revisión móvil se hizo emulando los tamaños en Edge.

## Validación

Seis inicios revisados en 320,390,768 y1440px: español/día e inglés/noche, controles de idioma y tema, cinco tarjetas seguidas hasta los hijos y cinco enlaces de regreso a la portada. Sin desbordamiento horizontal ni excepciones JavaScript observadas tras corregir el encabezado de Colegios. El enlace corporativo tiene texto de14px y área mínima de44px. Fulleo conserva foco visible de teclado pero ya no enfoca todo el comparador al abrir. Logística hace pública su portada informativa, manteniendo Personas protegido;139 controles HTTP de permisos/alcance/CSRF/2FA pasaron en pruebas aisladas sin abrir SQL Server.

No se probaron teléfonos físicos ni se declara revisión exhaustiva de todos los paneles autenticados. Se conservaron datos, configuraciones y cambios previos del usuario. Las fuentes de Academy/Fulleo y el cliente escolar tenían trabajo sin commit anterior a esta tarea; sus diferencias de esta entrega se documentan por separado en el repositorio corporativo privado.
