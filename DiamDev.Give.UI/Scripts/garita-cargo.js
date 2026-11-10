(function () {
    'use strict';
    var dialog=document.getElementById('cargo-document-dialog');
    if(!dialog) return;
    var close=document.getElementById('cargo-dialog-close');
    var noPhoto=document.getElementById('cargo-dialog-no-photo');
    var photos=[
        {figure:document.getElementById('cargo-dialog-photo-doc'),key:'fotoDoc'},
        {figure:document.getElementById('cargo-dialog-photo-client'),key:'fotoCliente'}
    ];
    var lastTrigger=null;
    function put(id,value){document.getElementById(id).textContent=value || 'Sin registrar';}
    Array.prototype.forEach.call(document.querySelectorAll('.cargo-document'),function(button){
        button.addEventListener('click',function(){
            var d=button.dataset;
            lastTrigger=button;
            put('cargo-dialog-title',d.documento || 'Documento sin número');
            put('cargo-dialog-subtitle',(d.tipo || 'Documento')+' · '+(d.empresa || 'Empresa sin registrar'));
            var result=document.getElementById('cargo-dialog-result');
            result.textContent=d.resultado || 'PENDIENTE';
            result.className='cargo-result cargo-result-'+(['delivered','warning'].indexOf(d.clase)>=0?d.clase:'pending');
            put('cargo-dialog-source',d.fuente);
            ['cliente','direccion','bultos','visito','fecha','fel','monto','motivo','observacion'].forEach(function(field){
                put('cargo-dialog-'+field,d[field]);
            });
            put('cargo-dialog-horas',(d.entrada || 'Sin registrar')+' / '+(d.salida || 'Sin registrar'));
            photos.forEach(function(photo){
                var image=photo.figure.querySelector('img');
                photo.figure.hidden=!d[photo.key];
                if(d[photo.key]) image.src=d[photo.key]; else image.removeAttribute('src');
            });
            noPhoto.hidden=photos.some(function(photo){return !photo.figure.hidden;});
            noPhoto.textContent='Sin fotografías adjuntas.';
            dialog.showModal();close.focus();
        });
    });
    photos.forEach(function(photo){
        photo.figure.querySelector('img').addEventListener('error',function(){
            photo.figure.hidden=true;
            noPhoto.hidden=photos.some(function(item){return !item.figure.hidden;});
            noPhoto.textContent='No se pudo cargar la fotografía. Vuelve a abrir la ruta.';
        });
    });
    close.addEventListener('click',function(){dialog.close();if(lastTrigger) lastTrigger.focus();});
    dialog.addEventListener('click',function(event){if(event.target===dialog){dialog.close();if(lastTrigger) lastTrigger.focus();}});
    dialog.addEventListener('close',function(){
        photos.forEach(function(photo){photo.figure.querySelector('img').removeAttribute('src');});
        if(lastTrigger) lastTrigger.focus();
    });
})();
