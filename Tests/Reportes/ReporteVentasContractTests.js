"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const ui = path.join(raiz, "DiamDev.Give.UI");
const controlador = fs.readFileSync(
    path.join(ui, "Controllers", "ReporteController.cs"), "utf8");
const vista = fs.readFileSync(
    path.join(ui, "Views", "Reporte", "Index.cshtml"), "utf8");
const proyecto = fs.readFileSync(
    path.join(ui, "DiamDev.Give.UI.csproj"), "utf8");
const archivoRpt = path.join(
    ui, "Reports", "Crystal", "Reporte de Ventas.rpt");

assert(fs.existsSync(archivoRpt) && fs.statSync(archivoRpt).size > 0,
    "El archivo Reporte de Ventas.rpt debe existir y no estar vacío.");
assert(proyecto.includes('Content Include="Reports\\Crystal\\Reporte de Ventas.rpt"'),
    "El proyecto debe publicar Reporte de Ventas.rpt.");

assert(controlador.includes('[Permiso("Control.Menu.Reporte_Ventas")]'),
    "El endpoint debe exigir acceso al módulo de reportes de ventas.");
assert(controlador.includes("public ActionResult ReporteVentas(long empresaId = 0"),
    "Debe existir el action ReporteVentas con empresaId estable.");
assert(controlador.includes('Server.MapPath("~/Reports/Crystal/Reporte de Ventas.rpt")'),
    "El action debe cargar el RPT correcto.");
assert(controlador.includes("TryResolverAccesoReporteVentas(CustomHelper.getUserId()"),
    "Empresa y agente deben validarse contra el usuario autenticado.");
assert(controlador.includes("r.EmpresaId == empresaId") &&
       controlador.includes("StringComparison.OrdinalIgnoreCase"),
    "La autorización debe comprobar empresa y agente sin depender de mayúsculas.");
assert(controlador.includes("DateTime.TryParseExact") &&
       controlador.includes('"yyyy-MM-dd"') &&
       controlador.includes("fechaInicioValor > fechaFinValor"),
    "El servidor debe validar formato y orden del rango de fechas.");
assert(controlador.includes("AplicarConexionHana(rpt, hanaDb)"),
    "El reporte debe usar el schema HANA resuelto para la empresa autorizada.");
[
    'rpt.SetParameterValue("Agente", agente.Trim())',
    'rpt.SetParameterValue("Empresa", empresa)',
    'rpt.SetParameterValue("Fecha Inicio", fechaInicioValor)',
    'rpt.SetParameterValue("Fecha Fin", fechaFinValor)'
].forEach(contrato => assert(controlador.includes(contrato),
    "Falta asignar directamente el parámetro Crystal: " + contrato));
assert(controlador.includes("Trace.TraceError") &&
       controlador.includes("No fue posible generar el Reporte de Ventas."),
    "Los errores internos deben registrarse sin exponer el stack trace.");
assert(controlador.includes("p.ParameterValueKind"),
    "DiagParametros debe mostrar el tipo real de los parámetros Crystal.");

assert(vista.includes("<h2>Reporte de Ventas</h2>") &&
       vista.includes('@Url.Action("ReporteVentas", "Reporte")'),
    "La sección Ventas debe incluir la tarjeta del nuevo reporte.");
[
    "modalReporteVentas",
    "rvEmpresa",
    "rvAgente",
    "rvFechaInicio",
    "rvFechaFin",
    "btnVerReporteVentas"
].forEach(id => assert(vista.includes('id="' + id + '"'),
    "Falta el control del modal: " + id));
assert(vista.includes('$("#rvEmpresa").on("change", actualizarAgentesReporteVentas)') &&
       vista.includes('poblarSelectAgentes(selAgente, empresaId, "#rvAgenteHelp")'),
    "Cambiar de empresa debe recargar únicamente sus agentes.");
[
    '"?empresaId=" + encodeURIComponent(empresaId)',
    '"&agente=" + encodeURIComponent(agente)',
    '"&fechaInicio=" + encodeURIComponent(fechaInicio)',
    '"&fechaFin=" + encodeURIComponent(fechaFin)'
].forEach(fragmento => assert(vista.includes(fragmento),
    "El navegador no envía correctamente: " + fragmento));
assert(vista.includes("if (!empresaId)") &&
       vista.includes("if (!agente)") &&
       vista.includes("if (!fechaInicio || !fechaFin)") &&
       vista.includes("if (fechaInicio > fechaFin)"),
    "El modal debe validar los cuatro parámetros y el orden de las fechas.");
assert(vista.includes('case "modalReporteVentas"') &&
       vista.includes('$("#btnVerReporteVentas").click()'),
    "Enter debe generar el Reporte de Ventas desde su modal.");

console.log("OK: integración MVC, seguridad, parámetros y publicación del Reporte de Ventas verificadas.");
