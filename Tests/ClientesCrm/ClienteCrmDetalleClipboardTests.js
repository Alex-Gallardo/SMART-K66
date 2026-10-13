'use strict';
const assert = require('assert');
const fs = require('fs');
const vm = require('vm');
const path = require('path');

const script = fs.readFileSync(path.join(__dirname, '../../DiamDev.Give.UI/Scripts/clientes-crm-detalle.js'), 'utf8');

function elemento(contenido) {
    const e = {
        children: contenido ? [{ textContent: contenido }] : [],
        classList: { add() {}, remove() {}, toggle() {} },
        style: {},
        events: {},
        setAttribute(nombre, valor) { this[nombre] = valor; },
        addEventListener(nombre, accion) { this.events[nombre] = accion; },
        appendChild(child) {
            if (child.parent) child.parent.children.splice(child.parent.children.indexOf(child), 1);
            this.children.push(child);
            child.parent = this;
        },
        removeChild(child) { this.children.splice(this.children.indexOf(child), 1); },
        querySelector() { return null; },
        focus() {}, select() {}, setSelectionRange() {}
    };
    for (const child of e.children) child.parent = e;
    Object.defineProperty(e, 'firstChild', { get() { return this.children[0]; } });
    Object.defineProperty(e, 'textContent', {
        get() { return this.children.map(child => child.textContent).join(''); },
        set(valor) { this.children = [{ textContent: valor, parent: this }]; }
    });
    return e;
}

async function probar(clipboardDisponible) {
    const valor = elemento('  José  Pérez  ');
    const compuesto = elemento('Uno · Dos');
    compuesto.querySelector = () => ({});
    const vacio = elemento('—');
    const estado = elemento('');
    const copiados = [];
    const body = elemento();
    const document = {
        body, activeElement: { focus() {} },
        querySelector() { return ficha; },
        getElementById() { return estado; },
        createElement() { return elemento(); },
        execCommand(comando) {
            assert.strictEqual(comando, 'copy');
            copiados.push(body.children[0].value);
            return true;
        }
    };
    const ficha = { querySelectorAll() { return [valor, compuesto, vacio]; } };
    const navigator = clipboardDisponible ? {
        clipboard: { writeText(texto) { copiados.push(texto); return Promise.resolve(); } }
    } : {};
    vm.runInNewContext(script, { document, navigator, window: {
        setTimeout() { return 1; }, clearTimeout() {}
    } });
    assert.strictEqual(valor.children[0].className, 'crm-copyable');
    assert.strictEqual(compuesto.children.length, 1, 'cada valor compuesto se prepara por separado');
    assert.strictEqual(vacio.children.length, 1, 'el guion de valor ausente no se copia');
    valor.children[0].events.click();
    await new Promise(resolve => setImmediate(resolve));
    assert.deepStrictEqual(copiados, ['José Pérez']);
    assert.strictEqual(estado.textContent, 'Dato copiado al portapapeles.');
}

(async () => {
    await probar(true);
    await probar(false); // El despliegue HTTP requiere la alternativa a navigator.clipboard.
    console.log('OK: copia directa y alternativa HTTP, valores compuestos y ausentes.');
})().catch(error => { console.error(error); process.exitCode = 1; });
