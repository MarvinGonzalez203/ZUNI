# Contexto común de Denis Corp

Corte documental: **17 de noviembre de 2026, America/Guatemala**.
Este inventario evita tener que volver a explicar cada proyecto. Las versiones
son las declaradas en las fuentes locales revisadas, no comprobaciones de servidores
públicos. Actualizar el catálogo y este documento al incorporar nuevas entregas.

| Programa | Propósito | Proyecto local | Fuente de estado revisada | Versión |
| --- | --- | --- | --- | --- |
| ZUNI | Acompañamiento estudiantil y atención profesional | `C:\Users\Lenovo\source\repos\ZUNI` | `docs/actualizacion-1.5.1.md`, `VersionSistema.cs` | 1.5.1 |
| Denis Academy | Autoestudio de inglés y español | `C:\traslate` | `estado del sistema.md`, `docs/AUDITORIA-CIA-1.2.1.md` | 1.2.1 |
| Fulleo | Consulta de precios de combustibles con fecha y fuente | `C:\U\10mo\Proyect G II\fulleo-web` | `README.md`, `CHANGELOG.md`, `docs/REPORTE-NAS.md` | 0.8.0 |
| Denis Corp POS | Ventas, inventario, caja y cuentas de negocios | `C:\pos-local` | `package.json`, `public/system-release.json` | 1.22.0 |
| System LSR · colegios | Matrículas, pagos, notas, asistencia y expediente escolar | `C:\Sistem Liceo` | `ESTADO_DEL_SISTEMA.md`, `README.md` | 1.4.2 |

## Rutas y responsabilidades

`/` y `/deniscorp/` son la portada de Denis Corp.

| Ruta | Contenido servido por este checkout |
| --- | --- |
| `/deniscorp/zuni/` | Presentación propia de ZUNI; sus accesos actuales siguen operativos |
| `/deniscorp/academy/` | Identidad y avances documentados de Academy |
| `/deniscorp/fulleo/` | Avances de Fulleo, muestra histórica y pendientes |
| `/deniscorp/pos/` | Identidad y capacidades documentadas del POS |
| `/deniscorp/colegios/` | Identidad y capacidades documentadas de System LSR |

Estas fichas no contienen cuentas, claves ni datos de los otros programas.
Los accesos a aplicaciones independientes requieren su despliegue, configuración
del prefijo y verificación de permisos. No se ha establecido una sesión compartida
ni se ha trasladado ningún proyecto o historial de Git.

## Identidad visual

- Denis Corp, POS y colegios: botones arena `#e3d5ca` de día y rojos `#e8031b`
  de noche. Las tarjetas conservan fondo oscuro y destellos rojo y arena.
- ZUNI: violeta `#cea2fd`; logo sobre dos círculos elípticos blancos de cristal.
- Academy: azul `#2965c7`.
- Fulleo: amarillo `#f2ca48`.
- El tema del portal se guarda en su propio ámbito de almacenamiento, separado
  de la preferencia pública o personal de ZUNI.
- Idioma español/inglés mediante el selector existente y cookie de idioma.
- Una V permite avanzar; al final se invierte y vuelve al principio.

Los archivos de `Zuni/wwwroot/images/productos/` se copiaron de los recursos locales
de los productos. Denis Corp y POS comparten el icono corporativo del POS.
Academy y Fulleo conservan la forma de sus SVG y adaptan su color al azul y amarillo
solicitados. Colegios utiliza el logo LSR propio, no el favicon corporativo heredado
de su proyecto. La presentación de ZUNI mantiene sus favicons originales.
Los cambios visuales de este portal no modifican los archivos de los otros proyectos.

## Alcance real de los avances

**Academy:** autoestudio, audios, repaso, minijuegos y progreso local. La documentación
identifica como pendientes cuentas de servidor, sincronización y un tutor de IA
conectado. No presentar las vistas preparatorias de acceso como autenticación real.

**Fulleo:** versión 0.8.0, consulta y comparación de observaciones, importación con
PDF, revisión, auditoría, idiomas y modalidad NAS. La muestra inicial documenta
19 estaciones y 88 observaciones del 26/10/2026. Es histórica; no anunciar esos
precios como actuales. Siguen pendientes la publicación verificada desde este
portal, el suministro diario automatizado, cobros, historial Plus y predicciones.

**POS:** módulos de ventas, inventario, caja, clientes y proveedores documentados.
La versión 1.22.0 incluye ampliaciones para baterías y garantías. Su ficha en este
portal no afirma que el servidor del POS esté publicado o disponible.

**Colegios:** funciones escolares conectadas en local. Su estado 1.4.2 indica
infraestructura formal de producción pendiente; conservar esa distinción.

## Mantenimiento del contexto

1. Revisar el estado y changelog del proyecto correspondiente.
2. Actualizar `PortalDenisCorp.Productos`: versión, avances y pendientes en ambos idiomas.
3. Actualizar este inventario y las novedades del portal si cambia su comportamiento.
4. Comprobar ficha, favicon, tema, contraste y móvil con las pruebas del portal.
5. Registrar un commit que distinga código local, publicación preparada y despliegue verificado.
