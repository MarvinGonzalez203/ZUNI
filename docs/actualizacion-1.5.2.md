# Portal Denis Corp / ZUNI 1.5.2

La oferta escolar pasa a **Denis Corp Colegios**, una propuesta SaaS general para
varias instituciones. LSR es un cliente de Denis Corp, no el nombre ni la marca
del producto. Se retira su imagen del catálogo y se incorpora un símbolo escolar
general. La ficha y el favicon dejan de usar la identidad del cliente. La versión
del sistema de LSR tampoco se presenta como versión del SaaS. Las capacidades
previstas y la disponibilidad comercial pendiente se distinguen expresamente.

Los cinco logos de las tarjetas, el de la cabecera y el de la presentación principal
comparten los dos círculos elípticos blancos de cristal. Se conservan las paletas
de cada programa y el tema del portal independiente de ZUNI.

La cabecera reemplaza el acceso destacado a ZUNI por **Blog** y **Contáctanos**.
Las rutas propias son `/deniscorp/blog/` y `/deniscorp/contacto/`, sin dependencia
de que otro servidor esté ejecutándose en el puerto 3000.

## Blog y centro de ayuda

Se toma como referencia visual y funcional la página local
`http://localhost:3000/DenisCorp/sistemas/blog`, consultada durante esta entrega.
Se adapta a los cinco programas de la empresa: guía destacada, temas con conteos,
búsqueda, tarjetas de artículos y pasos desplegables con accesos relacionados.

El contenido está centralizado en `BlogDenisCorp.cs`, con ocho guías bilingües.
La búsqueda omite diferencias de acentos, limita la longitud y admite caracteres
Unicode en el recorte sin romper la respuesta. GET renderiza los filtros sin
JavaScript; el navegador puede filtrar sin recargar, conservar la consulta en la URL
y mantener búsqueda, tema y artículos abiertos al cambiar de idioma. La biblioteca
no publica expedientes ni instrucciones para eludir permisos.

## Contacto

Se toma como referencia `http://localhost:3000/DenisCorp/sistemas/contacto`.
Incluye accesos rápidos, formulario de consulta, canales directos y preguntas
frecuentes, con diseño adaptable y modo día/noche.

Se reutilizan los canales empresariales públicos de la referencia: WhatsApp
`+502 3039 0175` y `corporation.denis@gmail.com`. Nombre, organización, programa,
tema y mensaje preparan un borrador para WhatsApp o el cliente de correo. La persona
revisa y envía desde esa aplicación; no se afirma haber enviado ni recibido un
mensaje desde ZUNI. El formulario no guarda mensajes en la base, no llama a Brevo
y no serializa el texto mediante una petición GET. El cambio de idioma conserva
el borrador y actualiza sus etiquetas. Las pruebas utilizan datos ficticios y no
abren ni envían mensajes a los canales externos.

## Entrega

Versión de proyecto y campana actualizada a 1.5.2. Sin cambios de esquema, cuentas,
roles, configuración del cliente LSR ni modificaciones de los otros proyectos.
El contexto común se corrige para distinguir producto SaaS y cliente. Las páginas
públicas no muestran nombres, marcas ni rutas locales del cliente.

Esta entrega es local. No crea un repositorio remoto ni afirma estar publicada
en zuni.lat. La publicación sigue requiriendo su proceso de despliegue y verificación.

Verificación realizada: compilación Release sin errores ni advertencias; 112
comprobaciones HTTP del portal (incluidos búsqueda, filtros, Unicode, identidad,
contacto y ausencia de envío al servidor); 272 comprobaciones de navegador en
360, 390, 768 y 1366 px (temas, contraste, idiomas, filtros, cristal, formularios
y borradores ficticios); suite completa de seguridad con sus módulos y 176
comprobaciones del núcleo; 45 comprobaciones de la campana de versiones.
