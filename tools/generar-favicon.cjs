// Composición de los iconos del sitio a partir del logotipo existente; no altera el original.
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('playwright');
(async()=>{
 const root=path.resolve(__dirname,'../Zuni/wwwroot');
 const png=fs.readFileSync(path.join(root,'images/zuni-logo-transparent.png')).toString('base64');
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
  const page=await browser.newPage();
  for(const [size,name] of [[96,'favicon-96.png'],[180,'apple-touch-icon.png'],[48,'favicon-48.png']]){
   const url=await page.evaluate(async({size,png})=>{
    const image=new Image();image.src='data:image/png;base64,'+png;await image.decode();
    const canvas=document.createElement('canvas');canvas.width=canvas.height=size;
    const c=canvas.getContext('2d');c.fillStyle='#fff';c.beginPath();c.arc(size/2,size/2,size/2,0,Math.PI*2);c.fill();
    const scale=size*.79/Math.max(image.width,image.height),w=image.width*scale,h=image.height*scale;
    c.drawImage(image,(size-w)/2,(size-h)/2,w,h);return canvas.toDataURL('image/png');
   },{size,png});
   fs.writeFileSync(path.join(root,name),Buffer.from(url.split(',')[1],'base64'));
  }
  const image=fs.readFileSync(path.join(root,'favicon-48.png')),header=Buffer.alloc(22);
  header.writeUInt16LE(1,2);header.writeUInt16LE(1,4);header[6]=header[7]=48;header.writeUInt16LE(1,10);header.writeUInt16LE(32,12);header.writeUInt32LE(image.length,14);header.writeUInt32LE(22,18);
  fs.writeFileSync(path.join(root,'favicon.ico'),Buffer.concat([header,image]));
  process.stdout.write('Iconos ZUNI generados con disco blanco y logotipo original.\n');
 }finally{await browser.close();}
})();
