(function (document, navigator, window) {
    'use strict';
    var ficha = document.querySelector('.crm-client-ficha');
    if (!ficha) return;
    var estado = document.getElementById('crmCopyStatus');
    var temporizador;

    function texto(elemento) {
        return (elemento.textContent || '').replace(/\s+/g, ' ').trim();
    }

    function avisar(mensaje, error) {
        if (!estado) return;
        window.clearTimeout(temporizador);
        estado.textContent = mensaje;
        estado.classList.toggle('is-error', !!error);
        estado.classList.add('is-visible');
        temporizador = window.setTimeout(function () {
            estado.classList.remove('is-visible');
        }, 2600);
    }

    function copiarConSeleccion(valor) {
        var anterior = document.activeElement;
        var temporal = document.createElement('textarea');
        temporal.value = valor;
        temporal.setAttribute('readonly', '');
        temporal.style.position = 'fixed';
        temporal.style.left = '-9999px';
        document.body.appendChild(temporal);
        temporal.focus();
        temporal.select();
        temporal.setSelectionRange(0, valor.length);
        var copiado = false;
        try {
            copiado = document.execCommand('copy');
        } finally {
            document.body.removeChild(temporal);
            if (anterior && anterior.focus) anterior.focus();
        }
        if (!copiado) throw new Error('No se pudo copiar el dato.');
    }

    function copiar(valor) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(valor).then(function () {
                avisar('Dato copiado al portapapeles.', false);
            }, function () {
                try {
                    copiarConSeleccion(valor);
                    avisar('Dato copiado al portapapeles.', false);
                } catch (error) {
                    avisar('No se pudo copiar. Seleccione el texto y cópielo manualmente.', true);
                }
            });
            return;
        }
        try {
            copiarConSeleccion(valor);
            avisar('Dato copiado al portapapeles.', false);
        } catch (error) {
            avisar('No se pudo copiar. Seleccione el texto y cópielo manualmente.', true);
        }
    }

    var valores = ficha.querySelectorAll(
        '.crm-head-row h1, .crm-record-number, ' +
        '.crm-detail-main .crm-data-grid dd, .crm-detail-main .crm-table tbody td, ' +
        '.crm-detail-main .crm-detail-metrics strong, ' +
        '.crm-detail-aside .crm-side-card:first-child dl dd, [data-crm-copy-value]');
    Array.prototype.forEach.call(valores, function (elemento) {
        if (elemento.querySelector('[data-crm-copy-value]')) return;
        var valor = texto(elemento);
        if (!valor || valor === '—' || valor === 'Pendiente de vincular') return;
        var boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'crm-copyable';
        boton.title = 'Copiar dato';
        boton.setAttribute('aria-label', 'Copiar ' + valor);
        while (elemento.firstChild) boton.appendChild(elemento.firstChild);
        elemento.appendChild(boton);
        boton.addEventListener('click', function () { copiar(valor); });
    });
}(document, navigator, window));
