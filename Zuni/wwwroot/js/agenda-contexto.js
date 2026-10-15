(() => {
    const sidebar = document.getElementById('dashboard-sidebar');
    if (sidebar) {
        const key = 'zuni:sidebar:' + (document.documentElement.dataset.viewScope||'publico') + ':' + (sidebar.dataset.navKey || sidebar.querySelector('nav')?.getAttribute('aria-label') || 'cuenta');
        try {
            const saved = JSON.parse(sessionStorage.getItem(key) || 'null');
            const groups = [...sidebar.querySelectorAll('.dash-nav-group')];
            groups.forEach(group => {
                const label = group.querySelector('a')?.getAttribute('href');
                if (!group.open && saved?.groups?.[label]) group.open = true;
            });
            const remember = () => sessionStorage.setItem(key, JSON.stringify({
                top: sidebar.scrollTop,
                groups: Object.fromEntries(groups.map(g => [g.querySelector('a')?.getAttribute('href'), g.open]))
            }));
            if (Number.isFinite(saved?.top)) requestAnimationFrame(() => requestAnimationFrame(() => { sidebar.scrollTop = saved.top; }));
            sidebar.addEventListener('scroll', remember, {passive:true});
            groups.forEach(g => g.addEventListener('toggle', remember));
            window.addEventListener('pagehide', remember);
        } catch { /* El menú sigue funcionando si el almacenamiento está deshabilitado. */ }
    }
    document.querySelectorAll('.dash-nav a.is-active').forEach(a => a.setAttribute('aria-current','page'));
    document.querySelectorAll('[data-calendario-profesional]').forEach(select => select.addEventListener('change',()=>select.form.requestSubmit()));
    const calendario=document.querySelector('[data-modal-inicial]');
    const modal=calendario && document.getElementById(calendario.dataset.modalInicial);
    if(!modal || !window.bootstrap) return;
    const cita=document.getElementById('cita-agenda-'+calendario.dataset.citaSeleccionada);
    modal.addEventListener('shown.bs.modal',()=>{if(cita){cita.classList.add('agenda-selected-appointment');cita.scrollIntoView({block:'center'});}} ,{once:true});
    bootstrap.Modal.getOrCreateInstance(modal).show();
})();
