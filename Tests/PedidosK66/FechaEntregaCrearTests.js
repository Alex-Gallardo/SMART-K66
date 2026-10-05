const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const view = fs.readFileSync(path.join(__dirname, '../../DiamDev.Give.UI/Views/Pedido_K66/Crear.cshtml'), 'utf8');
const start = view.indexOf('function actualizarFechaEntrega()');
const end = view.indexOf('\n    });', start);
assert(start !== -1 && end !== -1, 'No se encontró la lógica de Fecha Entrega en Crear.');
const script = view.slice(start, end);

const state = {
    tipo: '', nombreTipo: '--Seleccione--', fecha: '2026-10-05', prometida: '2026-10-05',
    visible: false, readonly: false, startDate: null, events: {}
};

function elemento(selector) {
    return {
        val(value) {
            const key = selector === '#TipoPedidoId' ? 'tipo' : selector === '#FechaEntrega' ? 'fecha' : 'prometida';
            if (value === undefined) return state[key];
            state[key] = value;
            return this;
        },
        text() { return state.nombreTipo; },
        toggle(value) { state.visible = value; return this; },
        prop(name, value) { assert.strictEqual(name, 'readonly'); state.readonly = value; return this; },
        datepicker(action, value) {
            if (action === 'setStartDate') state.startDate = value;
            else if (action === 'setDate') {
                state.fecha = [value.getFullYear(), String(value.getMonth() + 1).padStart(2, '0'),
                    String(value.getDate()).padStart(2, '0')].join('-');
                if (state.events['#FechaEntrega']) state.events['#FechaEntrega']({ preventDefault() {} });
            } else throw new Error('Acción del calendario inesperada: ' + action);
            return this;
        },
        change(handler) { state.events[selector] = handler; return this; }
    };
}

function $(selector) { return elemento(selector); }
$.trim = text => text.trim();
vm.runInNewContext(script, { $, fechaActual: '2026-10-05', fechaMinimaNormal: '2026-10-07', Date });

function seleccionar(tipo, nombre) {
    state.tipo = tipo;
    state.nombreTipo = nombre;
    state.events['#TipoPedidoId']();
}

assert.strictEqual(state.visible, false, 'La fecha debe iniciar oculta.');
seleccionar('N1', ' Normal ');
assert.strictEqual(state.visible, true, 'Normal debe mostrar la fecha, independientemente del código SAP.');
assert.strictEqual(state.readonly, true, 'Normal debe impedir escribir una fecha fuera del calendario.');
assert.strictEqual(state.startDate, '2026-10-07', 'Normal debe bloquear los días anteriores a hoy + 2.');
assert.strictEqual(state.fecha, '2026-10-07', 'La fecha inicial de Normal debe ser hoy + 2.');
assert.strictEqual(state.prometida, '2026-10-07', 'El dato enviado debe corresponder al campo visible.');

state.fecha = '2026-10-10';
state.events['#FechaEntrega']({ preventDefault() {} });
assert.strictEqual(state.prometida, '2026-10-10', 'Una fecha posterior debe enviarse correctamente.');

seleccionar('P', 'Temporada');
assert.strictEqual(state.visible, true, 'Temporada debe conservar el campo visible.');
assert.strictEqual(state.startDate, null, 'Temporada no debe conservar el mínimo de Normal.');
assert.strictEqual(state.readonly, false, 'Temporada debe conservar la edición existente.');
assert.strictEqual(state.prometida, '2026-10-10');

seleccionar('X', 'Otro');
assert.strictEqual(state.visible, false, 'Otro tipo debe ocultar la fecha.');
assert.strictEqual(state.prometida, '2026-10-05', 'Otro tipo no debe reutilizar una fecha anterior.');

state.fecha = '2026-10-05';
seleccionar('N1', 'Normal');
assert.strictEqual(state.fecha, '2026-10-07', 'Al regresar a Normal se debe restablecer la fecha mínima.');
console.log('FechaEntregaCrearTests: OK');
