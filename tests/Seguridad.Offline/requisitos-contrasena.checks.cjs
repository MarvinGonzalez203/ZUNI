const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const handlers = {};
const input = { id: 'Password', value: '', matches: selector => selector === 'input[autocomplete="new-password"]', setCustomValidity(message) { this.error = message; } };
const rules = ['number', 'upper', 'lower', 'symbol', 'length'].map(name => {
    const icon = { textContent: '○' }, state = { textContent: ': Pendiente' };
    return { dataset: { passwordRule: name }, met: false,
        classList: { toggle(_, value) { rules.find(rule => rule.dataset.passwordRule === name).met = value; } },
        querySelector: selector => selector === '[data-rule-icon]' ? icon : state };
});
const list = { dataset: { passwordFor: 'Password' }, querySelectorAll: () => rules };
const document = { readyState: 'complete', documentElement: { lang: 'es' },
    getElementById: id => id === input.id ? input : null,
    querySelectorAll: () => [list], addEventListener: (name, fn) => { handlers[name] = fn; } };
vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../../Zuni/wwwroot/js/requisitos-contrasena.js'), 'utf8'), { document });
function type(value) { input.value = value; handlers.input({ target: input }); }
function met() { return rules.map(rule => rule.met); }
assert.deepEqual(met(), [false, false, false, false, false]);
type('a1');
assert.deepEqual(met(), [true, false, true, false, false]);
assert.ok(input.error);
type('Abcdef1!');
assert.deepEqual(met(), [true, true, true, true, true]);
assert.equal(input.error, '');
assert.ok(rules.every(rule => rule.querySelector('[data-rule-icon]').textContent === '✓'));
type('Abcdef1');
assert.deepEqual(met(), [true, true, true, false, false]);
assert.ok(input.error, 'borrar caracteres vuelve a marcar requisitos pendientes');
type('Abcdef12 ');
assert.equal(rules[3].met, false, 'un espacio no cuenta como símbolo');
type('Ábcdef1!');
assert.ok(rules.every(rule => rule.met), 'acepta letras acentuadas');
type('');
assert.ok(rules.every(rule => !rule.met), 'vaciar el campo reinicia la lista');
input.value = 'Abcdef1!';
handlers.change();
assert.equal(input.error, '', 'actualiza al pegar o autocompletar');
type('Abcdef12');
document.documentElement.lang = 'en';
handlers['zuni:languagechange']();
assert.match(input.error, /symbol/);
assert.equal(rules[3].querySelector('[data-rule-state]').textContent, ': Pending');
console.log('OK: requisitos dinámicos, reversión, autocompletado, símbolos, acentos e idioma.');
