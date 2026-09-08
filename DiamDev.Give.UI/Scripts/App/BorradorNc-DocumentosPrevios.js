(function ($) {
    "use strict";

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : String(value)).html();
    }

    function numero(value) {
        var n = parseFloat(value);
        return isFinite(n) ? n : 0;
    }

    function dinero(value, moneda) {
        return (moneda ? escapeHtml(moneda) + " " : "") + numero(value).toLocaleString("es-GT", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function fecha(value) {
        return window.BorradorNcFechas.corta(value);
    }

    function claveDocumento(documento) {
        return String(documento.IdEmpresa || "").toUpperCase() + "|" +
            String(documento.IdBorrador || "");
    }

    function urlDetalle(baseUrl, borrador, previo, origen) {
        var separador = String(baseUrl || "").indexOf("?") >= 0 ? "&" : "?";
        return String(baseUrl || "") + separador +
            "empresa=" + encodeURIComponent(borrador.IdEmpresa || "") +
            "&idBorrador=" + encodeURIComponent(borrador.IdBorrador || "") +
            "&factura=" + encodeURIComponent(previo.Factura || "") +
            "&documento=" + encodeURIComponent(previo.Documento || "") +
            "&clase=" + encodeURIComponent(previo.Clase || "") +
            "&origen=" + encodeURIComponent(origen || "seguimiento");
    }

    function plantilla(id, opciones) {
        opciones = opciones || {};
        var titleId = id + "Title";
        return '<section class="bnc-prior-documents" id="' + escapeHtml(id) +
            '" aria-labelledby="' + escapeHtml(titleId) + '">' +
            '<div class="bnc-follow-section-head"><div class="bnc-follow-section-title">' +
            '<span class="bnc-follow-section-icon is-warning" aria-hidden="true"><i class="icon-warning-sign"></i></span>' +
            '<span><strong id="' + escapeHtml(titleId) + '">' +
            escapeHtml(opciones.titulo || "Notas de crédito y devoluciones previas en SAP") + '</strong>' +
            '<small>' + escapeHtml(opciones.subtitulo || "Antecedentes comerciales vinculados con las facturas del borrador.") +
            '</small></span></div><span class="bnc-invoice-overview" data-role="prior-count">' +
            '<i class="icon-refresh icon-spin" aria-hidden="true"></i> Cargando</span></div>' +
            '<div class="bnc-prior-list" data-role="prior-documents"><div class="bnc-loading">' +
            '<span class="bnc-spinner"></span>Consultando antecedentes en SAP...</div></div></section>';
    }

    function crear(opciones) {
        var id = opciones.id;
        var url = opciones.url;
        var detalleUrl = opciones.detalleUrl;
        var origen = opciones.origen || "seguimiento";
        var cache = {};
        var secuencia = 0;
        var actual = null;
        var namespace = ".bncPriorDocuments" + id;

        function $seccion() { return $("#" + id); }
        function $contador() { return $seccion().find('[data-role="prior-count"]'); }
        function $contenido() { return $seccion().find('[data-role="prior-documents"]'); }

        function vigente(documento, solicitud) {
            return solicitud === secuencia && actual && documento &&
                String(actual.IdEmpresa) === String(documento.IdEmpresa) &&
                String(actual.IdBorrador) === String(documento.IdBorrador);
        }

        function enlazar() {
            $seccion().off(namespace)
                .on("click" + namespace, '[data-action="retry-prior-documents"]', function () {
                    if (actual) cargar(actual, true);
                });
        }

        function renderCargando() {
            $contador().html('<i class="icon-refresh icon-spin" aria-hidden="true"></i> Cargando');
            $contenido().html('<div class="bnc-loading"><span class="bnc-spinner"></span>' +
                'Consultando antecedentes en SAP...</div>');
        }

        function renderError(mensaje) {
            $contador().html('<i class="icon-warning-sign" aria-hidden="true"></i> No disponible');
            $contenido().html('<div class="bnc-invoice-items-state is-error">' +
                '<span class="bnc-follow-section-icon" aria-hidden="true"><i class="icon-warning-sign"></i></span>' +
                '<div><strong>No fue posible consultar los antecedentes</strong><span>' + escapeHtml(mensaje) +
                '</span></div><button type="button" class="bnc-btn bnc-btn-ghost" data-action="retry-prior-documents">' +
                '<i class="icon-refresh"></i> Reintentar</button></div>');
        }

        function render(documento, previos) {
            var html = "";
            $.each(previos || [], function (_, previo) {
                var esDevolucion = String(previo.Clase || "").toUpperCase() === "DEVOLUCION";
                var clase = previo.ClaseTexto || (esDevolucion ? "Devolución" : "Nota de crédito");
                var icono = esDevolucion ? "icon-reply" : "icon-file-text";
                var urlCompleta = urlDetalle(detalleUrl, documento, previo, origen);
                var estado = previo.Cancelado
                    ? '<span class="bnc-prior-status is-cancelled">Cancelado</span>'
                    : '<span class="bnc-prior-status">' + escapeHtml(clase) + '</span>';

                html += '<article class="bnc-prior-item">' +
                    '<span class="bnc-prior-icon" aria-hidden="true"><i class="' + icono + '"></i></span>' +
                    '<div class="bnc-prior-copy"><div class="bnc-prior-title">' + estado +
                    '<strong>' + escapeHtml(clase) + ' ' + escapeHtml(previo.Documento) + '</strong></div>' +
                    '<div class="bnc-prior-meta"><span><i class="icon-link"></i> Factura ' + escapeHtml(previo.Factura) + '</span>' +
                    '<span><i class="icon-calendar"></i> ' + fecha(previo.Fecha) + '</span>' +
                    (previo.TiposOrigen ? '<span><i class="icon-tags"></i> ' + escapeHtml(previo.TiposOrigen) + '</span>' : '') +
                    '</div><p>' + escapeHtml(previo.Comentarios || "Sin comentarios") + '</p></div>' +
                    '<div class="bnc-prior-summary"><small>Total</small><strong>' + dinero(previo.Total, previo.Moneda) + '</strong></div>' +
                    '<a class="bnc-btn bnc-btn-primary bnc-prior-action" href="' + escapeHtml(urlCompleta) +
                    '" target="_blank" rel="noopener noreferrer" title="Abrir el detalle completo en una pestaña nueva" ' +
                    'aria-label="Ver detalle de ' + escapeHtml(clase) + ' ' + escapeHtml(previo.Documento) + '">' +
                    '<i class="icon-external-link" aria-hidden="true"></i><span>Ver detalle</span></a></article>';
            });

            $contador().html('<i class="icon-ok-circle" aria-hidden="true"></i> ' +
                (previos || []).length + ((previos || []).length === 1 ? " antecedente" : " antecedentes"));
            if (!html) {
                html = '<div class="bnc-inline-alert is-visible is-info"><i class="icon-check"></i>' +
                    '<span>No se encontraron notas de crédito ni devoluciones previas en SAP para estas facturas.</span></div>';
            }
            $contenido().html(html);
        }

        function cargar(documento, forzar) {
            actual = documento;
            enlazar();
            var clave = claveDocumento(documento);
            if (!forzar && Object.prototype.hasOwnProperty.call(cache, clave)) {
                render(documento, cache[clave]);
                return;
            }

            var solicitud = ++secuencia;
            renderCargando();
            $.ajax({
                url: url,
                type: "GET",
                dataType: "json",
                cache: false,
                data: { empresa: documento.IdEmpresa, idBorrador: documento.IdBorrador }
            }).done(function (respuesta) {
                if (!vigente(documento, solicitud)) return;
                if (!respuesta || !respuesta.ok) {
                    renderError(respuesta && respuesta.msg
                        ? respuesta.msg
                        : "No fue posible consultar los antecedentes en SAP.");
                    return;
                }
                cache[clave] = respuesta.data || [];
                render(documento, cache[clave]);
            }).fail(function (xhr) {
                if (!vigente(documento, solicitud)) return;
                var mensaje = xhr && xhr.responseJSON && xhr.responseJSON.msg
                    ? xhr.responseJSON.msg
                    : "No fue posible consultar los antecedentes en SAP.";
                renderError(mensaje);
            });
        }

        return {
            cargar: cargar,
            cancelar: function () { secuencia++; actual = null; },
            invalidar: function () { cache = {}; secuencia++; actual = null; }
        };
    }

    window.BorradorNcDocumentosPrevios = {
        crear: crear,
        plantilla: plantilla,
        urlDetalle: urlDetalle
    };
})(window.jQuery);
