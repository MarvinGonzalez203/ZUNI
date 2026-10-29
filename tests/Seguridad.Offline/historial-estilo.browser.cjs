const {chromium}=require('playwright');const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const dir=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');let checks=0;const check=(v,m)=>{assert.ok(v,m);checks++;};
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
const context=await browser.newContext({viewport:{width:1366,height:800},reducedMotion:'reduce'}),page=await context.newPage(),errors=[];let reads=0,docs=0;
page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(r.isNavigationRequest())docs++;});
await context.route('**/*',route=>{const u=new URL(route.request().url());if(u.origin!=='http://zuni.test')return route.abort();if(u.pathname==='/Actualizaciones/Leida'){reads++;return route.fulfill({status:204});}
if(route.request().isNavigationRequest()){const name=u.searchParams.get('lang')==='en'?'v1-super-en':'v1-super-unread-es';return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(dir,name+'.html'),'utf8')});}
const file=path.resolve(root,u.pathname.slice(1)),type={'.css':'text/css','.js':'text/javascript','.png':'image/png','.ico':'image/x-icon'}[path.extname(file)];return type&&file.startsWith(root+path.sep)&&fs.existsSync(file)?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();});
const current=fs.readFileSync(path.join(root,'../Zuni.csproj'),'utf8').match(/<Version>([^<]+)/)[1];
for(const width of [1366,390,320])for(const theme of ['light','dark']){
 await page.setViewportSize({width,height:844});await page.goto('http://zuni.test/SuperAdmin');await page.evaluate(t=>document.documentElement.dataset.theme=t,theme);
 await page.locator('.release-control>summary').click();const versions=await page.locator('.release-entry').evaluateAll(es=>es.map(e=>e.dataset.releaseVersion));
 check(JSON.stringify(versions)===JSON.stringify([current,'1.1.1','1.1.0']),'tres versiones reales '+width+' '+theme);
 check(await page.locator('.release-entry').evaluateAll(es=>es.every(e=>e.querySelector('h3')?.textContent.includes(e.dataset.releaseVersion)&&e.querySelectorAll('li').length>0)),'títulos y cambios por versión '+width+' '+theme);
 const box=await page.locator('.release-content').boundingBox();check(box.x>=0&&box.x+box.width<=width+1&&box.y>=0&&box.y+box.height<=844,'historial dentro de pantalla '+width+' '+theme);
 await page.locator('.release-history').evaluate(e=>e.scrollTop=e.scrollHeight);const last=await page.locator('.release-entry').last().boundingBox();check(last.y+last.height<=box.y+box.height,'versión anterior accesible por desplazamiento '+width+' '+theme);
 await page.locator('.release-history').evaluate(e=>e.scrollTop=0);
 if(theme==='dark'&&(width===1366||width===390))await page.screenshot({path:path.join(dir,'historial-'+width+'-oscuro.png'),fullPage:false});
 const count=docs;await page.locator('[data-release-read] button').click();await page.locator('[data-release-read]').waitFor({state:'hidden'});
 check(await page.locator('.release-dot').count()===0&&await page.locator('.release-entry').count()===3,'leer retira punto y conserva historial '+width+' '+theme);
 check(docs===count,'lectura no recarga documento '+width+' '+theme);
 await page.keyboard.press('Escape');check((await page.locator('.release-control').getAttribute('open'))===null,'escape cierra historial '+width+' '+theme);
}
await page.goto('http://zuni.test/SuperAdmin?lang=en');await page.locator('.release-control>summary').click();check(await page.locator('.release-content').innerText().then(t=>t.includes('Updates')&&t.includes('Consistent button')&&!t.includes('Botones con forma')),'historial traducido al inglés');
check(reads===6,'lecturas individuales enviadas por POST');check(errors.length===0,'sin errores JavaScript: '+errors.join(';'));console.log('OK: '+checks+' comprobaciones de historial, tamaños, temas, lectura y traducción.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
