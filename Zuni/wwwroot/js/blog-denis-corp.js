(() => {
    'use strict';
    const library = document.querySelector('[data-blog-library]');
    if (!library) return;
    const input = document.getElementById('blog-search'), topicField = document.getElementById('blog-topic');
    const articles = [...library.querySelectorAll('[data-blog-article]')];
    const links = [...library.querySelectorAll('[data-blog-topic]')];
    const normalize = s => s.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
    function paint(updateUrl = true) {
        const words = normalize(input.value.slice(0,80)).trim().split(/\s+/).filter(Boolean);
        let count = 0;
        for (const article of articles) {
            const match = (topicField.value === 'todos' || article.dataset.topic === topicField.value) && words.every(w => normalize([...article.querySelectorAll('h3,[data-blog-summary],details li')].map(e=>e.textContent).join(' ')).includes(w));
            article.hidden = !match; if (match) count++;
        }
        for (const link of links) {
            if (link.dataset.blogTopic === topicField.value) link.setAttribute('aria-current','true');
            else link.removeAttribute('aria-current');
        }
        library.querySelector('[data-blog-count]').textContent = count + (document.documentElement.lang.startsWith('en') ? ' guide(s) available' : ' guía(s) disponibles');
        library.querySelector('[data-blog-empty]').hidden = count !== 0;
        if (updateUrl) {
            const url = new URL(location.href);url.search = '';
            if (topicField.value !== 'todos') url.searchParams.set('tema',topicField.value);
            if (input.value.trim()) url.searchParams.set('q',input.value.trim().slice(0,80));
            history.replaceState(null,'',url.pathname+url.search+url.hash);
        }
    }
    input.addEventListener('input',()=>paint());
    input.form.addEventListener('submit',event=>{event.preventDefault();paint();});
    links.forEach(link=>link.addEventListener('click',event=>{event.preventDefault();topicField.value=link.dataset.blogTopic;paint();}));
    document.querySelector('[data-blog-featured]').addEventListener('click',event=>{
        event.preventDefault();input.value='';topicField.value='todos';paint();
        const article = document.getElementById('guia-elegir-programa');article.querySelector('details').open=true;
        history.replaceState(null,'',location.pathname+'#guia-elegir-programa');
        article.scrollIntoView({block:'start',behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth'});
    });
    document.addEventListener('zuni:languagechange',()=>paint(false));
    paint(false);
})();
