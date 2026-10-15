// Fixtures locales sobre un origen aislado: sin datos ni sesiones reales.
const {chromium}=require('playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const fixtures=process.argv[2],root=path.resolve(__dirname,'../../Zuni/wwwroot');
if(!fixtures)throw new Error('Indica el directorio de fixtures offline.');
let checks=0;
function check(value,message){assert.ok(value,message);checks++;}
(async()=>{
    const browser=await chromium.launch({channel:'msedge',headless:true});
    try{
        const page=await browser.newPage({viewport:{width:1280,height:900}});
        await page.route('**/*',async route=>{
            if(!route.request().isNavigationRequest())return route.abort();
            const name=new URL(route.request().url()).pathname.slice(1)||'home';
            if(!['home','admin','director','psicologo','estudiante','alta'].includes(name))return route.abort();
            let html=fs.readFileSync(path.join(fixtures,`idioma-${name}.html`),'utf8').replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi,'').replace(/<link\b[^>]*>/gi,'');
            const css=['lib/bootstrap/dist/css/bootstrap.min.css',name==='home'?'css/site.css':'css/dashboard.css','css/idioma.css','css/tema.css'].map(file=>fs.readFileSync(path.join(root,file),'utf8')).join('\n');
            const js=fs.readFileSync(path.join(root,'js/tema.js'),'utf8');
            html=html.replace('</head>',`<style>${css}</style><script>${js}</script></head>`);
            await route.fulfill({contentType:'text/html',body:html});
        });
        await page.goto('http://zuni.test/home');
        await page.locator('.zuni-theme-toggle').click();
        check(await page.evaluate(()=>localStorage.getItem('zuni-theme'))==='dark','preferencia persistida');
        for(const width of [1280,390,320]){
            await page.setViewportSize({width,height:900});
            let reference;
            for(const role of ['home','admin','director','psicologo','estudiante','alta']){
                await page.goto(`http://zuni.test/${role}`);
                check(await page.locator('html').getAttribute('data-theme')==='dark','tema conservado al navegar '+role+' '+width);
                check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'sin desborde '+role+' '+width);
                const language=await page.locator('.zuni-language button').boundingBox();
                const theme=await page.locator('.zuni-theme-toggle').boundingBox();
                check(Math.abs(theme.width-theme.height)<1&&Math.abs(language.width-language.height)<1,'controles cuadrados '+role+' '+width);
                check(theme.x>language.x&&Math.abs(theme.y-language.y)<1,'tema a la derecha del idioma '+role+' '+width);
                if(reference!==undefined)check(Math.abs(reference-theme.x)<1,'posición horizontal consistente '+role+' '+width);
                reference=theme.x;
            }
        }
        await page.locator('.zuni-theme-toggle').click();
        await page.reload();
        check(await page.locator('html').getAttribute('data-theme')==='light','regreso a claro conservado al recargar');
        await page.evaluate(()=>localStorage.setItem('zuni-theme','invalid'));
        await page.reload();
        check(await page.locator('html').getAttribute('data-theme')==='light','preferencia inválida vuelve al tema claro');
        console.log(`OK: ${checks} comprobaciones de tema persistente y controles fijos.`);
    }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
