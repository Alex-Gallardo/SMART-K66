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
        fire(name, event) { return this.listeners[name](event || {}); }
    };
}
function harness() {
    const ids = Object.fromEntries(['qr-open', 'qr-dialog', 'plate-search-form', 'placa', 'qr-video', 'qr-status', 'qr-file', 'qr-close', 'qr-cancel'].map(id => [id, element()]));
    ids['qr-dialog'].hidden = true;
    ids['qr-dialog'].querySelector = () => element();
    ids['plate-search-form'].submit = function () { this.submitted = true; };
    const doc = { getElementById: id => ids[id], addEventListener(name, callback) { this[name] = callback; } };
    let scanner;
    class QrScanner {
        constructor(video, callback, options) { this.callback = callback; this.options = options; scanner = this; }
        start() { this.started = true; return Promise.resolve(); }
        stop() { this.stopped = true; }
        destroy() { this.destroyed = true; }
        static scanImage() { return Promise.resolve({ data: 'c-777abc' }); }
    }
    const win = { QrScanner, addEventListener(name, callback) { this[name] = callback; } };
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
    console.log('OK QR garita: placa válida, QR inválido, cierre y foto.');
}()).catch(error => { console.error(error); process.exitCode = 1; });
