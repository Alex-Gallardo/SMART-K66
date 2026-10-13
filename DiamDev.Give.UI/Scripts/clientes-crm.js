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
                control.required = !seccion.hidden && control.type !== 'file' &&
                    control.name !== 'Ficha.TramiteContrasena' &&
                    control.name !== 'Ficha.ObservacionesComerciales' &&
                    control.name !== 'Ficha.CorreoFactura' &&
                    !/^Ficha\.Contactos\[\d+\]\.Correo$/.test(control.name);
            });
        });
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

    var guia = document.getElementById('crmGuide');
    var abrirGuia = document.getElementById('crmHelpOpen');
    if (wizard && guia && abrirGuia) {
        var cerrarGuia = document.getElementById('crmGuideClose');
        var anteriorGuia = document.getElementById('crmGuidePrev');
        var siguienteGuia = document.getElementById('crmGuideNext');
        var avisoGuia = document.getElementById('crmHelpHint');
        var cerrarAviso = document.getElementById('crmHelpHintClose');
        var claveAviso = 'crm-solicitud-guia-v1';
        var conDesarrollo = !!stepNav.querySelector('[data-step="5"]');
        var pasosGuia = [
            ['Antes de comenzar', 'La solicitud avanza por secciones. Guardar un paso o un borrador no la envía a Créditos. Puede retomarla desde Mis solicitudes.'],
            ['1. Identidad y facturación', 'Seleccione la empresa y el agente, y complete los datos fiscales. Pulse Guardar y continuar para pasar a Contactos. La solicitud seguirá como borrador.'],
            ['2. Contactos', 'Agregue al menos una persona, indique su tipo de contacto y canal de comunicación. Complete los campos requeridos antes de continuar.'],
            ['3. Entregas', 'Registre al menos una dirección con modalidad, referencia y horarios. Puede agregar otras ubicaciones si el cliente las utiliza.'],
            ['4. Perfil comercial', 'Describa la cobertura, los canales y el contexto comercial. Guarde el paso para llegar a Documentos.']
        ];
        if (conDesarrollo) pasosGuia.push(['5. Desarrollo comercial',
            'En una actualización, complete los indicadores y próximos pasos comerciales. Después continúe a Documentos.']);
        pasosGuia.push(['Último paso: Documentos y envío',
            'Adjunte RTU actualizado y DPI del representante, ambos lados. Los demás documentos son opcionales. Pulse Enviar a Créditos y compruebe el estado Enviada en el detalle de la solicitud.']);
        var indiceGuia = 0;
        var focoAnterior = null;
        function recordarGuia() {
            if (avisoGuia) avisoGuia.hidden = true;
            try { window.localStorage.setItem(claveAviso, '1'); } catch (e) { /* El aviso sigue siendo descartable en esta visita. */ }
        }
        function mostrarPasoGuia() {
            document.getElementById('crmGuideCount').textContent = 'Tema ' + (indiceGuia + 1) + ' de ' + pasosGuia.length;
            document.getElementById('crmGuideFill').style.width = ((indiceGuia + 1) * 100 / pasosGuia.length) + '%';
            document.getElementById('crmGuideTitle').textContent = pasosGuia[indiceGuia][0];
            document.getElementById('crmGuideDescription').textContent = pasosGuia[indiceGuia][1];
            anteriorGuia.disabled = indiceGuia === 0;
            siguienteGuia.textContent = indiceGuia === pasosGuia.length - 1 ? 'Cerrar guía' : 'Siguiente';
        }
        function cerrarRecorrido() {
            guia.hidden = true;
            abrirGuia.setAttribute('aria-expanded', 'false');
            if (focoAnterior && document.contains(focoAnterior)) focoAnterior.focus();
        }
        function iniciarRecorrido() {
            focoAnterior = document.activeElement;
            indiceGuia = 0;
            mostrarPasoGuia();
            recordarGuia();
            guia.hidden = false;
            abrirGuia.setAttribute('aria-expanded', 'true');
            cerrarGuia.focus();
        }
        try { if (window.localStorage.getItem(claveAviso) !== '1' && avisoGuia) avisoGuia.hidden = false; }
        catch (e) { if (avisoGuia) avisoGuia.hidden = false; }
        abrirGuia.addEventListener('click', iniciarRecorrido);
        cerrarGuia.addEventListener('click', cerrarRecorrido);
        if (cerrarAviso) cerrarAviso.addEventListener('click', function () {
            recordarGuia();
            abrirGuia.focus();
        });
        anteriorGuia.addEventListener('click', function () { if (indiceGuia > 0) { indiceGuia--; mostrarPasoGuia(); } });
        siguienteGuia.addEventListener('click', function () {
            if (indiceGuia === pasosGuia.length - 1) cerrarRecorrido();
            else { indiceGuia++; mostrarPasoGuia(); }
        });
        guia.addEventListener('click', function (e) { if (e.target === guia) cerrarRecorrido(); });
        guia.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { e.preventDefault(); cerrarRecorrido(); }
            if (e.key !== 'Tab') return;
            var elementos = [cerrarGuia, anteriorGuia, siguienteGuia].filter(function (boton) { return !boton.disabled; });
            if (e.shiftKey && document.activeElement === elementos[0]) { e.preventDefault(); elementos[elementos.length - 1].focus(); }
            if (!e.shiftKey && document.activeElement === elementos[elementos.length - 1]) { e.preventDefault(); elementos[0].focus(); }
        });
    }

    var iconosCampo = {
        Empresa: 'icon-building', CodigoOperador: 'icon-user', OrigenCodigoSap: 'icon-search',
        TipoOperacion: 'icon-briefcase', RazonSocial: 'icon-building', NombreComercial: 'icon-tag',
        NitDpi: 'icon-credit-card', TipoNegocioCodigo: 'icon-tags', DireccionFiscal: 'icon-map-marker',
        CorreoFactura: 'icon-envelope', CondicionPago: 'icon-money', MetodoPago: 'icon-credit-card',
        TemporadaPago: 'icon-calendar', TramiteContrasena: 'icon-comment', CambioRazonSocial: 'icon-exchange',
        Area: 'icon-building', Nombre: 'icon-user', Puesto: 'icon-briefcase', Telefono: 'icon-phone',
        Correo: 'icon-envelope', TomadorDecision: 'icon-user', TipoContacto: 'icon-tags',
        CanalComunicacion: 'icon-comments', Modalidad: 'icon-truck', Referencia: 'icon-map-marker',
        Direccion: 'icon-map-marker', HorarioSemana: 'icon-time', HorarioFinSemana: 'icon-time',
        RequiereCita: 'icon-calendar', Activa: 'icon-ok-circle', CoberturaGeografica: 'icon-globe',
        RegionesMayorVenta: 'icon-map-marker', SucursalesMayorVenta: 'icon-building',
        VendedoresCampo: 'icon-group', VendedoresTienda: 'icon-group', TeleventasRespuesta: 'icon-phone',
        VentaMostradorRespuesta: 'icon-shopping-cart', VentaInstitucionalRespuesta: 'icon-building',
        EcommerceRespuesta: 'icon-shopping-cart', ClientesFinales: 'icon-group',
        TemporadasDemanda: 'icon-calendar', ProyectosEventos: 'icon-calendar',
        NecesidadPrincipal: 'icon-lightbulb', ValorProveedor: 'icon-star', MotivoCompra: 'icon-shopping-cart',
        ObjecionPrincipal: 'icon-warning-sign', CompetidorPrincipal: 'icon-flag',
        FortalezaCompetidor: 'icon-signal', DebilidadCompetidor: 'icon-search',
        InformacionMercado: 'icon-globe', RiesgoComercial: 'icon-warning-sign',
        Satisfaccion: 'icon-star', CompraActualmenteRespuesta: 'icon-shopping-cart',
        MonedaIndicadores: 'icon-money', Venta12Meses: 'icon-bar-chart', PotencialAnual: 'icon-bar-chart',
        Objetivo12Meses: 'icon-bullseye', OportunidadCrecimiento: 'icon-bar-chart',
        CompetidorNegocio: 'icon-flag', Forecast: 'icon-bar-chart', ProximoPaso: 'icon-arrow-right',
        ObservacionesComerciales: 'icon-comment', Rtu: 'icon-file-text', Dpi: 'icon-file-text',
        Nombramiento: 'icon-file-text', PatenteComercio: 'icon-file-text', PatenteSociedad: 'icon-file-text'
    };
    function decorarCampos(contenedor) {
        if (!wizard || !contenedor) return;
        Array.prototype.forEach.call(contenedor.querySelectorAll('.crm-field label'), function (label) {
            if (label.querySelector('.crm-label-icon')) return;
            var campo = label.closest('.crm-field');
            var control = label.querySelector('[name]') ||
                campo.querySelector('input:not([type="hidden"])[name], select[name], textarea[name]') ||
                campo.querySelector('input[type="hidden"][name]');
            var clave = control ? control.name.split('.').pop() : '';
            var icono = document.createElement('i');
            icono.className = 'crm-label-icon ' + (iconosCampo[clave] || 'icon-info-sign');
            icono.setAttribute('aria-hidden', 'true');
            label.insertBefore(icono, label.firstChild);
        });
    }

    var documentos = document.getElementById('crm-documentos');
    if (documentos && (!wizard || pasoActual === 6)) {
        Array.prototype.forEach.call(documentos.querySelectorAll('input[type="file"]'), function (control) {
            control.required = (control.name === 'Rtu' || control.name === 'Dpi') &&
                control.getAttribute('data-existing') !== 'true';
        });
    }

    var enviarModal = document.getElementById('crmEnviarModal');
    var formularioEnvio = document.querySelector('.crm-editor');
    var enviando = false;
    if (enviarModal && formularioEnvio) {
        formularioEnvio.addEventListener('submit', function (e) {
            if (!e.submitter || e.submitter.value !== 'ENVIAR' || e.defaultPrevented) return;
            if (enviando) { e.preventDefault(); return; }
            enviando = true;
            formularioEnvio.setAttribute('aria-busy', 'true');
            enviarModal.hidden = false;
            enviarModal.setAttribute('aria-hidden', 'false');
            enviarModal.classList.add('is-visible');
            enviarModal.focus();
        });
        window.addEventListener('pageshow', function () {
            enviando = false;
            formularioEnvio.removeAttribute('aria-busy');
            enviarModal.classList.remove('is-visible');
            enviarModal.setAttribute('aria-hidden', 'true');
            enviarModal.hidden = true;
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
         ['TomadorDecision', 'Tomador de decisiones']].forEach(function (dato, indice) {
            var campo = input('Ficha.Contactos[' + i + '].' + dato[0], dato[1],
                dato[0] === 'Correo' ? 'email' : 'text');
            campo.className += indice < 4 ? ' crm-col-3' : ' crm-col-4';
            if (wizard && pasoActual === 2 && dato[0] !== 'Correo')
                campo.querySelector('input').required = true;
            campos.appendChild(campo);
        });
        var selectores = [['TipoContacto', 'Tipo de contacto', [
            ['TOMADOR DE DECISIONES', 'Tomador de decisiones'],
            ['INFLUENCIADOR TECNICO', 'Influenciador técnico'], ['COMPRADOR', 'Comprador']]],
            ['CanalComunicacion', 'Canal de comunicación', [['WHATSAPP', 'WhatsApp'],
            ['CELULAR', 'Llamada celular'], ['FIJO', 'Teléfono fijo'],
            ['CORREO', 'Correo electrónico'], ['PRESENCIAL', 'Presencial']]]];
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
            if ((wizard && pasoActual === 2) || (!wizard && dato[0] === 'TipoContacto'))
                select.required = true;
            campo.appendChild(label); campo.appendChild(select); campos.appendChild(campo);
        });
        fila.appendChild(campos); destino.appendChild(fila);
        decorarCampos(fila);
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
                [['DESPACHO', 'Despacho'], ['CLIENTE RECOGE', 'Cliente recoge'],
                 ['TRANSPORTE DE ENVIO', 'Transporte de envío']].forEach(function (valor) {
                    var o = document.createElement('option'); o.value = valor[0]; o.textContent = valor[1];
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
        decorarCampos(fila);
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
    decorarCampos(document.querySelector('.crm-editor'));

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
