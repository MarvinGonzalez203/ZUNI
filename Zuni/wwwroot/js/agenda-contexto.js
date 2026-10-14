(() => {
    document.querySelectorAll('.dash-nav a.is-active').forEach(a => a.setAttribute('aria-current','page'));
    document.querySelectorAll('[data-calendario-profesional]').forEach(select => select.addEventListener('change',()=>select.form.requestSubmit()));
    const calendario=document.querySelector('[data-modal-inicial]');
    const modal=calendario && document.getElementById(calendario.dataset.modalInicial);
    if(!modal || !window.bootstrap) return;
    const cita=document.getElementById('cita-agenda-'+calendario.dataset.citaSeleccionada);
    modal.addEventListener('shown.bs.modal',()=>{if(cita){cita.classList.add('agenda-selected-appointment');cita.scrollIntoView({block:'center'});}} ,{once:true});
    bootstrap.Modal.getOrCreateInstance(modal).show();
})();
