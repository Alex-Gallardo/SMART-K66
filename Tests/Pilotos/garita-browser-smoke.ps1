param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $ChromePath)) { throw 'Chrome no está disponible para la prueba de navegador de garita.' }
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$bin=Join-Path $PSScriptRoot 'bin'
$css=([Uri](Join-Path $repo 'DiamDev.Give.UI/Content/garita.css')).AbsoluteUri
$refreshCss=([Uri](Join-Path $repo 'DiamDev.Give.UI/Content/garita-refresh.css')).AbsoluteUri
$cargoJs=([Uri](Join-Path $repo 'DiamDev.Give.UI/Scripts/garita-cargo.js')).AbsoluteUri
$uiJs=([Uri](Join-Path $repo 'DiamDev.Give.UI/Scripts/garita-ui.js')).AbsoluteUri
$smoke=Join-Path $bin 'garita-browser-smoke.html'
foreach($name in @('vacia','lista','sin-resultados','diferencia','abierta','en-ruta','salida','empresa-pendiente','empresa-asignada','cerrada','entrada','cargo')) {
    $body=Get-Content -LiteralPath (Join-Path $bin ('garita-'+$name+'.html')) -Raw
    $body=$body.Replace('/pedidosap/Content/garita.css',$css).Replace('/pedidosap/Content/garita-refresh.css',$refreshCss).Replace('/pedidosap/Scripts/garita-ui.js',$uiJs)
    $probe=@"
<script>
if('$name'==='en-ruta'){window.garitaFetchCount=0;window.fetch=function(){window.garitaFetchCount++;var ok=window.garitaFetchCount>1;return Promise.resolve({ok:ok,headers:{get:function(){return 'application/json';}},json:function(){return Promise.resolve(ok?{ok:true,tipo:'salida',hora:'09:34 AM',url:'#'}:{ok:false,mensaje:'La ruta cambió. Revísala otra vez.'});}});};}
</script><script src="$cargoJs"></script>
<script>
(function(){try{
function check(x,m){if(!x)throw new Error(m);}
check(document.documentElement.scrollWidth<=innerWidth+1,'desbordamiento horizontal');
var detalle=!!document.querySelector('.garita-detail-content');
var buscador=document.querySelector('.search-card');
var footer=document.querySelector('.garita-footer');
var scrollArea=document.querySelector(detalle?'.garita-detail-content':'#contenido');
function checkFooter() {
 check(Math.abs(footer.getBoundingClientRect().bottom-innerHeight)<=1,'footer separado del borde inferior');
 check(footer.querySelector('a').getAttribute('href')==='tel:137','falta llamada al 137');
 check(scrollArea.getBoundingClientRect().bottom<=footer.getBoundingClientRect().top+1,'el footer tapa el contenido');
}
checkFooter();
if(detalle) {
 check(!buscador && document.querySelector('.topbar .search-back-link') && document.querySelector('.topbar h1').textContent.includes('PP26-'),'el detalle debe mostrar un solo encabezado con navegación y ruta');
 check(document.querySelector('.search-other-link') && document.querySelector('.search-other-link').getAttribute('href').indexOf('placa=')<0 && document.querySelector('.search-other-link').getAttribute('href').indexOf('id=')<0,'falta buscar otra placa');
} else check(buscador.querySelector('h1') && document.querySelector('input[name="placa"]') && !document.querySelector('.search-back-link'),'búsqueda inicial incompleta');
var theme=document.getElementById('theme-toggle');
for(var mode=0;mode<2;mode++){
 var oldTheme=document.documentElement.getAttribute('data-theme');
 theme.click();
 check(oldTheme!==document.documentElement.getAttribute('data-theme'),'el interruptor de tema no cambia el modo');
 checkFooter();
}
var form=document.getElementById('register-exit-form');
var authorize=document.querySelector('button[form="register-exit-form"]');
if('$name'==='abierta' || '$name'==='en-ruta' || '$name'==='empresa-asignada' || '$name'==='empresa-pendiente') {check(form,'falta registro de salida');check(form.querySelectorAll('input[type="checkbox"][required]').length===2,'debe haber dos verificaciones');check(form.querySelector('input[type="file"][name="foto"]'),'falta foto opcional');check(!form.querySelector('input[name="Precinto"]'),'precinto visible');check(document.body.textContent.indexOf('FAC-001')>=0,'falta documento');check(document.querySelector('#difference-dialog [name="MotivoRapido"]'),'faltan motivos rápidos');}
else check(!form,'registro de salida disponible en estado no autorizado');
if('$name'==='vacia') {check(!detalle && !document.querySelector('.route-option'),'la pantalla inicial debe mostrar solo la búsqueda');if(innerWidth<=750){var plate=document.getElementById('placa');plate.focus();plate.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',bubbles:true}));check(document.activeElement!==plate,'Enter debe ocultar teclado móvil');plate.focus();document.getElementById('qr-open').click();check(document.activeElement!==plate,'abrir QR debe ocultar teclado móvil');}}
if('$name'==='lista') {check(document.querySelectorAll('.route-option').length===4 && document.querySelectorAll('.pager').length===3 && document.querySelector('.open-results') && document.querySelector('.closed-results'),'la placa debe conservar A, E, C y paginación independiente');check(document.querySelectorAll('.route-tabs a').length===3 && document.querySelectorAll('.results-card:not([hidden])').length===1 && document.querySelector('.route-tabs a[href*="seccion=salidas"]'),'solo una sección debe quedar visible');check(document.querySelector('.route-option-counts').textContent.indexOf('1 documento')>=0,'plural singular incorrecto');}
if('$name'==='sin-resultados') check(!document.querySelector('.route-option') && document.body.textContent.indexOf('No hay rutas para esta placa')>=0,'debe mostrar placa sin rutas');
if('$name'==='en-ruta') check(document.querySelector('.route-status .state-open') && authorize && authorize.form===form,'falta acción de salida en ruta E');
if('$name'==='en-ruta') {
 check(authorize.disabled,'autorizar debe esperar dos verificaciones');
 var actions=authorize.closest('.garita-detail-actions');
 check(Math.abs(actions.getBoundingClientRect().bottom-footer.getBoundingClientRect().top)<=1,'acciones separadas del pie de página');
 var cargo=document.querySelector('.cargo-expand');
 cargo.querySelector('summary').click();check(cargo.open,'no se puede abrir el detalle de la carga');
 scrollArea.scrollTop=scrollArea.scrollHeight;
 if(innerWidth<=750)check(scrollArea.scrollTop>0,'la prueba debe desplazar el detalle móvil');
 checkFooter();
 var modal=document.getElementById('difference-dialog');document.getElementById('difference-open').click();check(modal.open,'no abre motivos rápidos');
 modal.querySelector('[value="BULTOS"]').click();check(!document.getElementById('descripcion').required,'motivo rápido exige texto innecesario');
 modal.querySelector('[value="OTRO"]').click();check(document.getElementById('descripcion').required && document.getElementById('descripcion').minLength===10,'Otro debe exigir detalle');
 document.getElementById('difference-close').click();check(!modal.open,'no cierra motivos rápidos');
}
if('$name'==='abierta') check(document.querySelector('.route-status .state-open') && document.body.textContent.indexOf('pasará de Abierta a En ruta')>=0,'falta acción de salida en ruta A');
if('$name'==='empresa-pendiente') check(form && document.body.textContent.indexOf('PILOTO DEMO')>=0,'garita debe usar el piloto de APK66');
if('$name'==='empresa-asignada') check(form && document.body.textContent.indexOf('JUAN PEREZ')>=0,'garita no mostró conductor asignado');
if('$name'==='cerrada') {var entry=document.querySelector('#register-entry-form');check(entry && !form && document.body.textContent.indexOf('Sin hora de entrada')>=0,'cerrada debe poder registrar entrada');check(entry.enctype==='multipart/form-data' && entry.querySelector('textarea[name="Observacion"]') && entry.querySelector('input[type="file"][name="foto"]'),'entrada debe aceptar observación y foto opcionales');}
if('$name'==='entrada') check(!document.querySelector('#register-entry-form') && !form && document.body.textContent.indexOf('Hora de entrada:')>=0 && document.body.textContent.indexOf('Regresó con carga')>=0 && document.body.textContent.indexOf('Ver foto de entrada')>=0,'entrada duplicada disponible y evidencia visible');
if('$name'==='cargo') {
 check(document.querySelectorAll('.customer-delivered').length===1 && document.querySelectorAll('.customer-warning').length===1 && document.querySelectorAll('.customer-pending').length===1,'bordes de clientes incorrectos');
 var delivered=document.querySelector('.cargo-document[data-documento="FAC-ENTREGADA"]');
 check(delivered && delivered.querySelector('.cargo-result-delivered') && delivered.dataset.fotoDoc.indexOf('FotoPiloto')>=0 && delivered.dataset.fotoCliente.indexOf('FotoPiloto')>=0,'estado o enlaces de fotos de factura entregada');
 delivered.click();var modal=document.getElementById('cargo-document-dialog');
 check(modal.open && document.getElementById('cargo-dialog-observacion').textContent==='Recibió en bodega' && document.getElementById('cargo-dialog-monto').textContent.indexOf('120')>=0,'modal no muestra detalles de factura');
 check(!document.getElementById('cargo-dialog-photo-doc').hidden && !document.getElementById('cargo-dialog-photo-client').hidden,'modal no muestra fotos disponibles');
 document.getElementById('cargo-dialog-close').click();check(!modal.open && document.activeElement===delivered,'modal no se cierra o no devuelve el foco');
 document.querySelector('.cargo-document[data-documento="FAC-NO-ENTREGADA"]').click();
 check(modal.open && document.getElementById('cargo-dialog-motivo').textContent==='TIEMPO RUTA' && !document.getElementById('cargo-dialog-no-photo').hidden,'modal no muestra motivo o ausencia de fotos');
 modal.close();
}
if('$name'==='en-ruta') {
 form.querySelector('[name="IdentidadVerificada"]').checked=true;form.querySelector('[name="IdentidadVerificada"]').dispatchEvent(new Event('change'));
 check(authorize.disabled,'una verificación no basta');
 form.querySelector('[name="CargaVerificada"]').checked=true;form.querySelector('[name="CargaVerificada"]').dispatchEvent(new Event('change'));
 check(!authorize.disabled,'dos verificaciones deben habilitar la salida');
 authorize.click();setTimeout(function(){try{
   check(window.garitaFetchCount===1 && !document.querySelector('.garita-receipt') && !authorize.disabled,'respuesta rechazada no debe mostrar éxito ni bloquear reintento');
   check(document.getElementById('register-exit-form-error').textContent.indexOf('ruta cambió')>=0,'falta mensaje de error');
   authorize.click();setTimeout(function(){try{check(window.garitaFetchCount===2 && document.querySelector('.garita-receipt').textContent.indexOf('09:34 AM')>=0,'el éxito confirmado debe mostrar hora');document.documentElement.setAttribute('data-garita-smoke','PASS');}catch(e){document.documentElement.setAttribute('data-garita-smoke','FAIL: '+e.message);}},50);
 }catch(e){document.documentElement.setAttribute('data-garita-smoke','FAIL: '+e.message);}},50);
} else document.documentElement.setAttribute('data-garita-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-garita-smoke','FAIL: '+e.message);}})();
</script>
"@
    Set-Content -LiteralPath $smoke -Value $body.Replace('</body>',"$probe</body>") -Encoding UTF8
    foreach($size in @('500,680','1440,1000')) {
        $stdout=Join-Path $bin 'garita-chrome-stdout.tmp'
        $stderr=Join-Path $bin 'garita-chrome-stderr.tmp'
        try {
            $chrome=Start-Process -FilePath $ChromePath -ArgumentList @('--headless=new','--disable-gpu','--disable-extensions','--no-first-run','--no-default-browser-check',"--window-size=$size",'--virtual-time-budget=1000','--dump-dom',([Uri]$smoke).AbsoluteUri) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
            $output=Get-Content -LiteralPath $stdout -Raw
            if($chrome.ExitCode -ne 0) { throw ('Chrome terminó con código '+$chrome.ExitCode) }
        } finally {
            Remove-Item -LiteralPath $stdout,$stderr -ErrorAction SilentlyContinue
        }
        if($output -notmatch 'data-garita-smoke="PASS"') {
            $resultado=[regex]::Match($output,'data-garita-smoke="([^"]+)"')
            throw ('Garita '+$name+' '+$size+': '+$(if($resultado.Success){$resultado.Groups[1].Value}else{'sin resultado'}))
        }
    }
    Write-Host ('OK Chrome garita: '+$name+' móvil/escritorio.')
}
