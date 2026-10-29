# ZUNI 1.1.1 — botones, submenús e historial

Corrección local de Director V2 del 29/10/2026 (Guatemala), posterior a 1.1.0. Se actualizan conjuntamente el ensamblado, la versión visible y las novedades de la campana. No se publica en zuni.lat.

- Los botones de acción de los paneles comparten radio de 9 px, tipografía y altura base. Los botones pequeños conservan una escala menor; las acciones primarias, secundarias y de riesgo mantienen sus funciones y colores. Se respetan los colores personales. Los controles circulares inferiores, selectores cuadrados de la cabecera e indicadores de desplazamiento mantienen sus formas específicas.
- Los submenús recuperan sangría en sus etiquetas, texto de menor peso y conectores visuales. Los iconos conservan el eje usado por el menú compacto para evitar saltos al comprimir. Los conectores se ocultan en la vista compacta.
- La campana presenta las últimas tres versiones registradas: **1.1.1, 1.1.0 y 1.0.0**. Cada sección contiene su número y cambios, con divisiones y desplazamiento interno si la pantalla lo necesita. No se inventan versiones intermedias.
- La lectura sigue siendo individual y corresponde a la versión actual. Marcarla como leída retira el punto pendiente, conserva el historial y no recarga la página. Se mantiene el cierre por Escape y por clic exterior.
- Textos nuevos disponibles en español e inglés. No se traducen datos personales ni anotaciones almacenadas.

Verificación: compilación Release correcta; 59 comprobaciones focalizadas de servidor con SQLite temporal; 71 comprobaciones de navegación, jerarquía, tipografía y geometría de botones; 45 comprobaciones de historial, temas, tamaños móviles, lectura y traducción. Revisión visual en escritorio y móvil.

Sin nuevas migraciones, cambios de esquema ni escrituras de mantenimiento en PostgreSQL. El registro UMG local de la entrega anterior se conserva. La versión de la nube continúa sin cambios.
