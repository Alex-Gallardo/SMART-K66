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

assert.strictEqual(core.normalizeTurno("1"), "Dia");
assert.strictEqual(core.normalizeTurno("Día"), "Dia");
assert.strictEqual(core.normalizeTurno("2"), "Noche");
assert.strictEqual(core.normalizeTurno("sin asignar"), "");
assert.strictEqual(core.TURNO_HORAS, 12,
    "La capacidad nominal debe ser de 12 horas por turno.");

assert.deepStrictEqual(
    core.enumerarDias("2024-02-28", "2024-03-01"),
    ["2024-02-28", "2024-02-29", "2024-03-01"],
    "La tendencia debe incluir todos los días calendario del rango.");
assert.deepStrictEqual(core.enumerarDias("2026-03-02", "2026-03-01"), [],
    "Un rango invertido no debe crear días.");

const shiftRows = [
    {
        Fecha: "2026-09-01", OT: 20, PosicionOT: 1, ItemID: "A",
        CodigoRecurso: "R1", DescripcionRecurso: "Formadora",
        Turno: "Dia", "Hora Real Día": 8, "Hora Plan": 20,
        "Cantidad Real Día": 100, "Cantidad Real Turno": 60
    },
    {
        Fecha: "2026-09-01", OT: 20, PosicionOT: 1, ItemID: "A",
        CodigoRecurso: "R1", DescripcionRecurso: "Formadora",
        Turno: "Noche", "Hora Real Día": 4, "Hora Plan": 20,
        "Cantidad Real Día": 100, "Cantidad Real Turno": 40
    },
    {
        Fecha: "2026-09-02", OT: 20, PosicionOT: 1, ItemID: "B",
        CodigoRecurso: "R1", DescripcionRecurso: "Formadora",
        Turno: "Dia", "Hora Real Día": 12, "Hora Plan": 20,
        "Cantidad Real Día": 60
    },
    {
        Fecha: "2026-09-02", OT: 20, PosicionOT: 1, ItemID: "C",
        CodigoRecurso: "R1", DescripcionRecurso: "Formadora",
        Turno: "Noche", "Hora Real Día": 14, "Hora Plan": 20,
        "Cantidad Real Día": 40
    },
    {
        Fecha: "2026-09-01", OT: 21, PosicionOT: 1, ItemID: "A",
        CodigoRecurso: "R2", DescripcionRecurso: "Formadora",
        Turno: "Dia", "Hora Real Día": 9, "Hora Plan": 6,
        "Cantidad Real Día": 25
    }
];

const shiftedQuantity = core.aggregateDailyQuantity(
    shiftRows.slice(0, 2),
    () => "total",
    false);
assert.strictEqual(shiftedQuantity.total, 100,
    "La cantidad diaria repetida en Día y Noche debe contarse una sola vez.");

const turnoHours = core.computeHorasPorTurno(shiftRows);
assert.strictEqual(turnoHours.horasPorTurno["2026-09-01|Dia|R1"], 8);
assert.strictEqual(turnoHours.horasPorTurno["2026-09-01|Noche|R1"], 4);

assert.deepStrictEqual(
    core.desglosarDiaRecurso(
        "2026-09-01", "R1",
        turnoHours.horasPorTurno,
        turnoHours.turnosConFila),
    { real: 12, paro: 12, disponible: 0 },
    "Dos turnos confirmados deben separar horas reales y faltante como paro.");
assert.deepStrictEqual(
    core.desglosarDiaRecurso(
        "2026-09-01", "R2",
        turnoHours.horasPorTurno,
        turnoHours.turnosConFila),
    { real: 9, paro: 3, disponible: 12 },
    "Un turno ausente debe quedar disponible, no sumarse como paro.");
assert.deepStrictEqual(
    core.desglosarDiaRecurso(
        "2026-09-03", "R1",
        turnoHours.horasPorTurno,
        turnoHours.turnosConFila),
    { real: 0, paro: 0, disponible: 24 },
    "Un día sin confirmaciones debe quedar completamente disponible.");
assert.deepStrictEqual(
    core.desglosarDiaRecurso(
        "2026-09-02", "R1",
        turnoHours.horasPorTurno,
        turnoHours.turnosConFila),
    { real: 26, paro: 0, disponible: 0 },
    "Una anomalía superior a 12 horas no debe producir paro negativo.");

assert.deepStrictEqual(core.apportionPlanPorDia(shiftRows), {
    "2026-09-01": 16,
    "2026-09-02": 10
}, "El plan debe prorratearse por combinación y día, sin duplicarse por turno.");

const cambios = core.computeCambiosDeMolde(shiftRows);
assert.strictEqual(cambios.total, 2,
    "A→B y B→C deben contarse como dos cambios de molde.");
assert.strictEqual(cambios.recursosConCambio.size, 1,
    "Recursos con la misma descripción no deben mezclarse si el código cambia.");
assert.strictEqual(cambios.porRecursoCount["R1 — Formadora"], 2);
assert.strictEqual(cambios.porRecursoCount["R2 — Formadora"], 0);
assert.strictEqual(cambios.porDiaRecurso["2026-09-02|R1"], 2);

assert.strictEqual(core.plantaFromRecurso({ CodigoRecurso: "pcmq0068" }), "PC",
    "La planta debe derivarse del código de recurso sin distinguir mayúsculas.");
assert.strictEqual(core.plantaFromRecurso({ DescripcionRecurso: "PP EXTRUSORA" }), "PP",
    "La descripción debe funcionar como respaldo cuando no hay código.");
assert.strictEqual(core.plantaFromRecurso({ CodigoRecurso: "R1", DescripcionRecurso: "PC ARMADO" }), null,
    "Un código presente y desconocido no debe reclasificarse usando la descripción.");
assert.strictEqual(core.plantaFromRecurso({}), null,
    "No se debe inventar una planta para recursos desconocidos.");

const motiveRows = [
    { "Motivo de Paro": "Mantenimiento" },
    { "Motivo de Paro": " mantenimiento " },
    { "Motivo de Paro": "" },
    { "Motivo de Paro": null }
];
for (let i = 0; i < 13; i++) motiveRows.push({ "Motivo de Paro": `Motivo ${i}` });
const motiveDistribution = core.computeMotivoParoDist(motiveRows);
assert.strictEqual(motiveDistribution.top[0].label, "Mantenimiento");
assert.strictEqual(motiveDistribution.top[0].count, 2,
    "Los motivos deben agruparse sin distinguir mayúsculas y conservando una etiqueta legible.");
assert.strictEqual(motiveDistribution.top.length, 12,
    "La gráfica debe limitar los motivos individuales para seguir siendo legible.");
assert.strictEqual(motiveDistribution.otrosCount, 2,
    "Los motivos fuera del top 12 deben agruparse en Otros.");
assert.strictEqual(motiveDistribution.sinRegistrar, 2,
    "Los valores vacíos deben contarse por separado.");

const oneDayKpis = core.computeKpisData(
    shiftRows.slice(0, 2),
    "2026-09-01",
    "2026-09-01",
    core.computeCambiosDeMolde(shiftRows.slice(0, 2)));
assert.strictEqual(oneDayKpis.sumReal, 12);
assert.strictEqual(oneDayKpis.sumPlan, 20,
    "El plan repetido por turno debe contarse una vez por OT/posición/recurso.");
assert.strictEqual(oneDayKpis.sumCant, 100,
    "La comparación debe usar la cantidad diaria desduplicada, no Cantidad Hecha repetida.");
assert.strictEqual(oneDayKpis.sumParo, 12);
assert.strictEqual(oneDayKpis.sumDisponible, 0);
assert.strictEqual(oneDayKpis.capacidadTotal, 24);

const trend = core.computeTrendSeries(
    shiftRows,
    "2026-09-01",
    "2026-09-02",
    "day",
    cambios);
assert.deepStrictEqual(trend.real, [21, 26]);
assert.deepStrictEqual(trend.plan, [16, 10]);
assert.deepStrictEqual(trend.paro, [15, 0]);
assert.deepStrictEqual(trend.disponible, [12, 24]);
assert.deepStrictEqual(trend.capacidad, [48, 48]);
assert.deepStrictEqual(trend.keys, ["2026-09-01", "2026-09-02"]);

const monthlyTrend = core.computeTrendSeries(
    shiftRows,
    "2026-09-01",
    "2026-09-02",
    "month",
    cambios);
assert.deepStrictEqual(monthlyTrend.keys, ["2026-09"]);
assert.deepStrictEqual(monthlyTrend.real, [47]);
assert.deepStrictEqual(monthlyTrend.capacidad, [96]);

assert.strictEqual(
    core.validateCompareRanges(
        "2026-09-01", "2026-09-02", "2026-09-03", "2026-09-04",
        "2026-09-01", "2026-09-30"),
    "");
assert(core.validateCompareRanges(
    "2026-09-02", "2026-09-01", "2026-09-03", "2026-09-04",
    "2026-09-01", "2026-09-30").includes("posterior"),
    "Los rangos invertidos deben rechazarse.");
assert(core.validateCompareRanges(
    "2026-08-31", "2026-09-01", "2026-09-03", "2026-09-04",
    "2026-09-01", "2026-09-30").includes("dentro"),
    "No se debe comparar silenciosamente fuera de los datos cargados.");

assert.strictEqual(
    core.filterRowsByRange(shiftRows, "2026-09-02", "2026-09-02").length,
    2,
    "El filtro comparativo por fecha debe incluir ambos extremos.");
assert.strictEqual(
    core.filterRowsByOtPosition(shiftRows, { ot: "20", pos: "1" }).length,
    4,
    "La selección de OT + posición debe comparar como texto para tolerar tipos del JSON.");

const exported = core.buildExportRows([
    {
        Fecha: "14/09/2026",
        OT: 1,
        PosicionOT: 2,
        DescripcionItem: "=CMD()",
        Turno: "1",
        "Cantidad Real Turno": 108,
        "Pieza*turnoPlan": 12.5,
        "Pieza*turnoReal": 9.82,
        "Eficiencia Dia": 80
    },
    {
        Fecha: "14/09/2026",
        OT: 1,
        PosicionOT: 2,
        Turno: "2",
        "Cantidad Real Turno": null
    }
]);
assert.strictEqual(Object.keys(exported[0]).length, 25,
    "Excel debe conservar las 25 columnas del detalle.");
assert.strictEqual(exported[0].Fecha, "2026-09-14");
assert.strictEqual(exported[0].Turno, "Dia");
assert.strictEqual(exported[0]["Cant.Real Turno"], 108);
assert.strictEqual(exported[0]["P/Hr Plan"], 12.5);
assert.strictEqual(exported[0]["P/Hr Real"], 9.82);
assert.strictEqual(exported[1]["Cant.Real Turno"], "",
    "Un turno sin confirmación debe exportarse vacío, no como cero.");
assert.strictEqual(exported[0]["Descripción Item"], "'=CMD()",
    "El texto exportado no debe poder interpretarse como fórmula de Excel.");

console.log("OK: dashboard, comparación, plantas, motivos y exportación de Producción v2 verificados.");
