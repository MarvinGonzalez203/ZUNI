// Ayuda de redacción del informe educativo: los factores marcados se agregan como texto editable
// al campo elegido. No se envían por sí mismos (las casillas no tienen name) ni se guardan sin que
// el psicólogo revise el texto y pulse Guardar borrador o Solicitar autorización.
(() => {
    const grupo = document.getElementById('factores-educativos');
    if (!grupo) return;
    const prefijo = grupo.dataset.prefijo || '';
    grupo.querySelectorAll('[data-agregar-factores]').forEach(boton => boton.addEventListener('click', () => {
        const destino = document.getElementById(boton.dataset.agregarFactores);
        const marcados = [...grupo.querySelectorAll('input[type="checkbox"][data-factor]:checked')].map(c => c.dataset.factor);
        if (!destino || marcados.length === 0) return;
        const linea = prefijo + ' ' + marcados.join('; ') + '.';
        if (destino.value.includes(linea)) return;
        const separador = destino.value.trim().length > 0 ? '\n' : '';
        const maximo = destino.maxLength > 0 ? destino.maxLength : Infinity;
        if ((destino.value + separador + linea).length > maximo) { destino.focus(); return; }
        destino.value = destino.value + separador + linea;
        destino.dispatchEvent(new Event('input', { bubbles: true }));
        destino.focus();
    }));
})();
