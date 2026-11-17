const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');let checks=0;
function check(value,message){assert.ok(value,message);checks++;}
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({reducedMotion:'reduce'});let cantidad=2;
 await page.route('http://zuni.test/**',route=>{
  const u=new URL(route.request().url());
  if(u.pathname==='/CitasProgramadas/Pendientes')return route.fulfill({contentType:'application/json',body:JSON.stringify({cantidad})});
  if(route.request().isNavigationRequest())return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(dir,u.pathname.slice(1)+'.html'),'utf8')});
  const file=path.resolve(root,u.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png','.svg':'image/svg+xml','.woff2':'font/woff2','.ico':'image/x-icon'}[path.extname(file)];
  return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();
 });
 for(const width of [360,390,768,1366]){
  await page.setViewportSize({width,height:900});
  for(const name of ['deniscorp','academy','2fa','2fa-preparado','ausencia']){
   await page.goto('http://zuni.test/'+name);await page.evaluate(()=>document.fonts.ready);
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),name+' sin desbordamiento '+width);
   for(const button of await page.locator('main a.corp-button,section button,section .dash-button').all()){
    const box=await button.boundingBox();if(box)check(box.width<=width&&box.height>=40,'acciones visibles y cómodas '+name+width);
   }
   if(width===390||width===1366)await page.screenshot({path:path.join(dir,name+'-'+width+'.png'),fullPage:true});
  }
  await page.goto('http://zuni.test/deniscorp');
  check(await page.locator('.product-card').count()===3,'tres programas '+width);
  const cards=await page.locator('.product-card').evaluateAll(es=>es.map(e=>({x:e.getBoundingClientRect().x,y:e.getBoundingClientRect().y})));
  check(width<700?cards[1].y>cards[0].y:cards.every(c=>c.y===cards[0].y),'tarjetas adaptadas al ancho '+width);
  check(await page.locator('.product-login').getAttribute('href')==='/Cuenta/IniciarSesion','login existente conservado '+width);
  await page.locator('.corp-hero .corp-button').focus();check(await page.locator('.corp-hero .corp-button').evaluate(e=>getComputedStyle(e).outlineStyle!=='none'),'foco de teclado visible '+width);
 }
 await page.clock.install();await page.goto('http://zuni.test/ausencia');await page.clock.fastForward(45001);
 await page.waitForFunction(()=>document.querySelector('[data-avisos-citas]')?.dataset.cantidad==='2');
 check(await page.locator('[data-avisos-cantidad]').isVisible(),'aviso nuevo visible sin recargar página');
 cantidad=0;await page.clock.fastForward(45001);await page.waitForFunction(()=>document.querySelector('[data-avisos-citas]')?.dataset.cantidad==='0');
 check(await page.locator('[data-avisos-cantidad]').isHidden(),'contador se oculta al no quedar avisos');
 console.log('OK: '+checks+' comprobaciones de presentación Denis Corp, 2FA, ausencias y avisos automáticos.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
