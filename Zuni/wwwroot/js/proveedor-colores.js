(()=>{
 const form=document.querySelector('[data-proveedor-colores]');if(!form)return;
 const color=form.querySelector('#ColorPrincipal'),swatches=[...form.querySelectorAll('[data-color-proveedor]')];
 const preview=()=>{
  const rgb=color.value.slice(1).match(/../g).map(v=>parseInt(v,16)/255).map(v=>v<=.04045?v/12.92:((v+.055)/1.055)**2.4);
  const l=.2126*rgb[0]+.7152*rgb[1]+.0722*rgb[2];
  document.documentElement.style.setProperty('--institucion-color',color.value);
  document.documentElement.style.setProperty('--institucion-texto',(l+.05)/.05>=1.05/(l+.05)?'#000000':'#ffffff');
  swatches.forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.colorProveedor.toLowerCase()===color.value.toLowerCase())));
 };
 swatches.forEach(b=>b.addEventListener('click',()=>{color.value=b.dataset.colorProveedor;preview();}));color.addEventListener('input',preview);
})();
