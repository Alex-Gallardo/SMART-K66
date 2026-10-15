"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const sql = fs.readFileSync(path.join(__dirname, "..", "..", "SqlMigrations",
    "Cotizaciones", "07_corregir_20_cotizaciones_historicas.sql"), "utf8");
const inicio = sql.indexOf("INSERT INTO @Objetivos");
const fin = sql.indexOf("IF (SELECT COUNT(*) FROM @Objetivos)", inicio);
assert(inicio >= 0 && fin > inicio);
const objetivos = [...sql.slice(inicio, fin).matchAll(
    /\(N'(GRACO|FAES)',\s*N'(COT-(?:GR|FA)-\d{8})'\)/g
)];
assert.strictEqual(objetivos.length, 20, "El lote debe tener exactamente 20 IDs.");
assert.strictEqual(new Set(objetivos.map(x => `${x[1]}/${x[2]}`)).size, 20,
    "No se deben repetir documentos.");
assert(!objetivos.some(x => x[2] === "COT-BO-00000001"),
    "El documento BOLIK tiene su propia corrección.");
assert(sql.includes("DECLARE @Aplicar bit = 0"));
assert(sql.includes("ACEPTO_REEMPLAZAR_20_TOTALES"));
assert(sql.includes("IF EXISTS (SELECT 1 FROM #Resumen WHERE VALIDACION <> N'LISTO')"));
assert(sql.includes("COT_RESPALDO_20261015_ENC") &&
       sql.includes("COT_RESPALDO_20261015_DET"));
assert(sql.includes("ROLLBACK TRANSACTION") && sql.includes("COMMIT TRANSACTION"));
assert(sql.includes("PRECIO_VISIBLE") && sql.includes("NUEVO_DESCUENTO") &&
       sql.includes("IMPUESTO_MONTO = 0"));

console.log("OK: lote SQL de 20 documentos, simulación, validación y respaldos.");
