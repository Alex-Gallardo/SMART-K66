(function () {
    'use strict';
    var root = document.documentElement;
    var manualTheme = null;
    try { manualTheme = sessionStorage.getItem('garita-theme'); } catch (_) { }
    var toggle = document.getElementById('theme-toggle');
    function applyTheme() {
        var dark = manualTheme ? manualTheme === 'dark' : (new Date().getHours() < 6 || new Date().getHours() >= 18);
        root.setAttribute('data-theme', dark ? 'dark' : 'light');
        if (toggle) { toggle.setAttribute('aria-pressed', String(dark)); toggle.setAttribute('aria-label', dark ? 'Activar modo claro' : 'Activar modo oscuro'); }
        var meta = document.querySelector('meta[name="theme-color"]');
        if (meta) meta.setAttribute('content', dark ? '#0b2235' : '#123c55');
    }
    applyTheme();
    if (toggle) toggle.addEventListener('click', function () {
        manualTheme = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        try { sessionStorage.setItem('garita-theme', manualTheme); } catch (_) { }
        applyTheme();
    });
    window.setInterval(function () { if (!manualTheme) applyTheme(); }, 60000);

    function mobile() { return window.matchMedia && window.matchMedia('(max-width: 750px)').matches; }
    function dismissKeyboard() {
        var active = document.activeElement;
        if (mobile() && active && /^(INPUT|TEXTAREA)$/.test(active.tagName) && active.type !== 'file') active.blur();
    }
    document.addEventListener('click', function (event) {
        if (event.target.closest('button, a, summary, .qr-dialog, .difference-dialog')) dismissKeyboard();
    }, true);
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' && document.activeElement && document.activeElement.tagName === 'INPUT') dismissKeyboard();
    });
    document.addEventListener('submit', dismissKeyboard, true);
    var typingTimer;
    var composing = false;
    document.addEventListener('compositionstart', function () { composing = true; });
    document.addEventListener('compositionend', function () { composing = false; });
    document.addEventListener('input', function (event) {
        if (!mobile() || event.target.tagName !== 'INPUT' || event.target.type !== 'text') return;
        window.clearTimeout(typingTimer);
        var field = event.target;
        typingTimer = window.setTimeout(function () { if (!composing && document.activeElement === field) field.blur(); }, 2200);
    });

    var audioContext;
    function readyAudio() {
        try {
            var AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) return;
            if (!audioContext) audioContext = new AudioContext();
            if (audioContext.state === 'suspended') audioContext.resume().catch(function () { });
        } catch (_) { }
    }
    function feedback(kind) {
        try { if (navigator.vibrate) navigator.vibrate(kind === 'stop' ? [55, 45, 55] : 45); } catch (_) { }
        if (!audioContext || audioContext.state !== 'running') return;
        try {
            var oscillator = audioContext.createOscillator();
            var gain = audioContext.createGain();
            var now = audioContext.currentTime;
            oscillator.type = 'sine';
            oscillator.frequency.setValueAtTime(kind === 'stop' ? 370 : 740, now);
            if (kind !== 'stop') oscillator.frequency.linearRampToValueAtTime(880, now + 0.14);
            gain.gain.setValueAtTime(0.0001, now);
            gain.gain.exponentialRampToValueAtTime(0.045, now + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.23);
            oscillator.connect(gain); gain.connect(audioContext.destination);
            oscillator.start(now); oscillator.stop(now + 0.24);
        } catch (_) { }
    }
    var qrOpen = document.getElementById('qr-open');
    if (qrOpen) qrOpen.addEventListener('click', readyAudio);
    window.GaritaUI = { qrAccepted: function (plate, next) {
        dismissKeyboard(); feedback('qr');
        var overlay = document.createElement('div'); overlay.className = 'garita-receipt garita-qr-receipt'; overlay.setAttribute('role', 'status');
        var box = document.createElement('div'); box.className = 'garita-receipt-box';
        var mark = document.createElement('span'); mark.className = 'garita-receipt-mark'; mark.setAttribute('aria-hidden', 'true'); mark.textContent = '✓';
        var title = document.createElement('h2'); title.textContent = 'Placa detectada';
        var message = document.createElement('p'); message.textContent = plate + ' · Buscando rutas…';
        box.appendChild(mark); box.appendChild(title); box.appendChild(message); overlay.appendChild(box); document.body.appendChild(overlay);
        window.setTimeout(next, 650);
    } };

    var plateForm = document.getElementById('plate-search-form');
    if (plateForm) plateForm.addEventListener('submit', function () {
        var plate = document.getElementById('placa');
        plate.value = plate.value.trim().toUpperCase();
        dismissKeyboard();
    });

    function photo(id) {
        var input = document.getElementById(id);
        if (!input) return;
        var preview = document.getElementById(id + '-preview');
        var clear = document.getElementById(id + '-clear');
        var objectUrl = null;
        function reset() {
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = null; input.value = ''; preview.hidden = true; preview.removeAttribute('src'); clear.hidden = true;
        }
        document.querySelectorAll('[data-photo-camera="' + id + '"]').forEach(function (button) {
            button.addEventListener('click', function () { dismissKeyboard(); input.setAttribute('capture', 'environment'); input.click(); });
        });
        document.querySelectorAll('[data-photo-gallery="' + id + '"]').forEach(function (button) {
            button.addEventListener('click', function () { dismissKeyboard(); input.removeAttribute('capture'); input.click(); });
        });
        input.addEventListener('change', function () {
            if (objectUrl) URL.revokeObjectURL(objectUrl);
            objectUrl = null;
            var file = input.files && input.files[0];
            if (!file) { reset(); return; }
            if (file.size > 10485760 || !/^image\/(jpeg|png|webp)$/.test(file.type)) {
                alert('Elige una foto JPG, PNG o WebP de hasta 10 MB.'); reset(); return;
            }
            objectUrl = URL.createObjectURL(file);
            preview.src = objectUrl; preview.hidden = false; clear.hidden = false;
        });
        clear.addEventListener('click', reset);
        window.addEventListener('pagehide', function () { if (objectUrl) URL.revokeObjectURL(objectUrl); });
    }
    photo('foto'); photo('foto-entrada');

    var differenceDialog = document.getElementById('difference-dialog');
    var differenceOpen = document.getElementById('difference-open');
    if (differenceDialog && differenceOpen) {
        differenceOpen.addEventListener('click', function () { readyAudio(); differenceDialog.showModal(); });
        function closeDifference() { differenceDialog.close(); differenceOpen.focus(); }
        document.getElementById('difference-close').addEventListener('click', closeDifference);
        differenceDialog.querySelector('.difference-cancel').addEventListener('click', closeDifference);
        differenceDialog.addEventListener('click', function (event) { if (event.target === differenceDialog) closeDifference(); });
        var reason = differenceDialog.querySelectorAll('[name="MotivoRapido"]');
        var note = document.getElementById('descripcion');
        function validateReason() {
            var other = differenceDialog.querySelector('[name="MotivoRapido"]:checked');
            note.required = !!(other && other.value === 'OTRO');
            note.minLength = note.required ? 10 : 0;
        }
        reason.forEach(function (radio) { radio.addEventListener('change', validateReason); });
    }

    var exitForm = document.getElementById('register-exit-form');
    if (exitForm) {
        var authorize = exitForm.querySelector('button[type="submit"]');
        var checks = exitForm.querySelectorAll('input[type="checkbox"][required]');
        function updateChecklist() {
            authorize.disabled = !Array.prototype.every.call(checks, function (check) { return check.checked; });
            authorize.setAttribute('aria-disabled', String(authorize.disabled));
        }
        checks.forEach(function (check) { check.addEventListener('change', updateChecklist); });
        updateChecklist();
    }

    function showReceipt(data) {
        var overlay = document.createElement('div');
        overlay.className = 'garita-receipt garita-receipt-' + data.tipo;
        overlay.setAttribute('role', 'dialog'); overlay.setAttribute('aria-modal', 'true');
        var box = document.createElement('div'); box.className = 'garita-receipt-box';
        var mark = document.createElement('span'); mark.className = 'garita-receipt-mark'; mark.setAttribute('aria-hidden', 'true');
        mark.textContent = data.tipo === 'diferencia' ? '!' : '✓'; box.appendChild(mark);
        var title = document.createElement('h2');
        title.textContent = data.tipo === 'salida' ? 'Salida autorizada' : data.tipo === 'entrada' ? 'Entrada registrada' : 'Salida detenida';
        box.appendChild(title);
        var message = document.createElement('p');
        message.textContent = data.tipo === 'diferencia' ? 'No permitas la salida. Llama a Distribución.' : 'Hora de Guatemala · ' + data.hora;
        box.appendChild(message);
        var next = document.createElement('a'); next.href = data.url; next.textContent = 'Continuar'; next.className = 'garita-receipt-next';
        box.appendChild(next); overlay.appendChild(box); document.body.appendChild(overlay); next.focus();
        feedback(data.tipo === 'diferencia' ? 'stop' : 'ok');
    }
    ['register-exit-form', 'register-entry-form', 'difference-form'].forEach(function (id) {
        var form = document.getElementById(id);
        if (!form || !window.fetch || !window.FormData) return;
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) return;
            event.preventDefault(); dismissKeyboard(); readyAudio();
            var button = form.querySelector('button[type="submit"]');
            if (button.disabled) return;
            var original = button.textContent;
            button.disabled = true; button.textContent = 'Guardando…';
            var alertBox = form.querySelector('.form-error');
            if (!alertBox) { alertBox = document.createElement('p'); alertBox.className = 'form-error'; alertBox.setAttribute('role', 'alert'); form.insertBefore(alertBox, button); }
            alertBox.textContent = '';
            fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (response) {
                    var type = response.headers.get('content-type') || '';
                    if (type.indexOf('application/json') < 0) throw new Error('La sesión pudo haber vencido. Vuelve a abrir la ruta.');
                    return response.json().then(function (data) { if (!response.ok || !data.ok) throw new Error(data.mensaje || 'No se pudo guardar. Intenta de nuevo.'); return data; });
                })
                .then(showReceipt)
                .catch(function (error) { alertBox.textContent = error.message || 'No se pudo confirmar el registro. Intenta de nuevo.'; button.disabled = false; button.textContent = original; });
        });
    });
}());
