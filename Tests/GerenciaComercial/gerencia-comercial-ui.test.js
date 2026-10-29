const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

class Element {
    constructor(tag = "div") {
        this.tagName = tag; this.children = []; this.attributes = {}; this.listeners = {};
        this.hidden = false; this.disabled = false; this.value = ""; this.style = {};
        this.classList = { toggle() {} }; this._text = "";
    }
    set textContent(value) { this._text = String(value); this.children = []; }
    get textContent() { return this._text + this.children.map(child => child.textContent).join(""); }
    get childNodes() { return this.children; }
    appendChild(child) { this.children.push(child); return child; }
    replaceChildren(...children) { this._text = ""; this.children = children; }
    setAttribute(name, value) { this.attributes[name] = String(value); }
    getAttribute(name) { return this.attributes[name] || null; }
    addEventListener(name, callback) { (this.listeners[name] ||= []).push(callback); }
    fire(name, event = {}) { for (const callback of this.listeners[name] || []) callback(event); }
    getBoundingClientRect() { return { left: 0, width: 610 }; }
    querySelector(selector) { return selector === ".gc-modal-close" ? closeButton : null; }
    focus() {}
}

const ids = "gc gc-desde gc-hasta gc-empresa gc-moneda gc-estado gc-agente gc-cliente gc-alert gc-kpis gc-trend gc-trend-stats gc-trend-day gc-companies gc-products-top gc-agents-top gc-clients-top gc-updated gc-rows gc-count gc-page gc-prev gc-next gc-agente-wrap gc-cliente-wrap gc-search gc-size gc-export gc-modal gc-modal-body gc-modal-subtitle gc-modal-title gc-filters gc-refresh gc-advanced-apply gc-advanced-clear gc-explorer-content gc-unavailable gc-unavailable-title".split(" ");
const nodes = Object.fromEntries(ids.map(id => [id, new Element()]));
const closeButton = new Element("button");
const periods = ["today", "week", "month", "custom"].map(value => { const e = new Element("button"); e.setAttribute("data-period", value); return e; });
const currencies = ["GTQ", "USD"].map(value => { const e = new Element("button"); e.setAttribute("data-trend-currency", value); return e; });
const tabs = ["ventas", "pedidos", "clientes", "productos", "agentes", "pedidosSql", "clientesSql", "productosSql", "agentesSql"].map(value => {
    const e = new Element("button"); e.setAttribute("data-type", value);
    if (["productos", "clientesSql", "productosSql"].includes(value)) e.setAttribute("data-unavailable", { productos: "Productos SAP", clientesSql: "Clientes SQL", productosSql: "Productos SQL" }[value]);
    return e;
});
const root = nodes.gc;
for (const kind of ["resumen", "listar", "detalle", "exportar"]) root.setAttribute("data-" + kind, "/" + kind);
root.querySelectorAll = selector => ({ "[data-period]": periods, "[data-trend-currency]": currencies, "[data-type]": tabs })[selector] || [];
root.querySelector = selector => {
    const match = selector.match(/^\[data-type="([^"]+)"\]\[data-unavailable\]$/);
    return match ? tabs.find(tab => tab.getAttribute("data-type") === match[1] && tab.getAttribute("data-unavailable")) : null;
};
nodes["gc-modal"].hidden = true;
const document = {
    body: new Element("body"), activeElement: new Element(),
    getElementById: id => nodes[id], createElement: tag => new Element(tag), createElementNS: (_, tag) => new Element(tag),
    createTextNode: value => { const e = new Element("text"); e.textContent = value; return e; }, addEventListener() {}
};
let listCalls = 0;
const summary = {
    desde: "2026-10-27", hasta: "2026-10-28", actualizado: "2026-10-28 10:00", advertencias: [],
    ventas: [
        { Dia: "2026-10-27", Moneda: "GTQ", Empresa: "FAES", Neto: 1000, Facturas: 2, NotasCredito: 1 },
        { Dia: "2026-10-28", Moneda: "GTQ", Empresa: "BOLIK", Neto: 500, Facturas: 1, NotasCredito: 0 },
        { Dia: "2026-10-27", Moneda: "USD", Empresa: "GRACO", Neto: 10, Facturas: 1, NotasCredito: 0 }
    ], cobros: [], catalogos: [], pedidosK66: [], productos: [], agentes: [], clientes: []
};
const detail = { secciones: { Principal: [{ Documento: 123, Fecha: "2026-10-27", Cliente: "Cliente", CodigoCliente: "C1", Agente: "Ana", Moneda: "GTQ", Subtotal: 100, Impuesto: 12, Total: 112, Estado: "Cerrado", BASEVAT: 999 }], Productos: [{ CodigoProducto: "P1", Producto: "Artículo", Cantidad: 2, Unidad: "UN", PrecioUnitario: 50, Importe: 100 }] } };
async function fetchMock(url) {
    const endpoint = String(url).split("?")[0];
    if (endpoint === "/listar") listCalls++;
    const data = endpoint === "/resumen" ? summary : endpoint === "/detalle" ? detail : { filas: [{ Empresa: "FAES", Codigo: "123", Nombre: "Cliente", Tipo: "Factura", Fuente: "SAP", Fecha: "2026-10-27", Moneda: "GTQ", Importe: 112 }], total: 1 };
    return { ok: true, redirected: false, json: async () => data };
}
const source = fs.readFileSync(path.join(__dirname, "../../DiamDev.Give.UI/Scripts/App/gerencia-comercial.js"), "utf8");
vm.runInNewContext(source, { document, window: { location: {} }, fetch: fetchMock, URLSearchParams, Intl, Date, setTimeout, clearTimeout });

(async () => {
    await new Promise(resolve => setTimeout(resolve, 0));
    assert.match(nodes["gc-trend-day"].textContent, /GTQ 500\.00/);
    assert.match(nodes["gc-trend-stats"].textContent, /GTQ 1,500\.00/);
    currencies[1].fire("click");
    assert.match(nodes["gc-trend-stats"].textContent, /USD 10\.00/);
    assert.doesNotMatch(nodes["gc-trend-stats"].textContent, /1,500/);
    assert.match(nodes["gc-trend"].children[0].getAttribute("aria-label"), /USD/);
    const slider = nodes["gc-trend"].children.find(child => child.tagName === "label").children[0];
    slider.value = "0"; slider.fire("input");
    assert.match(nodes["gc-trend-day"].textContent, /GRACO · USD 10\.00/);
    slider.value = "1"; slider.fire("input");
    assert.match(nodes["gc-trend-day"].textContent, /Sin movimientos en este día/);
    nodes["gc-trend"].children[0].children.at(-1).fire("mousemove", { clientX: 0 });
    assert.equal(slider.value, "0", "El día seleccionado debe seguir el cursor sobre la gráfica");
    const before = listCalls;
    for (const type of ["productos", "clientesSql", "productosSql"]) {
        tabs.find(tab => tab.getAttribute("data-type") === type).fire("click");
        assert.equal(nodes["gc-explorer-content"].hidden, true);
        assert.match(nodes["gc-unavailable-title"].textContent, /en desarrollo/);
    }
    assert.equal(listCalls, before, "Las pestañas en desarrollo no deben consultar listados");
    tabs[0].fire("click");
    await new Promise(resolve => setTimeout(resolve, 0));
    assert.equal(nodes["gc-explorer-content"].hidden, false);
    assert.equal(listCalls, before + 1);
    const action = nodes["gc-rows"].children[0].children.at(-1).children[0];
    action.fire("click");
    await new Promise(resolve => setTimeout(resolve, 0));
    assert.match(nodes["gc-modal-body"].textContent, /Documento y cliente/);
    assert.match(nodes["gc-modal-body"].textContent, /Artículo/);
    assert.doesNotMatch(nodes["gc-modal-body"].textContent, /BASEVAT/);
    console.log("OK UI: moneda, día, pestañas en desarrollo y modal de ventas.");
})().catch(error => { console.error(error); process.exitCode = 1; });
