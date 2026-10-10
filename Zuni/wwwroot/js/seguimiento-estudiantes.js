(() => {
    'use strict';
    const record = document.getElementById('student-record');
    if (!record) return;
    const modal = bootstrap.Modal.getOrCreateInstance(record);
    let changingDialog = false;
    // Bootstrap dialogs belong at page level, outside the scrollable record.
    record.querySelectorAll('.cita-real').forEach(dialog => {
        document.body.appendChild(dialog);
        const management = bootstrap.Modal.getOrCreateInstance(dialog);
        record.querySelectorAll('[data-bs-target="#' + dialog.id + '"]').forEach(button => {
            button.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                changingDialog = true;
                record.addEventListener('hidden.bs.modal', () => management.show(), { once: true });
                modal.hide();
            });
        });
        dialog.querySelectorAll('[data-bs-target="#student-record"]').forEach(button => {
            button.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                management.hide();
            });
        });
        dialog.addEventListener('hidden.bs.modal', () => {
            if (!changingDialog) return;
            changingDialog = false;
            modal.show();
        });
    });
    modal.show();
    if (new URL(window.location.href).searchParams.get('seccion') === 'citas') {
        bootstrap.Tab.getOrCreateInstance(document.getElementById('tab-citas')).show();
    }
    record.addEventListener('hidden.bs.modal', () => {
        if (changingDialog) return;
        const url = new URL(window.location.href);
        url.searchParams.delete('estudiante');
        url.searchParams.delete('seccion');
        window.history.replaceState(null, '', url);
    });
})();
