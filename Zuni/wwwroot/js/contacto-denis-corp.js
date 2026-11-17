(() => {
    'use strict';
    const form = document.querySelector('[data-corp-contact]');
    if (!form) return;
    const name=document.getElementById('contact-name'), organization=document.getElementById('contact-organization'),
        message=document.getElementById('contact-message'),program=document.getElementById('contact-program'),topic=document.getElementById('contact-topic');
    function prepare() {
        const en=document.documentElement.lang.startsWith('en');
        const body=[(en?'Name: ':'Nombre: ')+name.value.trim(),
            ...(organization.value.trim()?[(en?'Organization: ':'Organización: ')+organization.value.trim()]:[]),
            (en?'Program: ':'Programa: ')+program.selectedOptions[0].textContent,
            (en?'Topic: ':'Tema: ')+topic.selectedOptions[0].textContent,'',message.value.trim()].join('\n');
        form.querySelector('[data-contact-channel="whatsapp"]').href='https://wa.me/'+form.dataset.phone+'?text='+encodeURIComponent(body);
        form.querySelector('[data-contact-channel="email"]').href='mailto:'+form.dataset.email+'?subject='+encodeURIComponent('Denis Corp · '+topic.selectedOptions[0].textContent)+'&body='+encodeURIComponent(body);
    }
    form.addEventListener('submit',event=>event.preventDefault());
    form.addEventListener('input',prepare);form.addEventListener('change',prepare);
    form.querySelectorAll('[data-contact-channel]').forEach(link=>link.addEventListener('click',event=>{
        name.setCustomValidity(name.value.trim()?'':(document.documentElement.lang.startsWith('en')?'Enter your name.':'Escribe tu nombre.'));
        message.setCustomValidity(message.value.trim()?'':(document.documentElement.lang.startsWith('en')?'Write your message.':'Escribe tu mensaje.'));
        if(!form.reportValidity()){event.preventDefault();return;}
        prepare();form.querySelector('[role="status"]').textContent=document.documentElement.lang.startsWith('en')?'Review your message in the selected app before sending.':'Revisa tu mensaje en la aplicación elegida antes de enviarlo.';
    }));
    for(const field of [name,message])field.addEventListener('input',()=>field.setCustomValidity(''));
    document.addEventListener('zuni:languagechange',()=>{name.setCustomValidity('');message.setCustomValidity('');prepare();});
    prepare();
})();
