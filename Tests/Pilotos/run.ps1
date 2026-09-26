param([string]$MvcDll, [string]$WebPagesDll, [string]$RazorDll, [string]$ScriptDomDll)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$webConfig=New-Object System.Xml.XmlDocument
$webConfig.Load((Join-Path $repo 'DiamDev.Give.UI/Web.config'))
$cierreSetting=@($webConfig.SelectNodes("/configuration/appSettings/add[@key='Pilotos.PermitirCierre']"))
if($cierreSetting.Count -ne 1 -or $cierreSetting[0].GetAttribute('value') -notin @('true','false')) {
    throw 'El Web.config versionado debe declarar una sola clave booleana Pilotos.PermitirCierre.'
}
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
$sources=@('DiamDev.Give.Entities/PilotoRuta.cs','DiamDev.Give.Entities/PilotoReglas.cs','DiamDev.Give.Entities/PilotoDesafio.cs','DiamDev.Give.Entities/PilotoAdministracion.cs','DiamDev.Give.DAL/PilotoRutaDA.cs','DiamDev.Give.DAL/PilotoAdministracionDA.cs','DiamDev.Give.BLL/PilotoRutaBL.cs','DiamDev.Give.BLL/PilotoAdministracionBL.cs','DiamDev.Give.UI/Controllers/PilotoController.cs','Tests/Pilotos/PilotoTests.cs') | ForEach-Object { Join-Path $repo $_ }
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
    <add key="Pilotos.CatalogoRutas" value="TEST_RUTAS" />
  </appSettings>
  <connectionStrings>
    <add name="GiveContext" connectionString="Data Source=invalid.invalid;Initial Catalog=TEST_POS;Integrated Security=true;Connect Timeout=1" />
    <add name="APK66Context" connectionString="Data Source=other.invalid;Initial Catalog=OTHER_RUTAS;Integrated Security=true;Connect Timeout=1" />
  </connectionStrings>
</configuration>
'@ | Set-Content -LiteralPath $testConfig -Encoding UTF8
try {
    & (Join-Path $out 'PilotoTests.exe') consulta-temporal
    if($LASTEXITCODE -ne 0) { throw 'Fallo aislamiento del modo de consulta.' }
    $fixture=New-Object System.Xml.XmlDocument
    $fixture.Load($testConfig)
    $baseFixture=$fixture.OuterXml
    $cases=@(
        @{Name='config-invalida';Values=@{'Pilotos.UsuarioPrueba'=''}},
        @{Name='config-invalida';Values=@{'Pilotos.PlacaPrueba'=''}},
        @{Name='config-invalida';Values=@{'Pilotos.RutaPrueba'='DEMO-1'}},
        @{Name='config-invalida';Values=@{'Pilotos.PlacaPrueba'='ABCDEFGHIJKLMNOP'}},
        @{Name='config-invalida';Values=@{'Pilotos.PlacaPrueba'='';'Pilotos.RutaPrueba'='DEMO-1';'Pilotos.PilotoPrueba'='PILOTO DEMO'}},
        @{Name='config-invalida';Values=@{'Pilotos.PilotoPrueba'=('A'*91)}},
        @{Name='consulta-temporal';Values=@{'Pilotos.PilotoPrueba'='PILOTO DEMO'}},
        @{Name='ruta-fija';Values=@{'Pilotos.PlacaPrueba'='';'Pilotos.RutaPrueba'='DEMO-1';'Pilotos.PruebasSoloLectura'=' true '}},
        @{Name='sesion-normal';Values=@{'Pilotos.PruebasSoloLectura'='false';'Pilotos.Habilitado'='true'}},
        @{Name='escrituras-bloqueadas';Values=@{'Pilotos.PruebasSoloLectura'='false';'Pilotos.Habilitado'='true';'Pilotos.PermitirCierre'='false'}}
    )
    foreach($case in $cases) {
        $fixture.LoadXml($baseFixture)
        foreach($key in $case.Values.Keys) {
            $setting=$fixture.SelectSingleNode("/configuration/appSettings/add[@key='$key']")
            if(!$setting) { $setting=$fixture.CreateElement('add');$setting.SetAttribute('key',$key);$fixture.configuration.appSettings.AppendChild($setting) | Out-Null }
            $setting.SetAttribute('value',$case.Values[$key])
        }
        $fixture.Save($testConfig)
        & (Join-Path $out 'PilotoTests.exe') $case.Name
        if($LASTEXITCODE -ne 0) { throw ('Fallo escenario: '+$case.Name) }
    }
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
& $compiler /nologo /target:exe "/out:$out/RazorCompile.exe" /r:System.Web.dll /r:Microsoft.CSharp.dll "/r:$MvcDll" "/r:$WebPagesDll" "/r:$pagesDir/System.Web.WebPages.Razor.dll" "/r:$pagesDir/System.Web.Helpers.dll" "/r:$RazorDll" "/r:$out/PilotoTests.exe" (Join-Path $PSScriptRoot 'RazorCompile.cs') (Join-Path $PSScriptRoot 'RazorPreview.cs')
if($LASTEXITCODE -ne 0) { throw 'Fallo compilacion del verificador Razor.' }
& (Join-Path $out 'RazorCompile.exe') $repo --render
if($LASTEXITCODE -ne 0) { throw 'Fallaron las vistas Razor.' }
$index=Get-Content -LiteralPath (Join-Path $out 'index.html') -Raw
$abiertas=@([regex]::Matches($index,'(?s)<article class="route-card route-card-pending">.*?</article>'))
if($abiertas.Count -ne 2 -or ($abiertas | Where-Object { $_.Value -match '<a\b' -or $_.Value -notmatch '<button[^>]*disabled>Ruta pendiente de salida</button>' }) -or
   ([regex]::Matches($index,'class="button-link card-link"')).Count -ne 2) {
    throw 'Las abiertas deben quedar deshabilitadas y las dos rutas E conservar el enlace.'
}
$historial=Get-Content -LiteralPath (Join-Path $out 'historial.html') -Raw
if(([regex]::Matches($historial,'class="button-link card-link"')).Count -ne 4) { throw 'El historial sin liquidar debe conservar sus enlaces de consulta.' }
Write-Host 'OK: rutas abiertas deshabilitadas; E e historial consultables.'
$detalle=Get-Content -LiteralPath (Join-Path $out 'detalle-cierre.html') -Raw
if(([regex]::Matches($detalle,'class="customer-group(?:\s|\")')).Count -ne 2 -or
   ([regex]::Matches($detalle,'class="document-card"')).Count -ne 3 -or
   ([regex]::Matches($detalle,'class="bulk-delivered"')).Count -ne 2 -or
   ([regex]::Matches($detalle,'class="save-client"')).Count -ne 2 -or
   ([regex]::Matches($detalle,'class="document-toggle')).Count -ne 3 -or
   ([regex]::Matches($detalle,'class="image-upload-trigger')).Count -ne 3 -or
   ([regex]::Matches($detalle,'class="document-image-link')).Count -ne 3 -or
   ([regex]::Matches($detalle,'class="image-card-status')).Count -ne 3 -or
   ([regex]::Matches($detalle,'class="customer-documents" hidden')).Count -ne 2 -or
   ([regex]::Matches($detalle,'class="client-toggle button-secondary" aria-expanded="false"')).Count -ne 2 -or
   $detalle -notmatch 'name="Documentos\[1\]\.RowId"') {
    throw 'El detalle renderizado no conserva grupos, botones o indices de documentos.'
}
$consulta=Get-Content -LiteralPath (Join-Path $out 'detalle-historial.html') -Raw
if($consulta -match 'class="image-upload-trigger' -or $consulta -match 'class="bulk-delivered' -or
   ([regex]::Matches($consulta,'class="customer-documents" hidden')).Count -ne 25 -or
   ([regex]::Matches($consulta,'class="client-toggle button-secondary"')).Count -ne 25) {
    throw 'El historial no debe permitir cargas ni seleccionar resultados masivos.'
}
$borrador=Get-Content -LiteralPath (Join-Path $out 'detalle-borrador.html') -Raw
if($borrador -notmatch 'data-saved="true"' -or
   ([regex]::Matches($borrador,'class="customer-documents" hidden')).Count -ne 1 -or
   $borrador -notmatch 'class="client-toggle button-secondary" aria-expanded="true"' -or
   $borrador -notmatch 'class="client-complete-badge"' -or
   $borrador -notmatch 'class="client-compact-summary"' -or
   $borrador -notmatch 'Ver foto final del cliente' -or
   $borrador -notmatch 'name="Documentos\[1\]\.Entrega" value="ENTREGADO"' -or
   ([regex]::Matches($borrador,'class="bulk-delivered"')).Count -ne 1 -or
   ([regex]::Matches($borrador,'class="save-client"')).Count -ne 1) {
    throw 'El cliente completado debe quedar resumido, bloqueado y conservar sus resultados para el cierre.'
}
Write-Host 'OK: grupos, indices y acciones del detalle renderizado.'
if(!$ScriptDomDll) { $ScriptDomDll=Join-Path $env:USERPROFILE '.nuget/packages/microsoft.sqlserver.transactsql.scriptdom/161.8901.0/lib/net462/Microsoft.SqlServer.TransactSql.ScriptDom.dll' }
if(!(Test-Path -LiteralPath $ScriptDomDll)) { throw 'Indicar -ScriptDomDll (version 161/net462) para la validacion SQL sin conexion.' }
Copy-Item -LiteralPath $ScriptDomDll -Destination $out -Force
& $compiler /nologo /target:exe "/out:$out/SqlCompile.exe" "/r:$ScriptDomDll" (Join-Path $PSScriptRoot 'SqlCompile.cs')
if($LASTEXITCODE -ne 0) { throw 'Fallo compilacion del verificador SQL.' }
$scripts=Get-ChildItem -LiteralPath (Join-Path $repo 'SqlMigrations/Pilotos') -Filter '1*.sql' | ForEach-Object FullName
$scripts=@($scripts)+(Join-Path $repo 'SqlMigrations/Pilotos/06_rutas_activas_solo_lectura.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/07_pos_instalacion_solo_lectura.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/20_borrador_cliente_pos.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/21_app_test_dos_rutas_dos_clientes.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/22_cliente_completado_foto_pos.sql')
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/23_permiso_administrar_pos.sql'
$adminSql=Join-Path $out 'autorizacion-admin.sql'
& (Join-Path $out 'PilotoTests.exe') sql-admin | Set-Content -LiteralPath $adminSql -Encoding UTF8
if($LASTEXITCODE -ne 0) { throw 'Fallo obtener la consulta real de autorizacion.' }
$scripts+=$adminSql
$rutasSql=Join-Path $out 'alcance-rutas.sql'
& (Join-Path $out 'PilotoTests.exe') sql-rutas | Set-Content -LiteralPath $rutasSql -Encoding UTF8
if($LASTEXITCODE -ne 0) { throw 'Fallo proteccion de rutas abiertas/liquidadas en los alcances reales.' }
$scripts+=$rutasSql
& (Join-Path $out 'SqlCompile.exe') $scripts
if($LASTEXITCODE -ne 0) { throw 'Fallo sintaxis SQL.' }
