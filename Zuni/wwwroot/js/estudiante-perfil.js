(() => {
    'use strict';
    const form = document.getElementById('student-profile-form');
    if (!form) return;

    const button = form.querySelector('button[type="submit"]');
    const status = document.getElementById('profile-save-status');
    const snapshot = () => JSON.stringify(Array.from(new FormData(form).entries()));
    const initial = snapshot();
    let submitting = false;
    const dirty = () => snapshot() !== initial;

    const updateStatus = () => {
        status.textContent = dirty()
            ? 'Tienes cambios sin guardar.'
            : 'Los cambios se guardan al presionar Guardar perfil.';
    };
    form.addEventListener('input', updateStatus);
    form.addEventListener('change', updateStatus);

    window.addEventListener('beforeunload', event => {
        if (!submitting && dirty()) {
            event.preventDefault();
            event.returnValue = '';
        }
    });

    form.addEventListener('submit', event => {
        if (submitting) {
            event.preventDefault();
            return;
        }
        const valid = window.jQuery?.validator
            ? window.jQuery(form).valid()
            : form.checkValidity();
        if (!valid) {
            event.preventDefault();
            status.textContent = 'Revisa los campos indicados antes de guardar.';
            form.querySelector('.input-validation-error, :invalid')?.focus();
            return;
        }
        submitting = true;
        button.disabled = true;
        button.textContent = 'Guardando…';
        form.setAttribute('aria-busy', 'true');
        status.textContent = 'Guardando tu perfil…';
    });

    // Restaurar controles si el navegador recupera la página desde su caché.
    window.addEventListener('pageshow', () => {
        submitting = false;
        button.disabled = false;
        button.textContent = 'Guardar perfil';
        form.removeAttribute('aria-busy');
        updateStatus();
    });
    form.querySelector('.validation-summary-errors')?.focus();
})();
