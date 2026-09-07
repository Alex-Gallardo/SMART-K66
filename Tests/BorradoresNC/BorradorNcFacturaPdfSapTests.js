"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");

const hana = leer("DiamDev.Give.DAL", "HanaRepository.cs");
const bll = leer("DiamDev.Give.BLL", "BorradorNcBLL.cs");
const controlador = leer("DiamDev.Give.UI", "Controllers", "BorradorNcController.cs");
const modelo = leer("DiamDev.Give.UI", "Models", "BorradorNcViewModel.cs");
const vista = leer("DiamDev.Give.UI", "Views", "BorradorNc", "DetalleFactura.cshtml");

// La URL pertenece al encabezado OINV y se consulta sin duplicarla por INV1.
assert.match(hana, /public string ObtenerUrlPdfFactura/);
assert.match(hana, /H\.""U_FACE_PDFFILE""/);
assert.match(hana, /FROM ""\{0\}""\.""OINV"" H/);
assert.match(hana, /H\.""DocNum"" = \?/);
assert.match(hana, /H\.""CardCode"" = \?/);
assert.match(hana, /H\.""CANCELED"" = 'N'/);
assert.doesNotMatch(
    hana.match(/public string ObtenerUrlPdfFactura[\s\S]*?\n        }/)?.[0] || "",
    /INV1/,
    "La consulta del PDF no debe unir los renglones de la factura."
);

// Solo se permite redirigir hacia una URL absoluta HTTP o HTTPS obtenida de SAP.
assert.match(bll, /Uri\.TryCreate\(valor\.Trim\(\), UriKind\.Absolute/);
assert.match(bll, /Uri\.UriSchemeHttp/);
assert.match(bll, /Uri\.UriSchemeHttps/);
assert.match(bll, /return uri\.AbsoluteUri/);

// El endpoint vuelve a validar el contexto de empresa, agente, cliente y factura.
assert.match(controlador, /public ActionResult AbrirFacturaSap/);
assert.match(controlador, /\[BorradorNcPermiso\(PERMISO_VER\)\]/);
assert.match(controlador, /ObtenerFacturaAccesible\(/);
assert.match(controlador, /ResolverAgente\(empresa, codigoOperador\)/);
assert.match(controlador, /string\.Equals\(\(x\.CardCode/);
assert.match(controlador, /PdfFacturaDisponible = !string\.IsNullOrWhiteSpace\(urlPdf\)/);
assert.match(controlador, /return Redirect\(urlPdf\)/);

// La vista abre el PDF oficial en otra pestaña y comunica la ausencia del enlace.
assert.match(modelo, /bool PdfFacturaDisponible/);
assert.match(modelo, /string CodigoOperador/);
assert.match(vista, /Url\.Action\("AbrirFacturaSap", "BorradorNc"/);
assert.match(vista, /target="_blank" rel="noopener noreferrer"/);
assert.match(vista, /Abrir \/ imprimir factura/);
assert.match(vista, /PDF no disponible/);
assert.doesNotMatch(vista, /onclick="window\.print\(\)"/);

console.log("OK: apertura segura del PDF oficial de factura desde SAP validada.");
