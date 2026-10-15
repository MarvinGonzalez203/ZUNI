const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const fixtures=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');
if(!fixtures)throw new Error('Indica el directorio de fixtures offline.');
let checks=0;function check(value,message){assert.ok(value,message);checks++;}
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1366,height:768},reducedMotion:'reduce'});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.route('**/*',async route=>{
   const url=new URL(route.request().url());if(url.origin!=='http://zuni.test')return route.abort();
   if(url.pathname==='/Home/Guia'||route.request().isNavigationRequest()){
    const file=url.pathname==='/Home/Guia'?'guia-'+(url.searchParams.get('tema')||'cita')+(url.searchParams.has('en')?'-en':''):url.pathname==='/en'?'idioma-home':url.pathname==='/agenda'?'agenda-revision':'home-footer';
    return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(fixtures,file+'.html'),'utf8')});
   }
   const file=path.resolve(root,url.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png'}[path.extname(file)];
   if(type&&file.startsWith(root+path.sep)&&fs.existsSync(file))return route.fulfill({contentType:type,body:fs.readFileSync(file)});
   return route.abort();
  });
  for(const [width,height] of [[1366,768],[1024,600],[390,844],[320,844]]){
   await page.setViewportSize({width,height});const heights=[];
   for(const locale of ['/en','/']){
    await page.goto('http://zuni.test'+locale);
    heights.push((await page.locator('.quick-panel').boundingBox()).height);
    check(await page.locator('.zuni-logo-disc').count()===2,'dos logos circulares '+locale);
    check(await page.locator('.zuni-logo-disc').first().evaluate(e=>{const s=getComputedStyle(e);return s.backgroundColor==='rgb(255, 255, 255)'&&s.borderRadius==='50%';}),'fondo blanco circular '+locale);
    check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desborde '+width);
   }
   check(Math.abs(heights[0]-heights[1])<1,'altura igual ES EN '+width+' '+heights);
  }
  await page.setViewportSize({width:1280,height:800});
  await page.goto('http://zuni.test/Home/Guia?tema=cita');
  check(await page.locator('.guide-tabs a').count()===7,'siete guías');
  await page.locator(".guide-tabs").evaluate(e=>scrollTo(0,scrollY+e.getBoundingClientRect().top-40));
  for(const topic of ['seguimiento','cuenta','pruebas','informes','privacidad']){
   const y=await page.evaluate(()=>scrollY);
   await page.locator(`.guide-tabs a[href$="tema=${topic}"]`).click();
   await page.waitForFunction(topic=>new URL(location.href).searchParams.get('tema')===topic,topic);
   await page.waitForFunction(y=>Math.abs(scrollY-y)<2,y);
   check(await page.locator('.guide-tabs a[aria-current=page]').getAttribute('href').then(v=>v.endsWith('tema='+topic)),'selección actual '+topic);
   check(await page.locator('.guide-card').count()>=4,'tutorial completo '+topic);
  }
  await page.goBack();await page.waitForFunction(()=>new URL(location.href).searchParams.get('tema')==='informes');
  await page.waitForFunction(()=>document.querySelector('.guide-tabs a[aria-current=page]').href.endsWith('tema=informes'));
  check(true,'atrás recupera la guía');
  for(const topic of ['cuenta','pruebas','informes']){
   await page.goto('http://zuni.test/Home/Guia?tema='+topic+'&en=1');
   check(await page.locator('html').getAttribute('lang')==='en','guía nueva en inglés '+topic);
   check(await page.locator('.guide-card').count()===4,'cuatro tarjetas '+topic);
  }
  if(await page.locator('html').getAttribute('data-theme')!=='dark')await page.locator('.zuni-theme-toggle').click();
  await page.goto('http://zuni.test/agenda');
  const modal=page.locator('.modal.show');await modal.waitFor({state:'visible'});
  const cue=modal.locator('.modal-scroll-cue');await cue.waitFor({state:'visible'});
  check(await modal.evaluate(e=>getComputedStyle(e).scrollbarWidth==='none'),'barra modal oculta');
  check(await cue.evaluate(e=>getComputedStyle(e).borderTopWidth==='0px'),'indicador sin borde');
  check(await cue.evaluate(e=>getComputedStyle(e).color==='rgb(234, 213, 255)'),'indicador legible en oscuro');
  check(await cue.locator('svg').evaluate(e=>getComputedStyle(e).animationName==='none'),'indicador respeta movimiento reducido');
  const scrollTop=()=>modal.evaluate(e=>Math.max(e.scrollTop,...[...e.querySelectorAll('.modal-body,.modal-content')].map(n=>n.scrollTop)));
  const before=await scrollTop();await cue.click();await page.waitForFunction(before=>{const m=document.querySelector('.modal.show');return Math.max(m.scrollTop,...[...m.querySelectorAll('.modal-body,.modal-content')].map(n=>n.scrollTop))>before;},before);
  check(true,'V desplaza el contenido');
  await modal.evaluate(e=>[e,...e.querySelectorAll('.modal-body,.modal-content')].forEach(n=>n.scrollTop=n.scrollHeight));
  await cue.waitFor({state:'hidden'});check(true,'V se oculta al terminar');
  await modal.evaluate(e=>[e,...e.querySelectorAll('.modal-body,.modal-content')].forEach(n=>n.scrollTop=0));
  await cue.waitFor({state:'visible'});check(true,'V reaparece al volver');
  await page.screenshot({path:path.join(fixtures,'modal-con-indicador.png')});
  await modal.evaluate(e=>{e.scrollTop=0;const b=e.querySelector('.modal-body');b.style.maxHeight='220px';b.style.overflowY='auto';b.scrollTop=0;});
  await cue.waitFor({state:'visible'});
  await cue.click();
  await page.waitForFunction(()=>document.querySelector('.modal.show .modal-body').scrollTop>0);
  check(true,'V desplaza también el cuerpo interno');
  await modal.locator('.modal-body').evaluate(e=>e.scrollTop=0);
  await modal.locator('.modal-body').hover();await page.mouse.wheel(0,120);
  await page.waitForFunction(()=>document.querySelector('.modal.show .modal-body').scrollTop>0);
  check(true,'rueda conservada sin barra');
  await modal.evaluate(e=>{e.scrollTop=0;const b=e.querySelector('.modal-body');b.innerHTML='<p>Contenido breve</p>';b.style.maxHeight='';b.style.overflowY='auto';});
  await cue.waitFor({state:'hidden'});check(true,'ventana corta sin indicador innecesario');
  check(await modal.locator('.modal-scroll-cue').count()===1,'un solo indicador por ventana');
  check(errors.length===0,'sin errores JS: '+errors.join('; '));
  console.log(`OK: ${checks} comprobaciones de guías, logos, alturas e indicadores.`);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
