const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const script = fs.readFileSync(path.join(__dirname, '../../DiamDev.Give.UI/Scripts/garita-qr.js'), 'utf8');

function element() {
    return {
        listeners: {}, hidden: false, value: '', files: [], attributes: {},
        addEventListener(name, callback) { this.listeners[name] = callback; },
        getAttribute(name) { return this.attributes[name]; },
        focus() { this.focused = true; },
        click() { this.clicked = true; },
        fire(name, event) { return this.listeners[name](event || { currentTarget: this }); }
    };
}
function harness(options = {}) {
    const ids = Object.fromEntries(['qr-open', 'qr-dialog', 'plate-search-form', 'placa', 'qr-video', 'qr-status', 'qr-file', 'qr-gallery', 'qr-close', 'qr-cancel'].map(id => [id, element()]));
    ids['qr-dialog'].hidden = true;
    ids['qr-dialog'].querySelector = () => element();
    ids['plate-search-form'].submit = function () { this.submitted = true; };
    const doc = { getElementById: id => ids[id], addEventListener(name, callback) { this[name] = callback; } };
    let scanner;
    class QrScanner {
        constructor(video, callback, scannerOptions) { this.callback = callback; this.options = scannerOptions; scanner = this; }
        start() { this.started = true; return options.cameraError ? Promise.reject(options.cameraError) : Promise.resolve(); }
        stop() { this.stopped = true; }
        destroy() { this.destroyed = true; }
        static scanImage() { return Promise.resolve({ data: 'c-777abc' }); }
    }
    const win = {
        QrScanner,
        isSecureContext: options.secure !== false,
        navigator: options.mediaDevices === false ? {} : { mediaDevices: { getUserMedia() {} } },
        addEventListener(name, callback) { this[name] = callback; }
    };
    vm.runInNewContext(script, { window: win, document: doc, alert() {} });
    return { ids, doc, win, get scanner() { return scanner; } };
}

(async function () {
    let h = harness();
    h.ids['qr-open'].fire('click');
    await Promise.resolve();
    assert.equal(h.ids['qr-dialog'].hidden, false);
    assert.equal(h.scanner.options.preferredCamera, 'environment');
    h.scanner.callback({ data: 'https://example.org/plate/C-123' });
    assert.equal(h.ids['plate-search-form'].submitted, undefined, 'un QR ajeno no inicia búsqueda');
    h.scanner.callback({ data: ' c-123abc ' });
    assert.equal(h.ids['placa'].value, 'C-123ABC');
    assert.equal(h.ids['plate-search-form'].submitted, true);
    assert.ok(h.scanner.stopped && h.scanner.destroyed, 'cámara liberada antes de navegar');

    h = harness();
    h.ids['qr-open'].fire('click');
    h.ids['qr-close'].fire('click');
    assert.equal(h.ids['qr-dialog'].hidden, true);
    assert.ok(h.scanner.destroyed, 'cerrar libera cámara');
    assert.ok(h.ids['qr-open'].focused, 'el foco vuelve al botón');

    h = harness();
    h.ids['qr-open'].fire('click');
    h.ids['qr-file'].files = [{}];
    h.ids['qr-file'].fire('change');
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(h.ids['placa'].value, 'C-777ABC');
    assert.equal(h.ids['plate-search-form'].submitted, true, 'la foto usa la misma búsqueda');

    h = harness({ secure: false });
    h.ids['qr-open'].fire('click');
    assert.equal(h.scanner, undefined, 'HTTP público no intenta iniciar la cámara');
    assert.equal(h.ids['qr-video'].hidden, true, 'no deja un recuadro de cámara vacío');
    assert.match(h.ids['qr-status'].textContent, /HTTPS.*HTTP/, 'explica la causa y la solución');
    assert.ok(h.ids['qr-file'].clicked, 'HTTP abre la captura nativa tras el gesto del usuario');
    h.ids['qr-file'].files = [{}];
    h.ids['qr-file'].fire('change');
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(h.ids['plate-search-form'].submitted, true, 'la foto sigue funcionando sin HTTPS');

    h = harness({ secure: false });
    h.ids['qr-open'].fire('click');
    h.ids['qr-gallery'].files = [{}];
    h.ids['qr-gallery'].fire('change');
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(h.ids['plate-search-form'].submitted, true, 'una imagen guardada también permite buscar la placa');

    h = harness({ cameraError: { name: 'NotAllowedError' } });
    h.ids['qr-open'].fire('click');
    await new Promise(resolve => setImmediate(resolve));
    assert.match(h.ids['qr-status'].textContent, /permiso/, 'distingue permiso denegado de HTTP inseguro');
    assert.ok(h.scanner.destroyed, 'libera la cámara tras el rechazo');
    assert.equal(h.ids['qr-video'].hidden, true);

    h = harness({ mediaDevices: false });
    h.ids['qr-open'].fire('click');
    assert.equal(h.scanner, undefined, 'no inicia el lector si el navegador no ofrece cámara');
    assert.match(h.ids['qr-status'].textContent, /no ofrece acceso/);
    console.log('OK QR garita: placa válida, QR inválido, cierre y foto.');
}()).catch(error => { console.error(error); process.exitCode = 1; });
