// Reglas puras del prototipo; repetir estas validaciones en servidor al integrar.
((root) => {
    'use strict';
    const durations = [15, 30, 45, 50, 60];
    function minutes(value) {
        if (!/^\d{2}:\d{2}$/.test(value)) return NaN;
        const [hour, minute] = value.split(':').map(Number);
        return hour < 24 && minute < 60 ? hour * 60 + minute : NaN;
    }
    function validate(input, existing = [], opening = '08:00', closing = '18:00') {
        const start = minutes(input.start), end = minutes(input.end);
        const day = new Date(`${input.date}T12:00:00Z`);
        if (!/^\d{4}-\d{2}-\d{2}$/.test(input.date) || !Number.isFinite(day.getTime()) || day.toISOString().slice(0, 10) !== input.date)
            return 'Selecciona una fecha válida.';
        if (input.date < input.today) return 'No puedes configurar días pasados.';
        if (!Number.isFinite(start) || !Number.isFinite(end)) return 'Selecciona horas válidas.';
        if (start < minutes(opening) || end > minutes(closing)) return `El horario debe estar entre ${opening} y ${closing}.`;
        if (start >= end) return 'La hora final debe ser posterior a la inicial.';
        if (start % 5 || end % 5) return 'Selecciona las horas en intervalos de 5 minutos.';
        if (!durations.includes(input.duration)) return 'Selecciona una duración permitida.';
        if (end - start < input.duration) return 'El horario debe permitir al menos una cita completa.';
        if (!['Presencial', 'Virtual'].includes(input.modality)) return 'Selecciona una modalidad válida.';
        if (existing.some(range => start < minutes(range.end) && end > minutes(range.start)))
            return 'Este horario se cruza con otro definido para el mismo día.';
        if (input.date === input.today && start <= input.nowMinutes) return 'Selecciona una hora futura para hoy.';
        return '';
    }
    function slots(range) {
        const result = [];
        for (let start = minutes(range.start); start + range.duration <= minutes(range.end); start += range.duration)
            result.push({ start, end: start + range.duration });
        return result;
    }
    const rules = { validate, slots, minutes };
    if (typeof module !== 'undefined' && module.exports) module.exports = rules;
    else root.ZuniAvailabilityRules = rules;
})(typeof globalThis !== 'undefined' ? globalThis : this);
