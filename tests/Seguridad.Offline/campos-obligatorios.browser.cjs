// Usa HTML renderizado por CamposObligatoriosChecks y los scripts reales.
// No abre perfiles del usuario ni se conecta a PostgreSQL.
const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const fixtures = process.argv[2];
if (!fixtures) throw new Error('Indica el directorio ZUNI_FORMS_PREVIEW_DIR utilizado por las pruebas offline.');
const root = path.resolve(__dirname, '../../Zuni/wwwroot');
let checks = 0;
function check(value, text) { assert.ok(value, text); checks++; }

(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
        // Todos los archivos se cargan localmente: ninguna petición sale a Internet.
        await page.route('**/*', route => route.abort());
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        async function load(name, scripts = []) {
            let html = fs.readFileSync(path.join(fixtures, name + '.html'), 'utf8').replace(/<script\b[^>]*src=[^>]*>[\s\S]*?<\/script>/gi, '');
            const bootstrap = fs.readFileSync(path.join(root, 'lib/bootstrap/dist/js/bootstrap.bundle.min.js'), 'utf8');
            html = html.replace('<head>', '<head><script>' + bootstrap.replace(/<\/script/gi, '<\\/script') + '</script>');
            await page.setContent(html);
            for (const css of ['lib/bootstrap/dist/css/bootstrap.min.css', 'css/dashboard.css', 'css/campos-obligatorios.css'])
                await page.addStyleTag({ path: path.join(root, css) });
            for (const script of scripts) await page.addScriptTag({ path: path.join(root, 'js', script) });
            await page.addScriptTag({ path: path.join(root, 'js/campos-obligatorios.js') });
        }
        async function required(id, value) {
            await page.waitForFunction(({ id, value }) => {
                const control = document.getElementById(id);
                const label = [...document.querySelectorAll('label')].find(label => label.htmlFor === id || label.contains(control));
                const marker = label?.querySelector('.required-marker');
                return control.required === value && Boolean(marker && !marker.hidden) === value && control.getAttribute('aria-required') === String(value);
            }, { id, value });
            checks++;
        }
        await load('institucion');
        await required('DominiosAutorizados', true);
        await page.selectOption('#PoliticaCorreo', '1');
        await required('DominiosAutorizados', false);
        await page.selectOption('#PoliticaCorreo', '2');
        await required('DominiosAutorizados', false);
        await page.selectOption('#PoliticaCorreo', '0');
        await required('DominiosAutorizados', true);
        check(await page.locator('#CorreoSoporte').getAttribute('aria-required') === 'false', 'correo de soporte opcional');

        await load('agregar');
        await page.selectOption('#Rol', 'Estudiante');
        await page.waitForFunction(() => document.querySelector('#carne-estudiante legend .required-marker:not([hidden])'));
        check(await page.locator('#CarneParte1').getAttribute('aria-required') === 'true', 'carné obligatorio para estudiante');
        await page.selectOption('#Rol', 'Director');
        await page.waitForFunction(() => document.querySelector('#CarneParte1').getAttribute('aria-required') === 'false');
        check(await page.locator('#carne-estudiante').getAttribute('hidden') !== null, 'carné deja de exigirse para director');

        await load('solicitud', ['solicitar-atencion.js']);
        await required('NombreReferente', false);
        await page.selectOption('#TipoIngreso', '1');
        await required('NombreReferente', true);
        await required('MotivoReferencia', true);
        await page.selectOption('#TipoIngreso', '0');
        await required('NombreReferente', false);
        await page.locator('#Contactos_1__Nombre').fill('Contacto');
        await required('Contactos_1__Telefono', true);
        await required('Contactos_0__Nombre', false);
        await page.locator('#Contactos_1__Nombre').fill('');
        await required('Contactos_1__Telefono', false);
        check(await page.locator('#Sexo').getAttribute('aria-required') === 'true', 'género obligatorio');

        await load('atencion', ['atencion-cita.js']);
        await page.selectOption('#Asistio', 'true');
        for (const id of ['ResultadoProfesionalPrivado', 'ResenaRecomendacionesEstudiante', 'ResultadoBreveEstudiante']) await required(id, true);
        await page.locator('#RecomiendaOtraCita').check();
        await required('IndicacionesProximaCita', true);
        await page.selectOption('#Asistio', 'false');
        await required('ResenaRecomendacionesEstudiante', false);
        await required('IndicacionesProximaCita', false);

        await load('editor', ['pruebas-director.js']);
        await page.locator('#agregar-dimension').click();
        await page.locator('#agregar-pregunta').click();
        await page.waitForFunction(() => [...document.querySelectorAll('#dimensiones label, #preguntas label')].every(label => label.querySelector('.required-marker:not([hidden])')));
        check(await page.locator('#preguntas label').count() === 6, 'pregunta nueva con dimensión y dos opciones');
        await page.locator('[data-add-opcion]').click();
        await page.waitForFunction(() => document.querySelectorAll('#preguntas label .required-marker:not([hidden])').length === 8);
        check(await page.locator('#preguntas input[aria-required="true"]').count() === 7, 'opción agregada marcada y accesible');
        await page.screenshot({ path: path.join(fixtures, 'editor-obligatorios.png'), fullPage: true });
        await load('revisar-edad');
        await page.selectOption('#Aprobada', 'true');
        await required('Documento', true);
        await required('FechaNacimiento', true);
        await required('DocumentoComprobado', true);
        await page.selectOption('#Aprobada', 'false');
        await required('FechaNacimiento', false);
        await page.screenshot({ path: path.join(fixtures, 'revision-edad.png'), fullPage: true });
        check(errors.length === 0, 'sin errores JavaScript: ' + errors.join(', '));
        console.log(`OK: ${checks} comprobaciones de formularios en navegador.`);
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
