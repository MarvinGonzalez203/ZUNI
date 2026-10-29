(() => {
    const down = document.querySelector('.page-scroll-down'), up = document.querySelector('.page-scroll-top');
    if (!down || !up) return;
    let queued = false;
    const update = () => {
        queued = false;
        const bar = document.querySelector('.dashboard-topbar');
        if (bar) {
            const value = bar.getBoundingClientRect().height + 'px';
            if (document.documentElement.style.getPropertyValue('--zuni-topbar-height') !== value) document.documentElement.style.setProperty('--zuni-topbar-height', value);
        }
        const root = document.scrollingElement, viewport = window.visualViewport?.height || innerHeight;
        const obstructed = !!document.querySelector('.modal.show,.offcanvas.show');
        down.hidden = obstructed || root.scrollHeight - viewport - root.scrollTop <= 24;
        up.hidden = obstructed || root.scrollTop < 100;
        const nav = document.querySelector('.test-navigation:not([hidden])')?.getBoundingClientRect();
        down.style.bottom = nav && nav.top < innerHeight && nav.bottom > 0 ? Math.max(18, innerHeight - nav.top + 8) + 'px' : '';
    };
    const schedule = () => { if (!queued) { queued = true; requestAnimationFrame(update); } };
    const behavior = () => matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth';
    down.addEventListener('click', () => scrollBy({ top: Math.max(180, innerHeight * .65), behavior: behavior() }));
    up.addEventListener('click', () => scrollTo({ top: 0, behavior: behavior() }));
    addEventListener('scroll', schedule, { passive: true }); addEventListener('resize', schedule); addEventListener('pageshow', schedule);
    visualViewport?.addEventListener('resize', schedule);
    new ResizeObserver(schedule).observe(document.body);
    for (const name of ['zuni:viewloaded','zuni:languagechange','shown.bs.modal','hidden.bs.modal','shown.bs.offcanvas','hidden.bs.offcanvas']) document.addEventListener(name, schedule);
    new MutationObserver(schedule).observe(document.querySelector('main') || document.body, {childList:true,subtree:true,attributes:true,attributeFilter:['hidden']});
    schedule();
})();
