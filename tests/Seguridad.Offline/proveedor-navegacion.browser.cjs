const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');let checks=0;
function check(ok,m){assert.ok(ok,m);checks++;}
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({viewport:{width:1500,height:1000},reducedMotion:'reduce'}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 const pages={'/SuperAdmin':'superadmin','/SuperAdmin/Apariencia':'proveedor-apariencia','/MiCuenta':'proveedor-cuenta','/MiCuenta/Editar':'proveedor-datos','/MiCuenta/CambiarContrasena':'proveedor-clave','/Tarifas':'tarifas-superadmin','/Tarifas/Editar':'tarifas-editor','/Administrador/Tarifas':'tarifas'};
 await page.route('**/*',async route=>{const u=new URL(route.request().url());if(u.origin!=='http://zuni.test')return route.abort();if(route.request().isNavigationRequest()){const file=pages[u.pathname];return file?route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(dir,file+'.html'),'utf8')}):route.abort();}const file=path.resolve(root,u.pathname.slice(1)),type={'.js':'text/javascript','.css':'text/css','.png':'image/png'}[path.extname(file)];return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();});
 for(const url of ['/MiCuenta','/MiCuenta/Editar','/MiCuenta/CambiarContrasena','/Tarifas','/Tarifas/Editar','/SuperAdmin/Apariencia']){
  await page.goto('http://zuni.test'+url);
  check(await page.locator('.dash-nav a.is-active').count()===1,'una selección activa '+url);
  for(const href of ['/MiCuenta/Editar','/MiCuenta/CambiarContrasena','/Tarifas/Editar','/SuperAdmin/Apariencia'])check(await page.locator(`.dash-nav a[href="${href}"]`).count()===1,'acceso persistente '+href+' en '+url);
  check(await page.locator('.dash-space-link').getAttribute('href')==='/SuperAdmin','Mi espacio enlaza al tablero '+url);
 }
 await page.locator('.dash-space-link').click();check(new URL(page.url()).pathname==='/SuperAdmin','Mi espacio navega realmente');
 await page.goto('http://zuni.test/MiCuenta');
 const group=page.locator('.dash-nav-group').filter({has:page.locator('a[href="/MiCuenta/Editar"]')}),summary=group.locator('summary');
 check(await group.getAttribute('open')!==null,'grupo actual abierto');
 const coords=()=>summary.evaluate(e=>{const s=getComputedStyle(e,'::after');return [s.right,s.top,s.width,s.height,s.content];});
 const before=await coords();await summary.click();check(JSON.stringify(before)===JSON.stringify(await coords()),'V no cambia de posición al plegar');await summary.click();
 check(await summary.evaluate(e=>getComputedStyle(e,'::after').content==='""'),'V minimalista sin glifo');
 await page.goto('http://zuni.test/SuperAdmin/Apariencia');
 const sizes=await page.locator('[data-color-proveedor]').evaluateAll(es=>es.map(e=>[e.offsetWidth,e.offsetHeight]));check(sizes.length===5&&sizes.every(s=>s[0]===sizes[0][0]&&s[1]===sizes[0][1]),'cinco colores iguales');
 for(const theme of ['light','dark']){
  await page.evaluate(t=>document.documentElement.dataset.theme=t,theme);
  for(const color of ['#cea2fd','#111111','#b42332','#F9DB5C','#BBF1F8']){
   await page.locator(`[data-color-proveedor="${color}"]`).click();
   check((await page.locator('#ColorPrincipal').inputValue()).toLowerCase()===color.toLowerCase(),'selección de color '+theme+color);
   check(await page.locator(`[data-color-proveedor="${color}"]`).getAttribute('aria-pressed')==='true','selección accesible '+color);
   const expected=['#111111','#b42332'].includes(color)?'#ffffff':'#000000';
   check(await page.evaluate(()=>getComputedStyle(document.documentElement).getPropertyValue('--institucion-texto').trim())===expected,'contraste automático '+color);
  }
 }
 await page.screenshot({path:path.join(dir,'proveedor-colores-oscuro.png'),fullPage:true});
 for(const width of [768,390,320]){await page.setViewportSize({width,height:950});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'colores sin desborde '+width);}
 await page.setViewportSize({width:1500,height:1000});await page.goto('http://zuni.test/Tarifas');
 check(await page.locator('.tarifa-beneficios>article').count()===6,'seis tarjetas para SuperAdmin');
 check(await page.locator('.tarifa-capacidades-superadmin').innerText().then(t=>t.includes('Editar precios')&&t.includes('sin acceso a alumnos')),'capacidades comerciales y privacidad');
 await page.locator('.tarifa-capacidades-superadmin').scrollIntoViewIfNeeded();await page.screenshot({path:path.join(dir,'proveedor-capacidades.png'),fullPage:false});
 await page.goto('http://zuni.test/Administrador/Tarifas');check(await page.locator('.tarifa-capacidades-superadmin').count()===0&&await page.locator('.tarifa-beneficios>article').count()===5,'institución conserva cinco tarjetas');
 check(errors.length===0,'sin errores JavaScript: '+errors.join('; '));console.log(`OK: ${checks} comprobaciones visuales de navegación comercial.`);
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
