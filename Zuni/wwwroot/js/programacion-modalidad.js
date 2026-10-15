(() => {
    const mode=document.getElementById('Modalidad');const region=document.getElementById('programacion-lugar');const input=document.getElementById('DireccionAtencion');
    if(!mode||!region||!input)return;
    function update(){const presencial=mode.value==='Presencial';region.hidden=!presencial;input.disabled=!presencial;input.required=presencial;}
    mode.addEventListener('change',update);update();
})();
