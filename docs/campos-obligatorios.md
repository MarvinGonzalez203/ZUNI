# Campos obligatorios (R02)

Los formularios muestran un asterisco rojo junto al dato obligatorio y la leyenda «* Campo obligatorio». Los datos opcionales conservan su comportamiento. La marca también incluye texto para lectores de pantalla.

## Reglas según la selección

- Institución: los dominios son obligatorios al restringir el registro a dominios autorizados.
- Alta administrativa: el carné es obligatorio para Estudiante.
- Solicitud de atención: las confirmaciones de consentimiento y mayoría de edad son obligatorias. El ingreso por referencia exige quién refiere y el motivo. Se requiere al menos un contacto; cualquier contacto iniciado exige nombre, relación y teléfono.
- Atención: al seleccionar asistencia, los resultados privados y compartidos son obligatorios. Si se recomienda otra cita, también sus indicaciones. Una observación por inasistencia continúa siendo opcional.
- Pruebas del Director: los campos de dimensiones, preguntas y opciones recién agregadas también tienen indicadores.
- Big Five: el consentimiento y la confirmación de finalización se marcan; guardar un avance permite respuestas incompletas.

## Implementación y verificación

`EtiquetaObligatoriaTagHelper` reconoce atributos `Required` explícitos y controles obligatorios contenidos en una etiqueta. `zuni-required` declara reglas que dependen del formulario; `data-required-conditional` permite sincronizarlas con el estado `required` del control. No se deduce obligatoriedad de la nulabilidad del modelo. Los filtros y los formularios sin datos obligatorios no reciben leyenda.

El script compartido actualiza las marcas y `aria-required` cuando cambian los requisitos o se agregan controles. La validación del servidor continúa vigente aunque JavaScript no esté disponible.

Verificación: compilación Release; suites Seguridad.Offline y Agenda.Offline; formularios renderizados y cambios de selección en Edge sin ventana. Las pruebas usan SQLite en memoria y cuentas ficticias. No requieren migración ni acceso a PostgreSQL.

Para repetir la verificación de navegador, define `ZUNI_FORMS_PREVIEW_DIR` al ejecutar Seguridad.Offline y pasa esa carpeta a `tests/Seguridad.Offline/campos-obligatorios.browser.cjs`. El script necesita Playwright y Edge instalado; no instala paquetes ni abre el perfil personal del navegador.
