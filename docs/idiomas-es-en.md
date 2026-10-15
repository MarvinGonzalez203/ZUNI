# Español e inglés

El selector ES / EN aparece arriba del home, acceso, recuperación de contraseña y paneles. También funciona con cambio obligatorio de contraseña. Es un formulario nativo con protección antiforgery; no requiere JavaScript para cambiar el idioma.

La preferencia se guarda durante un año en una cookie HttpOnly, SameSite=Lax, con Secure en HTTPS. La cookie utiliza la identidad configurada de la instalación. El español es el idioma inicial. Solo se admiten `es` y `en`; la redirección acepta destinos locales y conserva ruta y filtros de consultas GET. En una respuesta de formulario POST, se vuelve a la página de origen del mismo sitio o al panel.

## Qué se traduce

Los textos de presentación usan claves explícitas `Idioma.Texto(...)` y el catálogo integrado `Zuni/Localizacion/en.json`. Se localizan navegación, tarjetas, formularios, estados, mensajes de operación y validación, textos de JavaScript, calendarios y rótulos de PDF. MVC utiliza el mismo catálogo para etiquetas y Data Annotations. Las fechas del calendario usan la cultura seleccionada; la hora de atención continúa siendo la de Guatemala.

No hay traducción automática del DOM ni de respuestas HTML/JSON, ni se utilizan servicios externos. Los valores enviados por formularios, rutas y roles internos mantienen sus identificadores originales.

Los nombres, correos, teléfonos, carné, carreras del catálogo institucional, notas, respuestas, textos de pruebas, políticas e informes redactados y demás contenido de usuarios o de la institución conservan su contenido y su idioma original. En PDF se traducen los rótulos fijos; se conserva el contenido del documento. El título de una prueba guardada se trata como un dato, incluso si coincide con una clave de interfaz.

Los estados y roles de la aplicación se traducen para su presentación, sin modificar sus valores persistidos. Cambiar de idioma no guarda formularios ni sustituye los controles de edad, consentimiento, asignación, autenticación o autorización.

## Mantenimiento

- Agregar textos nuevos con `Idioma.Texto("Texto español")` y su equivalente inglés al JSON. Los valores personales se pasan como parámetros de una clave de formato, nunca como claves de traducción.
- Para mensajes de formulario, usar la misma clave en la validación. Los mensajes de operación en TempData se traducen al renderizar y se mantienen en español al almacenarlos.
- Agregar mensajes JavaScript a la lista de `_IdiomaScripts` y consumirlos con `window.ZuniI18n.t(...)`; preservar nombres de campos, valores, selectores y claves de almacenamiento.
- Ejecutar `tools/verificar-idiomas.py` para detectar claves faltantes o parámetros incompatibles.

No hay cambios de esquema ni migraciones para esta función.

## Verificación

Las pruebas offline usan SQLite ficticio y HTTP local; no abren PostgreSQL. Verifican el cambio en ambos sentidos, cookies, sesión activa y cambio obligatorio de contraseña, redirecciones locales, protección antiforgery, etiquetas y datos que coinciden con claves del catálogo. El navegador comprueba el selector y los formularios en escritorio y móvil. Las pruebas existentes de agenda conservan sus comprobaciones de permisos y disponibilidad.

Validación: 617 comprobaciones de seguridad y aplicación (32 específicas de idiomas), 316 de agenda y 166 de navegador. El catálogo contiene 1,930 entradas; el auditor verifica sus parámetros y las claves explícitas utilizadas. Release compila sin errores.

La verificación de vulnerabilidades NuGet puede avisar que no hay conexión con su origen remoto; ese aviso no impide ejecutar las pruebas con las dependencias disponibles.
