const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');
let checks=0;const check=(value,message)=>{assert.ok(value,message);checks++;};
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({reducedMotion:'reduce'});let unlocks=0;
 await page.route('http://zuni.test/**',async route=>{
  const u=new URL(route.request().url());
  if(u.pathname==='/PinProfesional/Desbloquear'){
   unlocks++;const pin=route.request().postData()||'';
   // A real server-rendered fragment from the offline test, never sent before a valid POST.
   if(!pin.includes('0384'))return route.fulfill({status:409,contentType:'application/json',body:JSON.stringify({error:'PIN incorrecto'})});
   return route.fulfill({headers:{'Content-Type':'text/html','X-Zuni-Private-Note':'1','Cache-Control':'no-store'},body:fs.readFileSync(path.join(dir,'pin-desbloqueado.html'),'utf8')});
  }
  if(route.request().isNavigationRequest()){
   const name=u.pathname.includes('Recuperar')?'pin-recuperar':u.pathname.includes('EditarCentro')?'v1-contrato-es':u.pathname.includes('Guias')?'guias-Psicologo':u.pathname.includes('/Director')?'idioma-director':'pin-reporte';
   return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(dir,name+'.html'),'utf8')});
  }
  const file=path.resolve(root,u.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png','.ico':'image/x-icon'}[path.extname(file)];
  return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();
 });
 for(const width of [390,1366])for(const theme of ['light','dark']){
  await page.setViewportSize({width,height:900});await page.goto('http://zuni.test/Atenciones/Reporte');await page.evaluate(t=>document.documentElement.dataset.theme=t,theme);
  check(!await page.content().then(x=>x.includes('SECRETO-PRIVADO-PIN')),'sin texto privado en DOM bloqueado '+width+theme);
  check(await page.locator('.pin-preview').evaluate(e=>getComputedStyle(e).filter.includes('blur')),'placeholder borroso '+width+theme);
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'reporte sin desbordamiento '+width+theme);
  await page.locator('[name=pin]').fill('9999');await page.locator('[data-pin-unlock] button').click();await page.locator('[data-pin-error]').filter({hasText:'PIN incorrecto'}).waitFor();
  check(!await page.content().then(x=>x.includes('SECRETO-PRIVADO-PIN')),'PIN incorrecto no revela '+width+theme);
  await page.locator('[name=pin]').fill('0384');await page.locator('[data-pin-unlock] button').click();await page.locator('[data-private-revealed]').waitFor();
  check(await page.locator('[data-private-revealed]').innerText().then(x=>x.includes('SECRETO-PRIVADO-PIN')),'revela tras POST válido '+width+theme);
  await page.locator('[data-pin-lock]').click();
  check(!await page.content().then(x=>x.includes('SECRETO-PRIVADO-PIN'))&&await page.locator('[name=pin]').inputValue()==='','bloquear elimina la nota y el PIN '+width+theme);
  if(width===390&&theme==='dark')await page.locator('[data-private-note]').screenshot({path:path.join(dir,'pin-movil-oscuro.png')});
 }
 await page.goto('http://zuni.test/Atenciones/Reporte');await page.clock.install();
 await page.locator('[name=pin]').fill('0384');await page.locator('[data-pin-unlock] button').click();await page.locator('[data-private-revealed]').waitFor();
 await page.clock.fastForward(120001);check(await page.locator('[data-private-revealed]').count()===0,'notas eliminadas tras dos minutos');
 await page.goto('http://zuni.test/PinProfesional/Recuperar#usuario=ficticio&token=prueba-no-real');
 check(await page.locator('[name=Token]').inputValue()==='prueba-no-real'&&await page.locator('[name=Usuario]').inputValue()==='ficticio','fragmento carga el formulario de recuperación');
 check(await page.evaluate(()=>location.hash===''),'token retirado de la dirección');
 await page.goto('http://zuni.test/Atenciones/Reporte');await page.locator('[name=pin]').fill('0384');
 await page.locator('[data-pin-unlock]').evaluate(form=>{form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));});
 await page.locator('[data-private-revealed]').waitFor();
 check(unlocks===10,'doble envío solo realiza una consulta');
 await page.evaluate(()=>{Object.defineProperty(document,'hidden',{get:()=>true,configurable:true});document.dispatchEvent(new Event('visibilitychange'));});
 check(!await page.content().then(x=>x.includes('SECRETO-PRIVADO-PIN')),'ocultar pestaña elimina las notas');
 await page.goto('http://zuni.test/SuperAdmin/EditarCentro');
 const today=await page.locator('[data-contract-plan]').getAttribute('data-today'),prices=JSON.parse(await page.locator('[data-contract-plan]').getAttribute('data-prices'));
 for(const months of [1,3,6,12]){
  await page.locator('[name=MesesPlan]').selectOption(String(months));
  check(Number(await page.locator('[name=ImporteGtq]').inputValue())===prices[months],'precio cambia con el plan '+months);
  const expected=await page.evaluate(({today,months})=>{const [y,m,d]=today.split('-').map(Number),first=new Date(Date.UTC(y,m-1+months,1)),last=new Date(Date.UTC(first.getUTCFullYear(),first.getUTCMonth()+1,0)).getUTCDate(),end=new Date(Date.UTC(first.getUTCFullYear(),first.getUTCMonth(),Math.min(d,last)));end.setUTCDate(end.getUTCDate()-1);return end.toISOString().slice(0,10);},{today,months});
  check(await page.locator('[name=Inicio]').inputValue()===today&&await page.locator('[name=Fin]').inputValue()===expected,'vigencia automática '+months);
 }
 check(await page.locator('[name=Fin]').getAttribute('readonly')!==null&&await page.locator('[name=ImporteGtq]').getAttribute('readonly')!==null,'fecha final e importe no editables');
 await page.setViewportSize({width:390,height:900});await page.evaluate(()=>document.documentElement.dataset.theme='dark');await page.locator('[data-contract-plan]').screenshot({path:path.join(dir,'contrato-movil-oscuro.png')});
 await page.goto('http://zuni.test/Guias');
 check(await page.locator('.guide-place').evaluateAll(es=>es.every(e=>e.querySelector('a[href]'))),'todas las guías del perfil tienen acceso');
 await page.locator('.guide-place a[href="#zuni-theme-control"]').first().click();
 check(await page.locator('.zuni-theme-toggle').evaluate(e=>e===document.activeElement),'acceso directo enfoca el control del tema');
 for(const width of [390,1366]){
  await page.setViewportSize({width,height:900});await page.goto('http://zuni.test/Director');
  check(await page.locator('.dash-welcome').evaluate(e=>e.nextElementSibling.getBoundingClientRect().top-e.getBoundingClientRect().bottom>=15),'separación entre bienvenida y propuesta '+width);
 }
 check(unlocks===10,'peticiones de desbloqueo controladas');
 console.log('OK: '+checks+' comprobaciones visuales y funcionales de PIN, recuperación, contratos y accesos.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
