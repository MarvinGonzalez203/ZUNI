(() => {
    const actualizar = form => {
        const inicio = form.elements.HoraInicio, fin = form.elements.HoraFin;
        const parts = new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Guatemala', year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23' }).formatToParts(new Date());
        const value = key => parts.find(p => p.type === key).value;
        const hoy = `${value('year')}-${value('month')}-${value('day')}`;
        const actual = Number(value('hour')) * 60 + Number(value('minute'));
        const hora = n => `${String(Math.floor(n / 60)).padStart(2,'0')}:${String(n % 60).padStart(2,'0')}`;
        inicio.min = form.dataset.horarioDia === hoy ? hora(Math.min(1435, Math.max(480, Math.floor(actual / 5) * 5 + 5))) : '08:00';
        if (inicio.value) {
            const [h,m] = inicio.value.split(':').map(Number);
            const estimado = h * 60 + m + (Number(form.elements.DuracionMinutos.value) || 50);
            const msg = form.querySelector('[data-fin-estimado]');
            msg.textContent = estimado <= 1080 ? `Fin estimado si lo omites: ${hora(estimado)}. Es planificación; la sesión real puede durar más.` : 'Ese inicio no deja espacio para la duración antes de las 18:00. Elige otro inicio o duración.';
            fin.min = hora(Math.min(1435,h * 60 + m + 5));
        }
    };
    document.querySelectorAll('[data-horario-dia]').forEach(form => {
        form.addEventListener('input', () => actualizar(form));
        form.closest('.modal')?.addEventListener('show.bs.modal', () => { form.reset(); actualizar(form); });
        form.addEventListener('submit', () => actualizar(form));
        actualizar(form);
    });
})();
