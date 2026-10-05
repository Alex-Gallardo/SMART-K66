(function ($) {
    "use strict";

    function esc(value) { return $("<div>").text(value == null ? "" : String(value)).html(); }
    function clave(x) { return String(x.IdEmpresa || "").toUpperCase() + "|" + String(x.IdBorrador || "").toUpperCase(); }
    function hora(value) {
        var d = new Date(value);
        return isNaN(d.getTime()) ? "—" : d.toLocaleString("es-GT");
    }
    function plantilla(x, completo) {
        return '<div class="bnc-sap-indicator" data-sap-clave="' + esc(clave(x)) +
            '" data-sap-completo="' + (completo ? "true" : "false") + '">' +
            '<span class="bnc-sap-badge is-unknown">NC en SAP: por consultar</span></div>';
    }
    function crear(opciones) {
        var $root = opciones.root;
        var intervalo = Math.max(30, Math.min(600, Number($root.attr("data-sap-intervalo")) || 60)) * 1000;
        var cache = {}, borradores = {}, generacion = 0, solicitud = null, ocupado = false, enConsulta = [];
        var origen = opciones.origen;
        function visible() { return !document.hidden && (!opciones.activo || opciones.activo()); }
        function render() {
            $root.find("[data-sap-clave]").each(function () {
                var $el = $(this), k = $el.attr("data-sap-clave"), estado = cache[k] || {};
                var data = estado.data, completo = $el.attr("data-sap-completo") === "true";
                var texto = data ? (data.NcVigentes ? data.NcVigentes + " NC vigente(s) en SAP" : "Sin NC vigente en SAP") :
                    (estado.error ? "NC en SAP: no disponible" : "NC en SAP: por consultar");
                if (data && data.NcCanceladas) texto += " · " + data.NcCanceladas + " cancelada(s)";
                var clase = estado.error ? "is-stale" : data ? (data.NcVigentes ? "is-warning" : "is-current") : "is-unknown";
                var html = '<button type="button" class="bnc-sap-badge ' + clase + '" data-sap-refresh="' + esc(k) +
                    '" title="Actualizar NC de las facturas en SAP" aria-label="Actualizar NC de ' + esc(k) + '">' +
                    '<i class="icon-refresh" aria-hidden="true"></i> ' + esc(texto) + '</button>';
                if (data) html += '<small class="bnc-sap-time">Consulta SAP: ' + esc(hora(data.ActualizadoEn)) + '</small>';
                if (estado.error) html += '<small class="bnc-sap-error" role="status">' +
                    (data ? "No se pudo actualizar. Se conserva la última consulta." : "No se pudo consultar SAP. Pulse para reintentar.") + '</small>';
                if (completo) {
                    // html += '<small class="bnc-sap-help">Las NC corresponden a las facturas; no acreditan una aplicación a este borrador.</small>';
                    if (data && (data.Documentos || []).length) {
                        html += '<details class="bnc-sap-documents"><summary>Ver NC de las facturas</summary><ul>';
                        $.each(data.Documentos, function (_, nc) {
                            var x = borradores[k];
                            var url = $root.attr("data-url-documento-previo") + "?empresa=" + encodeURIComponent(x.IdEmpresa) +
                                "&idBorrador=" + encodeURIComponent(x.IdBorrador) + "&factura=" + encodeURIComponent(nc.Factura) +
                                "&documento=" + encodeURIComponent(nc.Documento) + "&clase=NOTA_CREDITO&origen=" + encodeURIComponent(origen);
                            html += '<li><a href="' + esc(url) + '" target="_blank" rel="noopener noreferrer">NC ' + esc(nc.Documento) +
                                ' · Factura ' + esc(nc.Factura) + '</a> <span>' + (nc.Cancelado ? "Cancelada" : "Vigente") + '</span></li>';
                        });
                        html += '</ul></details>';
                    }
                }
                // Preserve an opened disclosure across refreshes, without touching the draft selection.
                var abierto = $el.find("details").prop("open");
                var focoBoton = $el.find("[data-sap-refresh]").is(document.activeElement);
                var focoResumen = $el.find("summary").is(document.activeElement);
                $el.html(html);
                if (abierto) $el.find("details").prop("open", true);
                if (focoBoton) $el.find("[data-sap-refresh]").trigger("focus");
                if (focoResumen) $el.find("summary").trigger("focus");
            });
            if (opciones.cambio) opciones.cambio(cache);
        }
        function actualizar(forzar, solo) {
            if (!visible() || ocupado) return;
            var ahora = Date.now();
            var claves = Object.keys(borradores).filter(function (k) {
                return (!solo || solo === k) && (forzar || !cache[k] || (!cache[k].data && !cache[k].error) || ahora - cache[k].intento >= intervalo);
            });
            if (!claves.length) { render(); return; }
            ocupado = true;
            var version = generacion;
            function siguiente() {
                if (version !== generacion) return;
                enConsulta = [];
                if (!visible()) { ocupado = false; solicitud = null; return; }
                var lote = claves.splice(0, 100);
                if (!lote.length) { ocupado = false; solicitud = null; return; }
                enConsulta = lote;
                lote.forEach(function (k) { cache[k] = cache[k] || {}; cache[k].intento = Date.now(); });
                var peticion = $.ajax({
                    url: $root.attr("data-url-sap-indicadores"), type: "POST", dataType: "json", traditional: true, timeout: 180000,
                    data: { claves: lote, origen: origen,
                        __RequestVerificationToken: $root.find('input[name="__RequestVerificationToken"]').val() }
                });
                solicitud = peticion;
                function error() {
                    if (version !== generacion) return;
                    lote.forEach(function (k) { cache[k].error = true; });
                    render();
                }
                peticion.done(function (r) {
                    if (version !== generacion) return;
                    if (!r || !r.ok || !Array.isArray(r.data)) { error(); return; }
                    // Missing results are failures, never an implicit "no NC".
                    lote.forEach(function (k) { cache[k].error = true; });
                    $.each(r.data, function (_, x) {
                        var k = clave(x);
                        if (lote.indexOf(k) < 0) return;
                        if (x.Disponible === true) { cache[k].data = x; cache[k].error = false; }
                    });
                    render();
                }).fail(error).always(function () {
                    if (version === generacion) siguiente();
                });
            }
            siguiente();
        }
        function mostrar(filas) {
            var nuevos = {};
            (filas || []).forEach(function (x) { nuevos[clave(x)] = x; });
            if (Object.keys(nuevos).sort().join("\n") !== Object.keys(borradores).sort().join("\n")) {
                generacion++;
                ocupado = false;
                enConsulta.forEach(function (k) { if (cache[k]) cache[k].intento = 0; });
                enConsulta = [];
                if (solicitud) solicitud.abort();
                solicitud = null;
            }
            borradores = nuevos;
            render();
            actualizar(false);
        }
        $root.on("click", "[data-sap-refresh]", function (e) {
            e.preventDefault(); e.stopPropagation();
            actualizar(true, $(this).attr("data-sap-refresh"));
        });
        $root.on("click", ".bnc-sap-documents", function (e) { e.stopPropagation(); });
        $(document).on("visibilitychange", function () { if (visible()) actualizar(false); });
        $root.on("shown.bs.tab", function () { actualizar(false); });
        window.setInterval(function () { actualizar(false); }, intervalo);
        return { mostrar: mostrar, actualizar: actualizar, obtener: function (x) { return cache[clave(x)]; } };
    }
    window.BorradorNcSapIndicadores = { crear: crear, plantilla: plantilla };
})(window.jQuery);
