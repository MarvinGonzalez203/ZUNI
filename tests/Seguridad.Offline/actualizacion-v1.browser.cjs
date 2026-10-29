const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');let checks=0;
const check=(ok,label)=>{assert.ok(ok,label);checks++;};
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const context=await browser.newContext({viewport:{width:390,height:844},reducedMotion:'reduce'});let lang='es',failNext=false,holdGuide=false;
 await context.route('**/*',async route=>{
  const request=route.request(),url=new URL(request.url());if(url.origin!=='http://zuni.test')return route.abort();
  if(url.pathname==='/Idioma/Cambiar'){
   const body=request.postData()||'',field=name=>body.match(new RegExp('name="'+name+'"\\r\\n\\r\\n([^\\r]+)'))?.[1]||new URLSearchParams(body).get(name);
   lang=field('idioma');return route.fulfill({contentType:'application/json',body:JSON.stringify({idioma:lang,volver:field('volver')})});
  }
  const name=url.pathname==='/Home/Guia'?(url.searchParams.get('tema')==='cita'?'v1-cita':'v1-guia'):
   ({'/Cuenta/IniciarSesion':'v1-login','/SuperAdmin':'v1-super','/SuperAdmin/Guias':'v1-guias','/SuperAdmin/EditarCentro':'v1-contrato','/Privacidad':'v1-proteccion','/':'v1-home'})[url.pathname];
  if(name){
   if(failNext&&!request.isNavigationRequest()){failNext=false;return route.fulfill({status:503,body:'Temporary failure'});}
   const html=fs.readFileSync(path.join(dir,name+'-'+lang+'.html'),'utf8');
   if(holdGuide&&url.pathname==='/Home/Guia'&&url.searchParams.get('tema')==='cita'&&!request.isNavigationRequest())await new Promise(r=>setTimeout(r,180));
   try{return await route.fulfill({contentType:'text/html',body:html});}catch{return;}
  }
  const asset=path.resolve(root,url.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png','.ico':'image/x-icon'}[path.extname(asset)];
  return type&&asset.startsWith(root+path.sep)&&fs.existsSync(asset)?route.fulfill({contentType:type,body:fs.readFileSync(asset)}):route.abort();
 });
 const page=await context.newPage(),errors=[];let navigations=0;page.setDefaultTimeout(15000);
 page.on('pageerror',e=>errors.push(e.message));page.on('framenavigated',f=>{if(f===page.mainFrame())navigations++;});
 const toggle=async()=>{
  const box=await page.locator('.zuni-language button').boundingBox();check(box&&box.y>=0,'idioma accesible sin volver al inicio');
  await page.mouse.click(box.x+box.width/2,box.y+box.height/2);
  await page.waitForFunction(()=>!window.ZuniLanguageChanging);
 };
 const expectedGuide=language=>page.evaluate(html=>[...new DOMParser().parseFromString(html,'text/html').querySelectorAll('.guide-card h2,.guide-card p')].map(e=>e.textContent.trim()),fs.readFileSync(path.join(dir,'v1-guia-'+language+'.html'),'utf8'));
 for(const width of [390,320,1366]){
  await page.setViewportSize({width,height:844});lang='es';await page.goto('http://zuni.test/Cuenta/IniciarSesion');
  check(await page.locator('.auth-brand h2').isVisible(),'lema de acceso visible '+width);
  check(await page.locator('.auth-mobile-logo').count()===0,'sin logo repetido en acceso '+width);
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'acceso sin desborde '+width);
  await page.locator('.zuni-theme-toggle').click();
  if(width===390)await page.screenshot({path:path.join(dir,'acceso-movil-oscuro.png'),fullPage:false});
  lang='es';await page.goto('http://zuni.test/Home/Guia?tema=privacidad');
  await page.evaluate(()=>scrollTo({top:500,behavior:'instant'}));
  const baseline=navigations;
  for(let i=0;i<6;i++){
   const before=await page.evaluate(()=>scrollY);await toggle();const desired=i%2===0?'en':'es';
   await page.waitForFunction(l=>document.documentElement.lang===l,desired);
   check(Math.abs((await page.evaluate(()=>scrollY))-before)<3,'posición exacta conservada '+width+' cambio '+i);
   const actual=await page.locator('.guide-card h2,.guide-card p').allTextContents();
   assert.deepEqual(actual.map(s=>s.trim()),await expectedGuide(desired),'guía completa sin mezcla de idiomas');checks++;
  }
  check(navigations===baseline,'seis cambios sin navegar '+width);
  check(await page.locator('.page-scroll-down').isVisible(),'V persiste a media lectura '+width);
  const before=await page.evaluate(()=>scrollY);await page.locator('.page-scroll-down').click();await page.waitForFunction(y=>scrollY>y+50,before);
  check(await page.locator('.page-scroll-down').isVisible(),'V continúa mientras hay contenido '+width);
  check(await page.locator('.page-scroll-top').isVisible(),'subir disponible '+width);
  await page.locator('.page-scroll-top').click();await page.waitForFunction(()=>scrollY===0);check(true,'subir funciona '+width);
  await page.evaluate(()=>scrollTo({top:document.documentElement.scrollHeight,behavior:'instant'}));await page.locator('.page-scroll-down').waitFor({state:'hidden'});check(true,'V desaparece solo al final '+width);
 }
 await page.setViewportSize({width:390,height:844});lang='es';await page.goto('http://zuni.test/SuperAdmin/EditarCentro?ejemplo=true');
 await page.locator('#Nombre').fill('Borrador sin guardar');await page.locator('#Fin').fill('2027-01-31');
 await page.locator('[data-extend-months="1"]').click();check(await page.locator('#Fin').inputValue()==='2027-02-28','ampliación respeta fin de mes');
 await page.evaluate(()=>{document.querySelectorAll('.dash-nav-group').forEach(e=>e.open=true);scrollTo({top:300,behavior:'instant'});});
 const position=await page.evaluate(()=>scrollY);await toggle();await page.waitForFunction(()=>document.documentElement.lang==='en');
 check(await page.locator('#Nombre').inputValue()==='Borrador sin guardar'&&await page.locator('#Fin').inputValue()==='2027-02-28','borrador comercial conservado');
 check(Math.abs((await page.evaluate(()=>scrollY))-position)<3,'contrato conserva posición');
 check((await page.locator('label[for="Nombre"]').innerText()).includes('Educational institution'),'etiquetas con marcadores dinámicos traducidas');
 check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'contrato móvil sin desborde');
 await page.screenshot({path:path.join(dir,'contrato-movil-ingles.png'),fullPage:true});
 lang='es';await page.goto('http://zuni.test/SuperAdmin');
 check(await page.locator('.commercial-card').count()>=4,'tablero tiene tarjetas de funciones');
 check(await page.locator('.system-version').textContent()==='ZUNI · v'+fs.readFileSync(path.join(root,'../Zuni.csproj'),'utf8').match(/<Version>([^<]+)/)[1],'versión en menú lateral móvil cerrado');
 check(await page.locator('.release-control').count()===1,'campana de versiones dentro del sistema');
 check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'tablero móvil sin desborde');
 await page.screenshot({path:path.join(dir,'superadmin-movil.png'),fullPage:true});
 await page.setViewportSize({width:1366,height:900});await page.screenshot({path:path.join(dir,'superadmin-escritorio.png'),fullPage:true});
 lang='es';await page.goto('http://zuni.test/Home/Guia?tema=privacidad');holdGuide=true;
 await page.evaluate(()=>document.querySelector('.guide-tabs a[href*="tema=cita"]').click());
 await toggle();await page.waitForFunction(()=>document.documentElement.lang==='en');
 await page.waitForTimeout(220);check(page.url().includes('tema=privacidad'),'guía pendiente se cancela al cambiar idioma');
 assert.deepEqual((await page.locator('.guide-card h2,.guide-card p').allTextContents()).map(s=>s.trim()),await expectedGuide('en'));checks++;
 failNext=true;await toggle();await page.locator('.language-error').waitFor();
 check(lang==='en'&&await page.locator('html').getAttribute('lang')==='en','fallo de red revierte cookie y conserva idioma visible');
 check(errors.length===0,'sin errores de JavaScript: '+errors.join(';'));
 console.log(`OK: ${checks} comprobaciones de móvil, idioma repetido, carreras de navegación y desplazamiento.`);
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
