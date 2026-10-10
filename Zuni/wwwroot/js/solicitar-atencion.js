(() => {
    const tipo = document.getElementById("TipoIngreso");
    const campos = document.getElementById("campos-referencia");
    if (!tipo || !campos) return;
    const actualizar = () => { campos.hidden = tipo.value !== "1"; };
    tipo.addEventListener("change", actualizar);
    actualizar();
    // Sin JavaScript los campos permanecen visibles; el servidor siempre valida.
})();
