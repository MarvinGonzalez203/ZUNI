(() => {
    const recovery = document.querySelector('[data-pin-recovery]');
    if (recovery && location.hash) {
        const params = new URLSearchParams(location.hash.slice(1));
        recovery.querySelector('[name=Usuario]').value = params.get('usuario') || '';
        recovery.querySelector('[name=Token]').value = params.get('token') || '';
        history.replaceState(null, '', location.pathname);
    }
    if (window.zuniPinReady) return; window.zuniPinReady = true;
    const locks = new Map(), pendingForms = new WeakSet(), pendingControllers = new Set();
    function lock(card) {
        const state = locks.get(card); if (!state) return;
        clearTimeout(state.timer); card.querySelector('[data-private-note-body]').innerHTML = state.html; locks.delete(card);
    }
    function lockAll() { for (const controller of pendingControllers) controller.abort(); for (const card of locks.keys()) lock(card); }
    document.addEventListener('visibilitychange', () => { if (document.hidden) lockAll(); });
    window.addEventListener('pagehide', lockAll);
    document.addEventListener('click', event => { const button = event.target.closest('[data-pin-lock]'); if (button) lock(button.closest('[data-private-note]')); });
    document.addEventListener('submit', async event => {
        const form = event.target.closest('[data-pin-unlock]'); if (!form) return; event.preventDefault();
        if (pendingForms.has(form)) return;
        pendingForms.add(form);
        const controller = new AbortController(); pendingControllers.add(controller);
        const card = form.closest('[data-private-note]'), body = card.querySelector('[data-private-note-body]'), button = form.querySelector('button'), error = form.querySelector('[data-pin-error]');
        const data = new FormData(form); form.querySelector('[name=pin]').value = ''; button.disabled = true; error.textContent = '';
        try {
            const response = await fetch(form.action, { method: 'POST', body: data, credentials: 'same-origin', cache: 'no-store', signal: controller.signal });
            if (!response.ok || response.redirected || response.headers.get('X-Zuni-Private-Note') !== '1') { const problem = await response.json().catch(() => null); throw new Error(problem?.error || (document.documentElement.lang === 'en' ? 'Unable to unlock. Try again or sign in again.' : 'No se pudo desbloquear. Inténtalo nuevamente o vuelve a iniciar sesión.')); }
            const html = await response.text();
            if (controller.signal.aborted || document.hidden || !form.isConnected) return;
            const original = body.innerHTML; body.innerHTML = html;
            locks.set(card, { html: original, timer: setTimeout(() => lock(card), 120000) });
        } catch (e) { error.textContent = e.name === 'AbortError' ? '' : e.message; }
        finally { data.delete('pin'); button.disabled = false; pendingForms.delete(form); pendingControllers.delete(controller); }
    });
})();
