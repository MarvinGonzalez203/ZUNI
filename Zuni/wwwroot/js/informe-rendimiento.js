(() => {
    const select=document.getElementById('IncluyeAnalisisRendimiento');
    const section=document.getElementById('rendimiento-campos');
    if(!select||!section)return;
    function update(){const active=select.value==='true';section.hidden=!active;section.querySelectorAll('textarea,input[type=checkbox]').forEach(input=>{input.disabled=!active;input.required=active;});}
    const confirmation=document.getElementById('ContrasteRendimientoConfirmado');
    section.querySelectorAll('textarea').forEach(input=>input.addEventListener('input',()=>{confirmation.checked=false;}));
    select.addEventListener('change',()=>{confirmation.checked=false;update();});update();
})();
