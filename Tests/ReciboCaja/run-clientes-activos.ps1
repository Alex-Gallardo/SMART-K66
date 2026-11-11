$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$compiler = Join-Path $env:USERPROFILE '.nuget\packages\microsoft.net.compilers\3.11.0\tools\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Se requiere el compilador Roslyn 3.11.0.' }

$testDir = Join-Path $env:TEMP ('ReciboCajaClientesActivos-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
try {
    $exe = Join-Path $testDir 'ReciboCajaClientesActivosTests.exe'
    & $compiler /nologo /target:exe "/out:$exe" /r:System.Core.dll /r:System.Data.dll `
        (Join-Path $project 'DiamDev.Give.Entities\ClienteHana.cs') `
        (Join-Path $project 'DiamDev.Give.DAL\ReciboCajaClientesActivos.cs') `
        (Join-Path $PSScriptRoot 'ReciboCajaClientesActivosTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'No compilaron las pruebas de clientes activos.' }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de clientes activos.' }
}
finally {
    $tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
    $resolvedTestDir = [IO.Path]::GetFullPath($testDir)
    if (-not $resolvedTestDir.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'La carpeta temporal de pruebas está fuera de TEMP.'
    }
    Remove-Item -LiteralPath $testDir -Recurse -Force
}
