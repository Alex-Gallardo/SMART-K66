param([string]$Compiler)
$ErrorActionPreference = 'Stop'
$repoBnc = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (!$Compiler) {
    $Compiler = Join-Path $env:USERPROFILE '.nuget/packages/microsoft.net.compilers/3.11.0/tools/csc.exe'
}
if (!(Test-Path -LiteralPath $Compiler)) { throw 'Indique -Compiler con un csc Roslyn C# 6 o superior.' }
$salidaBnc = Join-Path ([IO.Path]::GetTempPath()) ('bnc-validacion-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $salidaBnc | Out-Null
$compiladasBnc = @()
foreach ($capaBnc in @('DiamDev.Give.Entities', 'DiamDev.Give.DAL', 'DiamDev.Give.BLL')) {
    $carpetaBnc = Join-Path $repoBnc $capaBnc
    [xml]$proyectoBnc = Get-Content -LiteralPath (Join-Path $carpetaBnc ($capaBnc + '.csproj')) -Raw
    $nsBnc = New-Object Xml.XmlNamespaceManager($proyectoBnc.NameTable)
    $nsBnc.AddNamespace('p', 'http://schemas.microsoft.com/developer/msbuild/2003')
    $fuentesBnc = @($proyectoBnc.SelectNodes('//p:Compile', $nsBnc) | ForEach-Object { Join-Path $carpetaBnc $_.Include })
    $referenciasBnc = @($compiladasBnc)
    foreach ($refBnc in $proyectoBnc.SelectNodes('//p:Reference', $nsBnc)) {
        $nombreBnc = ($refBnc.Include -split ',')[0]
        if ($refBnc.HintPath) {
            $rutaBnc = [IO.Path]::GetFullPath((Join-Path $carpetaBnc $refBnc.HintPath))
            if (!(Test-Path -LiteralPath $rutaBnc)) {
                # Legacy projects sometimes point to the UI bin instead of the restored package.
                $candidatasBnc = @(Get-ChildItem -LiteralPath (Join-Path $repoBnc 'packages') -Recurse -Filter ($nombreBnc + '.dll') |
                    Where-Object { $_.FullName -match '[/\\]lib[/\\]net(?:45|46\d*|40)[/\\]' } | Select-Object -First 1)
                if (!$candidatasBnc.Count) { throw "Restaurar dependencia: $nombreBnc" }
                $rutaBnc = $candidatasBnc[0].FullName
            }
            $referenciasBnc += $rutaBnc
        } else { $referenciasBnc += ($nombreBnc + '.dll') }
    }
    $dllBnc = Join-Path $salidaBnc ($capaBnc + '.dll')
    $argumentosBnc = @('/nologo', '/target:library', '/langversion:latest', "/out:$dllBnc") +
        @($referenciasBnc | Select-Object -Unique | ForEach-Object { "/r:$_" }) + $fuentesBnc
    # Response file avoids Windows' command-line limit with the legacy DAL's many migrations.
    $respuestaBnc = Join-Path $salidaBnc ($capaBnc + '.rsp')
    [IO.File]::WriteAllLines($respuestaBnc, @($argumentosBnc | ForEach-Object { '"' + $_ + '"' }))
    & $Compiler ("@" + $respuestaBnc)
    if ($LASTEXITCODE -ne 0) { throw "No compiló la capa $capaBnc" }
    $compiladasBnc += $dllBnc
    Write-Host "OK compilación: $capaBnc"
}
# Compila el controlador, modelos y filtros REALES del módulo, sin mocks de negocio.
$fuentesUiBnc = @('Controllers/BorradorNcController.cs', 'Models/BorradorNcViewModel.cs',
    'App_Start/CustomHelper.cs', 'App_Start/PermisoAttribute.cs', 'App_Start/BorradorNcPermisoAttribute.cs') |
    ForEach-Object { Join-Path $repoBnc ('DiamDev.Give.UI/' + $_) }
$referenciasUiBnc = @($compiladasBnc) + @('System.Web.dll', 'System.Configuration.dll', 'System.Data.dll',
    'System.ComponentModel.DataAnnotations.dll', 'System.Drawing.dll', 'System.Xml.dll', 'System.Core.dll',
    'System.IO.Compression.dll', 'System.IO.Compression.FileSystem.dll') + @(
    (Join-Path $repoBnc 'packages/Microsoft.AspNet.Mvc.5.2.9/lib/net45/System.Web.Mvc.dll'),
    (Join-Path $repoBnc 'packages/EPPlus.4.5.2.1/lib/net40/EPPlus.dll'))
$argumentosUiBnc = @('/nologo', '/target:library', '/langversion:latest', "/out:$salidaBnc/BorradorNc.UI.dll") +
    @($referenciasUiBnc | ForEach-Object { "/r:$_" }) + @($fuentesUiBnc)
& $Compiler $argumentosUiBnc
if ($LASTEXITCODE -ne 0) { throw 'No compiló el módulo BNC de UI.' }
Write-Host "OK compilación: controlador BNC, modelos y filtros. Salida temporal: $salidaBnc"
$argumentosTestBnc = @('/nologo', '/target:exe', "/out:$salidaBnc/SapRelacionTests.exe") +
    @($compiladasBnc | ForEach-Object { "/r:$_" }) + @((Join-Path $PSScriptRoot 'SapRelacionTests.cs'))
& $Compiler $argumentosTestBnc
if ($LASTEXITCODE -ne 0) { throw 'No compiló la prueba de relaciones SAP.' }
& (Join-Path $salidaBnc 'SapRelacionTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Falló la prueba de relaciones SAP.' }
$mvcBnc = Join-Path $repoBnc 'packages/Microsoft.AspNet.Mvc.5.2.9/lib/net45/System.Web.Mvc.dll'
$pagesBnc = Join-Path $repoBnc 'packages/Microsoft.AspNet.WebPages.3.2.9/lib/net45'
$razorBnc = Join-Path $repoBnc 'packages/Microsoft.AspNet.Razor.3.2.9/lib/net45/System.Web.Razor.dll'
Copy-Item -LiteralPath $mvcBnc -Destination $salidaBnc
Copy-Item -LiteralPath $razorBnc -Destination $salidaBnc
Get-ChildItem -LiteralPath $pagesBnc -Filter '*.dll' | Copy-Item -Destination $salidaBnc
$refsRazorBnc = @('System.Web.dll', 'Microsoft.CSharp.dll', $mvcBnc, $razorBnc,
    (Join-Path $pagesBnc 'System.Web.WebPages.dll'), (Join-Path $pagesBnc 'System.Web.WebPages.Razor.dll'))
$argsRazorBnc = @('/nologo', '/target:exe', "/out:$salidaBnc/RazorBncCompile.exe") +
    @($refsRazorBnc | ForEach-Object { "/r:$_" }) + @((Join-Path $PSScriptRoot 'RazorBncCompile.cs'))
& $Compiler $argsRazorBnc
if ($LASTEXITCODE -ne 0) { throw 'No compiló el verificador Razor.' }
& (Join-Path $salidaBnc 'RazorBncCompile.exe') $repoBnc
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación Razor de las vistas BNC.' }
