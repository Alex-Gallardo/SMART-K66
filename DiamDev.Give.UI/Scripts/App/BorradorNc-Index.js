(function ($) {
    "use strict";

    var $root = $("#bncApp");
    if (!$root.length) return;

    var urls = {
        clientes: $root.data("url-clientes"),
        facturas: $root.data("url-facturas"),
        estadoFactura: $root.data("url-estado-factura"),
        serie: $root.data("url-serie"),
        guardar: $root.data("url-guardar"),
        listar: $root.data("url-listar"),
        seguimiento: $root.data("url-seguimiento"),
        detalle: $root.data("url-detalle"),
        anular: $root.data("url-anular"),
        imprimir: $root.data("url-imprimir")
    };

    var state = {
        cliente: null,
        factura: null,
        facturas: [],
        lineas: [],
        seguimiento: [],
        seleccionado: null,
        seguimientoCargado: false,
        esAgente: String($root.data("es-agente")) === "true",
        puedeAnular: String($root.data("puede-anular")) === "true"
    };
    var estadoFacturaTimer = null;
    var estadoFacturaSecuencia = 0;

    function token() {
        return $root.find('input[name="__RequestVerificationToken"]').val();
    }

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : String(value)).html();
    }

    function numero(value) {
        if (typeof value === "number") return isFinite(value) ? value : 0;
        var raw = String(value == null ? "" : value).trim().replace(/\s/g, "");
        if (raw.indexOf(",") >= 0 && raw.indexOf(".") >= 0) raw = raw.replace(/,/g, "");
        else raw = raw.replace(",", ".");
        var parsed = parseFloat(raw);
        return isFinite(parsed) ? parsed : 0;
    }

    function dinero(value, moneda) {
        var n = numero(value);
        return (moneda ? escapeHtml(moneda) + " " : "") + n.toLocaleString("es-GT", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function resumenMonedas(filas) {
        var totales = {};
        $.each(filas || [], function (_, x) {
            var moneda = String(x.Moneda || "SIN MONEDA").toUpperCase();
            totales[moneda] = numero(totales[moneda]) + numero(x.Total);
        });
        var monedas = Object.keys(totales).sort();
        if (!monedas.length) return dinero(0);
        return $.map(monedas, function (moneda) {
            return dinero(totales[moneda], moneda);
        }).join(" / ");
    }

    function fechaCorta(value) {
        if (!value) return "—";
        var p = String(value).substring(0, 10).split("-");
        return p.length === 3 ? p[2] + "/" + p[1] + "/" + p[0] : value;
    }

    function hoyLocal() {
        var d = new Date();
        var mes = String(d.getMonth() + 1);
        var dia = String(d.getDate());
        return d.getFullYear() + "-" + (mes.length < 2 ? "0" + mes : mes) + "-" +
            (dia.length < 2 ? "0" + dia : dia);
    }

    function avisar(tipo, mensaje) {
        if (window.toastr && toastr[tipo]) {
            toastr.options = $.extend({}, toastr.options, {
                closeButton: true,
                progressBar: true,
                timeOut: tipo === "error" ? 6500 : 4000,
                positionClass: "toast-top-right"
            });
            toastr[tipo](mensaje);
        } else {
            window.alert(mensaje);
        }
    }

    function mensajeAjax(xhr) {
        if (xhr && xhr.responseJSON && xhr.responseJSON.msg) return xhr.responseJSON.msg;
        return "No fue posible completar la operación. Revise su conexión e inténtelo nuevamente.";
    }

    function get(url, data) {
        return $.ajax({ url: url, type: "GET", dataType: "json", cache: false, data: data || {} });
    }

    function post(url, data) {
        var payload = $.extend({}, data || {});
        payload.__RequestVerificationToken = token();
        return $.ajax({ url: url, type: "POST", dataType: "json", data: payload });
    }

    function empresa() { return $("#bncEmpresa").val() || ""; }
    function agente() { return $("#bncAgente").val() || ""; }

    function setBusy($button, busy, text) {
        if (!$button || !$button.length) return;
        if (busy) {
            $button.data("bnc-html", $button.html()).prop("disabled", true)
                .html('<span class="bnc-spinner" style="height:14px;width:14px;border-width:2px;"></span> ' + escapeHtml(text || "Procesando"));
        } else {
            $button.prop("disabled", false).html($button.data("bnc-html") || $button.html());
        }
    }

    function limpiarCliente() {
        state.cliente = null;
        $("#bncClienteCodigo,#bncClienteNombre,#bncNit,#bncDireccion,#bncCorreo").val("");
        if (!state.esAgente) $("#bncAgente").val("");
        $("#bncClientStripTitle").text("Ningún cliente seleccionado");
        $("#bncClientStripMeta").text("Elija la empresa y luego busque por código, nombre o NIT.");
        limpiarFactura();
    }

    function seleccionarCliente(cliente) {
        state.cliente = cliente;
        $("#bncClienteCodigo").val(cliente.CardCode || "");
        $("#bncClienteNombre").val(cliente.CardName || "");
        $("#bncNit").val(cliente.LicTradNum || "");
        $("#bncDireccion").val(cliente.Address || "");
        $("#bncCorreo").val(cliente.Email || "");
        $("#bncMoneda").val(normalizarMoneda(cliente.Currency || ""));
        if (!state.esAgente) $("#bncAgente").val(cliente.SlpName || "");
        $("#bncClientStripTitle").text((cliente.CardCode || "") + " · " + (cliente.CardName || ""));
        $("#bncClientStripMeta").text((cliente.LicTradNum || "Sin NIT") + " · " + (cliente.SlpName || "Sin agente") + " · " + (normalizarMoneda(cliente.Currency) || "Sin moneda"));
        limpiarFactura();
        $("#bncClienteModal").modal("hide");
    }

    function normalizarMoneda(value) {
        var m = String(value || "").toUpperCase();
        return m === "QTZ" ? "GTQ" : m;
    }

    function configurarClientes() {
        var $select = $("#bncClienteSelect");
        if (!$select.length || !$.fn.select2) return;

        $select.select2({
            placeholder: "Escriba para buscar...",
            minimumInputLength: 2,
            allowClear: true,
            ajax: {
                url: urls.clientes,
                dataType: "json",
                quietMillis: 350,
                data: function (term) {
                    return { empresa: empresa(), agente: agente(), filtro: term };
                },
                results: function (response) {
                    if (!response || !response.ok) {
                        avisar("error", response && response.msg ? response.msg : "No se pudieron consultar los clientes.");
                        return { results: [] };
                    }
                    return {
                        results: $.map(response.data || [], function (c) {
                            return { id: c.CardCode, text: c.CardCode + " · " + c.CardName, raw: c };
                        })
                    };
                }
            },
            formatResult: function (item) {
                var c = item.raw || {};
                return '<div class="bnc-main-cell"><strong>' + escapeHtml(c.CardCode) + " · " + escapeHtml(c.CardName) +
                    '</strong><small>' + escapeHtml(c.LicTradNum || "Sin NIT") + " · " + escapeHtml(c.SlpName || "Sin agente") + "</small></div>";
            },
            formatSelection: function (item) { return item.text; },
            escapeMarkup: function (markup) { return markup; }
        });
    }

    function limpiarFactura() {
        window.clearTimeout(estadoFacturaTimer);
        estadoFacturaSecuencia++;
        state.factura = null;
        $("#bncDocumento,#bncFechaDoc,#bncSerieFel,#bncNumeroFel,#bncImporte,#bncDescripcion").val("");
        $("#bncConcepto").val("");
        $("#bncSelectedDoc").addClass("is-empty").html('<i class="icon-file-text"></i><span>Seleccione una factura para visualizar su disponible.</span>');
        ocultarAlertaFactura();
    }

    function renderFacturaSeleccionada() {
        var f = state.factura;
        if (!f) { limpiarFactura(); return; }
        $("#bncSelectedDoc").removeClass("is-empty").html(
            '<div class="bnc-doc-metric"><small>Documento</small><strong>' + escapeHtml(f.DocNum) + " · " + fechaCorta(f.DocDate) + "</strong></div>" +
            '<div class="bnc-doc-metric is-money"><small>Total factura</small><strong>' + dinero(f.DocTotal, f.Moneda) + "</strong></div>" +
            '<div class="bnc-doc-metric is-money is-warning"><small>Comprometido / NC</small><strong>' + dinero(numero(f.Acumulado) + numero(f.NcPreviaSap), f.Moneda) + "</strong></div>" +
            '<div class="bnc-doc-metric is-money is-available"><small>Disponible</small><strong>' + dinero(Math.max(0, numero(f.Disponible)), f.Moneda) + "</strong></div>"
        );
        $("#bncDocumento").val(f.DocNum || "");
        $("#bncFechaDoc").val(String(f.DocDate || "").substring(0, 10));
        $("#bncSerieFel").val(f.SerieFel || "");
        $("#bncNumeroFel").val(f.NumeroFel || "");
        mostrarAlertasFactura(f);
    }

    function mostrarAlertasFactura(f) {
        var mensajes = [];
        var clase = "is-warning";
        var importeActual = numero($("#bncImporte").val());
        if (f.GeneraSaldoAFavor) mensajes.push("La factura está pagada; la NC generará saldo a favor.");
        if (numero(f.NcPreviaSap) > 0) mensajes.push("Existen " + dinero(f.NcPreviaSap, f.Moneda) + " en notas de crédito previas de SAP.");
        if (numero(f.Acumulado) > 0) mensajes.push("Ya está comprometida en " + escapeHtml(f.BorradoresRelacionados || "otros borradores") + ".");
        if (numero(f.Disponible) <= 0) {
            mensajes.push("El documento no tiene disponible para otro borrador.");
            clase = "is-danger";
        } else if (importeActual - numero(f.Disponible) > 0.005) {
            mensajes.push("El importe ingresado supera el disponible actualizado.");
            clase = "is-danger";
        }
        if (!mensajes.length) { ocultarAlertaFactura(); return; }
        $("#bncFacturaAlert").attr("class", "bnc-inline-alert is-visible " + clase)
            .html('<i class="icon-warning-sign"></i><span>' + mensajes.join(" ") + "</span>");
    }

    function ocultarAlertaFactura() {
        $("#bncFacturaAlert").attr("class", "bnc-inline-alert").empty();
    }

    function programarEstadoFactura() {
        window.clearTimeout(estadoFacturaTimer);
        if (!state.factura) return;

        var documento = String(state.factura.DocNum || "");
        var secuencia = ++estadoFacturaSecuencia;
        estadoFacturaTimer = window.setTimeout(function () {
            get(urls.estadoFactura, {
                empresa: empresa(),
                documento: documento,
                docTotal: numero(state.factura.DocTotal),
                pagado: numero(state.factura.Pagado)
            }).done(function (r) {
                if (secuencia !== estadoFacturaSecuencia || !state.factura ||
                    String(state.factura.DocNum || "") !== documento) return;

                if (!r || !r.ok) {
                    avisar("warning", r && r.msg ? r.msg :
                        "No se pudo actualizar el disponible de la factura.");
                    return;
                }

                var actual = r.data || {};
                $.extend(state.factura, {
                    Acumulado: numero(actual.Acumulado),
                    NcPreviaSap: numero(actual.NcPreviaSap),
                    Disponible: numero(actual.Disponible),
                    DisponibleNeto: numero(actual.DisponibleNeto),
                    GeneraSaldoAFavor: !!actual.GeneraSaldoAFavor,
                    BorradoresRelacionados: actual.BorradoresRelacionados || ""
                });
                renderFacturaSeleccionada();
            }).fail(function (xhr) {
                if (secuencia === estadoFacturaSecuencia)
                    avisar("warning", mensajeAjax(xhr));
            });
        }, 500);
    }

    function cargarSerie() {
        var emp = empresa();
        $("#bncPrefijo").text("—");
        if (!emp) return;
        get(urls.serie, { empresa: emp }).done(function (r) {
            if (r && r.ok) $("#bncPrefijo").text((r.data && r.data.Prefijo) || "Sin serie");
            else avisar("warning", r && r.msg ? r.msg : "No se encontró la serie.");
        }).fail(function (xhr) { avisar("error", mensajeAjax(xhr)); });
    }

    function cargarFacturas() {
        if (!empresa()) { avisar("warning", "Seleccione una empresa."); return; }
        if (!state.cliente) { avisar("warning", "Seleccione un cliente."); return; }
        var $button = $("#bncCargarFacturas");
        setBusy($button, true, "Buscando");
        $("#bncFacturaBody").empty();
        $("#bncFacturaEmpty").html('<div class="bnc-loading"><span class="bnc-spinner"></span>Consultando SAP y borradores vigentes...</div>').show();

        get(urls.facturas, {
            empresa: empresa(),
            clienteId: state.cliente.CardCode,
            agente: agente(),
            filtro: $("#bncFacturaFiltro").val() || ""
        }).done(function (r) {
            if (!r || !r.ok) {
                avisar("error", r && r.msg ? r.msg : "No se pudieron consultar las facturas.");
                state.facturas = [];
            } else state.facturas = r.data || [];
            renderFacturas();
        }).fail(function (xhr) {
            avisar("error", mensajeAjax(xhr));
            state.facturas = [];
            renderFacturas();
        }).always(function () { setBusy($button, false); });
    }

    function renderFacturas() {
        var html = "";
        $.each(state.facturas, function (i, f) {
            var flags = [];
            if (f.GeneraSaldoAFavor) flags.push('<span class="bnc-paid-flag">Pagada</span>');
            if (numero(f.NcPreviaSap) > 0) flags.push('<span class="bnc-nc-flag">Con NC</span>');
            html += '<tr data-index="' + i + '">' +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(f.DocNum) + '</strong><small>' + escapeHtml(f.SerieFel || "") + " " + escapeHtml(f.NumeroFel || "") + "</small></td>" +
                "<td>" + fechaCorta(f.DocDate) + "</td>" +
                "<td>" + escapeHtml(f.Moneda) + "</td>" +
                '<td class="bnc-money">' + dinero(f.DocTotal) + "</td>" +
                '<td class="bnc-money">' + dinero(f.Acumulado) + "</td>" +
                '<td class="bnc-money">' + dinero(f.NcPreviaSap) + "</td>" +
                '<td class="bnc-money" style="color:' + (numero(f.Disponible) > 0 ? "#047857" : "#b91c1c") + '">' + dinero(Math.max(0, numero(f.Disponible))) + "</td>" +
                "<td>" + (flags.join(" ") || '<span class="text-muted">Sin alertas</span>') + "</td></tr>";
        });
        $("#bncFacturaBody").html(html);
        $("#bncFacturaEmpty").toggle(!state.facturas.length).html(
            '<i class="icon-inbox"></i><strong>Sin facturas disponibles</strong><span>Pruebe otro número o confirme el cliente.</span>');
        $("#bncFacturaCount").text(state.facturas.length + (state.facturas.length === 1 ? " factura" : " facturas"));
    }

    function seleccionarFactura(index) {
        var f = state.facturas[index];
        if (!f) return;
        var monedaEnc = normalizarMoneda($("#bncMoneda").val());
        var monedaDoc = normalizarMoneda(f.Moneda);
        if (state.lineas.length && monedaEnc && monedaEnc !== monedaDoc) {
            avisar("warning", "El borrador ya contiene líneas en " + monedaEnc + ". Cree otro borrador para " + monedaDoc + ".");
            return;
        }
        if (numero(f.Disponible) <= 0) {
            avisar("warning", "Esta factura ya no tiene monto disponible.");
            return;
        }
        state.factura = $.extend({}, f, { Moneda: monedaDoc });
        $("#bncMoneda").val(monedaDoc);
        renderFacturaSeleccionada();
        $("#bncFacturaModal").modal("hide");
        $("#bncConcepto").focus();
    }

    function validarLinea() {
        if (!state.factura) return "Seleccione una factura.";
        if (!$("#bncConcepto").val()) return "Seleccione el concepto.";
        if (!$("#bncDescripcion").val().trim()) return "Escriba la descripción de la línea.";
        var importe = numero($("#bncImporte").val());
        if (importe <= 0) return "El importe debe ser mayor a cero.";
        if (importe - numero(state.factura.Disponible) > 0.005) return "El importe supera el disponible de la factura.";
        if (String(state.factura.DocDate).substring(0, 10) > $("#bncFecha").val()) return "La fecha de la factura es posterior a la fecha del borrador.";
        var repetida = $.grep(state.lineas, function (l) { return String(l.Documento) === String(state.factura.DocNum); }).length;
        if (repetida) return "La factura ya está agregada al borrador.";
        var moneda = normalizarMoneda($("#bncMoneda").val());
        if (moneda && moneda !== normalizarMoneda(state.factura.Moneda)) return "La factura está en una moneda distinta al borrador.";
        return "";
    }

    function agregarLinea() {
        var error = validarLinea();
        if (error) { avisar("warning", error); return; }
        var f = state.factura;
        state.lineas.push({
            Concepto: $("#bncConcepto").val(),
            Documento: f.DocNum,
            FechaDoc: String(f.DocDate).substring(0, 10),
            SerieFel: f.SerieFel || "",
            NumeroFel: f.NumeroFel || "",
            TotalFactura: numero(f.DocTotal),
            Pagado: numero(f.Pagado),
            NcPreviaSap: numero(f.NcPreviaSap),
            Moneda: normalizarMoneda(f.Moneda),
            Descripcion: $("#bncDescripcion").val().trim(),
            Importe: numero($("#bncImporte").val()),
            Disponible: numero(f.Disponible)
        });
        renderLineas();
        limpiarFactura();
        $("#bncBuscarFactura").focus();
    }

    function renderLineas() {
        var html = "", total = 0;
        $.each(state.lineas, function (i, l) {
            total += numero(l.Importe);
            html += "<tr>" +
                "<td>" + escapeHtml(l.Concepto) + "</td>" +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(l.Documento) + '</strong><small>' + escapeHtml(l.Moneda) + "</small></td>" +
                "<td>" + fechaCorta(l.FechaDoc) + "</td>" +
                "<td>" + escapeHtml((l.SerieFel || "") + " " + (l.NumeroFel || "")) + "</td>" +
                '<td class="bnc-money">' + dinero(l.TotalFactura) + "</td>" +
                '<td class="bnc-money">' + dinero(l.Disponible) + "</td>" +
                "<td>" + escapeHtml(l.Descripcion) + "</td>" +
                '<td class="bnc-money">' + dinero(l.Importe) + "</td>" +
                '<td><button type="button" class="bnc-icon-btn bnc-remove-line" data-index="' + i + '" title="Quitar línea"><i class="icon-trash"></i></button></td></tr>';
        });
        $("#bncLineBody").html(html);
        $("#bncLineEmpty").toggle(!state.lineas.length);
        $("#bncLineCount").text(state.lineas.length + (state.lineas.length === 1 ? " línea" : " líneas"));
        $("#bncTotal").text(dinero(total, $("#bncMoneda").val()));
        $("#bncMoneda").prop("disabled", state.lineas.length > 0);
    }

    function validarBorrador() {
        if (!empresa()) return "Seleccione una empresa.";
        if (!$("#bncFecha").val()) return "Seleccione la fecha.";
        if (!state.cliente) return "Seleccione un cliente.";
        if (!$("#bncNit").val().trim()) return "El cliente no tiene NIT registrado.";
        if (!agente().trim()) return "No se pudo determinar el agente.";
        if (!$("#bncMoneda").val()) return "Seleccione la moneda.";
        if (!state.lineas.length) return "Agregue al menos una línea.";
        return "";
    }

    function guardar() {
        var error = validarBorrador();
        if (error) { avisar("warning", error); return; }
        var $button = $("#bncGuardar");
        setBusy($button, true, "Guardando");

        var payload = {
            IdEmpresa: empresa(),
            Fecha: $("#bncFecha").val(),
            IdCliente: state.cliente.CardCode,
            Nombre: state.cliente.CardName,
            Nit: $("#bncNit").val(),
            Direccion: $("#bncDireccion").val(),
            Correo: $("#bncCorreo").val(),
            Agente: agente(),
            Moneda: $("#bncMoneda").val()
        };
        $.each(state.lineas, function (i, linea) {
            $.each(linea, function (propiedad, valor) {
                if (propiedad !== "Disponible")
                    payload["Detalles[" + i + "]." + propiedad] = valor;
            });
        });

        post(urls.guardar, payload).done(function (r) {
            if (!r || !r.ok) { avisar("error", r && r.msg ? r.msg : "No fue posible guardar el borrador."); return; }
            avisar("success", r.msg || "Borrador guardado.");
            $.each(r.advertencias || [], function (_, m) { avisar("warning", m); });
            var emp = empresa(), id = r.idBorrador;
            resetFormulario(false);
            state.seguimientoCargado = false;
            if (id && window.confirm("El borrador " + id + " fue creado. ¿Desea abrir la impresión?"))
                abrirImpresion(emp, id);
        }).fail(function (xhr) { avisar("error", mensajeAjax(xhr)); })
          .always(function () { setBusy($button, false); });
    }

    function resetFormulario(conservarEmpresa) {
        var emp = conservarEmpresa ? empresa() : "";
        state.lineas = [];
        limpiarCliente();
        $("#bncEmpresa").val(emp);
        $("#bncFecha").val(hoyLocal());
        $("#bncMoneda").val("").prop("disabled", false);
        $("#bncPrefijo").text("—");
        if (state.esAgente && emp) {
            var $op = $("#bncEmpresa option:selected");
            $("#bncAgente").val($op.data("agent") || "");
        }
        renderLineas();
        if (emp) cargarSerie();
    }

    function statusClass(estado) {
        return "bnc-status bnc-status-" + String(estado || "").toLowerCase();
    }

    function cargarSeguimiento() {
        var empresaFiltro = $("#bncFiltroEmpresa").val() || "";
        var desde = $("#bncFiltroDesde").val() || "";
        var hasta = $("#bncFiltroHasta").val() || "";
        $("#bncFollowBody").html('<tr><td colspan="7"><div class="bnc-loading"><span class="bnc-spinner"></span>Actualizando borradores...</div></td></tr>');
        $("#bncFollowEmpty").hide();

        var pendientes = null, resueltos = null, fallido = false;
        function completar() {
            if (pendientes === null || resueltos === null) return;
            state.seguimiento = (pendientes || []).concat(resueltos || []);
            state.seguimiento.sort(function (a, b) { return String(b.Fecha + b.IdBorrador).localeCompare(String(a.Fecha + a.IdBorrador)); });
            state.seguimientoCargado = !fallido;
            renderSeguimiento();
        }

        get(urls.listar, { empresa: empresaFiltro }).done(function (r) {
            if (!r || !r.ok) { fallido = true; avisar("error", r && r.msg ? r.msg : "No se pudieron cargar pendientes."); pendientes = []; }
            else pendientes = r.data || [];
            completar();
        }).fail(function (xhr) { fallido = true; pendientes = []; avisar("error", mensajeAjax(xhr)); completar(); });

        get(urls.seguimiento, { empresa: empresaFiltro, desde: desde, hasta: hasta }).done(function (r) {
            if (!r || !r.ok) { fallido = true; avisar("error", r && r.msg ? r.msg : "No se pudo cargar el seguimiento."); resueltos = []; }
            else resueltos = r.data || [];
            completar();
        }).fail(function (xhr) { fallido = true; resueltos = []; avisar("error", mensajeAjax(xhr)); completar(); });
    }

    function filasFiltradas() {
        var estado = $("#bncFiltroEstado").val() || "";
        var texto = String($("#bncFiltroTexto").val() || "").toLowerCase();
        var desde = $("#bncFiltroDesde").val() || "";
        var hasta = $("#bncFiltroHasta").val() || "";
        return $.grep(state.seguimiento, function (x) {
            if (estado && x.Estado !== estado) return false;
            var fecha = String(x.Fecha || "").substring(0, 10);
            if (desde && fecha < desde) return false;
            if (hasta && fecha > hasta) return false;
            if (!texto) return true;
            return [x.IdBorrador, x.IdEmpresa, x.IdCliente, x.Nombre, x.Agente, x.IdUsr]
                .join(" ").toLowerCase().indexOf(texto) >= 0;
        });
    }

    function renderSeguimiento() {
        var filas = filasFiltradas(), html = "";
        $.each(filas, function (i, x) {
            html += '<tr data-empresa="' + escapeHtml(x.IdEmpresa) + '" data-id="' + escapeHtml(x.IdBorrador) + '">' +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(x.IdBorrador) + '</strong><small>' + escapeHtml(x.IdUsr) + "</small></td>" +
                "<td>" + escapeHtml(x.IdEmpresa) + "</td>" +
                "<td>" + fechaCorta(x.Fecha) + "</td>" +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(x.Nombre) + '</strong><small>' + escapeHtml(x.IdCliente) + "</small></td>" +
                "<td>" + escapeHtml(x.Agente) + "</td>" +
                '<td><span class="' + statusClass(x.Estado) + '">' + escapeHtml(x.Estado) + "</span></td>" +
                '<td class="bnc-money">' + dinero(x.Total, x.Moneda) + "</td></tr>";
        });
        $("#bncFollowBody").html(html);
        $("#bncFollowEmpty").toggle(!filas.length);
        renderKpis();
    }

    function renderKpis() {
        var p = 0, a = 0, r = 0;
        var filas = filasFiltradas();
        $.each(filas, function (_, x) {
            if (x.Estado === "PENDIENTE") p++;
            else if (x.Estado === "AUTORIZADO") a++;
            else r++;
        });
        $("#bncKpiPendientes").text(p);
        $("#bncKpiAutorizados").text(a);
        $("#bncKpiRechazados").text(r);
        $("#bncKpiMonto").text(resumenMonedas(filas));
    }

    function cargarDetalle(empresaId, id) {
        state.seleccionado = { empresa: empresaId, id: id };
        $("#bncFollowBody tr").removeClass("is-selected").filter(function () {
            return $(this).data("empresa") === empresaId && String($(this).data("id")) === String(id);
        }).addClass("is-selected");
        $("#bncFollowDetail").html('<div class="bnc-loading"><span class="bnc-spinner"></span>Cargando detalle...</div>');
        get(urls.detalle, { empresa: empresaId, idBorrador: id }).done(function (r) {
            if (!r || !r.ok) { avisar("error", r && r.msg ? r.msg : "No se pudo abrir el detalle."); return; }
            renderDetalle(r.data);
        }).fail(function (xhr) { avisar("error", mensajeAjax(xhr)); });
    }

    function renderDetalle(x) {
        var lineas = "";
        $.each(x.Detalles || [], function (_, d) {
            lineas += "<tr>" +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(d.Documento) + '</strong><small>' + escapeHtml(d.Concepto) + "</small></td>" +
                "<td>" + fechaCorta(d.FechaDoc) + "</td>" +
                "<td>" + escapeHtml(d.Descripcion) + "</td>" +
                '<td class="bnc-money">' + dinero(d.Importe, d.Moneda) + "</td></tr>";
        });
        var motivo = x.MotivoResolucion
            ? '<div class="bnc-detail-note is-visible"><strong>Motivo:</strong> ' + escapeHtml(x.MotivoResolucion) + "</div>" : "";
        var anular = state.puedeAnular && x.Estado === "AUTORIZADO"
            ? '<button class="bnc-btn bnc-btn-danger" type="button" id="bncAnularSeleccionado"><i class="icon-ban-circle"></i> Anular</button>' : "";

        $("#bncFollowDetail").html(
            '<div class="bnc-detail-hero"><div class="bnc-detail-hero-top"><div><h4>' + escapeHtml(x.IdBorrador) + "</h4><p>" + escapeHtml(x.IdEmpresa) + " · " + fechaCorta(x.Fecha) +
            '</p></div><span class="' + statusClass(x.Estado) + '">' + escapeHtml(x.Estado) + "</span></div></div>" +
            '<div class="bnc-detail-meta"><div><small>Cliente</small><strong>' + escapeHtml(x.IdCliente + " · " + x.Nombre) +
            "</strong></div><div><small>Agente</small><strong>" + escapeHtml(x.Agente) +
            "</strong></div><div><small>NIT</small><strong>" + escapeHtml(x.Nit || "—") +
            "</strong></div><div><small>Total</small><strong>" + dinero(x.Total, x.Moneda) +
            "</strong></div><div><small>Capturado por</small><strong>" + escapeHtml(x.IdUsr) +
            "</strong></div><div><small>Resolución</small><strong>" + escapeHtml(x.ResueltoPor || "Pendiente") + " " + escapeHtml(x.FechaResolucion || "") + "</strong></div></div>" +
            motivo +
            '<div class="bnc-table-wrap" style="border-width:1px 0 0;border-radius:0;"><table class="table bnc-table" style="min-width:620px"><thead><tr><th>Documento</th><th>Fecha</th><th>Descripción</th><th class="text-right">Importe</th></tr></thead><tbody>' + lineas + "</tbody></table></div>" +
            '<div class="bnc-decision-bar"><button class="bnc-btn bnc-btn-ghost" type="button" id="bncImprimirSeleccionado"><i class="icon-print"></i> Imprimir</button>' + anular + "</div>"
        );
        state.seleccionado.documento = x;
    }

    function abrirImpresion(emp, id) {
        window.open(urls.imprimir + "?empresa=" + encodeURIComponent(emp) + "&idBorrador=" + encodeURIComponent(id), "_blank");
    }

    function confirmarAnulacion() {
        var motivo = $("#bncMotivoAnular").val().trim();
        if (!motivo) { avisar("warning", "Indique el motivo de la anulación."); return; }
        if (!state.seleccionado) return;
        var $button = $("#bncConfirmarAnular");
        setBusy($button, true, "Anulando");
        post(urls.anular, {
            Empresa: state.seleccionado.empresa,
            IdBorrador: state.seleccionado.id,
            Motivo: motivo
        }).done(function (r) {
            if (!r || !r.ok) { avisar("error", r && r.msg ? r.msg : "No se pudo anular."); return; }
            avisar("success", r.msg || "Borrador anulado.");
            $("#bncAnularModal").modal("hide");
            state.seleccionado = null;
            cargarSeguimiento();
            $("#bncFollowDetail").html('<div class="bnc-empty"><i class="icon-hand-right"></i><strong>Seleccione un borrador</strong><span>Su detalle y acciones aparecerán aquí.</span></div>');
        }).fail(function (xhr) { avisar("error", mensajeAjax(xhr)); })
          .always(function () { setBusy($button, false); });
    }

    function enlazarEventos() {
        $("#bncEmpresa").on("change", function () {
            var option = $(this).find("option:selected");
            limpiarCliente();
            state.lineas = [];
            renderLineas();
            if (state.esAgente) $("#bncAgente").val(option.data("agent") || "");
            cargarSerie();
        });

        $("#bncBuscarCliente").on("click", function () {
            if (!empresa()) { avisar("warning", "Seleccione una empresa antes de buscar clientes."); return; }
            $("#bncClienteSelect").select2("val", "");
            $("#bncClienteModal").modal("show");
        });
        $("#bncSeleccionarCliente").on("click", function () {
            var item = $("#bncClienteSelect").select2("data");
            if (!item || !item.raw) { avisar("warning", "Seleccione un cliente de la lista."); return; }
            seleccionarCliente(item.raw);
        });

        $("#bncBuscarFactura").on("click", function () {
            if (!state.cliente) { avisar("warning", "Seleccione un cliente antes de buscar facturas."); return; }
            $("#bncFacturaModal").modal("show");
            cargarFacturas();
        });
        $("#bncCargarFacturas").on("click", cargarFacturas);
        $("#bncFacturaFiltro").on("keydown", function (e) { if (e.which === 13) { e.preventDefault(); cargarFacturas(); } });
        $("#bncFacturaBody").on("click dblclick", "tr", function () { seleccionarFactura(Number($(this).data("index"))); });

        $("#bncAgregarLinea").on("click", agregarLinea);
        $("#bncImporte").on("input", programarEstadoFactura);
        $("#bncLineBody").on("click", ".bnc-remove-line", function () {
            state.lineas.splice(Number($(this).data("index")), 1);
            renderLineas();
        });
        $("#bncNuevo").on("click", function () {
            if (state.lineas.length && !window.confirm("Se perderán las líneas no guardadas. ¿Desea crear un borrador nuevo?")) return;
            resetFormulario(false);
        });
        $("#bncCancelar").on("click", function () {
            if (state.lineas.length && !window.confirm("Se perderán las líneas no guardadas. ¿Desea continuar?")) return;
            resetFormulario(true);
        });
        $("#bncGuardar").on("click", guardar);

        $("#bncTabSeguimiento").on("shown.bs.tab", function () { if (!state.seguimientoCargado) cargarSeguimiento(); });
        $("#bncRefrescarSeguimiento").on("click", cargarSeguimiento);
        $("#bncFiltroEmpresa,#bncFiltroDesde,#bncFiltroHasta").on("change", cargarSeguimiento);
        $("#bncFiltroEstado").on("change", renderSeguimiento);
        $("#bncFiltroTexto").on("input", renderSeguimiento);
        $("#bncFollowBody").on("click", "tr", function () { cargarDetalle($(this).data("empresa"), $(this).data("id")); });
        $("#bncFollowDetail").on("click", "#bncImprimirSeleccionado", function () {
            if (state.seleccionado) abrirImpresion(state.seleccionado.empresa, state.seleccionado.id);
        }).on("click", "#bncAnularSeleccionado", function () {
            $("#bncMotivoAnular").val("");
            $("#bncAnularModal").modal("show");
        });
        $("#bncConfirmarAnular").on("click", confirmarAnulacion);
    }

    $(function () {
        configurarClientes();
        enlazarEventos();
        renderLineas();
        if ($("#bncEmpresa option").length === 2) $("#bncEmpresa").val($("#bncEmpresa option:eq(1)").val()).trigger("change");
    });
})(window.jQuery);
