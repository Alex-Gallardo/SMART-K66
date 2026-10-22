param([string]$MvcDll, [string]$WebPagesDll, [string]$RazorDll, [string]$ScriptDomDll)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$project=Get-Content -LiteralPath (Join-Path $repo 'DiamDev.Give.UI/DiamDev.Give.UI.csproj') -Raw
$garitaDA=Get-Content -LiteralPath (Join-Path $repo 'DiamDev.Give.DAL/GaritaRutaDA.cs') -Raw
$empresaDA=Get-Content -LiteralPath (Join-Path $repo 'DiamDev.Give.DAL/PilotoAdministracionRutaDA.cs') -Raw
$rutasDA=Get-Content -LiteralPath (Join-Path $repo 'DiamDev.Give.DAL/PilotoRutaDA.cs') -Raw
if($empresaDA -notmatch 'DELETE dbo\.PilotoConductorRuta WHERE CatalogoRutas=@catalogo AND Ruta_Id=@id' -or
   $empresaDA -notmatch 'PilotoDocumentoImagen WHERE Ruta_Id=@ruta' -or
   $rutasDA -match 'ExigirConductor\(ruta\)' -or $rutasDA -notmatch 'a\.Empresa \? " AND r\.PLACA=@placaEmpresa"' -or
   $rutasDA -notmatch 'HuellaCierreGuardado' -or $rutasDA -notmatch 'ResultadosGuardados\(ruta\)') {
    throw 'Las rutas empresariales deben operar por placa y reconstruir el cierre grande desde borradores guardados.'
}
if($garitaDA -match 'UPDATE\s+[^;]*RT_RUTAS' -or $garitaDA -match 'SqlConfirmarEstado' -or $garitaDA -notmatch 'Cmd\(cn,tx,SqlRegistrarSalida\)') {
    throw 'Garita debe insertar el evento de salida sin actualizar el estado de RT_RUTAS.'
}
if($garitaDA -notmatch 'r\.PLACA=@placa AND r\.STATUS=N''E''' -or $garitaDA -notmatch 'GaritaCentro g WHERE g.Usuario_Id=@usuario' -or
   $garitaDA -notmatch 'INSERT dbo.GaritaSalidaImagen' -or $garitaDA -notmatch 'evento=Convert\.ToInt64\(c\.ExecuteScalar\(\)\)') {
    throw 'La búsqueda debe limitar placa/estado/centro y la foto debe vincularse al evento de salida.'
}
if($project -notmatch 'Content\\pilotos-panel-redesign\.css') { throw 'La hoja de estilos del panel debe incluirse en la publicación web.' }
$seguridad=Get-Content -LiteralPath (Join-Path $repo 'DiamDev.Give.UI/Controllers/SeguridadController.cs') -Raw
if($seguridad -notmatch 'UsuarioTieneRol\(UsuarioActual\.Login, "GARITA"\)' -or $seguridad -notmatch 'return IniciarGarita\(UsuarioActual\)' -or
   $seguridad -notmatch 'Session\["Garita\.Desafio"\]' -or $seguridad -notmatch 'return RedirectToAction\("Index","Garita"\)') {
    throw 'El login de GARITA debe redirigir a su vista y validar el desafío antes de emitir la sesión.'
}
$usuarioBL=Get-Content -LiteralPath (Join-Path $repo 'DiamDev.Give.BLL/UsuarioBL.cs') -Raw
$rolBL=Get-Content -LiteralPath (Join-Path $repo 'DiamDev.Give.BLL/RolBL.cs') -Raw
if($usuarioBL -notmatch 'UsuarioRolPlan\.Crear\(' -or $usuarioBL -match 'db\.Set<UsuarioRol>\(\)\.RemoveRange\(Roles\)' -or
   $rolBL -match 'db\.Set<RolPermiso>\(\)\.RemoveRange\(Permisos\)' -or $rolBL -notmatch 'solicitados\.Where\(x => !existentes\.Contains\(x\)\)') {
    throw 'La edición debe conservar roles y permisos existentes y agregar solo los faltantes.'
}
$adminViews=@('Administracion','Configurar','EditarRuta','AdministracionError','Panel','PanelDetalle','PanelError')
foreach($name in $adminViews) {
    $view=Get-Content -LiteralPath (Join-Path $repo ('DiamDev.Give.UI/Views/Piloto/'+$name+'.cshtml')) -Raw
    if($view -notmatch 'Layout\s*=\s*"~/Views/Shared/_Layout\.cshtml"' -or $view -notmatch 'ViewBag\.PilotoBackoffice\s*=\s*true') {
        throw ('La vista administrativa debe usar el layout principal: '+$name)
    }
}
foreach($name in @('Index','Detalle','Error')) {
    $view=Get-Content -LiteralPath (Join-Path $repo ('DiamDev.Give.UI/Views/Piloto/'+$name+'.cshtml')) -Raw
    if($view -notmatch 'Layout\s*=\s*"~/Views/Shared/_PilotoLayout\.cshtml"') { throw ('El portal piloto debe conservar su layout: '+$name) }
}
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
$sources=@('DiamDev.Give.Entities/PilotoRuta.cs','DiamDev.Give.Entities/PilotoReglas.cs','DiamDev.Give.Entities/PilotoDesafio.cs','DiamDev.Give.Entities/PilotoAdministracion.cs','DiamDev.Give.DAL/PilotoRutaDA.cs','DiamDev.Give.DAL/PilotoAdministracionDA.cs','DiamDev.Give.DAL/PilotoAdministracionRutaDA.cs','DiamDev.Give.BLL/PilotoRutaBL.cs','DiamDev.Give.BLL/PilotoAdministracionBL.cs','DiamDev.Give.UI/Controllers/PilotoController.cs','Tests/Pilotos/PilotoTests.cs') | ForEach-Object { Join-Path $repo $_ }
$sources+=@('DiamDev.Give.Entities/PilotoPanel.cs','DiamDev.Give.DAL/PilotoPanelDA.cs','DiamDev.Give.BLL/PilotoPanelBL.cs','Tests/Pilotos/PanelTests.cs') | ForEach-Object { Join-Path $repo $_ }
$sources+=@('DiamDev.Give.Entities/GaritaRuta.cs','DiamDev.Give.DAL/GaritaRutaDA.cs','DiamDev.Give.UI/Controllers/GaritaController.cs','Tests/Pilotos/GaritaTests.cs') | ForEach-Object { Join-Path $repo $_ }
$sources+=Join-Path $repo 'DiamDev.Give.Entities/UsuarioRolPlan.cs'
& $compiler /nologo /target:exe "/out:$out/PilotoTests.exe" /r:System.Configuration.dll /r:System.Data.dll /r:System.Xml.Linq.dll /r:System.Xml.dll /r:System.Web.dll /r:System.ComponentModel.DataAnnotations.dll "/r:$MvcDll" $sources
if($LASTEXITCODE -ne 0) { throw 'Fallo compilacion aislada del modulo.' }
Copy-Item -LiteralPath $MvcDll -Destination $out -Force
Copy-Item -LiteralPath $WebPagesDll -Destination $out -Force
& (Join-Path $out 'PilotoTests.exe')
if($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas.' }
& (Join-Path $out 'PilotoTests.exe') panel
if($LASTEXITCODE -ne 0) { throw 'Fallaron las reglas del panel.' }
& (Join-Path $out 'PilotoTests.exe') garita
if($LASTEXITCODE -ne 0) { throw 'Fallaron las reglas de garita.' }
& (Join-Path $out 'PilotoTests.exe') empresa
if($LASTEXITCODE -ne 0) { throw 'Fallaron las reglas de transportistas empresariales.' }
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
$empresaSinPlaca=Get-Content -LiteralPath (Join-Path $out 'index-empresa-sin-placa.html') -Raw
$empresaConPlaca=Get-Content -LiteralPath (Join-Path $out 'index-empresa.html') -Raw
if(([regex]::Matches($empresaSinPlaca,'class="company-plate-option"')).Count -ne 2 -or
   $empresaSinPlaca -notmatch 'Selecciona la placa' -or $empresaSinPlaca -match 'EMP-001' -or
   $empresaConPlaca -notmatch 'EMP-001' -or $empresaConPlaca -notmatch 'JUAN PEREZ' -or
   $empresaConPlaca -notmatch 'Viendo rutas de' -or $empresaConPlaca -notmatch 'Cambiar placa') {
    throw 'La empresa debe escoger una placa antes de ver rutas y conservar el conductor asignado.'
}
$empresaAdmin=Get-Content -LiteralPath (Join-Path $out 'editar-ruta-empresa.html') -Raw
if($empresaAdmin -notmatch 'name="EmpresaUsuarioId" value="22"' -or $empresaAdmin -notmatch 'name="Nombre"' -or $empresaAdmin -notmatch 'name="Documento"' -or
   $empresaAdmin -notmatch 'AsignarConductor' -or $empresaAdmin -notmatch 'JUAN PEREZ') {
    throw 'Distribución debe poder asignar el conductor real de la ruta empresarial.'
}
Write-Host 'OK: selector de placas y asignación de conductor empresarial renderizados.'
$empresaLista=Get-Content -LiteralPath (Join-Path $out 'detalle-empresa-lista.html') -Raw
$empresaPendiente=Get-Content -LiteralPath (Join-Path $out 'detalle-empresa-pendiente.html') -Raw
$empresaSinPermiso=Get-Content -LiteralPath (Join-Path $out 'detalle-empresa-sin-permiso.html') -Raw
$empresaGrande=Get-Content -LiteralPath (Join-Path $out 'detalle-empresa-grande.html') -Raw
$panelEmpresa=Get-Content -LiteralPath (Join-Path $out 'panel-empresa-pendiente.html') -Raw
$panelDetalleEmpresa=Get-Content -LiteralPath (Join-Path $out 'panel-detalle-empresa-pendiente.html') -Raw
if($empresaLista -notmatch 'class="save-client"' -or $empresaLista -notmatch 'id="complete-button"' -or
   $empresaLista -notmatch 'class="image-upload-trigger' -or $empresaLista -match 'Conductor pendiente' -or
   $empresaPendiente -match 'Conductor pendiente' -or $empresaPendiente -notmatch 'class="save-client"|id="complete-button"' -or
   $empresaPendiente -match 'Ruta disponible para consulta. Para cambios' -or
   $empresaSinPermiso -notmatch 'Pilotos.Rutas.Confirmar' -or $empresaSinPermiso -match 'class="save-client"|id="complete-button"' -or
   $panelEmpresa -match 'para habilitar las entregas' -or $panelDetalleEmpresa -match 'aún no puede registrar entregas') {
    throw 'La empresa debe operar por placa, incluso sin conductor adicional; la falta de permiso sí bloquea.'
}
Write-Host 'OK: ruta empresarial asignada editable y rutas pendientes con acceso directo a Distribución.'
if($empresaGrande -notmatch 'name="DesdeBorradores" value="true"' -or $empresaGrande -notmatch 'BLOQUE 11 DE 11' -or
   $empresaGrande -notmatch '10 de 11 bloques guardados' -or $empresaGrande -notmatch 'Guardar bloque' -or
   $empresaGrande -notmatch 'Confirmar y cerrar ruta' -or $empresaGrande -match 'Conductor pendiente|cierre de esta ruta corresponde a Distribuci') {
    throw 'La ruta empresarial grande debe mostrar bloques paginados y mantener el cierre disponible.'
}
$configEmpresa=Get-Content -LiteralPath (Join-Path $out 'configurar-empresa.html') -Raw
if($configEmpresa -notmatch 'TRANSPORTE DEMO' -or $configEmpresa -notmatch 'id="individual-pilot-identity" hidden' -or
   $configEmpresa -notmatch 'id="individual-pilot-centers" hidden' -or $configEmpresa -notmatch 'id="EmpleadoRowId" name="EmpleadoRowId" disabled' -or
   $configEmpresa -notmatch 'name="Placas" value="C-001"') {
    throw 'La configuración empresarial debe omitir empleado y centros y conservar sus placas.'
}
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
$pagina1=Get-Content -LiteralPath (Join-Path $out 'panel-pagina-1.html') -Raw
$pagina2=Get-Content -LiteralPath (Join-Path $out 'panel-pagina-2.html') -Raw
if(([regex]::Matches($pagina1,'<td data-label="Ruta">')).Count -ne 50 -or
   ([regex]::Matches($pagina2,'<td data-label="Ruta">')).Count -ne 1 -or
   $pagina1 -notmatch 'de 51 resultados' -or $pagina2 -notmatch 'de 51 resultados') {
    throw 'La tabla de rutas debe paginar 51 registros como 50 + 1 conservando el total.'
}
Write-Host 'OK: paginación de 50 resultados con total de 51.'
$detallePanel=Get-Content -LiteralPath (Join-Path $out 'panel-detalle.html') -Raw
if(([regex]::Matches($detallePanel,'class="dist-doc ')).Count -ne 3 -or
   $detallePanel -notmatch 'DOC-DEMO-001' -or $detallePanel -notmatch 'DOC-DEMO-002' -or $detallePanel -notmatch 'DOC-DEMO-003' -or
   $detallePanel -notmatch 'Observación POS pendiente:' -or $detallePanel -notmatch 'Hora de entrada' -or
   $detallePanel -notmatch 'Observación general' -or $detallePanel -notmatch 'Última actividad POS' -or
   $detallePanel -notmatch 'Volver al panel con mis filtros' -or $detallePanel -notmatch 'Ver foto de garita') {
    throw 'El detalle debe mostrar todas las facturas y los datos operativos sin perder navegación.'
}
Write-Host 'OK: detalle completo de clientes, facturas y datos de ruta.'
$garitaLista=Get-Content -LiteralPath (Join-Path $out 'garita-lista.html') -Raw
$garitaDiferencia=Get-Content -LiteralPath (Join-Path $out 'garita-diferencia.html') -Raw
$garitaSalida=Get-Content -LiteralPath (Join-Path $out 'garita-salida.html') -Raw
$garitaEnRuta=Get-Content -LiteralPath (Join-Path $out 'garita-en-ruta.html') -Raw
$garitaVacia=Get-Content -LiteralPath (Join-Path $out 'garita-vacia.html') -Raw
$garitaSinResultados=Get-Content -LiteralPath (Join-Path $out 'garita-sin-resultados.html') -Raw
$garitaEmpresaPendiente=Get-Content -LiteralPath (Join-Path $out 'garita-empresa-pendiente.html') -Raw
$garitaEmpresaAsignada=Get-Content -LiteralPath (Join-Path $out 'garita-empresa-asignada.html') -Raw
if($garitaEmpresaPendiente -match 'Conductor pendiente' -or $garitaEmpresaPendiente -notmatch 'id="register-exit-form"' -or
   $garitaEmpresaAsignada -notmatch 'JUAN PEREZ' -or $garitaEmpresaAsignada -notmatch 'L-1234' -or $garitaEmpresaAsignada -notmatch 'id="register-exit-form"') {
    throw 'Garita debe permitir verificar la salida con el piloto de APK66 y mostrar la asignación opcional.'
}
if($garitaVacia -notmatch 'name="placa"' -or $garitaVacia -match 'class="route-option"' -or $garitaVacia -match 'class="route-header"') {throw 'Garita debe mostrar rutas solo después de buscar una placa.'}
if($garitaLista -notmatch 'PP26-GARITA' -or $garitaLista -notmatch 'PP26-OTRA' -or $garitaLista -notmatch 'OTRO PILOTO' -or $garitaLista -notmatch 'Siguiente' -or $garitaLista -match 'id="register-exit-form"') {throw 'La búsqueda debe listar todas las rutas E de la placa y paginarlas.'}
if($garitaSinResultados -notmatch 'No hay rutas En ruta' -or $garitaSinResultados -match 'class="route-option"') {throw 'La placa sin rutas debe mostrar un mensaje claro.'}
if($garitaDiferencia -notmatch 'Faltan dos bultos' -or $garitaDiferencia -match 'id="register-exit-form"') {throw 'La diferencia debe bloquear el registro de salida.'}
if($garitaEnRuta -notmatch 'En ruta' -or $garitaEnRuta -notmatch 'id="register-exit-form"' -or $garitaEnRuta -notmatch 'name="CargaVerificada"' -or $garitaEnRuta -notmatch 'name="foto"' -or $garitaEnRuta -notmatch 'multipart/form-data' -or $garitaEnRuta -match 'PilotoVehiculoVerificados|name="Precinto"' -or $garitaEnRuta -notmatch 'Registrar hora de salida') {throw 'La ruta E debe mostrar un único check y la foto opcional.'}
if($garitaSalida -notmatch 'GUARDIA' -or $garitaSalida -notmatch 'Hora de salida:' -or $garitaSalida -notmatch 'Ver foto de salida' -or $garitaSalida -match 'id="register-exit-form"') {throw 'La salida registrada debe mostrar hora, agente y foto sin permitir otra salida.'}
$garitaAdmin=Get-Content -LiteralPath (Join-Path $out 'garita-admin.html') -Raw
if($garitaAdmin -notmatch 'Faltan dos bultos' -or $garitaAdmin -notmatch 'name="asignar"' -or $garitaAdmin -notmatch 'ResolverDiferencia') {throw 'La administración debe mostrar incidentes y permitir centros y resolución.'}
Write-Host 'OK: búsqueda, carga, bloqueo por diferencia y salida registrada en la vista de garita.'
if(Test-Path -LiteralPath 'C:\Program Files\Google\Chrome\Application\chrome.exe') {& (Join-Path $PSScriptRoot 'garita-browser-smoke.ps1'); if($LASTEXITCODE -ne 0) {throw 'Falló la prueba de navegador de garita.'}}
if(Test-Path -LiteralPath 'C:\Program Files\Google\Chrome\Application\chrome.exe') {& (Join-Path $PSScriptRoot 'empresa-browser-smoke.ps1'); if($LASTEXITCODE -ne 0) {throw 'Falló la prueba de navegador de transportistas.'}}
if(Test-Path -LiteralPath 'C:\Program Files\Google\Chrome\Application\chrome.exe') {& (Join-Path $PSScriptRoot 'route-admin-browser-smoke.ps1'); if($LASTEXITCODE -ne 0) {throw 'Falló la prueba de formularios administrativos.'}}
if(!$ScriptDomDll) { $ScriptDomDll=Join-Path $env:USERPROFILE '.nuget/packages/microsoft.sqlserver.transactsql.scriptdom/161.8901.0/lib/net462/Microsoft.SqlServer.TransactSql.ScriptDom.dll' }
if(!(Test-Path -LiteralPath $ScriptDomDll)) { throw 'Indicar -ScriptDomDll (version 161/net462) para la validacion SQL sin conexion.' }
Copy-Item -LiteralPath $ScriptDomDll -Destination $out -Force
& $compiler /nologo /target:exe "/out:$out/SqlCompile.exe" "/r:$ScriptDomDll" (Join-Path $PSScriptRoot 'SqlCompile.cs')
if($LASTEXITCODE -ne 0) { throw 'Fallo compilacion del verificador SQL.' }
$scripts=Get-ChildItem -LiteralPath (Join-Path $repo 'SqlMigrations/Pilotos') -Filter '1*.sql' | ForEach-Object FullName
$scripts=@($scripts)+(Join-Path $repo 'SqlMigrations/Pilotos/06_rutas_activas_solo_lectura.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/07_pos_instalacion_solo_lectura.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/20_borrador_cliente_pos.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/21_app_test_dos_rutas_dos_clientes.sql')+(Join-Path $repo 'SqlMigrations/Pilotos/22_cliente_completado_foto_pos.sql')
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/23_permiso_administrar_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/24_permiso_monitorear_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/25_eventos_cliente_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/26_historial_edicion_rutas_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/27_garita_salida_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/28_garita_reparar_acceso_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/29_garita_foto_salida_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/30_transportistas_empresa_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/31_empresa_placas_pos.sql'
$scripts+=Join-Path $repo 'SqlMigrations/Pilotos/32_indice_rutas_placa_apk66.sql'
$adminSql=Join-Path $out 'autorizacion-admin.sql'
& (Join-Path $out 'PilotoTests.exe') sql-admin | Set-Content -LiteralPath $adminSql -Encoding UTF8
if($LASTEXITCODE -ne 0) { throw 'Fallo obtener la consulta real de autorizacion.' }
$scripts+=$adminSql
$rutasSql=Join-Path $out 'alcance-rutas.sql'
& (Join-Path $out 'PilotoTests.exe') sql-rutas | Set-Content -LiteralPath $rutasSql -Encoding UTF8
if($LASTEXITCODE -ne 0) { throw 'Fallo proteccion de rutas abiertas/liquidadas en los alcances reales.' }
$scripts+=$rutasSql
$panelSql=Join-Path $out 'panel-consultas.sql'
& (Join-Path $out 'PilotoTests.exe') sql-panel | Set-Content -LiteralPath $panelSql -Encoding UTF8
if($LASTEXITCODE -ne 0) { throw 'Fallo obtener consultas reales del panel.' }
$scripts+=$panelSql
$garitaSql=Join-Path $out 'transicion-garita.sql'
& (Join-Path $out 'PilotoTests.exe') sql-garita | Set-Content -LiteralPath $garitaSql -Encoding UTF8
if($LASTEXITCODE -ne 0) { throw 'Fallo protección de la transición real de garita.' }
$scripts+=$garitaSql
& (Join-Path $out 'SqlCompile.exe') $scripts
if($LASTEXITCODE -ne 0) { throw 'Fallo sintaxis SQL.' }
