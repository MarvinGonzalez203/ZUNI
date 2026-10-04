const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const vm = require('node:vm');
const source = readFileSync(require('node:path').join(__dirname, '../Zuni/wwwroot/js/estudiante-perfil.js'), 'utf8');

function setup() {
    const events = {}, windowEvents = {}, button = {}, status = {};
    let value = 'original', valid = true, focused = false;
    const form = {
        querySelector: selector => selector === 'button[type="submit"]' ? button
            : selector.includes(':invalid') ? { focus() { focused = true; } } : null,
        addEventListener: (name, callback) => events[name] = callback,
        checkValidity: () => valid,
        setAttribute() {}, removeAttribute() {}
    };
    vm.runInNewContext(source, {
        document: { getElementById: id => id === 'student-profile-form' ? form : status },
        window: { addEventListener: (name, callback) => windowEvents[name] = callback },
        FormData: class { entries() { return [['Carrera', value]]; } }
    });
    function fire(name, windowEvent = false) {
        const event = { prevented: false, preventDefault() { this.prevented = true; } };
        (windowEvent ? windowEvents : events)[name](event);
        return event;
    }
    return { fire, button, status, change: v => value = v, invalid: () => valid = false, focused: () => focused };
}

test('avisa al salir solo cuando hay cambios pendientes', () => {
    const page = setup();
    assert.equal(page.fire('beforeunload', true).prevented, false);
    page.change('nueva carrera');
    assert.equal(page.fire('beforeunload', true).prevented, true);
    page.change('original');
    assert.equal(page.fire('beforeunload', true).prevented, false);
});
test('rechaza formulario inválido sin bloquear el botón', () => {
    const page = setup();
    page.invalid();
    assert.equal(page.fire('submit').prevented, true);
    assert.notEqual(page.button.disabled, true);
    assert.equal(page.focused(), true);
});
test('permite guardar y bloquea un segundo envío', () => {
    const page = setup();
    page.change('nueva carrera');
    assert.equal(page.fire('submit').prevented, false);
    assert.equal(page.button.disabled, true);
    assert.equal(page.fire('submit').prevented, true);
    assert.equal(page.fire('beforeunload', true).prevented, false);
});
test('restaura el botón al volver desde la caché del navegador', () => {
    const page = setup();
    page.fire('submit');
    page.fire('pageshow', true);
    assert.equal(page.button.disabled, false);
    assert.equal(page.fire('submit').prevented, false);
});
