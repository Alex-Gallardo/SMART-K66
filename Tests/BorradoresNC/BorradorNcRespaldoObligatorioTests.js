"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");

const conceptos = leer("DiamDev.Give.Entities", "ConceptosBorradorNc.cs");
const bll = leer("DiamDev.Give.BLL", "BorradorNcBLL.cs");
const captura = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Index.js");
const index = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Index.cshtml");
const estructura = leer("SqlMigrations", "BorradoresNC", "01_crear_estructura_segura.sql");
const migracion = leer("SqlMigrations", "BorradoresNC", "07_habilitar_descuento_autorizado_seguro.sql");

assert.match(conceptos, /DescuentoAutorizado = "DESCUENTO AUTORIZADO"/);
assert.match(conceptos, /\{ Devolucion, Descuento, DescuentoAutorizado, Otros \}/);
assert.match(conceptos, /bool EsDescuentoAutorizado\(string concepto\)/);

assert.match(bll, /RequiereDocumentoRespaldo\(enc\.Detalles\)/);
assert.match(bll, /!ConceptosBorradorNc\.EsDescuentoAutorizado\(d\.Concepto\)/);
assert.match(bll, /!enc\.Adjuntos\.Any\(a => a != null && a\.EsArchivo\)/,
    "Solamente un archivo real debe satisfacer el requisito; un enlace no es suficiente.");
assert.match(bll, /Debe adjuntar al menos un documento de respaldo para los conceptos seleccionados/);

assert.match(captura, /conceptoDescuentoAutorizado = "DESCUENTO AUTORIZADO"/);
assert.match(captura, /function requiereDocumentoRespaldo\(\)/);
assert.match(captura, /requiereDocumentoRespaldo\(\) && !state\.archivos\.length/);
assert.match(captura, /Documento requerido/);
assert.match(captura, /Documento opcional/);
assert.match(index, /id="bncRespaldoCard"/);
assert.match(index, /aria-describedby="bncRespaldoAyuda"/);

for (const sql of [estructura, migracion]) {
    assert.match(sql, /N'DESCUENTO AUTORIZADO'/);
    assert.match(sql, /CK_BND_CONCEPTO/);
}
assert.match(migracion, /BEGIN TRANSACTION/);
assert.match(migracion, /WITH CHECK[\s\S]*ADD CONSTRAINT CK_BND_CONCEPTO/);
assert.match(migracion, /is_not_trusted=0/);

console.log("OK: respaldo obligatorio y excepción Descuento autorizado validados en UI, servidor y SQL.");
