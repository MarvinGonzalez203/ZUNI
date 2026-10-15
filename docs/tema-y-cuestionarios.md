# Apariencia y cuestionarios

Los encabezados públicos y de los cuatro roles comparten los controles cuadrados de 48 px: idioma (globo y ES/EN) y tema (luna/sol). Permanecen alineados a la derecha con el mismo margen en escritorio y móvil. El idioma conserva su cookie y su protección antifalsificación; el tema se guarda únicamente en `localStorage` (`zuni-theme`), sin modificar cuentas ni datos personales. Se aplica desde el encabezado antes de mostrar la página. Si el almacenamiento está deshabilitado, el cambio funciona durante esa página.

El tema oscuro adapta superficies, formularios, tablas, alertas, ventanas, navegación y estados de agenda. Los colores institucionales y su texto de contraste se conservan. Las ventanas y la barra de avance usan superficies opacas para evitar texto superpuesto; los paneles decorativos conservan transparencias suaves. Los iconos son SVG geométricos propios, sin dependencias externas.

## Responder una prueba

Los tres niveles y la práctica ficticia usan una escala horizontal de diez celdas, navegable con teclado y tacto. Se presentan tres preguntas por pantalla, con Anterior/Siguiente. Se deben responder las preguntas visibles para avanzar; volver conserva la selección. El nivel 2 mantiene sus 35 preguntas: su última pantalla tiene dos. El nivel 3 tiene 54 preguntas: 18 pantallas. El nivel 1 mantiene 15: cinco pantallas.

La barra inferior muestra respuestas completadas, total y porcentaje. Finalizar aparece en la última pantalla. Guardar avance conserva las selecciones de todas las pantallas mediante el flujo existente, con consentimiento y revisión de concurrencia. Al reabrir un borrador se muestra la primera pantalla incompleta. Sin JavaScript se conservan todas las preguntas y el envío normal; las validaciones del servidor siguen siendo obligatorias. Los cuestionarios ya finalizados son de solo lectura.

El catálogo del Director ordena las dimensiones por la primera pregunta que contienen, evitando que el orden alfabético altere la numeración visible.

## Ajuste protegido del nivel 3

Se retiró la afirmación original 43 («Puedo cambiar de estrategia cuando una forma de trabajar no da resultado»), por solaparse con «Puedo ajustar un plan cuando las circunstancias cambian». Es una decisión de edición por redundancia, no una evaluación de validez psicométrica. Se conservó la pregunta final original y las claves de todas las demás preguntas/opciones; se renumeraron consecutivamente las posiciones posteriores.

Migración: `20261015064230_AjusteNivelTres54`. Solo modifica el borrador original inactivo, sin asignaciones y con su revisión inicial. Bloquea cambios concurrentes durante la migración. Si otra instalación ya lo editó, activó o asignó, la migración se detiene: se debe conservar ese instrumento y preparar una versión separada. La reversión también se protege frente a uso o edición posterior.

En la base local se verificaron cero asignaciones y se generó el respaldo `zuni-antes-tema-tests54-20261015.dump` antes de aplicar el ajuste. No se activaron pruebas ni se registraron atenciones o inducciones.

## Verificación

La suite de seguridad incluye el orden visible de todas las preguntas, el número de ítems, la conservación de claves y los permisos del flujo. Las pruebas de navegador verifican la navegación, respuestas conservadas, finales de 35/54 ítems, modo oscuro del modal, controles cuadrados y persistencia del tema entre roles. `tema.browser.cjs` utiliza únicamente fixtures sobre un origen aislado.
