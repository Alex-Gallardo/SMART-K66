$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$packages = Join-Path $project 'packages'
if (-not (Test-Path -LiteralPath $packages)) {
    $packages = Join-Path (Split-Path $project -Parent) 'SMART-K66\packages'
}
if (-not (Test-Path -LiteralPath $packages)) { throw 'Restaure los paquetes NuGet antes de ejecutar las pruebas.' }
$csc = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw 'Se requiere .NET Framework 4.x.' }
$mvc = Join-Path $packages 'Microsoft.AspNet.Mvc.5.2.9\lib\net45\System.Web.Mvc.dll'
$epplus = Join-Path $packages 'EPPlus.4.5.2.1\lib\net40\EPPlus.dll'
$razor = Join-Path $packages 'Microsoft.AspNet.Razor.3.2.9\lib\net45\System.Web.Razor.dll'
$webpages = Join-Path $packages 'Microsoft.AspNet.WebPages.3.2.9\lib\net45\System.Web.WebPages.dll'
$webpagesRazor = Join-Path $packages 'Microsoft.AspNet.WebPages.3.2.9\lib\net45\System.Web.WebPages.Razor.dll'
$testDir = Join-Path $env:TEMP ('GerenciaComercialTests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
$hanaStub = Join-Path $testDir 'HanaStub.cs'
$uiStub = Join-Path $testDir 'UiStub.cs'
@'
namespace DiamDev.Give.DAL {
    public static class HanaHelper {
        public static readonly System.Collections.Generic.List<string> Consultas = new System.Collections.Generic.List<string>();
        public static System.Data.DataTable EjecutarConsulta(string sql, System.Data.Odbc.OdbcParameter[] parametros = null) {
            Consultas.Add(sql);
            var table = new System.Data.DataTable();
            table.Columns.Add("Dato", typeof(int));
            table.Columns.Add("TotalFilas", typeof(int));
            table.Rows.Add(1, 1);
            return table;
        }
    }
}
'@ | Set-Content -LiteralPath $hanaStub -Encoding UTF8
@'
namespace DiamDev.Give.UI.App_Start {
    public static class CustomHelper { public static void setTitle(string a, string b) {} }
}
namespace DiamDev.Give.BLL {
    public class RolBL {
        public static bool Permitido;
        public static string UltimoPermiso;
        public bool AutorizacionPermisoPorUsuario(string usuario, string permiso) {
            UltimoPermiso = permiso;
            return Permitido;
        }
    }
}
'@ | Set-Content -LiteralPath $uiStub -Encoding UTF8
$dal = Join-Path $testDir 'GerenciaComercialDA.dll'
$ui = Join-Path $testDir 'GerenciaComercialUI.dll'
$exe = Join-Path $testDir 'GerenciaComercialTests.exe'
@'
<configuration><appSettings>
<add key="HanaSchema.BOLIK" value="SBOBOLIK" />
<add key="HanaSchema.FAES" value="SBOESCOCESA" />
<add key="HanaSchema.GRACO" value="SBO_GRACO" />
</appSettings></configuration>
'@ | Set-Content -LiteralPath ($exe + '.config') -Encoding UTF8
& $csc /nologo /target:library "/out:$dal" /r:System.Core.dll /r:System.Data.dll /r:System.Configuration.dll (Join-Path $project 'DiamDev.Give.DAL\GerenciaComercialDA.cs') (Join-Path $project 'DiamDev.Give.DAL\GerenciaComercialAnalisisDA.cs') $hanaStub
if ($LASTEXITCODE -ne 0) { throw 'No compiló la capa de datos.' }
& $csc /nologo /target:library "/out:$ui" /r:System.Core.dll /r:System.Data.dll /r:System.Drawing.dll /r:System.Web.dll "/r:$dal" "/r:$mvc" "/r:$epplus" (Join-Path $project 'DiamDev.Give.UI\Controllers\GerenciaComercialController.cs') (Join-Path $project 'DiamDev.Give.UI\App_Start\PermisoAttribute.cs') $uiStub
if ($LASTEXITCODE -ne 0) { throw 'No compiló el controlador.' }
& $csc /nologo /target:exe "/out:$exe" /r:System.Core.dll /r:System.Data.dll /r:System.Web.dll "/r:$dal" "/r:$ui" "/r:$mvc" (Join-Path $PSScriptRoot 'GerenciaComercialTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'No compilaron las pruebas.' }
Copy-Item -LiteralPath $mvc -Destination $testDir
Copy-Item -LiteralPath $epplus -Destination $testDir
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Las pruebas fallaron.' }
$razorExe = Join-Path $testDir 'RazorSyntaxCheck.exe'
& $csc /nologo /target:exe "/out:$razorExe" /r:System.Core.dll /r:System.Web.dll "/r:$mvc" "/r:$razor" "/r:$webpages" "/r:$webpagesRazor" (Join-Path $PSScriptRoot 'RazorSyntaxCheck.cs')
if ($LASTEXITCODE -ne 0) { throw 'No compiló el verificador Razor.' }
Copy-Item -LiteralPath $razor -Destination $testDir
Copy-Item -LiteralPath $webpages -Destination $testDir
Copy-Item -LiteralPath $webpagesRazor -Destination $testDir
& $razorExe $project
if ($LASTEXITCODE -ne 0) { throw 'Razor contiene errores.' }
$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) { throw 'Se requiere Node.js para las pruebas de interfaz.' }
& $node.Source (Join-Path $PSScriptRoot 'gerencia-comercial-ui.test.js')
if ($LASTEXITCODE -ne 0) { throw 'Las pruebas de interfaz fallaron.' }
& $node.Source --check (Join-Path $project 'DiamDev.Give.UI\Scripts\App\gerencia-comercial-analisis.js')
if ($LASTEXITCODE -ne 0) { throw 'JavaScript de análisis contiene errores.' }
& $node.Source (Join-Path $PSScriptRoot 'gerencia-comercial-analisis-ui.test.js')
if ($LASTEXITCODE -ne 0) { throw 'Las pruebas de análisis comercial fallaron.' }
