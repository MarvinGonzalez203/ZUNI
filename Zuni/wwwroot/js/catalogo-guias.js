(() => {
    let controller;
    const en=()=>document.documentElement.lang.startsWith('en');
    async function filter(url,historyNavigation=false){
        if(window.ZuniLanguageChanging)return;
        const form=document.querySelector('[data-guide-search]'),results=document.querySelector('[data-guide-results]');
        if(!form||!results)return;
        controller?.abort();controller=new AbortController();const signal=controller.signal;
        form.setAttribute('aria-busy','true');const status=form.querySelector('.guide-status');status.textContent='';
        try{
            const response=await fetch(url,{signal,credentials:'same-origin',cache:'no-store',headers:{Accept:'text/html'}});
            if(!response.ok||new URL(response.url).pathname!==location.pathname)throw new Error('Guide unavailable');
            const html=new DOMParser().parseFromString(await response.text(),'text/html');
            const replacement=html.querySelector('[data-guide-results]');
            if(signal.aborted)return;
            if(!replacement||html.documentElement.lang!==document.documentElement.lang||html.querySelector('script[data-view-scope]')?.dataset.viewScope!==document.querySelector('script[data-view-scope]')?.dataset.viewScope)throw new Error('Guide context changed');
            const y=scrollY,x=scrollX;
            results.replaceChildren(...replacement.childNodes);
            if(!historyNavigation)history.pushState({guideCatalog:true},'',url);
            document.dispatchEvent(new CustomEvent('zuni:viewloaded',{detail:{document:html}}));
            scrollTo({left:x,top:y,behavior:'instant'});
        }catch(error){if(error.name!=='AbortError')status.textContent=en()?'The guides could not be updated. Try again.':'No se pudieron actualizar las guías. Intenta nuevamente.';}
        finally{if(!signal.aborted)form.removeAttribute('aria-busy');}
    }
    function destination(form){const url=new URL(location.pathname,location.origin);for(const [k,v] of new FormData(form))if(String(v).trim())url.searchParams.set(k,String(v).trim());return url;}
    document.addEventListener('submit',event=>{if(event.target.matches('[data-guide-search]')){event.preventDefault();filter(destination(event.target));}});
    document.addEventListener('change',event=>{if(event.target.matches('select[data-guide-role]'))filter(destination(event.target.form));});
    document.addEventListener('click',event=>{const a=event.target.closest('[data-guide-reset]');if(a&&!event.ctrlKey&&!event.metaKey){event.preventDefault();const form=a.closest('form');form.querySelector('input[name=q]').value='';const role=form.querySelector('select[name=rol]');if(role)role.value='';filter(new URL(a.href));}});
    window.addEventListener('popstate',()=>{const form=document.querySelector('[data-guide-search]');if(!form)return;const query=new URL(location.href).searchParams;form.querySelector('input[name=q]').value=query.get('q')||'';const role=form.querySelector('select[name=rol]');if(role)role.value=query.get('rol')||'';filter(new URL(location.href),true);});
    document.addEventListener('zuni:languagechanging',()=>{controller?.abort();document.querySelector('[data-guide-search]')?.removeAttribute('aria-busy');});
})();
