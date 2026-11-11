(() => {
    const checks = {
        number: value => /[0-9]/.test(value),
        upper: value => /\p{Lu}/u.test(value),
        lower: value => /\p{Ll}/u.test(value),
        symbol: value => /[^\p{L}\p{N}\s]/u.test(value),
        length: value => value.length >= 8
    };
    function update(list) {
        const input = document.getElementById(list.dataset.passwordFor);
        if (!input) return;
        const en = document.documentElement.lang.startsWith('en');
        let valid = true;
        list.querySelectorAll('[data-password-rule]').forEach(rule => {
            const met = checks[rule.dataset.passwordRule](input.value);
            valid = valid && met;
            rule.classList.toggle('fw-semibold', met);
            const icon = met ? '✓' : '○';
            const state = ': ' + (met ? (en ? 'Met' : 'Cumplido') : (en ? 'Pending' : 'Pendiente'));
            if (rule.querySelector('[data-rule-icon]').textContent !== icon) rule.querySelector('[data-rule-icon]').textContent = icon;
            if (rule.querySelector('[data-rule-state]').textContent !== state) rule.querySelector('[data-rule-state]').textContent = state;
        });
        input.setCustomValidity(input.value && !valid ? (en
            ? 'Use at least 8 characters, a number, an uppercase letter, a lowercase letter and a symbol.'
            : 'Usa al menos 8 caracteres, un número, una letra mayúscula, una letra minúscula y un símbolo.') : '');
    }
    function updateAll() { document.querySelectorAll('[data-password-requirements]').forEach(update); }
    document.addEventListener('input', event => {
        if (!event.target.matches('input[autocomplete="new-password"]')) return;
        document.querySelectorAll('[data-password-requirements]').forEach(list => {
            if (list.dataset.passwordFor === event.target.id) update(list);
        });
    });
    document.addEventListener('change', updateAll);
    document.addEventListener('zuni:languagechange', updateAll);
    document.addEventListener('zuni:viewloaded', updateAll);
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', updateAll);
    else updateAll();
})();
