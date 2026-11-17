const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');let checks=0;
const check=(v,m)=>{assert.ok(v,m);checks++;};
const rgb=s=>s.match(/[\d.]+/g).slice(0,3).map(Number);
const luminance=c=>c.map(x=>{x/=255;return x<=.04045?x/12.92:((x+.055)/1.055)**2.4}).reduce((s,x,i)=>s+x*[.2126,.7152,.0722][i],0);
const contrast=(a,b)=>{a=luminance(rgb(a));b=luminance(rgb(b));return (Math.max(a,b)+.05)/(Math.min(a,b)+.05)};
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 for(const width of [360,390,768,1366]){
  const context=await browser.newContext({viewport:{width,height:900},reducedMotion:'reduce'}),page=await context.newPage();let lang='es',navigations=0;const errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('framenavigated',f=>{if(f===page.mainFrame())navigations++;});
  await page.route('http://denis.test/**',async route=>{
   const u=new URL(route.request().url());
   if(u.pathname==='/Idioma/Cambiar'){
    const body=route.request().postData()||'';lang=body.match(/name="idioma"\r?\n\r?\n(en|es)/)?.[1]||new URLSearchParams(body).get('idioma')||lang;
    return route.fulfill({contentType:'application/json',body:JSON.stringify({idioma:lang,volver:await page.evaluate(()=>location.pathname+location.hash)})});
   }
   if(route.request().isNavigationRequest()||route.request().headers().accept==='text/html'){
    const name=u.pathname==='/'||u.pathname==='/deniscorp/'?'deniscorp':u.pathname.split('/').filter(Boolean).at(-1);
    return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(dir,name+'-'+lang+'.html'),'utf8')});
   }
   const file=path.resolve(root,u.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png','.svg':'image/svg+xml','.ico':'image/x-icon'}[path.extname(file)];
   return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();
  });
  await page.goto('http://denis.test/');
  check(await page.locator('.product-card').count()===5,'cinco tarjetas '+width);
  check(await page.locator('.corp-header nav a').allTextContents().then(t=>t.some(x=>x==='Blog')&&t.some(x=>x.includes('Contáctanos'))&&!t.some(x=>x.includes('ZUNI'))),'navegación corporativa '+width);
  check(await page.locator('.brand-glass').count()===7,'cristal en todos los logos del inicio '+width);
  check(!(await page.locator('main').innerText()).includes('LSR'),'la propuesta escolar no usa la marca del cliente '+width);
  for(const theme of ['light','dark']){
   if(await page.locator('html').getAttribute('data-theme')!==theme)await page.locator('#zuni-theme-control').click();
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desbordamiento '+width+theme);
   check(await page.locator('#zuni-theme-control').getAttribute('aria-pressed')===(theme==='dark'?'true':'false'),'estado de tema accesible '+width+theme);
   for(const [id,color]of Object.entries({zuni:'rgb(206, 162, 253)',academy:'rgb(41, 101, 199)',fulleo:'rgb(242, 202, 72)',pos:theme==='dark'?'rgb(232, 3, 27)':'rgb(227, 213, 202)',colegios:theme==='dark'?'rgb(232, 3, 27)':'rgb(227, 213, 202)'})){
    const button=page.locator('[data-product="'+id+'"] .corp-button');await button.hover();
    const style=await button.evaluate(e=>({bg:getComputedStyle(e).backgroundColor,fg:getComputedStyle(e).color,filter:getComputedStyle(e).filter}));
    check(style.bg===color,'paleta '+id+width+theme);check(contrast(style.bg,style.fg)>=4.5&&style.filter==='none','contraste al pasar cursor '+id+width+theme);
   }
   const crystal=await page.locator('.brand-glass').evaluateAll(es=>es.every(e=>getComputedStyle(e,'::before').backgroundImage!=='none'&&getComputedStyle(e,'::after').borderTopColor.includes('255')));
   check(crystal,'círculos blancos de cristal en todos los logos '+width+theme);
   if(width===390||width===1366){await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(dir,'portal-'+width+'-'+theme+'.png'),fullPage:true});}
  }
  await page.reload();check(await page.locator('html').getAttribute('data-theme')==='dark','tema persiste '+width);
  check(await page.evaluate(()=>localStorage.getItem('zuni-theme:publico')===null),'preferencia ZUNI intacta '+width);
  await page.evaluate(()=>scrollTo(0,document.documentElement.scrollHeight));
  await page.waitForFunction(()=>document.querySelector('#corp-scroll-control')?.dataset.direction==='up');
  await page.locator('#corp-scroll-control').click();await page.waitForFunction(()=>scrollY===0);check(true,'V final vuelve al principio '+width);
  await page.locator('#corp-scroll-control').click();await page.waitForFunction(()=>scrollY>100);check(true,'V permite avanzar '+width);
  const beforeNavigation=navigations,previousY=await page.evaluate(()=>scrollY);
  await page.locator('form.zuni-language').evaluate(f=>f.requestSubmit());await page.waitForFunction(()=>document.documentElement.lang==='en');
  check(navigations===beforeNavigation,'idioma sin recarga '+width);
  check(Math.abs((await page.evaluate(()=>scrollY))-previousY)<2,'idioma conserva posición '+width);
  check(await page.locator('h1').innerText().then(t=>t.includes('A world of ideas')),'portada traducida '+width);
  check(await page.locator('html').getAttribute('data-theme')==='dark','idioma conserva tema '+width);
  const iconos=[];
  for(const id of ['academy','fulleo','pos','colegios']){
   await page.goto('http://denis.test/deniscorp/'+id+'/');
   iconos.push((await page.locator('link[rel=icon]').getAttribute('href')).split('?')[0]);
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'ficha sin desbordamiento '+id+width);
   check(await page.locator('h2').first().innerText()===(id==='colegios'?'School service offering':'Product progress'),'avances traducidos '+id+width);
   if(id==='fulleo'){
    check(await page.locator('.release-badge').innerText().then(t=>t.includes('0.8.0')),'versión Fulleo '+width);
    check(await page.locator('.progress-card').count()===4,'avances reales Fulleo '+width);
    if(width===390||width===1366)await page.screenshot({path:path.join(dir,'fulleo-'+width+'-dark-en.png'),fullPage:true});
   }
  }
  check(new Set(iconos).size===4,'favicons propios '+width);
  check(iconos[2]==='/images/productos/deniscorp-32.png','POS usa Denis Corp '+width);
  await page.goto('http://denis.test/deniscorp/blog/');
  check(await page.locator('[data-blog-article]:visible').count()===8,'biblioteca de guías '+width);
  await page.locator('#blog-search').fill('recovery');check(await page.locator('[data-blog-article]:visible').count()===1,'búsqueda de guía '+width);
  await page.locator('#blog-search').fill('');await page.locator('[data-blog-topic=fulleo]').click();check(await page.locator('[data-blog-article]:visible').count()===1,'filtro por programa '+width);
  await page.locator('#blog-search').fill('zzzz-no-resultados');check(await page.locator('[data-blog-empty]').isVisible(),'resultado vacío claro '+width);
  await page.locator('[data-blog-featured]').click();check(await page.locator('#guia-elegir-programa details').getAttribute('open')!==null&&await page.locator('[data-blog-article]:visible').count()===8,'guía destacada reinicia filtros y se abre '+width);
  await page.locator('[data-blog-topic=fulleo]').click();await page.locator('#blog-search').fill('Fulleo');
  await page.locator('form.zuni-language').evaluate(f=>f.requestSubmit());await page.waitForFunction(()=>document.documentElement.lang==='es');
  check(await page.locator('#blog-search').inputValue()==='Fulleo'&&await page.locator('[data-blog-article]:visible').count()===1,'idioma conserva búsqueda y tema '+width);
  await page.locator('[data-blog-featured]').click();
  for(const theme of ['light','dark']){if(await page.locator('html').getAttribute('data-theme')!==theme)await page.locator('#zuni-theme-control').click();check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'blog sin desbordamiento '+width+theme);if(width===390||width===1366){await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(dir,'blog-'+width+'-'+theme+'.png'),fullPage:true});}}
  await page.goto('http://denis.test/deniscorp/contacto/');
  check(await page.locator('[data-corp-contact] .required-description').evaluateAll(es=>es.length===2&&es.every(e=>e.getBoundingClientRect().width<=1)),'descripciones accesibles no rompen alineación de etiquetas '+width);
  let popups=0;page.on('popup',()=>popups++);await page.locator('[data-contact-channel=whatsapp]').click();
  check(popups===0&&await page.locator('#contact-name').evaluate(e=>!e.validity.valid),'formulario vacío no abre canal '+width);
  await page.locator('#contact-name').fill('Persona ficticia');await page.locator('#contact-organization').fill('Colegio de prueba');await page.locator('#contact-program').selectOption('colegios');await page.locator('#contact-topic').selectOption('contratacion');await page.locator('#contact-message').fill('Consulta ficticia de disponibilidad & requisitos');
  const wa=new URL(await page.locator('[data-contact-channel=whatsapp]').getAttribute('href')),mail=new URL(await page.locator('[data-contact-channel=email]').getAttribute('href'));
  check(wa.hostname==='wa.me'&&wa.pathname==='/50230390175'&&wa.searchParams.get('text').includes('Persona ficticia')&&wa.searchParams.get('text').includes('Denis Corp Colegios'),'WhatsApp prepara el borrador correcto sin enviarlo '+width);
  check(mail.protocol==='mailto:'&&mail.pathname==='corporation.denis@gmail.com'&&mail.searchParams.get('body').includes('& requisitos'),'correo conserva mensaje y caracteres '+width);
  check(await page.locator('[data-corp-contact]').evaluate(f=>[...new FormData(f)].length)===0,'datos del borrador no se envían al servidor '+width);
  for(const theme of ['light','dark']){if(await page.locator('html').getAttribute('data-theme')!==theme)await page.locator('#zuni-theme-control').click();check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'contacto sin desbordamiento '+width+theme);if(width===390||width===1366){await page.evaluate(()=>scrollTo(0,0));await page.screenshot({path:path.join(dir,'contacto-'+width+'-'+theme+'.png'),fullPage:true});}}
  await page.locator('form.zuni-language').evaluate(f=>f.requestSubmit());await page.waitForFunction(()=>document.documentElement.lang==='en');
  check(await page.locator('#contact-name').inputValue()==='Persona ficticia'&&await page.locator('#contact-message').inputValue()==='Consulta ficticia de disponibilidad & requisitos','idioma conserva el borrador '+width);
  check(new URL(await page.locator('[data-contact-channel=whatsapp]').getAttribute('href')).searchParams.get('text').includes('Name: Persona ficticia'),'borrador se adapta al idioma '+width);
  check(errors.length===0,'sin errores JavaScript '+errors.join(';'));
  await context.close();
 }
 console.log('OK: '+checks+' comprobaciones de temas, colores, contraste, idioma, favicons, avances y desplazamiento.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
