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
                control.required = !seccion.hidden && control.type !== 'file';
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

    var tipoNegocio = document.getElementById('Ficha_TipoNegocioCodigo');
    if (tipoNegocio && empresa) {
        var tipoNombre = document.getElementById('Ficha_TipoNegocio');
        var tipoStatus = document.getElementById('crmTipoNegocioStatus');
        var grupoRequest = 0;
        function cargarTiposNegocio(preservar) {
            var codigo = preservar ? (tipoNegocio.getAttribute('data-selected') || tipoNegocio.value) : '';
            var nombreAnterior = tipoNombre.value;
            function restaurarAnterior() {
                if (!preservar || !codigo) return;
                var opcion = document.createElement('option');
                opcion.value = codigo;
                opcion.textContent = nombreAnterior || 'Grupo SAP ' + codigo;
                tipoNegocio.appendChild(opcion);
                tipoNegocio.value = codigo;
            }
            if (!preservar) {
                tipoNombre.value = '';
                tipoNegocio.removeAttribute('data-selected');
            }
            tipoStatus.textContent = '';
            var request = ++grupoRequest;
            tipoNegocio.innerHTML = '';
            var inicial = document.createElement('option');
            inicial.value = '';
            inicial.textContent = empresa.value ? 'Cargando tipos de negocio...' : 'Seleccione una empresa...';
            tipoNegocio.appendChild(inicial);
            if (!empresa.value) return;
            fetch(tipoNegocio.getAttribute('data-url') + '?empresa=' + encodeURIComponent(empresa.value),
                { credentials: 'same-origin' })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    if (request !== grupoRequest) return;
                    tipoNegocio.innerHTML = '';
                    inicial.textContent = data.ok ? 'Seleccione...' : (data.mensaje || 'No fue posible cargar SAP.');
                    tipoNegocio.appendChild(inicial);
                    if (!data.ok) {
                        tipoStatus.textContent = inicial.textContent;
                        restaurarAnterior();
                        return;
                    }
                    data.grupos.forEach(function (grupo) {
                        var opcion = document.createElement('option');
                        opcion.value = String(grupo.codigo);
                        opcion.textContent = grupo.nombre;
                        tipoNegocio.appendChild(opcion);
                    });
                    if (codigo && Array.prototype.some.call(tipoNegocio.options,
                        function (opcion) { return opcion.value === codigo; })) {
                        tipoNegocio.value = codigo;
                        tipoNombre.value = tipoNegocio.options[tipoNegocio.selectedIndex].text;
                    } else if (preservar) {
                        tipoNombre.value = nombreAnterior;
                    }
                })
                .catch(function () {
                    if (request !== grupoRequest) return;
                    inicial.textContent = 'No fue posible cargar tipos de negocio de SAP.';
                    tipoStatus.textContent = inicial.textContent;
                    restaurarAnterior();
                });
        }
        tipoNegocio.addEventListener('change', function () {
            tipoNombre.value = tipoNegocio.value ?
                tipoNegocio.options[tipoNegocio.selectedIndex].text : '';
        });
        empresa.addEventListener('change', function () { cargarTiposNegocio(false); });
        cargarTiposNegocio(true);
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
            } else if (tipo === 'contacto') {
                var contactoTitulo = fila.querySelector('.crm-contact-top strong');
                var quitarContacto = fila.querySelector('.crm-contact-remove');
                if (contactoTitulo) contactoTitulo.textContent = 'Contacto ' + (i + 1);
                if (quitarContacto) quitarContacto.setAttribute('aria-label', 'Quitar contacto ' + (i + 1));
            }
        });
    }
    function actualizarContactos() {
        var soloUno = filas('contacto').length <= 1;
        Array.prototype.forEach.call(document.querySelectorAll('.crm-contact-remove'), function (boton) {
            boton.disabled = soloUno;
            boton.title = soloUno ? 'Debe conservar al menos un contacto' : 'Quitar contacto';
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
        var cabeza = document.createElement('div'); cabeza.className = 'crm-contact-top';
        var titulo = document.createElement('strong'); titulo.textContent = 'Contacto ' + (i + 1);
        cabeza.appendChild(titulo);
        var quitar = document.createElement('button');
        quitar.type = 'button'; quitar.className = 'crm-remove crm-contact-remove';
        quitar.setAttribute('aria-label', 'Quitar contacto ' + (i + 1));
        quitar.innerHTML = '<i class="icon-remove"></i> Quitar contacto';
        cabeza.appendChild(quitar); fila.appendChild(cabeza);
        var campos = document.createElement('div'); campos.className = 'crm-contact-fields';
        [['Area', 'Área'], ['Nombre', 'Nombre'], ['Puesto', 'Puesto'],
         ['Telefono', 'Teléfono'], ['Correo', 'Correo'],
         ['TomadorDecision', 'Tomador de decisiones'],
         ['InfluenciadorTecnico', 'Influenciador técnico / usuario']].forEach(function (dato, indice) {
            var campo = input('Ficha.Contactos[' + i + '].' + dato[0], dato[1],
                dato[0] === 'Correo' ? 'email' : 'text');
            campo.className += indice < 4 ? ' crm-col-3' : ' crm-col-4';
            if (wizard && pasoActual === 2) campo.querySelector('input').required = true;
            campos.appendChild(campo);
        });
        if (wizard) {
            var tipoContacto = input('Ficha.Contactos[' + i + '].TipoContacto', 'Tipo de contacto');
            tipoContacto.className += ' crm-col-4';
            tipoContacto.querySelector('input').maxLength = 100;
            if (pasoActual === 2) tipoContacto.querySelector('input').required = true;
            campos.appendChild(tipoContacto);
        }
        var selectores = [['CanalComunicacion', 'Canal de comunicación', [['WHATSAPP', 'WhatsApp'],
            ['CELULAR', 'Llamada celular'], ['FIJO', 'Teléfono fijo'],
            ['CORREO', 'Correo electrónico'], ['PRESENCIAL', 'Presencial']]]];
        if (!wizard) selectores.unshift(['TipoContacto', 'Tipo de contacto',
            [['PRINCIPAL', 'Principal'], ['COMPRAS', 'Compras'], ['PAGOS', 'Pagos'],
             ['LOGISTICA', 'Logística'], ['TECNICO', 'Técnico / usuario'],
             ['GERENCIA', 'Gerencia']]]);
        selectores.forEach(function (dato) {
            var campo = document.createElement('div'); campo.className = 'crm-field crm-col-4';
            var label = document.createElement('label'); label.textContent = dato[1];
            var select = document.createElement('select'); select.className = 'form-control';
            select.name = 'Ficha.Contactos[' + i + '].' + dato[0];
            var vacio = document.createElement('option'); vacio.value = ''; vacio.textContent = 'Seleccione...';
            select.appendChild(vacio);
            dato[2].forEach(function (opcion) {
                var item = document.createElement('option'); item.value = opcion[0];
                item.textContent = opcion[1]; select.appendChild(item);
            });
            if (wizard && pasoActual === 2) select.required = true;
            campo.appendChild(label); campo.appendChild(select); campos.appendChild(campo);
        });
        fila.appendChild(campos); destino.appendChild(fila);
        actualizarContactos();
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
            [['RequiereCita', 'Requiere cita', false],
             ['Activa', 'Dirección activa', true]].forEach(function (dato) {
                var campo = document.createElement('div'); campo.className = 'crm-field crm-col-4 crm-check';
                var label = document.createElement('label');
                var check = document.createElement('input'); check.type = 'checkbox'; check.value = 'true';
                check.name = 'Ficha.Direcciones[' + i + '].' + dato[0]; check.checked = dato[2];
                label.appendChild(check); label.appendChild(document.createTextNode(' ' + dato[1]));
                var hidden = document.createElement('input'); hidden.type = 'hidden'; hidden.value = 'false';
                hidden.name = check.name;
                campo.appendChild(label); campo.appendChild(hidden); grid.appendChild(campo);
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
        if (tipo === 'contacto' && filas('contacto').length <= 1) return;
        fila.parentNode.removeChild(fila);
        renumerar(tipo);
        if (tipo === 'contacto') actualizarContactos();
    });
    if (document.getElementById('crmContactosRows') && filas('contacto').length === 0) nuevoContacto();
    actualizarContactos();
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
