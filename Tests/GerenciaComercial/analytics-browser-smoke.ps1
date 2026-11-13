$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
if (-not (Test-Path -LiteralPath $edge)) { throw 'Se requiere Microsoft Edge para la prueba visual.' }
$taskDir = Join-Path $env:TEMP ('K66AnalyticsSmoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskDir | Out-Null
$view = Get-Content -LiteralPath (Join-Path $project 'DiamDev.Give.UI\Views\GerenciaComercial\Index.cshtml') -Raw -Encoding UTF8
$markup = $view.Substring($view.IndexOf('<div class="ka"'))
$markup = $markup.Substring(0, $markup.IndexOf('@section scripts'))
$markup = [regex]::Replace($markup, '@Url.Action\("(\w+)"\)', '/$1')
$css = ([uri](Join-Path $project 'DiamDev.Give.UI\Content\gerencia-comercial-analytics.css')).AbsoluteUri
$appSource = Get-Content -LiteralPath (Join-Path $project 'DiamDev.Give.UI\Scripts\App\gerencia-comercial-analytics.js') -Raw -Encoding UTF8
$mock = @'
<script>
window.calls=[];
window.addEventListener('error',e=>document.body.dataset.jsError=e.message);
window.addEventListener('unhandledrejection',e=>document.body.dataset.jsError=String(e.reason));
const agents=[{company:'FAES',agent:'Nombre repetido',agentKey:'FAES|1',agentCode:'1',current:150,previous:100},{company:'BOLIK',agent:'Nombre repetido',agentKey:'BOLIK|2',agentCode:'2',current:40,previous:80}];
const product={company:'FAES',agent:'Nombre repetido',agentKey:'FAES|1',code:'P1',name:'Producto <seguro>',unit:'CAJA',current:150,previous:100,qtyCurrent:3,qtyPrevious:2,participation:100,diff:50,growth:50,total:51};
window.fetch=async function(address){
 const u=new URL(address,'http://local');window.calls.push(u);
 const counts=[{clients:11,products:13}],trend=[{company:'FAES',month:1,current:150,previous:100}];
 let data;
 if(u.pathname.includes('Panel'))data={general:[{company:'FAES',type:'LOCAL',current:150,previous:100},{company:'BOLIK',type:'LOCAL',current:40,previous:80}],agentes:agents,productos:[product],tendencia:trend,conteos:counts,actualizado:'Prueba local'};
 else if(u.pathname.includes('Perfil'))data={ventas:[agents[0]],tendencia:trend,clientes:[product],productos:[product],conteos:counts};
 else data={rows:[product],total:51};
 return {ok:true,redirected:false,json:async()=>data};
};
window.print=()=>{window.printCalled=true;};
</script>
'@
$smoke = @'
<script>
(async function(){
 const wait=()=>new Promise(resolve=>setTimeout(resolve,350));
 const check=(ok,message)=>{if(!ok)throw Error(message);};
 try{
 await wait();
 check(document.querySelector('#trendChart svg'),'Gráfica inicial');
 check(getComputedStyle(document.querySelector('.ka')).color==='rgb(246, 247, 248)','Tema aislado y legible');
 check(document.querySelector('#kpis').textContent.includes('11'),'Conteos completos del servidor');
 const nav=v=>document.querySelector('[data-view="'+v+'"]').click();
 nav('clientes');await wait();
 check(document.querySelector('#detailTable').textContent.includes('Producto <seguro>'),'Texto seguro en tabla');
 check(!document.querySelector('#detailTable seguro'),'No se inyecta HTML de SAP');
 document.querySelector('#nextPage').click();await wait();
 check(window.calls.at(-1).searchParams.get('pagina')==='2','Paginación servidor');
 const search=document.querySelector('#searchInput');search.value='CLIENTE';search.dispatchEvent(new Event('input'));await wait();
 check(window.calls.at(-1).searchParams.get('buscar')==='CLIENTE','Búsqueda servidor');
 nav('perfil-agente');await wait();
 const select=document.querySelector('#profileAgent');
 check(select.options.length===3,'Agentes con mismo nombre conservan identidad');
 select.value='FAES|1';select.dispatchEvent(new Event('change'));await wait();
 check(window.calls.at(-1).searchParams.get('agente')==='FAES|1','Perfil por código y empresa');
 check(document.querySelector('#profileKpis').textContent.includes('13'),'Perfil con conteos completos');
 document.querySelector('#profileDetails').click();await wait();
 check(window.calls.at(-1).searchParams.get('agente')==='FAES|1','Explorar productos del agente');
 const currency=document.querySelector('#currency');currency.value='USD';currency.dispatchEvent(new Event('change'));await wait();
 check(window.calls.some(u=>u.searchParams.get('moneda')==='USD'),'Moneda enviada al servidor');
 for(const v of ['general','agentes','productos','producto-agente','dashboard']){nav(v);await wait();}
 document.querySelector('#printBtn').click();check(window.printCalled,'Impresión habilitada');
 document.body.setAttribute('data-smoke','PASS');
 }catch(e){document.body.setAttribute('data-smoke','FAIL: '+e.message+' · '+document.body.dataset.jsError);}
})();
</script>
'@
$html = '<!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="'+$css+'"><style>body{margin:0;padding:16px;background:#eef2f7}</style></head><body>'+$markup+$mock+'<script>'+$appSource+'</script>'+$smoke+'</body></html>'
$fixture = Join-Path $taskDir 'analytics.html'
Set-Content -LiteralPath $fixture -Value $html -Encoding UTF8
$uri = ([uri]$fixture).AbsoluteUri
$profile = Join-Path $taskDir 'edge-profile'
$ErrorActionPreference = 'Continue'
$dom = & $edge --headless --disable-gpu --virtual-time-budget=10000 --dump-dom "--user-data-dir=$profile" $uri 2>$null | Out-String
$ErrorActionPreference = 'Stop'
if ($dom -notmatch 'data-smoke="PASS"') { throw ('Prueba de navegador falló. ' + ([regex]::Match($dom,'data-smoke="[^"]*"')).Value + ' Fixture: '+$fixture) }
Write-Output 'OK Analytics navegador: siete vistas, perfil, identidad, filtros, paginación, moneda, texto seguro e impresión.'
Write-Output ("Fixture visual: " + $fixture)
