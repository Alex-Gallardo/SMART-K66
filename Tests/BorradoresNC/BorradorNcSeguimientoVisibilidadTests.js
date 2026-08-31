"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const controlador = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Controllers", "BorradorNcController.cs"), "utf8");
const bll = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.BLL", "BorradorNcBLL.cs"), "utf8");
const dal = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.DAL", "BorradorNcDA.cs"), "utf8");

function bloque(texto, inicio, fin) {
    const desde = texto.indexOf(inicio);
    const hasta = texto.indexOf(fin, desde + inicio.length);
    assert(desde >= 0 && hasta > desde,
        "No se encontró el bloque entre " + inicio + " y " + fin + ".");
    return texto.substring(desde, hasta);
}

const listado = bloque(controlador,
    "public JsonResult Listar(string empresa = null)",
    "public JsonResult ListarSeguimiento(");
const listadoSeguimiento = bloque(controlador,
    "public JsonResult ListarSeguimiento(",
    "public JsonResult ObtenerDetalle(");
const contextos = bloque(controlador,
    "private IEnumerable<ContextoConsulta> ContextosConsulta(",
    "private IEnumerable<string> EmpresasConsulta(");
const consultaSeguimiento = bloque(controlador,
    "private bool PuedeConsultarSeguimiento(",
    "private bool PuedeImprimir(");
const detalle = bloque(controlador,
    "public JsonResult ObtenerDetalle(",
    "public JsonResult ObtenerDetallesFacturas(");
const detalleFacturas = bloque(controlador,
    "public JsonResult ObtenerDetallesFacturas(",
    "public ActionResult Autorizaciones(");

assert(listado.includes("contexto.Agentes") &&
       listadoSeguimiento.includes("contexto.Agentes"),
    "Ambos listados de Seguimiento deben usar todos los agentes asignados.");
assert(!listado.includes("PERMISO_VER_TODOS") &&
       !listadoSeguimiento.includes("PERMISO_VER_TODOS"),
    "VerTodos no debe omitir el alcance por Usuario_Empresa en Seguimiento.");

assert(contextos.includes("ParseCodigo(x.Codigo).AgenteNombre"),
    "Los agentes deben obtenerse desde las asignaciones Usuario_Empresa.");
assert(contextos.includes("GroupBy(x => x.Empresa, StringComparer.OrdinalIgnoreCase)"),
    "El alcance debe agrupar los agentes por empresa.");
assert(!contextos.includes("EsAgente()"),
    "La visibilidad no debe depender del nombre del rol del usuario.");

assert(consultaSeguimiento.includes("enc.IdUsr") &&
       consultaSeguimiento.includes("User.Identity.Name") &&
       consultaSeguimiento.includes("enc.Agente"),
    "El detalle debe aceptar al creador o a un usuario que comparta el agente.");
assert(detalle.includes("PuedeConsultarSeguimiento(enc)") &&
       detalleFacturas.includes("PuedeConsultarSeguimiento(enc)"),
    "El encabezado y las líneas de factura deben validar el mismo alcance.");

const pendientesBll = bloque(bll,
    "public List<BorradorNcEncabezado> ListarPendientes(",
    "public List<BorradorNcEncabezado> ListarSeguimiento(");
const seguimientoBll = bloque(bll,
    "public List<BorradorNcEncabezado> ListarSeguimiento(",
    "public List<BorradorNcEncabezado> ListarParaAutorizar(");
assert(pendientesBll.includes("_da.ListarVisibles(") &&
       seguimientoBll.includes("_da.ListarVisibles("),
    "La regla de visibilidad debe centralizarse en la consulta de datos.");

const consultaDal = bloque(dal,
    "public List<BorradorNcEncabezado> ListarVisibles(",
    "public BorradorNcEncabezado ObtenerPorId(");
assert(consultaDal.includes("WHERE ID_EMPRESA = @empresa"),
    "La consulta visible siempre debe limitarse a una empresa asignada.");
assert(consultaDal.includes('alcance.Add("ID_USR = @idUsr")') &&
       consultaDal.includes('alcance.Add("AGENTE = @agente" + i)') &&
       consultaDal.includes('string.Join(" OR ", alcance)'),
    "La consulta debe aplicar creador OR cualquiera de los agentes asignados.");
assert(consultaDal.includes("cmd.Parameters.Add(\"@agente\" + i"),
    "Los agentes deben enviarse mediante parámetros SQL.");

function esVisible(registro, login, empresas, agentesPorEmpresa) {
    if (empresas.indexOf(registro.empresa) < 0) return false;
    if (registro.creador.toLowerCase() === login.toLowerCase()) return true;
    return (agentesPorEmpresa[registro.empresa] || [])
        .some(x => x.toLowerCase() === registro.agente.toLowerCase());
}

const empresas = ["GRACO"];
const agentes = { GRACO: ["ANA", "CARLOS"] };
assert.strictEqual(esVisible(
    { empresa: "GRACO", creador: "otro", agente: "ANA" },
    "usuario", empresas, agentes), true, "agente compartido");
assert.strictEqual(esVisible(
    { empresa: "GRACO", creador: "usuario", agente: "NO ASIGNADO" },
    "usuario", empresas, agentes), true, "creado por el usuario");
assert.strictEqual(esVisible(
    { empresa: "GRACO", creador: "otro", agente: "NO ASIGNADO" },
    "usuario", empresas, agentes), false, "agente ajeno");
assert.strictEqual(esVisible(
    { empresa: "BOLIK", creador: "usuario", agente: "ANA" },
    "usuario", empresas, agentes), false, "empresa no asignada");

console.log("OK: Seguimiento limita borradores por empresa, creador y agentes asignados.");
