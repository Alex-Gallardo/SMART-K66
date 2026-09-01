"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");

const entidad = leer("DiamDev.Give.Entities", "BorradorNcAdjunto.cs");
const encabezado = leer("DiamDev.Give.Entities", "BorradorNcEncabezado.cs");
const bll = leer("DiamDev.Give.BLL", "BorradorNcBLL.cs");
const dal = leer("DiamDev.Give.DAL", "BorradorNcDA.cs");
const controlador = leer("DiamDev.Give.UI", "Controllers", "BorradorNcController.cs");
const modelo = leer("DiamDev.Give.UI", "Models", "BorradorNcViewModel.cs");
const index = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Index.cshtml");
const autorizaciones = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Autorizaciones.cshtml");
const captura = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Index.js");
const adjuntos = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Adjuntos.js");
const css = leer("DiamDev.Give.UI", "Content", "borrador-nc.css");
const config = leer("DiamDev.Give.UI", "Web.config");
const sql = leer("SqlMigrations", "BorradoresNC", "04_crear_adjuntos_seguro.sql");

assert.match(entidad, /class BorradorNcAdjunto/);
assert.match(entidad, /byte\[\] Contenido/);
assert.match(encabezado, /List<BorradorNcAdjunto> Adjuntos/);
assert.match(modelo, /List<BorradorNcEnlaceRequest> Enlaces/);
assert.match(modelo, /List<BorradorNcAdjuntoViewModel> Adjuntos/);

assert.match(bll, /MaximoArchivosAdjuntos = 5/);
assert.match(bll, /MaximoEnlacesAdjuntos = 5/);
assert.match(bll, /MaximoBytesPorArchivo = 10L \* 1024L \* 1024L/);
assert.match(bll, /MaximoBytesAdjuntos = 25L \* 1024L \* 1024L/);
assert.match(bll, /enc\.Adjuntos = enc\.Adjuntos \?\? new List<BorradorNcAdjunto>\(\)/,
    "Un borrador sin adjuntos debe normalizarse a una lista vacía.");
assert.match(bll, /FirmaValida\(extension, contenido\)/);
assert.match(bll, /Uri\.UriSchemeHttp/);
assert.match(bll, /Uri\.UriSchemeHttps/);

assert.match(dal, /BEGIN TRANSACTION|BeginTransaction\(\)/);
assert.match(dal, /INSERT INTO dbo\.BORR_NC_ADJUNTO/);
assert.match(dal, /foreach \(var adjunto in enc\.Adjuntos \?\? new List<BorradorNcAdjunto>\(\)\)/);
assert.match(dal, /public BorradorNcAdjunto ObtenerAdjunto\([\s\S]*?SELECT ADJUNTO_ID[\s\S]*?CONTENIDO/);
assert.match(dal, /LeerAdjunto\(r, false\)/,
    "El detalle solo debe recuperar metadatos, nunca los blobs.");

assert.match(controlador, /IEnumerable<HttpPostedFileBase> archivos/);
assert.match(controlador, /new MemoryStream\(\)/);
assert.match(controlador, /public ActionResult DescargarAdjunto/);
assert.match(controlador, /if \(!PuedeImprimir\(enc\)\) return new HttpUnauthorizedResult\(\)/);
assert.match(controlador, /X-Content-Type-Options/);
assert.match(controlador, /Adjuntos = \(x\.Adjuntos/);

assert.match(index, /Documentación de respaldo/);
assert.match(index, /id="bncArchivos" multiple/);
assert.match(index, /Puede guardar el borrador sin documentación/);
assert.match(index, /BorradorNc-Adjuntos\.js/);
assert.match(autorizaciones, /data-url-adjunto/);
assert.match(autorizaciones, /BorradorNc-Adjuntos\.js/);

assert.match(captura, /new window\.FormData\(\)/);
assert.match(captura, /formulario\.append\("archivos", archivo, archivo\.name\)/);
assert.match(captura, /state\.archivos = \[\]/);
assert.match(captura, /state\.enlaces = \[\]/);
assert.match(captura, /window\.BorradorNcAdjuntos\.plantilla\(x/);
assert.match(adjuntos, /function plantilla\(documento, opciones\)/);
assert.match(adjuntos, /Sin documentación adjunta/);
assert.match(adjuntos, /rel="noopener noreferrer"/);
assert.match(adjuntos, /bnc-attachment-preview-frame/);
assert.match(css, /\.bnc-attachment-capture-grid/);
assert.match(css, /@media \(max-width: 767px\)[\s\S]*?\.bnc-attachment-capture-grid \{ grid-template-columns: 1fr; \}/);

assert.match(config, /<location path="BorradorNc\/Guardar">/);
assert.match(config, /maxRequestLength="27648"/);
assert.match(config, /maxAllowedContentLength="28311552"/);

assert.match(sql, /USE \[POS-SmartK66\]/);
assert.match(sql, /IF DB_NAME\(\) <> N'POS-SmartK66'/);
assert.match(sql, /CREATE TABLE dbo\.BORR_NC_ADJUNTO/);
assert.match(sql, /FOREIGN KEY \(ID_EMPRESA, ID_BORRADOR\)/);
assert.match(sql, /ON DELETE CASCADE/);
assert.match(sql, /TIPO IN \('ARCHIVO', 'ENLACE'\)/);
assert.match(sql, /TAMANO BETWEEN 1 AND 10485760/);

console.log("OK: adjuntos opcionales de BorradorNc validados en captura, persistencia, seguridad y vistas.");
