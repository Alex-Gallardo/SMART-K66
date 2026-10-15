(function (document, window) {
    'use strict';
    var modulo = document.querySelector('.crm');
    if (!modulo) return;

    function mostrarOpcionesEnMayusculas() {
        Array.prototype.forEach.call(modulo.querySelectorAll('select option'), function (opcion) {
            var texto = opcion.textContent || '';
            var mayusculas = texto.toLocaleUpperCase('es-GT');
            if (texto !== mayusculas) opcion.textContent = mayusculas;
        });
    }

    mostrarOpcionesEnMayusculas();
    if (window.MutationObserver) {
        new window.MutationObserver(mostrarOpcionesEnMayusculas)
            .observe(modulo, { childList: true, characterData: true, subtree: true });
    }
}(document, window));
