(function () {
    'use strict';
    function confirmar(formId,dialogId,submitId,confirmId,cancelId){
        var form=document.getElementById(formId),dialog=document.getElementById(dialogId);
        if(!form || !dialog) return;
        var submit=document.getElementById(submitId),confirm=document.getElementById(confirmId),cancel=document.getElementById(cancelId);
        form.addEventListener('submit',function(event){
            if(form.dataset.confirmed==='true') return;
            event.preventDefault();
            if(!form.reportValidity()) return;
            dialog.showModal();cancel.focus();
        });
        cancel.addEventListener('click',function(){dialog.close();submit.focus();});
        confirm.addEventListener('click',function(){
            dialog.close();form.dataset.confirmed='true';confirm.disabled=true;submit.disabled=true;
            submit.textContent='Guardando…';form.requestSubmit();
        });
    }
    confirmar('route-admin-form','route-admin-dialog','route-admin-submit','route-admin-confirm','route-admin-cancel');
    confirmar('document-admin-form','document-admin-dialog','document-admin-submit','document-admin-confirm','document-admin-cancel');
    var removeDialog=document.getElementById('rwe-delete-dialog'),removeForm=document.getElementById('rwe-delete-form');
    if(removeDialog && removeForm){
        Array.prototype.forEach.call(document.querySelectorAll('.rwe-delete-trigger'),function(button){
            button.addEventListener('click',function(){
                removeForm.elements.rowId.value=button.dataset.row;
                removeForm.elements.version.value=button.dataset.version;
                document.getElementById('rwe-delete-number').textContent=button.dataset.doc || '#'+button.dataset.row;
                removeDialog.showModal();document.getElementById('rwe-delete-reason').focus();
            });
        });
        document.getElementById('rwe-delete-cancel').addEventListener('click',function(){removeDialog.close();});
        removeForm.addEventListener('submit',function(){
            var button=document.getElementById('rwe-delete-confirm');button.disabled=true;button.textContent='Retirando…';
        });
    }
})();
