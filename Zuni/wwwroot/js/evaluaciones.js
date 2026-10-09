(() => {
 'use strict';
 const form=document.getElementById('respuestas-form');
 if(form){
  const questions=Array.from(form.querySelectorAll('.pregunta'));const selects=Array.from(form.querySelectorAll('select'));let current=Math.max(0,selects.findIndex(s=>!s.value));let dirty=false;let submitting=false;
  const previous=document.getElementById('anterior'),next=document.getElementById('siguiente');
  function show(){questions.forEach((q,i)=>q.hidden=i!==current);previous.disabled=current===0;next.disabled=current===questions.length-1;}
  previous.hidden=false;next.hidden=false;previous.addEventListener('click',()=>{current=Math.max(0,current-1);show();});next.addEventListener('click',()=>{current=Math.min(questions.length-1,current+1);show();});show();
  form.addEventListener('change',()=>{dirty=true;const done=selects.filter(s=>s.value).length;document.getElementById('progreso').value=done;document.getElementById('avance').textContent=`${done} de ${questions.length} preguntas respondidas · ${questions.length ? Math.round(100 * done / questions.length) : 0} %`;document.getElementById('guardado-aviso').textContent='Tienes cambios sin guardar.';});
  window.addEventListener('beforeunload',e=>{if(dirty&&!submitting){e.preventDefault();e.returnValue='';}});
  form.addEventListener('submit',e=>{if(submitting){e.preventDefault();return;}submitting=true;document.getElementById('guardado-aviso').textContent='Guardando…';});
  window.addEventListener('pageshow',()=>{submitting=false;});
 }
 const dialog=document.getElementById('confirmacion-finalizar'),open=document.getElementById('abrir-confirmacion'),cancel=document.getElementById('cancelar-finalizacion'),finalForm=document.getElementById('finalizar-form');
 if(dialog&&open&&cancel&&finalForm){open.hidden=false;open.addEventListener('click',()=>dialog.showModal());cancel.addEventListener('click',()=>dialog.close());let sending=false;finalForm.addEventListener('submit',e=>{if(sending){e.preventDefault();return;}sending=true;finalForm.querySelector('button').disabled=true;});window.addEventListener('pageshow',()=>{sending=false;finalForm.querySelector('button').disabled=false;});}
})();
