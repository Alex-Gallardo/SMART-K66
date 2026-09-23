"use strict";

const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const raiz = path.resolve(__dirname, "../..");
const leer = nombre => fs.readFileSync(path.join(raiz, nombre), "utf8");
const js = leer("DiamDev.Give.UI/Scripts/App/BorradorNc-Index.js");
const controlador = leer("DiamDev.Give.UI/Controllers/BorradorNcController.cs");
const bll = leer("DiamDev.Give.BLL/BorradorNcBLL.cs");
const dal = leer("DiamDev.Give.DAL/BorradorNcDA.cs");

const funcion = js.match(/function estadoAnulable\(estado\) \{[\s\S]*?\n    \}/);
assert.ok(funcion, "Seguimiento debe definir la elegibilidad por estado.");
const estadoAnulable = vm.runInNewContext("(" + funcion[0] + ")");
for (const estado of ["PENDIENTE", "AUTORIZADO", "RECHAZADO"])
    assert.equal(estadoAnulable(estado), true, estado + " debe permitir anulación");
for (const estado of ["ANULADO", "", "DESCONOCIDO", null])
    assert.equal(estadoAnulable(estado), false, String(estado) + " no debe permitir anulación");
assert.match(js, /state\.puedeAnular && estadoAnulable\(x\.Estado\)/);
assert.match(js, /else if \(x\.Estado === "RECHAZADO"\) r\+\+/);

const accion = controlador.split("public JsonResult Anular(AnularBorradorNcRequest request)")[1]
    .split("public ActionResult Imprimir(")[0];
assert.match(accion, /ValidarEmpresa\(request\.Empresa\)/);
assert.match(accion, /_bll\.ObtenerPorId\(request\.Empresa, request\.IdBorrador\)/);
assert.match(accion, /!PuedeConsultarSeguimiento\(enc\)/);
assert.ok(accion.indexOf("!PuedeConsultarSeguimiento(enc)") < accion.indexOf("_bll.Anular("),
    "El alcance debe verificarse antes de modificar el borrador.");
assert.match(controlador, /\[ValidateAntiForgeryToken\]\s*\[BorradorNcPermiso\(PERMISO_ANULAR\)\]\s*public JsonResult Anular/);

const negocio = bll.split("public ResultadoBorradorNc Anular(")[1]
    .split("// =====================================================================")[0];
assert.match(negocio, /AutorizacionPermisoPorUsuario\(usuario, PERMISO_ANULAR\)/);
assert.match(negocio, /string\.IsNullOrWhiteSpace\(motivo\)/);
assert.match(negocio, /pendientes, autorizados o rechazados/);

const datos = dal.split("public int Anular(")[1].split("// =====================================================================")[0];
assert.match(datos, /ESTADO IN \('PENDIENTE', 'AUTORIZADO', 'RECHAZADO'\)/);
assert.match(datos, /OUTPUT deleted\.ESTADO INTO @estadosAnteriores/);
assert.match(datos, /INSERT dbo\.BORR_NC_BITACORA[\s\S]*FROM @estadosAnteriores E/);
assert.match(datos, /using \(var tx = cn\.BeginTransaction\(\)\)/);
assert.doesNotMatch(datos, /AND ESTADO\s*=\s*'AUTORIZADO'/);

for (const nombre of [
    "08_ampliar_permiso_anular_seguro.sql",
    "08_ampliar_permiso_anular_DEV_seguro.sql"
]) {
    const sql = leer("SqlMigrations/BorradoresNC/" + nombre);
    assert.match(sql, /Control\.BorradorNC\.Anular/);
    assert.match(sql, /Anular borradores pendientes, autorizados o rechazados/);
    assert.match(sql, /BEGIN TRANSACTION;[\s\S]*COMMIT TRANSACTION;/);
    assert.doesNotMatch(sql, /INSERT\s+(?:INTO\s+)?dbo\.Rol_Permiso/i);
}

console.log("OK: anulación por estado, alcance, permiso, bitácora y scripts de actualización.");
