(function () {
    'use strict';
    var app = document.getElementById('crmApp');
    var empresa = document.getElementById('Empresa');
    var agente = document.getElementById('CodigoOperador');
    var catalogo = document.getElementById('crmAgentesCatalogo');

    function actualizarEmpresa() {
        if (!empresa) return;
        if (app) app.setAttribute('data-empresa', empresa.value || 'BOLIK');
        if (!agente || !catalogo) return;
        var seleccionado = agente.getAttribute('data-selected') || agente.value;
        agente.innerHTML = '';
        var inicial = document.createElement('option');
        inicial.value = '';
        inicial.textContent = 'Seleccione un agente';
        agente.appendChild(inicial);
        Array.prototype.forEach.call(catalogo.querySelectorAll('[data-empresa]'), function (item) {
            if (item.getAttribute('data-empresa') !== empresa.value) return;
            var option = document.createElement('option');
            option.value = item.getAttribute('data-codigo');
            option.textContent = item.getAttribute('data-nombre');
            if (option.value === seleccionado) option.selected = true;
            agente.appendChild(option);
        });
        agente.removeAttribute('data-selected');
    }
    if (empresa) { empresa.addEventListener('change', actualizarEmpresa); actualizarEmpresa(); }

    var stepNav = document.querySelector('.crm-step-nav');
    if (stepNav && app) {
        function updateStepOffset() {
            var navbar = document.querySelector('.app-navbar');
            var height = navbar ? Math.ceil(navbar.getBoundingClientRect().height) : 54;
            app.style.setProperty('--crm-shell-height', height + 'px');
        }
        window.addEventListener('resize', updateStepOffset);
        updateStepOffset();
    }

    function filas(tipo) {
        return document.querySelectorAll('[data-row="' + tipo + '"]');
    }
    function renumerar(tipo) {
        Array.prototype.forEach.call(filas(tipo), function (fila, i) {
            Array.prototype.forEach.call(fila.querySelectorAll('[name]'), function (control) {
                control.name = control.name.replace(/\[\d+\]/, '[' + i + ']');
            });
            if (tipo === 'direccion') {
                var titulo = fila.querySelector('.crm-repeat-top strong');
                if (titulo) titulo.textContent = 'Ubicación ' + (i + 1);
            }
        });
    }
    function input(nombre, etiqueta, tipo) {
        var contenedor = document.createElement('div');
        contenedor.className = 'crm-field';
        var label = document.createElement('label');
        label.textContent = etiqueta;
        var control = document.createElement('input');
        control.className = 'form-control';
        control.type = tipo || 'text';
        control.name = nombre;
        contenedor.appendChild(label);
        contenedor.appendChild(control);
        return contenedor;
    }
    function nuevoContacto() {
        var destino = document.getElementById('crmContactosRows');
        if (!destino || filas('contacto').length >= 30) return;
        var i = filas('contacto').length;
        var fila = document.createElement('div');
        fila.className = 'crm-repeat-row';
        fila.setAttribute('data-row', 'contacto');
        ['Area', 'Nombre', 'Puesto', 'Telefono', 'Correo'].forEach(function (campo) {
            fila.appendChild(input('Ficha.Contactos[' + i + '].' + campo,
                campo === 'Area' ? 'Área' : campo === 'Telefono' ? 'Teléfono' : campo,
                campo === 'Correo' ? 'email' : 'text'));
        });
        var quitar = document.createElement('button');
        quitar.type = 'button'; quitar.className = 'crm-remove';
        quitar.setAttribute('aria-label', 'Quitar contacto');
        quitar.textContent = 'Quitar'; fila.appendChild(quitar);
        destino.appendChild(fila);
        fila.querySelector('[name$=".Nombre"]').focus();
    }
    function nuevoDireccion() {
        var destino = document.getElementById('crmDireccionesRows');
        if (!destino || filas('direccion').length >= 20) return;
        var i = filas('direccion').length;
        var fila = document.createElement('div');
        fila.className = 'crm-address-row'; fila.setAttribute('data-row', 'direccion');
        var cabeza = document.createElement('div'); cabeza.className = 'crm-repeat-top';
        var titulo = document.createElement('strong'); titulo.textContent = 'Ubicación ' + (i + 1);
        var quitar = document.createElement('button'); quitar.type = 'button';
        quitar.className = 'crm-remove'; quitar.textContent = 'Quitar';
        cabeza.appendChild(titulo); cabeza.appendChild(quitar); fila.appendChild(cabeza);
        var grid = document.createElement('div'); grid.className = 'crm-grid';
        [['Nombre', 'Nombre', 'crm-col-4'], ['Modalidad', 'Modalidad', 'crm-col-4'],
         ['Referencia', 'Referencia', 'crm-col-4'], ['Direccion', 'Dirección completa', 'crm-col-12'],
         ['HorarioSemana', 'Horario entre semana', 'crm-col-5'],
         ['HorarioFinSemana', 'Horario fin de semana', 'crm-col-5']].forEach(function (dato) {
            var campo = input('Ficha.Direcciones[' + i + '].' + dato[0], dato[1]);
            campo.className += ' ' + dato[2];
            if (dato[0] === 'Modalidad') {
                var antiguo = campo.querySelector('input');
                var select = document.createElement('select');
                select.className = 'form-control'; select.name = antiguo.name;
                ['Despacho', 'Cliente recoge', 'Transporte de envío'].forEach(function (valor) {
                    var o = document.createElement('option'); o.value = valor; o.textContent = valor;
                    select.appendChild(o);
                });
                campo.replaceChild(select, antiguo);
            }
            grid.appendChild(campo);
        });
        var opciones = document.createElement('div'); opciones.className = 'crm-field crm-col-2 crm-check';
        [['RequiereCita', 'Requiere cita', false], ['Activa', 'Activa', true]].forEach(function (dato) {
            var label = document.createElement('label');
            var check = document.createElement('input'); check.type = 'checkbox'; check.value = 'true';
            check.name = 'Ficha.Direcciones[' + i + '].' + dato[0]; check.checked = dato[2];
            label.appendChild(check); label.appendChild(document.createTextNode(' ' + dato[1]));
            opciones.appendChild(label);
        });
        grid.appendChild(opciones); fila.appendChild(grid); destino.appendChild(fila);
        fila.querySelector('[name$=".Nombre"]').focus();
    }
    Array.prototype.forEach.call(document.querySelectorAll('[data-add]'), function (boton) {
        boton.addEventListener('click', function () {
            if (boton.getAttribute('data-add') === 'contacto') nuevoContacto();
            else nuevoDireccion();
        });
    });
    document.addEventListener('click', function (e) {
        var boton = e.target.closest ? e.target.closest('.crm-remove') : null;
        if (!boton) return;
        var fila = boton.closest('[data-row]');
        if (!fila) return;
        var tipo = fila.getAttribute('data-row');
        fila.parentNode.removeChild(fila);
        renumerar(tipo);
    });
    if (document.getElementById('crmContactosRows') && filas('contacto').length === 0) {
        nuevoContacto();
    }
    if (document.getElementById('crmDireccionesRows') && filas('direccion').length === 0) nuevoDireccion();

    var condicion = document.getElementById('crmCondicionPago');
    var cambio = document.getElementById('crmCambioRazon');
    function actualizarDocumentos() {
        var requerido = (condicion && condicion.value === 'CREDITO') || (cambio && cambio.checked);
        Array.prototype.forEach.call(document.querySelectorAll('.crm-credit-file'), function (campo) {
            campo.classList.toggle('crm-file-highlight', !!requerido);
        });
    }
    if (condicion) condicion.addEventListener('change', actualizarDocumentos);
    if (cambio) cambio.addEventListener('change', actualizarDocumentos);
    actualizarDocumentos();

    var todos = document.getElementById('crmSeleccionarTodos');
    if (todos) todos.addEventListener('change', function () {
        Array.prototype.forEach.call(document.querySelectorAll('#crmExportForm input[name="ids"]'),
            function (c) { c.checked = todos.checked; });
    });
    var exportar = document.getElementById('crmExportForm');
    if (exportar) exportar.addEventListener('submit', function (e) {
        if (!exportar.querySelector('input[name="ids"]:checked')) {
            e.preventDefault(); window.alert('Seleccione al menos una solicitud.');
        }
    });
})();
