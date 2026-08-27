(function ($) {
    "use strict";

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
        return (moneda ? escapeHtml(moneda) + " " : "") + numero(value).toLocaleString("es-GT", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function decimal(value, maximoDecimales) {
        return numero(value).toLocaleString("es-GT", {
            minimumFractionDigits: 0,
            maximumFractionDigits: maximoDecimales == null ? 4 : maximoDecimales
        });
    }

    function fecha(value) {
        return window.BorradorNcFechas.corta(value);
    }

    function mensajeAjax(xhr) {
        return xhr && xhr.responseJSON && xhr.responseJSON.msg
            ? xhr.responseJSON.msg
            : "No fue posible consultar los productos de las facturas.";
    }

    function claveDocumento(documento) {
        return String(documento.IdEmpresa || "").toUpperCase() + "|" +
            String(documento.IdBorrador || "");
    }

    function plantilla(id, opciones) {
        opciones = opciones || {};
        var titleId = id + "Title";
        return '<section class="bnc-follow-invoices" id="' + escapeHtml(id) +
            '" aria-labelledby="' + escapeHtml(titleId) + '">' +
            '<div class="bnc-follow-section-head"><div class="bnc-follow-section-title">' +
            '<span class="bnc-follow-section-icon" aria-hidden="true"><i class="icon-list"></i></span>' +
            '<span><strong id="' + escapeHtml(titleId) + '">' +
            escapeHtml(opciones.titulo || "Productos y servicios de las facturas") + '</strong>' +
            '<small>' + escapeHtml(opciones.subtitulo || "Renglones originales que conforman cada factura en SAP.") +
            '</small></span></div><span class="bnc-invoice-overview" data-role="invoice-product-count">' +
            '<i class="icon-refresh icon-spin" aria-hidden="true"></i> Cargando</span></div>' +
            '<div data-role="invoice-products"><div class="bnc-loading bnc-invoice-items-loading">' +
            '<span class="bnc-spinner"></span>Consultando productos y servicios en SAP...</div></div></section>';
    }

    function crear(opciones) {
        var id = opciones.id;
        var url = opciones.url;
        var cache = {};
        var secuencia = 0;
        var actual = null;
        var namespace = ".bncInvoiceProducts" + id;

        function $seccion() { return $("#" + id); }
        function $contador() { return $seccion().find('[data-role="invoice-product-count"]'); }
        function $contenido() { return $seccion().find('[data-role="invoice-products"]'); }

        function vigente(documento, solicitud) {
            return solicitud === secuencia && actual && documento &&
                String(actual.IdEmpresa) === String(documento.IdEmpresa) &&
                String(actual.IdBorrador) === String(documento.IdBorrador);
        }

        function enlazar() {
            var $host = $seccion();
            $host.off(namespace)
                .on("click" + namespace, '[data-action="toggle-invoice-products"]', function () {
                    var $factura = $(this).closest(".bnc-sap-invoice");
                    var abrir = !$factura.hasClass("is-open");
                    $factura.toggleClass("is-open", abrir);
                    $(this).attr("aria-expanded", abrir ? "true" : "false");
                    $factura.find(".bnc-sap-invoice-body").first()
                        .prop("hidden", !abrir).attr("aria-hidden", abrir ? "false" : "true");
                    $(this).find('[data-role="invoice-toggle-label"]')
                        .text(abrir ? "Ocultar detalle" : "Ver detalle");
                })
                .on("click" + namespace, '[data-action="retry-invoice-products"]', function () {
                    if (actual) cargar(actual, true);
                });
        }

        function renderCargando() {
            $contador().html('<i class="icon-refresh icon-spin" aria-hidden="true"></i> Cargando');
            $contenido().html('<div class="bnc-loading bnc-invoice-items-loading">' +
                '<span class="bnc-spinner"></span>Consultando productos y servicios en SAP...</div>');
        }

        function renderError(mensaje) {
            $contador().html('<i class="icon-warning-sign" aria-hidden="true"></i> No disponible');
            $contenido().html(
                '<div class="bnc-invoice-items-state is-error"><span class="bnc-follow-section-icon" aria-hidden="true"><i class="icon-warning-sign"></i></span>' +
                '<div><strong>No fue posible consultar los productos</strong><span>' + escapeHtml(mensaje) +
                '</span></div><button type="button" class="bnc-btn bnc-btn-ghost" data-action="retry-invoice-products">' +
                '<i class="icon-refresh"></i> Reintentar</button></div>');
        }

        function renderProducto(producto, indice) {
            var sku = producto.Sku || "SERVICIO";
            var tipo = producto.EsServicio ? "Servicio" : "SKU";
            var unidad = producto.UnidadMedida ? escapeHtml(producto.UnidadMedida) : "Sin unidad";
            var descuento = numero(producto.DescuentoPorcentaje);
            var impuestoMeta = decimal(producto.ImpuestoPorcentaje, 2) + "%" +
                (producto.CodigoImpuesto ? " · " + escapeHtml(producto.CodigoImpuesto) : "");
            var bodega = producto.Bodega
                ? '<span class="bnc-sap-item-tag"><i class="icon-archive" aria-hidden="true"></i> Bodega ' + escapeHtml(producto.Bodega) + '</span>'
                : '<span class="bnc-sap-item-tag is-muted"><i class="icon-archive" aria-hidden="true"></i> Sin bodega</span>';

            return '<article class="bnc-sap-item">' +
                '<div class="bnc-sap-item-head"><div class="bnc-sap-item-identity">' +
                '<span class="bnc-sap-item-number"><small>Línea</small>' + decimal((producto.NumeroLinea == null ? indice : producto.NumeroLinea) + 1, 0) + '</span>' +
                '<span class="bnc-sap-item-copy"><span class="bnc-sap-item-sku"><span>' + escapeHtml(tipo) + '</span><b>' + escapeHtml(sku) + '</b></span>' +
                '<strong>' + escapeHtml(producto.Descripcion || "Sin descripción") + '</strong></span></div>' +
                '<div class="bnc-sap-item-tags">' + bodega + '</div></div>' +
                '<dl class="bnc-sap-item-metrics"><div><dt>Cantidad</dt><dd>' + decimal(producto.Cantidad, 4) + '<small>' + unidad + '</small></dd></div>' +
                '<div><dt>Precio unitario</dt><dd>' + dinero(producto.PrecioUnitario, producto.Moneda) + '</dd></div>' +
                '<div' + (descuento ? '' : ' class="is-muted"') + '><dt>Descuento</dt><dd>' + (descuento ? decimal(descuento, 2) + '%' : 'Sin descuento') + '</dd></div>' +
                '<div><dt>Subtotal</dt><dd>' + dinero(producto.Subtotal, producto.Moneda) + '</dd></div>' +
                '<div><dt>Impuesto <small>' + impuestoMeta + '</small></dt><dd>' + dinero(producto.Impuesto, producto.Moneda) + '</dd></div>' +
                '<div class="is-total"><dt>Total de línea</dt><dd>' + dinero(producto.Total, producto.Moneda) + '</dd></div></dl></article>';
        }

        function renderFactura(factura, indice) {
            var productos = factura.Productos || [];
            var idContenido = id + "InvoiceContent" + indice;
            var fel = $.trim((factura.SerieFel || "") + " " + (factura.NumeroFel || ""));
            var metadatos = '<span><i class="icon-calendar" aria-hidden="true"></i> ' + fecha(factura.FechaDoc) + '</span>';
            if (fel) metadatos += '<span><i class="icon-file-text" aria-hidden="true"></i> FEL ' + escapeHtml(fel) + '</span>';
            if (factura.Concepto) metadatos += '<span><i class="icon-tag" aria-hidden="true"></i> ' + escapeHtml(factura.Concepto) + '</span>';

            var items = "";
            $.each(productos, function (i, producto) { items += renderProducto(producto, i); });
            if (!items) {
                items = '<div class="bnc-sap-invoice-empty"><i class="icon-info-sign"></i>' +
                    '<span>SAP no devolvió productos o servicios para esta factura.</span></div>';
            }

            var abierta = indice === 0;
            return '<article class="bnc-sap-invoice' + (abierta ? " is-open" : "") + '">' +
                '<button type="button" class="bnc-sap-invoice-toggle" data-action="toggle-invoice-products" ' +
                'aria-expanded="' + (abierta ? "true" : "false") + '" aria-controls="' + escapeHtml(idContenido) + '">' +
                '<span class="bnc-sap-invoice-heading"><span class="bnc-sap-invoice-icon" aria-hidden="true"><i class="icon-file-text"></i></span>' +
                '<span class="bnc-sap-invoice-copy"><small class="bnc-sap-invoice-eyebrow">Factura</small><strong>' + escapeHtml(factura.Documento) + '</strong>' +
                '<span class="bnc-sap-invoice-meta">' + metadatos + '</span></span></span>' +
                '<span class="bnc-sap-invoice-summary"><span class="bnc-sap-invoice-amount"><small>Monto solicitado</small><strong>' + dinero(factura.ImporteSolicitado, factura.Moneda) + '</strong></span>' +
                '<span class="bnc-sap-invoice-amount"><small>Total de factura</small><strong>' + dinero(factura.TotalFactura, factura.Moneda) + '</strong></span>' +
                '<span class="bnc-sap-invoice-count"><i class="icon-list" aria-hidden="true"></i> ' + productos.length + (productos.length === 1 ? " línea" : " líneas") + '</span>' +
                '<span class="bnc-sap-invoice-action"><span data-role="invoice-toggle-label">' + (abierta ? "Ocultar detalle" : "Ver detalle") + '</span>' +
                '<i class="icon-chevron-down bnc-sap-invoice-chevron" aria-hidden="true"></i></span></span></button>' +
                '<div class="bnc-sap-invoice-body" id="' + escapeHtml(idContenido) + '" role="region" ' +
                'aria-label="Productos de la factura ' + escapeHtml(factura.Documento) + '" aria-hidden="' + (abierta ? "false" : "true") + '"' +
                (abierta ? "" : " hidden") + '><div class="bnc-sap-item-list">' + items + '</div></div></article>';
        }

        function render(facturas) {
            var html = "", totalProductos = 0;
            $.each(facturas || [], function (indice, factura) {
                totalProductos += (factura.Productos || []).length;
                html += renderFactura(factura, indice);
            });

            $contador().html('<i class="icon-ok-circle" aria-hidden="true"></i> ' +
                (facturas || []).length + ((facturas || []).length === 1 ? " factura" : " facturas") + " · " +
                totalProductos + (totalProductos === 1 ? " línea facturada" : " líneas facturadas"));

            if (!html) {
                html = '<div class="bnc-invoice-items-state"><span class="bnc-follow-section-icon" aria-hidden="true"><i class="icon-info-sign"></i></span>' +
                    '<div><strong>Sin facturas asociadas</strong><span>Este borrador no contiene facturas para consultar en SAP.</span></div></div>';
            }
            $contenido().html(html);
        }

        function cargar(documento, forzar) {
            actual = documento;
            enlazar();
            var clave = claveDocumento(documento);
            if (!forzar && Object.prototype.hasOwnProperty.call(cache, clave)) {
                render(cache[clave]);
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
                        : "No fue posible consultar los productos de las facturas.");
                    return;
                }
                cache[clave] = respuesta.data || [];
                render(cache[clave]);
            }).fail(function (xhr) {
                if (vigente(documento, solicitud)) renderError(mensajeAjax(xhr));
            });
        }

        return {
            cargar: cargar,
            cancelar: function () { secuencia++; actual = null; },
            invalidar: function () { cache = {}; secuencia++; actual = null; }
        };
    }

    window.BorradorNcFacturasDetalle = {
        crear: crear,
        plantilla: plantilla
    };
})(window.jQuery);
