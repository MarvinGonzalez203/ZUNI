(() => {
    'use strict';
    document.querySelectorAll('.modal:not(.agenda-modal) form').forEach(form => form.addEventListener('submit', event => event.preventDefault()));
    document.querySelectorAll('.modal:not(.agenda-modal)').forEach(modal => modal.addEventListener('hidden.bs.modal', () => modal.querySelectorAll('form').forEach(form => form.reset())));
    const levelSelect = document.getElementById('resultado-evaluacion');
    if (levelSelect) {
        levelSelect.addEventListener('change', () => {
            const message = document.getElementById('resultado-nivel-aviso');
            message.textContent = levelSelect.value
                ? `${levelSelect.options[levelSelect.selectedIndex].textContent}: cuestionario pendiente de definición. No hay resultados para mostrar.`
                : 'Opciones provisionales. Los cuestionarios oficiales todavía no están disponibles.';
        });
    }
    const panel = document.getElementById('disponibilidad-panel');
    if (!panel) return;
    const rules = window.ZuniAvailabilityRules;
    const byId = id => document.getElementById(id);
    const form = byId('disponibilidad-form'), dateInput = byId('disponibilidad-fecha');
    const days = new Map(); // Estado temporal: nunca se guardan datos clínicos en el navegador.
    const zone = 'America/Guatemala';
    const current = () => {
        const parts = Object.fromEntries(new Intl.DateTimeFormat('en-US', { timeZone: zone, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).formatToParts(new Date()).map(part => [part.type, part.value]));
        return { date: `${parts.year}-${parts.month}-${parts.day}`, minutes: Number(parts.hour) * 60 + Number(parts.minute) };
    };
    let selected = current().date;
    let month = new Date(`${selected.slice(0, 7)}-01T12:00:00Z`);
    const dateKey = (year, monthIndex, day) => `${year}-${String(monthIndex + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    const dateLabel = date => new Intl.DateTimeFormat('es-GT', { dateStyle: 'full', timeZone: 'UTC' }).format(new Date(`${date}T12:00:00Z`));
    const hourLabel = minutes => `${String(Math.floor(minutes / 60)).padStart(2, '0')}:${String(minutes % 60).padStart(2, '0')}`;
    function render() {
        const now = current();
        dateInput.min = now.date;
        byId('calendario-mes').textContent = new Intl.DateTimeFormat('es-GT', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(month);
        const grid = byId('calendario-dias');
        grid.replaceChildren();
        const year = month.getUTCFullYear(), monthIndex = month.getUTCMonth();
        const offset = (month.getUTCDay() + 6) % 7;
        for (let i = 0; i < offset; i++) grid.append(document.createElement('span'));
        const count = new Date(Date.UTC(year, monthIndex + 1, 0)).getUTCDate();
        for (let day = 1; day <= count; day++) {
            const date = dateKey(year, monthIndex, day), state = days.get(date);
            const free = state?.ranges.some(range => rules.slots(range).some(slot => date > now.date || (date === now.date && slot.start > now.minutes)));
            const busy = state?.blocked || (state?.ranges.length && !free);
            const label = free ? 'Disponible' : busy ? 'Ocupado' : 'Sin definir';
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `calendar-day ${free ? 'is-available' : busy ? 'is-busy' : ''} ${date === selected ? 'is-selected' : ''}`;
            button.disabled = date < now.date;
            button.setAttribute('aria-pressed', String(date === selected));
            button.setAttribute('aria-label', `${dateLabel(date)}: ${label}`);
            const number = document.createElement('span'); number.textContent = day;
            const status = document.createElement('small'); status.textContent = label;
            button.append(number, status);
            button.addEventListener('click', () => select(date));
            grid.append(button);
        }
        dateInput.value = selected;
        byId('disponibilidad-dia').textContent = dateLabel(selected);
        renderRanges(now);
    }
    function select(date) {
        selected = date;
        month = new Date(`${date.slice(0, 7)}-01T12:00:00Z`);
        byId('disponibilidad-error').textContent = '';
        byId('disponibilidad-aviso').textContent = '';
        render();
    }
    function renderRanges(now) {
        const list = byId('disponibilidad-horarios'), state = days.get(selected);
        list.replaceChildren();
        if (state?.blocked) {
            const item = document.createElement('li'); item.className = 'small text-danger'; item.textContent = 'Día bloqueado por el profesional. Añade un horario para volver a habilitarlo.'; list.append(item);
        }
        (state?.ranges || []).forEach((range, index) => {
            const item = document.createElement('li'); item.className = 'availability-range';
            const description = document.createElement('div');
            const title = document.createElement('strong'); title.textContent = `${range.start}–${range.end} · ${range.modality}`;
            const detail = document.createElement('small'); detail.className = 'd-block text-secondary';
            const slots = rules.slots(range).filter(slot => selected > now.date || (selected === now.date && slot.start > now.minutes));
            detail.textContent = `${range.duration} min por cita · ${slots.length} espacios disponibles`;
            const times = document.createElement('small'); times.className = 'd-block text-secondary'; times.textContent = slots.map(slot => `${hourLabel(slot.start)}–${hourLabel(slot.end)}`).join(' · ');
            description.append(title, detail, times);
            const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'btn btn-outline-secondary btn-sm'; remove.textContent = 'Quitar'; remove.setAttribute('aria-label', `Quitar horario de ${range.start} a ${range.end}`);
            remove.addEventListener('click', () => { state.ranges.splice(index, 1); if (!state.ranges.length) days.delete(selected); byId('disponibilidad-aviso').textContent = 'Horario eliminado.'; render(); });
            item.append(description, remove); list.append(item);
        });
    }
    dateInput.addEventListener('change', () => { if (dateInput.checkValidity()) select(dateInput.value); });
    form.addEventListener('submit', event => {
        event.preventDefault();
        const now = current();
        const range = { date: dateInput.value, start: byId('disponibilidad-inicio').value, end: byId('disponibilidad-fin').value, duration: Number(byId('disponibilidad-duracion').value), modality: byId('disponibilidad-modalidad').value, today: now.date, nowMinutes: now.minutes };
        const error = rules.validate(range, days.get(range.date)?.ranges || [], panel.dataset.opening, panel.dataset.closing);
        byId('disponibilidad-error').textContent = error;
        if (error) return;
        const state = days.get(range.date) || { blocked: false, ranges: [] };
        state.blocked = false; state.ranges.push(range); state.ranges.sort((a, b) => a.start.localeCompare(b.start)); days.set(range.date, state);
        select(range.date); byId('disponibilidad-aviso').textContent = 'Disponibilidad añadida. El día aparece en verde.';
    });
    const validDate = () => {
        const now = current(); dateInput.min = now.date;
        if (!dateInput.reportValidity()) return false;
        select(dateInput.value); return true;
    };
    byId('disponibilidad-bloquear').addEventListener('click', () => { if (!validDate()) return; days.set(selected, { blocked: true, ranges: [] }); render(); byId('disponibilidad-aviso').textContent = 'Día marcado como ocupado. Se retiraron sus horarios disponibles.'; });
    byId('disponibilidad-limpiar').addEventListener('click', () => { if (!validDate()) return; days.delete(selected); render(); byId('disponibilidad-aviso').textContent = 'Día sin configuración.'; });
    for (const [id, delta] of [['calendario-anterior', -1], ['calendario-siguiente', 1]]) byId(id).addEventListener('click', () => { month = new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth() + delta, 1, 12)); render(); });
    byId('disponibilidad-enfocar').addEventListener('click', () => { panel.scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' }); dateInput.focus({ preventScroll: true }); });
    render();
})();
