(function(){
/* Adaptación de Alex-Gallardo/analytics-agents-k66 para SMART-K66. Datos de servidor. */
'use strict';
const root=document.getElementById('k66-analytics'); if(!root)return;
const $=s=>root.querySelector(s); const $$=s=>[...root.querySelectorAll(s)];
const money = n => state.currency+' ' + Number(n||0).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});
const shortMoney = n => state.currency+' '+(Math.abs(n)>=1e6?(n/1e6).toFixed(2)+'M':Math.abs(n)>=1000?(n/1000).toFixed(1)+'K':n.toFixed(0));
const number = n => Number(n||0).toLocaleString('en-US',{maximumFractionDigits:2});
const pct = n => (n===null?'N/D':(n>=0?'+':'')+n.toFixed(2)+'%');
const safe = s => String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const companies=['GRACO','FAES','BOLIK'];
const state={profileAgent:'',profileSort:'current',profileDesc:true,view:'dashboard',company:'all',currency:'GTQ',origen:false,from:'',to:'',search:'',sort:null,sortDesc:true,page:1,pageSize:15,agent:'',client:'',code:'',product:''};
const months=['Ene','Feb','Mar','Abr','May','Jun','Jul','Ago','Sep','Oct','Nov','Dic'];

const liveData={general:[],agentes:[],productos:[],tendencia:[],clientes:[],"producto-agente":[],"agente-tendencia":[]};
let liveLoaded=false,requestSerial=0,detailSerial=0,profileSerial=0,counts=[],profileCounts=[];
const agentView='agentes',customerView='clientes',productView='productos',productAgentView='producto-agente';
function filtered(view){return liveData[view]||[];}
function query(extra={}){return new URLSearchParams({empresa:state.company,desde:state.from,hasta:state.to,moneda:state.currency,origen:String(state.origen),...extra});}
async function request(kind,params={}){
 const response=await fetch(root.dataset[kind]+'?'+query(params),{credentials:'same-origin',cache:'no-store',headers:{Accept:'application/json'}});
 if(response.redirected&&/login/i.test(response.url)){window.location.href=response.url;throw Error('La sesión terminó.');}
 const data=await response.json(); if(!response.ok)throw Error(data.error||'No fue posible consultar SAP.'); return data;
}
function normalize(rows){return (rows||[]).map(r=>({...r,current:Number(r.current||0),previous:Number(r.previous||0),qtyCurrent:Number(r.qtyCurrent||0),qtyPrevious:Number(r.qtyPrevious||0),month:Number(r.month||0)}));}
async function loadSales(){
 const serial=++requestSerial;detailSerial++;profileSerial++;liveLoaded=false;$('#applyBtn').disabled=true;$('#exportBtn').disabled=true;root.setAttribute('aria-busy','true');$('#periodLabel').textContent='Consultando información comercial…';
 try{
 const data=await request('panel');if(serial!==requestSerial)return;
 ['general','agentes','productos','tendencia'].forEach(k=>liveData[k]=normalize(data[k]));counts=data.conteos||[];liveLoaded=true;clearTimeout(toast.timer);$('#message').hidden=true;
 render();$('#periodLabel').textContent='Actualizado: '+data.actualizado;
 }catch(e){if(serial!==requestSerial)return;Object.keys(liveData).forEach(k=>liveData[k]=[]);counts=[];render();$('#periodLabel').textContent='Datos no disponibles';toast(e.message);}
 finally{if(serial===requestSerial){$('#applyBtn').disabled=false;$('#exportBtn').disabled=!liveLoaded;root.setAttribute('aria-busy','false');}}
}
function metricValue(value){value=String(value);return value.startsWith(state.currency+' ')?'<span class="gca-kpi-currency">'+safe(state.currency)+'</span><span class="gca-kpi-amount">'+safe(value.slice(state.currency.length+1))+'</span>':safe(value);}
function metricIcon(value){const icons={'▤':'icon-file-text','▥':'icon-bar-chart','↗':'icon-bar-chart','♧':'icon-group','◇':'icon-th-large'};return '<i class="'+(icons[value]||'icon-bar-chart')+'" aria-hidden="true"></i>';}
function total(rows,key){return rows.reduce((s,r)=>s+Number(r[key]||0),0)}
function change(row){return row.current-row.previous}
function growth(row){return row.previous===0?null:100*(row.current-row.previous)/Math.abs(row.previous)}
function getGeneral(){return liveData.general.filter(r=>state.company==='all'||r.company===state.company)}
function companyName(v){return v==='FAES'?'FAES / ESCOCESA':v}
function renderKpis(){const d=getGeneral();const a=total(d,'current'),b=total(d,'previous'),g=b?100*(a-b)/Math.abs(b):null;const clients=number(total(counts,'clients'));const products=number(total(counts,'products'));const cards=[['▤','Venta total año actual',money(a),pct(g)+' vs. año anterior'],['▥','Venta total año anterior',money(b),'Período comparable'],['↗','Crecimiento',money(a-b),pct(g)+' de variación'],['♧','Clientes con movimientos',clients,'Período seleccionado'],['◇','Productos vendidos',products,'Período seleccionado']];$('#kpis').innerHTML=cards.map((c,i)=>`<article class="gca-kpi"><div class="gca-kpi-head"><span class="gca-kpi-icon">${metricIcon(c[0])}</span>${c[1]}</div><div class="gca-kpi-value">${metricValue(c[2])}</div><div class="gca-kpi-foot ${i===2?(a-b>=0?'positive':'negative'):''}">${safe(c[3])}</div></article>`).join('')}
function drawTrend(target,rows){
 const periods=[],cursor=new Date(state.from+'T00:00:00');cursor.setDate(1);const finish=new Date(state.to+'T00:00:00');while(cursor<=finish){const m=cursor.getMonth(),key=cursor.getFullYear()+'-'+String(m+1).padStart(2,'0');periods.push({key,month:months[m]+(state.from.slice(0,4)!==state.to.slice(0,4)?' '+cursor.getFullYear():''),number:m+1});cursor.setMonth(cursor.getMonth()+1);}
 const data=periods.map(p=>({month:p.month,current:total(rows.filter(r=>r.monthKey?r.monthKey===p.key:r.month===p.number),'current'),previous:total(rows.filter(r=>r.monthKey?r.monthKey===p.key:r.month===p.number),'previous')}));
 const W=800,H=270,p={l:78,r:20,t:20,b:32},lo=Math.min(0,...data.flatMap(r=>[r.current,r.previous])),hi=Math.max(1,...data.flatMap(r=>[r.current,r.previous]))*1.1;
 const x=i=>p.l+(data.length===1?(W-p.l-p.r)/2:i*(W-p.l-p.r)/(data.length-1)),y=v=>p.t+(H-p.t-p.b)*(hi-v)/(hi-lo);
 const path=k=>data.map((d,i)=>(i?'L':'M')+x(i).toFixed(1)+','+y(d[k]).toFixed(1)).join(' ');
 const grid=[0,.25,.5,.75,1].map(t=>{const v=lo+(hi-lo)*t;return '<line class="gca-gridline" x1="'+p.l+'" x2="'+(W-p.r)+'" y1="'+y(v)+'" y2="'+y(v)+'"/><text class="gca-axis-label" text-anchor="end" x="'+(p.l-8)+'" y="'+(y(v)+3)+'">'+shortMoney(v)+'</text>';}).join('');
 $(target).innerHTML='<svg viewBox="0 0 '+W+' '+H+'" role="img" aria-label="Ventas mensuales actuales y anteriores">'+grid+data.map((d,i)=>'<text class="gca-axis-label" text-anchor="middle" x="'+x(i)+'" y="'+(H-4)+'">'+d.month+'</text>').join('')+'<path d="'+path('previous')+'" fill="none" stroke="#8295a9" stroke-width="2.5" stroke-dasharray="7 7"/><path d="'+path('current')+'" fill="none" stroke="var(--accent)" stroke-width="3.5"/>'+data.map((d,i)=>'<circle cx="'+x(i)+'" cy="'+y(d.current)+'" r="4" fill="var(--accent)"><title>'+safe(d.month+' · '+money(d.current)+' · anterior '+money(d.previous))+'</title></circle>').join('')+'</svg>';
}
function renderTrend(){drawTrend('#trendChart',liveData.tendencia);}
function renderCompanyBars(){const rows=companies.filter(c=>state.company==='all'||state.company===c).map(c=>{let ds=getGeneral().filter(r=>r.company===c);return {name:companyName(c),current:total(ds,'current'),previous:total(ds,'previous')}});let max=Math.max(1,...rows.map(r=>r.current));$('#companyBars').innerHTML=rows.map(r=>`<div class="gca-company-row"><div class="gca-company-info"><strong>${safe(r.name)}</strong><span class="gca-money">${shortMoney(r.current)}</span></div><div class="gca-bar-wrap"><div class="gca-bar-track"><div class="gca-bar-fill" style="width:${Math.max(0,r.current/max*100)}%"></div></div><span class="gca-pct-tag ${r.current<r.previous?'negative':''}">${pct(growth(r))}</span></div></div>`).join('')}
function renderDonut(selector,items){if(items.some(x=>x.value<0)){ $(selector).innerHTML='<p class="gca-subtle">Participación no representable con ventas netas negativas.</p>'+items.map(x=>'<p>'+safe(x.name)+' · '+money(x.value)+'</p>').join('');return;}let sum=items.reduce((s,x)=>s+Math.max(0,x.value),0);const colors=['var(--accent)','rgba(var(--accent-rgb),.70)','rgba(var(--accent-rgb),.40)','#94a3b8'];let offset=0;const stops=items.map((x,i)=>{let start=offset;offset+=sum?Math.max(0,x.value)/sum*100:0;return `${colors[i%colors.length]} ${start}% ${offset}%`}).join(',');$(selector).innerHTML=`<div class="gca-donut-content"><div class="gca-donut" style="background:conic-gradient(${stops||'#666 0% 100%'})"><div class="gca-donut-hole"><strong>${shortMoney(sum)}</strong><span>Ventas netas</span></div></div><div class="gca-donut-legend">${items.map((x,i)=>`<div class="gca-donut-row"><span class="gca-legend-name"><i style="background:${colors[i%colors.length]}"></i>${safe(x.name)}</span><b>${sum?(100*x.value/sum).toFixed(1):'0.0'}%</b></div>`).join('')}</div></div>`}
function renderDonuts(){const d=getGeneral();renderDonut('#companyDonut',companies.filter(c=>state.company==='all'||state.company===c).map(c=>({name:companyName(c),value:total(d.filter(r=>r.company===c),'current')})));renderDonut('#typeDonut',['LOCAL','EXPORTACION'].map(t=>({name:t==='LOCAL'?'Local':'Exportación',value:total(d.filter(r=>r.type===t),'current')})))}
function renderAgents(){const m=new Map();for(const a of filtered(agentView))m.set(a.agent+' · '+a.company+' · '+a.agentCode,a.current);const items=[...m].map(([name,value])=>({name,value})).sort((a,b)=>b.value-a.value).slice(0,5),max=Math.max(1,...items.map(r=>r.value));$('#topAgents').innerHTML=items.map((r,i)=>`<div class="gca-agent-entry"><span class="gca-agent-name" title="${safe(r.name)}">${i+1}. ${safe(r.name)}</span><div class="gca-bar-track"><div class="gca-bar-fill" style="width:${Math.max(0,r.value/max*100)}%"></div></div><span class="gca-agent-amount">${shortMoney(r.value)}</span></div>`).join('')||'<p class="gca-subtle">Sin datos</p>'}
function renderProducts(){let list=filtered(productView).sort((a,b)=>b.current-a.current).slice(0,5);$('#topProducts').innerHTML=list.map((r,i)=>`<div class="gca-rank-item"><span class="gca-rank-num">${i+1}</span><span class="gca-rank-name" title="${safe(r.name)}">${safe(r.name+' · '+r.company+' · '+r.unit)}</span><span class="gca-rank-money">${shortMoney(r.current)}</span></div>`).join('')}
function renderOverview(){let d=getGeneral().slice().sort((a,b)=>b.current-a.current),sum=total(d,'current');$('#overviewTable').innerHTML='<thead><tr>'+['Empresa','Tipo','Año anterior','Año actual','Diferencia','Participación'].map((h,i)=>`<th class="${i>1?'number':''}">${h}</th>`).join('')+'</tr></thead><tbody>'+d.map(r=>`<tr><td>${safe(companyName(r.company))}</td><td>${safe(r.type)}</td><td class="number">${money(r.previous)}</td><td class="number">${money(r.current)}</td><td class="number">${money(change(r))}</td><td class="number">${sum?(r.current/sum*100).toFixed(2):'0.00'}%</td></tr>`).join('')+`<tr class="gca-total-row"><td colspan="2">TOTAL</td><td class="number">${money(total(d,'previous'))}</td><td class="number">${money(sum)}</td><td class="number">${money(sum-total(d,'previous'))}</td><td class="number">100.00%</td></tr></tbody>`}
const columns={
 general:[['company','Empresa'],['type','Tipo'],['previous','Venta año anterior','money'],['current','Venta año actual','money'],['diff','Diferencia','money'],['growth','% Diferencia','pct'],['participation','% Participación','pct'],['rank','Ranking','integer']],
 agentes:[['agent','Agente'],['company','Empresa'],['previous','Venta año anterior','money'],['current','Venta año actual','money'],['diff','Diferencia','money'],['growth','% Crecimiento','pct'],['indicator','Indicador']],
 clientes:[['company','Empresa'],['unit','Unidad'],['code','Cliente código'],['name','Cliente nombre'],['agent','Agente'],['qtyPrevious','Cantidad anterior','number'],['previous','Venta año anterior','money'],['qtyCurrent','Cantidad actual','number'],['current','Venta año actual','money'],['diff','Diferencia','money'],['growth','% Diferencia','pct'],['participation','% Participación','pct']],
 productos:[['unit','Unidad'],['code','Código'],['name','Nombre'],['company','Empresa'],['qtyPrevious','Cantidad anterior','number'],['previous','Venta año anterior','money'],['qtyCurrent','Cantidad actual','number'],['current','Venta año actual','money'],['diff','Diferencia','money'],['growth','% Diferencia','pct'],['participation','% Participación','pct']],
 'producto-agente':[['unit','Unidad'],['code','Código'],['name','Nombre'],['agent','Agente'],['company','Empresa'],['qtyPrevious','Cantidad anterior','number'],['previous','Venta año anterior','money'],['qtyCurrent','Cantidad actual','number'],['current','Venta año actual','money'],['diff','Diferencia','money'],['growth','% Diferencia','pct'],['participation','% Participación','pct']]
};
const info={dashboard:['Resumen comercial','Análisis de ventas por empresa, agentes, clientes y productos'],general:['Venta general','Comparativo por empresa y tipo de venta'],agentes:['Análisis por agente–empresa','Venta neta y crecimiento por agente en las tres empresas'],clientes:['Análisis por cliente','Cantidades y ventas por cliente y agente'],productos:['Análisis por producto','Rendimiento y participación de los productos vendidos'],'producto-agente':['Análisis producto–agente','Ventas por producto y vendedor asignado'],'perfil-agente':['Perfil comercial del agente','Indicadores, evolución de ventas, mejores clientes y productos por agente']};

let detailTimer;
function scheduleDetail(){detailSerial++;clearTimeout(detailTimer);detailTimer=setTimeout(renderDetail,250);}
function detailParams(){return {vista:state.view,pagina:state.page,tamano:state.pageSize,agente:state.agent,cliente:state.client,codigo:state.code,producto:state.product,buscar:state.search,orden:state.sort||'current',descendente:String(state.sortDesc)};}
function format(v,kind){if(v==null)return '—';if(kind==='money')return money(v);if(kind==='number'||kind==='integer')return number(v);if(kind==='pct')return pct(Number(v));return safe(v);}
function populateAdvancedFilters(){
 $('#detailFilters').open=!!(state.agent||state.client||state.code||state.product);
 const cols=columns[state.view]||[],has=k=>cols.some(c=>c[0]===k);
 $('#filterAgent').innerHTML='<option value="">Todos los agentes</option>'+liveData.agentes.map(r=>'<option value="'+safe(r.agentKey)+'">'+safe(r.agent+' · '+r.company+' · '+r.agentCode)+'</option>').join('');
 $('#filterAgent').value=state.agent;
 $('#filterAgent').disabled=!has('agent');
 $('#filterClient').disabled=state.view!=='clientes';$('#filterCode').disabled=!has('code');$('#filterProduct').disabled=!['productos','producto-agente'].includes(state.view);
 ['filterAgent','filterClient','filterCode','filterProduct'].forEach(id=>{const input=$('#'+id);input.closest('label').hidden=input.disabled;});
 $('#sortField').innerHTML=cols.map(([key,title])=>'<option value="'+key+'">'+safe(title)+'</option>').join('');
 if(!cols.some(c=>c[0]===state.sort))state.sort='current';$('#sortField').value=state.sort;$('#sortDirection').value=state.sortDesc?'desc':'asc';
 $('#filtersHint').textContent='Búsqueda, orden y paginación en servidor. Participación sobre el total de cada empresa y moneda, antes de los filtros de detalle. Las cantidades no se suman entre unidades distintas.';
}
async function renderDetail(){const serial=++detailSerial,v=state.view,cols=columns[v]; if(!cols)return;
$('#detailTitle').textContent='Resultados'; $('#detailTable').setAttribute('aria-busy','true');$('#detailTable').innerHTML='<tbody><tr><td>Consultando…</td></tr></tbody>';
try{const data=await request('detail',detailParams());if(serial!==detailSerial||v!==state.view)return;
$('#detailTable').setAttribute('aria-busy','false');const subset=normalize(data.rows),count=Number(data.total||0),pages=Math.max(1,Math.ceil(count/state.pageSize));$('#detailTitle').textContent='Resultados';$('#detailDesc').textContent='Consulta, filtra y ordena los registros del período.';$('#rowCount').textContent=count+' registros';$('#detailTable').innerHTML='<thead><tr>'+cols.map(([k,name,kind])=>`<th class="${['money','number','pct','integer'].includes(kind)?'number':''}" data-sort="${k}" title="Ordenar por ${safe(name)}">${safe(name)} ${state.sort===k?(state.sortDesc?'↓':'↑'):'↕'}</th>`).join('')+'</tr></thead><tbody>'+(subset.length?subset.map(r=>'<tr>'+cols.map(([key,name,kind])=>`<td class="${['money','number','pct','integer'].includes(kind)?'number':''} ${key==='diff'?(r.diff>=0?'positive':'negative'):''} ${key==='indicator'?(r.indicator==='SUBE'?'positive':r.indicator==='BAJA'?'negative':''):''}">${format(r[key],kind)}</td>`).join('')+'</tr>').join(''):`<tr><td colspan="${cols.length}">No hay resultados para estos filtros.</td></tr>` )+'</tbody>';$('#tableInfo').textContent=`Mostrando ${count?((state.page-1)*state.pageSize+1):0}–${Math.min(state.page*state.pageSize,count)} de ${count}`;$('#pageIndicator').textContent=`Página ${state.page} de ${pages}`;$('#prevPage').disabled=state.page===1;$('#nextPage').disabled=state.page===pages;$$('#detailTable th[data-sort]').forEach(el=>el.onclick=()=>{let key=el.dataset.sort;state.sortDesc=state.sort===key?!state.sortDesc:true;state.sort=key;state.page=1;$('#sortField').value=key;$('#sortDirection').value=state.sortDesc?'desc':'asc';renderDetail()})}catch(e){if(serial===detailSerial){$('#detailTable').setAttribute('aria-busy','false');$('#detailTable').innerHTML='<tbody><tr><td>No fue posible cargar esta vista.</td></tr></tbody>';toast(e.message);}}}
function profileItems(kind){
  return liveData[kind].filter(r=>r.agentKey===state.profileAgent&&(state.company==='all'||r.company===state.company));
}
function profileTableRows(){
 const grouped=new Map();
 for(const r of profileItems('producto-agente')){
   const key=r.company+'|'+r.code+'|'+r.unit;
   if(!grouped.has(key))grouped.set(key,{company:r.company,code:r.code,name:r.name,unit:r.unit,current:0,previous:0,qtyCurrent:0,qtyPrevious:0});
   const v=grouped.get(key);for(const col of ['current','previous','qtyCurrent','qtyPrevious'])v[col]+=Number(r[col]||0);
 }
 const result=[...grouped.values()]; const key=state.profileSort,dir=state.profileDesc?-1:1;
 result.sort((x,y)=>dir*(typeof x[key]==='number'&&typeof y[key]==='number'?x[key]-y[key]:String(x[key]??'').localeCompare(String(y[key]??''),'es')));
 return result;
}
function renderProfileTrend(){drawTrend('#profileTrend',liveData['agente-tendencia']);}
function renderProfileRank(selector,items,kind){
 const max=Math.max(1,...items.map(r=>r.current));
 $(selector).innerHTML=items.length?items.map((r,i)=>`<div class="gca-profile-rank"><span class="gca-rank-num">${i+1}</span><div class="gca-profile-rank-info"><strong title="${safe(r.name)}">${safe(r.name)}</strong><div class="gca-bar-track"><div class="gca-bar-fill" style="width:${Math.max(0,r.current/max*100)}%"></div></div><small>${kind==='product'?safe(r.code)+' · '+number(r.qtyCurrent)+' '+safe(r.unit||'Sin unidad'):'Cliente '+safe(r.code)}</small></div><b>${shortMoney(r.current)}</b></div>`).join(''):'<p class="gca-subtle">Sin movimientos en este período.</p>';
}
async function renderAgentProfile(){
 const select=$('#profileAgent');
 select.innerHTML='<option value="">Seleccionar agente</option>'+liveData.agentes.map(r=>'<option value="'+safe(r.agentKey)+'">'+safe(r.agent+' · '+r.company+' · '+r.agentCode)+'</option>').join('');
 if(!liveData.agentes.some(r=>r.agentKey===state.profileAgent))state.profileAgent='';
 select.value=state.profileAgent;$('#profileEmpty').hidden=!!state.profileAgent;$('#profileContent').hidden=true;$('#profileExport').disabled=!state.profileAgent||!liveLoaded;
 if(!state.profileAgent)return;
 const serial=++profileSerial, key=state.profileAgent;try{
 const data=await request('profile',{agente:key});if(serial!==profileSerial||key!==state.profileAgent||state.view!=='perfil-agente')return;
 liveData['agente-tendencia']=normalize(data.tendencia).map(r=>({...r,agentKey:key}));
 liveData.clientes=normalize(data.clientes);liveData['producto-agente']=normalize(data.productos);profileCounts=data.conteos||[];$('#profileContent').hidden=false;
 const sales=profileItems('agentes');const current=total(sales,'current'),previous=total(sales,'previous');
 const clients=profileItems('clientes'),productRows=profileItems('producto-agente');


 $('#profileName').textContent=(liveData.agentes.find(r=>r.agentKey===state.profileAgent)||{}).agent||state.profileAgent;
 $('#profilePeriod').textContent=`${state.from} al ${state.to} · ${state.company==='all'?'Todas las empresas':state.company}`;
 const metrics=[['▤','Venta actual',money(current),'Período seleccionado'],['▥','Venta anterior',money(previous),'Período comparable'],['↗','Crecimiento',pct(growth({current,previous})),money(current-previous)+' de diferencia'],['♧','Clientes con compras',number(total(profileCounts,'clients')),'Clientes distintos por empresa'],['◇','Productos vendidos',number(total(profileCounts,'products')),'Artículos distintos por empresa']];
 $('#profileKpis').innerHTML=metrics.map(([ico,label,value,sub])=>`<article class="gca-kpi"><div class="gca-kpi-head"><span class="gca-kpi-icon">${metricIcon(ico)}</span>${label}</div><div class="gca-kpi-value">${metricValue(value)}</div><div class="gca-kpi-foot">${sub}</div></article>`).join('');
 renderProfileTrend();
 const companySales=companies.filter(c=>state.company==='all'||c===state.company).map(c=>({name:companyName(c),current:total(sales.filter(r=>r.company===c),'current'),previous:total(sales.filter(r=>r.company===c),'previous')}));
 const max=Math.max(1,...companySales.map(r=>r.current));
 $('#profileCompanies').innerHTML=companySales.map(r=>`<div class="gca-company-row"><div class="gca-company-info"><strong>${safe(r.name)}</strong><span class="gca-money">${money(r.current)}</span></div><div class="gca-bar-wrap"><div class="gca-bar-track"><div class="gca-bar-fill" style="width:${Math.max(0,r.current/max*100)}%"></div></div><span class="gca-pct-tag ${r.current<r.previous?'negative':''}">${pct(growth(r))}</span></div></div>`).join('');
 const topClients=[...clients].sort((a,b)=>b.current-a.current).filter(r=>r.current>0).slice(0,10);
 const topProducts=profileTableRows().slice().sort((a,b)=>b.current-a.current).filter(r=>r.current>0).slice(0,10);
 renderProfileRank('#profileCustomers',topClients.map(r=>({...r,name:r.name+' · '+r.company})),'client');
 renderProfileRank('#profileProducts',topProducts,'product');
 const rows=profileTableRows();$('#profileRowCount').textContent='Vista previa · primeros 10 resultados por venta';
 const fields=[['code','Código'],['name','Producto'],['company','Empresa'],['unit','Unidad'],['qtyCurrent','Cantidad'],['previous','Año anterior'],['current','Año actual']];
 $('#profileProductsTable').innerHTML='<thead><tr>'+fields.map(([k,label])=>`<th data-profile-sort="${k}" title="Ordenar por ${label}">${label} ${state.profileSort===k?(state.profileDesc?'↓':'↑'):'↕'}</th>`).join('')+'</tr></thead><tbody>'+rows.map(r=>`<tr><td>${safe(r.code)}</td><td>${safe(r.name)}</td><td>${safe(r.company)}</td><td>${safe(r.unit)}</td><td class="number">${number(r.qtyCurrent)}</td><td class="number">${money(r.previous)}</td><td class="number">${money(r.current)}</td></tr>`).join('')+'</tbody>';
 $$('#profileProductsTable th[data-profile-sort]').forEach(th=>th.onclick=()=>{const key=th.dataset.profileSort;state.profileDesc=state.profileSort===key?!state.profileDesc:true;state.profileSort=key;renderAgentProfile()});
}catch(e){if(serial===profileSerial){$('#profileContent').hidden=true;toast(e.message);}}
}
function render(){$('#exportBtn').hidden=state.view==='perfil-agente';root.dataset.theme=state.company;$$('.gca-company-tabs button').forEach(b=>b.classList.toggle('selected',b.dataset.company===state.company));$$('.gca-nav-link').forEach(b=>{const active=b.dataset.view===state.view;b.classList.toggle('active',active);b.setAttribute('aria-current',active?'page':'false');if(active){const n=b.parentElement,r=b.getBoundingClientRect(),p=n.getBoundingClientRect();if(r.left<p.left)n.scrollLeft-=p.left-r.left;if(r.right>p.right)n.scrollLeft+=r.right-p.right;}});$('#pageTitle').textContent=info[state.view][0];$('#pageSubtitle').textContent=info[state.view][1];$('#dashboardView').classList.toggle('active',state.view==='dashboard');$('#agentProfileView').classList.toggle('active',state.view==='perfil-agente');$('#detailView').classList.toggle('active',state.view!=='dashboard'&&state.view!=='perfil-agente');$('#periodLabel').textContent=`Comparando con ${Number(state.from.slice(0,4))-1}`;if(state.view==='dashboard'){renderKpis();renderTrend();renderCompanyBars();renderDonuts();renderAgents();renderProducts();renderOverview()}else if(state.view==='perfil-agente'){renderAgentProfile()}else{populateAdvancedFilters();renderDetail()}}
function toast(message){const e=$('#message');e.textContent=message;e.hidden=false;clearTimeout(toast.timer);toast.timer=setTimeout(()=>e.hidden=true,5000)}
function exportExcel(){
 if(!liveLoaded)return toast('Cargue los datos antes de exportar.');
 const params=detailParams();params.vista=state.view;
 if(state.view==='dashboard'){params.agente='';params.buscar='';params.cliente='';params.codigo='';params.producto='';}
 if(state.view==='perfil-agente'){if(!state.profileAgent)return toast('Seleccione un agente.');params.agente=state.profileAgent;params.buscar='';params.cliente='';params.codigo='';params.producto='';}
 window.location.href=root.dataset.excel+'?'+query(params);
}
function resetDetailFilters(){state.search='';state.agent='';state.client='';state.code='';state.product='';state.sort='current';state.sortDesc=true;state.page=1;for(const id of ['searchInput','filterClient','filterCode','filterProduct'])$('#'+id).value='';}
$$('.gca-nav-link[data-view]').forEach(b=>b.addEventListener('click',()=>{detailSerial++;profileSerial++;state.view=b.dataset.view;resetDetailFilters();render()}));$$('.gca-company-tabs button').forEach(b=>b.onclick=()=>{state.company=b.dataset.company;state.page=1;loadSales()});$$('[data-goto]').forEach(b=>b.onclick=()=>{$(`.gca-nav-link[data-view="${b.dataset.goto}"]`).click()});$('#applyBtn').onclick=()=>{const from=$('#startDate').value,to=$('#endDate').value;if(!from||!to||from>to)return toast('Revisá el rango: la fecha inicial debe ser anterior o igual a la final.');state.from=from;state.to=to;state.page=1;loadSales()};$('#searchInput').oninput=e=>{state.search=e.target.value;state.page=1;scheduleDetail()};$('#prevPage').onclick=()=>{state.page--;renderDetail()};$('#nextPage').onclick=()=>{state.page++;renderDetail()};$('#exportBtn').onclick=exportExcel;$('#profileExport').onclick=exportExcel;$('#profileAgent').onchange=e=>{state.profileAgent=e.target.value;renderAgentProfile()};
for(const [id,key] of [['filterClient','client'],['filterCode','code'],['filterProduct','product']]){
 $('#'+id).addEventListener('input',e=>{state[key]=e.target.value;state.page=1;scheduleDetail()});
}
$('#filterAgent').addEventListener('change',e=>{state.agent=e.target.value;state.page=1;renderDetail()});
$('#sortField').addEventListener('change',e=>{state.sort=e.target.value;state.page=1;renderDetail()});
$('#sortDirection').addEventListener('change',e=>{state.sortDesc=e.target.value==='desc';state.page=1;renderDetail()});
$('#clearFilters').onclick=()=>{resetDetailFilters();populateAdvancedFilters();renderDetail()};

const now=new Date(),date=d=>d.getFullYear()+'-'+String(d.getMonth()+1).padStart(2,'0')+'-'+String(d.getDate()).padStart(2,'0');
state.from=date(new Date(now.getFullYear(),0,1));state.to=date(now);$('#startDate').value=state.from;$('#endDate').value=state.to;
$('#currency').onchange=e=>{state.currency=e.target.value;loadSales();};$('#coverage').onchange=e=>{state.origen=e.target.value==='true';loadSales();};
$('#profileDetails').onclick=()=>{const agent=state.profileAgent;state.view='producto-agente';resetDetailFilters();state.agent=agent;render();};
$('#printBtn').onclick=()=>{if(!liveLoaded)return toast('Cargue los datos antes de imprimir.');$('#printContext').textContent=(state.company==='all'?'Todas las empresas':state.company)+' · '+state.from+' al '+state.to+' · '+state.currency+' · '+(state.origen?'Cobertura Analytics':'Cobertura completa')+' · Impresión de la vista actual; las tablas muestran su página visible. Excel incluye el detalle filtrado completo.';window.print();};
render();loadSales();

})();
