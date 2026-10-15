const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const fixtures=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');
let checks=0;function check(v,m){assert.ok(v,m);checks++;}
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({viewport:{width:1600,height:1000},reducedMotion:'reduce'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 const pages={'/Tarifas':'tarifas','/SuperAdmin/Tarifas':'tarifas-superadmin','/SuperAdmin':'superadmin','/CarrerasAdmin':'carreras-simple','/Institucion':'institucion-paleta','/PruebasDirector/Detalle':'prueba-director-escala'};
 await page.route('**/*',async route=>{const url=new URL(route.request().url());if(url.origin!=='http://zuni.test')return route.abort();if(route.request().isNavigationRequest()){const fixture=pages[url.pathname];if(!fixture)return route.abort();return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(fixtures,fixture+'.html'),'utf8')});}const file=path.resolve(root,url.pathname.slice(1));const type={'.css':'text/css','.js':'text/javascript','.png':'image/png'}[path.extname(file)];return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();});
 await page.goto('http://zuni.test/Tarifas');
 check(!(await page.locator('.tarifa-grid').innerText()).includes('PROPUESTO'),'Administrador no ve precio propuesto');
 check(await page.locator('.tarifa-beneficios>article').count()===5,'cinco grupos de beneficios');
 check(await page.locator('.tarifa-beneficios').evaluate(e=>getComputedStyle(e).gridTemplateColumns.split(' ').length)===5,'cinco columnas en escritorio');
 const links=await page.locator('.dash-nav a').evaluateAll(a=>a.map(e=>e.getAttribute('href')));
 check(links.at(-2)==='/Tarifas'&&links.at(-1)==='/Institucion','tarifas penúltima e institución última');
 check(await page.locator('.dash-top-profile').getAttribute('href')==='/MiCuenta','avatar y nombre llevan a Mi cuenta');
 for(const theme of ['light','dark']){
  await page.evaluate(t=>document.documentElement.dataset.theme=t,theme);
  for(const width of [1600,768,390,320]){
   await page.setViewportSize({width,height:950});
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desborde tarifas '+theme+width);
   check(await page.locator('.dash-top-profile').isVisible(),'avatar interactivo visible '+theme+width);
  }
 }
 await page.setViewportSize({width:1600,height:1000});
 await page.screenshot({path:path.join(fixtures,'tarifas-beneficios-oscuro.png'),fullPage:true});
 await page.goto('http://zuni.test/SuperAdmin/Tarifas');
 check((await page.locator('.tarifa-grid').innerText()).includes('PROPUESTO'),'SuperAdmin sí ve precio propuesto');
 await page.goto('http://zuni.test/CarrerasAdmin');
 check(await page.locator('form[action="/CarrerasAdmin/AgregarArea"] #Area').isVisible(),'área separada visible');
 check(await page.locator('#Carreras').isVisible(),'carreras separadas visibles');
 check(await page.locator('details input[type=file]').isHidden(),'importación avanzada recogida');
 await page.screenshot({path:path.join(fixtures,'carreras-simple.png'),fullPage:false});
 await page.goto('http://zuni.test/Institucion');
 const swatches=page.locator('[data-color-institucion]');
 check(await swatches.count()===5,'cinco colores');
 const sizes=await swatches.evaluateAll(es=>es.map(e=>[e.getBoundingClientRect().width,e.getBoundingClientRect().height]));
 check(sizes.every(s=>s[0]===sizes[0][0]&&s[1]===sizes[0][1]),'todos los selectores del mismo tamaño');
 check(await page.locator('[data-color-institucion="#F9DB5C"]').count()===1&&await page.locator('[data-color-institucion="#BBF1F8"]').count()===1,'amarillo y celeste exactos');
 for(const theme of ['light','dark']){
  await page.evaluate(t=>document.documentElement.dataset.theme=t,theme);
  const backgrounds=[];
  for(const color of ['#cea2fd','#111111','#b42332','#F9DB5C','#BBF1F8']){
   await page.locator(`[data-color-institucion="${color}"]`).click();
   check((await page.locator('#ColorPrincipal').inputValue()).toLowerCase()===color.toLowerCase(),'selección del color '+color);
   // La identidad se aplica al guardar. Ensayar las mismas variables del documento guardado.
   await page.evaluate(c=>document.documentElement.style.setProperty('--institucion-color',c),color);
   backgrounds.push(await page.locator('.dash-nav a.is-active').evaluate(e=>getComputedStyle(e).backgroundColor));
   check(await page.locator('.dash-nav a.is-active').evaluate(e=>getComputedStyle(e).backdropFilter.includes('blur')),'pestaña conserva cristal '+theme+color);
  }
  check(new Set(backgrounds).size===5,'cada paleta cambia la pestaña '+theme);
 }
 await page.screenshot({path:path.join(fixtures,'institucion-paleta-oscuro.png'),fullPage:false});
 await page.goto('http://zuni.test/PruebasDirector/Detalle');
 const escala=page.locator('.prueba-preview-scale').first();
 check(await escala.locator('.prueba-preview-cell').count()===10,'diez celdas en Director');
 check(await escala.locator('.prueba-preview-cell').evaluateAll(es=>es.every(e=>Math.abs(e.offsetTop-es[0].offsetTop)<2)),'escala horizontal en Director');
 check(await escala.locator('input').count()===0,'vista previa no guarda respuestas');
 check(await page.locator('form[action="/PruebasDirector/Eliminar"]').count()===0,'nivel no ofrece borrado');
 await escala.scrollIntoViewIfNeeded();
 await page.screenshot({path:path.join(fixtures,'director-escala-horizontal.png'),fullPage:false});
 check(errors.length===0,'sin errores JS: '+errors.join('; '));console.log(`OK: ${checks} comprobaciones visuales de SuperAdmin y catálogo.`);
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
