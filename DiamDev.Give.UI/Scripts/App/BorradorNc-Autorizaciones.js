(function ($) {
    "use strict";

    var $root = $("#bncAuthApp");
    if (!$root.length) return;

    var urls = {
        listar: $root.data("url-listar"),
        detalle: $root.data("url-detalle"),
        notas: $root.data("url-notas"),
        resolver: $root.data("url-resolver"),
        imprimir: $root.data("url-imprimir")
    };
    var state = { pendientes: [], seleccionado: null, documento: null };

    function token() { return $root.find('input[name="__RequestVerificationToken"]').val(); }
    function escapeHtml(value) { return $("<div>").text(value == null ? "" : String(value)).html(); }
    function numero(value) { var n = parseFloat(value); return isFinite(n) ? n : 0; }
    function dinero(value, moneda) {
        return (moneda ? escapeHtml(moneda) + " " : "") + numero(value).toLocaleString("es-GT", {
            minimumFractionDigits: 2, maximumFractionDigits: 2
        });
    }
    function fecha(value) {
        if (!value) return "—";
        var p = String(value).substring(0, 10).split("-");
        return p.length === 3 ? p[2] + "/" + p[1] + "/" + p[0] : value;
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
        state.seleccionado = null;
        state.documento = null;
        $("#bncAuthDetail").html('<div class="bnc-empty"><i class="icon-hand-left"></i><strong>Seleccione una solicitud</strong><span>Revise cada línea y sus NC previas antes de autorizar.</span></div>');
        $("#bncAuthBody").html('<tr><td colspan="5"><div class="bnc-loading"><span class="bnc-spinner"></span>Consultando pendientes...</div></td></tr>');
        $("#bncAuthEmpty").hide();
        get(urls.listar, { empresa: $("#bncAuthEmpresa").val() || "" }).done(function (r) {
            if (!r || !r.ok) {
                state.pendientes = [];
                avisar("error", r && r.msg ? r.msg : "No se pudo abrir la bandeja.");
            } else state.pendientes = r.data || [];
            renderLista();
        }).fail(function (xhr) {
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
        var filas = filtrados(), html = "", total = 0, conNc = 0;
        $.each(state.pendientes, function (_, x) { total += numero(x.Total); if (x.TieneNcPrevia) conNc++; });
        $.each(filas, function (_, x) {
            html += '<tr data-empresa="' + escapeHtml(x.IdEmpresa) + '" data-id="' + escapeHtml(x.IdBorrador) + '">' +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(x.IdBorrador) + '</strong><small>' + escapeHtml(x.IdEmpresa) + " · " + escapeHtml(x.IdUsr) + "</small></td>" +
                "<td>" + fecha(x.Fecha) + "</td>" +
                '<td class="bnc-main-cell"><strong>' + escapeHtml(x.Nombre) + '</strong><small>' + escapeHtml(x.IdCliente) + (x.TieneNcPrevia ? ' · <span style="color:#a16207">Con NC previa</span>' : "") + "</small></td>" +
                "<td>" + escapeHtml(x.Agente) + "</td>" +
                '<td class="bnc-money">' + dinero(x.Total, x.Moneda) + "</td></tr>";
        });
        $("#bncAuthBody").html(html);
        $("#bncAuthEmpty").toggle(!filas.length);
        $("#bncAuthResultCount").text(filas.length + (filas.length === 1 ? " resultado" : " resultados"));
        $("#bncAuthKpiCount").text(state.pendientes.length);
        $("#bncAuthKpiMonto").text(dinero(total));
        $("#bncAuthKpiNc").text(conNc);
    }

    function seleccionar(empresa, id) {
        state.seleccionado = { empresa: empresa, id: id };
        $("#bncAuthBody tr").removeClass("is-selected").filter(function () {
            return $(this).data("empresa") === empresa && String($(this).data("id")) === String(id);
        }).addClass("is-selected");
        $("#bncAuthDetail").html('<div class="bnc-loading"><span class="bnc-spinner"></span>Cargando solicitud y antecedentes...</div>');
        get(urls.detalle, { empresa: empresa, idBorrador: id }).done(function (r) {
            if (!r || !r.ok) { avisar("error", r && r.msg ? r.msg : "No se pudo cargar el detalle."); return; }
            state.documento = r.data;
            renderDetalle(r.data);
            cargarNotas(r.data);
        }).fail(function (xhr) { avisar("error", mensajeAjax(xhr)); });
    }

    function renderDetalle(x) {
        var lineas = "";
        $.each(x.Detalles || [], function (i, d) {
            var alertas = [];
            if (numero(d.Pagado) >= numero(d.TotalFactura) - .005) alertas.push('<span class="bnc-paid-flag">Pagada</span>');
            if (numero(d.NcPreviaSap) > 0) alertas.push('<span class="bnc-nc-flag">NC ' + dinero(d.NcPreviaSap) + "</span>");
            lineas += '<tr><td class="bnc-main-cell"><strong>' + escapeHtml(d.Documento) + '</strong><small>' + escapeHtml(d.Concepto) + "</small></td>" +
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
            '<div class="bnc-table-wrap" style="border-width:1px 0;border-radius:0"><table class="table bnc-table" style="min-width:700px"><thead><tr><th>Documento</th><th>Fecha</th><th class="text-right">Total factura</th><th class="text-right">Solicitado</th><th>Alertas</th></tr></thead><tbody>' + lineas + "</tbody></table></div>" +
            '<div class="bnc-card-head" style="min-height:46px"><div class="bnc-card-title"><i class="icon-warning-sign"></i><div><h3>Notas de crédito previas en SAP</h3><small>Antecedentes por cada documento del borrador.</small></div></div></div>' +
            '<div class="bnc-nc-list" id="bncAuthNotes"><div class="bnc-loading"><span class="bnc-spinner"></span>Consultando SAP...</div></div>' +
            '<div class="bnc-decision-bar"><button class="bnc-btn bnc-btn-ghost" type="button" id="bncAuthPrint"><i class="icon-print"></i> Imprimir</button>' +
            '<button class="bnc-btn bnc-btn-danger" type="button" id="bncAuthReject"><i class="icon-remove"></i> Rechazar</button>' +
            '<button class="bnc-btn bnc-btn-success" type="button" id="bncAuthApprove"><i class="icon-check"></i> Autorizar</button></div>'
        );
    }

    function cargarNotas(x) {
        var detalles = x.Detalles || [];
        if (!detalles.length) { $("#bncAuthNotes").html('<div class="bnc-empty"><span>Sin documentos.</span></div>'); return; }
        var notas = [], terminadas = 0;
        $.each(detalles, function (_, d) {
            get(urls.notas, { empresa: x.IdEmpresa, documento: d.Documento }).done(function (r) {
                if (r && r.ok) notas = notas.concat(r.data || []);
            }).always(function () {
                terminadas++;
                if (terminadas === detalles.length) renderNotas(notas);
            });
        });
    }

    function renderNotas(notas) {
        if (!notas.length) {
            $("#bncAuthNotes").html('<div class="bnc-inline-alert is-visible is-info"><i class="icon-check"></i><span>No se encontraron notas de crédito previas en SAP para estos documentos.</span></div>');
            return;
        }
        var html = "";
        $.each(notas, function (_, n) {
            html += '<div class="bnc-nc-item"><strong>Factura ' + escapeHtml(n.Factura) + " · NC " + escapeHtml(n.Nota) + " · " + dinero(n.Total, n.Moneda) +
                "</strong><span>" + fecha(n.Fecha) + " · " + escapeHtml(n.Tipo || "NC") + " · " + escapeHtml(n.Comentarios || n.Origen || "Sin comentario") + "</span></div>";
        });
        $("#bncAuthNotes").html(html);
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
