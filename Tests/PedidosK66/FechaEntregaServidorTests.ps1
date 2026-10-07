$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot '..\..\DiamDev.Give.UI\Controllers\Pedido_K66Controller.cs'
$source = Get-Content -LiteralPath $sourcePath -Raw
$matchTipo = [regex]::Match($source, 'private static bool RequiereFechaEntrega\(string tipoId, string nombreTipo\)\s*\{[^}]*\}')
$matchFecha = [regex]::Match($source, 'private static bool FechaEntregaValida\(DateTime\? fechaPrometida, DateTime fechaPedido\)\s*\{[^}]*\}')
if (-not $matchTipo.Success -or -not $matchFecha.Success) { throw 'No se encontraron las reglas de fecha del controlador.' }
$methods = $matchTipo.Value.Replace('private static', 'public static') + $matchFecha.Value.Replace('private static', 'public static')
$type = Add-Type -TypeDefinition "using System; public static class FechaEntregaPedidoK66Prueba { $methods }" -PassThru

foreach ($caso in @(
    @('P', 'Otro', $true),
    @('N1', 'Normal', $true),
    @('PG', 'Programacion', $true),
    @('PG', 'Programación', $true),
    @('T', 'Temporada', $true),
    @('X', 'Otro', $false)
)) {
    if ($type::RequiereFechaEntrega($caso[0], $caso[1]) -ne $caso[2]) {
        throw "Tipo=$($caso[0]); nombre=$($caso[1]); clasificación incorrecta."
    }
}

function Assert-Fecha([Nullable[DateTime]]$fecha, [DateTime]$hoy, [bool]$esperado) {
    $actual = $type::FechaEntregaValida($fecha, $hoy)
    if ($actual -ne $esperado) { throw "Fecha=$fecha; hoy=$hoy; esperado=$esperado; actual=$actual" }
}

$hoy = [DateTime]::new(2026, 10, 5)
Assert-Fecha $null $hoy $false
Assert-Fecha ($hoy.AddDays(-1)) $hoy $false
Assert-Fecha $hoy $hoy $true
Assert-Fecha ($hoy.AddDays(1)) $hoy $true
Assert-Fecha ($hoy.AddDays(2)) $hoy $true
Assert-Fecha ($hoy.AddDays(2).AddHours(18)) $hoy $true
Assert-Fecha ($hoy.AddDays(3)) $hoy $true
Assert-Fecha ([DateTime]::new(2026, 12, 30)) ([DateTime]::new(2026, 12, 31)) $false
Assert-Fecha ([DateTime]::new(2026, 12, 31)) ([DateTime]::new(2026, 12, 31)) $true
Assert-Fecha ([DateTime]::new(2027, 1, 1)) ([DateTime]::new(2026, 12, 31)) $true
Assert-Fecha ([DateTime]::new(2027, 1, 2)) ([DateTime]::new(2026, 12, 31)) $true
Write-Output 'FechaEntregaServidorTests: OK'
