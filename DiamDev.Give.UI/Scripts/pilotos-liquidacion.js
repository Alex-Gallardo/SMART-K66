(function(){'use strict';
 var checks=[].slice.call(document.querySelectorAll('.liq-route-check'));
 var exportForm=document.getElementById('liq-export-form');
 if(exportForm&&checks.length){
  var key='liquidacion-rutas:'+location.pathname+'?'+[document.getElementById('liq-buscar')&&document.getElementById('liq-buscar').value,document.getElementById('liq-desde')&&document.getElementById('liq-desde').value,document.getElementById('liq-hasta')&&document.getElementById('liq-hasta').value,!!document.querySelector('.liq-switch input:checked')].join('|');
  var selected=[];try{selected=JSON.parse(sessionStorage.getItem(key)||'[]');if(!Array.isArray(selected))selected=[];}catch(e){selected=[];}
  selected=selected.filter(function(id){return typeof id==='string'&&id.length>0&&id.length<=15;}).slice(0,100);
  var all=document.getElementById('liq-select-visible'),count=document.getElementById('liq-selected-count'),button=document.getElementById('liq-export-button');
  function save(){try{sessionStorage.setItem(key,JSON.stringify(selected));}catch(e){}refresh();}
  function refresh(){checks.forEach(function(c){c.checked=selected.indexOf(c.value)!==-1;});if(all)all.checked=checks.length>0&&checks.every(function(c){return c.checked;});count.textContent=selected.length+' seleccionadas';button.disabled=selected.length===0;}
  checks.forEach(function(c){c.addEventListener('change',function(){var i=selected.indexOf(c.value);if(c.checked&&i<0){if(selected.length>=100){c.checked=false;alert('Selecciona hasta 100 rutas por reporte.');return;}selected.push(c.value);}else if(!c.checked&&i>=0)selected.splice(i,1);save();});});
  if(all)all.addEventListener('change',function(){checks.forEach(function(c){var i=selected.indexOf(c.value);if(all.checked&&i<0&&selected.length<100)selected.push(c.value);else if(!all.checked&&i>=0)selected.splice(i,1);});save();});
  exportForm.addEventListener('submit',function(e){if(!selected.length){e.preventDefault();return;}checks.forEach(function(c){c.removeAttribute('form');});[].slice.call(exportForm.querySelectorAll('.liq-generated-id')).forEach(function(x){x.remove();});selected.forEach(function(id){var input=document.createElement('input');input.type='hidden';input.name='ids';input.value=id;input.className='liq-generated-id';exportForm.appendChild(input);});});
  refresh();
 }
 var modal=document.getElementById('liq-confirm-modal');if(!modal)return;
 var pending=null,accept=document.getElementById('liq-modal-accept'),cancel=document.getElementById('liq-modal-cancel'),message=document.getElementById('liq-modal-message');
 [].slice.call(document.querySelectorAll('.liq-confirm-form')).forEach(function(form){form.addEventListener('submit',function(e){if(form.dataset.confirmed==='true')return;e.preventDefault();pending=form;message.textContent=form.getAttribute('data-confirm')||'¿Confirmas la liquidación?';accept.textContent=form.getAttribute('data-confirm-action')||'Confirmar';modal.hidden=false;cancel.focus();});});
 function close(){modal.hidden=true;pending=null;}
 cancel.addEventListener('click',close);modal.addEventListener('click',function(e){if(e.target===modal)close();});document.addEventListener('keydown',function(e){if(e.key==='Escape'&&!modal.hidden)close();});
 accept.addEventListener('click',function(){if(!pending)return;var form=pending;modal.hidden=true;pending=null;form.dataset.confirmed='true';accept.disabled=true;form.submit();});
})();
