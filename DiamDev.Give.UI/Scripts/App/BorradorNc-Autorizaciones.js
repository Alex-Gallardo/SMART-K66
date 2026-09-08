(function ($) {
    "use strict";

    var $root = $("#bncAuthApp");
    if (!$root.length) return;

    var urls = {
        listar: $root.data("url-listar"),
        detalle: $root.data("url-detalle"),
        detalleFacturas: $root.data("url-detalle-facturas"),
        facturaBorrador: $root.data("url-factura-borrador"),
        documentosPrevios: $root.data("url-documentos-previos"),
        documentoPrevio: $root.data("url-documento-previo"),
        adjunto: $root.data("url-adjunto"),
        resolver: $root.data("url-resolver"),
        imprimir: $root.data("url-imprimir")
    };
    var state = { pendientes: [], seleccionado: null, documento: null };
    var cargaSecuencia = 0;
    var detalleSecuencia = 0;
    var facturasDetalle = window.BorradorNcFacturasDetalle.crear({
        id: "bncAuthInvoices",
        url: urls.detalleFacturas
    });
    var documentosPrevios = window.BorradorNcDocumentosPrevios.crear({
        id: "bncAuthPriorDocuments",
        url: urls.documentosPrevios,
        detalleUrl: urls.documentoPrevio,
        origen: "autorizaciones"
    });

    function token() { return $root.find('input[name="__RequestVerificationToken"]').val(); }
    function escapeHtml(value) { return $("<div>").text(value == null ? "" : String(value)).html(); }
    function numero(value) { var n = parseFloat(value); return isFinite(n) ? n : 0; }
    function dinero(value, moneda) {
        return (moneda ? escapeHtml(moneda) + " " : "") + numero(value).toLocaleString("es-GT", {
            minimumFractionDigits: 2, maximumFractionDigits: 2
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
    function fecha(value) {
        return window.BorradorNcFechas.corta(value);
    }
    function avisar(tipo, mensaje) {
        if (window.toastr && toastr[tipo]) {
            toastr.options = $.extend({}, toastr.options, { closeButton: true, progressBar: true, timeOut: tipo === "error" ? 6500 : 4000 });
            toastr[tipo](mensaje);
        } else window.alert(mensaje);
    }
    function mensajeAjax(xhr) {
        return xhr && xhr.responseJSON && xhr.responseJSON.msg
            ? xhr.responseJSON.msg
            : "No fue posible completar la operación.";
    }
    function get(url, data) { return $.ajax({ url: url, type: "GET", dataType: "json", cache: false, data: data || {} }); }
    function post(url, data) {
        var payload = $.extend({}, data || {});
        payload.__RequestVerificationToken = token();
        return $.ajax({ url: url, type: "POST", dataType: "json", data: payload });
    }
    function setBusy($button, busy, label) {
        if (busy) $button.data("html", $button.html()).prop("disabled", true)
            .html('<span class="bnc-spinner" style="height:14px;width:14px;border-width:2px;"></span> ' + escapeHtml(label));
        else $button.prop("disabled", false).html($button.data("html"));
    }

    function cargar() {
        var secuencia = ++cargaSecuencia;
        detalleSecuencia++;
        facturasDetalle.invalidar();
        documentosPrevios.invalidar();
        state.seleccionado = null;
        state.documento = null;
        $("#bncAuthDetail").html('<div class="bnc-empty"><i class="icon-hand-left"></i><strong>Seleccione una solicitud</strong><span>Revise cada línea y sus documentos previos de SAP antes de autorizar.</span></div>');
        $("#bncAuthBody").html('<tr><td colspan="5"><div class="bnc-loading"><span class="bnc-spinner"></span>Consultando pendientes...</div></td></tr>');
        $("#bncAuthEmpty").hide();
        get(urls.listar, { empresa: $("#bncAuthEmpresa").val() || "" }).done(function (r) {
            if (secuencia !== cargaSecuencia) return;
            if (!r || !r.ok) {
                state.pendientes = [];
                avisar("error", r && r.msg ? r.msg : "No se pudo abrir la bandeja.");
            } else state.pendientes = r.data || [];
            renderLista();
        }).fail(function (xhr) {
            if (secuencia !== cargaSecuencia) return;
            state.pendientes = [];
            avisar("error", mensajeAjax(xhr));
            renderLista();
        });
    }

    function filtrados() {
        var q = String($("#bncAuthSearch").val() || "").toLowerCase();
        if (!q) return state.pendientes;
        return $.grep(state.pendientes, function (x) {
            return [x.IdBorrador, x.IdEmpresa, x.IdCliente, x.Nombre, x.Agente, x.IdUsr]
                .join(" ").toLowerCase().indexOf(q) >= 0;
        });
    }

    function renderLista() {
        var filas = filtrados(), html = "", conNc = 0;
        $.each(filas, function (_, x) {
            if (x.TieneNcPrevia) conNc++;
            html += '<tr data-empresa="' + escapeHtml(x.IdEmpresa) + '" data-id="' + escapeHtml(x.IdBorrador) + '">' +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(x.IdBorrador) + '</strong><small>' + escapeHtml(x.IdEmpresa) + " · " + escapeHtml(x.IdUsr) + "</small></td>" +
                "<td>" + fecha(x.Fecha) + "</td>" +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(x.Nombre) + '</strong><small>' + escapeHtml(x.IdCliente) + (x.TieneNcPrevia ? ' · <span style="color:#a16207">Con antecedentes SAP</span>' : "") + "</small></td>" +
                "<td>" + escapeHtml(x.Agente) + "</td>" +
                '<td class="bnc-money">' + dinero(x.Total, x.Moneda) + "</td></tr>";
        });
        $("#bncAuthBody").html(html);
        $("#bncAuthEmpty").toggle(!filas.length);
        $("#bncAuthResultCount").text(filas.length + (filas.length === 1 ? " resultado" : " resultados"));
        $("#bncAuthKpiCount").text(filas.length);
        $("#bncAuthKpiMonto").text(resumenMonedas(filas));
        $("#bncAuthKpiNc").text(conNc);
    }

    function seleccionar(empresa, id) {
        var secuencia = ++detalleSecuencia;
        facturasDetalle.cancelar();
        documentosPrevios.cancelar();
        state.seleccionado = { empresa: empresa, id: id };
        $("#bncAuthBody tr").removeClass("is-selected").filter(function () {
            return $(this).data("empresa") === empresa && String($(this).data("id")) === String(id);
        }).addClass("is-selected");
        $("#bncAuthDetail").html('<div class="bnc-loading"><span class="bnc-spinner"></span>Cargando solicitud y antecedentes...</div>');
        get(urls.detalle, { empresa: empresa, idBorrador: id }).done(function (r) {
            if (secuencia !== detalleSecuencia || !state.seleccionado ||
                state.seleccionado.empresa !== empresa || String(state.seleccionado.id) !== String(id)) return;
            if (!r || !r.ok) { avisar("error", r && r.msg ? r.msg : "No se pudo cargar el detalle."); return; }
            state.documento = r.data;
            renderDetalle(r.data);
            facturasDetalle.cargar(r.data);
            documentosPrevios.cargar(r.data);
        }).fail(function (xhr) {
            if (secuencia === detalleSecuencia) avisar("error", mensajeAjax(xhr));
        });
    }

    function renderDetalle(x) {
        var lineas = "";
        $.each(x.Detalles || [], function (i, d) {
            var alertas = [];
            var urlFactura = window.BorradorNcFacturasDetalle.urlFacturaBorrador(
                urls.facturaBorrador, x, d, "autorizaciones");
            if (numero(d.Pagado) >= numero(d.TotalFactura) - .005) alertas.push('<span class="bnc-paid-flag">Pagada</span>');
            if (numero(d.NcPreviaSap) > 0) alertas.push('<span class="bnc-nc-flag">Antecedentes SAP ' + dinero(d.NcPreviaSap) + "</span>");
            lineas += '<tr><td class="bnc-linked-invoice-action-cell"><a class="bnc-btn bnc-btn-primary bnc-linked-invoice-action" href="' + escapeHtml(urlFactura) +
                '" target="_blank" rel="noopener noreferrer" title="Abrir el detalle completo en una pestaña nueva" ' +
                'aria-label="Ver factura ' + escapeHtml(d.Documento) + ' en una pestaña nueva">' +
                '<i class="icon-external-link" aria-hidden="true"></i><span>Ver factura</span></a></td>' +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(d.Documento) + '</strong><small>' + escapeHtml(d.Concepto) + "</small></td>" +
                "<td>" + fecha(d.FechaDoc) + "</td>" +
                '<td class="bnc-money">' + dinero(d.TotalFactura, d.Moneda) + "</td>" +
                '<td class="bnc-money">' + dinero(d.Importe, d.Moneda) + "</td>" +
                "<td>" + (alertas.join(" ") || '<span class="text-muted">Sin alertas</span>') + "</td></tr>";
        });

        $("#bncAuthDetail").html(
            '<div class="bnc-detail-hero"><div class="bnc-detail-hero-top"><div><h4>' + escapeHtml(x.IdBorrador) + "</h4><p>" + escapeHtml(x.IdEmpresa) + " · Capturado por " + escapeHtml(x.IdUsr) +
            '</p></div><span class="bnc-status bnc-status-pendiente">Pendiente</span></div></div>' +
            '<div class="bnc-detail-meta"><div><small>Cliente</small><strong>' + escapeHtml(x.IdCliente + " · " + x.Nombre) +
            "</strong></div><div><small>NIT</small><strong>" + escapeHtml(x.Nit || "—") +
            "</strong></div><div><small>Agente</small><strong>" + escapeHtml(x.Agente) +
            "</strong></div><div><small>Fecha</small><strong>" + fecha(x.Fecha) +
            "</strong></div><div><small>Moneda</small><strong>" + escapeHtml(x.Moneda) +
            "</strong></div><div><small>Total solicitado</small><strong>" + dinero(x.Total, x.Moneda) + "</strong></div></div>" +
            '<div class="bnc-table-wrap bnc-linked-invoice-table-wrap" style="border-width:1px 0;border-radius:0"><table class="table bnc-table bnc-linked-invoice-table is-authorization"><thead><tr><th class="text-center">Acción</th><th>Documento</th><th>Fecha</th><th class="text-right">Total factura</th><th class="text-right">Solicitado</th><th>Alertas</th></tr></thead><tbody>' + lineas + "</tbody></table></div>" +
            window.BorradorNcFacturasDetalle.plantilla("bncAuthInvoices", {
                titulo: "Productos a revisar",
                subtitulo: "Contenido completo de las facturas incluidas en este borrador."
            }) +
            window.BorradorNcAdjuntos.plantilla(x, {
                baseUrl: urls.adjunto,
                titulo: "Documentación de respaldo",
                subtitulo: "Evidencias proporcionadas por quien creó la solicitud."
            }) +
            window.BorradorNcDocumentosPrevios.plantilla("bncAuthPriorDocuments", {
                subtitulo: "Revise el detalle de cada antecedente antes de tomar una decisión."
            }) +
            '<div class="bnc-decision-bar"><button class="bnc-btn bnc-btn-ghost" type="button" id="bncAuthPrint"><i class="icon-print"></i> Imprimir</button>' +
            '<button class="bnc-btn bnc-btn-danger" type="button" id="bncAuthReject"><i class="icon-remove"></i> Rechazar</button>' +
            '<button class="bnc-btn bnc-btn-success" type="button" id="bncAuthApprove"><i class="icon-check"></i> Autorizar</button></div>'
        );
    }

    function resolver(accion, motivo, $button) {
        if (!state.seleccionado) return;
        setBusy($button, true, accion === "AUTORIZADO" ? "Autorizando" : "Rechazando");
        post(urls.resolver, {
            Empresa: state.seleccionado.empresa,
            IdBorrador: state.seleccionado.id,
            Accion: accion,
            Motivo: motivo || ""
        }).done(function (r) {
            if (!r || !r.ok) { avisar("error", r && r.msg ? r.msg : "No se pudo registrar la decisión."); return; }
            avisar("success", r.msg || "Decisión registrada.");
            $("#bncRejectModal,#bncApproveModal").modal("hide");
            cargar();
        }).fail(function (xhr) { avisar("error", mensajeAjax(xhr)); })
          .always(function () { setBusy($button, false); });
    }

    function enlazar() {
        $("#bncAuthRefresh").on("click", cargar);
        $("#bncAuthEmpresa").on("change", cargar);
        $("#bncAuthSearch").on("input", renderLista);
        $("#bncAuthBody").on("click", "tr", function () { seleccionar($(this).data("empresa"), $(this).data("id")); });
        $("#bncAuthDetail")
            .on("click", "#bncAuthPrint", function () {
                if (!state.seleccionado) return;
                window.open(urls.imprimir + "?empresa=" + encodeURIComponent(state.seleccionado.empresa) + "&idBorrador=" + encodeURIComponent(state.seleccionado.id), "_blank");
            })
            .on("click", "#bncAuthReject", function () { $("#bncRejectReason").val(""); $("#bncRejectModal").modal("show"); })
            .on("click", "#bncAuthApprove", function () { $("#bncApproveNote").val(""); $("#bncApproveModal").modal("show"); });

        $("#bncConfirmReject").on("click", function () {
            var motivo = $("#bncRejectReason").val().trim();
            if (!motivo) { avisar("warning", "Indique el motivo del rechazo."); return; }
            resolver("RECHAZADO", motivo, $(this));
        });
        $("#bncConfirmApprove").on("click", function () {
            resolver("AUTORIZADO", $("#bncApproveNote").val().trim(), $(this));
        });
    }

    $(function () { enlazar(); cargar(); });
})(window.jQuery);
