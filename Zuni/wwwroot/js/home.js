(() => {
    const cue = document.querySelector('.home-scroll-cue');
    const footer = document.getElementById('zuni-footer');
    const header = document.querySelector('.home-page > header');
    if (!cue || !footer) return;
    const update = () => {
        cue.hidden = window.scrollY > 24 || document.documentElement.scrollHeight <= window.innerHeight + 24;
    };
    const measure = () => {
        if (header) document.documentElement.style.setProperty('--zuni-header-height', `${header.getBoundingClientRect().height}px`);
        update();
    };
    cue.addEventListener('click', () => {
        footer.focus({preventScroll:true});
        footer.scrollIntoView({block:'start',behavior:matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth'});
    });
    window.addEventListener('scroll', update, {passive:true});
    window.addEventListener('resize', measure);
    window.addEventListener('pageshow', measure);
    if (header && window.ResizeObserver) new ResizeObserver(measure).observe(header);
    measure();
})();
