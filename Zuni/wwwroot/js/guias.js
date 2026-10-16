(() => {
    let controller;
    const en = () => document.documentElement.lang.startsWith('en');
    async function navigate(url, restoreY, historyNavigation = false) {
        controller?.abort(); controller = new AbortController();
        const signal = controller.signal;
        const page = document.querySelector('.guide-page');
        if (!page) return;
        page.setAttribute('aria-busy','true');
        try {
            const response = await fetch(url, {signal,credentials:'same-origin'});
            if (!response.ok) throw new Error('Guide unavailable');
            const html = new DOMParser().parseFromString(await response.text(), 'text/html');
            const replacement = html.querySelector('.guide-page');
            if (!replacement || signal.aborted) return;
            const y = restoreY ?? window.scrollY;
            if (!historyNavigation) {
                history.replaceState({...history.state,zuniGuide:true,y:window.scrollY},'',location.href);
                history.pushState({zuniGuide:true,y},'',url);
            }
            page.replaceWith(replacement); document.title = html.title; document.dispatchEvent(new Event('zuni:viewloaded'));
            replacement.querySelector('[aria-current="page"]')?.focus({preventScroll:true});
            requestAnimationFrame(() => window.scrollTo({top:y,left:0,behavior:'instant'}));
        } catch (error) {
            if (signal.aborted) return;
            let message = page.querySelector('.guide-status');
            if (!message) { message = document.createElement('p'); message.className = 'guide-status'; message.setAttribute('role','alert'); page.querySelector('.guide-tabs').after(message); }
            message.textContent = en() ? 'The guide could not be loaded. Select the topic again to retry.' : 'No se pudo cargar la guía. Selecciona el tema otra vez para reintentar.';
        } finally { if (!signal.aborted) page.removeAttribute('aria-busy'); }
    }
    document.addEventListener('click', event => {
        const link = event.target.closest('.guide-tabs a');
        if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        const url = new URL(link.href);
        if (url.origin !== location.origin) return;
        event.preventDefault();
        if (url.href !== location.href) navigate(url.href);
    });
    window.addEventListener('popstate', event => {
        if (event.state?.zuniGuide) navigate(location.href,event.state.y ?? 0,true);
    });
})();
