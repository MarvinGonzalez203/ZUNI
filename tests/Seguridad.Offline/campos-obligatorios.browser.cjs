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
            for (const href of await page.locator('link[rel="stylesheet"]').evaluateAll(links=>links.map(link=>link.getAttribute('href')))) {
                const local=path.resolve(root,href.split('?')[0].replace(/^\//,''));
                if(local.startsWith(root+path.sep)&&fs.existsSync(local)) await page.addStyleTag({path:local});
            }
            const scoped=path.resolve(root,'../obj/Release/net8.0/scopedcss/bundle/Zuni.styles.css');
            if(fs.existsSync(scoped)) await page.addStyleTag({path:scoped});
            for (const css of ['lib/bootstrap/dist/css/bootstrap.min.css', 'css/dashboard.css', 'css/campos-obligatorios.css', 'css/agenda-contexto.css', 'css/idioma.css'])
                await page.addStyleTag({ path: path.join(root, css) });
            await page.addScriptTag({path:path.join(root,'js/idioma.js')});
            for (const script of scripts) await page.addScriptTag({ path: path.join(root, 'js', script) });
            await page.addScriptTag({ path: path.join(root, 'js/campos-obligatorios.js') });
            await page.addStyleTag({content:'*,*::before,*::after{transition:none!important;animation:none!important}'});
            const imageData={};
            for (const src of await page.locator('img[src^="/images/"]').evaluateAll(imgs=>imgs.map(img=>img.getAttribute('src')))) {
                const local=path.resolve(root,src.split('?')[0].slice(1));
                if(local.startsWith(root+path.sep)&&fs.existsSync(local)) imageData[src]='data:image/png;base64,'+fs.readFileSync(local).toString('base64');
            }
            await page.locator('img[src^="/images/"]').evaluateAll((imgs,data)=>imgs.forEach(img=>{const value=data[img.getAttribute('src')];if(value)img.src=value;}),imageData);
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
        for (const color of ['#111111','#b42332','#216e45','#2458a6','#cea2fd']) {
            await page.locator(`[data-color-institucion="${color}"]`).click();
            check(await page.locator('#ColorPrincipal').inputValue()===color,'color predefinido '+color);
            check(await page.locator('#color-preview').evaluate(e=> {
                const rgb=getComputedStyle(e).backgroundColor.match(/\d+/g).slice(0,3).map(Number).map(v=>v/255).map(v=>v<=.04045?v/12.92:((v+.055)/1.055)**2.4);
                const l=.2126*rgb[0]+.7152*rgb[1]+.0722*rgb[2];
                return (getComputedStyle(e).color==='rgb(0, 0, 0)'?(l+.05)/.05:1.05/(l+.05))>=4.5;
            }),'contraste legible '+color);
        }
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
        await load('solicitud', ['solicitar-atencion.js']);
        for (const genero of ['Mujer', 'Hombre', 'No binario', 'Otros']) {
            await page.selectOption('#Sexo', genero);
            check(await page.locator('#Sexo').inputValue() === genero, 'opción de género ' + genero);
        }
        await load('registro');
        for (const bloque of ['25', '125', '1225', '12225', '125678']) {
            await page.locator('#CarneParte3').fill(bloque);
            check(await page.locator('#CarneParte3').evaluate(input => input.checkValidity() && input.value.length >= 2 && input.value.length <= 6), 'rango continuo del carné ' + bloque.length);
        }
        await load('informe');
        check(await page.getByRole('link', { name: 'Descargar PDF' }).count() === 1, 'PDF de versión emitida visible para el destinatario');
        check(await page.locator('body').innerText().then(text => !text.includes('MOTIVO-CLINICO-PRIVADO')), 'vista del Director sin notas privadas');
        await page.screenshot({ path: path.join(fixtures, 'informe-vista-director.png'), fullPage: true });
        await load('privacidad');
        check(await page.getByRole('heading', { name: 'Mis solicitudes', exact: true }).count() === 1, 'centro de derechos disponible');
        await page.screenshot({ path: path.join(fixtures, 'privacidad-vista-estudiante.png'), fullPage: true });
        await load('reunion-preparar');
        await required('Enlace', true);
        await required('AccesoRestringidoRevisado', true);
        check(await page.locator('input[name="Revision"]').count() === 1, 'edición protegida frente a cambios simultáneos');
        await page.screenshot({ path: path.join(fixtures, 'reunion-preparar.png'), fullPage: true });
        await load('reunion-acceso');
        check(await page.locator('a[href*="/Reuniones/Entrar"]').count() === 1, 'entrada mediante verificación de ZUNI');
        check(await page.locator('body').innerText().then(text => !text.includes('meet.google.com/')), 'acceso sin publicar URL de la reunión');
        await page.screenshot({ path: path.join(fixtures, 'reunion-acceso.png'), fullPage: true });
        await load('director-color');
        const directorButton = page.locator('.btn-primary').first();
        const directorColor = await directorButton.evaluate(button => getComputedStyle(button).backgroundColor);
        check(directorColor === 'rgb(206, 162, 253)', 'Director usa el violeta del Psicólogo');
        await directorButton.hover();
        check(await directorButton.evaluate(button => getComputedStyle(button).backgroundColor) === directorColor, 'hover conserva el violeta');
        await page.screenshot({ path: path.join(fixtures, 'director-color.png'), fullPage: true });
        await load('psicologo-color');
        check(await page.locator('.dash-button').first().evaluate(button => getComputedStyle(button).backgroundColor) === directorColor, 'Psicólogo usa el mismo color que Director');
        await load('agenda-mejoras', ['agenda-dia.js']);
        await page.addStyleTag({ path: path.join(root, 'css/agenda-citas.css') });
        await page.addStyleTag({ path: path.join(root, 'css/modales.css') });
        const historico = page.locator('.agenda-day').filter({ has: page.locator('small') }).first();
        await historico.click();
        await page.waitForSelector('.modal.show');
        check(await page.locator('.modal.show a').filter({ hasText: 'Ver reporte compartido y reimprimir' }).count() > 0, 'ventana de fecha pasada ofrece reporte compartido');
        check(await page.locator('.modal-backdrop.show').evaluate(b => getComputedStyle(b).backdropFilter) === 'blur(6px)', 'desenfoque del fondo aplicado');
        await page.screenshot({ path: path.join(fixtures, 'agenda-pasada.png'), fullPage: true });
        await page.locator('.modal.show .modal-footer [data-bs-dismiss]').click();
        await page.waitForSelector('.modal.show', { state: 'hidden' });
        const form = page.locator('form[data-horario-dia]').nth(1);
        const modalId = await form.evaluate(f => f.closest('.modal').id);
        await page.locator(`[data-bs-target="#${modalId}"]`).click();
        check(await form.locator('[name="HoraFin"]').inputValue() === '' && !await form.locator('[name="HoraFin"]').evaluate(i => i.required), 'fin opcional sin valor persistido de 12:00');
        await form.locator('[name="HoraInicio"]').fill('09:00');
        await page.waitForFunction(() => [...document.querySelectorAll('[data-fin-estimado]')].some(e => e.textContent.includes('09:50')));
        check(await form.locator('[data-fin-estimado]').innerText().then(t => t.includes('09:50')), 'estimación de fin calculada con 50 minutos');
        await page.screenshot({ path: path.join(fixtures, 'agenda-fin-opcional.png'), fullPage: true });
        await load('reporte-compartido');
        check(await page.locator('body').innerText().then(t => !t.includes('SECRETO-NO-IMPRIMIR-110CT') && t.includes('RESULTADO-COMPARTIDO-110CT')), 'vista reimprimible sin notas privadas');
        await load('guia-publica');
        check(await page.locator('.guide-card').count() >= 4, 'guía pública explica funciones en tarjetas');
        await load('atencion', ['atencion-cita.js']);
        await page.addStyleTag({ path: path.join(root, 'css/modales.css') });
        await page.emulateMedia({ media: 'print' });
        check(await page.locator('[data-contenido-privado]').evaluate(e => getComputedStyle(e).display) === 'none', 'impresión del navegador oculta campos profesionales privados');
        await page.emulateMedia({ media: 'screen' });
        for(const tema of ['cita','seguimiento','bienestar','privacidad']) {
            await load('guia-'+tema);
            await page.addStyleTag({path:path.join(root,'css/site.css')});await page.addStyleTag({path:path.join(root,'css/guias-publicas.css')});
            const logo=fs.readFileSync(path.join(root,'images/zuni-logo-transparent.png')).toString('base64');await page.locator('img[src*="zuni-logo-transparent"]').evaluateAll((imgs,b)=>imgs.forEach(i=>i.src='data:image/png;base64,'+b),logo);
            check(await page.locator('.guide-card svg').count()>=4,'iconos en '+tema);check(await page.locator('.guide-place').count()>=4,'indicaciones de navegación en '+tema);
            await page.screenshot({path:path.join(fixtures,'guia-'+tema+'.png'),fullPage:true});
            await page.setViewportSize({width:390,height:844});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'guía móvil sin desborde '+tema);await page.setViewportSize({width:1280,height:900});
        }
        await load('home-footer');await page.addStyleTag({path:path.join(root,'css/site.css')});await page.addStyleTag({path:path.join(root,'css/guias-publicas.css')});
        const footerLogo=fs.readFileSync(path.join(root,'images/zuni-logo-transparent.png')).toString('base64');await page.locator('img[src*="zuni-logo-transparent"]').evaluateAll((imgs,b)=>imgs.forEach(i=>i.src='data:image/png;base64,'+b),footerLogo);
        await page.waitForFunction(()=>document.querySelector('.zuni-footer img').naturalWidth>0);
        check(await page.locator('.footer-grid section').count()===4,'pie con cuatro columnas');await page.locator('.zuni-footer').screenshot({path:path.join(fixtures,'home-footer.png')});
        await load('programar-cita');check(!await page.locator('#HoraFin').evaluate(i=>i.required),'fin aproximado opcional al programar');check(await page.locator('#PerfilEstudianteId option').count()>1,'estudiantes vinculados seleccionables');
        await load('notificaciones');check(await page.locator('body').innerText().then(t=>t.includes('Director')&&t.includes('Fecha actual de la cita')),'notificación separa emisor y fecha actual');
        for(const name of ['calendario-director','calendario-estudiante','catalogo-carreras','resumen-prueba']) {
            await load(name);await page.addStyleTag({path:path.join(root,'css/agenda-citas.css')});await page.addStyleTag({path:path.join(root,'css/modales.css')});
            const mark=fs.readFileSync(path.join(root,'images/zuni-logo-transparent.png')).toString('base64');await page.locator('img[src*="zuni-logo-transparent"]').evaluateAll((imgs,b)=>imgs.forEach(i=>i.src='data:image/png;base64,'+b),mark);
            if(name.startsWith('calendario')) {check(await page.locator('.agenda-day').count()>=28,'mes completo '+name);check(await page.locator('button').filter({hasText:'Solicitar cita'}).count()===0,'consulta sin reserva no autorizada '+name);}
            if(name==='catalogo-carreras')check(await page.locator('#Archivo').getAttribute('accept')==='.txt,.csv','catálogo admite listado TXT/CSV');
            if(name==='resumen-prueba'){check(await page.locator('meter').count()===5,'resumen de cinco temas exploratorios');await page.emulateMedia({media:'print'});check(await page.locator('[data-contenido-privado]').evaluate(e=>getComputedStyle(e).display)==='none','resumen individual oculto al imprimir');await page.emulateMedia({media:'screen'});}
            await page.screenshot({path:path.join(fixtures,name+'.png'),fullPage:true});await page.setViewportSize({width:390,height:844});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desborde móvil '+name);await page.setViewportSize({width:1280,height:900});
        }
        await load('notificacion-revision');
        const revisionLogo=fs.readFileSync(path.join(root,'images/zuni-logo-transparent.png')).toString('base64');await page.locator('img[src*="zuni-logo-transparent"]').evaluateAll((imgs,b)=>imgs.forEach(i=>i.src='data:image/png;base64,'+b),revisionLogo);
        check(await page.locator('.notification-data dt').count()>=8,'datos de la notificación separados por tema');
        check(await page.locator('.notification-data').innerText().then(t=>t.includes('Estudiante ficticio almuerzo')),'notificación identifica al alumno');
        check(await page.locator('.notification-actions').evaluate(e=>parseFloat(getComputedStyle(e).gap)>=16),'separación de botones de notificación');
        check(await page.locator('.notification-actions a').getAttribute('href').then(h=>h.includes('citaId=')),'enlace dirige a una cita concreta');
        await page.screenshot({path:path.join(fixtures,'notificacion-revision.png'),fullPage:true});
        await load('agenda-revision',['agenda-contexto.js','agenda-dia.js']);await page.addStyleTag({path:path.join(root,'css/agenda-citas.css')});await page.addStyleTag({path:path.join(root,'css/modales.css')});
        await page.locator('img[src*="zuni-logo-transparent"]').evaluateAll((imgs,b)=>imgs.forEach(i=>i.src='data:image/png;base64,'+b),revisionLogo);
        await page.locator('.agenda-selected-appointment').waitFor({state:'visible'});
        check(await page.locator('.agenda-modal.show').count()===1,'aviso abre automáticamente la ventana del día');
        check(await page.locator('.agenda-modal.show .agenda-selected-appointment').count()===1,'cita solicitada resaltada dentro de su día');
        check(await page.locator('.agenda-day.is-pending').count()>0,'estado amarillo pendiente de revisar');
        check(await page.locator('.agenda-modal.show').innerText().then(t=>t.includes('Aceptar cita')&&t.includes('No puedo atender')),'propuesta se responde en la ventana');
        check(await page.locator('.dash-nav a.is-active').count()===1,'solo una pestaña activa del psicólogo');
        check(await page.locator('.dash-nav-group[open]').count()>=1,'grupo del contexto expandido');
        await page.screenshot({path:path.join(fixtures,'agenda-revision.png'),fullPage:false});
        for(const name of ['solicitud-estudiante','solicitudes-psicologo','expedientes-psicologo']){
            await load(name,['agenda-contexto.js']);check(await page.locator('.dash-nav a.is-active').count()===1,'submenú único '+name);await page.setViewportSize({width:390,height:844});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desborde móvil '+name);await page.setViewportSize({width:1280,height:900});
        }
        await load('demostracion-estudiante',['agenda-contexto.js']);
        check(await page.locator('select[name^="Respuestas["]').count()===15,'demo tiene 15 respuestas');
        check(await page.locator('.dash-nav a.is-active').count()===1,'demo con selección lateral exclusiva');
        for(const select of await page.locator('select[name^="Respuestas["]').all()) await select.selectOption('5');
        check(await page.locator('form[action="/Demostracion"]').evaluate(f=>!f.checkValidity()),'demo exige confirmación voluntaria');
        await page.locator('#Consentimiento').check();
        check(await page.locator('form[action="/Demostracion"]').evaluate(f=>f.checkValidity()),'demo permite finalizar 15 respuestas válidas');
        await page.setViewportSize({width:390,height:844});
        check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'demo sin desborde móvil');
        await page.screenshot({path:path.join(fixtures,'demostracion-estudiante.png'),fullPage:false});
        await page.setViewportSize({width:1280,height:900});
        await load('login'); await page.addStyleTag({path:path.join(root,'css/site.css')});
        check(await page.locator('.auth-brand .auth-logo').count()===0,'logo retirado solo del panel lateral del login');
        check(await page.locator('.auth-brand-copy').evaluate(e=>{const a=e.closest('aside').getBoundingClientRect(),b=e.getBoundingClientRect();return Math.abs((a.y+a.height/2)-(b.y+b.height/2))<3;}),'texto de acceso centrado verticalmente');
        await page.screenshot({path:path.join(fixtures,'login-sin-logo-lateral.png'),fullPage:false});
        for(const name of ['home','login','admin','director','psicologo','estudiante']) {
            await load('idioma-'+name,['agenda-contexto.js']);
            if(name==='home'||name==='login') {await page.addStyleTag({path:path.join(root,'css/site.css')});await page.addStyleTag({path:path.join(root,'css/guias-publicas.css')});}
            check(await page.locator('html').getAttribute('lang')==='en','idioma del documento '+name);
            const selector=page.locator('.zuni-language');
            check(await selector.count()===1 && await selector.isVisible(),'selector único visible '+name);
            check(await selector.locator('[value="en"]').getAttribute('aria-pressed')==='true','estado EN accesible '+name);
            const submitted=await selector.evaluate(f=>Object.fromEntries(new FormData(f,f.querySelector('[value="es"]'))));
            check(submitted.idioma==='es' && submitted.__RequestVerificationToken && submitted.volver.startsWith('/'),'cambio nativo a español con protección '+name);
            await page.setViewportSize({width:390,height:844});await page.evaluate(()=>scrollTo(0,0));
            check(await selector.isVisible(),'selector disponible sin abrir menú móvil '+name);
            check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desborde en inglés '+name+' '+JSON.stringify(await page.evaluate(()=>[...document.querySelectorAll('body *')].filter(e=>{const r=e.getBoundingClientRect();return r.right>innerWidth+1&&r.width>0&&!e.closest('.offcanvas,.modal,.table-responsive');}).slice(0,6).map(e=>[e.tagName,e.className,Math.round(e.getBoundingClientRect().width)]))));
            await page.screenshot({path:path.join(fixtures,'idioma-'+name+'-movil.png'),fullPage:false});
            await page.setViewportSize({width:1280,height:900});await page.evaluate(()=>scrollTo(0,0));
            await page.screenshot({path:path.join(fixtures,'idioma-'+name+'.png'),fullPage:false});
        }
        await load('idioma-login',['site.js']);await page.addStyleTag({path:path.join(root,'css/site.css')});
        check(await page.locator('label[for="RememberMe"]').innerText().then(t=>t.includes('Remember me')),'opción recordar traducida');
        check(await page.locator('#Email').getAttribute('data-val-required').then(t=>t.includes('Enter your email')),'mensaje requerido del validador en inglés');
        await page.locator('#Email').fill('correo-invalido');
        await page.locator('#Email').evaluate(e=>e.reportValidity());
        check(await page.locator('#Email').evaluate(e=>e.validationMessage.includes('Enter a valid email')),'mensaje nativo de correo en inglés');
        await page.locator('#Email').fill('alumno@example.com');
        check(await page.locator('#Email').evaluate(e=>!e.validity.customError),'validación se limpia al editar');
        await load('idioma-alta');
        check(await page.locator('#Rol option[value="Estudiante"]').innerText()==='Student','opción visible traducida conserva valor Estudiante');
        await page.selectOption('#Rol','Estudiante');
        check(await page.locator('#carne-estudiante').isVisible(),'dependencia de rol funciona en inglés');
        const touchContext = await browser.newContext({hasTouch:true,viewport:{width:1280,height:600}});
        const touch = await touchContext.newPage();
        const cssNav=['lib/bootstrap/dist/css/bootstrap.min.css','css/dashboard.css','css/agenda-contexto.css'].map(p=>fs.readFileSync(path.join(root,p),'utf8')).join('\n');
        const navJs=fs.readFileSync(path.join(root,'js/agenda-contexto.js'),'utf8');
        await touch.route('**/*',async route=> {
            if(!route.request().isNavigationRequest()) return route.abort();
            const pathname=new URL(route.request().url()).pathname;
            const fixture=pathname.includes('Catedraticos')?'admin-retirados':pathname.includes('Psicologo')?'solicitudes-psicologo':'admin-tablero';
            let html=fs.readFileSync(path.join(fixtures,fixture+'.html'),'utf8').replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi,'').replace(/<link\b[^>]*>/gi,'');
            html=html.replace('</head>','<style>'+cssNav+'</style></head>').replace('</body>','<script>'+navJs+'</script></body>');
            await route.fulfill({contentType:'text/html',body:html});
        });
        await touch.goto('http://zuni.test/Administrador');
        check(await touch.locator('a.dash-summary-link').count()>=6,'tarjetas completas son enlaces sin enlaces anidados');
        check(await touch.locator('a.dash-summary-link a').count()===0,'sin enlaces anidados');
        const studentCard=touch.locator('a[data-rol="Estudiante"]');const cardBox=await studentCard.boundingBox();
        await Promise.all([touch.waitForURL('**/Administrador/Estudiantes'),touch.touchscreen.tap(cardBox.x+12,cardBox.y+12)]);
        check(touch.url().endsWith('/Administrador/Estudiantes'),'touch en borde de tarjeta navega');
        await touch.goto('http://zuni.test/Administrador');
        await touch.locator('a[data-rol="Director"]').click();
        check(touch.url().endsWith('/Administrador/Directores'),'clic en tarjeta navega');
        await touch.goto('http://zuni.test/Administrador');
        check(await touch.locator('#dashboard-sidebar').evaluate(e=>getComputedStyle(e).scrollbarWidth==='none'&&e.scrollHeight>e.clientHeight),'barra oculta conserva contenido desplazable');
        await touch.locator('#dashboard-sidebar').evaluate(e=>e.scrollTop=140);
        await touch.waitForTimeout(100);
        const scrollBefore=await touch.locator('#dashboard-sidebar').evaluate(e=>e.scrollTop);
        await touch.locator('.dash-nav a[href="/Administrador/Catedraticos"]').click();
        await touch.waitForTimeout(100);
        check(await touch.locator('#dashboard-sidebar').evaluate((e,b)=>Math.abs(e.scrollTop-b)<2,scrollBefore),'posición lateral conservada entre pestañas');
        check(await touch.locator('.dash-nav a.is-active').count()===1 && await touch.locator('.dash-nav a.is-active').getAttribute('href')==='/Administrador/Catedraticos','selección exclusiva roles retirados');
        await touch.goto('http://zuni.test/Psicologo/Solicitudes');
        const summary=touch.locator('.dash-nav-group summary').first();
        const indicator=()=>summary.evaluate(e=> {const s=getComputedStyle(e,'::after');return [s.content,s.right,s.top,s.width,s.height];});
        const initialIndicator=await indicator();await summary.click();
        check(JSON.stringify(initialIndicator)===JSON.stringify(await indicator()),'V conserva posición y tamaño al expandir');
        check(initialIndicator[0]==='""','V dibujada sin glifo genérico');
        await touch.screenshot({path:path.join(fixtures,'sidebar-admin-mejorada.png'),fullPage:false});
        await touchContext.close();
        check(errors.length === 0, 'sin errores JavaScript: ' + errors.join(', '));
        console.log(`OK: ${checks} comprobaciones de formularios en navegador.`);
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
