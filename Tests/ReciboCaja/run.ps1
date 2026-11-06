$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Se requiere .NET Framework 4.x.' }

$testDir = Join-Path $env:TEMP ('ReciboCajaTests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
try {
    $exe = Join-Path $testDir 'ReciboCajaDeptoResolverTests.exe'
    & $compiler /nologo /target:exe "/out:$exe" `
        (Join-Path $project 'DiamDev.Give.BLL\ReciboCajaDeptoResolver.cs') `
        (Join-Path $project 'DiamDev.Give.BLL\ReciboCajaOperadores.cs') `
        (Join-Path $PSScriptRoot 'ReciboCajaDeptoResolverTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'No compilaron las pruebas de resolución de depto.' }
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de resolución de depto.' }
}
finally {
    $tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
    $resolvedTestDir = [IO.Path]::GetFullPath($testDir)
    if (-not $resolvedTestDir.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'La carpeta temporal de pruebas está fuera de TEMP.'
    }
    Remove-Item -LiteralPath $testDir -Recurse -Force
}
