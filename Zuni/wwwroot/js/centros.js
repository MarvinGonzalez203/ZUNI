(() => {
    const form = document.querySelector('[data-contract-plan]');
    if (!form) return;
    const months = form.querySelector('[name=MesesPlan]'), start = form.querySelector('[name=Inicio]'), end = form.querySelector('[name=Fin]'), amount = form.querySelector('[name=ImporteGtq]'), renew = form.querySelector('[name=Renovar]');
    const prices = JSON.parse(form.dataset.prices), today = form.dataset.today;
    function calculate() {
        const count = Number(months.value);
        if (![1, 3, 6, 12].includes(count)) return;
        const [y, m, d] = today.split('-').map(Number);
        const first = new Date(Date.UTC(y, m - 1 + count, 1));
        const last = new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth() + 1, 0)).getUTCDate();
        const anniversary = new Date(Date.UTC(first.getUTCFullYear(), first.getUTCMonth(), Math.min(d, last)));
        anniversary.setUTCDate(anniversary.getUTCDate() - 1);
        start.value = today; end.value = anniversary.toISOString().slice(0, 10);
        amount.value = Number(prices[count]).toFixed(2); renew.value = 'true';
        for (const input of [start, end, amount]) input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    months.addEventListener('change', calculate);
    form.querySelector('[data-renew-contract]')?.addEventListener('click', calculate);
})();
