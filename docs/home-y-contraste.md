# Home compacto y legibilidad nocturna

El bloque inicial conserva sus esquinas, gradientes y accesos. Se redujeron márgenes, tamaño máximo del título y espacio vertical para mostrar el rectángulo completo en escritorio, incluyendo pantallas con solo 600 px de altura útil. La altura del encabezado se mide para ajustar el espacio disponible. En móvil y con texto ampliado el contenido conserva su altura natural: no se recorta para forzarlo dentro de la pantalla.

El logo central utiliza las dos órbitas elípticas del diseño del tablero. La flecha inferior es un botón sin borde, con etiqueta ES/EN, acceso por teclado y destino al pie de página. Se oculta después de 24 px de desplazamiento y reaparece al volver arriba. El movimiento y el desplazamiento suave respetan la preferencia de movimiento reducido.

La corrección de contraste define el color efectivo de las tablas, además de sus variables: Bootstrap 5.1 no consume `--bs-table-color` para el texto normal. Se cubren encabezados, celdas, filas alternadas, selección y hover. También se adaptaron las listas del psicólogo, etiquetas de navegación, descripciones de encabezados, pies, pestañas, listas y menús desplegables. Los textos sobre botones institucionales conservan su cálculo de contraste.

Verificación: `home-contraste.browser.cjs` sirve exclusivamente HTML y recursos locales del proyecto y comprueba el rectángulo completo en ES/EN, las dos órbitas, la flecha funcional, movimiento reducido, ausencia de desborde móvil y contraste mínimo 4.5:1 para textos examinados en catálogos, informes, listas y navegación. Incluye inspección visual de las capturas generadas. No requiere cambios en la base de datos.
