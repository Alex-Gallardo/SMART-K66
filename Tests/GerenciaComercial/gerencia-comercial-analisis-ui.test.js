const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

class Element {
    constructor(tag = "div") { this.tagName = tag; this.children = []; this.attributes = {}; this.listeners = {}; this.value = ""; this.hidden = false; this.disabled = false; this.style = {}; this.classList = { toggle() {} }; this._text = ""; }
    set textContent(v) { this._text = String(v); this.children = []; }
    get textContent() { return this._text + this.children.map(c => c.textContent).join(""); }
    get firstChild() { return this.children[0] || null; }
    appendChild(c) { this.children.push(c); return c; }
    insertBefore(c, before) { const i = this.children.indexOf(before); this.children.splice(i < 0 ? 0 : i, 0, c); return c; }
    replaceChildren(...children) { this._text = ""; this.children = children; }
    setAttribute(k, v) { this.attributes[k] = String(v); }
    getAttribute(k) { return this.attributes[k] || null; }
    addEventListener(k, cb) { (this.listeners[k] ||= []).push(cb); }
    dispatchEvent(e) { this.fire(e.type, e); }
    fire(k, e = {}) { for (const cb of this.listeners[k] || []) cb(e); }
    focus() {}
}
const ids = "gc gc-empresa gc-moneda gc-desde gc-hasta gc-analysis-search gc-analysis-order gc-analysis-size gc-analysis-front gc-analysis-front-label gc-analysis-status gc-analysis-rows gc-analysis-count gc-analysis-page gc-analysis-prev gc-analysis-next gc-analysis-export gc-fronts gc-report-money gc-report-percent gc-report-closed gc-report gc-history-modal gc-history-title gc-history-body gc-filters gc-refresh".split(" ");
const nodes = Object.fromEntries(ids.map(id => [id, new Element()]));
nodes["gc-empresa"].value = "FAES"; nodes["gc-moneda"].value = "GTQ"; nodes["gc-desde"].value = "2026-10-01"; nodes["gc-hasta"].value = "2026-10-24"; nodes["gc-analysis-order"].value = "actual";
const tabs = ["agentes", "clientes", "productos"].map(v => { const n = new Element("button"); n.setAttribute("data-analysis", v); return n; });
const periods = ["today", "week", "month", "custom"].map(v => { const n = new Element("button"); n.setAttribute("data-period", v); return n; });
const root = nodes.gc;
for (const kind of ["comparativo", "historico", "exportar-comparativo"]) root.setAttribute("data-" + kind, "/" + kind);
root.querySelectorAll = q => q === "[data-analysis]" ? tabs : q === "[data-period]" ? periods : [];
const document = { body: new Element("body"), getElementById: id => nodes[id], createElement: tag => new Element(tag), addEventListener() {} };
const agent = (code, front, current, prior) => ({ Empresa: "FAES", Codigo: code, Nombre: front, Frente: front, Moneda: "GTQ", Actual: current, Anterior: prior, Delta: current - prior, Variacion: 100 * (current - prior) / prior, Participacion: 100 * current / 300, CantidadActual: 0 });
const agents = [agent("1", "Sala de Ventas", 100, 80), agent("2", "Gerencia", 200, 160)];
const other = { Empresa: "FAES", Codigo: "C1", Nombre: "Cliente A", Familia: "Familia A", Moneda: "GTQ", Actual: 50, Anterior: 40, Delta: 10, Variacion: 25, Participacion: 16.67, CantidadActual: 5 };
let prints = 0, calls = [];
async function fetchMock(url) {
    const u = new URL(String(url), "http://local"); calls.push(u);
    const dimension = u.searchParams.get("dimension");
    const filas = dimension === "agentes" ? agents : [other];
    return { ok: true, redirected: false, json: async () => ({ filas, total: filas.length, anteriorDesde: "2025-10-01", anteriorHasta: "2025-10-24" }) };
}
const source = fs.readFileSync(path.join(__dirname, "../../DiamDev.Give.UI/Scripts/App/gerencia-comercial-analisis.js"), "utf8");
vm.runInNewContext(source, { document, window: { print() { prints++; }, location: {} }, fetch: fetchMock, URL, URLSearchParams, Intl, Date, Event: class { constructor(type) { this.type = type; } }, setTimeout, clearTimeout });
const tick = () => new Promise(resolve => setTimeout(resolve, 0));
(async () => {
    await tick();
    assert.equal(nodes["gc-analysis-rows"].children.length, 2, "Deben verse ambos agentes");
    assert.match(nodes["gc-fronts"].textContent, /Sala de Ventas/);
    nodes["gc-analysis-front"].value = "Gerencia"; nodes["gc-analysis-front"].fire("change");
    assert.equal(nodes["gc-analysis-rows"].children.length, 1, "El frente filtra el listado individual");
    nodes["gc-report-percent"].fire("click"); await tick();
    assert.equal(prints, 1, "El informe debe abrir impresión/PDF");
    assert.match(nodes["gc-report"].textContent, /Versión porcentual/);
    assert.match(nodes["gc-report"].textContent, /66[.,]7%/);
    assert.doesNotMatch(nodes["gc-report"].textContent, /GTQ\s*[\d,.]+/, "La versión porcentual no debe publicar importes");
    nodes["gc-report-money"].fire("click"); await tick();
    assert.equal(prints, 2);
    assert.match(nodes["gc-report"].textContent, /GTQ 300[.,]00/);
    assert(calls.some(u => u.searchParams.get("dimension") === "clientes" && u.searchParams.get("orden") === "caida"));
    nodes["gc-report-closed"].fire("click"); await tick();
    assert.equal(prints, 3, "El cierre mensual debe generar el informe en una acción");
    const lastMonth = new Date(); lastMonth.setDate(0);
    assert.equal(nodes["gc-hasta"].value.slice(0, 7), lastMonth.getFullYear() + "-" + String(lastMonth.getMonth() + 1).padStart(2, "0"));
    console.log("OK UI análisis: frentes, informe porcentual sin importes e impresión.");
})().catch(e => { console.error(e); process.exitCode = 1; });
