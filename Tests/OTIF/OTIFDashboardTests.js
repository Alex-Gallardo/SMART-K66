"use strict";

const assert = require("assert");
const path = require("path");
const dashboard = require(path.resolve(__dirname, "../../DiamDev.Give.UI/Scripts/App/otif-viz.js"));
const core = dashboard.__test;
global.document = { getElementById: () => null };

const event = (order, line, item, promised, deliveryId, deliveredAt, qty, ordered = 10) => ({
    OrderDocEntry: order, OrderNumber: 100, CustomerCode: "C1", CustomerName: "Cliente",
    LineNumber: line, ItemCode: item, ItemDescription: item, DueDate: promised,
    LineShipDate: null, OrderedQty: ordered, OpenQty: 0, LineStatus: "C",
    DeliveryDocEntry: deliveryId, DeliveryLineNumber: deliveryId == null ? null : 0,
    DeliveryDate: deliveredAt, DeliveryQty: qty
});

const source = [
    event(1, 0, "A", "2026-08-15", 10, "2026-08-14", 4),
    event(1, 0, "A", "2026-08-15", 11, "2026-08-20", 6),
    event(1, 1, "B", "2026-09-05", 12, "2026-09-04", 10),
    event(2, 0, "A", "2026-09-05", null, null, null),
    event(3, 0, "C", "2026-09-05", 30, "2026-09-04", 10)
];
const normalized = core.normalizeDataRows(source);
assert.deepStrictEqual(normalized.missing, []);
assert.strictEqual(normalized.rows.length, 4, "Las entregas no deben duplicar líneas.");
assert.strictEqual(normalized.rows[0].DeliveredQty, 10);
assert.strictEqual(normalized.rows[0].FirstDeliveryDate, "2026-08-14");
assert.strictEqual(normalized.rows[0].LastDeliveryDate, "2026-08-20");
assert.strictEqual(core.normalizeDataRows([source[0], source[0]]).rows[0].DeliveredQty, 4,
    "Un evento duplicado no debe sumar cantidad dos veces.");
const incompatible = { ...source[0] };
delete incompatible.DeliveryDate;
assert(core.normalizeDataRows([incompatible]).missing.includes("DeliveryDate"),
    "La consulta incompatible debe fallar de forma explícita.");

const filters = { mode: "line", fillThreshold: 1, toleranceDays: 0,
    q: "", familia: "", origen: "", agente: "", itemCode: "", customerCode: "" };
const row = core.computeRow(normalized.rows[0], filters, "2026-09-30");
assert.strictEqual(row.onTime, true, "La primera entrega sí fue puntual.");
assert.strictEqual(row.complete, true, "La cantidad finalmente se completó.");
assert.strictEqual(row.otif, false, "La cantidad completa llegó fuera de plazo.");
assert.strictEqual(core.computeRow(normalized.rows[0], { ...filters, toleranceDays: 5 }, "2026-09-30").otif, true);
assert.strictEqual(core.computeRow(normalized.rows[0], { ...filters, fillThreshold: 0.4 }, "2026-09-30").otif, true);
assert.strictEqual(core.computeRow(normalized.rows[2], filters, "2026-09-30").closedNoDelivery, true);

const september = { from: "2026-09-01", to: "2026-09-30" };
const lineSelection = core.selectRowsForMode(normalized.rows, filters, september);
assert.strictEqual(lineSelection.rows.length, 3, "El modo línea limita las fechas prometidas.");
const orderSelection = core.selectRowsForMode(normalized.rows, { ...filters, mode: "order" }, september);
assert.strictEqual(orderSelection.rows.length, 4,
    "El modo pedido incluye todas las líneas del pedido cuya última promesa cae en el período.");
const computed = orderSelection.rows.map(r => core.computeRow(r, filters, "2026-09-30"));
const orders = core.aggregateToOrders(computed);
assert.strictEqual(orders.length, 3);
assert.strictEqual(orders.find(o => o.OrderDocEntry === "1").otif, false,
    "Una línea tardía hace fallar el pedido completo.");
assert.strictEqual(orders.find(o => o.OrderDocEntry === "1").promised, "2026-09-05");
const itemSelection = core.selectRowsForMode(normalized.rows,
    { ...filters, mode: "order", itemCode: "b" }, september);
assert.strictEqual(itemSelection.rows.length, 2,
    "El filtro de item elige pedidos, pero conserva todas sus líneas para puntuarlos.");
assert.strictEqual(itemSelection.itemRows.length, 1,
    "El resumen por item solo usa las líneas que coinciden con el filtro.");
assert.strictEqual(core.computeAll(itemSelection.rows,
    { ...filters, mode: "order", itemCode: "b" }, itemSelection.itemRows, "2026-09-30").itemTable.length, 1);

const repeatedNumber = core.aggregateToOrders([
    core.computeRow(normalized.rows[0], filters, "2026-09-30"),
    core.computeRow(normalized.rows[3], filters, "2026-09-30")
]);
assert.strictEqual(repeatedNumber.length, 2, "DocNum repetido no debe fusionar pedidos distintos.");

console.log("OTIFDashboardTests: OK");
