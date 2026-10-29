(() => {
    const sidebar=document.getElementById('dashboard-sidebar');
    if(!sidebar)return;
    const region=sidebar.querySelector('[data-sidebar-scroll]'),toggle=document.querySelector('.sidebar-collapse'),cue=sidebar.querySelector('.sidebar-scroll-cue');
    const scope=document.documentElement.dataset.viewScope||'cuenta';
    const key='zuni:sidebar-compact:'+scope;
    const en=()=>document.documentElement.lang.startsWith('en');
    let compact=false;
    try{compact=localStorage.getItem(key)==='true';}catch{}
    function update(){
        document.body.classList.toggle('sidebar-compact',compact&&innerWidth>=992);
        toggle?.setAttribute('aria-expanded',String(!compact));
        if(toggle){toggle.title=en()?(compact?'Expand menu':'Collapse menu'):(compact?'Expandir menú':'Comprimir menú');toggle.setAttribute('aria-label',toggle.title);}
        if(cue)cue.hidden=region.scrollHeight-region.clientHeight-region.scrollTop<=16;
        sidebar.querySelectorAll('.dash-nav-link,summary,.dash-bottom-action').forEach(e=>{
            const label=e.querySelector('span:not(.dash-round-control):not(.dash-avatar)')?.textContent?.trim()||e.textContent.trim();
            if(label){e.title=label;if(!e.getAttribute('aria-label'))e.setAttribute('aria-label',label);}
        });
    }
    toggle?.addEventListener('click',()=>{
        const top=region.scrollTop,x=scrollX,y=scrollY;
        compact=!compact;try{localStorage.setItem(key,String(compact));}catch{}update();
        region.scrollTop=top;scrollTo({left:x,top:y,behavior:'instant'});
        requestAnimationFrame(()=>{region.scrollTop=top;scrollTo({left:x,top:y,behavior:'instant'});});
    });
    cue?.addEventListener('click',()=>region.scrollBy({top:Math.max(140,region.clientHeight*.65),behavior:matchMedia('(prefers-reduced-motion:reduce)').matches?'auto':'smooth'}));
    region.addEventListener('scroll',update,{passive:true});
    window.addEventListener('resize',update);
    document.addEventListener('zuni:languagechange',update);
    sidebar.addEventListener('toggle',()=>requestAnimationFrame(update),true);
    new ResizeObserver(update).observe(region);
    sidebar.addEventListener('shown.bs.offcanvas',update);
    update();
})();
