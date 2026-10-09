const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const script = fs.readFileSync('Zuni/wwwroot/js/bigfive.js', 'utf8');
function fixture(done = 0, finalizedButton = false) {
    function element(extra = {}) { return { hidden: false, disabled: false, value: '', textContent: '', events: {}, scrollIntoView() { this.scrolled = true; }, addEventListener(name, cb) { this.events[name] = cb; }, ...extra }; }
    const pages = Array.from({ length: 5 }, () => element());
    const selects = Array.from({ length: 50 }, (_, i) => element({ value: i < done ? '3' : '' }));
    const finalButton = element();
    const form = element({ querySelectorAll: selector => selector === '.bf-page' ? pages : selects });
    const finish = finalizedButton ? element({ querySelector: () => finalButton }) : null;
    const nodes = { 'bigfive-form': form, 'bf-finalizar': finish, 'bf-anterior': element(), 'bf-siguiente': element(), 'bf-pagina': element(), 'bf-progress': element(), 'bf-avance': element(), 'bf-save-status': element() };
    const window = element();
    vm.runInNewContext(script, { document: { getElementById: id => nodes[id] }, window });
    function trigger(target, event) { const e = { prevented: false, preventDefault() { this.prevented = true; } }; target.events[event](e); return e; }
    return { nodes, form, pages, selects, window, finish, finalButton, trigger };
}
test('retoma el bloque de la primera afirmación pendiente, mostrando solo diez', () => {
    const f = fixture(13);
    assert.equal(f.pages.filter(p => !p.hidden).length, 1);
    assert.equal(f.pages[1].hidden, false);
    assert.match(f.nodes['bf-pagina'].textContent, /Bloque 2 de 5/);
});
test('navegar conserva selecciones y limita anterior/siguiente', () => {
    const f = fixture(); f.selects[0].value = '4';
    assert.equal(f.nodes['bf-anterior'].disabled, true);
    for (let i = 0; i < 4; i++) f.trigger(f.nodes['bf-siguiente'], 'click');
    assert.equal(f.pages[4].hidden, false); assert.equal(f.nodes['bf-siguiente'].disabled, true);
    assert.equal(f.pages[4].scrolled, true);
    f.trigger(f.nodes['bf-anterior'], 'click');
    assert.equal(f.selects[0].value, '4');
});
test('el progreso refleja cambios y se avisa al salir sin guardar', () => {
    const f = fixture(10); f.selects[10].value = '5'; f.trigger(f.form, 'change');
    assert.equal(f.nodes['bf-progress'].value, 11); assert.match(f.nodes['bf-avance'].textContent, /22 %/);
    assert.equal(f.trigger(f.window, 'beforeunload').prevented, true);
});
test('guardar bloquea el doble envío y permite navegar al guardar', () => {
    const f = fixture(); f.trigger(f.form, 'change');
    assert.equal(f.trigger(f.form, 'submit').prevented, false);
    assert.equal(f.trigger(f.form, 'submit').prevented, true);
    assert.equal(f.trigger(f.window, 'beforeunload').prevented, false);
});
test('un cambio pendiente impide finalizar respuestas viejas', () => {
    const f = fixture(50, true); f.selects[0].value = '1'; f.trigger(f.form, 'change');
    assert.equal(f.finalButton.disabled, true);
    assert.equal(f.trigger(f.finish, 'submit').prevented, true);
    assert.match(f.nodes['bf-save-status'].textContent, /Guarda los cambios/);
});
test('finalización bloquea envíos repetidos y se recupera al volver', () => {
    const f = fixture(50, true);
    assert.equal(f.trigger(f.finish, 'submit').prevented, false);
    assert.equal(f.finalButton.disabled, true);
    assert.equal(f.trigger(f.finish, 'submit').prevented, true);
    f.trigger(f.window, 'pageshow'); assert.equal(f.finalButton.disabled, false);
});
