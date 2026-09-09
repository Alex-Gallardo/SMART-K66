const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "../..");
const read = p => fs.readFileSync(path.join(root, p), "utf8");

const controller = read("DiamDev.Give.UI/Controllers/BorradorNcController.cs");
const bll = read("DiamDev.Give.BLL/BorradorNcDashboardBLL.cs");
const dal = read("DiamDev.Give.DAL/BorradorNcDashboardDA.cs");
const entitiesProject = read("DiamDev.Give.Entities/DiamDev.Give.Entities.csproj");
const dalProject = read("DiamDev.Give.DAL/DiamDev.Give.DAL.csproj");
const bllProject = read("DiamDev.Give.BLL/DiamDev.Give.BLL.csproj");
const uiProject = read("DiamDev.Give.UI/DiamDev.Give.UI.csproj");
const view = read("DiamDev.Give.UI/Views/BorradorNc/DashboardBNC.cshtml");
const migration = read("SqlMigrations/BorradoresNC/06_crear_dashboard_bitacora_seguro.sql");
const migrationDev = read("SqlMigrations/BorradoresNC/06_crear_dashboard_bitacora_DEV_seguro.sql");
const validation = read("SqlMigrations/BorradoresNC/06_validar_dashboard_solo_lectura.sql");

// Evita la regresión que originó CS0246 en BorradorNcDashboardBLL: cada archivo
// debe estar compilado y cada capa debe conservar sus referencias de proyecto.
assert.match(entitiesProject, /<Compile Include="BorradorNcDashboard\.cs"\s*\/>/);
assert.match(dalProject, /<Compile Include="BorradorNcDashboardDA\.cs"\s*\/>/);
assert.match(dalProject, /<ProjectReference Include="\.\.\\DiamDev\.Give\.Entities\\DiamDev\.Give\.Entities\.csproj">/);
assert.match(bllProject, /<Compile Include="BorradorNcDashboardBLL\.cs"\s*\/>/);
assert.match(bllProject, /<ProjectReference Include="\.\.\\DiamDev\.Give\.DAL\\DiamDev\.Give\.DAL\.csproj">/);
assert.match(bllProject, /<ProjectReference Include="\.\.\\DiamDev\.Give\.Entities\\DiamDev\.Give\.Entities\.csproj">/);
assert.match(uiProject, /<ProjectReference Include="\.\.\\DiamDev\.Give\.BLL\\DiamDev\.Give\.BLL\.csproj">/);
assert.match(uiProject, /<Compile Include="Controllers\\BorradorNcController\.cs"\s*\/>/);
assert.match(uiProject, /<Content Include="Views\\BorradorNc\\DashboardBNC\.cshtml"\s*\/>/);
assert.match(bll, /using DiamDev\.Give\.DAL;/);
assert.match(bll, /using DiamDev\.Give\.Entities;/);

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
assert.match(migration, /Control\.BorradorNC\.Dashboard/);
assert.match(migration, /USE \[POS-SmartK66\]/);
assert.doesNotMatch(migration, /N'CREDITOS'/);
assert.doesNotMatch(migration, /INSERT\s+(?:INTO\s+)?dbo\.Rol\b/);
assert.doesNotMatch(migration, /INSERT\s+(?:INTO\s+)?dbo\.Rol_Permiso\b/);
assert.match(migrationDev, /USE \[POS-SmartK66_DEV\]/);
assert.match(migrationDev, /DB_NAME\(\) <> N'POS-SmartK66_DEV'/);
assert.match(migrationDev, /CREATE TABLE dbo\.BORR_NC_BITACORA/);
assert.doesNotMatch(migrationDev, /INSERT\s+(?:INTO\s+)?dbo\.Rol(?:_Permiso)?\b/);
assert.match(validation, /SOLO LECTURA/);
assert.doesNotMatch(validation, /\b(?:INSERT|UPDATE|DELETE|CREATE|ALTER|DROP|TRUNCATE)\s+(?:TABLE\s+)?dbo\./i);

console.log("OK: dashboard BorradorNC valida compilación por capas, alcance, exportación, impresión y bitácora.");
