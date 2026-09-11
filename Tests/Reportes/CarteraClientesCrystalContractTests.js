const assert = require("assert");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..", "..");
const ui = path.join(root, "DiamDev.Give.UI");
const controlador = fs.readFileSync(
    path.join(ui, "Controllers", "ReporteController.cs"), "utf8");
const vista = fs.readFileSync(
    path.join(ui, "Views", "Reporte", "Index.cshtml"), "utf8");
const proyecto = fs.readFileSync(
    path.join(ui, "DiamDev.Give.UI.csproj"), "utf8");

const reportes = [
    { empresa: "Bolik", constante: "ID_BOLIK", archivo: "Cartera Clientes Bolik.rpt" },
    { empresa: "Faes", constante: "ID_FAES", archivo: "Cartera Clientes Faes.rpt" },
    { empresa: "Graco", constante: "ID_GRACO", archivo: "Cartera Clientes Graco.rpt" }
];

for (const reporte of reportes) {
    const archivoRpt = path.join(ui, "Reports", "Crystal", reporte.archivo);
    assert(fs.existsSync(archivoRpt) && fs.statSync(archivoRpt).size > 0,
        `El archivo ${reporte.archivo} debe existir y no estar vacío.`);
    assert(proyecto.includes(`Content Include="Reports\\Crystal\\${reporte.archivo}"`),
        `El proyecto debe publicar ${reporte.archivo}.`);
    assert(controlador.includes(`public ActionResult CarteraClientes${reporte.empresa}`),
        `Debe conservarse el endpoint CarteraClientes${reporte.empresa}.`);
    assert(controlador.includes(`UsuarioEmpresaBL.${reporte.constante}`) &&
           controlador.includes(`"${reporte.archivo}"`),
        `${reporte.empresa} debe usar su empresa y archivo Crystal correspondientes.`);
    assert(vista.includes(`CarteraClientes${reporte.empresa}`),
        `La vista debe enlazar el reporte de ${reporte.empresa}.`);
}

const reporteAnterior = path.join(ui, "Reports", "Crystal", "Cartera de Clientes.rpt");
assert(fs.existsSync(reporteAnterior) && fs.statSync(reporteAnterior).size > 0,
    "El reporte anterior Cartera de Clientes.rpt debe conservarse.");
assert(proyecto.includes('Content Include="Reports\\Crystal\\Cartera de Clientes.rpt"'),
    "El reporte anterior debe continuar registrado en el proyecto.");
assert(!controlador.includes('Server.MapPath("~/Reports/Crystal/Cartera de Clientes.rpt")'),
    "Los endpoints ya no deben ejecutar el reporte anterior.");

assert((controlador.match(/\[Permiso\("Control\.Menu\.Reporte_Ventas"\)\]/g) || []).length >= 4,
    "Los tres endpoints de cartera deben exigir permiso de reportes de ventas.");
assert(controlador.includes("private ActionResult GenerarCarteraClientes") &&
       controlador.includes("TryResolverAccesoReporteVentas(CustomHelper.getUserId()"),
    "La generación debe centralizarse y validar empresa/agente para el usuario autenticado.");
assert(controlador.includes('rpt.SetParameterValue("FECHA CORTE", fechaCorteValor)') &&
       controlador.includes('rpt.SetParameterValue("AGENTE", agente.Trim())') &&
       controlador.includes('rpt.SetParameterValue("CLIENTE",'),
    "Deben establecerse los parámetros obligatorios del contrato Crystal.");
assert(controlador.includes("TryParseFechaReporte(fechaCorte") &&
       controlador.includes('new HttpStatusCodeResult(403'),
    "La fecha debe validarse de forma estable y el acceso no autorizado debe rechazarse.");
assert(controlador.includes('"[CarteraClientes] Error al generar') &&
       controlador.includes('"No fue posible generar el reporte de Cartera de Clientes."'),
    "Los errores deben registrarse y responder sin exponer la excepción.");
assert(vista.includes('if (!ag) { toastr.error("Selecciona un agente"); return; }'),
    "El modal debe validar el agente requerido antes de abrir el PDF.");
assert(vista.includes('toastr.warning("Espera mientras se cargan tus empresas y agentes")') &&
       vista.includes('toastr.error("No tienes agentes configurados para esta empresa")'),
    "El modal no debe abrirse antes de cargar accesos ni para empresas sin agentes.");

console.log("CarteraClientesCrystalContractTests: OK");
