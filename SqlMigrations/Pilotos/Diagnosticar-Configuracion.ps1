param([Parameter(Mandatory=$true)][string]$WebConfig,[string]$Usuario)
# Solo lee el archivo indicado. No abre conexiones, cambia opciones ni imprime secretos.
$ErrorActionPreference='Stop'
$pilotPath=(Resolve-Path -LiteralPath $WebConfig).Path
$pilotXml=New-Object System.Xml.XmlDocument
try { $pilotXml.Load($pilotPath) } catch { throw 'No se pudo leer el XML. Revisa el archivo localmente; no compartas su contenido completo.' }
$pilotApp=$pilotXml.SelectSingleNode('/configuration/appSettings')
if(!$pilotApp) { throw 'No existe appSettings en el archivo indicado.' }
if($pilotApp.GetAttribute('configSource') -or $pilotApp.GetAttribute('file')) {
    throw 'appSettings usa un archivo externo. Revisa la configuracion efectiva de IIS antes de continuar.'
}
$pilotSettings=@{}
foreach($pilotNode in $pilotApp.SelectNodes('add')) {
    $pilotKey=$pilotNode.GetAttribute('key')
    if($pilotKey -like 'Pilotos.*') {
        if($pilotSettings.ContainsKey($pilotKey)) { throw 'Hay claves Pilotos duplicadas. Corrigelas en el archivo local.' }
        $pilotSettings[$pilotKey]=$pilotNode.GetAttribute('value').Trim()
    }
}
$pilotReadOnly=$pilotSettings['Pilotos.PruebasSoloLectura'] -eq 'true'
$pilotPlate=[string]$pilotSettings['Pilotos.PlacaPrueba']
$pilotRoute=[string]$pilotSettings['Pilotos.RutaPrueba']
$pilotConnection=$pilotXml.SelectSingleNode('/configuration/connectionStrings/add[@name="GiveContext"]')
[pscustomobject]@{
    ArchivoRevisado=$pilotPath
    ModuloNormal=($pilotSettings['Pilotos.Habilitado'] -eq 'true')
    ConsultaTemporal=$pilotReadOnly
    CierreEfectivo=(!$pilotReadOnly -and $pilotSettings['Pilotos.Habilitado'] -eq 'true' -and $pilotSettings['Pilotos.PermitirCierre'] -eq 'true')
    UsuarioPruebaConfigurado=![string]::IsNullOrWhiteSpace($pilotSettings['Pilotos.UsuarioPrueba'])
    UsuarioCoincide=if($Usuario){$Usuario -eq $pilotSettings['Pilotos.UsuarioPrueba']}else{$null}
    AlcanceTemporalValido=(($pilotPlate.Length -gt 0) -xor ($pilotRoute.Length -gt 0)) -and $pilotPlate.Length -le 15 -and $pilotRoute.Length -le 15
    CatalogoExplicitoConfigurado=![string]::IsNullOrWhiteSpace($pilotSettings['Pilotos.CatalogoRutas'])
    ConexionPosPresente=($null -ne $pilotConnection)
    Nota='Lectura de archivo; no demuestra que IIS lo haya cargado ni valida acceso SQL.'
}
