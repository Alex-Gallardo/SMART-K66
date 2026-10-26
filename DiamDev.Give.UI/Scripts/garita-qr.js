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
    var gallery = document.getElementById('qr-gallery');
    var scanner = null;
    var active = false;
    var submitted = false;

    function plate(value) {
        var cleaned = String(value || '').trim().toUpperCase();
        return /^[A-Z0-9-]{3,15}$/.test(cleaned) ? cleaned : null;
    }
    function message(value) { status.textContent = value; }
    function stopCamera() {
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
    function release() {
        active = false;
        stopCamera();
    }
    function cameraError(error) {
        var name = error && error.name;
        if (name === 'NotAllowedError' || name === 'PermissionDeniedError')
            return 'El navegador no tiene permiso para usar la cámara. Permite el acceso en la configuración del sitio y vuelve a intentarlo.';
        if (name === 'NotFoundError' || name === 'DevicesNotFoundError')
            return 'No se encontró una cámara en este dispositivo. Usa una foto del QR o escribe la placa.';
        if (name === 'NotReadableError' || name === 'TrackStartError')
            return 'Otra aplicación está usando la cámara o el dispositivo no pudo iniciarla. Cierra esa aplicación y vuelve a intentarlo.';
        return 'No se pudo abrir la cámara. Usa una foto del QR o escribe la placa.';
    }
    function close() {
        release();
        dialog.hidden = true;
        file.value = '';
        gallery.value = '';
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
        document.getElementById('qr-close').focus();
        if (window.isSecureContext === false) {
            video.hidden = true;
            message('El escaneo en vivo requiere HTTPS. En esta dirección HTTP, toma una foto del QR con la cámara del teléfono o elige una imagen guardada.');
            file.value = '';
            try { file.click(); }
            catch (error) { message('El escaneo en vivo requiere HTTPS. Pulsa «Tomar foto del QR» o elige una imagen guardada.'); }
            return;
        }
        if (!window.navigator || !window.navigator.mediaDevices || !window.navigator.mediaDevices.getUserMedia) {
            video.hidden = true;
            message('Este navegador no ofrece acceso a la cámara. Usa una foto del QR o escribe la placa.');
            return;
        }
        video.hidden = false;
        message('Abriendo la cámara…');
        try {
            scanner = new window.QrScanner(video, accept, { preferredCamera: 'environment', returnDetailedScanResult: true });
            scanner.start().then(function () {
                if (active) message('Apunta la cámara al QR de la placa.');
            }).catch(function (error) {
                if (active) {
                    stopCamera();
                    video.hidden = true;
                    message(cameraError(error));
                }
            });
        } catch (error) {
            stopCamera();
            video.hidden = true;
            message(cameraError(error));
        }
    });
    document.getElementById('qr-close').addEventListener('click', close);
    document.getElementById('qr-cancel').addEventListener('click', close);
    dialog.querySelector('.qr-backdrop').addEventListener('click', close);
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !dialog.hidden) close();
    });
    window.addEventListener('pagehide', release);
    function scanFile(event) {
        var selected = event.currentTarget;
        if (!active || !selected.files || !selected.files.length) return;
        var image = selected.files[0];
        selected.value = '';
        message('Leyendo la foto…');
        window.QrScanner.scanImage(image, { returnDetailedScanResult: true }).then(accept).catch(function () {
            if (active) message('No se encontró un QR en la foto. Prueba otra imagen o escribe la placa.');
        });
    }
    file.addEventListener('change', scanFile);
    gallery.addEventListener('change', scanFile);
}());
