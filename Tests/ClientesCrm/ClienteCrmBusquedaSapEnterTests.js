"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const vm = require("vm");

const root = path.resolve(__dirname, "..", "..");
const updateView = fs.readFileSync(path.join(root, "DiamDev.Give.UI", "Views", "ClientesCrm", "Clientes.cshtml"), "utf8");
const editorView = fs.readFileSync(path.join(root, "DiamDev.Give.UI", "Views", "ClientesCrm", "Editor.cshtml"), "utf8");
const editorScript = fs.readFileSync(path.join(root, "DiamDev.Give.UI", "Scripts", "clientes-crm.js"), "utf8");

assert(!editorView.includes("crmSapLookup") && !editorScript.includes("crmSapLookup"),
    "El formulario de solicitudes no debe ofrecer búsqueda SAP.");
assert(editorView.includes('esCredito ? "Observaciones / canal de solicitud" : "Comentarios / canal de solicitud"'),
    "El nuevo label debe aplicarse a altas y actualizaciones, sin cambiar la ficha directa.");

const inlineScript = updateView.match(/<script>([\s\S]*?)<\/script>/);
assert(inlineScript, "Falta el script de búsqueda de actualización.");
const source = inlineScript[1].replace(/@Url\.Action\("([^"]+)"\)/g, "/ClientesCrm/$1");
new vm.Script(source);

function element(value = "") {
    return {
        value,
        listeners: {},
        children: [],
        textContent: "",
        addEventListener(name, listener) { this.listeners[name] = listener; },
        appendChild(child) { this.children.push(child); },
        querySelectorAll() { return []; }
    };
}

const elements = {
    crmUpdateEmpresa: element("BOLIK"),
    crmUpdateAgente: element("A1"),
    crmUpdateFiltro: element("cliente"),
    crmUpdateStatus: element(),
    crmUpdateResultados: element(),
    crmUpdateAgentes: element(),
    crmUpdateBuscar: element()
};
const calls = [];
vm.runInNewContext(source, {
    document: {
        getElementById(id) { return elements[id]; },
        createElement() { return element(); }
    },
    fetch(url, options) {
        calls.push({ url, options });
        return Promise.resolve({ json: () => Promise.resolve({ ok: true, clientes: [] }) });
    },
    encodeURIComponent
});

(async function () {
    elements.crmUpdateBuscar.listeners.click();
    let prevented = false;
    elements.crmUpdateFiltro.listeners.keydown({
        key: "Enter", preventDefault() { prevented = true; }
    });
    assert(prevented, "Enter debe evitar el comportamiento predeterminado del campo.");
    assert.strictEqual(calls.length, 2, "El botón y Enter deben iniciar una búsqueda cada uno.");
    assert.strictEqual(calls[0].url, calls[1].url, "El botón y Enter deben consultar con los mismos filtros.");
    assert.strictEqual(calls[0].options.credentials, "same-origin");
    elements.crmUpdateFiltro.listeners.keydown({ key: "a", preventDefault() { throw Error("Tecla inesperada"); } });
    assert.strictEqual(calls.length, 2, "Otras teclas no deben buscar.");
    await new Promise(resolve => setImmediate(resolve));
    assert.strictEqual(elements.crmUpdateStatus.textContent, "0 clientes encontrados.");
    console.log("OK: Enter y botón buscan en SAP con el mismo flujo de actualización.");
})().catch(error => { console.error(error); process.exitCode = 1; });
