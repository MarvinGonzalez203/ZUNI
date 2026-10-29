(() => {
    document.addEventListener('submit',async event=>{
        const form=event.target.closest('[data-release-read]');if(!form)return;event.preventDefault();
        const button=form.querySelector('button');button.disabled=true;
        try {
            const response=await fetch(form.action,{method:'POST',body:new FormData(form),credentials:'same-origin',redirect:'error'});
            if(response.status!==204)throw new Error();
            form.closest('.release-control').querySelectorAll('.release-dot,.release-unread').forEach(e=>e.remove());form.hidden=true;
        }catch{form.querySelector('.release-status').textContent=document.documentElement.lang==='en'?'Try again.':'Intenta nuevamente.';button.disabled=false;}
    });
    document.addEventListener('click',event=>document.querySelectorAll('.release-control[open]').forEach(e=>{if(!e.contains(event.target))e.open=false;}));
    document.addEventListener('keydown',event=>{if(event.key==='Escape')document.querySelectorAll('.release-control[open]').forEach(e=>{e.open=false;e.querySelector('summary').focus({preventScroll:true});});});
})();
