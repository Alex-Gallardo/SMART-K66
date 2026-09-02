"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");

const vista = leer("DiamDev.Give.UI", "Views", "BorradorNc", "Index.cshtml");
const detalle = leer("DiamDev.Give.UI", "Views", "BorradorNc", "DetalleFactura.cshtml");
const javascript = leer("DiamDev.Give.UI", "Scripts", "App", "BorradorNc-Index.js");
const css = leer("DiamDev.Give.UI", "Content", "borrador-nc.css");
const controlador = leer("DiamDev.Give.UI", "Controllers", "BorradorNcController.cs");
const bll = leer("DiamDev.Give.BLL", "BorradorNcBLL.cs");
const modelo = leer("DiamDev.Give.UI", "Models", "BorradorNcViewModel.cs");
const proyecto = leer("DiamDev.Give.UI", "DiamDev.Give.UI.csproj");

function contiene(texto, fragmento, mensaje) {
    assert.ok(texto.includes(fragmento), mensaje || `Falta: ${fragmento}`);
}

function noContiene(texto, fragmento, mensaje) {
    assert.ok(!texto.includes(fragmento), mensaje || `No debe existir: ${fragmento}`);
}

// Autocomplete directo y modal deben coexistir y usar una selección canónica.
contiene(vista, 'id="bncClienteCodigo"', "Debe conservarse el input de cliente.");
contiene(vista, 'aria-controls="bncClienteDropdown"', "El input debe exponer su listbox.");
contiene(vista, 'id="bncClienteDropdown"', "Debe existir el dropdown directo.");
contiene(vista, 'id="bncClienteModal"', "La búsqueda por modal debe mantenerse.");
noContiene(vista, 'id="bncClienteCodigo" readonly', "El código de cliente debe permitir búsqueda escrita.");
contiene(javascript, 'programarBusquedaClientes("directo")');
contiene(javascript, 'programarBusquedaClientes("modal")');
contiene(javascript, '}, 350);', "La consulta debe conservar el debounce de ReciboCaja.");
contiene(javascript, "clienteSeleccionadoValido()", "Guardar y buscar facturas deben exigir una selección SAP.");
contiene(css, ".bnc-client-dropdown");
contiene(css, "position: absolute;");

// El enlace se captura solo mediante URL; el nombre lo deriva el BLL del dominio.
noContiene(vista, 'id="bncEnlaceTitulo"');
noContiene(javascript, 'Enlaces[" + i + "].Titulo');
noContiene(modelo, "public string Titulo { get; set; }");
noContiene(controlador, "Nombre = enlace.Titulo");
contiene(bll, "if (nombre.Length == 0) nombre = uri.Host;");

// La acción visual no debe competir con el clic usado para seleccionar la fila.
contiene(vista, 'data-url-factura-detalle');
contiene(vista, "<th class=\"text-center\">Acciones</th>");
contiene(javascript, 'class="bnc-btn bnc-btn-ghost bnc-btn-compact bnc-view-invoice"');
contiene(javascript, 'target="_blank" rel="noopener"');
contiene(javascript, 'closest(".bnc-view-invoice")');

// La consulta directa valida el contexto del usuario y exige coincidencia exacta.
contiene(controlador, "public ActionResult DetalleFactura");
contiene(controlador, "[BorradorNcPermiso(PERMISO_VER)]");
contiene(controlador, "string agenteEfectivo = ResolverAgente(empresa, codigoOperador);");
contiene(controlador, "StringComparison.OrdinalIgnoreCase");
contiene(controlador, "_bll.ObtenerDetallesFacturas(");

// El detalle se presenta como tabla HTML real con desplazamiento horizontal.
contiene(detalle, '<table class="bnc-sap-lines-table">');
contiene(detalle, '<th scope="col">Producto / servicio</th>');
contiene(detalle, '<td class="bnc-sap-line-product">');
contiene(css, ".bnc-sap-table-region");
contiene(css, "overflow-x: auto;");
contiene(proyecto, 'Views\\BorradorNc\\DetalleFactura.cshtml');

console.log("OK: búsqueda de clientes, enlaces y detalle de factura verificados.");
