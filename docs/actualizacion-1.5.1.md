# ZUNI / portal Denis Corp 1.5.1

Se completa la identidad de Denis Corp: selector español/inglés, día/noche persistente
en ámbito propio, tarjetas oscuras con destellos rojo y arena y cinco programas.
Los botones corporativos, POS y colegios son arena de día y rojos de noche.
ZUNI conserva violeta; Academy azul; Fulleo amarillo. El hover conserva el contraste.

El catálogo `PortalDenisCorp.cs` centraliza nombres, rutas, favicons, descripciones,
versiones documentadas, avances y pendientes en ambos idiomas. Se agregan las fichas
`/deniscorp/pos/` y `/deniscorp/colegios/`. Fulleo presenta los avances de su versión
0.8.0 y distingue observaciones históricas de precios actuales. La publicación
de sus aplicaciones independientes sigue pendiente de integrar y verificar.

El logo ZUNI incorpora dos círculos blancos de cristal detrás. Denis Corp y POS
usan el favicon corporativo; Academy, Fulleo, colegios y ZUNI tienen el propio.
Se reutilizan los recursos existentes de los proyectos y se adaptan los SVG de
Academy y Fulleo a la paleta solicitada. Los proyectos externos no se modificaron.

La V central permite bajar y se orienta hacia arriba al llegar al final para volver
al principio. Respeta movimiento reducido y actualiza su etiqueta con el idioma.
El cambio de idioma conserva tema, ruta y posición sin recargar el documento.

La versión de proyecto y campana pasa a 1.5.1. No hay cambios de esquema de base.
Se conserva el acceso y la identidad propios de ZUNI. Esta entrega es local y no
afirma estar desplegada en zuni.lat.

El [contexto común](denis-corp/contexto-productos.md) reúne los cinco proyectos,
fuentes, ubicaciones y límites. La [guía de GitHub privado](denis-corp/github-privado.md)
explica cómo crear main y ramas, con la distinción entre privacidad de repositorio
y organización del código. No se creó ni se publicó ningún repositorio remoto.

Verificación: compilación Release sin errores ni advertencias; 80 comprobaciones
HTTP del portal; 192 comprobaciones de presentación en 360, 390, 768 y 1366 px,
incluidos contraste al pasar el cursor, persistencia de tema, idioma sin recarga,
favicons y V de desplazamiento; suite completa de seguridad con sus módulos y
176 comprobaciones del núcleo; 56 controles de 2FA, ausencias y notificaciones;
45 comprobaciones de campana, lectura de versiones y traducción. Las pruebas usan
datos ficticios; también pasaron 86 comprobaciones de presentación de 2FA, ausencias,
avisos automáticos y portal. Los proyectos externos y producción no se modificaron.
