(() => {
    const controlsFor = label => {
        if (label.tagName === 'LEGEND') return [...label.parentElement.querySelectorAll('input, select, textarea')];
        const control = label.htmlFor && document.getElementById(label.htmlFor);
        return control ? [control] : [...label.querySelectorAll('input, select, textarea')];
    };
    const refresh = () => {
        document.querySelectorAll('form label, form legend[data-required-label]').forEach(label => {
            const controls = controlsFor(label).filter(control => control.type !== 'hidden');
            const conditional = label.hasAttribute('data-required-conditional');
            const required = controls.some(control => !control.matches(':disabled') && control.required)
                || (!conditional && label.dataset.requiredLabel === 'true' && (controls.length === 0 || !controls.every(control => control.matches(':disabled'))));
            let marker = label.querySelector(':scope > .required-marker');
            let description = label.querySelector(':scope > .required-description');
            if (required && !marker) {
                marker = document.createElement('span');
                marker.className = 'required-marker';
                marker.setAttribute('aria-hidden', 'true');
                marker.textContent = ' *';
                description = document.createElement('span');
                description.className = 'visually-hidden required-description';
                description.textContent = window.ZuniI18n.t(' (Campo obligatorio)');
                const nested = label.querySelector('input:not([type="checkbox"]), select, textarea');
                label.insertBefore(marker, nested);
                label.insertBefore(description, nested);
            }
            if (marker) marker.hidden = !required;
            if (description) description.hidden = !required;
            controls.forEach(control => {
                const value = required && !control.matches(':disabled') ? 'true' : 'false';
                if (control.getAttribute('aria-required') !== value) control.setAttribute('aria-required', value);
            });
        });
        document.querySelectorAll('form').forEach(form => {
            if (!form.querySelector('.required-marker:not([hidden])') || form.querySelector('.required-fields-help')) return;
            const help = document.createElement('p');
            help.className = 'required-fields-help';
            help.innerHTML = '<span class="required-marker" aria-hidden="true">*</span> '+window.ZuniI18n.t('Campo obligatorio');
            form.prepend(help);
        });
    };
    const start = () => {
        refresh();
        new MutationObserver(refresh).observe(document.body, {
            subtree: true, childList: true, attributes: true, attributeFilter: ['required', 'disabled']
        });
    };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true });
    else start();
    document.addEventListener('zuni:languagechange',()=>{
        document.querySelectorAll('.required-description').forEach(e=>e.textContent=window.ZuniI18n.t(' (Campo obligatorio)'));
        document.querySelectorAll('.required-fields-help').forEach(e=>[...e.childNodes].filter(n=>n.nodeType===Node.TEXT_NODE).forEach(n=>n.nodeValue=' '+window.ZuniI18n.t('Campo obligatorio')));
    });
})();
