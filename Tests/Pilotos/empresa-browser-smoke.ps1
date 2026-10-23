param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $ChromePath)) { throw 'Chrome no está disponible para la prueba de transportistas.' }
$bin=Join-Path $PSScriptRoot 'bin'
$smoke=Join-Path $bin 'empresa-browser-smoke.html'
$pilotScript=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../DiamDev.Give.UI/Scripts/pilotos.js') -Raw
$pilotCss=([Uri](Join-Path $PSScriptRoot '../../DiamDev.Give.UI/Content/pilotos.css')).AbsoluteUri
foreach($scenario in @('index-empresa-sin-placa','index-empresa','detalle-empresa-lista','detalle-empresa-pendiente','detalle-empresa-sin-permiso','detalle-empresa-grande','editar-ruta-empresa')) {
    $html=Get-Content -LiteralPath (Join-Path $bin ($scenario+'.html')) -Raw
    $html=$html.Replace('href="/Content/pilotos.css"',('href="'+$pilotCss+'"'))
    $probe=@'
<script>
(function(){try{
function check(ok,message){if(!ok)throw new Error(message);}
check(document.documentElement.scrollWidth<=innerWidth+1,'desbordamiento horizontal');
var escenario='__SCENARIO__';
if(escenario.indexOf('index-')===0){
 if(escenario==='index-empresa-sin-placa') {
  var placas=document.querySelectorAll('.company-plate-option');
  check(placas.length===8 && placas[0].getBoundingClientRect().width>100,'lista de placas incompleta u oculta');
  check(!document.querySelector('.route-card'),'ruta visible antes de escoger placa');
  var filtro=document.getElementById('company-plate-filter');
  check(filtro,'falta búsqueda de placas');
  filtro.value='c002';filtro.dispatchEvent(new Event('input'));
  check(Array.prototype.filter.call(placas,function(p){return !p.hidden;}).length===1 && !placas[1].hidden,'filtro de placas incorrecto');
  filtro.value='sin placa';filtro.dispatchEvent(new Event('input'));
  check(!document.querySelector('.company-plate-empty').hidden,'falta mensaje sin coincidencias');
 } else {
  var card=document.querySelector('.route-card');
  check(document.querySelector('.company-selected-plate').textContent.includes('C-002') && card && card.textContent.includes('EMP-001') && card.textContent.includes('JUAN PEREZ'),'ruta o conductor no visibles');
  check(document.querySelector('.company-change-plate').getBoundingClientRect().height>=50,'Cambiar placa debe ser fácil de tocar');
 }
} else if(escenario.indexOf('detalle-')===0) {
 var editable=!!document.querySelector('.save-client') && !!document.querySelector('#complete-button');
 check(editable!==(escenario==='detalle-empresa-sin-permiso'),'acciones de piloto incorrectas para '+escenario);
 if(escenario==='detalle-empresa-pendiente') check(!Array.prototype.some.call(document.querySelectorAll('.notice'),function(n){return n.textContent.includes('Conductor pendiente');}),'asignación opcional bloquea ruta');
 if(escenario==='detalle-empresa-sin-permiso') check(document.body.textContent.includes('Pilotos.Rutas.Confirmar'),'falta explicar el permiso pendiente');
 if(escenario==='detalle-empresa-grande') {
  var groups=document.querySelectorAll('.customer-group');
  check(groups.length===3 && groups[2].textContent.includes('BLOQUE 11 DE 11'),'paginación de bloques grandes incorrecta');
  check(document.querySelector('input[name="DesdeBorradores"][value="true"]') && document.querySelector('.pagination'),'falta cierre por borradores o paginación');
  document.getElementById('review-button').click();
  check(document.getElementById('complete-modal').hidden,'ruta incompleta abrió cierre');
  groups[2].setAttribute('data-saved','true');
  document.getElementById('complete-route').setAttribute('data-completed-groups','11');
  document.getElementById('review-button').click();
  check(!document.getElementById('complete-modal').hidden,'ruta completa no abrió confirmación');
  var form=document.getElementById('complete-route');
  var reviewed=document.getElementById('confirm-reviewed');reviewed.checked=true;reviewed.dispatchEvent(new Event('change'));
  form.dispatchEvent(new Event('submit',{cancelable:true}));
  var payload=new FormData(form);
  check(payload.get('DesdeBorradores')==='true' && !Array.from(payload.keys()).some(function(key){return key.indexOf('Documentos[')===0;}),
   'el cierre grande envió resultados parciales');
 }
} else {
 var form=document.querySelector('form[action*="AsignarConductor"]');
 check(form && form.elements.EmpresaUsuarioId.value==='22' && form.elements.Nombre.value==='JUAN PEREZ','formulario de conductor incompleto');
}
document.documentElement.setAttribute('data-empresa-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-empresa-smoke','FAIL: '+e.message);}})();
</script>
'@
    $script=if($scenario -eq 'detalle-empresa-grande' -or $scenario -eq 'index-empresa-sin-placa') {'<script>'+$pilotScript+'</script>'} else {''}
    $html=$html.Replace('</body>',$script+$probe.Replace('__SCENARIO__',$scenario)+'</body>')
    Set-Content -LiteralPath $smoke -Value $html -Encoding UTF8
    foreach($size in @('425,900','1280,900')) {
        $stdout=Join-Path $bin 'empresa-chrome-stdout.tmp'
        $stderr=Join-Path $bin 'empresa-chrome-stderr.tmp'
        try {
            $chrome=Start-Process -FilePath $ChromePath -ArgumentList @('--headless=new','--disable-gpu','--disable-extensions','--no-first-run','--no-default-browser-check',"--window-size=$size",'--virtual-time-budget=1000','--dump-dom',([Uri]$smoke).AbsoluteUri) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
            $output=Get-Content -LiteralPath $stdout -Raw
            if($chrome.ExitCode -ne 0) {throw ('Chrome terminó con código '+$chrome.ExitCode)}
        } finally {Remove-Item -LiteralPath $stdout,$stderr -ErrorAction SilentlyContinue}
        if($output -notmatch 'data-empresa-smoke="PASS"') {
            $failure=[regex]::Match($output,'data-empresa-smoke="([^"]+)"')
            throw ('Transportista '+$scenario+' '+$size+': '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'}))
        }
    }
    Write-Host ('OK Chrome empresa: '+$scenario+' móvil/escritorio.')
}
