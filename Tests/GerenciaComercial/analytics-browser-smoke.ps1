$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
if (-not (Test-Path -LiteralPath $edge)) { throw 'Se requiere Microsoft Edge para la prueba visual.' }
$taskDir = Join-Path $env:TEMP ('K66AnalyticsSmoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskDir | Out-Null
$view = Get-Content -LiteralPath (Join-Path $project 'DiamDev.Give.UI\Views\GerenciaComercial\Index.cshtml') -Raw -Encoding UTF8
$markup = $view.Substring($view.IndexOf('<div class="gca"'))
$markup = $markup.Substring(0, $markup.IndexOf('@section scripts'))
$markup = [regex]::Replace($markup, '@Url.Action\("(\w+)"\)', '/$1')
$css = ([uri](Join-Path $project 'DiamDev.Give.UI\Content\gerencia-comercial-analytics.css')).AbsoluteUri
$appSource = Get-Content -LiteralPath (Join-Path $project 'DiamDev.Give.UI\Scripts\App\gerencia-comercial-analytics.js') -Raw -Encoding UTF8
$mock = @'
<script>
window.calls=[];
window.addEventListener('error',e=>document.body.dataset.jsError=e.message);
window.addEventListener('unhandledrejection',e=>document.body.dataset.jsError=String(e.reason));
const agents=[{company:'FAES',agent:'Nombre repetido',agentKey:'FAES|1',agentCode:'1',current:150000000.25,previous:100000000.50},{company:'BOLIK',agent:'Nombre repetido',agentKey:'BOLIK|2',agentCode:'2',current:40000000,previous:80000000}];
const product={company:'FAES',agent:'Nombre repetido',agentKey:'FAES|1',code:'P1',name:'Producto <seguro>',unit:'CAJA',current:150000000.25,previous:100000000.50,qtyCurrent:3,qtyPrevious:2,participation:100,diff:50,growth:50,total:51};
window.fetch=async function(address){
 const u=new URL(address,'http://local');window.calls.push(u);
 const counts=[{clients:11,products:13}],trend=[{company:'FAES',month:1,current:150000000.25,previous:100000000.50}];
 if(window.failNext){window.failNext=false;return {ok:false,redirected:false,json:async()=>({error:'Error de prueba'})};}
 let data;
 if(window.emptyNext&&u.pathname.includes('Detalle')){window.emptyNext=false;return {ok:true,redirected:false,json:async()=>({rows:[],total:0})};}
 if(u.pathname.includes('Panel'))data={general:[{company:'FAES',type:'LOCAL',current:150000000.25,previous:100000000.50},{company:'BOLIK',type:'LOCAL',current:40000000,previous:80000000}],agentes:agents,productos:[product],tendencia:trend,conteos:counts,actualizado:'Prueba local'};
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
 check(getComputedStyle(document.querySelector('.gca')).color==='rgb(15, 23, 42)','Texto del tema claro');
 check(getComputedStyle(document.querySelector('.gca-kpi')).backgroundColor==='rgb(255, 255, 255)','KPI blanco');
 check(!document.querySelector('.gca .main-content,.gca .sidebar,.gca .app-shell,.gca .btn,.gca .panel,.gca .search-box'),'Clases sin colisión con el layout');
 const geometry=()=>{
 const root=document.querySelector('.gca'),main=document.querySelector('.gca-main-content'),outer=document.querySelector('#main-content');
 for(const amount of document.querySelectorAll('.gca-kpi-amount'))check(amount.scrollWidth<=amount.clientWidth+1,'Importe completo dentro del KPI');
 const a=root.getBoundingClientRect(),b=main.getBoundingClientRect(),container=document.querySelector('.app-content-container').getBoundingClientRect();
 check(getComputedStyle(main).marginLeft==='0px','Sin segundo margen de menú');
 check(Math.abs(a.left-b.left)<2,'Contenido alineado con el módulo');
 check(a.left>=container.left-1&&a.right<=container.right+1,'Módulo dentro del contenedor');
 check(document.documentElement.scrollWidth<=document.documentElement.clientWidth+1,'Sin desbordamiento de la página');
 check(parseFloat(getComputedStyle(outer).marginLeft)===(window.innerWidth<=1024?0:window.layoutCase.compact?72:248),'Menú global conserva su margen');
 const visible=[...document.querySelectorAll('.gca input:not([disabled]),.gca select:not([disabled]),.gca .gca-btn')].filter(e=>e.getClientRects().length);
 check(visible.every(e=>e.getBoundingClientRect().right<=a.right+1),'Controles dentro de su componente');
 };
 geometry();
 const decline=document.querySelector('.gca-pct-tag.negative');
 check(decline&&getComputedStyle(decline).color==='rgb(184, 62, 73)','Caídas con color rojo');
 document.querySelector('[data-view="dashboard"]').focus();check(document.activeElement.dataset.view==='dashboard','Navegación por teclado');
 check(document.querySelector('#kpis').textContent.includes('11'),'Conteos completos del servidor');
 const nav=v=>{document.querySelector('[data-view="'+v+'"]').click();geometry();};
 nav('clientes');await wait();document.querySelector('#detailFilters').open=true;geometry();check(document.querySelector('#filterProduct').closest('label').hidden,'Filtros no aplicables ocultos');document.querySelector('#detailFilters').open=false;
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
 check(document.querySelector('#profileKpis').textContent.includes('13'),'Perfil con conteos completos');geometry();
 document.querySelector('#profileDetails').click();await wait();
 check(window.calls.at(-1).searchParams.get('agente')==='FAES|1','Explorar productos del agente');
 const currency=document.querySelector('#currency');currency.value='USD';currency.dispatchEvent(new Event('change'));await wait();
 check(window.calls.some(u=>u.searchParams.get('moneda')==='USD'),'Moneda enviada al servidor');
 window.emptyNext=true;nav('productos');await wait();check(document.querySelector('#detailTable').textContent.includes('No hay resultados'),'Estado vacío claro');check(document.querySelector('#nextPage').disabled,'Paginación vacía deshabilitada');
 window.failNext=true;currency.value='GTQ';currency.dispatchEvent(new Event('change'));await wait();check(document.querySelector('#exportBtn').disabled,'Exportación deshabilitada si falla la consulta');check(document.querySelector('#periodLabel').textContent==='Datos no disponibles','Error persistente visible');
 currency.value='USD';currency.dispatchEvent(new Event('change'));await wait();
 check(document.querySelector('#message').hidden,'Aviso anterior se limpia al recuperar datos');
 for(const v of ['general','agentes','productos','producto-agente','dashboard']){nav(v);await wait();}
 geometry();document.querySelector('#printBtn').click();check(window.printCalled,'Impresión habilitada');
 document.body.setAttribute('data-smoke','PASS');
 }catch(e){document.body.setAttribute('data-smoke','FAIL: '+e.message+' · '+document.body.dataset.jsError);}
})();
</script>
'@
$layout = Get-Content -LiteralPath (Join-Path $project 'DiamDev.Give.UI\Views\Shared\_Layout.cshtml') -Raw -Encoding UTF8
$links = ''
foreach ($match in [regex]::Matches($layout.Substring(0, $layout.IndexOf('    @if')), '<link[^>]*href="(~/Content/[^"?]+)(?:\?[^"]*)?"[^>]*>')) {
    $relative = $match.Groups[1].Value.Substring(2).Replace('/', '\')
    $asset = Join-Path (Join-Path $project 'DiamDev.Give.UI') $relative
    if (Test-Path -LiteralPath $asset) { $links += '<link rel="stylesheet" href="'+([uri]$asset).AbsoluteUri+'">' }
}
$shell = @'
<div class="navbar navbar-inverse navbar-fixed-top app-navbar"><div class="container app-navbar-container"><div class="navbar-header"><a class="app-brand" href="#"><strong class="app-brand-name">Smart K66</strong><span class="app-brand-context">APP WEB</span></a></div></div></div>
<div class="main-container"><aside id="app-sidebar" class="main-navigation app-sidebar"><ul class="main-navigation-menu"><li><a href="#"><i class="icon-home"></i> Inicio</a></li><li class="active"><a href="#">Gerencia Comercial</a></li><li><a href="#">Clientes</a></li></ul></aside><main class="main-content" id="main-content"><div class="container app-content-container"><div class="app-page-heading"><ol class="breadcrumb app-breadcrumb"><li>Inicio</li><li class="active">Gerencia Comercial</li></ol><div class="page-header app-page-header"><h1>Gerencia Comercial <small>Panel de consulta</small></h1></div></div>
'@
$setup = @'
<script>document.body.classList.toggle('navigation-small',!!window.layoutCase?.compact);if(window.layoutCase?.zoom===125)document.documentElement.style.zoom='1.25';</script>
'@
$html = '<!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'+$links+'<link rel="stylesheet" href="'+$css+'"></head><body class="app-shell">'+$setup+$shell+$markup+'</div></main></div>'+$mock+'<script>'+$appSource+'</script>'+$smoke+'</body></html>'
$fixture = Join-Path $taskDir 'analytics.html'
Set-Content -LiteralPath $fixture -Value $html -Encoding UTF8
$node = Get-Command node -ErrorAction Stop
& $node.Source (Join-Path $PSScriptRoot 'analytics-layout-browser.test.js') $fixture $taskDir
if ($LASTEXITCODE -ne 0) { throw 'Falló la integración visual con el layout SMART-K66.' }
Write-Output ("Fixture y capturas: " + $taskDir)
