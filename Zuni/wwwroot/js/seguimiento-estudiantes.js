(() => {
    'use strict';
    const record = document.getElementById('student-record');
    if (!record) return;
    const modal = bootstrap.Modal.getOrCreateInstance(record);
    modal.show();
    record.addEventListener('hidden.bs.modal', () => {
        const url = new URL(window.location.href);
        url.searchParams.delete('estudiante');
        window.history.replaceState(null, '', url);
    });
})();
