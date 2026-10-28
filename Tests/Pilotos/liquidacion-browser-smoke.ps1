param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $ChromePath)) { throw 'Chrome no está disponible para la prueba de Liquidación.' }
$bin=Join-Path $PSScriptRoot 'bin'
$smoke=Join-Path $bin 'liquidacion-browser-smoke.html'
$probe=@'
<script>
(function(){try {
 function check(ok,reason){if(!ok)throw new Error(reason);}
 var scenario='__SCENARIO__';
 check(document.documentElement.scrollWidth<=innerWidth+1,'desbordamiento horizontal');
 if(scenario==='liq-index'){
  var checks=document.querySelectorAll('.liq-route-check');
  check(checks.length>=2,'rutas seleccionables ausentes');
  var all=document.getElementById('liq-select-visible'),button=document.getElementById('liq-export-button');
  check(button.disabled,'Excel habilitado sin selección');
  checks[0].click();
  check(!button.disabled && document.getElementById('liq-selected-count').textContent.indexOf('1 seleccionadas')>=0,'selección Excel incorrecta');
  all.click();check(Array.prototype.every.call(checks,function(c){return c.checked;}),'seleccionar página incorrecto');
  var excel=document.getElementById('liq-export-form');
  excel.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
  check(new FormData(excel).getAll('ids').length===checks.length,'Excel no envía todas las rutas seleccionadas');
  var routeForm=document.querySelector('.liq-confirm-form'),modal=document.getElementById('liq-confirm-modal');
  check(routeForm && modal,'confirmación de ruta ausente');
  routeForm.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
  check(!modal.hidden && modal.textContent.indexOf('Liquidar definitivamente')>=0,'no abre confirmación de ruta');
  document.getElementById('liq-modal-cancel').click();check(modal.hidden,'cancelar no cierra confirmación');
 } else if(scenario==='liq-detalle'){
  var bulk=document.querySelector('form[action*="LiquidarTodos"]'),docs=document.querySelectorAll('form[action*="LiquidarDocumento"]');
  check(bulk && docs.length===2,'acciones documentales ausentes');
  bulk.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
  check(!document.getElementById('liq-confirm-modal').hidden && document.getElementById('liq-modal-message').textContent.indexOf('todos los documentos')>=0,'confirmación de lote incorrecta');
  document.getElementById('liq-modal-cancel').click();
  check(document.getElementById('liq-confirm-modal').hidden,'cancelación de lote incorrecta');
 } else if(scenario==='liq-liquidada'){
  check(!document.querySelector('.liq-confirm-form'),'ruta liquidada conserva acciones');
 }
 document.documentElement.setAttribute('data-liq-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-liq-smoke','FAIL: '+e.message);}})();
</script>
'@
foreach($scenario in @('liq-index','liq-detalle','liq-liquidada')) {
    $html=Get-Content -LiteralPath (Join-Path $bin ($scenario+'.html')) -Raw
    $html=$html.Replace('</body>',$probe.Replace('__SCENARIO__',$scenario)+'</body>')
    Set-Content -LiteralPath $smoke -Value $html -Encoding UTF8
    foreach($size in @('425,900','1280,900')) {
        $stdout=Join-Path $bin 'liq-chrome-stdout.tmp'
        $stderr=Join-Path $bin 'liq-chrome-stderr.tmp'
        try {
            $chrome=Start-Process -FilePath $ChromePath -ArgumentList @('--headless=new','--disable-gpu','--disable-extensions','--no-first-run','--no-default-browser-check',"--window-size=$size",'--virtual-time-budget=1000','--dump-dom',([Uri]$smoke).AbsoluteUri) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
            $output=Get-Content -LiteralPath $stdout -Raw
            if($chrome.ExitCode -ne 0) {throw ('Chrome terminó con código '+$chrome.ExitCode)}
        } finally {Remove-Item -LiteralPath $stdout,$stderr -ErrorAction SilentlyContinue}
        if($output -notmatch 'data-liq-smoke="PASS"') {
            $failure=[regex]::Match($output,'data-liq-smoke="([^"]+)"')
            throw ('Liquidación '+$scenario+' '+$size+': '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'}))
        }
    }
    Write-Host ('OK Chrome Liquidación: '+$scenario+' móvil/escritorio.')
}
