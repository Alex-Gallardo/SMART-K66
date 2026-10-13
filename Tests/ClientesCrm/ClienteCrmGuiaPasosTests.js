'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const script = fs.readFileSync(path.join(__dirname, '../../DiamDev.Give.UI/Scripts/clientes-crm.js'), 'utf8');
const inicio = script.indexOf('    var guia = document.getElementById(\'crmGuide\');');
const fin = script.indexOf('    var iconosCampo =', inicio);
assert(inicio >= 0 && fin > inicio, 'No se encontró el recorrido de ayuda.');
const recorrido = script.slice(inicio, fin);

function elemento() {
    const clases = new Set();
    return {
        hidden: true, disabled: false, textContent: '', style: { setProperty(nombre, valor) { this[nombre] = valor; } },
        listeners: {}, classList: {
            add(nombre) { clases.add(nombre); }, remove(nombre) { clases.delete(nombre); },
            contains(nombre) { return clases.has(nombre); }
        },
        addEventListener(nombre, callback) { this.listeners[nombre] = callback; },
        setAttribute(nombre, valor) { this[nombre] = valor; },
        focus() { this.focused = true; },
        getBoundingClientRect() { return { left: 100, width: 90, top: 65, bottom: 102 }; },
        offsetLeft: 100, offsetWidth: 90
    };
}

function probar(incluirDesarrollo) {
    const enlaces = {};
    for (const numero of [1, 2, 3, 4, ...(incluirDesarrollo ? [5] : []), 6]) {
        enlaces[numero] = elemento();
        enlaces[numero].offsetLeft = numero * 120;
    }
    const nav = elemento();
    nav.clientWidth = 300;
    nav.scrollLeft = 0;
    nav.querySelector = selector => enlaces[Number(selector.match(/data-step="(\d+)"/)[1])] || null;
    nav.getBoundingClientRect = () => ({ top: 170, bottom: 210 });
    const guia = elemento();
    guia.querySelector = () => elemento();
    const ids = Object.fromEntries([
        'crmGuide', 'crmHelpOpen', 'crmGuideClose', 'crmGuidePrev', 'crmGuideNext',
        'crmHelpHint', 'crmHelpHintClose', 'crmGuideCount', 'crmGuideFill',
        'crmGuideTitle', 'crmGuideDescription'
    ].map(id => [id, elemento()]));
    ids.crmGuide = guia;
    const document = {
        activeElement: ids.crmHelpOpen,
        getElementById(id) { return ids[id]; },
        contains() { return true; }
    };
    const window = {
        innerWidth: 900, innerHeight: 700, pageYOffset: 0,
        localStorage: { getItem() { return null; }, setItem() {} },
        requestAnimationFrame(callback) { callback(); },
        getComputedStyle() { return { getPropertyValue() { return '54px'; } }; },
        scrollTo(_x, y) { this.pageYOffset = y; },
        addEventListener() {}
    };
    vm.runInNewContext(recorrido, { document, window, wizard: true, stepNav: nav, app: elemento() });

    ids.crmHelpOpen.listeners.click();
    assert.strictEqual(guia.hidden, false);
    assert(nav.classList.contains('is-guide-active'));
    assert(enlaces[1].classList.contains('is-guide-target'));
    assert.strictEqual(ids.crmGuideCount.textContent, `Paso 1 de ${incluirDesarrollo ? 6 : 5}`);
    assert(ids.crmGuideDescription.textContent.includes('borrador'));
    assert(Number(guia.style['--crm-guide-pointer-left'].replace('px', '')) > 0);

    ids.crmGuideNext.listeners.click();
    assert(enlaces[2].classList.contains('is-guide-target'));
    ids.crmGuidePrev.listeners.click();
    assert(enlaces[1].classList.contains('is-guide-target'));

    for (let i = 1; i < (incluirDesarrollo ? 6 : 5); i++) {
        ids.crmGuideNext.listeners.click();
    }
    assert(enlaces[6].classList.contains('is-guide-target'), 'El último tema señala Documentos.');
    assert(ids.crmGuideDescription.textContent.includes('Enviar a Créditos'));
    assert(ids.crmGuideDescription.textContent.includes('no entrega la solicitud'));
    assert(!enlaces[1].classList.contains('is-guide-target'));

    ids.crmGuideNext.listeners.click();
    assert.strictEqual(guia.hidden, true);
    assert(!nav.classList.contains('is-guide-active'));
    assert(!enlaces[6].classList.contains('is-guide-target'));
    assert(ids.crmHelpOpen.focused, 'El foco vuelve al botón que abrió la guía.');

    ids.crmHelpOpen.listeners.click();
    let escapePrevenido = false;
    guia.listeners.keydown({ key: 'Escape', preventDefault() { escapePrevenido = true; } });
    assert(escapePrevenido && guia.hidden, 'Escape cierra la guía sin dejar el paso resaltado.');
}

probar(false);
probar(true);
console.log('OK: guía anclada al navegador, cinco o seis pasos y cierre con foco restaurado.');
