(() => {
    'use strict';
    const cue = document.getElementById('corp-scroll-control');
    if (!cue) return;
    // Un único control: V para avanzar; al llegar al final se orienta hacia arriba.
    document.querySelectorAll('[data-corp-top]').forEach(button => button.hidden = true);
    let queued = false;
    const atEnd = () => document.documentElement.scrollHeight - innerHeight - scrollY <= 32;
    function update() {
        queued = false;
        cue.hidden = document.documentElement.scrollHeight <= innerHeight + 32;
        const up = atEnd(), en = document.documentElement.lang.startsWith('en');
        cue.classList.toggle('is-up', up);
        cue.dataset.direction = up ? 'up' : 'down';
        cue.setAttribute('aria-label', up ? (en ? 'Back to top' : 'Volver al principio') : (en ? 'View more content' : 'Ver más contenido'));
    }
    function schedule() { if (!queued) { queued = true; requestAnimationFrame(update); } }
    cue.addEventListener('click', () => {
        const behavior = matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth';
        if (atEnd()) scrollTo({top: 0, behavior});
        else scrollBy({top: Math.max(180, innerHeight * .72), behavior});
    });
    addEventListener('scroll', schedule, {passive: true});
    addEventListener('resize', schedule);
    addEventListener('pageshow', schedule);
    document.addEventListener('zuni:languagechange', schedule);
    new ResizeObserver(schedule).observe(document.body);
    schedule();
})();
