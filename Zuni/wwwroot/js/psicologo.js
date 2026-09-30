// Interacciones exclusivas del prototipo: sin peticiones ni almacenamiento local.
(() => {
    'use strict';
    document.querySelectorAll('.modal form').forEach(form => {
        form.addEventListener('submit', event => event.preventDefault());
    });
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('hidden.bs.modal', () => {
            modal.querySelectorAll('form').forEach(form => form.reset());
        });
    });
})();
