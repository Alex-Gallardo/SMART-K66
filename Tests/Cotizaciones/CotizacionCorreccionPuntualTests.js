"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const sql = fs.readFileSync(path.join(__dirname, "..", "..", "SqlMigrations",
    "Cotizaciones", "06_corregir_COT_BO_00000001.sql"), "utf8");

assert(sql.includes("DECLARE @Aplicar bit = 0"),
    "La corrección debe iniciar siempre como simulación.");
assert(sql.includes("DB_NAME() <> @BaseEsperada") &&
       sql.includes("DECLARE @BaseEsperada sysname"),
    "La base de destino debe verificarse de forma explícita.");
assert(sql.includes("ACEPTO_TOTAL_1889_28") &&
       sql.includes("@Confirmacion"),
    "La aplicación debe requerir confirmación expresa.");
assert(sql.includes("COT_RESPALDO_BO_00000001_ENC") &&
       sql.includes("COT_RESPALDO_BO_00000001_DET"),
    "Deben guardarse snapshots completos antes de cambiar importes.");
assert(sql.includes("ITEM_CODE = N'PT00362'") &&
       sql.includes("CANTIDAD = 144") &&
       sql.includes("ROUND(PRECIO_UNITARIO, 2) = 13.12"),
    "La corrección debe restringirse al producto y precio del PDF.");
assert(sql.includes("@NuevoTotal <> 1889.28") &&
       sql.includes("IMPUESTO_MONTO = 0") &&
       sql.includes("IMPUESTO_TOTAL = 0"),
    "El total debe ser 1889.28 sin IVA monetario adicional.");
assert(sql.includes("YA_CORREGIDO") &&
       sql.includes("ROLLBACK TRANSACTION") &&
       sql.includes("COMMIT TRANSACTION"),
    "Reejecutar no debe volver a modificar un documento corregido.");
assert(!/\b(?:DELETE|TRUNCATE|DROP)\b/i.test(sql),
    "El script no debe borrar registros ni estructuras.");

console.log("OK: corrección puntual segura, simulable y verificable.");
