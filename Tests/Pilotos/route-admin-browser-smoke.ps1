param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Invoke-SmokeChrome([string]$size,[string]$file) {
    $stdout=Join-Path $PSScriptRoot 'bin/route-admin-chrome-stdout.tmp'
    $stderr=Join-Path $PSScriptRoot 'bin/route-admin-chrome-stderr.tmp'
    try {
        $process=Start-Process -FilePath $ChromePath -ArgumentList @('--headless=new','--disable-gpu','--disable-extensions','--no-first-run','--no-default-browser-check',"--window-size=$size",'--virtual-time-budget=3000','--dump-dom',([Uri]$file).AbsoluteUri) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        if($process.ExitCode -ne 0) {throw ('Chrome terminó con código '+$process.ExitCode)}
        return Get-Content -LiteralPath $stdout -Raw
    } finally {Remove-Item -LiteralPath $stdout,$stderr -ErrorAction SilentlyContinue}
}
$fixture=Join-Path $PSScriptRoot 'bin/editar-ruta.html'
if(!(Test-Path -LiteralPath $fixture)) { throw 'Ejecuta Tests/Pilotos/run.ps1 para generar la vista previa.' }
if(!(Test-Path -LiteralPath $ChromePath)) { throw 'Indica la ruta de Chrome con -ChromePath.' }
$scriptUri=([Uri](Join-Path $repo 'DiamDev.Give.UI/Scripts/pilotos-admin-ruta.js')).AbsoluteUri
$fixtureHtml=Get-Content -LiteralPath $fixture -Raw
$html=$fixtureHtml
$marker='<script src="/Scripts/pilotos-admin-ruta.js"></script>'
if(!$html.Contains($marker)) { throw 'No se encontró el script de edición de rutas.' }
$probe=@'
<script src="__SCRIPT_URI__"></script>
<script>
(function(){try {
    function check(ok,message){if(!ok) throw new Error(message);}
    var form=document.getElementById('route-admin-form'),dialog=document.getElementById('route-admin-dialog');
    check(!!form && !!dialog && form.elements.RutaId.value==='DEMO-2026-001','Falta el formulario o la ruta');
    check(form.elements.Piloto.value==='EMPLEADO DEMO' && form.elements.Centro.value==='CENTRO-1' && form.elements.Placa.value==='C-001','No se cargaron todas las opciones');
    form.elements.Motivo.value='Cambio operativo de prueba';
    form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
    check(dialog.open,'No se abrió el diálogo de confirmación');
    document.getElementById('route-admin-cancel').click();
    check(!dialog.open,'Cancelar no cerró el diálogo');
    form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
    var sent=0;form.requestSubmit=function(){sent++;};
    document.getElementById('route-admin-confirm').click();
    check(sent===1 && form.dataset.confirmed==='true' && !dialog.open,'La confirmación no envía la ruta una sola vez');
    document.documentElement.setAttribute('data-route-admin-smoke','PASS');
} catch(error){document.documentElement.setAttribute('data-route-admin-smoke','FAIL: '+error.message);}})();
</script>
'@
$html=$html.Replace($marker,$probe.Replace('__SCRIPT_URI__',$scriptUri))
$smoke=Join-Path $env:TEMP ('pilotos-route-admin-'+[Guid]::NewGuid().ToString('N')+'.html')
try {
    Set-Content -LiteralPath $smoke -Value $html -Encoding UTF8
    foreach($size in @('425,900','1280,900')) {
        $output=Invoke-SmokeChrome $size $smoke
        if($output -notmatch 'data-route-admin-smoke="PASS"') {
            $failure=[regex]::Match($output,'data-route-admin-smoke="([^"]+)"')
            throw ('Edición administrativa '+$size+': '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'}))
        }
    }
    $routeProbe=@'
<script src="__SCRIPT_URI__"></script>
<script>
(function(){try{
  var button=document.querySelector('.rwe-delete-trigger'),dialog=document.getElementById('rwe-delete-dialog'),form=document.getElementById('rwe-delete-form');
  if(!button||!dialog||!form) throw new Error('Faltan controles de documentos');
  button.click();
  if(!dialog.open||form.elements.rowId.value!=='101'||!form.elements.version.value||document.getElementById('rwe-delete-number').textContent!=='10001') throw new Error('No se preparó la eliminación del detalle exacto');
  document.getElementById('rwe-delete-cancel').click();
  if(dialog.open) throw new Error('Cancelar no cerró el diálogo');
  document.documentElement.setAttribute('data-rwe-document-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-rwe-document-smoke','FAIL: '+e.message);}})();
</script>
'@
    Set-Content -LiteralPath $smoke -Value $fixtureHtml.Replace($marker,$routeProbe.Replace('__SCRIPT_URI__',$scriptUri)) -Encoding UTF8
    foreach($size in @('425,900','1280,900')) {
        $output=Invoke-SmokeChrome $size $smoke
        if($output -notmatch 'data-rwe-document-smoke="PASS"') { $failure=[regex]::Match($output,'data-rwe-document-smoke="([^"]+)"'); throw ('El diálogo de eliminación de documentos no funcionó '+$size+': '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'})) }
    }
    $documentFixture=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'bin/editar-documento.html') -Raw
    if($documentFixture -notmatch 'id="document-admin-form"') { throw 'No se encontró el formulario del documento.' }
    $documentProbe=@'
<script src="__SCRIPT_URI__"></script>
<script>
(function(){try{
  var form=document.getElementById('document-admin-form'),dialog=document.getElementById('document-admin-dialog');
  if(!form||!dialog) throw new Error('Faltan formulario o diálogo');
  form.elements.MotivoCambio.value='Corrección de prueba';
  form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
  if(!dialog.open) throw new Error('No abrió la confirmación');
  document.getElementById('document-admin-cancel').click();
  if(dialog.open) throw new Error('Cancelar no funcionó');
  form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
  var sent=0;form.requestSubmit=function(){sent++;};
  document.getElementById('document-admin-confirm').click();
  if(sent!==1||dialog.open) throw new Error('No confirmó una sola vez');
  document.documentElement.setAttribute('data-rwe-edit-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-rwe-edit-smoke','FAIL: '+e.message);}})();
</script>
'@
    $injected=$documentProbe.Replace('__SCRIPT_URI__',$scriptUri)
    $documentHtml=if($documentFixture.Contains($marker)){$documentFixture.Replace($marker,$injected)}else{$documentFixture.Replace('</body>',$injected+'</body>')}
    Set-Content -LiteralPath $smoke -Value $documentHtml -Encoding UTF8
    foreach($size in @('425,900','1280,900')) {
        $output=Invoke-SmokeChrome $size $smoke
        if($output -notmatch 'data-rwe-edit-smoke="PASS"') { throw 'El diálogo de edición documental no funcionó: '+$size }
    }
    $config=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'bin/configurar.html') -Raw
    $configMarker='<script src="/Scripts/pilotos.js"></script>'
    $configScript=([Uri](Join-Path $repo 'DiamDev.Give.UI/Scripts/pilotos.js')).AbsoluteUri
    $configProbe=@'
<script src="__SCRIPT_URI__"></script>
<script>
(function(){try {
    var form=document.getElementById('pilot-admin-form'),dialog=document.getElementById('pilot-admin-dialog');
    if(!form || !dialog) throw new Error('Falta el formulario o diálogo del piloto');
    form.elements.Motivo.value='Actualización de prueba';
    form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
    if(!dialog.open) throw new Error('No abrió la confirmación del piloto');
    document.getElementById('pilot-admin-cancel').click();
    if(dialog.open) throw new Error('Cancelar no cerró la confirmación');
    form.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
    var sent=0;form.requestSubmit=function(){sent++;};
    document.getElementById('pilot-admin-confirm').click();
    if(sent!==1 || form.dataset.confirmed!=='true' || dialog.open) throw new Error('No confirmó una sola vez');
    document.documentElement.setAttribute('data-config-smoke','PASS');
} catch(error){document.documentElement.setAttribute('data-config-smoke','FAIL: '+error.message);}})();
</script>
'@
    if(!$config.Contains($configMarker)) { throw 'No se encontró el script del formulario de piloto.' }
    $config=$config.Replace($configMarker,$configProbe.Replace('__SCRIPT_URI__',$configScript))
    Set-Content -LiteralPath $smoke -Value $config -Encoding UTF8
    $output=Invoke-SmokeChrome '425,900' $smoke
    if($output -notmatch 'data-config-smoke="PASS"') {
        $failure=[regex]::Match($output,'data-config-smoke="([^"]+)"')
        throw ('Configuración de piloto: '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'}))
    }
    $companyConfig=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'bin/configurar-empresa.html') -Raw
    $companyProbe=@'
<script src="__SCRIPT_URI__"></script>
<script>
(function(){try {
    var form=document.getElementById('pilot-admin-form');
    var company=form.querySelector('input[name=CuentaEmpresa][type=checkbox]');
    var employee=form.querySelector('#EmpleadoRowId');
    var centers=form.querySelector('#individual-pilot-centers');
    if(!company.checked || !employee.disabled || employee.required || !centers.hidden) throw new Error('La empresa aún exige empleado o centro');
    if(document.documentElement.scrollWidth>innerWidth+1) throw new Error('Desbordamiento horizontal');
    company.click();
    if(employee.disabled || !employee.required || centers.hidden) throw new Error('El piloto individual no recupera sus campos');
    company.click();
    if(!employee.disabled || !centers.hidden) throw new Error('La empresa no recupera sus campos');
    document.documentElement.setAttribute('data-company-config-smoke','PASS');
} catch(error){document.documentElement.setAttribute('data-company-config-smoke','FAIL: '+error.message);}})();
</script>
'@
    if(!$companyConfig.Contains($configMarker)) { throw 'No se encontró el script de configuración empresarial.' }
    $companyConfig=$companyConfig.Replace($configMarker,$companyProbe.Replace('__SCRIPT_URI__',$configScript))
    Set-Content -LiteralPath $smoke -Value $companyConfig -Encoding UTF8
    foreach($size in @('425,900','1280,900')) {
        $output=Invoke-SmokeChrome $size $smoke
        if($output -notmatch 'data-company-config-smoke="PASS"') {
            $failure=[regex]::Match($output,'data-company-config-smoke="([^"]+)"')
            throw ('Configuración empresarial '+$size+': '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'}))
        }
    }
    Write-Host 'OK Chrome: confirmación de edición de rutas y configuración de piloto.'
} finally { Remove-Item -LiteralPath $smoke -Force -ErrorAction SilentlyContinue }
