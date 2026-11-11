(() => {
    const alumno = document.getElementById('PerfilEstudianteId');
    const aviso = document.querySelector('[data-imprevista-requisito]');
    if (!alumno || !aviso) return;
    function update() {
        const pendiente = alumno.selectedOptions[0]?.dataset.contrasenaPendiente === 'true';
        aviso.hidden = !pendiente;
        alumno.setCustomValidity(pendiente ? aviso.textContent.trim() : '');
    }
    alumno.addEventListener('change', update);
    document.addEventListener('zuni:languagechange', update);
    update();
})();
