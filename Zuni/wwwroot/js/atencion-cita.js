(() => {
    const form = document.getElementById('atencion-form');
    if (!form) return;
    const asistio = form.querySelector('#Asistio');
    const recomienda = form.querySelector('#RecomiendaOtraCita');
    function actualizar() {
        const presente = asistio.value === 'true';
        form.querySelectorAll('[data-con-asistencia]').forEach(panel => {
            panel.hidden = !presente;
            panel.querySelectorAll('textarea, input[type="checkbox"]').forEach(input => input.disabled = !presente);
        });
        const proxima = presente && recomienda.checked;
        form.querySelector('#proxima-cita').hidden = !proxima;
        form.querySelector('#IndicacionesProximaCita').disabled = !proxima;
        form.querySelector('#IndicacionesProximaCita').required = proxima;
        ['ResultadoProfesionalPrivado', 'ResenaRecomendacionesEstudiante', 'ResultadoBreveEstudiante']
            .forEach(id => form.querySelector('#' + id).required = presente);
    }
    asistio.addEventListener('change', actualizar);
    recomienda.addEventListener('change', actualizar);
    actualizar();
})();
