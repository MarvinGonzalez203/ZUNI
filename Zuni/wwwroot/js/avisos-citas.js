// Solo cuenta avisos propios: sin cargar nombres, notas ni el listado de citas.
(() => {
    'use strict';
    const control = document.querySelector('[data-avisos-citas]');
    if (!control?.dataset.url) return;
    let consultando = false, activo = true;
    async function actualizar() {
        if (!activo || consultando || document.hidden) return;
        consultando = true;
        try {
            const r = await fetch(control.dataset.url, {credentials:'same-origin', cache:'no-store', redirect:'manual'});
            if (r.type === 'opaqueredirect' || r.status === 401 || r.status === 403) { activo = false; return; }
            if (!r.ok || !r.headers.get('Content-Type')?.includes('application/json')) return;
            const {cantidad} = await r.json();
            if (!Number.isSafeInteger(cantidad) || cantidad < 0) return;
            const anterior = Number(control.dataset.cantidad);
            control.dataset.cantidad = String(cantidad);
            const etiqueta = control.querySelector('.notification-label')?.textContent || 'Notificaciones';
            const contador = control.querySelector('[data-avisos-cantidad]');
            contador.textContent = `(${cantidad})`; contador.hidden = cantidad === 0;
            control.setAttribute('aria-label', `${etiqueta} (${cantidad})`);
            if (cantidad > anterior) document.querySelector('[data-avisos-estado]').textContent = `${etiqueta}: ${cantidad}`;
        } catch { /* Se conserva el acceso y su último conteo ante una interrupción de red. */ }
        finally { consultando = false; }
    }
    const timer = setInterval(actualizar, 45000);
    document.addEventListener('visibilitychange', () => { if(!document.hidden) actualizar(); });
    window.addEventListener('pagehide', () => clearInterval(timer), {once:true});
})();
