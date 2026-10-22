param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $ChromePath)) { throw 'Chrome no está disponible para la prueba de transportistas.' }
$bin=Join-Path $PSScriptRoot 'bin'
$smoke=Join-Path $bin 'empresa-browser-smoke.html'
foreach($scenario in @('index-empresa-sin-placa','index-empresa','editar-ruta-empresa')) {
    $html=Get-Content -LiteralPath (Join-Path $bin ($scenario+'.html')) -Raw
    $probe=@'
<script>
(function(){try{
function check(ok,message){if(!ok)throw new Error(message);}
check(document.documentElement.scrollWidth<=innerWidth+1,'desbordamiento horizontal');
var escenario='__SCENARIO__';
if(escenario.indexOf('index-')===0){
 if(escenario==='index-empresa-sin-placa') {
  var placas=document.querySelectorAll('.company-plate-option');
  check(placas.length===2 && placas[0].getBoundingClientRect().width>100,'lista de placas incompleta u oculta');
  check(!document.querySelector('.route-card'),'ruta visible antes de escoger placa');
 } else {
  var card=document.querySelector('.route-card');
  check(document.querySelector('.company-selected-plate').textContent.includes('C-002') && card && card.textContent.includes('EMP-001') && card.textContent.includes('JUAN PEREZ'),'ruta o conductor no visibles');
 }
} else {
 var form=document.querySelector('form[action*="AsignarConductor"]');
 check(form && form.elements.EmpresaUsuarioId.value==='22' && form.elements.Nombre.value==='JUAN PEREZ','formulario de conductor incompleto');
}
document.documentElement.setAttribute('data-empresa-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-empresa-smoke','FAIL: '+e.message);}})();
</script>
'@
    $html=$html.Replace('</body>',$probe.Replace('__SCENARIO__',$scenario)+'</body>')
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
