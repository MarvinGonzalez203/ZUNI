# ZUNI 1.3.3 - contraste, calendario móvil y documentos

Los botones con un color personalizado conservan el texto de contraste calculado
al pasar el cursor, enfocar y pulsar, también en modo noche. Las acciones de
contorno solo adoptan ese texto cuando su fondo toma el color personalizado.

El calendario conserva una altura uniforme para todos los días. Debajo del número
reserva dos líneas para estado y cantidad; un texto largo termina en puntos
suspensivos. El contenido completo permanece en el nombre accesible, el título y
el detalle del día. Las acciones de reprogramación y atención imprevista comparten
una fila flexible con igual altura, incluyendo varias citas. Los controles del mes
se alinean en tres columnas en móvil. Los desplegables usan chevrones abiertos en V;
los detalles giran al abrirse y los selectores también comparten ese dibujo.

`DocumentoZuniPdf` comparte medidas, encabezado, pie y logo con círculos blancos
entre el reporte de atención y la ficha comercial. Las tarjetas arena tienen
esquinas redondeadas y el mismo ancho que las líneas violetas. El cuerpo pagina
con MigraDoc, reservando espacio para el aviso "Sobre este documento" y el pie,
que se dibujan en una posición fija en cada página. La ficha comercial conserva
su aclaración de registro comercial sin datos de estudiantes. Las filas de datos
alinean verticalmente las etiquetas y los valores, incluidos valores de dos líneas.

La versión del proyecto y la campana pasan a 1.3.3. No hay migración de base de
datos ni cambios de permisos o de contenido clínico. La publicación en zuni.lat
requiere desplegar esta versión; estos cambios se verifican localmente.

## Validación

- Suite de seguridad offline: datos ficticios, SQLite en memoria, sin PostgreSQL.
- `calendario-estilo.browser.cjs`: cuatro anchos, ambos temas, alturas, recorte,
  desbordamiento, alineación de acciones, hover, foco y giro de los chevrones.
- `DocumentosDemo --pdf-demo`: reporte y contrato cortos y extensos, más el informe
  educativo existente. Renderizados con Poppler e inspeccionados visualmente.
- Verificación de posiciones de aviso, pie y contenido en todas las páginas de los
  PDF ficticios extensos; comprobación de las tres versiones en la campana.

Para reproducir el navegador, primero generar las vistas con
`ZUNI_FORMS_PREVIEW_DIR` durante la suite offline; pasar esa carpeta como argumento
a `node tests/Seguridad.Offline/calendario-estilo.browser.cjs`. Requiere Playwright y Edge.
