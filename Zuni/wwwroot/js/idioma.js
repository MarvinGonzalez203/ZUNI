(() => {
    // Reconciliar solo texto de la misma vista. No reemplazar formularios ni controles vivos.
    let original = document.documentElement.cloneNode(true), changing = false;
    const ignored = 'script,style,[contenteditable],[data-language-preserve]';
    const runtime = 'script,style,.language-error,.required-marker,.required-description,.required-fields-help,.modal-scroll-cue,.modal-backdrop,.offcanvas-backdrop';
    const children = node => [...node.childNodes].filter(n => !(n.nodeType === Node.TEXT_NODE && !n.nodeValue.trim()) && !(n.nodeType === Node.ELEMENT_NODE && n.matches(runtime)));
    document.addEventListener('DOMContentLoaded',()=>{original=document.documentElement.cloneNode(true);},{once:true});
    function key(node) {
        if (node.nodeType === Node.TEXT_NODE) return '#text';
        if (node.nodeType !== Node.ELEMENT_NODE) return '#other';
        const identity = ['id','name','href','data-question','data-field'].map(a => node.getAttribute(a) || '').join('|');
        const classes = [...node.classList].filter(c => !['show','active','is-active','collapsed','collapsing','was-validated'].includes(c)).sort().join('.');
        return node.tagName + '|' + identity + '|' + classes;
    }
    function translate(current, before, after) {
        if (!current || !before || !after || before.nodeType !== after.nodeType || current.nodeType !== before.nodeType) return;
        if (before.nodeType === Node.TEXT_NODE) {
            if (before.nodeValue !== after.nodeValue && current.nodeValue === before.nodeValue) current.nodeValue = after.nodeValue;
            return;
        }
        if (before.nodeType !== Node.ELEMENT_NODE || before.tagName !== after.tagName || current.tagName !== before.tagName || current.matches(ignored)) return;
        for (const name of ['title','aria-label','placeholder','data-val-required','data-val-regex','data-val-email']) {
            if (before.getAttribute(name) !== after.getAttribute(name) && current.getAttribute(name) === before.getAttribute(name)) {
                if (after.hasAttribute(name)) current.setAttribute(name, after.getAttribute(name)); else current.removeAttribute(name);
            }
        }
        if (current.matches('input,textarea')) return;
        const source = children(before), target = children(after), live = children(current);
        let t = 0, l = 0;
        for (const node of source) {
            const signature = key(node);
            const ti = target.findIndex((n,i) => i >= t && key(n) === signature);
            const li = live.findIndex((n,i) => i >= l && key(n) === signature);
            if (ti < 0 || li < 0) continue;
            translate(live[li], node, target[ti]); t = ti + 1; l = li + 1;
        }
    }
    document.addEventListener('zuni:viewloaded', event => {
        original = (event.detail?.document?.documentElement || document.documentElement).cloneNode(true);
    });
    // Evitar el scroll de enfoque nativo, especialmente en barras móviles con controles fijos.
    document.addEventListener('pointerdown', event => {
        if (event.target.closest('form.zuni-language button') && event.button === 0) event.preventDefault();
    });
    document.addEventListener('submit', async event => {
        const form = event.target.closest('form.zuni-language'); if (!form) return;
        event.preventDefault(); if (changing) return;
        const button = form.querySelector('button[name="idioma"]'), data = new FormData(form);
        data.set('idioma', button.value);
        // Filters and guide navigation can change the URL without replacing the toolbar.
        data.set('volver', location.pathname + location.search + location.hash);
        const expected = new URL(data.get('volver'), location.href), currentPage = location.pathname + location.search;
        const oldLanguage = document.documentElement.lang.startsWith('en') ? 'en' : 'es';
        const focused = document.activeElement;
        changing = window.ZuniLanguageChanging = true; button.disabled = true; form.setAttribute('aria-busy','true');
        document.dispatchEvent(new Event('zuni:languagechanging'));
        try {
            const setting = await fetch(form.action, {method:'POST',body:data,credentials:'same-origin',headers:{Accept:'application/json'}});
            if (!setting.ok) throw new Error('Language unavailable');
            const destination = new URL((await setting.json()).volver, location.href);
            if (destination.origin !== location.origin || destination.pathname !== expected.pathname) throw new Error('Language destination changed');
            const response = await fetch(destination, {credentials:'same-origin',cache:'no-store',headers:{Accept:'text/html'}});
            if (!response.ok || new URL(response.url).pathname !== expected.pathname) throw new Error('Language unavailable');
            const html = new DOMParser().parseFromString(await response.text(),'text/html');
            if (location.pathname + location.search !== currentPage) throw new Error('Page changed during language request');
            if (html.querySelector('script[data-view-scope]')?.dataset.viewScope !== document.querySelector('script[data-view-scope]')?.dataset.viewScope) throw new Error('Session changed');
            const payload = JSON.parse(html.getElementById('zuni-i18n-data').textContent);
            if (payload.language !== data.get('idioma')) throw new Error('Language cookie unavailable');
            const x = scrollX, y = scrollY, root = document.documentElement;
            const previousAnchor = root.style.overflowAnchor, previousBehavior = root.style.scrollBehavior;
            root.style.overflowAnchor = 'none'; root.style.scrollBehavior = 'auto';
            const scrollers = [...document.querySelectorAll('.dashboard-sidebar,[data-sidebar-scroll],.modal-body,[data-modal-scroll]')].map(e => [e,e.scrollTop]);
            translate(document.body, original.querySelector('body'), html.body);
            document.title = html.title; root.lang = payload.language;
            window.ZuniI18n.language = payload.language; window.ZuniI18n.messages = payload.messages;
            for (const control of document.querySelectorAll('form.zuni-language button[name="idioma"]')) {
                control.value = payload.language === 'en' ? 'es' : 'en'; control.querySelector('span').textContent = payload.language.toUpperCase();
            }
            original = html.documentElement.cloneNode(true);
            document.dispatchEvent(new CustomEvent('zuni:languagechange',{detail:{language:payload.language}}));
            const restore = () => { scrollers.forEach(([e,value]) => e.scrollTop = value); scrollTo({left:x,top:y,behavior:'instant'}); };
            restore();
            await new Promise(resolve => requestAnimationFrame(() => { restore(); requestAnimationFrame(() => { restore(); resolve(); }); }));
            root.style.overflowAnchor = previousAnchor; root.style.scrollBehavior = previousBehavior;
            form.querySelector('[role="alert"]')?.remove();
        } catch (failure) {
            // Si el GET falla tras cambiar la cookie, conservar el idioma efectivo de la vista.
            const rollback = new FormData(form); rollback.set('idioma',oldLanguage);
            try { await fetch(form.action,{method:'POST',body:rollback,credentials:'same-origin',headers:{Accept:'application/json'}}); } catch {}
            document.dispatchEvent(new CustomEvent('zuni:languageerror',{detail:{reason:failure.message}}));
            let error = form.querySelector('[role="alert"]');
            if (!error) { error = document.createElement('span'); error.className = 'language-error'; error.setAttribute('role','alert'); form.append(error); }
            error.textContent = oldLanguage === 'en' ? 'Language could not be changed. Your answers are preserved; try again.' : 'No se pudo cambiar el idioma. Tus respuestas se conservan; intenta nuevamente.';
        } finally {
            changing = window.ZuniLanguageChanging = false; button.disabled = false; form.removeAttribute('aria-busy');
            if (focused === button) button.focus({preventScroll:true});
        }
    });
    const localized = new WeakSet();
    document.addEventListener('invalid', event => {
        const field = event.target;
        if (!field.validity || !window.ZuniI18n || (field.validity.customError && !localized.has(field))) return;
        field.setCustomValidity('');
        const v = field.validity;
        const key = v.valueMissing ? (field.dataset.valRequired || 'Campo obligatorio')
            : v.typeMismatch && field.type === 'email' ? 'Ingresa un correo válido.'
            : v.patternMismatch ? (field.dataset.valRegex || 'Utiliza el formato indicado.')
            : 'Ingresa un valor válido.';
        field.setCustomValidity(window.ZuniI18n.t(key)); localized.add(field);
    }, true);
    for (const name of ['input','change']) document.addEventListener(name,event => {
        if (localized.has(event.target)) { event.target.setCustomValidity(''); localized.delete(event.target); }
    }, true);
})();
