"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");

const entidad = leer("DiamDev.Give.Entities", "DocumentoPrevioSap.cs");
const repositorio = leer("DiamDev.Give.DAL", "HanaRepository.cs");
const bll = leer("DiamDev.Give.BLL", "BorradorNcBLL.cs");
const controlador = leer("DiamDev.Give.UI", "Controllers", "BorradorNcController.cs");
const modelo = leer("DiamDev.Give.UI", "Models", "BorradorNcViewModel.cs");
const index = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Index.cshtml");
const autorizaciones = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Autorizaciones.cshtml");
const detalle = leer("DiamDev.Give.UI", "Views", "BorradorNc", "DetalleDocumentoPrevio.cshtml");
const componente = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-DocumentosPrevios.js");
const seguimientoJs = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Index.js");
const autorizacionesJs = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Autorizaciones.js");
const estilos = leer("DiamDev.Give.UI", "Content", "borrador-nc.css");

// El dominio representa explícitamente ambas clases y sus renglones.
assert.match(entidad, /NOTA_CREDITO \/ DEVOLUCION/);
assert.match(entidad, /List<DocumentoPrevioDetalleSap> Lineas/);
assert.match(entidad, /TipoBase/);
assert.match(entidad, /ReferenciaBase/);

// SAP se consulta por las tablas estándar verificadas en el diagnóstico.
for (const tabla of ["ORIN", "RIN1", "ORDN", "RDN1"]) {
    assert.ok(repositorio.includes(tabla), `Falta consultar ${tabla}`);
}
assert.match(repositorio, /public List<DocumentoPrevioSap> ObtenerDocumentosPrevios/);
assert.match(repositorio, /public DocumentoPrevioSap ObtenerDetalleDocumentoPrevio/);
assert.match(repositorio, /GroupBy\(x => string\.Join\("\|", x\.Clase, x\.Factura,[\s\S]*?x\.Documento, x\.DocEntry\)/);
assert.match(repositorio, /Distinct\(StringComparer\.OrdinalIgnoreCase\)/);
assert.match(repositorio, /H\.""DocEntry"" = \?/);
assert.match(repositorio, /H\.""CardCode"" = \?/);
assert.match(repositorio, /Tipo"", ''\)\)\) LIKE 'NC%'/);
assert.match(repositorio, /Tipo"", ''\)\)\) NOT LIKE 'NC%'/);

// El cálculo financiero legado permanece separado y sin sustituciones.
assert.match(bll, /var ncPrevias = _hana\.ObtenerNotasCreditoPrevias\(empresa, docs\)/);
assert.match(bll, /f\.NcPreviaSap = notas\.Sum\(n => n\.Total\)/);
assert.match(bll, /ObtenerDocumentosPrevios/);

// Los endpoints recargan el borrador y aplican permisos/visibilidad antes de SAP.
assert.match(controlador, /public JsonResult ObtenerDocumentosPrevios\(/);
assert.match(controlador, /public JsonResult ObtenerDocumentosPreviosAutorizacion\(/);
assert.match(controlador, /public ActionResult DetalleDocumentoPrevio\(/);
assert.match(controlador, /PuedeConsultarFacturaBorrador\(enc\)/);
assert.match(controlador, /BuscarDocumentoPrevio\(\s*enc, factura, documento, clase\)/);
assert.match(controlador, /BuscarDetalleFactura\(enc, factura\)/);
assert.match(controlador, /enc\.IdEmpresa, previo\.Clase, previo\.DocEntry, enc\.IdCliente/);
assert.match(controlador, /public ActionResult AbrirDocumentoPrevioSap\(/);

// Seguimiento y Autorizaciones usan el mismo componente y una sola consulta por borrador.
assert.match(index, /data-url-documentos-previos="@Url\.Action\("ObtenerDocumentosPrevios"/);
assert.match(autorizaciones, /data-url-documentos-previos="@Url\.Action\("ObtenerDocumentosPreviosAutorizacion"/);
assert.match(index, /BorradorNc-DocumentosPrevios\.js/);
assert.match(autorizaciones, /BorradorNc-DocumentosPrevios\.js/);
assert.match(seguimientoJs, /documentosPrevios\.cargar\(r\.data\)/);
assert.match(autorizacionesJs, /documentosPrevios\.cargar\(r\.data\)/);
assert.doesNotMatch(autorizacionesJs, /cargarNotas\(/);

// Cada antecedente tiene una acción evidente y abre la vista segura en otra pestaña.
assert.match(componente, /Notas de crédito y devoluciones previas en SAP/);
assert.match(componente, /<span>Ver detalle<\/span>/);
assert.match(componente, /target="_blank" rel="noopener noreferrer"/);
assert.match(componente, /factura=" \+ encodeURIComponent\(previo\.Factura/);
assert.match(componente, /clase=" \+ encodeURIComponent\(previo\.Clase/);

// La vista completa presenta encabezado y líneas en una tabla HTML desplazable.
assert.match(modelo, /class BorradorNcDocumentoPrevioViewModel/);
assert.match(detalle, /<table class="bnc-sap-lines-table">/);
assert.match(detalle, /<th scope="col">Producto \/ servicio<\/th>/);
assert.match(detalle, /foreach \(var producto in Model\.Productos\)/);
assert.match(detalle, /AbrirDocumentoPrevioSap/);
assert.match(estilos, /\.bnc-prior-item\s*\{[\s\S]*?grid-template-columns:/);
assert.match(estilos, /@media \(max-width: 430px\)[\s\S]*?\.bnc-prior-item/);
assert.match(estilos, /\.bnc-sap-table-region\s*\{[\s\S]*?overflow-x: auto/);

console.log("OK: notas de crédito y devoluciones previas tienen detalle seguro, deduplicado y responsive.");
