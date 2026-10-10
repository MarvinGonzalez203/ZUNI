(() => {
    'use strict';
    const form = document.getElementById('bigfive-form');
    if (!form) return;
    const pages = Array.from(form.querySelectorAll('.bf-page'));
    const selects = Array.from(form.querySelectorAll('select'));
    const first = selects.findIndex(s => !s.value);
    let page = first < 0 ? 0 : Math.floor(first / 10);
    let dirty = false, sending = false;
    const previous = document.getElementById('bf-anterior');
    const next = document.getElementById('bf-siguiente');
    const status = document.getElementById('bf-save-status');
    const finalize = document.getElementById('bf-finalizar');
    function show() {
        pages.forEach((p, i) => p.hidden = i !== page);
        previous.disabled = page === 0;
        next.disabled = page === pages.length - 1;
        document.getElementById('bf-pagina').textContent = `Bloque ${page + 1} de ${pages.length} · 10 afirmaciones`;
    }
    previous.hidden = next.hidden = false;
    previous.addEventListener('click', () => { page = Math.max(0, page - 1); show(); pages[page].scrollIntoView({ block: 'start', behavior: 'auto' }); });
    next.addEventListener('click', () => { page = Math.min(pages.length - 1, page + 1); show(); pages[page].scrollIntoView({ block: 'start', behavior: 'auto' }); });
    form.addEventListener('change', () => {
        dirty = true;
        const count = selects.filter(s => s.value).length;
        document.getElementById('bf-progress').value = count;
        document.getElementById('bf-avance').textContent = `${count} de 50 respondidas`;
        status.textContent = 'Tienes respuestas sin guardar.';
        if (finalize) finalize.querySelector('button').disabled = true;
    });
    form.addEventListener('submit', e => {
        if (sending) { e.preventDefault(); return; }
        sending = true;
        status.textContent = 'Guardando…';
    });
    window.addEventListener('beforeunload', e => {
        if (dirty && !sending) { e.preventDefault(); e.returnValue = ''; }
    });
    if (finalize) finalize.addEventListener('submit', e => {
        if (dirty || sending) { e.preventDefault(); status.textContent = 'Guarda los cambios antes de finalizar.'; return; }
        sending = true;
        finalize.querySelector('button').disabled = true;
    });
    window.addEventListener('pageshow', () => {
        sending = false;
        if (finalize) finalize.querySelector('button').disabled = dirty;
    });
    show();
})();
