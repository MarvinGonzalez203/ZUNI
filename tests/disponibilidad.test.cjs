const { test } = require('node:test');
const assert = require('node:assert/strict');
const rules = require('../Zuni/wwwroot/js/disponibilidad-rules.js');
const input = { date: '2026-10-01', today: '2026-09-30', nowMinutes: 600, start: '08:00', end: '12:00', duration: 50, modality: 'Presencial' };
test('rechaza horas de madrugada y fuera de jornada', () => {
    assert.match(rules.validate({ ...input, start: '03:00' }), /entre/);
    assert.match(rules.validate({ ...input, end: '19:00' }), /entre/);
});
test('rechaza fechas pasadas, imposibles y horarios ya transcurridos', () => {
    assert.match(rules.validate({ ...input, date: '2026-09-29' }), /pasados/);
    assert.match(rules.validate({ ...input, date: '2026-02-30' }), /válida/);
    assert.match(rules.validate({ ...input, date: input.today }), /futura/);
});
test('rechaza cruces pero permite rangos contiguos', () => {
    assert.match(rules.validate(input, [{ start: '10:00', end: '13:00' }]), /cruza/);
    assert.equal(rules.validate(input, [{ start: '12:00', end: '13:00' }]), '');
});
test('valida orden, duración, intervalos y modalidad', () => {
    assert.match(rules.validate({ ...input, end: '08:00' }), /posterior/);
    assert.match(rules.validate({ ...input, end: '08:30' }), /completa/);
    assert.match(rules.validate({ ...input, duration: 20 }), /permitida/);
    assert.match(rules.validate({ ...input, start: '08:02' }), /5 minutos/);
    assert.match(rules.validate({ ...input, modality: 'Otra' }), /modalidad/);
});
test('genera solo citas completas dentro de un rango', () => {
    const slots = rules.slots(input);
    assert.equal(slots.length, 4);
    assert.deepEqual(slots[0], { start: 480, end: 530 });
    assert.deepEqual(slots[3], { start: 630, end: 680 });
    assert.equal(rules.validate({ ...input, start: '17:10', end: '18:00' }), '');
});
