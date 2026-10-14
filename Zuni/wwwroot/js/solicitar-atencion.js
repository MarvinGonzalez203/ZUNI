(() => {
    const tipo = document.getElementById("TipoIngreso");
    const campos = document.getElementById("campos-referencia");
    if (!tipo || !campos) return;
    const actualizar = () => {
        const referencia = tipo.value === "1";
        campos.hidden = !referencia;
        campos.querySelectorAll('input, textarea').forEach(control => control.required = referencia);
    };
    tipo.addEventListener("change", actualizar);
    actualizar();
    document.querySelectorAll('[data-contacto-emergencia]').forEach(grupo => {
        const inputs = [...grupo.querySelectorAll('input')];
        const actualizarContacto = () => {
            const iniciado = inputs.some(input => input.value.trim() !== '');
            inputs.forEach(input => input.required = iniciado);
        };
        grupo.addEventListener('input', actualizarContacto);
        actualizarContacto();
    });
    // Sin JavaScript los campos permanecen visibles; el servidor siempre valida.
})();
