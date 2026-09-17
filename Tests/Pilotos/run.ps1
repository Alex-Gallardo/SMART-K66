param([string]$MvcDll, [string]$WebPagesDll, [string]$RazorDll, [string]$ScriptDomDll)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if(!$MvcDll) {
    $MvcDll=Join-Path $repo 'packages/Microsoft.AspNet.Mvc.5.2.9/lib/net45/System.Web.Mvc.dll'
    if(!(Test-Path -LiteralPath $MvcDll)) { $MvcDll=Join-Path $env:USERPROFILE '.nuget/packages/microsoft.aspnet.mvc/5.2.9/lib/net45/System.Web.Mvc.dll' }
}
if(!(Test-Path -LiteralPath $MvcDll)) { throw 'Restaurar Microsoft.AspNet.Mvc 5.2.9 o indicar -MvcDll.' }
if(!$WebPagesDll) {
    $WebPagesDll=Join-Path $repo 'packages/Microsoft.AspNet.WebPages.3.2.9/lib/net45/System.Web.WebPages.dll'
    if(!(Test-Path -LiteralPath $WebPagesDll)) { $WebPagesDll=Join-Path $env:USERPROFILE '.nuget/packages/microsoft.aspnet.webpages/3.2.9/lib/net45/System.Web.WebPages.dll' }
}
if(!(Test-Path -LiteralPath $WebPagesDll)) { throw 'Restaurar Microsoft.AspNet.WebPages 3.2.9 o indicar -WebPagesDll.' }
$out=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $out | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sources=@('DiamDev.Give.Entities/PilotoRuta.cs','DiamDev.Give.Entities/PilotoReglas.cs','DiamDev.Give.Entities/PilotoDesafio.cs','DiamDev.Give.DAL/PilotoRutaDA.cs','DiamDev.Give.BLL/PilotoRutaBL.cs','DiamDev.Give.UI/Controllers/PilotoController.cs','Tests/Pilotos/PilotoTests.cs') | ForEach-Object { Join-Path $repo $_ }
& $compiler /nologo /target:exe "/out:$out/PilotoTests.exe" /r:System.Configuration.dll /r:System.Data.dll /r:System.Xml.Linq.dll /r:System.Xml.dll /r:System.Web.dll /r:System.ComponentModel.DataAnnotations.dll "/r:$MvcDll" $sources
if($LASTEXITCODE -ne 0) { throw 'Fallo compilacion aislada del modulo.' }
Copy-Item -LiteralPath $MvcDll -Destination $out -Force
Copy-Item -LiteralPath $WebPagesDll -Destination $out -Force
& (Join-Path $out 'PilotoTests.exe')
if($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas.' }
$testConfig=Join-Path $out 'PilotoTests.exe.config'
@'
<configuration>
  <appSettings>
    <add key="Pilotos.Habilitado" value="false" />
    <add key="Pilotos.PruebasSoloLectura" value="true" />
    <add key="Pilotos.PermitirCierre" value="true" />
    <add key="Pilotos.UsuarioPrueba" value="consulta_demo" />
    <add key="Pilotos.PlacaPrueba" value="DEMO" />
  </appSettings>
  <connectionStrings>
    <add name="GiveContext" connectionString="Data Source=invalid.invalid;Initial Catalog=TEST_POS;Integrated Security=true;Connect Timeout=1" />
    <add name="APK66Context" connectionString="Data Source=invalid.invalid;Initial Catalog=TEST_RUTAS;Integrated Security=true;Connect Timeout=1" />
  </connectionStrings>
</configuration>
'@ | Set-Content -LiteralPath $testConfig -Encoding UTF8
try {
    & (Join-Path $out 'PilotoTests.exe') consulta-temporal
    if($LASTEXITCODE -ne 0) { throw 'Fallo aislamiento del modo de consulta.' }
} finally {
    # Solo elimina el archivo de configuracion ficticio creado por este test.
    Remove-Item -LiteralPath $testConfig -Force
}
if(!$RazorDll) {
    $RazorDll=Join-Path $repo 'packages/Microsoft.AspNet.Razor.3.2.9/lib/net45/System.Web.Razor.dll'
    if(!(Test-Path -LiteralPath $RazorDll)) { $RazorDll=Join-Path $env:USERPROFILE '.nuget/packages/microsoft.aspnet.razor/3.2.9/lib/net45/System.Web.Razor.dll' }
}
if(!(Test-Path -LiteralPath $RazorDll)) { throw 'Restaurar Microsoft.AspNet.Razor 3.2.9 o indicar -RazorDll.' }
$pagesDir=Split-Path $WebPagesDll
Get-ChildItem -LiteralPath $pagesDir -Filter '*.dll' | Copy-Item -Destination $out -Force
Copy-Item -LiteralPath $RazorDll -Destination $out -Force
& $compiler /nologo /target:exe "/out:$out/RazorCompile.exe" /r:System.Web.dll /r:Microsoft.CSharp.dll "/r:$MvcDll" "/r:$WebPagesDll" "/r:$pagesDir/System.Web.WebPages.Razor.dll" "/r:$RazorDll" (Join-Path $PSScriptRoot 'RazorCompile.cs')
if($LASTEXITCODE -ne 0) { throw 'Fallo compilacion del verificador Razor.' }
& (Join-Path $out 'RazorCompile.exe') $repo
if($LASTEXITCODE -ne 0) { throw 'Fallaron las vistas Razor.' }
if(!$ScriptDomDll) { $ScriptDomDll=Join-Path $env:USERPROFILE '.nuget/packages/microsoft.sqlserver.transactsql.scriptdom/161.8901.0/lib/net462/Microsoft.SqlServer.TransactSql.ScriptDom.dll' }
if(!(Test-Path -LiteralPath $ScriptDomDll)) { throw 'Indicar -ScriptDomDll (version 161/net462) para la validacion SQL sin conexion.' }
Copy-Item -LiteralPath $ScriptDomDll -Destination $out -Force
& $compiler /nologo /target:exe "/out:$out/SqlCompile.exe" "/r:$ScriptDomDll" (Join-Path $PSScriptRoot 'SqlCompile.cs')
if($LASTEXITCODE -ne 0) { throw 'Fallo compilacion del verificador SQL.' }
$scripts=Get-ChildItem -LiteralPath (Join-Path $repo 'SqlMigrations/Pilotos') -Filter '1*.sql' | ForEach-Object FullName
& (Join-Path $out 'SqlCompile.exe') $scripts
if($LASTEXITCODE -ne 0) { throw 'Fallo sintaxis SQL.' }
