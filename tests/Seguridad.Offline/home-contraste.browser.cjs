const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const fixtures=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');
if(!fixtures)throw new Error('Indica el directorio de fixtures offline.');
let checks=0;
function check(value,message){assert.ok(value,message);checks++;}
(async()=>{
    const browser=await chromium.launch({channel:'msedge',headless:true});
    try{
        const page=await browser.newPage({viewport:{width:1366,height:768},reducedMotion:'reduce'});
        const routes={home:'idioma-home',inicio:'home-footer',catalogo:'catalogo-contraste',informes:'informes-contraste',estudiantes:'solicitudes-psicologo',director:'idioma-director'};
        await page.route('**/*',async route=>{
            const url=new URL(route.request().url());
            if(url.origin!=='http://zuni.test')return route.abort();
            if(route.request().isNavigationRequest()){
                const fixture=routes[url.pathname.slice(1)];
                if(!fixture)return route.abort();
                return route.fulfill({contentType:'text/html',body:fs.readFileSync(path.join(fixtures,fixture+'.html'),'utf8')});
            }
            const file=path.resolve(root,url.pathname.slice(1));
            if(!file.startsWith(root+path.sep)||!fs.existsSync(file)||!fs.statSync(file).isFile())return route.abort();
            const types={'.css':'text/css','.js':'text/javascript','.png':'image/png'};
            const type=types[path.extname(file)];
            return type?route.fulfill({contentType:type,body:fs.readFileSync(file)}):route.abort();
        });
        async function theme(dark){if(await page.locator('html').getAttribute('data-theme')!==(dark?'dark':'light'))await page.locator('.zuni-theme-toggle').click();}
        for(const language of ['home','inicio'])for(const [width,height] of [[1366,600],[1366,650],[1366,768],[1920,1080],[1024,600],[1024,768]]){
            await page.setViewportSize({width,height});
            await page.goto('http://zuni.test/'+language);
            const box=await page.locator('.landing-hero').boundingBox();
            check(box.y+box.height<=height,'rectángulo completo '+language+' '+width+'x'+height+' bottom='+(box.y+box.height));
            check(await page.locator('.quick-panel').evaluate(e=>e.getBoundingClientRect().bottom<=e.closest('.landing-hero').getBoundingClientRect().bottom),'barra dentro del rectángulo');
            check(await page.locator('.hero-symbol .hero-glass-orbit').count()===2,'dos círculos del logo');
            check(await page.locator('.home-scroll-cue').isVisible(),'indicador disponible al inicio');
        }
        await page.setViewportSize({width:1366,height:768});await page.goto('http://zuni.test/home');
        await theme(true);
        check(await page.locator('.home-scroll-cue').evaluate(e=>getComputedStyle(e).borderTopWidth==='0px'),'flecha sin borde');
        check(await page.locator('.home-scroll-cue svg').evaluate(e=>getComputedStyle(e).animationName==='none'),'respeta movimiento reducido');
        await page.screenshot({path:path.join(fixtures,'home-compacto-oscuro.png')});
        await page.locator('.home-scroll-cue').click();
        await page.waitForFunction(()=>scrollY>24&&document.querySelector('.home-scroll-cue').hidden);
        check(await page.locator('#zuni-proposito').evaluate(e=>document.activeElement===e),'flecha lleva a misión y visión y mueve el foco');
        await page.evaluate(()=>scrollTo(0,0));await page.locator('.home-scroll-cue').waitFor({state:'visible'});
        check(true,'indicador reaparece al volver al inicio');
        await page.emulateMedia({reducedMotion:'no-preference'});
        check(await page.locator('.home-scroll-cue svg').evaluate(e=>getComputedStyle(e).animationName==='home-scroll-hint'),'animación disponible');
        for(const width of [390,320]){
            await page.setViewportSize({width,height:844});await page.goto('http://zuni.test/home');
            check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'home móvil sin desborde '+width);
        }
        await page.setViewportSize({width:1366,height:768});
        for(const name of ['catalogo','informes','estudiantes','director']){
            await page.goto('http://zuni.test/'+name);await theme(true);
            const results=await page.locator('table th,table td,.dash-nav-link,.dash-nav-group summary,.dash-welcome-art>span,.dash-welcome p,.dash-footer,.psych-assigned-muted').evaluateAll(elements=>{
                const luminance=rgb=>{const c=rgb.map(v=>{v/=255;return v<=.04045?v/12.92:Math.pow((v+.055)/1.055,2.4);});return c[0]*.2126+c[1]*.7152+c[2]*.0722;};
                return elements.filter(e=>e.getBoundingClientRect().height&&e.textContent.trim()).map(e=>{
                    const rgb=getComputedStyle(e).color.match(/[\d.]+/g).slice(0,3).map(Number);
                    let background=[25,23,30];
                    const ancestors=[];for(let node=e;node;node=node.parentElement)ancestors.unshift(node);
                    for(const node of ancestors){const bg=getComputedStyle(node).backgroundColor.match(/[\d.]+/g)?.map(Number);if(bg){const a=bg[3]??1;background=background.map((v,i)=>bg[i]*a+v*(1-a));}}
                    const fg=luminance(rgb),bg=luminance(background);
                    return {text:e.textContent.trim().slice(0,60),contrast:(Math.max(fg,bg)+.05)/(Math.min(fg,bg)+.05)};
                });
            });
            check(results.length>0,'textos examinados '+name);
            for(const result of results)check(result.contrast>=4.5,'contraste '+name+' '+JSON.stringify(result));
            await page.screenshot({path:path.join(fixtures,'contraste-'+name+'-oscuro.png')});
        }
        console.log(`OK: ${checks} comprobaciones de home, desplazamiento y contraste.`);
    }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
