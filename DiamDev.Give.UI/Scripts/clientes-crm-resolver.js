(function (document) {
    'use strict';
    var modal = document.getElementById('crmAprobacionModal');
    var abrir = document.getElementById('crmAbrirAprobacion');
    if (!modal || !abrir) return;
    var cerrar = document.getElementById('crmCerrarAprobacion');
    var cancelar = document.getElementById('crmCancelarAprobacion');
    var confirmar = document.getElementById('crmConfirmarAprobacion');
    var codigo = document.getElementById('crmCodigoSapAprobacion');
    var formulario = document.getElementById('crmResolverForm');
    var focoAnterior;
    var enviando = false;

    function cerrarModal() {
        modal.hidden = true;
        modal.classList.remove('is-visible');
        codigo.disabled = true;
        codigo.value = '';
        codigo.setCustomValidity('');
        abrir.setAttribute('aria-expanded', 'false');
        if (focoAnterior && document.contains(focoAnterior)) focoAnterior.focus();
    }

    abrir.addEventListener('click', function () {
        focoAnterior = document.activeElement;
        codigo.disabled = false;
        modal.hidden = false;
        modal.classList.add('is-visible');
        abrir.setAttribute('aria-expanded', 'true');
        codigo.focus();
    });
    cerrar.addEventListener('click', cerrarModal);
    cancelar.addEventListener('click', cerrarModal);
    modal.addEventListener('click', function (e) { if (e.target === modal) cerrarModal(); });
    modal.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { e.preventDefault(); cerrarModal(); return; }
        if (e.key !== 'Tab') return;
        var elementos = [cerrar, codigo, cancelar, confirmar];
        var primero = elementos[0], ultimo = elementos[elementos.length - 1];
        if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo.focus(); }
        if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero.focus(); }
    });
    codigo.addEventListener('input', function () { codigo.setCustomValidity(''); });
    codigo.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); confirmar.click(); }
    });
    confirmar.addEventListener('click', function (e) {
        if (enviando) { e.preventDefault(); return; }
        codigo.value = codigo.value.trim();
        codigo.setCustomValidity(codigo.value ? '' : 'Ingrese el código de cliente SAP.');
        if (!codigo.checkValidity()) { e.preventDefault(); codigo.reportValidity(); codigo.focus(); }
    });
    formulario.addEventListener('submit', function (e) {
        if (!e.submitter || e.submitter.value !== 'APROBAR') return;
        if (enviando) { e.preventDefault(); return; }
        if (!codigo.value.trim()) {
            e.preventDefault();
            codigo.setCustomValidity('Ingrese el código de cliente SAP.');
            codigo.reportValidity();
            codigo.focus();
            return;
        }
        enviando = true;
        confirmar.textContent = 'Aprobando...';
        modal.classList.add('is-saving');
    });
}(document));
