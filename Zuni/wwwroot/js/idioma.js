(() => {
    // Solo memoria: el DOM vivo conserva respuestas, controles y listeners sin navegar ni persistir borradores.
    let original = document.documentElement.cloneNode(true), changing = false;
    const ignored = 'script,style,[contenteditable],[data-language-preserve]';
    function translate(current, before, after) {
        if (!current || !before || !after || before.nodeType !== after.nodeType || current.nodeType !== before.nodeType) return;
        if (before.nodeType === Node.TEXT_NODE) {
            if (before.nodeValue !== after.nodeValue && current.nodeValue === before.nodeValue) current.nodeValue = after.nodeValue;
            return;
        }
        if (before.nodeType !== Node.ELEMENT_NODE || before.tagName !== after.tagName || current.tagName !== before.tagName || current.matches(ignored)) return;
        for (const name of ['title','aria-label','placeholder','data-val-required','data-val-regex','data-val-email']) {
            if (before.getAttribute(name) !== after.getAttribute(name) && current.getAttribute(name) === before.getAttribute(name)) {
                if (after.hasAttribute(name)) current.setAttribute(name,after.getAttribute(name)); else current.removeAttribute(name);
            }
        }
        if(current.matches('input,textarea'))return;
        const children = node => [...node.childNodes].filter(n=>!(n.nodeType===Node.TEXT_NODE&&!n.nodeValue.trim())&&!(n.nodeType===Node.ELEMENT_NODE&&n.matches('script,style,.language-error')));
        const source = children(before), target = children(after), live = children(current);
        if (source.length !== target.length && current.tagName!=='BODY') return;
        source.forEach((node,i) => translate(live[i],node,target[i]));
    }
    document.addEventListener('zuni:viewloaded', () => { original = document.documentElement.cloneNode(true); });
    document.addEventListener('submit', async event => {
        const form = event.target.closest('form.zuni-language');
        if (!form) return;
        event.preventDefault(); if (changing) return;
        const button = form.querySelector('button[name="idioma"]');
        const data = new FormData(form); data.set('idioma',button.value);
        const expected = new URL(data.get('volver'),location.href);
        const currentPage=location.pathname+location.search, focused=document.activeElement;
        changing = true; button.disabled = true; form.setAttribute('aria-busy','true');
        try {
            const setting = await fetch(form.action,{method:'POST',body:data,credentials:'same-origin',headers:{Accept:'application/json'}});
            if(!setting.ok)throw new Error('Language unavailable');
            const destination=new URL((await setting.json()).volver,location.href);
            if(destination.origin!==location.origin||destination.pathname!==expected.pathname)throw new Error('Language destination changed');
            const response = await fetch(destination,{credentials:'same-origin',cache:'no-store',headers:{Accept:'text/html'}});
            if (!response.ok || new URL(response.url).pathname !== expected.pathname) throw new Error('Language unavailable');
            const html = new DOMParser().parseFromString(await response.text(),'text/html');
            if(location.pathname+location.search!==currentPage)throw new Error('Page changed during language request');
            const scope = html.querySelector('script[data-view-scope]')?.dataset.viewScope;
            if (scope !== document.querySelector('script[data-view-scope]')?.dataset.viewScope) throw new Error('Session changed');
            const payload = JSON.parse(html.getElementById('zuni-i18n-data').textContent);
            if(payload.language!==data.get('idioma'))throw new Error('Language cookie unavailable');
            const top = document.querySelector('.dashboard-topbar')?.getBoundingClientRect().bottom || 0;
            const anchor = [...document.querySelectorAll('main h1,main h2,main p,main [data-question],main article')]
                .find(e=>e.getBoundingClientRect().bottom>top+12 && e.getBoundingClientRect().top<innerHeight && e.getClientRects().length);
            const offset = anchor?.getBoundingClientRect().top, x = scrollX, y = scrollY;
            const scrollers = [...document.querySelectorAll('.dashboard-sidebar,.modal-body,[data-modal-scroll]')].map(e=>[e,e.scrollTop]);
            translate(document.body,original.querySelector('body'),html.body);
            document.title = html.title; document.documentElement.lang = payload.language;
            window.ZuniI18n.language = payload.language; window.ZuniI18n.messages = payload.messages;
            for(const control of document.querySelectorAll('form.zuni-language button[name="idioma"]')) {
                control.value = payload.language==='en'?'es':'en'; control.querySelector('span').textContent = payload.language.toUpperCase();
            }
            original = html.documentElement.cloneNode(true);
            document.dispatchEvent(new CustomEvent('zuni:languagechange',{detail:{language:payload.language}}));
            scrollers.forEach(([e,value])=>{e.scrollTop=value;});
            const targetY=Math.max(0,y+(anchor?anchor.getBoundingClientRect().top-offset:0));
            const restore = () => window.scrollTo({left:x,top:targetY,behavior:'instant'});
            restore(); requestAnimationFrame(restore);
            form.querySelector('[role="alert"]')?.remove();
        } catch (failure) {
            document.dispatchEvent(new CustomEvent('zuni:languageerror',{detail:{reason:failure.message}}));
            let error = form.querySelector('[role="alert"]');
            if(!error){error=document.createElement('span');error.className='language-error';error.setAttribute('role','alert');form.append(error);}
            error.textContent=document.documentElement.lang==='en'?'Language could not be changed. Your answers are preserved; try again.':'No se pudo cambiar el idioma. Tus respuestas se conservan; intenta nuevamente.';
        } finally { changing=false;button.disabled=false;form.removeAttribute('aria-busy');if(focused===button)button.focus({preventScroll:true}); }
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
