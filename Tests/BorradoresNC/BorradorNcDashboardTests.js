const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "../..");
const read = p => fs.readFileSync(path.join(root, p), "utf8");

const controller = read("DiamDev.Give.UI/Controllers/BorradorNcController.cs");
const dal = read("DiamDev.Give.DAL/BorradorNcDashboardDA.cs");
const view = read("DiamDev.Give.UI/Views/BorradorNc/DashboardBNC.cshtml");
const migration = read("SqlMigrations/BorradoresNC/06_crear_dashboard_bitacora_seguro.sql");

assert.match(controller, /Control\.BorradorNC\.Dashboard/);
assert.match(controller, /Control\.BorradorNC\.VerTodos/);
assert.match(controller, /\[ValidateAntiForgeryToken\][\s\S]*ExportarDashboardBNC/);
assert.match(controller, /\[ValidateAntiForgeryToken\][\s\S]*ImprimirLoteBNC/);
assert.match(controller, /PuedeConsultarDashboard\(enc\)/);
assert.match(dal, /E\.ID_USR=@alcanceUsuario/);
assert.match(dal, /E\.ID_EMPRESA=.*E\.AGENTE IN/);
assert.match(dal, /OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY/);
assert.match(dal, /new SqlParameter\(nombre/);
assert.doesNotMatch(dal, /ORDER BY " \+ filtro\.Orden/);
assert.match(view, /Exportar Excel/);
assert.match(view, /Imprimir selección/);
assert.match(view, /data-url-bitacora/);
assert.match(migration, /CREATE TABLE dbo\.BORR_NC_BITACORA/);
assert.match(migration, /N'CREDITOS'/);
assert.match(migration, /Control\.BorradorNC\.Dashboard/);
assert.match(migration, /Control\.BorradorNC\.VerTodos/);

console.log("OK: dashboard BorradorNC valida alcance, exportación, impresión y bitácora.");
