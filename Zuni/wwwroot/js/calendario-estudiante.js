(() => {
    let loading = false;
    const idle = () => !document.hidden && !window.ZuniLanguageChanging && !document.querySelector('.modal.show, .modal.showing, .modal.hiding');
    function content(element) {
        const clone = element.cloneNode(true);
        clone.querySelectorAll('input[name="__RequestVerificationToken"]').forEach(input => input.remove());
        clone.querySelectorAll('.modal-scroll-cue').forEach(cue => cue.remove());
        return clone.innerHTML;
    }
    async function refresh() {
        const current = document.querySelector('[data-calendario-estudiante]');
        if (!current || loading || !idle()) return;
        loading = true;
        const url = location.href;
        const scope = document.querySelector('script[data-view-scope]')?.dataset.viewScope;
        const lang = document.documentElement.lang;
        try {
            const response = await fetch(url, { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'text/html' } });
            if (!response.ok || response.url !== url) return;
            const html = new DOMParser().parseFromString(await response.text(), 'text/html');
            const next = html.querySelector('[data-calendario-estudiante]');
            if (!next || !idle() || location.href !== url || document.documentElement.lang !== lang || html.documentElement.lang !== lang ||
                html.querySelector('script[data-view-scope]')?.dataset.viewScope !== scope) return;
            // Leave open reservation forms intact; only replace the read-only calendar while idle.
            if (content(current) === content(next)) return;
            current.querySelectorAll('.modal').forEach(modal => window.bootstrap?.Modal.getInstance(modal)?.dispose());
            current.replaceChildren(...next.childNodes);
            document.dispatchEvent(new CustomEvent('zuni:viewloaded', { detail: { document: html } }));
        } catch {
            const status = current.querySelector('[data-calendario-status]');
            if (status) status.textContent = document.documentElement.lang.startsWith('en')
                ? 'Schedules could not be refreshed. Reload the page to try again.'
                : 'No se pudieron actualizar los horarios. Recarga la página para volver a intentarlo.';
        } finally { loading = false; }
    }
    window.addEventListener('focus', refresh);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
    window.setInterval(refresh, 30000);
})();
