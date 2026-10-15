document.querySelectorAll('form[data-questionnaire]').forEach(form => {
    const questions = [...form.querySelectorAll('[data-question]')];
    const nav = form.querySelector('.test-navigation');
    if (!questions.length || !nav) return;
    const pages = Math.ceil(questions.length / 3), readonly = form.dataset.readonly === 'true';
    const en = document.documentElement.lang.startsWith('en');
    const back = nav.querySelector('[data-test-back]'), next = nav.querySelector('[data-test-next]');
    const error = nav.querySelector('[data-test-error]');
    let page = 0;
    const answered = q => !!q.querySelector('input:checked');
    const firstEmpty = questions.findIndex(q => !answered(q));
    if (!readonly && firstEmpty >= 0) page = Math.floor(firstEmpty / 3);
    function show(focus = false) {
        questions.forEach((q,i) => { q.hidden = Math.floor(i / 3) !== page; });
        const count = questions.filter(answered).length;
        nav.hidden = false;
        nav.querySelector('[data-test-progress]').value = count / questions.length * 100;
        nav.querySelector('[data-test-status]').textContent = en ? `${count} of ${questions.length} answered · ${Math.round(count / questions.length * 100)}%` : `${count} de ${questions.length} respondidas · ${Math.round(count / questions.length * 100)}%`;
        nav.querySelector('[data-test-page]').textContent = en ? `Page ${page+1} of ${pages}` : `Pantalla ${page+1} de ${pages}`;
        back.disabled = page === 0; next.hidden = page === pages - 1;
        form.querySelectorAll('[data-test-final]').forEach(el => { el.hidden = page !== pages - 1; });
        error.hidden = true;
        if (focus) { const first = questions[page*3]; first.setAttribute('tabindex','-1'); first.focus({preventScroll:true}); first.scrollIntoView({block:'start',behavior:'smooth'}); }
    }
    back.addEventListener('click', () => { if(page > 0) { page--; show(true); } });
    next.addEventListener('click', () => {
        if(!readonly) {
            const empty = questions.slice(page*3, page*3+3).find(q => !answered(q));
            if(empty) { error.hidden = false; empty.querySelector('input').focus(); return; }
        }
        if(page < pages-1) { page++; show(true); }
    });
    form.addEventListener('change', () => show());
    form.addEventListener('submit', event => {
        if (event.submitter?.value === 'false') return;
        const empty = questions.findIndex(q => !answered(q));
        if(empty >= 0) { event.preventDefault(); page = Math.floor(empty/3); show(true); error.hidden = false; }
    });
    show();
});
