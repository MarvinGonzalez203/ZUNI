const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const events = {}, alumnoEvents = {};
const aviso = { hidden: true, textContent: 'El estudiante debe cambiar su contraseña temporal.' };
const alumno = { selectedOptions: [{ dataset: { contrasenaPendiente: 'false' } }],
    setCustomValidity(message) { this.validityMessage = message; },
    addEventListener: (name, fn) => { alumnoEvents[name] = fn; } };
vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../../Zuni/wwwroot/js/imprevista.js'), 'utf8'), {
    document: { getElementById: () => alumno, querySelector: () => aviso, addEventListener: (name, fn) => { events[name] = fn; } }
});
assert.equal(aviso.hidden, true);
assert.equal(alumno.validityMessage, '');
alumno.selectedOptions[0].dataset.contrasenaPendiente = 'true';
alumnoEvents.change();
assert.equal(aviso.hidden, false, 'avisa al seleccionar un alumno pendiente');
assert.match(alumno.validityMessage, /contraseña temporal/, 'explica el requisito antes de enviar');
aviso.textContent = 'The student must change their temporary password.';
events['zuni:languagechange']();
assert.match(alumno.validityMessage, /temporary password/, 'actualiza la explicación al cambiar idioma');
alumno.selectedOptions = [];
alumnoEvents.change();
assert.equal(aviso.hidden, true);
assert.equal(alumno.validityMessage, '', 'retira el bloqueo al cambiar la selección');
console.log('OK: aviso anticipado de contraseña pendiente, validación del formulario e idioma.');
