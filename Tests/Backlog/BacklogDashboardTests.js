"use strict";

const assert = require("assert");
const path = require("path");

const dashboard = require(path.resolve(
    __dirname,
    "..",
    "..",
    "DiamDev.Give.UI",
    "Scripts",
    "App",
    "backlog-viz.js"));

const core = dashboard.__test;
assert(core, "La lógica verificable del dashboard debe estar exportada.");

assert.strictEqual(core.normalizeFechaToISO("05/01/2026"), "2026-01-05");
assert.strictEqual(
    core.normalizeFechaToISO("2026-08-31T00:00:00"),
    "2026-08-31");
assert.strictEqual(
    core.formatLocalDateISO(new Date(2026, 7, 31, 23, 30)),
    "2026-08-31",
    "El formateo debe usar la fecha local.");
assert.strictEqual(core.isoWeek("2021-01-01"), "2020-W53");
assert.strictEqual(core.trendBucketKey("2021-01-01", "week"), "2020-W53");
assert.strictEqual(core.trendBucketKey("2026-08-31", "month"), "2026-08");

const from = { value: "" };
const to = { value: "" };
dashboard.setDefaultDateRange(
    from,
    to,
    new Date(2026, 7, 31, 23, 30));
assert.strictEqual(from.value, "2026-06-03",
    "El rango predeterminado debe incluir exactamente 90 días.");
assert.strictEqual(to.value, "2026-08-31");

const today = new Date(2026, 7, 31, 18, 0);
assert.strictEqual(
    core.daysLateFor({ DueDate: "2026-08-31" }, today),
    0,
    "Un pedido no debe vencer durante su propia fecha de entrega.");
assert.strictEqual(
    core.daysLateFor({ DueDate: "2026-08-30" }, today),
    1);
assert.strictEqual(
    core.daysLateFor({ DueDate: "2026-09-01" }, today),
    -1);
assert.strictEqual(core.bucketFor(1), "Vencido");
assert.strictEqual(core.bucketFor(0), "Vence en 7 días");
assert.strictEqual(core.bucketFor(-8), "Vence en 30 días");
assert.strictEqual(core.bucketFor(null), "Sin fecha");

assert(core.isOpenLine({ OpenQty: 1, LineStatus: "O" }));
assert(!core.isOpenLine({ OpenQty: 0, LineStatus: "O" }));
assert(!core.isOpenLine({ OpenQty: 1, LineStatus: "C" }));
assert(core.isWithinDateRange(
    "2026-08-31", "2026-08-01", "2026-08-31"));
assert(!core.isWithinDateRange(
    "2026-07-31", "2026-08-01", "2026-08-31"));

const rows = [
    {
        OrderDocEntry: 10, OrderNumber: 100, LineNumber: 0,
        OrderDate: "2026-06-01", DueDate: "2026-08-15",
        CustomerName: "Cliente anterior", ItemCode: "PT-1",
        ItemDescription: "Vaso", FamilyName: "PT VASOS",
        OrderedQty: 60, OpenQty: 60, LineStatus: "O",
        StockOnHand: 100
    },
    {
        OrderDocEntry: 20, OrderNumber: 200, LineNumber: 0,
        OrderDate: "2026-08-10", DueDate: "2026-09-05",
        CustomerName: "Cliente visible", ItemCode: "PT-1",
        ItemDescription: "Vaso", FamilyName: "PT VASOS",
        OrderedQty: 50, OpenQty: 50, LineStatus: "O",
        StockOnHand: 100
    },
    {
        OrderDocEntry: 30, OrderNumber: 300, LineNumber: 0,
        OrderDate: "2026-08-12", DueDate: "2026-09-10",
        CustomerName: "Cliente dos", ItemCode: "PT-2",
        ItemDescription: "Domo", FamilyName: "PT DOMOS",
        OrderedQty: 25, OpenQty: 25, LineStatus: "O",
        StockOnHand: 0
    },
    {
        OrderDocEntry: 40, OrderNumber: 400, LineNumber: 0,
        OrderDate: "2026-08-13", DueDate: "2026-09-12",
        CustomerName: "Cliente sin stock conocido", ItemCode: "PT-3",
        ItemDescription: "Tapa", FamilyName: "PT TAPAS",
        OrderedQty: 10, OpenQty: 10, LineStatus: "O",
        StockOnHand: null
    }
];

const allocation = core.computeTentativeAvailability(rows);
assert.strictEqual(
    allocation.get(core.lineKey(rows[0])),
    100,
    "El pedido más antiguo debe reclamar primero el stock.");
assert.strictEqual(
    allocation.get(core.lineKey(rows[1])),
    40,
    "Los filtros posteriores no deben liberar el stock del pedido anterior.");
assert.strictEqual(
    allocation.get(core.lineKey(rows[2])),
    0,
    "Stock conocido en cero debe conservarse como cero.");
assert.strictEqual(
    allocation.get(core.lineKey(rows[3])),
    null,
    "Stock desconocido no debe presentarse como cero confirmado.");

const trendRows = [
    rows[1],
    {
        OrderDocEntry: 21, OrderNumber: 201, LineNumber: 0,
        OrderDate: "2026-08-11", DueDate: "2026-09-05",
        CustomerName: "Cliente cerrado", ItemCode: "PT-1",
        ItemDescription: "Vaso", FamilyName: "PT VASOS",
        OrderedQty: 20, OpenQty: 0, LineStatus: "C",
        StockOnHand: 100
    }
];

const computed = core.computeAll(
    [rows[1]],
    trendRows,
    allocation,
    { trendBucket: "month", today });
assert.strictEqual(computed.kpi.openLines, 1);
assert.strictEqual(computed.kpi.openQty, 50);
assert.strictEqual(computed.backlogTable[0].tentativeAvailable, 40);
assert.strictEqual(computed.itemTable[0].qty, 50);
assert.strictEqual(computed.itemTable[0].orders, 1);
assert.strictEqual(computed.monthly[0].value, 70,
    "La tendencia debe incluir estados abiertos y cerrados del rango.");
assert.strictEqual(computed.monthly[0].orders, 2);

console.log("OK: fechas, vencimientos, rangos, agregados y FIFO de Backlog verificados.");
