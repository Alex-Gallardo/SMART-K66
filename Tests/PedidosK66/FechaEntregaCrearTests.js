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
const context = { $, fechaActual: '2026-10-05', fechaEntregaInicial: '2026-10-07', fechaEntregaElegida: false, Date };
vm.runInNewContext(script, context);

function seleccionar(tipo, nombre) {
    state.tipo = tipo;
    state.nombreTipo = nombre;
    state.events['#TipoPedidoId']();
}

assert.strictEqual(state.visible, false, 'La fecha debe iniciar oculta.');
for (const [tipo, nombre] of [['N1', ' Normal '], ['P', 'Temporada'], ['PG', 'Programacion'], ['PG', 'Programación']]) {
    context.fechaEntregaElegida = false;
    state.fecha = '2026-10-05';
    seleccionar(tipo, nombre);
    assert.strictEqual(state.visible, true, `${nombre} debe mostrar la fecha.`);
    assert.strictEqual(state.readonly, true, `${nombre} debe impedir escribir una fecha pasada.`);
    assert.strictEqual(state.startDate, '2026-10-05', `${nombre} debe bloquear los días anteriores a hoy.`);
    assert.strictEqual(state.fecha, '2026-10-07', `${nombre} debe proponer hoy + 2 días.`);
    assert.strictEqual(state.prometida, '2026-10-07', `${nombre} debe enviar la fecha visible.`);
}

state.fecha = '2026-10-05';
state.events['#FechaEntrega']({ preventDefault() {} });
assert.strictEqual(state.prometida, '2026-10-05', 'Hoy debe poder elegirse y enviarse.');

seleccionar('P', 'Temporada');
assert.strictEqual(state.fecha, '2026-10-05', 'Cambiar de tipo debe conservar una fecha válida elegida.');
assert.strictEqual(state.startDate, '2026-10-05');

state.fecha = '2026-10-04';
seleccionar('PG', 'Programación');
assert.strictEqual(state.fecha, '2026-10-07', 'Una fecha pasada debe corregirse al cambiar de tipo.');

seleccionar('X', 'Otro');
assert.strictEqual(state.visible, false, 'Otro tipo debe ocultar la fecha.');
assert.strictEqual(state.startDate, null, 'Otro tipo no debe tener el mínimo de los tres tipos.');
assert.strictEqual(state.prometida, '2026-10-05', 'Otro tipo no debe reutilizar una fecha anterior.');

state.fecha = '2026-10-04';
seleccionar('N1', 'Normal');
assert.strictEqual(state.fecha, '2026-10-07', 'Al regresar a Normal se debe corregir una fecha pasada.');
console.log('FechaEntregaCrearTests: OK');
