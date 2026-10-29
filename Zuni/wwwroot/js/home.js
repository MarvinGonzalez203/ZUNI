(() => {
    const header = document.querySelector('.home-page > header');
    if (!header) return;
    const measure = () => document.documentElement.style.setProperty('--zuni-header-height', header.getBoundingClientRect().height + 'px');
    new ResizeObserver(measure).observe(header); measure();
})();