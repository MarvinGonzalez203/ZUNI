// Uses the genuine HTML produced by the offline portal fixture, with its current CSS/scripts.
const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');
let checks=0;const check=(v,m)=>{assert.ok(v,m);checks++;};
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({reducedMotion:'reduce'});
 await page.route('http://zuni.test/**',route=>{
  const u=new URL(route.request().url());
  if(route.request().isNavigationRequest())return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(dir,'agenda-mejoras.html'),'utf8')});
  const file=path.resolve(root,u.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png','.ico':'image/x-icon'}[path.extname(file)];
  return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();
 });
 for(const width of [320,390,600,1366])for(const theme of ['light','dark']){
  await page.setViewportSize({width,height:900});await page.goto('http://zuni.test/Psicologo/Agenda');
  await page.evaluate(t=>{document.documentElement.dataset.theme=t;document.documentElement.style.setProperty('--institucion-color','#ffda44');document.documentElement.style.setProperty('--institucion-texto','#000000');},theme);
  const cells=page.locator('.agenda-day');
  const sizes=await cells.evaluateAll(es=>es.map(e=>e.getBoundingClientRect().height));
  check(new Set(sizes).size===1,'todos los días tienen la misma altura '+width+' '+theme);
  const label=cells.filter({has:page.locator('.agenda-day-description')}).first().locator('.agenda-day-description');
  await label.evaluate(e=>e.textContent='Pendiente de revisar 123 citas con un texto deliberadamente extenso para verificar el límite');
  check(await label.evaluate(e=>{const s=getComputedStyle(e);return s.webkitLineClamp==='2'&&e.clientHeight<=parseFloat(s.lineHeight)*2+1&&e.scrollHeight>e.clientHeight;}),'dos líneas y recorte de texto extenso '+width+' '+theme);
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desbordamiento horizontal '+width+' '+theme);
  check(await page.locator('.agenda-month').evaluate(e=>Array.from(e.children).every(c=>c.scrollWidth<=c.clientWidth+1)),'título y navegación del mes sin superponerse '+width+' '+theme);
  const summary=page.locator('.agenda-card details>summary').first();
  const before=await summary.evaluate(e=>({marker:getComputedStyle(e).listStyleType,border:getComputedStyle(e,'::after').borderBottomWidth,transform:getComputedStyle(e,'::after').transform}));
  check(before.marker==='none'&&parseFloat(before.border)>0,'chevrón abierto en lugar del marcador nativo '+width+' '+theme);
  await summary.click();
  check(await summary.evaluate((e,t)=>getComputedStyle(e,'::after').transform!==t,before.transform),'chevrón cambia al desplegar '+width+' '+theme);
  const group=page.locator('.agenda-day-actions').filter({has:page.locator('.btn-outline-primary')}).first();
  // Combine real links from the fixture to exercise the two-action mobile case.
  await group.evaluate(e=>{if(!e.querySelector('.dash-button'))e.append(document.querySelector('.agenda-day-actions .dash-button').cloneNode(true));});
  const modal=group.locator('xpath=ancestor::div[contains(@class,"agenda-modal")]');
  const id=await modal.getAttribute('id');await page.locator(`[data-bs-target="#${id}"]`).click();
  const actions=await group.locator('a').evaluateAll(es=>es.map(e=>{const b=e.getBoundingClientRect();return {top:b.top,height:b.height,left:b.left,right:b.right};}));
  check(actions.length>=2&&Math.abs(actions[0].top-actions[1].top)<1&&Math.abs(actions[0].height-actions[1].height)<1,'acciones alineadas y de igual altura '+width+' '+theme);
  for(const selector of ['.dash-button','.btn-outline-primary']){
   const button=group.locator(selector).first();await button.hover();
   check(await button.evaluate(e=>getComputedStyle(e).color==='rgb(0, 0, 0)'&&getComputedStyle(e).backgroundColor==='rgb(255, 218, 68)'),'texto oscuro sobre amarillo al pasar el cursor '+selector+' '+width+' '+theme);
   await button.focus();check(await button.evaluate(e=>getComputedStyle(e).color==='rgb(0, 0, 0)'),'contraste al enfocar '+selector+' '+width+' '+theme);
  }
  if(width===390&&theme==='dark'){await page.screenshot({path:path.join(dir,'acciones-movil-oscuro.png')});}
  await modal.locator('[data-bs-dismiss=modal]').first().click();
  if(width===390&&theme==='dark'){await page.locator('.agenda-mensual').screenshot({path:path.join(dir,'calendario-movil-oscuro.png')});}
 }
 console.log('OK: '+checks+' comprobaciones de calendario, acciones, contraste y desplegables en cuatro tamaños y ambos temas.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
