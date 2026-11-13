(() => {
    if (window.zuniGuideAccess) return;
    window.zuniGuideAccess = true;
    const targets = {
        '#zuni-theme-control': '.zuni-theme-toggle', '#zuni-language-control': '.zuni-language button',
        '#zuni-menu-control': '.sidebar-collapse', '#zuni-release-control': '.release-control',
        '#zuni-scroll-control': '.page-scroll-down', '#zuni-logout-control': '.dash-logout'
    };
    document.addEventListener('click', event => {
        const link = event.target.closest('a[href]'), selector = targets[link?.getAttribute('href')];
        if (!selector) return;
        event.preventDefault();
        let target = document.querySelector(selector);
        if (selector === '.sidebar-collapse' && !target?.getClientRects().length) target = document.querySelector('.dash-menu');
        if (target?.matches('details')) { target.open = true; target = target.querySelector('summary'); }
        if (!target || !target.getClientRects().length) target = document.querySelector('main');
        target?.scrollIntoView({ block: 'center', behavior: 'auto' }); target?.focus({ preventScroll: true });
    });
})();
