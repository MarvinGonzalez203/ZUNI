(()=>{
 const cue=document.querySelector('.dashboard-scroll-cue'),main=document.getElementById('dashboard-main'),bar=document.querySelector('.dashboard-topbar');if(!cue||!main||!bar)return;
 let queued=false;
 const update=()=>{
  queued=false;const height=bar.getBoundingClientRect().height;document.documentElement.style.setProperty('--zuni-topbar-height',height+'px');document.documentElement.style.scrollPaddingTop=(height+12)+'px';
  const first=main.querySelector('[data-question]:not([hidden])');
  const cutoff=first?Math.max(24,first.getBoundingClientRect().top+scrollY-height+24):24;
  cue.hidden=!!document.querySelector('.modal.show')||document.documentElement.scrollHeight-innerHeight-scrollY<36||scrollY>cutoff;
  const progress=main.querySelector('.test-navigation:not([hidden])'),r=progress?.getBoundingClientRect();
  cue.style.bottom=r&&r.top<innerHeight&&r.bottom>0?Math.max(12,innerHeight-r.top+8)+'px':'16px';
 };
 const request=()=>{if(!queued){queued=true;requestAnimationFrame(update);}};
 cue.addEventListener('click',()=>{main.focus({preventScroll:true});scrollBy({top:Math.max(240,innerHeight*.65),behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'auto':'smooth'});});
 addEventListener('scroll',request,{passive:true});addEventListener('resize',request);
 new ResizeObserver(request).observe(main);new ResizeObserver(request).observe(bar);
 new MutationObserver(request).observe(main,{subtree:true,attributes:true,attributeFilter:['hidden']});
 document.addEventListener('shown.bs.modal',request);document.addEventListener('hidden.bs.modal',request);request();
})();
