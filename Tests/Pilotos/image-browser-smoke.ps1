param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture=Join-Path $PSScriptRoot 'bin/detalle-cierre.html'
if(!(Test-Path -LiteralPath $fixture)) { throw 'Ejecuta Tests/Pilotos/run.ps1 para generar la vista previa.' }
if(!(Test-Path -LiteralPath $ChromePath)) { throw 'Indica la ruta de Chrome con -ChromePath.' }
$scriptUri=([Uri](Join-Path $repo 'DiamDev.Give.UI/Scripts/pilotos.js')).AbsoluteUri
$html=Get-Content -LiteralPath $fixture -Raw
$marker='<script src="/Scripts/pilotos.js"></script>'
if(!$html.Contains($marker)) { throw 'No se encontró el script del portal en la vista previa.' }
$probe=@'
<script>
window.__imageFetches=0;
window.confirm=function(){ return true; };
window.fetch=function(){
    window.__imageFetches++;
    var ok=window.__imageFetches<3;
    return Promise.resolve({headers:{get:function(){return 'application/json';}},json:function(){
        return Promise.resolve(ok ? {ok:true,url:'/Piloto/Imagen/DEMO-2026-001?rowId=2'} : {ok:false,mensaje:'Error de prueba'});
    }});
};
</script>
<script src="__SCRIPT_URI__"></script>
<script>
(async function(){
    try {
        function check(value,message){if(!value) throw new Error(message);}
        function chooseFile(){
            var input=document.getElementById('image-file');
            var transfer=new DataTransfer();
            transfer.items.add(new File(['imagen de prueba'],'prueba.jpg',{type:'image/jpeg'}));
            input.files=transfer.files;
            input.dispatchEvent(new Event('change',{bubbles:true}));
        }
        async function upload(){
            chooseFile();
            document.getElementById('image-upload-form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
            await new Promise(function(resolve){setTimeout(resolve,30);});
        }
        var group=document.querySelector('.customer-group');
        check(group.querySelector('.customer-documents').hidden,'Cliente nuevo debía comenzar contraído');
        group.querySelector('.client-toggle').click();
        check(!group.querySelector('.customer-documents').hidden,'No se pudo expandir el cliente');
        var card=group.querySelectorAll('.document-card')[1];
        card.querySelector('.document-toggle').click();
        var trigger=card.querySelector('.image-upload-trigger');
        trigger.click();
        await upload();
        var link=card.querySelector('.document-image-link');
        check(!link.hidden && link.querySelector('a').href.indexOf('/Piloto/Imagen/')>=0,'Falta el enlace de imagen');
        check(trigger.getAttribute('data-replace')==='true' && trigger.querySelector('.image-upload-label').textContent==='Reemplazar imagen','Falta estado de reemplazo');
        check(card.querySelector('.image-card-status').textContent.indexOf('Imagen guardada')>=0,'Falta mensaje de éxito');
        check(document.getElementById('image-modal').hidden,'El modal no se cerró');
        trigger.click();
        await upload();
        check(card.querySelectorAll('.document-image-link').length===1 && card.querySelectorAll('.image-card-status').length===1,'Se duplicaron elementos al reemplazar');
        trigger.click();
        await upload();
        check(!document.getElementById('image-modal').hidden && document.getElementById('image-status').textContent==='Error de prueba','No se mostró el error del servidor');
        check(!link.hidden && window.__imageFetches===3,'El error alteró la imagen existente');
        document.documentElement.setAttribute('data-image-smoke','PASS');
    } catch(error) {
        document.documentElement.setAttribute('data-image-smoke','FAIL: '+error.message);
    }
})();
</script>
'@
$probe=$probe.Replace('__SCRIPT_URI__',$scriptUri)
$html=$html.Replace($marker,$probe)
$smokeFile=Join-Path $env:TEMP ('pilotos-image-smoke-'+[Guid]::NewGuid().ToString('N')+'.html')
try {
    Set-Content -LiteralPath $smokeFile -Value $html -Encoding UTF8
    $uri=([Uri]$smokeFile).AbsoluteUri
    foreach($size in @('425,900','1280,900')) {
        $output=& $ChromePath '--headless=new' '--disable-gpu' '--no-first-run' '--no-default-browser-check' "--window-size=$size" '--virtual-time-budget=3000' '--dump-dom' $uri 2>&1 | Out-String
        if($output -notmatch 'data-image-smoke="PASS"') {
            $failure=[regex]::Match($output,'data-image-smoke="([^"]+)"')
            if($failure.Success) { throw ('Falló la interacción en navegador '+$size+': '+$failure.Groups[1].Value) }
            throw ('Chrome no devolvió el resultado de la interacción en '+$size+'.')
        }
    }
    $workflowProbe=@'
<script>
window.confirm=function(){return true;};
window.__clientPosts=0;
window.__failNextClient=false;
window.fetch=function(url){var client=String(url).indexOf('GuardarCliente')>=0;if(client) window.__clientPosts++;var fail=client && window.__failNextClient;if(fail) window.__failNextClient=false;return Promise.resolve({headers:{get:function(){return 'application/json';}},json:function(){
    return Promise.resolve(fail?{ok:false,mensaje:'Error de prueba'}:{ok:true,url:String(url).indexOf('SubirImagenCliente')>=0?'/Piloto/ImagenCliente/DEMO-2026-001?primerRowId=1':null});
}});};
</script>
<script src="__SCRIPT_URI__"></script>
<script>
(async function(){try {
    function check(ok,message){if(!ok) throw new Error(message);}
    var group=document.querySelector('.customer-group'), cards=group.querySelectorAll('.document-card');
    check(cards[0].querySelector('.image-upload-trigger').hidden,'Entregado permite subir foto de documento');
    check(!cards[1].querySelector('.image-upload-trigger').hidden,'No entregado oculta foto de documento');
    var photo=group.querySelector('.client-image-upload-trigger');
    check(!photo.disabled,'Foto final debe habilitarse con todos los documentos resueltos');
    cards[1].querySelector(".delivery-result[value='ENTREGADO']").click();
    check(cards[1].querySelector('.image-upload-trigger').hidden,'Entregado mantiene boton de documento');
    cards[1].querySelector(".delivery-result[value='NO ENTREGADO']").click();
    cards[1].querySelector('.delivery-reason').value='CLIENTE CERRADO';
    cards[1].querySelector('.delivery-observation').value='Local cerrado';
    cards[1].querySelector('.delivery-observation').dispatchEvent(new Event('input',{bubbles:true}));
    check(!photo.disabled,'Foto final no se rehabilitó');
    photo.click();
    var transfer=new DataTransfer();transfer.items.add(new File(['imagen'],'cliente.jpg',{type:'image/jpeg'}));
    var input=document.getElementById('image-file');input.files=transfer.files;
    document.getElementById('image-upload-form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));
    await new Promise(function(resolve){setTimeout(resolve,30);});
    check(group.querySelector('.client-image-link').hidden===false,'No aparece foto final');
    group.querySelector('.save-client').click();
    check(!document.getElementById('client-save-modal').hidden,'No abrió confirmación del cliente');
    var sheet=document.querySelector('#client-save-modal .client-save-sheet'), bounds=sheet.getBoundingClientRect();
    check(bounds.left>=0 && bounds.right<=innerWidth && bounds.top>=0 && bounds.bottom<=innerHeight,'Modal fuera de pantalla');
    check(sheet.contains(document.activeElement),'El foco no ingresó al modal');
    check(window.__clientPosts===0,'Guardó el cliente antes de confirmar');
    document.querySelector('#client-save-modal .modal-actions [data-close-client]').click();
    check(document.getElementById('client-save-modal').hidden && window.__clientPosts===0,'Cancelar modificó el cliente');
    group.querySelector('.save-client').click();
    document.getElementById('client-save-confirm').click();
    await new Promise(function(resolve){setTimeout(resolve,30);});
    check(group.getAttribute('data-saved')==='true','Cliente no se bloqueó');
    check(document.getElementById('client-save-modal').hidden && window.__clientPosts===1,'No cerró modal tras guardar');
    check(group.querySelectorAll('.document-card').length===0,'Formulario editable quedó visible');
    check(group.querySelectorAll('.completed-document').length===2,'Falta resumen compacto');
    check(group.querySelectorAll("input[name$='.RowId']").length===2,'Faltan datos canónicos para cierre');
    check(!!group.querySelector('.client-photo-link'),'No se conservó foto final');
    group.querySelector('.client-toggle').click();
    check(!group.querySelector('.customer-documents').hidden,'Cliente completado no expande resumen');
    var second=document.querySelectorAll('.customer-group')[1], last=second.querySelector('.document-card');
    document.getElementById('review-button').click();
    check(document.getElementById('complete-modal').hidden && !second.querySelector('.client-warning').hidden,'Cierre no señaló el cliente pendiente');
    second.querySelector('.save-client').click();
    check(document.getElementById('client-save-modal').hidden,'Abrió modal con factura incompleta');
    check(!second.querySelector('.client-warning').hidden && !last.querySelector('.document-warning').hidden,'Faltan advertencias del formulario');
    check(!last.querySelector('.document-edit').hidden && last.querySelector('.delivery-result').getAttribute('aria-invalid')==='true','No mostró ni enfocó la factura pendiente');
    last.querySelector(".delivery-result[value='ENTREGADO']").click();
    check(last.querySelector('.document-warning').hidden && second.querySelector('.client-warning').hidden,'Advertencias no se limpiaron al corregir');
    check(second.querySelector('.save-client')!==null,'Falta acción del segundo cliente');
    second.querySelector('.save-client').click();
    check(!document.getElementById('client-save-modal').hidden,'No abrió modal tras corregir');
    window.__failNextClient=true;
    document.getElementById('client-save-confirm').click();
    await new Promise(function(resolve){setTimeout(resolve,30);});
    check(second.getAttribute('data-saved')==='false' && !document.getElementById('client-save-modal').hidden,'Error del servidor bloqueó cliente o cerró modal');
    check(document.getElementById('client-save-status').textContent==='Error de prueba' && !last.querySelector('.delivery-result:checked').disabled,'Falta advertencia o recuperación tras error');
    document.getElementById('client-save-confirm').click();
    await new Promise(function(resolve){setTimeout(resolve,30);});
    check(second.getAttribute('data-saved')==='true','Segundo cliente no se bloqueó');
    check(window.__clientPosts===3,'El reintento no llegó al servidor');
    document.getElementById('review-button').click();
    check(!document.getElementById('complete-modal').hidden,'No abrió resumen final');
    check(document.getElementById('summary-delivered').textContent==='2','Resumen perdió entregas');
    check(document.getElementById('summary-failed').textContent==='1','Resumen perdió no entregados');
    document.documentElement.setAttribute('data-workflow-smoke','PASS');
}catch(error){document.documentElement.setAttribute('data-workflow-smoke','FAIL: '+error.message);}})();
</script>
'@
    $workflowProbe=$workflowProbe.Replace('__SCRIPT_URI__',$scriptUri)
    $workflowHtml=(Get-Content -LiteralPath $fixture -Raw).Replace($marker,$workflowProbe)
    Set-Content -LiteralPath $smokeFile -Value $workflowHtml -Encoding UTF8
    foreach($size in @('425,900','1280,900')) {
        $output=& $ChromePath '--headless=new' '--disable-gpu' '--no-first-run' '--no-default-browser-check' "--window-size=$size" '--virtual-time-budget=3000' '--dump-dom' $uri 2>&1 | Out-String
        if($output -notmatch 'data-workflow-smoke="PASS"') {
            $failure=[regex]::Match($output,'data-workflow-smoke="([^"]+)"')
            throw ('Falló el flujo completo en '+$size+': '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'}))
        }
    }
    $groupProbe=@'
<script src="__SCRIPT_URI__"></script>
<script>
(function(){
    try {
        var groups=document.querySelectorAll('.customer-group');
        var saved=document.body.getAttribute('data-saved-fixture')==='true';
        var first=groups[0].querySelector('.customer-documents');
        var second=groups[1] && groups[1].querySelector('.customer-documents');
        if(first.hidden===saved || (second && !second.hidden)) throw new Error('Estado inicial incorrecto');
        groups[0].querySelector('.client-toggle').click();
        if(first.hidden!==saved) throw new Error('El control no cambia el estado');
        document.documentElement.setAttribute('data-group-smoke','PASS');
    } catch(error) { document.documentElement.setAttribute('data-group-smoke','FAIL: '+error.message); }
})();
</script>
'@
    $groupProbe=$groupProbe.Replace('__SCRIPT_URI__',$scriptUri)
    foreach($fixtureName in @('detalle-borrador','detalle-historial')) {
        $groupHtml=Get-Content -LiteralPath (Join-Path $PSScriptRoot ('bin/'+$fixtureName+'.html')) -Raw
        $groupHtml=$groupHtml.Replace($marker,$groupProbe).Replace('<body>','<body data-saved-fixture="'+($fixtureName -eq 'detalle-borrador').ToString().ToLowerInvariant()+'">')
        Set-Content -LiteralPath $smokeFile -Value $groupHtml -Encoding UTF8
        $output=& $ChromePath '--headless=new' '--disable-gpu' '--no-first-run' '--no-default-browser-check' '--window-size=425,900' '--virtual-time-budget=3000' '--dump-dom' $uri 2>&1 | Out-String
        if($output -notmatch 'data-group-smoke="PASS"') {
            $failure=[regex]::Match($output,'data-group-smoke="([^"]+)"')
            throw ('Falló el estado de '+$fixtureName+': '+$(if($failure.Success){$failure.Groups[1].Value}else{'sin resultado'}))
        }
    }
    Write-Host 'OK Chrome móvil/escritorio: fotos por resultado, foto final, bloqueo, resumen y expansión.'
} finally {
    Remove-Item -LiteralPath $smokeFile -Force -ErrorAction SilentlyContinue
}
