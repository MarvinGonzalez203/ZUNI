(() => {
    const editor = document.getElementById('prueba-editor');
    if (!editor) return;
    const dimensiones = document.getElementById('dimensiones');
    const preguntas = document.getElementById('preguntas');
    const append = (parent, template) => {
        const node = document.getElementById(template).content.firstElementChild.cloneNode(true);
        parent.append(node);
        return node;
    };
    const renumerar = () => {
        const lista = document.getElementById('dimension-nombres');
        lista.replaceChildren();
        [...dimensiones.children].forEach((d, i) => {
            const input = d.querySelector('input');
            input.name = `Dimensiones[${i}].Nombre`;
            const option = document.createElement('option');
            option.value = input.value;
            lista.append(option);
        });
        [...preguntas.children].forEach((q, i) => {
            q.querySelector('legend').textContent = window.ZuniI18n.t('Pregunta {0}',i+1);
            q.querySelector('textarea').name = `Preguntas[${i}].Texto`;
            q.querySelector('[data-field="Dimension"]').name = `Preguntas[${i}].Dimension`;
            [...q.querySelector('[data-opciones]').children].forEach((o, j) => {
                o.querySelector('[data-field="Texto"]').name = `Preguntas[${i}].Opciones[${j}].Texto`;
                o.querySelector('[data-field="Valor"]').name = `Preguntas[${i}].Opciones[${j}].Valor`;
            });
        });
    };
    editor.addEventListener('click', event => {
        const button = event.target.closest('button');
        if (!button) return;
        let created;
        if (button.id === 'agregar-dimension') {
            if (dimensiones.children.length >= 30) return;
            created = append(dimensiones, 'dimension-template');
        }
        if (button.id === 'agregar-pregunta') {
            if (preguntas.children.length >= 200) return;
            created = append(preguntas, 'pregunta-template');
            append(created.querySelector('[data-opciones]'), 'opcion-template');
            append(created.querySelector('[data-opciones]'), 'opcion-template');
        }
        if (button.hasAttribute('data-add-opcion')) {
            const options = button.closest('[data-pregunta]').querySelector('[data-opciones]');
            if (options.children.length >= 20) return;
            created = append(options, 'opcion-template');
        }
        if (button.hasAttribute('data-remove')) button.closest('[data-opcion], [data-pregunta], [data-dimension]').remove();
        renumerar();
        created?.querySelector('input, textarea')?.focus();
    });
    dimensiones.addEventListener('input', renumerar);
    editor.addEventListener('submit', renumerar);
    renumerar();
})();
