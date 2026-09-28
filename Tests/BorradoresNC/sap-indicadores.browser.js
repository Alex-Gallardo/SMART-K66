"use strict";
// Browser test of the actual shared component + jQuery. No SQL/HANA or production credentials.
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { chromium } = require(process.env.BNC_PLAYWRIGHT_PATH || "playwright");
const root = path.resolve(__dirname, "../..");
const read = p => fs.readFileSync(path.join(root, p), "utf8");

(async () => {
    const browser = await chromium.launch({ channel: "chrome", headless: true });
    try {
        const page = await browser.newPage();
        const errors = [];
        page.on("pageerror", e => errors.push(e.message));
        await page.setContent('<html><head></head><body style="margin:0"><main id="test" data-sap-intervalo="60" ' +
            'data-url-sap-indicadores="/test" data-url-documento-previo="/detalle" style="max-width:100%;padding:16px;box-sizing:border-box">' +
            '<input name="__RequestVerificationToken" value="test-token"><div id="slot"></div></main></body></html>');
        await page.addScriptTag({ content: read("DiamDev.Give.UI/Scripts/jquery-1.10.2.js") });
        await page.addStyleTag({ content: read("DiamDev.Give.UI/Content/borrador-nc-sap-indicadores.css") });
        await page.evaluate(() => {
            window.requests = []; window.ticks = []; window.clock = 100000;
            Date.now = () => window.clock;
            window.setInterval = f => { window.ticks.push(f); return window.ticks.length; };
            window.jQuery.ajax = options => {
                const deferred = window.jQuery.Deferred();
                const request = deferred.promise();
                // Late completions after cancellation are intentionally possible to exercise the generation guard.
                request.abort = () => { request.aborted = true; };
                window.requests.push({ options, deferred, request });
                return request;
            };
        });
        await page.addScriptTag({ content: read("DiamDev.Give.UI/Scripts/App/BorradorNc-SapIndicadores.js") });
        await page.evaluate(() => {
            window.a = { IdEmpresa: "GRACO", IdBorrador: "BWG-1" };
            window.b = { IdEmpresa: "GRACO", IdBorrador: "BWG-2" };
            window.component = window.BorradorNcSapIndicadores.crear({ root: $("#test"), origen: "seguimiento" });
            $("#slot").html(window.BorradorNcSapIndicadores.plantilla(a, true));
            component.mostrar([a]);
        });
        assert.match(await page.locator("#slot").innerText(), /por consultar/);
        assert.equal(await page.evaluate(() => requests[0].options.data.__RequestVerificationToken), "test-token");
        assert.equal(await page.evaluate(() => requests[0].options.type), "POST");
        await page.evaluate(() => requests[0].deferred.resolve({ ok: true, data: [{ ...a,
            Disponible: true, ActualizadoEn: "2026-09-28T16:00:00Z", NcVigentes: 1, NcCanceladas: 1,
            Documentos: [{ Documento: '<script>window.injected=true</script>', Factura: "123", Cancelado: false }] }] }));
        assert.match(await page.locator("#slot").innerText(), /1 NC vigente/);
        assert.match(await page.locator("#slot").innerText(), /1 cancelada/);
        assert.match(await page.locator("#slot").innerText(), /Consulta SAP:/);
        await page.locator("summary").click();
        assert.equal(await page.locator("details").getAttribute("open"), "");
        assert.equal(await page.evaluate(() => window.injected), undefined);
        const href = await page.locator("details a").getAttribute("href");
        assert.ok(href.includes("factura=123") && href.includes("clase=NOTA_CREDITO"));
        assert.equal(await page.locator("details a").getAttribute("target"), "_blank");
        // A manual refresh failure preserves the last successful result and an open disclosure.
        await page.locator("[data-sap-refresh]").click();
        await page.evaluate(() => { requests[1].deferred.reject({ status: 503 }); });
        assert.match(await page.locator("#slot").innerText(), /1 NC vigente/);
        assert.match(await page.locator("#slot").innerText(), /conserva la última consulta/);
        assert.equal(await page.locator("details").getAttribute("open"), "");
        assert.equal(await page.evaluate(() => document.activeElement.hasAttribute("data-sap-refresh")), true);
        // A success with no active NC replaces—not accumulates—the previous result.
        await page.locator("[data-sap-refresh]").click();
        await page.evaluate(() => requests[2].deferred.resolve({ ok: true, data: [{ ...a,
            Disponible: true, ActualizadoEn: "2026-09-28T16:01:00Z", NcVigentes: 0, NcCanceladas: 1, Documentos: [] }] }));
        assert.match(await page.locator("#slot").innerText(), /Sin NC vigente/);
        assert.doesNotMatch(await page.locator("#slot").innerText(), /No se pudo/);
        // Do not poll a background tab. Resume when visible, respecting the configured interval.
        await page.evaluate(() => {
            window.clock += 60001;
            Object.defineProperty(document, "hidden", { configurable: true, value: true });
            window.ticks[0]();
        });
        assert.equal(await page.evaluate(() => requests.length), 3);
        await page.evaluate(() => {
            Object.defineProperty(document, "hidden", { configurable: true, value: false });
            document.dispatchEvent(new Event("visibilitychange"));
        });
        assert.equal(await page.evaluate(() => requests.length), 4);
        // Change draft during an in-flight request; ignore its late response.
        await page.evaluate(() => {
            $("#slot").html(BorradorNcSapIndicadores.plantilla(b, true));
            component.mostrar([b]);
            requests[3].deferred.resolve({ ok: true, data: [{ ...a, Disponible: true, NcVigentes: 99 }] });
        });
        assert.match(await page.locator("#slot").innerText(), /por consultar/);
        assert.equal(await page.evaluate(() => requests[3].request.aborted), true);
        // A missing/failed result is not a "zero NC" result.
        await page.evaluate(() => requests[4].deferred.resolve({ ok: true, data: [{ ...b, Disponible: false }] }));
        assert.match(await page.locator("#slot").innerText(), /no disponible/);
        assert.doesNotMatch(await page.locator("#slot").innerText(), /Sin NC vigente/);
        // More than 100 rows are serialized into bounded batches.
        await page.evaluate(() => {
            window.many = Array.from({ length: 205 }, (_, i) => ({ IdEmpresa: "BOLIK", IdBorrador: "BWB-" + i }));
            $("#slot").html(BorradorNcSapIndicadores.plantilla(many[0], true));
            component.mostrar(many);
        });
        for (const [index, size] of [[5, 100], [6, 100], [7, 5]]) {
            assert.equal(await page.evaluate(i => requests[i].options.data.claves.length, index), size);
            if (index === 5) await page.evaluate(() => Object.defineProperty(document, "hidden", { configurable: true, value: true }));
            await page.evaluate(i => requests[i].deferred.resolve({ ok: true, data:
                requests[i].options.data.claves.map(k => { const [IdEmpresa, IdBorrador] = k.split("|");
                    return { IdEmpresa, IdBorrador, Disponible: true, ActualizadoEn: "2026-09-28T16:02:00Z",
                        NcVigentes: 0, NcCanceladas: 0, Documentos: [] }; }) }), index);
            if (index === 5) {
                assert.equal(await page.evaluate(() => requests.length), 6, "Do not initiate the next batch in a hidden tab.");
                await page.evaluate(() => {
                    Object.defineProperty(document, "hidden", { configurable: true, value: false });
                    document.dispatchEvent(new Event("visibilitychange"));
                });
            }
        }
        assert.equal(await page.evaluate(() => requests.length), 8);
        // Responsive indicator layout: no document-level overflow, even with long labels and errors.
        for (const width of [375, 768, 1440]) {
            await page.setViewportSize({ width, height: 800 });
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
        }
        // The existing full antecedents component also refreshes, retaining cached data after SAP failure.
        await page.addScriptTag({ content: read("DiamDev.Give.UI/Scripts/App/BorradorNc-Fechas.js") });
        await page.addScriptTag({ content: read("DiamDev.Give.UI/Scripts/App/BorradorNc-DocumentosPrevios.js") });
        await page.evaluate(() => {
            $("#test").append(BorradorNcDocumentosPrevios.plantilla("priorTest"));
            window.prior = BorradorNcDocumentosPrevios.crear({ id: "priorTest", url: "/previos", detalleUrl: "/detalle" });
            prior.cargar(a);
            requests[8].deferred.resolve({ ok: true, data: [{ Clase: "NOTA_CREDITO", Documento: "999",
                Factura: "123", Moneda: "GTQ", Total: 12, Fecha: "2026-09-28", Comentarios: "NC de prueba" }] });
            prior.cargar(a);
        });
        assert.equal(await page.evaluate(() => requests.length), 9, "Use a fresh cache without querying SAP twice.");
        assert.match(await page.locator("#priorTest").innerText(), /NC de prueba/);
        await page.evaluate(() => { prior.cargar(a, true); requests[9].deferred.reject({ status: 503 }); });
        assert.match(await page.locator("#priorTest").innerText(), /NC de prueba/);
        assert.match(await page.locator("#priorTest").innerText(), /No se pudo actualizar SAP/);
        await page.evaluate(() => { prior.cargar(a); });
        assert.match(await page.locator("#priorTest").innerText(), /Datos sin actualizar/);
        await page.evaluate(() => { prior.cargar(b); requests[10].deferred.resolve({ ok: false }); });
        assert.doesNotMatch(await page.locator("#priorTest").innerText(), /No se encontraron/);
        assert.match(await page.locator("#priorTest").innerText(), /No fue posible/);
        await page.evaluate(() => {
            prior.cargar(a, true); prior.cargar(b);
            requests[11].deferred.resolve({ ok: true, data: [{ Documento: "OLDDATA", Factura: "123" }] });
        });
        assert.doesNotMatch(await page.locator("#priorTest").innerText(), /OLDDATA/);
        await page.evaluate(() => { requests[12].deferred.resolve({ ok: true, data: [] }); });
        assert.match(await page.locator("#priorTest").innerText(), /No se encontraron/);
        assert.match(await page.locator("#priorTest").innerText(), /Consulta SAP:/);
        assert.deepEqual(errors, []);
        console.log("OK navegador: NC, canceladas, errores sin pérdida, caché, refresco, visibilidad, carreras, lotes y responsive.");
    } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
