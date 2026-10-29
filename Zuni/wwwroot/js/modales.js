(() => {
    const instances = new WeakMap();
    const en = () => document.documentElement.lang.startsWith('en');
    function initialize(modal) {
        if (instances.has(modal)) return instances.get(modal);
        const cue = document.createElement('button');
        cue.type = 'button'; cue.className = 'modal-scroll-cue'; cue.hidden = true;
        cue.setAttribute('aria-label', en() ? 'Scroll down for more information' : 'Desplazarse para ver más información');
        cue.innerHTML = '<svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="m5 9 7 7 7-7"/></svg>';
        modal.append(cue);
        let scroller;
        let scheduled = false;
        const update = () => {
            scheduled = false;
            if (!modal.classList.contains('show')) { cue.hidden = true; return; }
            scroller = [...modal.querySelectorAll('.modal-body,.modal-content'), modal].find(element =>
                /auto|scroll/.test(getComputedStyle(element).overflowY) && element.scrollHeight > element.clientHeight + 4);
            cue.hidden = !scroller || scroller.scrollHeight - scroller.clientHeight - scroller.scrollTop <= 12;
            if (cue.hidden) return;
            const region = scroller === modal ? modal.querySelector('.modal-content') : scroller;
            const rect = region.getBoundingClientRect();
            const top = Math.max(0, rect.top), bottom = Math.min(innerHeight, rect.bottom);
            if (bottom - top < 64) { cue.hidden = true; return; }
            cue.style.left = `${Math.max(24, Math.min(innerWidth - 24, rect.left + rect.width / 2))}px`;
            cue.style.top = `${bottom - 46}px`;
        };
        const schedule = () => { if (!scheduled) { scheduled = true; requestAnimationFrame(update); } };
        cue.addEventListener('click', () => {
            scroller?.scrollBy({top:Math.max(120, scroller.clientHeight * .7),behavior:matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth'});
        });
        modal.addEventListener('scroll', schedule, true);
        const resize = new ResizeObserver(schedule);
        resize.observe(modal);
        modal.querySelectorAll('.modal-content,.modal-body').forEach(element => resize.observe(element));
        // Watch content changes, excluding the cue's own style/hidden updates.
        const mutation = new MutationObserver(schedule);
        const content = modal.querySelector('.modal-content');
        if (content) mutation.observe(content, {childList:true,subtree:true,characterData:true,attributes:true});
        modal.addEventListener('hidden.bs.modal', () => { cue.hidden = true; });
        const dispose = () => { resize.disconnect(); mutation.disconnect(); window.removeEventListener('resize', schedule); };
        window.addEventListener('resize', schedule);
        const instance = {update,dispose}; instances.set(modal,instance);
        return instance;
    }
    document.addEventListener('shown.bs.modal', event => initialize(event.target).update());
    document.addEventListener('zuni:languagechange',()=>document.querySelectorAll('.modal-scroll-cue').forEach(cue=>cue.setAttribute('aria-label',en()?'Scroll down for more information':'Desplazarse para ver más información')));
    document.querySelectorAll('.modal.show').forEach(modal => initialize(modal).update());
    // Agenda pages can replace dialogs dynamically; release detached observers/listeners.
    new MutationObserver(records => {
        for (const record of records) for (const node of record.removedNodes) {
            if (!(node instanceof Element)) continue;
            for (const modal of [node,...node.querySelectorAll('.modal')]) {
                if (!modal.isConnected) { instances.get(modal)?.dispose(); instances.delete(modal); }
            }
        }
    }).observe(document.body,{childList:true,subtree:true});
})();
