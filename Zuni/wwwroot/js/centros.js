document.addEventListener('click',event=>{
    const button=event.target.closest('[data-extend-months]');if(!button)return;
    const end=document.getElementById('Fin');if(!end?.value||!end.validity.valid)return;
    const [y,m,d]=end.value.split('-').map(Number),months=Number(button.dataset.extendMonths);
    const first=new Date(Date.UTC(y,m-1+months,1)),last=new Date(Date.UTC(first.getUTCFullYear(),first.getUTCMonth()+1,0)).getUTCDate();
    const result=new Date(Date.UTC(first.getUTCFullYear(),first.getUTCMonth(),Math.min(d,last))).toISOString().slice(0,10);
    if(result<='2200-12-31'){end.value=result;end.dispatchEvent(new Event('input',{bubbles:true}));end.dispatchEvent(new Event('change',{bubbles:true}));}
});
