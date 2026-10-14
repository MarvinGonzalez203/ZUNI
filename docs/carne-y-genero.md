# Carné y género

Formato de carné: **4 dígitos - 2 dígitos - de 2 a 6 dígitos** en el último bloque. Admite todas las longitudes intermedias: 2, 3, 4, 5 y 6. Ejemplos: `1590-18-25`, `1590-18-125`, `1590-18-1225`, `1590-18-12225` y `7490-26-125678`.

El rango ya estaba implementado en `CarneHelper`; se aclaró el mensaje y la ayuda de las pantallas. Registro, alta administrativa y configuración inicial de Mi cuenta usan la misma construcción/validación. Se conserva como texto, sin quitar ceros iniciales; las búsquedas administrativas admiten guiones y la presentación los repone. La columna actual admite estos tamaños; no se necesita alterar carnés históricos.

Se comprobaron construcción y formato, rechazo de bloques de uno o siete dígitos, letras y números no ASCII; registro real por HTTP para las cinco longitudes; y validación en navegador. La unicidad continúa aplicándose al valor completo sin guiones.

La solicitud presenta **Mujer, Hombre, No binario, Otros y Prefiero no responder**. El servidor valida las opciones. El antiguo valor `Otro` sigue siendo compatible con registros anteriores, aunque la nueva opción visible se llama `Otros`.
