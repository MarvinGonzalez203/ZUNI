(() => {
    const root = document.documentElement;
    const key='zuni-theme:'+(document.currentScript?.dataset.viewScope||'publico');
    root.dataset.viewScope=document.currentScript?.dataset.viewScope||'publico';
    let theme = 'light';
    try { theme = localStorage.getItem(key) === 'dark' ? 'dark' : 'light'; } catch {}
    const apply = value => {
        root.dataset.theme = value;
        root.dataset.bsTheme = value;
        root.style.colorScheme = value;
        const en = root.lang.startsWith('en');
        document.querySelectorAll('.zuni-theme-toggle').forEach(button => {
            button.setAttribute('aria-label', en ? `Switch to ${value === 'dark' ? 'light' : 'dark'} theme` : `Cambiar a tema ${value === 'dark' ? 'claro' : 'oscuro'}`);
            button.setAttribute('aria-pressed', String(value === 'dark'));
        });
    };
    apply(theme);
    document.addEventListener('DOMContentLoaded', () => {
        apply(root.dataset.theme);
        document.querySelectorAll('.zuni-theme-toggle').forEach(button => button.addEventListener('click', () => {
            const next = root.dataset.theme === 'dark' ? 'light' : 'dark';
            apply(next);
            try { localStorage.setItem(key, next); } catch {}
        }));
    });
    window.addEventListener('storage', event => { if (event.key === key) apply(event.newValue === 'dark' ? 'dark' : 'light'); });
    document.addEventListener('zuni:languagechange',()=>apply(root.dataset.theme));
})();
