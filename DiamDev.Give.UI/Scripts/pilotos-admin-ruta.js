(function () {
    'use strict';
    var form=document.getElementById('route-admin-form'),dialog=document.getElementById('route-admin-dialog');
    if(!form || !dialog || !document.getElementById('route-admin-submit')) return;
    var submit=document.getElementById('route-admin-submit'),confirm=document.getElementById('route-admin-confirm');
    form.addEventListener('submit',function(event){
        if(form.dataset.confirmed==='true') return;
        event.preventDefault();
        if(!form.reportValidity()) return;
        dialog.showModal();
        document.getElementById('route-admin-cancel').focus();
    });
    document.getElementById('route-admin-cancel').addEventListener('click',function(){dialog.close();submit.focus();});
    confirm.addEventListener('click',function(){
        dialog.close();form.dataset.confirmed='true';
        confirm.disabled=true;submit.disabled=true;submit.textContent='Guardando ruta…';
        form.requestSubmit();
    });
})();
