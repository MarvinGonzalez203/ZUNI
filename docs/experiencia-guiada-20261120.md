# Experiencia guiada y asistentes Denis Corp — 2026-11-20

Trabajo iniciado el 19 y verificado el 20 de noviembre de 2026. Entrega local; no se publica en nube ni se modifica código de POS. Sus capacidades sí se documentan en el blog corporativo.

| Producto | Versión anterior | Versión nueva |
|---|---|---|
| ZUNI | 1.7.0 | 1.8.0 |
| Academy | 1.5.0 | 1.6.0 |
| Fulleo | 0.12.0 | 0.13.0 |
| Colegios | 1.9.0 | 1.10.0 |
| Logística | 0.5.0 | 0.6.0 |

## Cambios visibles

Botones de acción de 52 px y selectores de archivo de 48 px, bordes y paleta coherentes con el diseño minimalista. ZUNI conserva texto violeta oscuro al pasar el cursor; se elimina la herencia azul de Bootstrap. Cada producto vuelve a su propio inicio al cerrar sesión; el enlace by Denis Corp mantiene el regreso a la portada corporativa.

Hay siete avisos de primeros pasos por producto, con fondo atenuado, foco accesible, Atrás, Siguiente, cierre y finalización. ZUNI: Mi cuenta → Ajustes generales → reproducir; Academy y Fulleo: Ajustes generales → primeros pasos; Colegios: Ajustes generales → Tutorial inicial; Logística: Ajustes generales → reproducir. Se recuerda la finalización por perfil en este navegador. Los usuarios públicos de Academy y Fulleo también pueden conocer sus funciones sin crear una cuenta.

El blog raíz tiene veinte artículos y filtros por solución, con cuatro artículos de POS. Explica funciones, alcance y pasos útiles; la búsqueda tolera errores comunes. Logística se describe según sus funciones existentes, sin anunciar rutas o flotas como terminadas. Colegios conserva su identidad SaaS y no se presenta como un único cliente.

## Asistente

Se abre desde el acceso de conversación, integrado en el menú cuando está disponible o flotante en móvil. Cierra con X, Escape o clic afuera. Responde sobre las funciones de cada producto, propone temas relacionados y ofrece accesos, pasos y esquemas cuando el catálogo dispone de ellos. Las guías actuales de ZUNI conservan nombre y organización.

El motor local normaliza tildes, mayúsculas, signos, abreviaturas y confusiones fonéticas, con distancia de edición y transposiciones. Reconoce períodos en español e inglés: días, semanas, quincenas, meses, rangos entre meses, trimestres, años, últimos intervalos e historial completo. Las fechas relativas se anclan al día de la pregunta en Guatemala. No depende de un proveedor externo de IA.

Las cifras tienen un alcance explícito: ZUNI cuenta citas del estudiante o psicólogo autenticado, incluyendo canceladas; Academy cuenta lecciones del curso elegido según progreso cargado, una finalización por lección; Fulleo compara observaciones cargadas con fecha, fuente y filtros y distingue demostraciones; Logística cuenta Personas según propietario y permisos, sin simular un filtro temporal inexistente. Colegios dirige al módulo autorizado para revisar cifras; no calcula saldos sin el contexto requerido. Los endpoints no aceptan ampliar el alcance enviando otro identificador de usuario.

La conversación guarda las últimas treinta preguntas en sessionStorage, separadas por producto, cuenta y centro cuando corresponde. Permanece al cerrar el panel y recargar la pestaña. Se elimina al cerrar sesión, con reinicio o Borrar conversación; una cuenta diferente no hereda las preguntas anteriores. Las respuestas a métricas pueden conservar cifras autorizadas durante esa sesión: cerrar sesión al terminar sigue siendo necesario. Las entradas se escriben como texto y los accesos rechazan esquemas inseguros. No se amplían permisos ni se muestran notas clínicas privadas.

## Verificación y límites

Compilación correcta de ZUNI, Academy, Colegios y Logística; sintaxis de Fulleo comprobada. Motor: 73 pruebas de lenguaje/calendario, 11 de intención de conteo y 4 de curso y fecha de Guatemala. Regresión completa de ZUNI aprobada, con contador principal de 195 comprobaciones y suites adicionales; Logística: 161 comprobaciones aisladas. Vistas de prueba con cuentas ficticias y SQLite, sin abrir ni migrar bases de producción.

Revisión en Edge con emulación 320, 390, 768 y 1440 px: siete pasos, repetición, preguntas, diagramas, cierre, recarga, borrado y controles. Evidencia adicional para inglés y tema oscuro, búsqueda del blog, hover, aislamiento por cuenta, texto malicioso y enlaces inseguros. Los JSON de resultados indican el alcance exacto de cada prueba; no equivalen a pruebas en teléfonos físicos ni a auditar cada módulo de los productos.

Se conserva un aviso NU1900 de NuGet por falta de acceso a su servicio de vulnerabilidades y los avisos previos de nulabilidad en Persona de Logística. No se renovó la auditoría de dependencias. No se alteraron datos institucionales, credenciales o configuración de correo. Los cambios de privacidad previos de ZUNI quedan fuera del commit de esta entrega.

## Entorno local y registro

Portada: http://localhost:5001/ . Iniciador: C:/Denis Corp/Iniciar Denis Corp.cmd. Se corrigió la espera de pg_ctl para aguardar únicamente al proceso iniciador, sin esperar al servidor PostgreSQL; también se reutilizan servicios saludables sin reemplazar procesos existentes. PostgreSQL y POS no se detienen.

Los commits de ZUNI y Logística registran únicamente este trabajo. Los cinco parches del repositorio corporativo representan diferencias respecto al estado inicial de cada carpeta, no una copia de bases o secretos. Academy y Fulleo tenían trabajo previo: se conserva separado. Colegios se registra mediante su parche corporativo. El manifiesto incluye versiones y hashes verificables.
