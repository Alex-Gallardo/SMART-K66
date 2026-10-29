param([Parameter(Mandatory = $true)][string]$ConfigPath)

# Ejecute en el servidor de la aplicación con acceso a SAP HANA y SQL Server.
# Solo realiza SELECT y devuelve nombres de columnas faltantes y conteos; no imprime credenciales ni registros.
$ErrorActionPreference = 'Stop'
function SafeError($errorRecord) {
    $ex = $errorRecord.Exception
    while ($ex.InnerException) { $ex = $ex.InnerException }
    $message = $ex.Message -replace '(?i)(PWD|Password|UID|User ID)\s*=\s*[^;\s]+', '$1=[OCULTO]'
    $message = ($message -replace '[\r\n]+', ' ').Trim()
    if ($message.Length -gt 220) { $message = $message.Substring(0,220) }
    return ($ex.GetType().Name + ': ' + $message)
}
$config = [xml](Get-Content -LiteralPath $ConfigPath -Raw)
$settings = @{}
foreach ($entry in $config.configuration.appSettings.add) { $settings[$entry.key] = $entry.value }
$sqlConfig = $config.configuration.connectionStrings.add | Where-Object { $_.name -eq 'GiveContext' } | Select-Object -First 1
if (-not $sqlConfig) { throw 'No existe GiveContext en el archivo indicado.' }

$requiredSql = @{
    Cliente = @('Cliente_Id','Empresa_Id','Nit','Nombre','Direccion','No_Telefono','Email_Cliente','Descuento','Vip','Activo','Dias_Credito','Limite_Credito','Nombre_Contacto','Telefono_Contacto','Celular_Contacto','Correo_Contacto','Fecha')
    Producto = @('Producto_Id','Empresa_Id','Codigo','Nombre','Descripcion','Minimo','Maximo','Cantidad','Tiene_Identificador','Tiene_Envase','Cantidad_Envase','Tiene_Lote','Activo','Fecha')
    Vendedor = @('Vendedor_Id','Empresa_Id','Nombre','Activo')
    Pedido_K66 = @('Pedido_Id','Empresa_Id','Fecha','Nombre','ID_K66','Sincronizado','Moneda','Tipo_Pedido_Id','Estado_Id','Nit','Direccion','Direccion_Entrega','Orden_Compra_Cliente','Termino_Entrega','Vendedor','Fecha_Hora_Pedido','Fecha_Hora_Sincronizacion','Fecha_Prometida')
    Pedido_Detalle_K66 = @('Pedido_Id','Detalle_Id','Producto_Id','WarehouseId','Nombre','Unidad','Cantidad','Precio_Original','Precio','Descuento')
}
$sqlBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($sqlConfig.connectionString)
if ($sqlBuilder.InitialCatalog -ne 'POS-SmartK66_DEV') { throw 'GiveContext no apunta a POS-SmartK66_DEV.' }
$sqlBuilder.ApplicationIntent = [System.Data.SqlClient.ApplicationIntent]::ReadOnly
try {
    $cn = New-Object System.Data.SqlClient.SqlConnection($sqlBuilder.ConnectionString)
    $cn.Open()
    Write-Output 'SQL: CONECTADO a POS-SmartK66_DEV'
    foreach ($table in ($requiredSql.Keys | Sort-Object)) {
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = 'SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=''dbo'' AND TABLE_NAME=@tabla'
        [void]$cmd.Parameters.Add('@tabla',[System.Data.SqlDbType]::NVarChar,128)
        $cmd.Parameters['@tabla'].Value = $table
        $columns = @()
        $reader = $cmd.ExecuteReader()
        while ($reader.Read()) { $columns += $reader.GetString(0) }
        $reader.Close()
        $missing = @($requiredSql[$table] | Where-Object { $columns -notcontains $_ })
        Write-Output ("SQL {0}: {1}" -f $table, $(if ($columns.Count -eq 0) { 'NO DISPONIBLE' } elseif ($missing.Count -gt 0) { 'FALTAN ' + ($missing -join ',') } else { 'OK' }))
    }
    $cn.Close()
} catch { Write-Output ('SQL: ERROR ' + (SafeError $_)) }

$requiredHana = @{
    OINV = @('DocEntry','DocNum','DocDate','DocCur','DocTotal','DocTotalFC','VatSum','VatSumFC','CANCELED','CardCode','CardName','SlpCode')
    ORIN = @('DocEntry','DocNum','DocDate','DocCur','DocTotal','DocTotalFC','VatSum','VatSumFC','CANCELED','CardCode','CardName','SlpCode')
    ORDR = @('DocEntry','DocNum','DocDate','DocCur','DocTotal','DocTotalFC','CANCELED','DocStatus','CardCode','CardName','SlpCode')
    OCRD = @('CardCode','CardName','CardType','Currency','validFor','SlpCode','LicTradNum')
    OITM = @('ItemCode','ItemName','validFor','InvntryUom','OnHand','IsCommited','ItmsGrpCod')
    OITB = @('ItmsGrpCod','ItmsGrpNam')
    OSLP = @('SlpCode','SlpName','Active')
    ORCT = @('DocDate','DocCurr','DocTotal','DocTotalFC','Canceled')
    INV1 = @('DocEntry','ItemCode','Quantity','Dscription')
    RIN1 = @('DocEntry','ItemCode','Quantity','Dscription')
    RDR1 = @('DocEntry')
    CRD1 = @('CardCode')
    OITW = @('ItemCode')
    OCPR = @('CardCode')
    ITM1 = @('ItemCode')
}
$odbc = New-Object System.Data.Odbc.OdbcConnectionStringBuilder
$odbc['Driver'] = $settings['HANA_Driver']
$odbc['SERVERNODE'] = $settings['HANA_Server']
$odbc['DATABASENAME'] = $settings['HANA_TenantDB']
$odbc['UID'] = $settings['HANA_User']
$odbc['PWD'] = $settings['HANA_Password']
try {
    $hana = New-Object System.Data.Odbc.OdbcConnection($odbc.ConnectionString)
    $hana.Open()
    Write-Output 'SAP HANA: CONECTADO'
    foreach ($company in @('BOLIK','FAES','GRACO')) {
        $schema = $settings['HanaSchema.' + $company]
        if ($schema -notmatch '^[A-Za-z0-9_]+$') { Write-Output ("SAP {0}: SCHEMA NO CONFIGURADO" -f $company); continue }
        foreach ($table in ($requiredHana.Keys | Sort-Object)) {
            try {
                $cmd = $hana.CreateCommand()
                $cmd.CommandText = 'SELECT * FROM "' + $schema + '"."' + $table + '" WHERE 1=0'
                $reader = $cmd.ExecuteReader()
                $columns = @()
                for ($i=0; $i -lt $reader.FieldCount; $i++) { $columns += $reader.GetName($i) }
                $reader.Close()
                $missing = @($requiredHana[$table] | Where-Object { $columns -notcontains $_ })
                Write-Output ("SAP {0} {1}: {2}" -f $company, $table, $(if ($missing.Count -gt 0) { 'FALTAN ' + ($missing -join ',') } else { 'OK' }))
            } catch { Write-Output ("SAP {0} {1}: ERROR {2}" -f $company, $table, (SafeError $_)) }
        }
    }
    $hana.Close()
} catch { Write-Output ('SAP HANA: ERROR ' + (SafeError $_)) }
