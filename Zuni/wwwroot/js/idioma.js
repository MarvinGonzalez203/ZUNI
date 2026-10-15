(() => {
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
