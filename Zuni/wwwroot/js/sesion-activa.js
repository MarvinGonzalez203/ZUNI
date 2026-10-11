// Mientras la persona escribe en un formulario (por ejemplo, una nota de atención larga) se genera
// actividad autenticada cada 5 minutos para que la sesión de 30 minutos de inactividad se renueve.
// No envía datos del formulario y no extiende el máximo absoluto de la sesión.
(() => {
    'use strict';
    const url = document.currentScript?.dataset.url;
    if (!url) return;
    const intervalo = 5 * 60 * 1000;
    let actividad = false;
    document.addEventListener('input', event => {
        if (event.target instanceof Element && event.target.closest('form[method="post" i]')) actividad = true;
    }, true);
    setInterval(() => {
        if (!actividad) return;
        actividad = false;
        fetch(url, { credentials: 'same-origin', cache: 'no-store', redirect: 'manual' }).catch(() => { });
    }, intervalo);
})();
