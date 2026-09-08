"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");

const controlador = leer("DiamDev.Give.UI", "Controllers", "BorradorNcController.cs");
const modelo = leer("DiamDev.Give.UI", "Models", "BorradorNcViewModel.cs");
const index = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Index.cshtml");
const autorizaciones = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Autorizaciones.cshtml");
const detalle = leer("DiamDev.Give.UI", "Views", "BorradorNc", "DetalleFactura.cshtml");
const seguimientoJs = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Index.js");
const autorizacionesJs = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Autorizaciones.js");
const facturasJs = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-FacturasDetalle.js");
const estilos = leer("DiamDev.Give.UI", "Content", "borrador-nc.css");

// Ambas pantallas publican la misma ruta de consulta desde un borrador persistido.
assert.match(index, /data-url-factura-borrador="@Url\.Action\("DetalleFacturaBorrador"/);
assert.match(autorizaciones, /data-url-factura-borrador="@Url\.Action\("DetalleFacturaBorrador"/);
assert.match(seguimientoJs, /facturaBorrador: \$root\.data\("url-factura-borrador"\)/);
assert.match(autorizacionesJs, /facturaBorrador: \$root\.data\("url-factura-borrador"\)/);

// La URL se arma con identidad del borrador y documento; no con datos comerciales del navegador.
assert.match(facturasJs, /function urlFacturaBorrador\(baseUrl, borrador, factura, origen\)/);
assert.match(facturasJs, /empresa=" \+ encodeURIComponent\(borrador\.IdEmpresa/);
assert.match(facturasJs, /&idBorrador=" \+ encodeURIComponent\(borrador\.IdBorrador/);
assert.match(facturasJs, /&documento=" \+ encodeURIComponent\(factura\.Documento/);

// El acceso principal ocupa una columna independiente y es la primera de la tabla.
for (const script of [seguimientoJs, autorizacionesJs]) {
    assert.match(script, /<thead><tr><th class="text-center">Acción<\/th><th>Documento<\/th>/);
    assert.match(script, /<td class="bnc-linked-invoice-action-cell"><a class="bnc-btn bnc-btn-primary bnc-linked-invoice-action"/);
    assert.match(script, /bnc-linked-invoice-action/);
    assert.match(script, /<span>Ver factura<\/span>/);
    assert.match(script, /target="_blank" rel="noopener noreferrer"/);
}
assert.match(seguimientoJs, /urls\.facturaBorrador, x, d, "seguimiento"/);
assert.match(autorizacionesJs, /urls\.facturaBorrador, x, d, "autorizaciones"/);
assert.doesNotMatch(seguimientoJs, /<th class="text-center">Acciones<\/th>/);
assert.doesNotMatch(autorizacionesJs, /<th class="text-center">Acciones<\/th>/);

// Las tablas resumen respetan el ancho del panel y no fuerzan el ancho global.
assert.match(seguimientoJs, /bnc-linked-invoice-table/);
assert.match(autorizacionesJs, /bnc-linked-invoice-table is-authorization/);
assert.match(estilos, /\.bnc-linked-invoice-table\s*\{[\s\S]*?min-width: 0 !important;[\s\S]*?table-layout: fixed;/);
assert.match(estilos, /\.bnc-linked-invoice-table-wrap\s*\{[\s\S]*?overflow-x: hidden;/);
assert.match(estilos, /\.bnc-linked-invoice-action\s*\{/);
assert.match(estilos, /\.bnc-linked-invoice-action-cell\s*\{/);
assert.match(estilos, /\.bnc-linked-invoice-action\s*\{[\s\S]*?width: 100%;/);
assert.match(estilos, /\.bnc-detail-layout > \*,[\s\S]*?\.bnc-auth-layout > \*[\s\S]*?min-width: 0;/);

// El servidor obtiene nuevamente el borrador, comprueba acceso y confirma pertenencia.
assert.match(controlador, /public ActionResult DetalleFacturaBorrador/);
assert.match(controlador, /_bll\.ObtenerPorId\(empresa, idBorrador\)/);
assert.match(controlador, /PuedeConsultarFacturaBorrador\(enc\)/);
assert.match(controlador, /BuscarDetalleFactura\(enc, documento\)/);
assert.match(controlador, /return View\("DetalleFactura", modelo\)/);
assert.match(controlador, /TienePermiso\(PERMISO_VER\) && PuedeConsultarSeguimiento\(enc\)/);
assert.match(controlador, /TienePermiso\(PERMISO_AUTORIZAR\)/);
assert.match(controlador, /EstadosBorradorNc\.Pendiente/);
assert.match(controlador, /La factura no pertenece al borrador indicado/);

// El PDF oficial tiene una variante con el mismo control de acceso del borrador.
assert.match(controlador, /public ActionResult AbrirFacturaSapBorrador/);
assert.match(controlador, /_bll\.ObtenerUrlPdfFactura\([\s\S]*?enc\.IdEmpresa, enc\.IdCliente, factura\.Documento\)/);

// DetalleFactura conserva una sola vista y adapta navegación y PDF al contexto.
assert.match(modelo, /string IdBorrador/);
assert.match(modelo, /bool DesdeAutorizaciones/);
assert.match(detalle, /Url\.Action\("AbrirFacturaSapBorrador"/);
assert.match(detalle, /Model\.DesdeAutorizaciones \? "Autorizaciones" : "Index"/);
assert.match(detalle, /Factura vinculada/);

console.log("OK: navegación segura a DetalleFactura desde Seguimiento y Autorizaciones validada.");
