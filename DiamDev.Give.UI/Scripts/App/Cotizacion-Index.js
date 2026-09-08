(function ($) {
    "use strict";

    var $app = $("#cotApp");
    if (!$app.length) return;

    var urls = {
        clientes: $app.data("url-clientes"),
        productos: $app.data("url-productos"),
        precio: $app.data("url-precio"),
        guardar: $app.data("url-guardar"),
        listar: $app.data("url-listar"),
        detalle: $app.data("url-detalle"),
        imprimir: $app.data("url-imprimir"),
        anular: $app.data("url-anular")
    };
    var puedeAnular = String($app.data("puede-anular")) === "true";
    var estado = {
        operadores: [],
        cliente: null,
        prospectoActivo: false,
        productos: [],
        productoPagina: 1,
        productoTieneMas: false,
        lineas: [],
        listado: [],
        seleccionado: null,
        anular: null,
        clienteTimer: null,
        clienteSolicitud: 0,
        filtroTimer: null,
        productoTimer: null,
        productoRequest: null,
        productoSolicitud: 0
    };

    function token() {
        return $app.find("input[name='__RequestVerificationToken']").val() || "";
    }

    function html(valor) {
        return $("<div>").text(valor == null ? "" : String(valor)).html();
    }

    function atributo(valor) {
        return String(valor == null ? "" : valor)
            .replace(/&/g, "&amp;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
    }

    function numero(valor) {
        if (typeof valor === "string") valor = valor.replace(/,/g, "").trim();
        var n = parseFloat(valor);
        return isFinite(n) ? n : 0;
    }

    function redondear(valor) {
        return Math.round((numero(valor) + 0.0000000001) * 100) / 100;
    }

    function moneda(valor, codigo) {
        return (codigo || $("#cotMoneda").val() || "") + " " +
            numero(valor).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function normalizarMoneda(valor) {
        var m = String(valor || "").trim().toUpperCase();
        return m === "QTZ" || m === "Q" ? "GTQ" : m;
    }

    function fuentePrecio(valor) {
        var fuentes = {
            CLIENTE_CANTIDAD: "Especial cliente por cantidad",
            CLIENTE_PERIODO: "Especial cliente por vigencia",
            CLIENTE: "Especial del cliente",
            GRUPO_DESCUENTO: "Grupo de descuento SAP",
            LISTA_CANTIDAD: "Especial de lista por cantidad",
            LISTA_PERIODO: "Especial de lista por vigencia",
            LISTA_ESPECIAL: "Especial de lista",
            LISTA: "Lista de precios",
            // SIN_PRECIO: "Sin precio configurado"
            SIN_PRECIO: ""
        };
        // return fuentes[String(valor || "").toUpperCase()] || "Precio SAP";
        return fuentes[String(valor || "").toUpperCase()] || "";
    }

    function tienePrecioSap(producto) {
        return producto && numero(producto.Precio) > 0 &&
            String(producto.FuentePrecio || "").toUpperCase() !== "SIN_PRECIO";
    }

    function avisar(tipo, mensaje) {
        if (window.toastr && window.toastr[tipo]) window.toastr[tipo](mensaje);
        else window.alert(mensaje);
    }

    function mensajeError(xhr) {
        return xhr && xhr.responseJSON && xhr.responseJSON.msg
            ? xhr.responseJSON.msg : "No fue posible completar la solicitud.";
    }

    function get(url, data) {
        return $.ajax({ url: url, type: "GET", dataType: "json", cache: false, data: data || {} });
    }

    function post(url, data) {
        var payload = $.extend({}, data || {});
        payload.__RequestVerificationToken = token();
        return $.ajax({ url: url, type: "POST", dataType: "json", data: payload });
    }

    function setBusy($btn, busy, texto) {
        if (busy) {
            $btn.data("texto", $btn.html()).prop("disabled", true)
                .html('<i class="icon-spinner icon-spin"></i> ' + html(texto || "Procesando"));
        } else {
            $btn.prop("disabled", false).html($btn.data("texto") || $btn.html());
        }
    }

    function empresa() { return $("#cotEmpresa").val() || ""; }
    function codigoOperador() { return $("#cotAgente").val() || ""; }
    function agente() { return $("#cotAgente option:selected").data("agente") || ""; }

    function cargarOperadores() {
        estado.operadores = [];
        $("#cotOperadoresCatalogo span").each(function () {
            estado.operadores.push({
                empresa: $(this).data("empresa"),
                codigo: $(this).data("codigo"),
                agente: $(this).data("agente")
            });
        });
    }

    function poblarAgentes(emp) {
        var $select = $("#cotAgente").empty();
        var lista = $.grep(estado.operadores, function (x) { return x.empresa === emp; });
        $select.append($("<option>").val("").text(lista.length ? "Seleccione..." : "Sin agentes configurados"));
        $.each(lista, function (_, x) {
            $select.append($("<option>").val(x.codigo).text(x.agente).attr("data-agente", x.agente));
        });
        $select.prop("disabled", !emp || !lista.length);
        if (lista.length === 1) $select.val(lista[0].codigo).trigger("change");
        actualizarDisponibilidadCliente();
    }

    function actualizarDisponibilidadCliente() {
        var deshabilitado = !empresa() || !codigoOperador();
        $("#cotBuscarCliente,#cotClienteCodigo").prop("disabled", deshabilitado);
        $("#cotClienteCodigo").attr("aria-disabled", deshabilitado ? "true" : "false");
        if (deshabilitado) ocultarResultadosClientes($("#cotClienteDropdown"));
    }

    function limpiarCliente() {
        cancelarBusquedaProductos();
        cancelarBusquedaClientes();
        ocultarResultadosClientes($("#cotClienteDropdown"));
        ocultarResultadosClientes($("#cotClienteResultados"));
        estado.cliente = null;
        estado.prospectoActivo = false;
        $("#cotClienteCodigo,#cotClienteNombre,#cotNit,#cotCorreo,#cotDireccion").val("");
        $("#cotMoneda").val("");
        estado.lineas = [];
        renderLineas();
        actualizarResumenCliente();
    }

    function actualizarResumenCliente() {
        var nombre = $("#cotClienteNombre").val().trim() || "Sin nombre";
        var nit = $("#cotNit").val().trim() || "Sin NIT";
        var monedaActual = normalizarMoneda($("#cotMoneda").val()) || "Sin moneda";
        var $strip = $("#cotClientStrip").removeClass("is-selected is-prospect");

        if (estado.cliente) {
            $strip.addClass("is-selected");
            $("#cotClientTitle").text((estado.cliente.CardCode || "") + " · " + nombre);
            $("#cotClientMeta").text("NIT: " + nit + " · Agente: " +
                (estado.cliente.SlpName || agente()) + " · Moneda: " + monedaActual);
            return;
        }

        var hayDatos = !!($("#cotClienteNombre").val().trim() || $("#cotNit").val().trim() ||
            $("#cotCorreo").val().trim() || $("#cotDireccion").val().trim() || $("#cotMoneda").val());
        if (estado.prospectoActivo || hayDatos) {
            estado.prospectoActivo = true;
            $strip.addClass("is-prospect");
            $("#cotClientTitle").text("Prospecto sin código SAP" +
                (nombre === "Sin nombre" ? "" : " · " + nombre));
            $("#cotClientMeta").text("NIT: " + nit + " · Agente: " +
                (agente() || "Sin agente") + " · Moneda: " + monedaActual);
            return;
        }

        $("#cotClientTitle").text("Cliente SAP o prospecto nuevo");
        $("#cotClientMeta").text(empresa() && codigoOperador()
            ? "Busque un cliente SAP o capture los datos de un prospecto."
            : "Seleccione empresa y agente para continuar.");
    }

    function iniciarProspecto() {
        if (!empresa() || !codigoOperador()) {
            avisar("warning", "Seleccione empresa y agente antes de registrar un prospecto.");
            return;
        }
        cancelarBusquedaProductos();
        cancelarBusquedaClientes();
        ocultarResultadosClientes($("#cotClienteDropdown"));
        estado.cliente = null;
        estado.prospectoActivo = true;
        $("#cotClienteCodigo,#cotClienteNombre,#cotNit,#cotCorreo,#cotDireccion").val("");
        $("#cotMoneda").val("");
        estado.lineas = [];
        renderLineas();
        actualizarResumenCliente();
        $("#cotClienteNombre").focus();
    }

    function seleccionarCliente(c) {
        var cambiaCliente = !estado.cliente ||
            String(estado.cliente.CardCode || "") !== String(c.CardCode || "");
        if (cambiaCliente && estado.lineas.length) {
            if (!window.confirm("Cambiar de cliente eliminará los productos no guardados. ¿Desea continuar?"))
                return false;
            estado.lineas = [];
            renderLineas();
        }

        cancelarBusquedaClientes();
        cancelarBusquedaProductos();
        estado.cliente = c;
        estado.prospectoActivo = false;
        $("#cotClienteCodigo").val(c.CardCode || "");
        $("#cotClienteNombre").val(c.CardName || "");
        $("#cotNit").val(c.LicTradNum || "");
        $("#cotCorreo").val(c.Email || "");
        $("#cotDireccion").val(c.Address || "");
        var m = normalizarMoneda(c.Currency);
        if (!estado.lineas.length) $("#cotMoneda").val(m && m !== "##" ? m : "");
        $("#cotClientStrip").addClass("is-selected");
        actualizarResumenCliente();
        ocultarResultadosClientes($("#cotClienteDropdown"));
        $("#cotClienteModal").modal("hide");
        return true;
    }

    function abrirClientes() {
        if (!empresa() || !codigoOperador()) {
            avisar("warning", "Seleccione empresa y agente antes de buscar clientes.");
            return;
        }
        ocultarResultadosClientes($("#cotClienteDropdown"));
        restaurarClienteSeleccionado();
        limpiarBusquedaClientes();
        $("#cotClienteResultados").html('<div class="cot-client-result-empty"><i class="icon-search"></i><span>Escriba al menos dos caracteres para buscar.</span></div>');
        mostrarResultadosClientes($("#cotClienteResultados"));
        $("#cotClienteModal").modal("show");
    }

    function cancelarBusquedaClientes() {
        window.clearTimeout(estado.clienteTimer);
        estado.clienteTimer = null;
        estado.clienteSolicitud++;
    }

    function ocultarResultadosClientes($resultados) {
        $resultados.empty().removeClass("is-visible");
        if ($resultados.is("#cotClienteDropdown"))
            $("#cotClienteCodigo").attr("aria-expanded", "false");
    }

    function limpiarBusquedaClientes() {
        cancelarBusquedaClientes();
        $("#cotClienteFiltro").val("");
        ocultarResultadosClientes($("#cotClienteResultados"));
    }

    function contextoBusquedaClientes(origen) {
        return origen === "directo"
            ? { $input: $("#cotClienteCodigo"), $resultados: $("#cotClienteDropdown") }
            : { $input: $("#cotClienteFiltro"), $resultados: $("#cotClienteResultados") };
    }

    function mostrarResultadosClientes($resultados) {
        $resultados.addClass("is-visible");
        if ($resultados.is("#cotClienteDropdown"))
            $("#cotClienteCodigo").attr("aria-expanded", "true");
    }

    function renderClientes(clientes, $resultados) {
        $resultados.empty();
        if (!clientes.length) {
            $resultados.html('<div class="cot-client-result-empty"><i class="icon-search"></i><span>Sin resultados para esta búsqueda.</span></div>');
            mostrarResultadosClientes($resultados);
            return;
        }

        $.each(clientes, function (_, cliente) {
            var $opcion = $("<button>", {
                type: "button",
                "class": "cot-client-result",
                role: "option"
            }).data("cliente", cliente);
            $opcion.append(
                $('<span class="cot-client-result-main"></span>')
                    .append($("<strong>").text((cliente.CardCode || "") + " · " + (cliente.CardName || "")))
                    .append($("<small>").text((cliente.LicTradNum || "Sin NIT") + " · " + (cliente.SlpName || "Sin agente")))
            ).append('<i class="icon-chevron-right" aria-hidden="true"></i>');
            $resultados.append($opcion);
        });
        mostrarResultadosClientes($resultados);
    }

    function programarBusquedaClientes(origen) {
        window.clearTimeout(estado.clienteTimer);
        var contexto = contextoBusquedaClientes(origen);
        var filtro = contexto.$input.val().trim();
        var $resultados = contexto.$resultados;

        if (filtro.length < 2) {
            estado.clienteSolicitud++;
            ocultarResultadosClientes($resultados);
            return;
        }

        var empresaConsulta = empresa();
        var operadorConsulta = codigoOperador();
        var solicitud = ++estado.clienteSolicitud;
        estado.clienteTimer = window.setTimeout(function () {
            $resultados.html('<div class="cot-client-result-empty"><i class="icon-spinner icon-spin"></i><span>Consultando clientes en SAP...</span></div>');
            mostrarResultadosClientes($resultados);
            get(urls.clientes, {
                empresa: empresaConsulta,
                codigoOperador: operadorConsulta,
                filtro: filtro
            })
            .done(function (r) {
                if (solicitud !== estado.clienteSolicitud || empresa() !== empresaConsulta ||
                    codigoOperador() !== operadorConsulta) return;
                if (!r || !r.ok) {
                    avisar("error", r && r.msg ? r.msg : "No se pudieron consultar los clientes.");
                    renderClientes([], $resultados);
                    return;
                }
                renderClientes(r.data || [], $resultados);
            })
            .fail(function (xhr) {
                if (solicitud !== estado.clienteSolicitud) return;
                avisar("error", mensajeError(xhr));
                renderClientes([], $resultados);
            });
        }, 350);
    }

    function restaurarClienteSeleccionado() {
        if (clienteSeleccionadoValido()) return;
        $("#cotClienteCodigo").val(estado.cliente ? estado.cliente.CardCode || "" : "");
    }

    function clienteSeleccionadoValido() {
        return !!estado.cliente && $("#cotClienteCodigo").val().trim().toUpperCase() ===
            String(estado.cliente.CardCode || "").trim().toUpperCase();
    }

    function abrirProductos() {
        if (!empresa() || !codigoOperador()) {
            avisar("warning", "Seleccione empresa y agente antes de agregar productos.");
            return;
        }
        if (!estado.cliente) {
            estado.prospectoActivo = true;
            actualizarResumenCliente();
        }
        cancelarBusquedaProductos();
        $("#cotProductoFiltro").val("");
        estado.productos = [];
        $("#cotProductoCount").text("");
        estado.productoPagina = 1;
        estado.productoTieneMas = false;
        $("#cotProductoContexto")
            .toggleClass("is-prospect", !estado.cliente)
            .html(estado.cliente
                ? '<i class="icon-user"></i><span>Precios de referencia para <strong>' + html(estado.cliente.CardCode) + ' · ' + html($("#cotClienteNombre").val().trim()) + '</strong>.</span>'
                : '<i class="icon-edit"></i><span><strong>Prospecto sin código SAP:</strong> seleccione productos del catálogo e ingrese el precio neto manual en el detalle.</span>');
        mostrarEstadoProductos("icon-search", "Preparando catálogo", "Consultando productos disponibles para la venta.");
        $("#cotProductoModal").modal("show");
        buscarProductos(1);
        setTimeout(function () { $("#cotProductoFiltro").focus(); }, 250);
    }

    function cancelarBusquedaProductos() {
        clearTimeout(estado.productoTimer);
        estado.productoTimer = null;
        estado.productoSolicitud++;
        if (estado.productoRequest && estado.productoRequest.readyState !== 4) {
            estado.productoRequest.abort();
        }
        estado.productoRequest = null;
        setBusy($("#cotBuscarProducto"), false);
    }

    function mostrarEstadoProductos(icono, titulo, detalle) {
        $("#cotProductoBody").empty();
        $("#cotProductoEmpty")
            .html('<i class="' + atributo(icono) + '"></i><strong>' + html(titulo) + '</strong>' +
                (detalle ? '<span>' + html(detalle) + '</span>' : ''))
            .show();
        $("#cotProductoAnterior,#cotProductoSiguiente").prop("disabled", true);
    }

    function buscarProductos(pagina) {
        cancelarBusquedaProductos();
        pagina = Math.max(1, parseInt(pagina, 10) || 1);
        var solicitud = estado.productoSolicitud;
        var filtro = $.trim($("#cotProductoFiltro").val() || "");
        var $btn = $("#cotBuscarProducto");
        setBusy($btn, true, "Buscando");
        $("#cotProductoCount").text("Buscando…");
        mostrarEstadoProductos("icon-spinner icon-spin", "Consultando SAP", filtro ? "Buscando “" + filtro + "”." : "Recorriendo el catálogo disponible para la venta.");

        var request = get(urls.productos, {
            empresa: empresa(), codigoOperador: codigoOperador(),
            clienteId: estado.cliente ? estado.cliente.CardCode : "",
            filtro: filtro,
            pagina: pagina, tamano: 100
        });
        estado.productoRequest = request;

        request.done(function (r) {
            if (solicitud !== estado.productoSolicitud) return;
            if (!r.ok) {
                estado.productos = [];
                $("#cotProductoCount").text("");
                mostrarEstadoProductos("icon-warning-sign", "No se pudo consultar SAP", r.msg || "La consulta no devolvió una respuesta válida.");
                avisar("error", r.msg);
                return;
            }
            var datos = r.data || {};
            estado.productos = datos.Items || [];
            estado.productoPagina = numero(datos.Pagina) || pagina;
            estado.productoTieneMas = !!datos.TieneMas;
            renderProductos();
        }).fail(function (xhr, estadoAjax) {
            if (solicitud !== estado.productoSolicitud || estadoAjax === "abort") return;
            estado.productos = [];
            $("#cotProductoCount").text("");
            mostrarEstadoProductos("icon-warning-sign", "No se pudo consultar SAP", mensajeError(xhr));
            avisar("error", mensajeError(xhr));
        }).always(function () {
            if (solicitud !== estado.productoSolicitud) return;
            estado.productoRequest = null;
            setBusy($btn, false);
        });
    }

    function renderProductos() {
        var $body = $("#cotProductoBody").empty();
        $("#cotProductoCount").text(
            "Página " + estado.productoPagina + " · " +
            estado.productos.length + " productos");
        $("#cotProductoAnterior").prop("disabled", estado.productoPagina <= 1);
        $("#cotProductoSiguiente").prop("disabled", !estado.productoTieneMas);
        if (!estado.productos.length) {
            mostrarEstadoProductos("icon-search", "Sin coincidencias", "Pruebe con otro código, nombre o grupo.");
            return;
        }
        $("#cotProductoEmpty").hide();
        $.each(estado.productos, function (i, p) {
            var stockClass = numero(p.Disponible) <= 0 ? "cot-stock-low" : "";
            var precioHtml = !estado.cliente
                ? '<strong class="cot-price-missing">Precio manual</strong><br><small class="cot-price-help">Se captura al agregar</small>'
                : (tienePrecioSap(p)
                    ? '<strong>' + html(moneda(p.Precio, normalizarMoneda(p.Moneda))) + '</strong>' +
                    '<br><small class="text-muted">Neto · ' + html(fuentePrecio(p.FuentePrecio)) + '</small>'
                    : '<strong class="cot-price-missing">Sin precio SAP</strong><br><small class="cot-price-help">Se requiere precio manual</small>');
            var $tr = $("<tr tabindex='0'></tr>").append(
                '<td><strong>' + html(p.ItemCode) + '</strong><br><small class="text-muted">' + html(p.ItemName) + '</small></td>' +
                '<td>' + html(p.Grupo || "—") + '</td><td>' + html(p.Unidad || "—") + '</td>' +
                '<td class="text-right">' + numero(p.Existencia).toLocaleString("es-GT") + '</td>' +
                '<td class="text-right">' + numero(p.Comprometido).toLocaleString("es-GT") + '</td>' +
                '<td class="text-right ' + stockClass + '">' + numero(p.Disponible).toLocaleString("es-GT") + '</td>' +
                '<td class="text-right">' + precioHtml + '</td>' +
                '<td class="text-right"><strong>' + numero(p.ImpuestoPorcentaje).toFixed(2) + '%</strong><br><small class="text-muted">' + html(p.GrupoImpuesto || "—") + '</small></td>');
            $tr.on("click keydown", function (e) {
                if (e.type === "click" || e.keyCode === 13) agregarProducto(i);
            });
            $body.append($tr);
        });
    }

    function agregarProducto(indice) {
        var p = estado.productos[indice];
        if (!p) return;
        var esProspecto = !estado.cliente;
        var sinPrecioSap = esProspecto || !tienePrecioSap(p);
        var precioInicial = esProspecto ? 0 : numero(p.Precio);
        var existente = -1;
        $.each(estado.lineas, function (i, x) {
            if (String(x.ItemCode).toUpperCase() === String(p.ItemCode).toUpperCase()) existente = i;
        });
        if (existente >= 0) estado.lineas[existente].Cantidad = numero(estado.lineas[existente].Cantidad) + 1;
        else estado.lineas.push({
            ItemCode: p.ItemCode,
            ItemName: p.ItemName,
            Descripcion: p.ItemName,
            Grupo: p.Grupo,
            Unidad: p.Unidad,
            ListaPrecio: p.ListaPrecio,
            Moneda: normalizarMoneda(p.Moneda),
            Existencia: numero(p.Existencia),
            Disponible: numero(p.Disponible),
            Cantidad: 1,
            PrecioLista: precioInicial,
            PrecioUnitario: precioInicial,
            DescuentoPorcentaje: 0,
            ImpuestoPorcentaje: numero(p.ImpuestoPorcentaje),
            PrecioBruto: numero(p.PrecioBruto),
            FuentePrecio: esProspecto ? "SIN_PRECIO" : p.FuentePrecio,
            PrecioManual: esProspecto,
            PrecioVersion: 0
        });
        renderLineas();
        $("#cotProductoModal").modal("hide");
        if (esProspecto) {
            avisar("info", "Ingrese el precio neto manual para " + p.ItemCode + ".");
        } else if (sinPrecioSap) {
            avisar("warning", "SAP no tiene un precio efectivo para " +
                p.ItemCode + ". Ingrese un precio neto manual mayor que cero.");
        }
        actualizarPrecioSap(existente >= 0 ? existente : estado.lineas.length - 1);
    }

    function actualizarPrecioSap(indice) {
        var x = estado.lineas[indice];
        if (!x || numero(x.Cantidad) <= 0 || !estado.cliente) return;
        x.PrecioVersion = numero(x.PrecioVersion) + 1;
        var version = x.PrecioVersion, referencia = x;
        get(urls.precio, {
            empresa: empresa(), codigoOperador: codigoOperador(),
            clienteId: estado.cliente.CardCode, itemCode: x.ItemCode,
            cantidad: numero(x.Cantidad)
        }).done(function (r) {
            if (!r.ok) {
                avisar("warning", r.msg ||
                    "No fue posible actualizar la escala de precio SAP.");
                return;
            }
            if (!r.data || estado.lineas[indice] !== referencia ||
                estado.lineas[indice].PrecioVersion !== version) return;
            var p = r.data, linea = estado.lineas[indice];
            linea.PrecioLista = numero(p.Precio);
            linea.PrecioBruto = numero(p.PrecioBruto);
            linea.FuentePrecio = p.FuentePrecio;
            linea.ImpuestoPorcentaje = numero(p.ImpuestoPorcentaje);
            linea.Existencia = numero(p.Existencia);
            linea.Disponible = numero(p.Disponible);
            if (!linea.PrecioManual) linea.PrecioUnitario = numero(p.Precio);
            renderLineas();
        }).fail(function () {
            avisar("warning", "No fue posible actualizar la escala SAP de " + x.ItemCode + ".");
        });
    }

    function calcularLinea(x) {
        var bruto = redondear(numero(x.Cantidad) * numero(x.PrecioUnitario));
        var descuento = redondear(bruto * numero(x.DescuentoPorcentaje) / 100);
        var subtotal = redondear(bruto - descuento);
        var impuesto = redondear(subtotal * numero(x.ImpuestoPorcentaje) / 100);
        return { bruto: bruto, descuento: descuento, subtotal: subtotal, impuesto: impuesto, total: redondear(subtotal + impuesto) };
    }

    function renderLineas() {
        var $body = $("#cotLineBody").empty();
        $("#cotLineEmpty").toggle(!estado.lineas.length);
        $("#cotLineCount").text(estado.lineas.length + (estado.lineas.length === 1 ? " producto" : " productos"));
        $.each(estado.lineas, function (i, x) {
            var c = calcularLinea(x);
            var stockClass = numero(x.Disponible) < numero(x.Cantidad) ? "cot-stock-low" : "";
            var priceClass = numero(x.PrecioUnitario) <= 0 ? " cot-price-input-missing" : "";
            var priceHelpClass = numero(x.PrecioLista) <= 0
                ? "cot-price-help" : "text-muted";
            $body.append('<tr data-index="' + i + '">' +
                '<td data-label="Producto"><div class="cot-product"><strong>' + html(x.ItemCode) + '</strong><small>' + html(x.ItemName) + '</small></div></td>' +
                '<td data-label="Descripción"><input class="form-control cot-desc" maxlength="500" data-field="Descripcion" value="' + atributo(x.Descripcion) + '" /></td>' +
                '<td data-label="Unidad">' + html(x.Unidad || "—") + '</td>' +
                '<td data-label="Disponible" class="text-right ' + stockClass + '">' + numero(x.Disponible).toLocaleString("es-GT") + '</td>' +
                '<td data-label="Cantidad"><input class="form-control text-right" type="number" min="0.000001" step="0.01" data-field="Cantidad" value="' + numero(x.Cantidad) + '" /></td>' +
                '<td data-label="Precio neto"><input class="form-control text-right' + priceClass + '" type="number" min="0.000001" step="0.01" data-field="PrecioUnitario" title="Referencia SAP neta: ' + numero(x.PrecioLista).toFixed(2) + ' · ' + atributo(fuentePrecio(x.FuentePrecio)) + '" value="' + numero(x.PrecioUnitario) + '" /><small class="' + priceHelpClass + '">' + html(fuentePrecio(x.FuentePrecio)) + '</small></td>' +
                '<td data-label="Descuento %"><input class="form-control text-right" type="number" min="0" max="100" step="0.01" data-field="DescuentoPorcentaje" value="' + numero(x.DescuentoPorcentaje) + '" /></td>' +
                '<td data-label="IVA SAP"><input class="form-control text-right" type="number" readonly data-field="ImpuestoPorcentaje" value="' + numero(x.ImpuestoPorcentaje) + '" /></td>' +
                '<td data-label="Total" class="text-right"><strong>' + html(moneda(c.total)) + '</strong></td>' +
                '<td data-label="Acciones"><button type="button" class="cot-remove" title="Quitar"><i class="icon-trash"></i></button></td></tr>');
        });
        renderTotales();
    }

    function renderTotales() {
        var t = { bruto: 0, descuento: 0, subtotal: 0, impuesto: 0, total: 0 };
        $.each(estado.lineas, function (_, x) {
            var c = calcularLinea(x);
            $.each(t, function (k) { t[k] = redondear(t[k] + c[k]); });
        });
        $("#cotBruto").text(moneda(t.bruto));
        $("#cotDescuento").text(moneda(t.descuento));
        $("#cotSubtotal").text(moneda(t.subtotal));
        $("#cotImpuesto").text(moneda(t.impuesto));
        $("#cotTotal").text(moneda(t.total));
    }

    function validar() {
        if (!empresa()) return "Seleccione una empresa.";
        if (!codigoOperador()) return "Seleccione un agente.";
        if (!$("#cotClienteNombre").val().trim()) return "Ingrese el nombre del cliente.";
        if ($("#cotClienteNombre").val().trim().length > 200) return "El nombre del cliente excede 200 caracteres.";
        if ($("#cotNit").val().trim().length > 50) return "El NIT excede 50 caracteres.";
        if ($("#cotDireccion").val().trim().length > 300) return "La dirección excede 300 caracteres.";
        if ($("#cotCorreo").val().trim().length > 150) return "El correo excede 150 caracteres.";
        if ($("#cotCorreo").val().trim() && !document.getElementById("cotCorreo").checkValidity())
            return "Ingrese un correo electrónico válido.";
        if (!$("#cotMoneda").val()) return "Seleccione una moneda.";
        if (!$("#cotFecha").val() || !$("#cotValidaHasta").val()) return "Complete las fechas.";
        if ($("#cotValidaHasta").val() < $("#cotFecha").val()) return "La validez no puede ser anterior a la emisión.";
        if (!estado.lineas.length) return "Agregue al menos un producto.";
        for (var i = 0; i < estado.lineas.length; i++) {
            var x = estado.lineas[i];
            if (numero(x.Cantidad) <= 0) return "La cantidad de " + x.ItemCode + " debe ser mayor que cero.";
            if (numero(x.PrecioUnitario) <= 0) return "Ingrese un precio neto mayor que cero para " + x.ItemCode + ".";
            if (numero(x.DescuentoPorcentaje) < 0 || numero(x.DescuentoPorcentaje) > 100) return "Revise el descuento de " + x.ItemCode + ".";
            if (numero(x.ImpuestoPorcentaje) < 0 || numero(x.ImpuestoPorcentaje) > 100) return "Revise el IVA de " + x.ItemCode + ".";
        }
        return null;
    }

    function guardar() {
        var error = validar();
        if (error) { avisar("warning", error); return; }
        var request = {
            IdEmpresa: empresa(), Fecha: $("#cotFecha").val(), ValidaHasta: $("#cotValidaHasta").val(),
            IdCliente: estado.cliente ? estado.cliente.CardCode : "",
            NombreCliente: $("#cotClienteNombre").val().trim(), Nit: $("#cotNit").val().trim(),
            Direccion: $("#cotDireccion").val().trim(), Correo: $("#cotCorreo").val().trim(),
            CodigoOperador: codigoOperador(), Moneda: $("#cotMoneda").val(),
            CondicionesPago: $("#cotCondicionesPago").val(), TiempoEntrega: $("#cotTiempoEntrega").val(),
            Observaciones: $("#cotObservaciones").val()
        };
        $.each(estado.lineas, function (i, x) {
            request["Detalles[" + i + "].ItemCode"] = x.ItemCode;
            request["Detalles[" + i + "].Descripcion"] = x.Descripcion;
            request["Detalles[" + i + "].Cantidad"] = numero(x.Cantidad);
            request["Detalles[" + i + "].PrecioUnitario"] = numero(x.PrecioUnitario);
            request["Detalles[" + i + "].DescuentoPorcentaje"] = numero(x.DescuentoPorcentaje);
            request["Detalles[" + i + "].ImpuestoPorcentaje"] = numero(x.ImpuestoPorcentaje);
        });
        var $btn = $("#cotGuardar");
        setBusy($btn, true, "Guardando");
        post(urls.guardar, request).done(function (r) {
            if (!r.ok) { avisar("error", r.msg); return; }
            avisar("success", r.msg);
            var emp = empresa();
            window.open(urls.imprimir + "?empresa=" + encodeURIComponent(emp) + "&idCotizacion=" + encodeURIComponent(r.idCotizacion), "_blank");
            resetFormulario(true);
        }).fail(function (xhr) { avisar("error", mensajeError(xhr)); })
          .always(function () { setBusy($btn, false); });
    }

    function resetFormulario(conservarEmpresa) {
        var emp = conservarEmpresa ? empresa() : "";
        $("#cotEmpresa").val(emp);
        if (!conservarEmpresa) poblarAgentes("");
        limpiarCliente();
        $("#cotCondicionesPago,#cotTiempoEntrega,#cotObservaciones").val("");
        var hoy = new Date(), valida = new Date(); valida.setDate(valida.getDate() + 15);
        $("#cotFecha").val(fechaInput(hoy)); $("#cotValidaHasta").val(fechaInput(valida));
    }

    function fechaInput(d) {
        var m = String(d.getMonth() + 1); if (m.length < 2) m = "0" + m;
        var dia = String(d.getDate()); if (dia.length < 2) dia = "0" + dia;
        return d.getFullYear() + "-" + m + "-" + dia;
    }

    function cargarListado() {
        var $btn = $("#cotRefrescar"); setBusy($btn, true, "Actualizando");
        get(urls.listar, { empresa: $("#cotFiltroEmpresa").val(), estado: $("#cotFiltroEstado").val(), desde: $("#cotFiltroDesde").val(), hasta: $("#cotFiltroHasta").val(), filtro: $("#cotFiltroTexto").val() })
            .done(function (r) { if (!r.ok) { avisar("error", r.msg); return; } estado.listado = r.data || []; renderListado(); })
            .fail(function (xhr) { avisar("error", mensajeError(xhr)); })
            .always(function () { setBusy($btn, false); });
    }

    function claseEstado(valor) {
        var estadoValor = String(valor || "").toUpperCase();
        if (estadoValor === "VIGENTE") return "is-vigente";
        if (estadoValor === "VENCIDA") return "is-vencida";
        if (estadoValor === "ANULADA") return "is-anulada";
        return "is-draft";
    }

    function renderListado() {
        var $body = $("#cotListBody").empty();
        $("#cotListEmpty").toggle(!estado.listado.length);
        var k = { VIGENTE: 0, VENCIDA: 0, ANULADA: 0, monedas: {} };
        $.each(estado.listado, function (i, x) {
            k[x.Estado] = (k[x.Estado] || 0) + 1;
            var codigo = normalizarMoneda(x.Moneda) || "—";
            k.monedas[codigo] = redondear((k.monedas[codigo] || 0) + numero(x.Total));
            var $tr = $("<tr tabindex='0'></tr>").append(
                '<td><strong>' + html(x.IdCotizacion) + '</strong><br><small class="text-muted">' + html(x.IdUsr) + '</small></td>' +
                '<td>' + html(x.Fecha) + '</td><td>' + html(x.IdEmpresa) + '</td>' +
                '<td><strong>' + html(x.NombreCliente) + '</strong><br><small class="text-muted">' + html(x.IdCliente || "Prospecto") + '</small></td>' +
                '<td>' + html(x.Agente) + '</td><td><span class="cot-status ' + claseEstado(x.Estado) + '">' + html(x.Estado) + '</span></td>' +
                '<td class="text-right"><strong>' + html(moneda(x.Total, x.Moneda)) + '</strong></td>');
            $tr.on("click keydown", function (e) { if (e.type === "click" || e.keyCode === 13) cargarDetalle(i, $tr); });
            $body.append($tr);
        });
        var resumen = [];
        $.each(k.monedas, function (codigo, total) { resumen.push(moneda(total, codigo)); });
        $("#cotKpiVigentes").text(k.VIGENTE || 0); $("#cotKpiVencidas").text(k.VENCIDA || 0); $("#cotKpiAnuladas").text(k.ANULADA || 0); $("#cotKpiTotal").text(resumen.join(" · ") || "0.00");
        if (!estado.listado.length) $("#cotDetail").html('<div class="cot-empty"><i class="icon-hand-right"></i><strong>Seleccione una cotización</strong></div>');
    }

    function cargarDetalle(indice, $tr) {
        var x = estado.listado[indice]; if (!x) return;
        $("#cotListBody tr").removeClass("is-selected"); $tr.addClass("is-selected");
        $("#cotDetail").html('<div class="cot-empty"><i class="icon-spinner icon-spin"></i><strong>Cargando detalle</strong></div>');
        get(urls.detalle, { empresa: x.IdEmpresa, idCotizacion: x.IdCotizacion })
            .done(function (r) { if (!r.ok) { avisar("error", r.msg); return; } estado.seleccionado = r.data; renderDetalle(r.data); })
            .fail(function (xhr) { avisar("error", mensajeError(xhr)); });
    }

    function renderDetalle(x) {
        var lineas = "";
        $.each(x.Detalles || [], function (_, d) {
            var referencia = numero(d.PrecioLista) <= 0
                ? ' · precio manual'
                : (Math.abs(numero(d.PrecioLista) - numero(d.PrecioUnitario)) > 0.000001
                    ? ' · lista SAP ' + numero(d.PrecioLista).toFixed(2) : '');
            lineas += '<div class="cot-detail-line"><div><strong>' + html(d.ItemCode) + ' · ' + html(d.Descripcion) + '</strong><span>' + numero(d.Cantidad) + ' ' + html(d.Unidad) + ' × ' + numero(d.PrecioUnitario).toFixed(2) + referencia + '</span></div><strong>' + html(moneda(d.Total, x.Moneda)) + '</strong></div>';
        });
        var acciones = '<button class="cot-btn cot-btn-primary cot-print"><i class="icon-print"></i> Imprimir</button>';
        if (puedeAnular && x.Estado !== "ANULADA") acciones += '<button class="cot-btn cot-btn-danger cot-cancel"><i class="icon-ban-circle"></i> Anular</button>';
        $("#cotDetail").html('<div class="cot-detail-head"><h4>' + html(x.IdCotizacion) + '</h4><p>' + html((x.IdCliente ? x.IdCliente : "Prospecto") + " · " + x.NombreCliente) + '</p></div>' +
            '<div class="cot-detail-meta"><div><small>Estado</small><strong><span class="cot-status ' + claseEstado(x.Estado) + '">' + html(x.Estado) + '</span></strong></div><div><small>Total</small><strong>' + html(moneda(x.Total, x.Moneda)) + '</strong></div><div><small>Emisión / validez</small><strong>' + html(x.Fecha) + ' / ' + html(x.ValidaHasta) + '</strong></div><div><small>Agente</small><strong>' + html(x.Agente) + '</strong></div><div><small>NIT</small><strong>' + html(x.Nit || "—") + '</strong></div><div><small>Creada por</small><strong>' + html(x.IdUsr) + '</strong></div><div><small>Pago</small><strong>' + html(x.CondicionesPago || "—") + '</strong></div><div><small>Entrega</small><strong>' + html(x.TiempoEntrega || "—") + '</strong></div><div class="cot-detail-wide"><small>Observaciones</small><strong>' + html(x.Observaciones || "—") + '</strong></div></div>' +
            '<div class="cot-detail-lines">' + lineas + '</div><div class="cot-detail-actions">' + acciones + '</div>');
        $("#cotDetail .cot-print").on("click", function () { abrirImpresion(x); });
        $("#cotDetail .cot-cancel").on("click", function () { estado.anular = x; $("#cotMotivoAnular").val(""); $("#cotAnularModal").modal("show"); });
    }

    function abrirImpresion(x) {
        window.open(urls.imprimir + "?empresa=" + encodeURIComponent(x.IdEmpresa) + "&idCotizacion=" + encodeURIComponent(x.IdCotizacion), "_blank");
    }

    function anular() {
        if (!estado.anular) return;
        var motivo = $("#cotMotivoAnular").val().trim();
        if (motivo.length < 5) { avisar("warning", "Escriba un motivo de al menos 5 caracteres."); return; }
        var $btn = $("#cotConfirmarAnular"); setBusy($btn, true, "Anulando");
        post(urls.anular, { Empresa: estado.anular.IdEmpresa, IdCotizacion: estado.anular.IdCotizacion, Motivo: motivo })
            .done(function (r) { if (!r.ok) { avisar("error", r.msg); return; } avisar("success", r.msg); $("#cotAnularModal").modal("hide"); estado.anular = null; cargarListado(); })
            .fail(function (xhr) { avisar("error", mensajeError(xhr)); })
            .always(function () { setBusy($btn, false); });
    }

    function enlazar() {
        $("#cotEmpresa").on("change", function () {
            limpiarCliente();
            poblarAgentes(empresa());
            actualizarResumenCliente();
            actualizarDisponibilidadCliente();
        });
        $("#cotAgente").on("change", function () {
            limpiarCliente();
            actualizarDisponibilidadCliente();
        });
        $("#cotBuscarCliente").on("click", abrirClientes);
        $("#cotNuevoProspecto").on("click", iniciarProspecto);
        $("#cotClienteModal").on("shown.bs.modal", function () {
            $("#cotClienteFiltro").focus();
        }).on("hidden.bs.modal", limpiarBusquedaClientes);
        $("#cotClienteFiltro").on("input", function () { programarBusquedaClientes("modal"); })
            .on("keydown", function (e) {
                if (e.which === 13) {
                    e.preventDefault();
                    $("#cotClienteResultados .cot-client-result:first").trigger("click");
                }
            });
        $("#cotClienteCodigo").on("input", function () { programarBusquedaClientes("directo"); })
            .on("keydown", function (e) {
                if (e.which === 13) {
                    e.preventDefault();
                    $("#cotClienteDropdown .cot-client-result:first").trigger("click");
                } else if (e.which === 27) {
                    ocultarResultadosClientes($("#cotClienteDropdown"));
                    restaurarClienteSeleccionado();
                }
            });
        $("#cotClienteResultados,#cotClienteDropdown").on("click", ".cot-client-result", function () {
            var cliente = $(this).data("cliente");
            if (cliente && !seleccionarCliente(cliente)) restaurarClienteSeleccionado();
        });
        $(document).on("click.cotizacionCliente", function (e) {
            if (!$(e.target).closest("#cotClienteCombobox").length) {
                ocultarResultadosClientes($("#cotClienteDropdown"));
                restaurarClienteSeleccionado();
            }
        });
        $("#cotAgregarProducto").on("click", abrirProductos);
        $("#cotBuscarProducto").on("click", function () { buscarProductos(1); });
        $("#cotProductoFiltro").on("input", function () {
            cancelarBusquedaProductos();
            estado.productoPagina = 1;
            estado.productoTieneMas = false;
            $("#cotProductoCount").text("");
            mostrarEstadoProductos("icon-spinner icon-spin", "Preparando búsqueda", "Los resultados se actualizarán al terminar de escribir.");
            estado.productoTimer = setTimeout(function () {
                estado.productoTimer = null;
                buscarProductos(1);
            }, 300);
        }).on("keydown", function (e) {
            if (e.keyCode === 13) { e.preventDefault(); buscarProductos(1); }
        });
        $("#cotProductoModal").on("hidden.bs.modal", cancelarBusquedaProductos);
        $("#cotProductoAnterior").on("click", function () { buscarProductos(estado.productoPagina - 1); });
        $("#cotProductoSiguiente").on("click", function () { buscarProductos(estado.productoPagina + 1); });
        $("#cotLineBody").on("change", "input", function () {
            var i = numero($(this).closest("tr").data("index"));
            var campo = $(this).data("field");
            if (!estado.lineas[i]) return;
            estado.lineas[i][campo] = campo === "Descripcion"
                ? $(this).val() : numero($(this).val());
            if (campo === "PrecioUnitario")
                estado.lineas[i].PrecioManual = true;
            renderLineas();
            if (campo === "Cantidad") actualizarPrecioSap(i);
        })
            .on("input", ".cot-desc", function () { var i = numero($(this).closest("tr").data("index")); if (estado.lineas[i]) estado.lineas[i].Descripcion = $(this).val(); })
            .on("click", ".cot-remove", function () { estado.lineas.splice(numero($(this).closest("tr").data("index")), 1); renderLineas(); });
        $("#cotClienteNombre,#cotNit,#cotCorreo,#cotDireccion").on("input", actualizarResumenCliente);
        $("#cotMoneda").on("change", function () { renderLineas(); actualizarResumenCliente(); });
        $("#cotGuardar").on("click", guardar); $("#cotNuevo").on("click", function () { resetFormulario(false); });
        $("#cotTabListado").on("shown.bs.tab click", function () { if (!estado.listado.length) cargarListado(); });
        $("#cotRefrescar").on("click", cargarListado);
        $("#cotFiltroEmpresa,#cotFiltroEstado,#cotFiltroDesde,#cotFiltroHasta").on("change", cargarListado);
        $("#cotFiltroTexto").on("input", function () { clearTimeout(estado.filtroTimer); estado.filtroTimer = setTimeout(cargarListado, 350); });
        $("#cotConfirmarAnular").on("click", anular);
    }

    cargarOperadores();
    enlazar();
    renderLineas();
    actualizarDisponibilidadCliente();
    if ($("#cotEmpresa option").length === 2) $("#cotEmpresa").prop("selectedIndex", 1).trigger("change");
})(jQuery);
