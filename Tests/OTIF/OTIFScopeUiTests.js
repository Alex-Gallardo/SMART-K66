"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const vm = require("vm");

const view = fs.readFileSync(path.resolve(__dirname,
    "../../DiamDev.Give.UI/Views/OTIF/Index.cshtml"), "utf8");
const inline = [...view.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)]
    .map(match => match[1]).filter(Boolean)[0];

function response(status, body, contentType = "application/json; charset=utf-8") {
    return {
        ok: status >= 200 && status < 300,
        status,
        headers: { get: key => key === "content-type" ? contentType : null },
        json: async () => body
    };
}

async function runCase(scope, dataResponse, scopeStatus = 200) {
    const elements = new Map();
    const calls = [];
    const viz = {
        rows: null, compareRows: null,
        setRawData(rows) { this.rows = rows; calls.push("setRawData:" + rows.length); },
        clearCompareRawData() { this.compareRows = null; calls.push("clearCompare"); },
        setCompareRawData(rows) { this.compareRows = rows; },
        setDateRange() {}, applyPalette() {}, init() {}
    };
    function element(id) {
        if (!elements.has(id)) elements.set(id, {
            value: "", disabled: id === "empresa" || id === "refresh", checked: false, textContent: "",
            options: [], addEventListener() {},
            replaceChildren(...options) {
                this.options = options;
                this.value = options.length ? options[0].value : "";
            }
        });
        return elements.get(id);
    }
    const context = vm.createContext({
        document: { getElementById: element },
        Option: function Option(text, value) { this.text = text; this.value = value; },
        OTIFViz: viz, console: { error() {} }, Date, Set, Promise,
        fetch: async url => {
            calls.push(url.includes("GetScope") ? "scope" : "data:" + url);
            return url.includes("GetScope") ? response(scopeStatus, scope) : dataResponse;
        }
    });
    vm.runInContext(inline.replace(/initializeScope\(\);\s*$/, "globalThis.scopeDone = initializeScope();"), context);
    await context.scopeDone;
    return { context, elements, calls, viz, element };
}

(async () => {
    const empty = await runCase({ global: false, empresas: [] }, response(200, { rows: [] }));
    assert.deepStrictEqual(empty.calls.filter(c => c.startsWith("data:")), [],
        "Sin asignaciones no debe haber consulta HANA.");
    assert.strictEqual(empty.element("empresa").disabled, true);
    assert.strictEqual(empty.viz.rows.length, 0);
    assert(empty.element("status").textContent.includes("Usuario_Empresa"));

    const scopeError = await runCase({}, response(200, { rows: [] }), 500);
    assert.deepStrictEqual(scopeError.calls.filter(c => c.startsWith("data:")), [],
        "Un error al resolver el alcance no debe consultar HANA.");
    assert.strictEqual(scopeError.element("empresa").disabled, true);

    const restricted = await runCase({ global: false,
        empresas: [{ code: "BOLIK", name: "Bolik" }] }, response(200, { rows: [{ OrderNumber: 10 }] }));
    assert.deepStrictEqual(restricted.element("empresa").options.map(x => x.value), ["BOLIK"]);
    assert(restricted.calls.some(c => c.startsWith("data:") && c.includes("empresa=BOLIK")));
    assert.strictEqual(restricted.viz.rows.length, 1);
    assert.strictEqual(restricted.element("refresh").disabled, false);
    restricted.element("compareToggle").checked = true;
    const beforeCompare = restricted.calls.filter(c => c.startsWith("data:")).length;
    await vm.runInContext("load()", restricted.context);
    const compareCalls = restricted.calls.filter(c => c.startsWith("data:")).slice(beforeCompare);
    assert.strictEqual(compareCalls.length, 2, "Ambos períodos deben consultar la API protegida.");
    assert(compareCalls.every(c => c.includes("empresa=BOLIK")));
    assert.strictEqual(restricted.viz.compareRows.length, 1);

    const global = await runCase({ global: true,
        empresas: [
            { code: "GRACO", name: "Graco" },
            { code: "BOLIK", name: "Bolik" },
            { code: "ESCOCESA", name: "Escocesa" }
        ] }, response(200, { rows: [] }));
    assert.deepStrictEqual(global.element("empresa").options.map(x => x.value),
        ["GRACO", "BOLIK", "ESCOCESA"]);
    assert(global.element("asOfLine").textContent.includes("Vista global"));

    const denied = await runCase({ global: false,
        empresas: [{ code: "GRACO", name: "Graco" }] }, response(403, {}));
    assert.strictEqual(denied.viz.rows.length, 0,
        "Un 403 debe borrar los resultados visibles previamente.");
    assert(denied.element("status").textContent.includes("No tienes acceso"));

    console.log("OTIFScopeUiTests: OK");
})().catch(error => { console.error(error); process.exitCode = 1; });
