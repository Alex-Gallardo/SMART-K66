'use strict';
const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const script = fs.readFileSync(path.join(__dirname, '../../DiamDev.Give.UI/Scripts/clientes-crm-selects.js'), 'utf8');
const opciones = [{ value: 'Privada', textContent: 'Privada' }, { value: '', textContent: 'Seleccione...' }];
const modulo = { querySelectorAll(selector) { assert.strictEqual(selector, 'select option'); return opciones; } };
let observarCambios;
const window = { MutationObserver: class {
    constructor(callback) { observarCambios = callback; }
    observe(elemento, opcionesObservacion) {
        assert.strictEqual(elemento, modulo);
        assert(opcionesObservacion.subtree && opcionesObservacion.childList);
    }
} };
const document = { querySelector(selector) { assert.strictEqual(selector, '.crm'); return modulo; } };
vm.runInNewContext(script, { document, window });
assert.deepStrictEqual(opciones.map(o => o.textContent), ['PRIVADA', 'SELECCIONE...']);
assert.strictEqual(opciones[0].value, 'Privada', 'El valor enviado al servidor no debe cambiar.');

opciones.push({ value: 'Crédito', textContent: 'Crédito' });
observarCambios();
assert.strictEqual(opciones[2].textContent, 'CRÉDITO');
assert.strictEqual(opciones[2].value, 'Crédito');
console.log('OK: etiquetas iniciales y dinámicas en mayúsculas, sin cambiar valores enviados.');
