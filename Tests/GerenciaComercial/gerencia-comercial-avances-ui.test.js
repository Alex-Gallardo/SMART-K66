const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

class Element {
    constructor(tag = "div") { this.tagName = tag; this.children = []; this.attributes = {}; this.listeners = {}; this.value = ""; this.disabled = false; this.hidden = false; this.style = {}; this.classList = { toggle() {} }; this._text = ""; }
    set textContent(value) { this._text = String(value); this.children = []; }
    get textContent() { return this._text + this.children.map(child => child.textContent).join(""); }
    appendChild(child) { this.children.push(child); return child; }
    replaceChildren(...children) { this._text = ""; this.children = children; }
    setAttribute(key, value) { this.attributes[key] = String(value); }
    getAttribute(key) { return this.attributes[key] || null; }
    addEventListener(key, callback) { (this.listeners[key] ||= []).push(callback); }
    fire(key, event = {}) { for (const callback of this.listeners[key] || []) callback(event); }
}
const ids = "gc gc-advance-status gc-advance-periods gc-advance-form gc-advance-company gc-advance-currency gc-advance-add-year gc-advance-add-cutoff gc-advance-add-custom gc-advance-year gc-advance-from gc-advance-to gc-advance-search gc-advance-front gc-advance-count gc-advance-chart gc-advance-chart-wrap gc-advance-legend gc-advance-kpis gc-advance-units gc-advance-reading gc-advance-alerts gc-advance-head gc-advance-rows gc-advance-table-note gc-advance-export gc-advance-pdf gc-advance-print".split(" ");
const nodes = Object.fromEntries(ids.map(id => [id, new Element()]));
nodes.gc.setAttribute("data-avances", "/Avances"); nodes.gc.setAttribute("data-pdf-avances", "/PdfAvances"); nodes["gc-advance-company"].value = "FAES"; nodes["gc-advance-currency"].value = "GTQ"; nodes["gc-advance-count"].value = "12";
const modes = ["bars", "lines"].map(value => { const button = new Element("button"); button.setAttribute("data-advance-mode", value); return button; });
nodes.gc.querySelectorAll = selector => selector === "[data-advance-mode]" ? modes : [];
const document = { getElementById: id => nodes[id], createElement: tag => new Element(tag), createElementNS: (_, tag) => new Element(tag) };
const FixedDate = class extends Date { constructor(...args) { super(...(args.length ? args : ["2026-11-04T12:00:00"])); } static now() { return new Date("2026-11-04T12:00:00").getTime(); } };
const calls = [];
async function fetchMock(url) {
    const query = new URL(String(url), "http://local"); calls.push(query);
    const periods = query.searchParams.get("rangos").split("|");
    const ventas = [], unidades = [];
    for (let i = 0; i < periods.length; i++) {
        for (let agent = 1; agent <= 13; agent++) {
            const net = agent === 1 ? [100, 150, 180][i] : agent === 2 ? [90, 50, 20][i] : 10;
            ventas.push({ Empresa: "FAES", Periodo: i, Codigo: String(agent), Nombre: agent === 1 ? "Rubidio" : agent === 2 ? "Manuel" : "Agente " + agent, Frente: agent === 1 ? "Sala de Ventas" : "Agente comercial", Activo: "Y", Neto: net, Facturas: 1, NotasCredito: 0 });
        }
        unidades.push({ Periodo: i, Codigo: "1", Unidad: "UN", Cantidad: [30, 42, 50][i] });
        if (periods.length > 2 && i === 2) unidades.push({ Periodo: i, Codigo: "2", Unidad: "CAJA", Cantidad: 3 });
    }
    return { ok: true, redirected: false, json: async () => ({ empresa: "FAES", moneda: "GTQ", ventas, unidades, actualizado: "2026-11-04 08:00", advertencia: null }) };
}
const source = fs.readFileSync(path.join(__dirname, "../../DiamDev.Give.UI/Scripts/App/gerencia-comercial-avances.js"), "utf8");
const pdfWindow = { location: {}, open(url) { this.printUrl = url; } };
vm.runInNewContext(source, { document, window: pdfWindow, fetch: fetchMock, URL, URLSearchParams, Date: FixedDate, Intl, Number, Object, Array, Math, String, isFinite });
const tick = () => new Promise(resolve => setTimeout(resolve, 0));
(async () => {
    await tick();
    assert.equal(calls.length, 1, "Se consulta la comparación inicial de dos períodos");
    assert.equal(calls[0].searchParams.get("rangos").split("|").length, 2);
    assert.match(nodes["gc-advance-kpis"].textContent, /GTQ 300\.00/);
    assert.match(nodes["gc-advance-kpis"].textContent, /GTQ 310\.00/);
    assert.equal(nodes["gc-advance-kpis"].children.length, 2, "Los KPI deben separarse por ventas y variación");
    assert.match(nodes["gc-advance-reading"].textContent, /Rubidio/);
    assert.match(nodes["gc-advance-alerts"].textContent, /Manuel/);
    assert.match(nodes["gc-advance-alerts"].children[0].className, /gc-advance-finding/, "Alertas con jerarquía visual");
    assert.match(nodes["gc-advance-units"].textContent, /UN/);
    assert.equal(nodes["gc-advance-chart"].children[0].tagName, "svg");
    assert.match(nodes["gc-advance-chart"].getAttribute("aria-label"), /12 agentes/);
    nodes["gc-advance-count"].value = "all"; nodes["gc-advance-count"].fire("change");
    assert.match(nodes["gc-advance-chart"].getAttribute("aria-label"), /13 agentes/);
    modes[1].fire("click");
    assert.equal(calls.length, 1, "Cambiar de barras a líneas no debe volver a consultar SAP");
    assert(nodes["gc-advance-chart"].children[0].children.some(child => child.tagName === "polyline"), "La vista de líneas conecta los valores por agente");
    nodes["gc-advance-year"].value = "2024"; nodes["gc-advance-add-year"].fire("click");
    assert.equal(nodes["gc-advance-periods"].children.length, 3);
    nodes["gc-advance-form"].fire("submit", { preventDefault() {} }); await tick();
    assert.equal(calls[1].searchParams.get("rangos").split("|").length, 3);
    assert.match(calls[1].searchParams.get("rangos").split("|")[0], /^2024-/, "Un año añadido se coloca cronológicamente y puede reordenarse");
    assert.match(nodes["gc-advance-units"].textContent, /Se mantienen separadas/);
    assert.equal(nodes["gc-advance-head"].children[0].children.length, 8, "Panorama por agente con tres columnas de período");
    nodes["gc-advance-pdf"].fire("click");
    assert.match(pdfWindow.location.href, /\/PdfAvances\?/);
    assert.equal(new URL(pdfWindow.location.href, "http://local").searchParams.get("rangos").split("|").length, 3, "El PDF utiliza todos los períodos elegidos");
    nodes["gc-advance-print"].fire("click");
    assert.equal(new URL(pdfWindow.printUrl, "http://local").searchParams.get("inline"), "true", "Imprimir abre el PDF en el visor");
    console.log("OK UI avances: períodos dinámicos, barras/líneas, KPIs, alertas y unidades.");
})().catch(error => { console.error(error); process.exitCode = 1; });
