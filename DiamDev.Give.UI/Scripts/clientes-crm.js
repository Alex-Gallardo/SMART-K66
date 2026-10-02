(function () {
    'use strict';
    var app = document.getElementById('crmApp');
    var empresa = document.getElementById('Empresa');
    var agente = document.getElementById('CodigoOperador');
    var catalogo = document.getElementById('crmAgentesCatalogo');
    var stepNav = document.querySelector('.crm-step-nav');
    var wizard = stepNav && stepNav.getAttribute('data-wizard') === 'true';
    var pasoActual = wizard ? parseInt(stepNav.getAttribute('data-paso'), 10) : 0;

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

    if (stepNav && app) {
        function updateStepOffset() {
            var navbar = document.querySelector('.app-navbar');
            var height = navbar ? Math.ceil(navbar.getBoundingClientRect().height) : 54;
            app.style.setProperty('--crm-shell-height', height + 'px');
        }
        window.addEventListener('resize', updateStepOffset);
        updateStepOffset();
    }

    if (wizard) {
        var secciones = ['crm-identidad', 'crm-contactos', 'crm-direcciones',
            'crm-perfil', 'crm-desarrollo', 'crm-documentos'];
        Array.prototype.forEach.call(document.querySelectorAll('.crm-editor section[id^="crm-"]'), function (seccion) {
            seccion.hidden = seccion.id !== secciones[pasoActual - 1];
            Array.prototype.forEach.call(seccion.querySelectorAll('.form-control'), function (control) {
                control.required = !seccion.hidden && control.type !== 'file' && control.id !== 'crmSapFiltro';
            });
        });
        var documentos = document.getElementById('crm-documentos');
        if (pasoActual === 6 && documentos) {
            Array.prototype.forEach.call(documentos.querySelectorAll('input[type="file"]'), function (control) {
                control.required = control.getAttribute('data-existing') !== 'true';
            });
        }
        Array.prototype.forEach.call(stepNav.querySelectorAll('a[data-step]'), function (link) {
            var paso = parseInt(link.getAttribute('data-step'), 10);
            if (paso > pasoActual) link.setAttribute('aria-disabled', 'true');
            link.addEventListener('click', function (e) {
                if (paso === pasoActual) return;
                e.preventDefault();
                if (paso > pasoActual || !stepNav.getAttribute('data-solicitud')) return;
                var url = stepNav.getAttribute('data-edit-url');
                window.location.href = url + '?id=' + encodeURIComponent(stepNav.getAttribute('data-solicitud')) + '&paso=' + paso;
            });
        });
        var formulario = document.querySelector('.crm-editor');
        formulario.addEventListener('submit', function (e) {
            if (e.submitter && e.submitter.value === 'BORRADOR') return;
            if (pasoActual === 2 && filas('contacto').length === 0) {
                e.preventDefault(); window.alert('Agregue al menos un contacto.');
            }
            if (pasoActual === 3 && filas('direccion').length === 0) {
                e.preventDefault(); window.alert('Agregue al menos una dirección.');
            }
        });
    }

    var sapLookup = document.getElementById('crmSapLookup');
    if (sapLookup) {
        var usarSap = document.getElementById('crmUsarSap');
        var sapControls = document.getElementById('crmSapControls');
        var sapFiltro = document.getElementById('crmSapFiltro');
        var sapResultados = document.getElementById('crmSapResultados');
        var sapCodigo = document.getElementById('Ficha_CodigoSapOrigen');
        function descartarSap() {
            sapCodigo.value = '';
            sapResultados.textContent = '';
        }
        usarSap.addEventListener('change', function () {
            sapControls.hidden = !usarSap.checked;
            if (!usarSap.checked) descartarSap();
        });
        if (empresa) empresa.addEventListener('change', descartarSap);
        if (agente) agente.addEventListener('change', descartarSap);
        document.getElementById('crmSapBuscar').addEventListener('click', function () {
            if (!empresa.value || !agente.value || sapFiltro.value.trim().length < 2) {
                sapResultados.textContent = 'Seleccione empresa y agente; escriba al menos dos caracteres.';
                return;
            }
            sapResultados.textContent = 'Buscando clientes en SAP...';
            var query = '?empresa=' + encodeURIComponent(empresa.value) +
                '&codigoOperador=' + encodeURIComponent(agente.value) +
                '&filtro=' + encodeURIComponent(sapFiltro.value.trim());
            fetch(sapLookup.getAttribute('data-url') + query, { credentials: 'same-origin' })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    sapResultados.textContent = '';
                    if (!data.ok) { sapResultados.textContent = data.mensaje || 'No fue posible consultar SAP.'; return; }
                    if (!data.clientes.length) { sapResultados.textContent = 'No se encontraron clientes.'; return; }
                    data.clientes.forEach(function (cliente) {
                        var boton = document.createElement('button');
                        boton.type = 'button'; boton.className = 'crm-btn crm-btn-outline';
                        boton.textContent = cliente.codigo + ' · ' + cliente.nombre + (cliente.nit ? ' · ' + cliente.nit : '');
                        boton.addEventListener('click', function () {
                            sapCodigo.value = cliente.codigo;
                            [['Ficha_RazonSocial', cliente.nombre], ['Ficha_NitDpi', cliente.nit],
                             ['Ficha_DireccionFiscal', cliente.direccion], ['Ficha_CorreoFactura', cliente.correo],
                             ['Ficha_MonedaIndicadores', cliente.moneda]].forEach(function (dato) {
                                var control = document.getElementById(dato[0]);
                                if (control && dato[1]) control.value = dato[1];
                            });
                            sapResultados.textContent = 'Cliente SAP seleccionado: ' + cliente.codigo;
                        });
                        var filaSap = document.createElement('div');
                        filaSap.appendChild(boton);
                        sapResultados.appendChild(filaSap);
                    });
                })
                .catch(function () { sapResultados.textContent = 'No fue posible consultar SAP. Intente nuevamente.'; });
        });
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
        fila.className = 'crm-repeat-row crm-contact-row';
        fila.setAttribute('data-row', 'contacto');
        [['Area', 'Área'], ['Nombre', 'Nombre'], ['Puesto', 'Puesto'],
         ['Telefono', 'Teléfono'], ['Correo', 'Correo'],
         ['TomadorDecision', 'Tomador de decisiones'],
         ['InfluenciadorTecnico', 'Influenciador técnico / usuario']].forEach(function (dato) {
            var campo = input('Ficha.Contactos[' + i + '].' + dato[0], dato[1],
                dato[0] === 'Correo' ? 'email' : 'text');
            if (wizard && pasoActual === 2) campo.querySelector('input').required = true;
            fila.appendChild(campo);
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
                if (wizard) {
                    var vacio = document.createElement('option');
                    vacio.value = ''; vacio.textContent = 'Seleccione...';
                    select.appendChild(vacio);
                }
                ['Despacho', 'Cliente recoge', 'Transporte de envío'].forEach(function (valor) {
                    var o = document.createElement('option'); o.value = valor; o.textContent = valor;
                    select.appendChild(o);
                });
                campo.replaceChild(select, antiguo);
            }
            if (wizard && pasoActual === 3) campo.querySelector('.form-control').required = true;
            grid.appendChild(campo);
        });
        if (wizard) {
            [['RequiereCitaRespuesta', 'Requiere cita'],
             ['ActivaRespuesta', 'Dirección activa']].forEach(function (dato) {
                var campo = document.createElement('div'); campo.className = 'crm-field crm-col-4';
                var label = document.createElement('label'); label.textContent = dato[1];
                var select = document.createElement('select'); select.className = 'form-control';
                select.name = 'Ficha.Direcciones[' + i + '].' + dato[0];
                [['', 'Seleccione...'], ['SI', 'Sí'], ['NO', 'No']].forEach(function (opcion) {
                    var item = document.createElement('option'); item.value = opcion[0];
                    item.textContent = opcion[1]; select.appendChild(item);
                });
                select.required = pasoActual === 3;
                campo.appendChild(label); campo.appendChild(select); grid.appendChild(campo);
            });
        } else {
            var opciones = document.createElement('div'); opciones.className = 'crm-field crm-col-2 crm-check';
            [['RequiereCita', 'Requiere cita', false], ['Activa', 'Activa', true]].forEach(function (dato) {
                var label = document.createElement('label');
                var check = document.createElement('input'); check.type = 'checkbox'; check.value = 'true';
                check.name = 'Ficha.Direcciones[' + i + '].' + dato[0]; check.checked = dato[2];
                label.appendChild(check); label.appendChild(document.createTextNode(' ' + dato[1]));
                opciones.appendChild(label);
            });
            grid.appendChild(opciones);
        }
        fila.appendChild(grid); destino.appendChild(fila);
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
    if (document.getElementById('crmDireccionesRows') && filas('direccion').length === 0) nuevoDireccion();

    var condicion = document.getElementById('crmCondicionPago');
    var cambio = document.getElementById('crmCambioRazon');
    function actualizarDocumentos() {
        var requerido = wizard || (condicion && condicion.value === 'CREDITO') || (cambio && cambio.checked);
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
