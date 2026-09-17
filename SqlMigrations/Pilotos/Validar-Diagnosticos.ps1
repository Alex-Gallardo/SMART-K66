<#
Valida archivos locales. No abre conexiones ni ejecuta SQL.
Requiere Microsoft.SqlServer.TransactSql.ScriptDom (ensamblado compatible con PowerShell).
ScriptDom acepta SET LOCK_TIMEOUT con variable aunque SQL Server lo rechaza:
por eso la validacion de literales es adicional al analisis sintactico.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ScriptDomPath
)

$ErrorActionPreference = 'Stop'
Add-Type -Path $ScriptDomPath

function Assert-LockTimeoutLiteral([string]$SqlText) {
    $code = [regex]::Replace($SqlText, '(?s)/\*.*?\*/|--[^\r\n]*', '')
    foreach ($statement in [regex]::Matches($code, '(?i)\bSET\s+LOCK_TIMEOUT\s+([^;]+);')) {
        if ($statement.Groups[1].Value.Trim() -notmatch '^(3000|-1)$') {
            throw 'LOCK_TIMEOUT debe usar el literal 3000 o -1, nunca una variable.'
        }
    }
}

# Regresion: debe detectar el caso reportado en SSMS aunque ScriptDom lo acepte.
$rejected = $false
try { Assert-LockTimeoutLiteral 'SET LOCK_TIMEOUT @LockTimeoutAnterior;' }
catch { $rejected = $true }
if (-not $rejected) { throw 'La validacion no detecto la regresion de LOCK_TIMEOUT.' }

foreach ($fileName in @('00_apk66_estructura_solo_lectura.sql', '01_pos_estructura_solo_lectura.sql')) {
    $sql = Get-Content -LiteralPath (Join-Path $PSScriptRoot $fileName) -Raw
    Assert-LockTimeoutLiteral $sql
    $code = [regex]::Replace($sql, '(?s)/\*.*?\*/|--[^\r\n]*', '')
    if ($code -notmatch '(?s)IF @@LOCK_TIMEOUT <> -1\s+BEGIN.*?RETURN;\s+END;\s+BEGIN TRY\s+SET LOCK_TIMEOUT 3000;') {
        throw "$fileName no comprueba el valor de sesion antes de cambiarlo."
    }
    if ($code -notmatch '(?s)SET LOCK_TIMEOUT -1;\s+END TRY\s+BEGIN CATCH\s+SET LOCK_TIMEOUT -1;') {
        throw "$fileName no restaura el valor en las dos salidas."
    }
    if ($code -match '(?im)^\s*(INSERT|UPDATE|DELETE|MERGE|CREATE|ALTER|DROP|TRUNCATE|EXEC(?:UTE)?|GRANT|DENY|REVOKE|BACKUP|RESTORE|DBCC|USE)\b' -or
        $code -match '(?is)\bSELECT\b[^;]*\bINTO\b') {
        throw "$fileName contiene una sentencia ajena al diagnostico de solo lectura."
    }

    # Comprobar sintaxis con gramaticas SQL Server 2008 y 2022.
    foreach ($version in @(100, 160)) {
        $parser = New-Object "Microsoft.SqlServer.TransactSql.ScriptDom.TSql${version}Parser" -ArgumentList $true
        $reader = New-Object System.IO.StringReader($sql)
        try {
            $parseErrors = $null
            $null = $parser.Parse($reader, [ref]$parseErrors)
            if ($parseErrors.Count -gt 0) {
                throw (($parseErrors | ForEach-Object { "${fileName}:$($_.Line): $($_.Message)" }) -join "`n")
            }
        }
        finally { $reader.Dispose() }
    }
    Write-Output "$fileName : sintaxis y controles estaticos OK (sin ejecutar SQL)."
}
