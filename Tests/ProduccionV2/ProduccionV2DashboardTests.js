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
    "produccion-v2-shared.js"));

const core = dashboard.__test;

assert(core, "La lógica verificable del dashboard debe estar exportada.");

assert.strictEqual(
    core.normalizeFechaToISO("05/01/2026"),
    "2026-01-05",
    "DD/MM/YYYY debe normalizarse sin invertir día y mes.");
assert.strictEqual(
    core.normalizeFechaToISO("2026-08-31T00:00:00"),
    "2026-08-31",
    "Una fecha ISO debe conservar el día.");
assert.strictEqual(
    core.isoWeek("2021-01-01"),
    "2020-W53",
    "La semana ISO debe usar el año de semana correcto.");
assert.strictEqual(core.bucketKey("2026-08-31", "month"), "2026-08");
assert.strictEqual(core.bucketHours("2024", "year"), 8784);
assert.strictEqual(core.bucketHours("2025-02", "month"), 672);

const from = { value: "" };
const to = { value: "" };
dashboard.applyDatePreset("30d", from, to, new Date(2026, 7, 31, 21, 30));
assert.strictEqual(from.value, "2026-08-02", "30 días debe ser inclusivo.");
assert.strictEqual(to.value, "2026-08-31", "El preset debe usar la fecha local.");

const rows = [
    {
        Fecha: "2026-08-01", OT: 10, PosicionOT: 1, ItemID: "PT-1",
        CodigoRecurso: "R1", DescripcionRecurso: "Formadora",
        FamilyName: "PT VASOS", TipoItem: "Producto Terminado (PT)",
        "Cantidad Real Día": 100
    },
    {
        Fecha: "2026-08-01", OT: 10, PosicionOT: 1, ItemID: "PT-1",
        CodigoRecurso: "R2", DescripcionRecurso: "Empacadora",
        FamilyName: "PT VASOS", TipoItem: "Producto Terminado (PT)",
        "Cantidad Real Día": 100
    },
    {
        Fecha: "2026-08-02", OT: 10, PosicionOT: 1, ItemID: "PT-1",
        CodigoRecurso: "R1", DescripcionRecurso: "Formadora",
        FamilyName: "PT VASOS", TipoItem: "Producto Terminado (PT)",
        "Cantidad Real Día": 50
    }
];

const physicalTotal = core.aggregateDailyQuantity(rows, () => "total", false);
assert.strictEqual(
    physicalTotal.total,
    150,
    "La producción física no debe duplicarse por cada recurso de la OT.");

const perResource = core.aggregateDailyQuantity(
    rows,
    core.resourceLabel,
    true);
assert.deepStrictEqual(perResource, {
    "R1 — Formadora": 150,
    "R2 — Empacadora": 100
}, "El throughput debe desduplicarse dentro de cada recurso.");

const perFamily = core.aggregateDailyQuantity(
    rows,
    row => row.FamilyName,
    false);
assert.strictEqual(
    perFamily["PT VASOS"],
    150,
    "El desglose por familia debe reconciliar con el total físico.");

assert.strictEqual(
    core.resourceValue(rows[0]),
    "R1",
    "Los filtros deben identificar el recurso por código.");
assert.strictEqual(
    core.resourceLabel(rows[0]),
    "R1 — Formadora",
    "La UI debe conservar código y descripción del recurso.");

console.log("OK: fechas, presets, recursos y cantidades de Producción v2 verificados.");
