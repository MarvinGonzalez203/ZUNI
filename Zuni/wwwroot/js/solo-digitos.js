document.addEventListener('input',e=>{if(e.target.matches('[data-only-digits]'))e.target.value=e.target.value.replace(/[^0-9]/g,'');});
