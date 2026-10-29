const {chromium}=require('playwright');const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');let checks=0;function check(v,m){assert.ok(v,m);checks++;}
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const context=await browser.newContext({viewport:{width:1400,height:900},reducedMotion:'reduce'});
 const pages={'/alumno':'panel-alumno','/otro':'panel-otro-alumno','/admin':'panel-admin','/psicologo':'panel-psicologo','/demo':'demo-ingles','/colores':'colores-alumno','/':'home-footer'};
 await context.route('**/*',async route=>{const u=new URL(route.request().url());if(u.origin!=='http://zuni.test')return route.abort();if(route.request().isNavigationRequest()){const f=pages[u.pathname];return f?route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(dir,f+'.html'),'utf8')}):route.abort();}const file=path.resolve(root,u.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png'}[path.extname(file)];return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();});
 const page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://zuni.test/alumno');const scope=await page.locator('html').getAttribute('data-view-scope');
 await page.locator('.zuni-theme-toggle').click();check(await page.locator('html').getAttribute('data-theme')==='dark','alumno elige oscuro');
 const other=await context.newPage();await other.goto('http://zuni.test/admin');check(await other.locator('html').getAttribute('data-theme')==='light','Administrador no hereda tema del alumno');
 await other.locator('.zuni-theme-toggle').click();await other.locator('.zuni-theme-toggle').click();
 check(await page.locator('html').getAttribute('data-theme')==='dark','evento storage de otra cuenta no cambia al alumno');
 await page.reload();check(await page.locator('html').getAttribute('data-theme')==='dark','tema personal conservado al recargar');
 check(await page.evaluate(s=>localStorage.getItem('zuni-theme:'+s)==='dark',scope),'clave incluye cuenta y rol');
 const circles=await page.locator('.dash-bottom-controls .dash-round-control').evaluateAll(es=>es.map(e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,w:r.width,h:r.height,title:e.getAttribute('aria-label')};}));
 check(circles.length===3&&circles.every(r=>r.w===48&&r.h===48),'tres círculos iguales');check(circles.every(r=>r.x===circles[0].x)&&circles[1].y>circles[0].y&&circles[2].y>circles[1].y,'círculos verticales');check((await page.locator('.dash-bottom-controls .dash-bottom-action').allTextContents()).every(t=>t.trim().length>0),'controles con textos visibles');
 check(await page.locator('.dash-logo-glass').count()===2,'dos círculos de cristal del logo');
 check(await page.locator('.dash-bottom-controls a').nth(1).getAttribute('href')==='/','control home público');
 check(await page.locator('.dash-bottom-controls form').getAttribute('method')==='post','logout conserva POST');
 for(const url of ['/alumno','/admin','/psicologo']){await page.goto('http://zuni.test'+url);const account=page.locator('.dash-nav-group').filter({has:page.locator('a[href="/MiCuenta/Editar"]')});check(await account.count()===1,'Mi cuenta tiene submenús '+url);check(await page.locator('.dash-nav a.is-active').count()===1,'una selección '+url);}
 await page.goto('http://zuni.test/colores');check(await page.locator('[data-color-proveedor]').count()===5,'alumno dispone de colores');await page.locator('[data-color-proveedor="#F9DB5C"]').click();check(await page.locator('#ColorPrincipal').inputValue()==='#f9db5c','vista previa de color personal');
 await page.goto('http://zuni.test/demo');
 check((await page.locator('main').innerText()).includes('There are no right or wrong answers'),'instrucciones traducidas');
 check(!(await page.locator('main').innerText()).includes('No existen respuestas correctas'),'sin mezcla de instrucciones');
 check(await page.locator('[data-question]:visible').count()===3,'tres preguntas por página');
 for(const width of [1400,390]){
  await page.setViewportSize({width,height:900});await page.evaluate(()=>scrollTo(0,0));
  await page.waitForFunction(()=>!document.querySelector('.page-scroll-down').hidden);
  check(await page.locator('.page-scroll-down').isVisible(),'V disponible '+width);
  if(width===390)await page.screenshot({path:path.join(dir,'panel-v-ingles.png'),fullPage:false});
  await page.locator('.page-scroll-down').click();await page.waitForFunction(()=>scrollY>100);
  check(await page.locator('.dashboard-topbar').evaluate(e=>Math.abs(e.getBoundingClientRect().top)<1),'barra superior fija '+width);
  await page.evaluate(()=>scrollTo(0,document.documentElement.scrollHeight));await page.waitForFunction(()=>document.querySelector('.page-scroll-down').hidden);check(true,'V se oculta al final '+width);
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desborde en test '+width);
 }
 for(let block=0;block<5;block++){
  for(const q of await page.locator('[data-question]:visible').all())await q.locator('input[value="5"]').check();
  if(block<4)await page.locator('[data-test-next]').click();
 }
 check(await page.locator('[data-test-progress]').evaluate(e=>e.value)===100,'recorrido de quince respuestas con cabecera fija');
 await page.screenshot({path:path.join(dir,'panel-test-ingles-movil.png'),fullPage:false});
 await page.setViewportSize({width:1400,height:1000});await page.goto('http://zuni.test/alumno');await page.screenshot({path:path.join(dir,'panel-controles-personales.png'),fullPage:false});
 check(errors.length===0,'sin errores JS: '+errors.join('; '));console.log(`OK: ${checks} comprobaciones visuales de aislamiento, controles y test.`);
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
