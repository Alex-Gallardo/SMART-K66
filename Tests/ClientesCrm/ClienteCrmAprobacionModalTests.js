'use strict';
const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const script = fs.readFileSync(path.join(__dirname, '../../DiamDev.Give.UI/Scripts/clientes-crm-resolver.js'), 'utf8');

function elemento() {
    const clases = new Set();
    return {
        hidden: true, disabled: false, value: '', listeners: {},
        classList: { add(c) { clases.add(c); }, remove(c) { clases.delete(c); }, contains(c) { return clases.has(c); } },
        addEventListener(evento, fn) { this.listeners[evento] = fn; },
        setAttribute(nombre, valor) { this[nombre] = valor; },
        setCustomValidity(valor) { this.error = valor; },
        checkValidity() { return !this.disabled && !!this.value && !this.error; },
        reportValidity() { this.reportado = true; },
        focus() { document.activeElement = this; },
        click() { this.listeners.click({ preventDefault() { this.preventido = true; } }); }
    };
}

const ids = Object.fromEntries([
    'crmAprobacionModal', 'crmAbrirAprobacion', 'crmCerrarAprobacion',
    'crmCancelarAprobacion', 'crmConfirmarAprobacion',
    'crmCodigoSapAprobacion', 'crmResolverForm'
].map(id => [id, elemento()]));
const document = {
    activeElement: ids.crmAbrirAprobacion,
    getElementById(id) { return ids[id]; },
    contains() { return true; }
};
ids.crmCodigoSapAprobacion.disabled = true;
vm.runInNewContext(script, { document });

ids.crmAbrirAprobacion.listeners.click();
assert.strictEqual(ids.crmAprobacionModal.hidden, false);
assert.strictEqual(ids.crmCodigoSapAprobacion.disabled, false);
assert.strictEqual(document.activeElement, ids.crmCodigoSapAprobacion);

let detenido = false;
ids.crmConfirmarAprobacion.listeners.click({ preventDefault() { detenido = true; } });
assert(detenido && ids.crmCodigoSapAprobacion.reportado, 'No debe aprobar sin código SAP.');
ids.crmCodigoSapAprobacion.value = '  CL0320  ';
ids.crmCodigoSapAprobacion.listeners.input();
ids.crmConfirmarAprobacion.listeners.click({ preventDefault() { throw Error('Código válido'); } });
assert.strictEqual(ids.crmCodigoSapAprobacion.value, 'CL0320');
ids.crmResolverForm.listeners.submit({ submitter: { value: 'APROBAR' }, preventDefault() {} });
assert(!ids.crmConfirmarAprobacion.disabled, 'El botón submit debe quedar habilitado para enviar decision=APROBAR.');
assert(ids.crmAprobacionModal.classList.contains('is-saving'));
let duplicadoDetenido = false;
ids.crmConfirmarAprobacion.listeners.click({ preventDefault() { duplicadoDetenido = true; } });
assert(duplicadoDetenido, 'No debe enviarse dos veces.');

ids.crmCancelarAprobacion.listeners.click();
assert(ids.crmAprobacionModal.hidden && ids.crmCodigoSapAprobacion.disabled);
assert.strictEqual(ids.crmCodigoSapAprobacion.value, '');
assert.strictEqual(document.activeElement, ids.crmAbrirAprobacion);

let rechazoDetenido = false;
ids.crmResolverForm.listeners.submit({ submitter: { value: 'RECHAZAR' }, preventDefault() { rechazoDetenido = true; } });
assert(!rechazoDetenido, 'El rechazo no necesita código SAP.');
console.log('OK: modal exige código SAP, lo limpia al cancelar y permite rechazar sin código.');
