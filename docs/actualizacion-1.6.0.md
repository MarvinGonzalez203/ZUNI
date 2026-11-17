# ZUNI 1.6.0 — rutas y seguridad del navegador

17/11/2026. Entrega local sin despliegue ni migración de datos. La campana conserva 1.5.3 y 1.5.2.

- Hosting:PathBase admite vacío o `/deniscorp/zuni`, sin barra final. El gateway debe conservarlo y usar un host dedicado. Hosting:ApplicationOnly=true muestra ZUNI al abrir la raíz de su prefijo. Las fichas Academy/Fulleo/Colegios pasan a `/deniscorp/programas/{id}/`; se añade Logística. POS conserva su ficha y no se audita su aplicación.
- Cookie de sesión y antiforgery host-only, Path coherente con la base. Cambiar origen requiere nuevo login; no copiar cookies, claves ni almacenamiento entre programas.
- Correo:UrlPublica debe contener el origen HTTPS y la base con barra final, por ejemplo https://atencion.zuni.lat/deniscorp/zuni/. Su ruta tiene que coincidir con Hosting:PathBase; Brevo sigue requiriendo remitente verificado y clave privada. No se envió correo en estas pruebas.
- Zuni:ProxyInverso=true y Hosting:KnownProxies con las IP exactas del gateway. Se conserva el límite de un salto y las listas seguras de ASP.NET, nunca confiar en cualquier X-Forwarded-For.
- Nonce aleatorio por respuesta en scripts/estilos Razor de confianza. Los datos de usuarios nunca reciben nonce. La nueva política script-src/style-src empieza en Content-Security-Policy-Report-Only; la política básica sí se aplica. Seguridad:CspEnforced=true activa la ampliada tras revisión de todos los roles en staging. Faltan esa revisión y la comprobación a través de HTTPS real para cerrar SEG-07. style-src-attr permite estilos inline existentes, no scripts inline arbitrarios ni eval. Las violaciones se consultan en el navegador; no se han enviado reportes con datos clínicos a terceros.
- El filtro numérico de registro pasa a un archivo externo. Los enlaces procesados por Razor conservan IHtmlContent antes de aplicar el prefijo.

Pruebas: `./tools/dotnet10.ps1 run --project tests/Seguridad.Offline -c Release -- --infra-07-13`, --portal-denis, --mejoras-14, --mejoras-15 y suite general. SQLite y HTTP efímeros; no usa PostgreSQL real. El informe corporativo distingue código corregido de publicación pendiente. Conservar la configuración/artefacto anterior y respaldos verificados para rollback; esta entrega no transforma datos.
