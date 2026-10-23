(function () {
    'use strict';
    var open = document.getElementById('qr-open');
    var dialog = document.getElementById('qr-dialog');
    var form = document.getElementById('plate-search-form');
    if (!open || !dialog || !form) return;

    var input = document.getElementById('placa');
    var video = document.getElementById('qr-video');
    var status = document.getElementById('qr-status');
    var file = document.getElementById('qr-file');
    var scanner = null;
    var active = false;
    var submitted = false;

    function plate(value) {
        var cleaned = String(value || '').trim().toUpperCase();
        return /^[A-Z0-9-]{3,15}$/.test(cleaned) ? cleaned : null;
    }
    function message(value) { status.textContent = value; }
    function release() {
        active = false;
        if (scanner) {
            scanner.stop();
            scanner.destroy();
            scanner = null;
        }
        if (video.srcObject) {
            video.srcObject.getTracks().forEach(function (track) { track.stop(); });
            video.srcObject = null;
        }
    }
    function close() {
        release();
        dialog.hidden = true;
        file.value = '';
        open.focus();
    }
    function accept(result) {
        if (!active || submitted) return;
        var value = plate(result && result.data !== undefined ? result.data : result);
        if (!value) {
            message('El QR no contiene una placa válida. Debe tener solo letras, números o guion, hasta 15 caracteres.');
            return;
        }
        submitted = true;
        input.value = value;
        release();
        dialog.hidden = true;
        form.submit();
    }
    open.addEventListener('click', function () {
        if (!window.QrScanner) {
            alert('El lector QR no está disponible. Escribe la placa para continuar.');
            input.focus();
            return;
        }
        submitted = false;
        active = true;
        dialog.hidden = false;
        message('Abriendo la cámara…');
        document.getElementById('qr-close').focus();
        try {
            scanner = new window.QrScanner(video, accept, { preferredCamera: 'environment', returnDetailedScanResult: true });
            scanner.start().then(function () {
                if (active) message('Apunta la cámara al QR de la placa.');
            }).catch(function () {
                if (active) message('No se pudo abrir la cámara. Permite el acceso en el navegador, usa una foto del QR o escribe la placa.');
            });
        } catch (error) {
            scanner = null;
            message('La cámara no está disponible. Usa una foto del QR o escribe la placa.');
        }
    });
    document.getElementById('qr-close').addEventListener('click', close);
    document.getElementById('qr-cancel').addEventListener('click', close);
    dialog.querySelector('.qr-backdrop').addEventListener('click', close);
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !dialog.hidden) close();
    });
    window.addEventListener('pagehide', release);
    file.addEventListener('change', function () {
        if (!active || !file.files || !file.files.length) return;
        message('Leyendo la foto…');
        window.QrScanner.scanImage(file.files[0], { returnDetailedScanResult: true }).then(accept).catch(function () {
            if (active) message('No se encontró un QR en la foto. Prueba otra imagen o escribe la placa.');
        });
    });
}());
