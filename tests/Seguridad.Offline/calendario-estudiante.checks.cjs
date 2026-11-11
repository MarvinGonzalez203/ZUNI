const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync(path.resolve(__dirname, '../../Zuni/wwwroot/js/calendario-estudiante.js'), 'utf8');

function harness() {
    const events = {}, windowEvents = {};
    const state = { requests: 0, replacements: 0, changed: 0, modal: false, lang: 'es', scope: 'estudiante-1' };
    const status = { textContent: '' };
    const fragment = value => ({
        innerHTML: value, childNodes: [value],
        cloneNode() { return fragment(this.innerHTML); },
        querySelectorAll() { return []; }
    });
    const current = Object.assign(fragment('original'), {
        querySelector: () => status,
        replaceChildren(...nodes) { this.innerHTML = nodes.join(''); state.replacements++; }
    });
    const next = fragment('horario nuevo');
    const html = {
        documentElement: { lang: 'es' },
        querySelector: selector => selector === '[data-calendario-estudiante]' ? next : { dataset: { viewScope: state.scope } }
    };
    const document = {
        hidden: false, documentElement: { lang: 'es' },
        querySelector: selector => selector === '[data-calendario-estudiante]' ? current :
            selector.startsWith('.modal') ? state.modal : { dataset: { viewScope: 'estudiante-1' } },
        addEventListener: (name, fn) => { events[name] = fn; },
        dispatchEvent: () => { state.changed++; }
    };
    const location = { href: 'https://zuni.test/Estudiante/Citas?fecha=2030-01-07' };
    const window = {
        addEventListener: (name, fn) => { windowEvents[name] = fn; },
        setInterval: (fn, delay) => { state.timer = fn; assert.equal(delay, 30000); }
    };
    let respond = async () => ({ ok: true, url: location.href, text: async () => '<html>' });
    const context = {
        document, location, window,
        fetch: async (url, options) => {
            state.requests++;
            assert.equal(options.cache, 'no-store');
            assert.equal(options.credentials, 'same-origin');
            return respond();
        },
        DOMParser: class { parseFromString() { return html; } },
        CustomEvent: class {}
    };
    vm.runInNewContext(source, context);
    return { state, current, next, html, document, window, location, events,
        focus: () => windowEvents.focus(), respond: fn => { respond = fn; } };
}

(async () => {
    const h = harness();
    await h.focus();
    assert.equal(h.state.replacements, 1, 'volver a la pestaña muestra nuevos horarios');
    assert.equal(h.state.changed, 1, 'notifica a los controles compartidos');
    await h.state.timer();
    assert.equal(h.state.replacements, 1, 'no reemplaza un calendario sin cambios');
    h.state.modal = true;
    await h.focus();
    assert.equal(h.state.requests, 2, 'no consulta mientras una reserva está abierta');
    h.state.modal = false;
    h.document.hidden = true;
    await h.state.timer();
    assert.equal(h.state.requests, 2, 'no consulta pestañas ocultas');
    h.document.hidden = false;
    h.window.ZuniLanguageChanging = true;
    await h.focus();
    assert.equal(h.state.requests, 2, 'no interrumpe un cambio de idioma');

    for (const conflict of ['modal', 'scope', 'lang', 'url']) {
        const c = harness();
        c.respond(async () => {
            if (conflict === 'modal') c.state.modal = true;
            if (conflict === 'scope') c.state.scope = 'otra-cuenta';
            if (conflict === 'lang') c.html.documentElement.lang = 'en';
            if (conflict === 'url') c.location.href += '&pagina=2';
            return { ok: true, url: 'https://zuni.test/Estudiante/Citas?fecha=2030-01-07', text: async () => '<html>' };
        });
        await c.focus();
        assert.equal(c.state.replacements, 0, 'descarta respuesta tras cambiar ' + conflict);
    }
    const redirected = harness();
    redirected.respond(async () => ({ ok: true, url: 'https://zuni.test/Cuenta/IniciarSesion' }));
    await redirected.focus();
    assert.equal(redirected.state.replacements, 0, 'no inserta una pantalla de acceso en el calendario');
    const failed = harness();
    failed.respond(async () => { throw new Error('offline'); });
    await failed.focus();
    assert.equal(failed.state.replacements, 0, 'conserva el calendario ante fallo de conexión');
    assert.match(failed.current.querySelector().textContent, /Recarga/);
    console.log('OK: actualización del calendario, reservas abiertas, contexto, sesión y fallos de conexión.');
})().catch(error => { console.error(error); process.exitCode = 1; });
