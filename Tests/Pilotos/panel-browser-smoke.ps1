param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$bin=Join-Path $PSScriptRoot 'bin'
$scriptUri=([Uri](Join-Path $repo 'DiamDev.Give.UI/Scripts/pilotos-panel.js')).AbsoluteUri
$cssUri=([Uri](Join-Path $repo 'DiamDev.Give.UI/Content/pilotos.css')).AbsoluteUri
$panelCssUri=([Uri](Join-Path $repo 'DiamDev.Give.UI/Content/pilotos-panel.css')).AbsoluteUri
$smoke=Join-Path $bin 'panel-smoke.html'
$probe=@'
<script>
window.alert=function(){throw new Error('No debe usar alert nativo');};
window.confirm=function(){throw new Error('No debe usar confirm nativo');};
window.__panelCalls=0;
window.fetch=function(){window.__panelCalls++;return Promise.resolve({ok:false,status:503,json:function(){return Promise.resolve({mensaje:'Error simulado de consulta'});}});};
</script>
<script src="__SCRIPT_URI__"></script>
<script>
(async function(){
try {
function check(value,text){if(!value)throw new Error(text);}
check(document.documentElement.scrollWidth<=window.innerWidth+1,'Desbordamiento horizontal');
check(!window.PilotoPanelUI.validDates('2026-09-30','2026-09-01'),'Fechas invertidas');
check(!window.PilotoPanelUI.validDates('2026-02-30','2026-03-01'),'Fecha inexistente');
check(window.PilotoPanelUI.validDates('2026-09-01','2026-10-01'),'31 días inclusivos');
var content=document.getElementById('panel-content');
if(content){
    var antes=content.innerHTML;
    document.getElementById('panel-desde').value='2026-09-30';document.getElementById('panel-hasta').value='2026-09-01';
    var submit=new Event('submit',{bubbles:true,cancelable:true});document.getElementById('panel-filters').dispatchEvent(submit);
    check(submit.defaultPrevented && !document.getElementById('panel-message').hidden,'Validación propia visible');
    await window.PilotoPanelUI.refresh(false);
    check(content.innerHTML===antes,'Error conserva los datos previos');
    check(document.getElementById('panel-message').textContent.indexOf('Error simulado')>=0,'Error integrado');
    check(!document.getElementById('panel-refresh').disabled,'Reintento habilitado');
    window.fetch=function(){window.__panelCalls++;return Promise.resolve({ok:true,redirected:false,headers:{get:function(){return 'text/html';}},text:function(){return Promise.resolve(antes);}});};
    await window.PilotoPanelUI.refresh(false);
    check(document.getElementById('panel-message').hidden && !document.getElementById('panel-toast').hidden,'Reintento y notificación de éxito');
} else {
    var link=document.querySelector('.panel-photo');var customer=link.closest('details');if(customer)customer.open=true;link.focus();link.click();
    var dialog=document.getElementById('panel-photo-dialog');
    check(!dialog.hidden && document.activeElement.id==='panel-photo-close','Visor accesible');
    document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));
    check(dialog.hidden && document.activeElement===link,'Cerrar devuelve el foco');
}
document.documentElement.setAttribute('data-panel-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-panel-smoke','FAIL: '+e.message);}
})();
</script>
'@
$probe=$probe.Replace('__SCRIPT_URI__',$scriptUri)
try {
    foreach($name in @('resumen','rutas','pilotos','documentos','actividad','alertas','detalle')) {
        $html=Get-Content -LiteralPath (Join-Path $bin ('panel-'+$name+'.html')) -Raw
        $html=$html.Replace('/Content/pilotos.css',$cssUri).Replace('/Content/pilotos-panel.css',$panelCssUri)
        $html=$html.Replace('<script src="/Scripts/pilotos.js"></script>',$probe)
        Set-Content -LiteralPath $smoke -Value $html -Encoding UTF8
        foreach($size in @('425,900','1440,1000')) {
            $output=& $ChromePath '--headless=new' '--disable-gpu' '--no-first-run' '--no-default-browser-check' "--window-size=$size" '--virtual-time-budget=4000' '--dump-dom' ([Uri]$smoke).AbsoluteUri 2>&1 | Out-String
            if($output -notmatch 'data-panel-smoke="PASS"') {
                $errorResult=[regex]::Match($output,'data-panel-smoke="([^"]+)"')
                throw ('Panel '+$name+' '+$size+': '+$(if($errorResult.Success){$errorResult.Groups[1].Value}else{'sin resultado'}))
            }
        }
        Write-Host ('OK Chrome panel: '+$name+' móvil/escritorio; alertas, reintento, fechas y visor.')
    }
    $pilotos=Get-Content -LiteralPath (Join-Path $bin 'panel-pilotos.html') -Raw
    $admin=Get-Content -LiteralPath (Join-Path $bin 'panel-admin.html') -Raw
    if($pilotos -match 'Configurar piloto' -or $admin -notmatch 'Configurar piloto') {throw 'La administración debe estar separada del permiso de consulta.'}
} finally { Remove-Item -LiteralPath $smoke -Force -ErrorAction SilentlyContinue }
