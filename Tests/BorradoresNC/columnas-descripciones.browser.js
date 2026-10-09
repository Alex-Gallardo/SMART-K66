"use strict";
// Ejecuta los renderizadores reales con datos de prueba, sin consultar SQL ni SAP.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const os = require("node:os");
const { chromium } = require(process.env.BNC_PLAYWRIGHT_PATH || "playwright");
const root = path.resolve(__dirname, "../..");
const read = p => fs.readFileSync(path.join(root, p), "utf8");
const app = "DiamDev.Give.UI/";

function table(view, body, classes) {
    const source = read(app + "Views/BorradorNc/" + view + ".cshtml");
    const end = source.indexOf('<tbody id="' + body + '"');
    const start = source.lastIndexOf("<thead", end);
    assert.ok(start >= 0 && end > start);
    return '<div class="bnc-table-wrap bncd-table-wrap"><table class="' + classes + '">' +
        source.slice(start, end) + '<tbody id="' + body + '"></tbody></table></div>';
}

function expose(name, exportCode) {
    let source = read(app + "Scripts/App/BorradorNc-" + name + ".js");
    if (name === "Dashboard") {
        source = source.replace(/    actualizarFiltrosAvanzados\(\);\s+cargar\(\);\s+\}\)\(window.jQuery\);\s*$/, exportCode + '\n})(window.jQuery);');
    } else {
        const start = source.lastIndexOf("    $(function ()");
        assert.ok(start >= 0);
        source = source.slice(0, start) + exportCode + '\n})(window.jQuery);';
    }
    assert.ok(source.includes(exportCode));
    return source;
}

(async () => {
    const browser = await chromium.launch({ channel: "chrome", headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1280, height: 1100 } });
        const errors = [];
        page.on("pageerror", e => errors.push(e.message));
        await page.setContent('<html lang="es"><head></head><body style="margin:0;font-family:Segoe UI,Arial,sans-serif">' +
            '<main style="padding:16px;box-sizing:border-box;max-width:100%">' +
            '<section id="bncApp" class="bnc" data-url-sap-indicadores="/sap" data-url-factura-borrador="/factura">' +
            '<div id="bncSeguimiento" class="active">' + table("Index", "bncFollowBody", "table bnc-table") + '</div>' +
            '<article id="bncFollowDetail" class="bnc-detail-panel" style="width:520px;max-width:100%;margin-top:16px"></article></section>' +
            '<section id="bncAuthApp" class="bnc" data-url-sap-indicadores="/sap"><div class="bnc-auth-list">' +
            table("Autorizaciones", "bncAuthBody", "table bnc-table") + '</div>' +
            '<article id="bncAuthDetail" class="bnc-detail-panel" style="width:520px;max-width:100%;margin-top:16px"></article></section>' +
            '<section id="bncDashboard" class="bncd" data-url-sap-indicadores="/sap">' +
            table("DashboardBNC", "dashboardFilas", "bncd-table") + '</section>' +
            '</main></body></html>');
        // Base table layout is supplied by Bootstrap in the application.
        await page.addStyleTag({ content: 'table{border-collapse:collapse} .text-right{text-align:right}.text-center{text-align:center}' });
        for (const css of ["borrador-nc.css", "borrador-nc-dashboard.css", "borrador-nc-sap-indicadores.css"]) {
            await page.addStyleTag({ content: read(app + "Content/" + css) });
        }
        await page.addScriptTag({ content: read(app + "Scripts/jquery-1.10.2.js") });
        await page.evaluate(() => {
            window.requests = [];
            $.ajax = options => {
                const deferred = $.Deferred();
                const request = deferred.promise();
                request.abort = () => {};
                requests.push({ options, deferred });
                return request;
            };
            // Unrelated components are intentionally absent from this UI-only fixture.
            const empty = { crear: () => ({ mostrar() {}, limpiar() {}, invalidar() {} }), plantilla: () => "" };
            window.BorradorNcFacturasDetalle = Object.assign({}, empty, {
                urlFacturaBorrador: (url, x, d) => url + "?documento=" + encodeURIComponent(d.Documento)
            });
            window.BorradorNcDocumentosPrevios = empty;
            window.BorradorNcAdjuntos = empty;
        });
        for (const js of ["Fechas", "SapIndicadores"]) {
            await page.addScriptTag({ content: read(app + "Scripts/App/BorradorNc-" + js + ".js") });
        }
        await page.addScriptTag({ content: expose("Index",
            'window.followTest = { state: state, lista: renderSeguimiento, detalle: renderDetalle };') });
        await page.addScriptTag({ content: expose("Autorizaciones",
            'window.authTest = { state: state, lista: renderLista, detalle: renderDetalle };') });
        await page.addScriptTag({ content: expose("Dashboard", 'window.dashboardTest = { lista: pintar };') });
        await page.evaluate(() => {
            window.description = "VALE NO. 085428 TORRE PUERTO SAN JOSE MOTIVO CRUCE DE BARRA CRAYON FABIK CORTO DEVOLUCION.\nNO. DCB26-00082 SOPORTE 264307 <img src=x onerror=window.injected=true>";
            window.draft = { IdEmpresa: "BOLIK", IdBorrador: "BWB-00066", IdCliente: "CL0112", Nombre: "UNISUPER, S.A.",
                Agente: "BERNARDA AGUILAR", Estado: "AUTORIZADO", Moneda: "GTQ", Total: 27.58,
                Fecha: "2026-09-22", IdUsr: "mlopez", Facturas: 2, Adjuntos: 0,
                Detalles: [
                    { Documento: "1007388", Concepto: "OTROS", FechaDoc: "2026-08-06", Moneda: "GTQ", Importe: 27.58, Descripcion: description },
                    { Documento: "1007389", Concepto: "OTROS", FechaDoc: "2026-08-07", Moneda: "GTQ", Importe: 1234567.89, Descripcion: "" }
                ] };
            followTest.state.seguimiento = [draft];
            followTest.state.seleccionado = { empresa: draft.IdEmpresa, id: draft.IdBorrador };
            followTest.lista(); followTest.detalle(draft);
            authTest.state.pendientes = [Object.assign({}, draft, { Estado: "PENDIENTE" })];
            authTest.lista(); authTest.detalle(draft);
            dashboardTest.lista({ Filas: [draft], TotalFilas: 1, TamanoPagina: 25 });
            requests.forEach(r => r.deferred.resolve({ ok: true, data: [Object.assign({}, draft, {
                Disponible: true, NcVigentes: 0, NcCanceladas: 0, ActualizadoEn: "2026-09-28T16:00:00Z", Documentos: []
            })] }));
        });

        // The same live component occupies one new cell, immediately after Estado.
        for (const [id, total, stateIndex] of [["bncFollowBody", 8, 5], ["bncAuthBody", 7, 4], ["dashboardFilas", 14, 7]]) {
            const result = await page.locator("#" + id).evaluate((body, stateIndex) => {
                const row = body.rows[0];
                const headers = body.closest("table").tHead.rows[0].cells;
                return { cells: row.cells.length, headers: headers.length,
                    stateHeader: headers[stateIndex].textContent.trim(), sapHeader: headers[stateIndex + 1].textContent.trim(),
                    stateIndicators: row.cells[stateIndex].querySelectorAll(".bnc-sap-indicator").length,
                    sapIndicators: row.cells[stateIndex + 1].querySelectorAll(".bnc-sap-indicator").length };
            }, stateIndex);
            assert.deepEqual(result, { cells: total, headers: total, stateHeader: "Estado", sapHeader: "NC en SAP", stateIndicators: 0, sapIndicators: 1 });
        }
        const countBefore = await page.evaluate(() => requests.length);
        await page.locator("#bncFollowBody [data-sap-refresh]").click();
        assert.equal(await page.evaluate(() => requests.length), countBefore + 1, "El botón SAP conserva su actualización");
        assert.equal(await page.locator("#bncFollowDetail .bnc-sap-detail .bnc-sap-indicator").count(), 1);
        for (const [panel, columns] of [["bncFollowDetail", 4], ["bncAuthDetail", 6]]) {
            const descriptions = page.locator("#" + panel + " .bnc-document-description");
            assert.deepEqual(await descriptions.locator("p").allTextContents(),
                [await page.evaluate(() => description), "Sin descripción"]);
            assert.deepEqual(await descriptions.locator("small").allTextContents(),
                ["Descripción del documento 1007388", "Descripción del documento 1007389"]);
            assert.equal(await page.locator("#" + panel + " .bnc-document-description-row td[colspan='" + columns + "']").count(), 2);
            assert.equal(await page.locator("#" + panel + " .bnc-linked-invoice-action[target='_blank']").count(), 2);
            assert.equal(await page.locator("#" + panel + " .bnc-linked-invoice-table thead th").count(), columns);
        }
        assert.equal(await page.evaluate(() => window.injected), undefined);
        assert.equal(await page.locator(".bnc-document-description img").count(), 0);

        // Readability and overflow: local scrolling only, at desktop/tablet/mobile widths.
        for (const width of [1280, 768, 520, 375, 320]) {
            await page.setViewportSize({ width, height: 1100 });
            const layout = await page.evaluate(() => {
                const panel = document.querySelector("#bncFollowDetail");
                const box = panel.querySelector(".bnc-document-description");
                const table = panel.querySelector("table");
                const wrap = table.parentElement;
                return { viewport: innerWidth, page: document.documentElement.scrollWidth,
                    panel: panel.getBoundingClientRect().width, description: box.getBoundingClientRect().width,
                    table: table.getBoundingClientRect().width, wrap: wrap.getBoundingClientRect().width,
                    overflow: getComputedStyle(wrap).overflowX,
                    whiteSpace: getComputedStyle(box.querySelector("p")).whiteSpace };
            });
            assert.ok(layout.page <= layout.viewport + 1, "Sin desbordamiento global a " + width + "px");
            assert.ok(layout.description >= Math.min(layout.panel * .85, 300), "Descripción legible a " + width + "px");
            assert.equal(layout.overflow, "auto");
            assert.equal(layout.whiteSpace, "pre-line");
            assert.ok(layout.table >= layout.wrap - 1);
            assert.ok(await page.locator("#bncFollowDetail .bnc-money").evaluateAll(cells => cells.every(cell => {
                const range = document.createRange();
                range.selectNodeContents(cell);
                const text = range.getBoundingClientRect();
                const bounds = cell.getBoundingClientRect();
                return text.left >= bounds.left && text.right <= bounds.right + 1;
            })), "Los importes largos no invaden otra celda a " + width + "px");
        }
        await page.setViewportSize({ width: 768, height: 1100 });
        const screenshot = path.join(os.tmpdir(), "bnc-columnas-descripciones-" + Date.now() + ".png");
        await page.locator("#bncFollowDetail").screenshot({ path: screenshot });
        assert.deepEqual(errors, []);
        console.log("OK: columnas SAP, actualización intacta, descripciones completas/seguras y responsive (1280–320px).");
        console.log("Captura: " + screenshot);
    } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
