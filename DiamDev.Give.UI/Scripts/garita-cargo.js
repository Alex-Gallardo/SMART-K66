(function () {
    'use strict';
    var dialog = document.getElementById('cargo-document-dialog');
    if (!dialog) return;

    var close = document.getElementById('cargo-dialog-close');
    var body = dialog.querySelector('.cargo-dialog-body');
    var photoMessage = document.getElementById('cargo-dialog-no-photo');
    var photos = [
        { figure: document.getElementById('cargo-dialog-photo-doc'), key: 'fotoDoc' },
        { figure: document.getElementById('cargo-dialog-photo-client'), key: 'fotoCliente' }
    ];
    var lastTrigger;

    function put(field, value) {
        document.getElementById('cargo-dialog-' + field).textContent = value || 'Sin registrar';
    }

    function closeDocument() {
        dialog.close();
        photos.forEach(function (photo) {
            photo.figure.hidden = true;
            photo.image.removeAttribute('src');
        });
        if (lastTrigger) lastTrigger.focus({ preventScroll: true });
        lastTrigger = null;
    }

    document.querySelectorAll('.cargo-document').forEach(function (button) {
        button.addEventListener('click', function () {
            var data = button.dataset;
            lastTrigger = button;
            put('title', data.documento || 'Documento sin número');
            put('subtitle', (data.tipo || 'Documento') + ' · ' + (data.empresa || 'Empresa sin registrar'));
            put('source', data.fuente);
            var result = document.getElementById('cargo-dialog-result');
            result.textContent = data.resultado || 'PENDIENTE';
            var state = ['delivered', 'warning'].indexOf(data.clase) >= 0 ? data.clase : 'pending';
            result.className = 'cargo-result cargo-result-' + state;
            ['cliente', 'direccion', 'bultos', 'visito', 'fecha', 'fel', 'monto', 'motivo', 'observacion'].forEach(function (field) {
                put(field, data[field]);
            });
            put('horas', (data.entrada || 'Sin registrar') + ' / ' + (data.salida || 'Sin registrar'));
            photoMessage.textContent = 'Sin fotografías adjuntas.';
            photoMessage.hidden = photos.some(function (photo) { return !!data[photo.key]; });
            photos.forEach(function (photo) {
                var url = data[photo.key];
                photo.figure.hidden = !url;
                if (url) photo.image.src = url;
            });
            dialog.showModal();
            body.scrollTop = 0;
            close.focus({ preventScroll: true });
        });
    });

    photos.forEach(function (photo) {
        photo.image = photo.figure.querySelector('img');
        photo.image.addEventListener('error', function () {
            if (!dialog.open || photo.figure.hidden) return;
            photo.figure.hidden = true;
            photoMessage.hidden = false;
            photoMessage.textContent = 'No se pudo cargar una fotografía. Vuelve a abrir la ruta.';
        });
    });
    close.addEventListener('click', closeDocument);
    dialog.addEventListener('cancel', function (event) {
        event.preventDefault();
        closeDocument();
    });
    dialog.addEventListener('click', function (event) {
        if (event.target === dialog) closeDocument();
    });
})();
