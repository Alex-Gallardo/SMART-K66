"use strict";
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { chromium } = require(process.env.BNC_PLAYWRIGHT_PATH || "playwright");
const root = path.resolve(__dirname, "../..");
const read = p => fs.readFileSync(path.join(root, p), "utf8");

(async () => {
    const browser = await chromium.launch({ channel: "chrome", headless: true });
    try {
        const page = await browser.newPage({ viewport: { width: 1200, height: 720 } });
        const errors = [];
        page.on("pageerror", e => errors.push(e.message));
        await page.setContent('<main class="bnc bnc-shell bncdd" id="bncDetalleDashboard" data-empresa="BOLIK" data-borrador="BWB-1" ' +
            'data-url-sap-indicadores="/sap" data-url-documento-previo="/documento"><input name="__RequestVerificationToken" value="test" />' +
            '<section class="bncdd-card"><div class="bncdd-links">' +
            '<div class="bncdd-link" data-doc-entry="123"><span class="bncdd-link-status">Sin verificar ahora</span></div>' +
            '<div class="bncdd-link" data-doc-entry="456"><span class="bncdd-link-status">Sin verificar ahora</span></div>' +
            '</div><div id="bncddLiveSap"></div></section></main>');
        await page.addScriptTag({ content: read("DiamDev.Give.UI/Scripts/jquery-1.10.2.js") });
        await page.addStyleTag({ content: read("DiamDev.Give.UI/Content/borrador-nc-dashboard-detalle.css") });
        await page.addStyleTag({ content: read("DiamDev.Give.UI/Content/borrador-nc-sap-indicadores.css") });
        await page.evaluate(() => {
            window.requests = [];
            $.ajax = options => {
                const deferred = $.Deferred(), request = deferred.promise();
                request.abort = () => {};
                requests.push({ options, deferred });
                return request;
            };
        });
        await page.addScriptTag({ content: read("DiamDev.Give.UI/Scripts/App/BorradorNc-SapIndicadores.js") });
        await page.addScriptTag({ content: read("DiamDev.Give.UI/Scripts/App/BorradorNc-DashboardDetalle.js") });
        assert.equal(await page.evaluate(() => requests[0].options.data.origen), "dashboard");
        assert.equal(await page.evaluate(() => requests[0].options.data.__RequestVerificationToken), "test");
        await page.evaluate(() => requests[0].deferred.resolve({ ok: true, data: [{ IdEmpresa: "BOLIK", IdBorrador: "BWB-1", Disponible: true,
            NcVigentes: 1, NcCanceladas: 1, ActualizadoEn: "2026-10-06T12:00:00Z", Documentos: [
                { DocEntry: 123, Documento: "2000434", Factura: "100", Cancelado: false },
                { DocEntry: 456, Documento: "2000435", Factura: "101", Cancelado: true }
            ] }] }));
        assert.deepEqual(await page.locator(".bncdd-link-status").allTextContents(), ["Vigente en SAP", "Cancelada en SAP"]);
        await page.locator("[data-sap-refresh]").click();
        assert.equal(await page.evaluate(() => requests.length), 2);
        await page.evaluate(() => { requests[1].deferred.reject({ status: 503 }); });
        assert.deepEqual(await page.locator(".bncdd-link-status").allTextContents(), ["Sin verificar ahora", "Sin verificar ahora"]);
        for (const width of [1200, 768, 375, 320]) {
            await page.setViewportSize({ width, height: 720 });
            assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
        }
        assert.deepEqual(errors, []);
        console.log("OK navegador: vínculo confirmado, vigencia/cancelación SAP, error sin falso positivo y responsive.");
    } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
