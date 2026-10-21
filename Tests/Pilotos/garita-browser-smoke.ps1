param([string]$ChromePath='C:\Program Files\Google\Chrome\Application\chrome.exe')
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $ChromePath)) { throw 'Chrome no está disponible para la prueba de navegador de garita.' }
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$bin=Join-Path $PSScriptRoot 'bin'
$css=([Uri](Join-Path $repo 'DiamDev.Give.UI/Content/garita.css')).AbsoluteUri
$smoke=Join-Path $bin 'garita-browser-smoke.html'
foreach($name in @('vacia','lista','diferencia','en-ruta','salida')) {
    $body=Get-Content -LiteralPath (Join-Path $bin ('garita-'+$name+'.html')) -Raw
    $probe=@"
<!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="$css"></head><body><main>$body</main>
<script>
(function(){try{
function check(x,m){if(!x)throw new Error(m);}
check(document.documentElement.scrollWidth<=innerWidth+1,'desbordamiento horizontal');
check(document.querySelector('input[name="id"]'),'falta búsqueda');
var form=document.getElementById('confirm-form');
if('$name'==='lista') {check(form,'falta confirmación');check(form.querySelectorAll('input[type="checkbox"][required]').length===2,'faltan verificaciones');check(document.body.textContent.indexOf('FAC-001')>=0,'falta documento');}
else check(!form,'confirmación disponible en estado no autorizado');
if('$name'==='vacia') check(!document.querySelector('.route-header') && !document.querySelector('.recent-route'),'la pantalla inicial debe mostrar solo la búsqueda');
if('$name'==='en-ruta') check(document.querySelector('.state-info') && document.body.textContent.indexOf('Ruta en curso')>=0,'falta estado En ruta de consulta');
document.documentElement.setAttribute('data-garita-smoke','PASS');
}catch(e){document.documentElement.setAttribute('data-garita-smoke','FAIL: '+e.message);}})();
</script></body></html>
"@
    Set-Content -LiteralPath $smoke -Value $probe -Encoding UTF8
    foreach($size in @('500,900','1440,1000')) {
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
