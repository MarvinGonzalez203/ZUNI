// Playwright debe estar disponible en NODE_PATH. La sesión firmada se recibe por stdin, nunca se imprime.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
async function main() {
 let input='';for await(const chunk of process.stdin)input+=chunk;
 const {cookie,id,questions,origin}=JSON.parse(input);
 const browser=await chromium.launch({channel:'msedge',headless:true});
 let count=0;function check(ok,name){assert.ok(ok,name);count++;console.log('BROWSER OK: '+name);}
 try {
  const context=await browser.newContext();await context.addCookies([{name:'.AspNetCore.Cookies',value:cookie,url:origin,httpOnly:true,sameSite:'Lax'}]);
  const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto(origin+'/Estudiante/Evaluaciones');check(await page.locator('article[data-estado="EnProceso"]').count()===2,'Juan ve la pendiente iniciada y Hábitos en proceso');
  await page.goto(origin+'/Estudiante/Evaluaciones/'+id);
  check((await page.locator('#avance').innerText()).includes('33 %'),'Progreso persistido 33 %');
  check(await page.locator('[name="respuestas['+questions[0]+']"]').inputValue()==='2','Respuesta inicial recuperada');
  await page.locator('[name="respuestas['+questions[1]+']"]').selectOption('4');
  check((await page.locator('#avance').innerText()).includes('67 %'),'Progreso interactivo 67 %');
  await Promise.all([page.waitForURL('**/Estudiante/Evaluaciones/'+id),page.getByRole('button',{name:'Guardar avance',exact:true}).click()]);
  check((await page.locator('#avance').innerText()).includes('67 %'),'Recarga tras guardar conserva 67 %');
  await page.getByRole('button',{name:'Cerrar sesión',exact:true}).click();await page.waitForURL(url=>url.pathname==='/' || url.pathname==='/Home');
  await page.goto(origin+'/Estudiante/Evaluaciones/'+id);check(page.url().includes('IniciarSesion'),'Cerrar sesión impide acceso posterior');
  // Nueva sesión en contexto independiente: sin cookies de la sesión anterior.
  await context.close();const fresh=await browser.newContext();await fresh.addCookies([{name:'.AspNetCore.Cookies',value:cookie,url:origin,httpOnly:true,sameSite:'Lax'}]);
  const resumed=await fresh.newPage();resumed.on('pageerror',e=>errors.push(e.message));await resumed.goto(origin+'/Estudiante/Evaluaciones/'+id);
  check(await resumed.locator('[name="respuestas['+questions[1]+']"]').inputValue()==='4','Nueva sesión recupera PostgreSQL después de salir');
  await resumed.locator('[name="respuestas['+questions[2]+']"]').selectOption('5');check((await resumed.locator('#avance').innerText()).includes('100 %'),'Progreso completo 100 %');
  await Promise.all([resumed.waitForURL('**/revisar'),resumed.getByRole('button',{name:'Guardar y revisar respuestas'}).click()]);
  await resumed.getByRole('button',{name:'Finalizar evaluación',exact:true}).click();check(await resumed.locator('dialog').evaluate(d=>d.open),'Confirmación abre un diálogo modal real');
  await resumed.getByRole('button',{name:'Cancelar',exact:true}).click();check(!await resumed.locator('dialog').evaluate(d=>d.open),'Cancelar cierra el diálogo sin envío');
  await resumed.getByRole('link',{name:'Volver a editar'}).click();check((await resumed.locator('#avance').innerText()).includes('100 %'),'Cancelar conserva En proceso y todas las respuestas');
  for(let i=0;i<3;i++)check(await resumed.locator('[name="respuestas['+questions[i]+']"]').inputValue()===['2','4','5'][i],'Cancelar conserva respuesta '+(i+1));
  await Promise.all([resumed.waitForURL('**/revisar'),resumed.getByRole('button',{name:'Guardar y revisar respuestas'}).click()]);
  await resumed.getByRole('button',{name:'Finalizar evaluación',exact:true}).click();
  await Promise.all([resumed.waitForURL('**/Estudiante/Evaluaciones/'+id),resumed.getByRole('button',{name:'Confirmar y finalizar',exact:true}).click()]);
  check((await resumed.locator('body').innerText()).includes('Comprobante:'),'Confirmación devuelve comprobante');check(await resumed.locator('#respuestas-form').count()===0,'Finalizada no ofrece campos editables');
  await resumed.screenshot({path:'.visual-check/juan-verification/finalizada.png',fullPage:true});
  check(errors.length===0,'Sin errores JavaScript de página');
  await fresh.close();console.log('BROWSER COMPLETADO: '+count+' comprobaciones.');
 } finally {await browser.close();}
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
