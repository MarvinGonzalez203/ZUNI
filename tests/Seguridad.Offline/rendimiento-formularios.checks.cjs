const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
let observe, frames = [], scans = 0, globalScans = 0;
class Element {
    constructor(form = null) { this.form = form; this.isConnected = true; }
    matches(selector) { return selector.split(',').includes('form') && this.isForm; }
    closest() { return this.form; }
    querySelector() { return null; }
    querySelectorAll() { scans++; return []; }
}
const form = new Element(); form.isForm = true;
const document = {
    readyState: 'complete', body: new Element(),
    querySelectorAll() { globalScans++; return [form]; },
    addEventListener() {}, getElementById() { return null; }
};
vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../../Zuni/wwwroot/js/campos-obligatorios.js'), 'utf8'), {
    document, Element, window: { ZuniI18n: { t: text => text } },
    MutationObserver: class { constructor(callback) { observe = callback; } observe() {} },
    requestAnimationFrame: callback => { frames.push(callback); }
});
assert.equal(globalScans, 1, 'un solo barrido global al iniciar');
scans = 0;
for (let i = 0; i < 100; i++) observe([{ type: 'childList', target: new Element(form), addedNodes: [{ nodeType: 3 }], removedNodes: [] }]);
assert.equal(scans, 0, '100 cambios de texto o checks no recorren formularios');
assert.equal(frames.length, 0, 'no agenda trabajo por cambios de texto');
for (let i = 0; i < 100; i++) observe([{ type: 'attributes', target: new Element(form) }]);
assert.equal(frames.length, 1, '100 cambios de requisitos se agrupan en un fotograma');
frames.shift()();
assert.equal(scans, 1, 'revisa únicamente el formulario afectado una vez');
assert.equal(globalScans, 1, 'no vuelve a escanear todos los formularios');
const added = new Element(); added.isForm = true;
observe([{ type: 'childList', target: document.body, addedNodes: [added], removedNodes: [] }]);
assert.equal(frames.length, 1, 'detecta formularios añadidos dinámicamente');
frames.shift()();
assert.equal(scans, 3, 'solo consulta el nuevo nodo y el formulario añadido');
console.log('OK: 100 cambios de texto sin barridos; 100 cambios de atributos agrupados en una revisión local.');
