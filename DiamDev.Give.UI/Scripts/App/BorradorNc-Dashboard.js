(function ($) {
    "use strict";
    var $app = $("#bncDashboard");
    if (!$app.length) return;

    var estado = { pagina: 1, orden: "FECHA", direccion: "DESC", total: 0, paginas: 1 };
    var seleccion = {};
    var opcionesCargadas = false;
    var temporizador;

    function filtro() {
        return {
            Empresa: $("#fEmpresa").val(), Estado: $("#fEstado").val(),
            CampoFecha: $("#fCampoFecha").val(), Desde: $("#fDesde").val(), Hasta: $("#fHasta").val(),
            Agente: $("#fAgente").val(), Creador: $("#fCreador").val(), ResueltoPor: $("#fResuelto").val(),
            Moneda: $("#fMoneda").val(), ConAdjuntos: $("#fAdjuntos").val(),
            ConAntecedentesSap: $("#fAntecedentes").val(), Texto: $("#fTexto").val(),
            Pagina: estado.pagina, TamanoPagina: $("#tamanoPagina").val(),
            Orden: estado.orden, Direccion: estado.direccion
        };
    }

    function fecha(valor, hora) {
        if (!valor) return "—";
        var m = /\/Date\((\d+)/.exec(valor);
        var d = m ? new Date(parseInt(m[1], 10)) : new Date(valor);
        if (isNaN(d.getTime())) return "—";
        var s = ("0" + d.getDate()).slice(-2) + "/" + ("0" + (d.getMonth() + 1)).slice(-2) + "/" + d.getFullYear();
        return hora ? s + " " + ("0" + d.getHours()).slice(-2) + ":" + ("0" + d.getMinutes()).slice(-2) : s;
    }

    function esc(v) { return $("<div>").text(v == null ? "" : v).html(); }
    function moneda(v) { return Number(v || 0).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function clave(x) { return x.IdEmpresa + "|" + x.IdBorrador; }

    function cargar() {
        $app.addClass("bncd-loading");
        $.ajax({ url: $app.data("url-consultar"), data: filtro(), cache: false })
            .done(function (r) {
                if (!r || !r.ok) { mostrarError(r && r.msg); return; }
                pintar(r.data);
            })
            .fail(function (xhr) { mostrarError(xhr.status === 403 ? "No tiene acceso al dashboard." : "No fue posible consultar los borradores."); })
            .always(function () { $app.removeClass("bncd-loading"); });
    }

    function mostrarError(mensaje) {
        $("#dashboardFilas").html('<tr><td colspan="13" class="empty">' + esc(mensaje || "Ocurrió un error.") + "</td></tr>");
    }

    function pintar(data) {
        data = data || {};
        var filas = data.Filas || [];
        estado.total = data.TotalFilas || 0;
        estado.paginas = Math.max(1, Math.ceil(estado.total / (data.TamanoPagina || 25)));
        if (!opcionesCargadas) {
            opciones($("#fEmpresa"), data.Empresas); opciones($("#fAgente"), data.Agentes);
            opciones($("#fCreador"), data.Creadores); opciones($("#fResuelto"), data.Resolutores);
            opciones($("#fMoneda"), data.Monedas); opcionesCargadas = true;
        }
        pintarResumen(data.Resumen || {});
        var html = "";
        $.each(filas, function (_, x) {
            var k = clave(x), url = $app.data("url-ver") + "?empresa=" + encodeURIComponent(x.IdEmpresa) + "&idBorrador=" + encodeURIComponent(x.IdBorrador);
            html += '<tr><td class="action"><a class="bncd-view" target="_blank" rel="noopener" title="Ver e imprimir borrador" href="' + url + '">&#128065;</a>' +
                '<button type="button" class="bncd-view audit" title="Ver auditoría" data-empresa="' + esc(x.IdEmpresa) + '" data-borrador="' + esc(x.IdBorrador) + '">&#128337;</button></td>' +
                '<td class="check"><input type="checkbox" class="fila-check" data-clave="' + esc(k) + '" ' + (seleccion[k] ? "checked" : "") + ' aria-label="Seleccionar ' + esc(x.IdBorrador) + '"></td>' +
                '<td><span class="bncd-main">' + esc(x.IdBorrador) + '</span><span class="bncd-sub">' + esc(x.IdUsr) + '</span></td>' +
                '<td>' + esc(x.IdEmpresa) + '</td><td>' + fecha(x.Registro || x.Fecha, false) + '</td>' +
                '<td><span class="bncd-main">' + esc(x.Nombre) + '</span><span class="bncd-sub">' + esc(x.IdCliente) + ' · ' + esc(x.Nit) + '</span></td>' +
                '<td>' + esc(x.Agente) + '</td><td><span class="bncd-state ' + esc(x.Estado) + '">' + esc(x.Estado) + '</span></td>' +
                '<td class="number"><span class="bncd-main">' + esc(x.Moneda) + ' ' + moneda(x.Total) + '</span></td>' +
                '<td>' + x.Facturas + (x.TieneAntecedentesSap ? '<span class="bncd-sub bncd-flag">Con antecedentes</span>' : "") + '</td>' +
                '<td>' + x.Adjuntos + '</td><td>' + esc(x.IdUsr) + '</td><td><span class="bncd-main">' + esc(x.ResueltoPor || "Pendiente") + '</span><span class="bncd-sub">' + fecha(x.FechaResolucion, true) + '</span></td></tr>';
        });
        $("#dashboardFilas").html(html || '<tr><td colspan="13" class="empty">No hay borradores para los filtros elegidos.</td></tr>');
        var inicio = estado.total ? ((estado.pagina - 1) * (data.TamanoPagina || 25)) + 1 : 0;
        var fin = Math.min(estado.total, estado.pagina * (data.TamanoPagina || 25));
        $("#totalFilas").text(estado.total); $("#rangoPagina").text(inicio + "–" + fin + " de " + estado.total);
        $("#paginaActual").text(estado.pagina + " / " + estado.paginas);
        $("#paginaAnterior").prop("disabled", estado.pagina <= 1);
        $("#paginaSiguiente").prop("disabled", estado.pagina >= estado.paginas);
        refrescarSeleccion();
    }

    function pintarResumen(r) {
        $("#kpiTotal").text(r.Total || 0); $("#kpiPendientes").text(r.Pendientes || 0);
        $("#kpiVencidos").text((r.PendientesVencidos || 0) + " vencidos");
        $("#kpiAutorizados").text(r.Autorizados || 0); $("#kpiRechazados").text(r.Rechazados || 0);
        $("#kpiAnulados").text(r.Anulados || 0);
        $("#kpiPromedio").text(r.HorasPromedioResolucion == null ? "—" : Number(r.HorasPromedioResolucion).toFixed(1) + " h");
        var html = ""; $.each(r.TotalesPorMoneda || [], function (_, x) { html += "<span>" + esc(x.Moneda) + " " + moneda(x.Total) + "</span>"; });
        $("#totalesMoneda").html(html || "<span>Sin totales monetarios</span>");
    }

    function opciones($select, valores) {
        var actual = $select.val(), html = $select.find("option:first")[0].outerHTML;
        $.each(valores || [], function (_, x) { html += '<option value="' + esc(x) + '">' + esc(x) + "</option>"; });
        $select.html(html).val(actual);
    }

    function refrescarSeleccion() {
        var cantidad = Object.keys(seleccion).length;
        $("#seleccionados").text(cantidad + " seleccionados");
        $("#btnImprimir").prop("disabled", cantidad === 0);
        var checks = $(".fila-check"), marcados = checks.filter(":checked").length;
        $("#seleccionarPagina").prop("checked", checks.length > 0 && checks.length === marcados);
    }

    function limpiarSeleccion() { seleccion = {}; refrescarSeleccion(); }

    function agregarCampos($form, datos) {
        $form.find("input[data-dinamico]").remove();
        $.each(datos, function (nombre, valor) {
            if (valor === "" || valor == null) return;
            $("<input>", { type: "hidden", name: nombre, value: valor, "data-dinamico": "1" }).appendTo($form);
        });
    }

    $(document).on("change", ".fila-check", function () { var k = $(this).data("clave"); if (this.checked) seleccion[k] = true; else delete seleccion[k]; refrescarSeleccion(); });
    $(document).on("click", ".bncd-view.audit", function () {
        var empresa = $(this).data("empresa"), borrador = $(this).data("borrador");
        $("#subtituloBitacora").text(empresa + " · " + borrador);
        $("#listaBitacora").html('<div class="empty">Cargando historial…</div>');
        $("#modalBitacora").prop("hidden", false);
        $.getJSON($app.data("url-bitacora"), { empresa: empresa, idBorrador: borrador })
            .done(function (r) {
                if (!r || !r.ok) { $("#listaBitacora").html('<div class="empty">' + esc(r && r.msg || "No fue posible consultar el historial.") + '</div>'); return; }
                var html = "";
                $.each(r.data || [], function (_, x) {
                    var cambio = x.EstadoAnterior || x.EstadoNuevo ? " · " + esc(x.EstadoAnterior || "—") + " → " + esc(x.EstadoNuevo || "—") : "";
                    html += '<article class="bncd-event"><strong>' + esc(x.Evento) + cambio + '</strong><small>' + fecha(x.Registro, true) + ' · ' + esc(x.Usuario) + (x.Ip ? " · " + esc(x.Ip) : "") + '</small>' + (x.Detalle ? '<p>' + esc(x.Detalle) + '</p>' : "") + '</article>';
                });
                $("#listaBitacora").html(html || '<div class="empty">Este borrador no tiene eventos registrados todavía.</div>');
            });
    });
    $("#cerrarBitacora").on("click", function () { $("#modalBitacora").prop("hidden", true); });
    $("#modalBitacora").on("click", function (e) { if (e.target === this) $(this).prop("hidden", true); });
    $("#seleccionarPagina").on("change", function () { var activo = this.checked; $(".fila-check").each(function () { this.checked = activo; var k = $(this).data("clave"); if (activo) seleccion[k] = true; else delete seleccion[k]; }); refrescarSeleccion(); });
    $(".bncd-filter-grid select,.bncd-filter-grid input[type=date]").on("change", function () { estado.pagina = 1; limpiarSeleccion(); cargar(); });
    $("#fTexto").on("input", function () { clearTimeout(temporizador); temporizador = setTimeout(function () { estado.pagina = 1; limpiarSeleccion(); cargar(); }, 350); });
    $(".bncd-kpi[data-state]").on("click", function () { $("#fEstado").val($(this).data("state")); $(".bncd-kpi").removeClass("active"); $(this).addClass("active"); estado.pagina = 1; limpiarSeleccion(); cargar(); });
    $("#fEstado").on("change", function () { $(".bncd-kpi").removeClass("active").filter('[data-state="' + $(this).val() + '"]').addClass("active"); });
    $("#btnLimpiar").on("click", function () { $(".bncd-filter-grid input").val(""); $(".bncd-filter-grid select").prop("selectedIndex", 0); $(".bncd-kpi").removeClass("active").first().addClass("active"); estado.pagina = 1; limpiarSeleccion(); cargar(); });
    $("#tamanoPagina").on("change", function () { estado.pagina = 1; cargar(); });
    $("#paginaAnterior").on("click", function () { if (estado.pagina > 1) { estado.pagina--; cargar(); } });
    $("#paginaSiguiente").on("click", function () { if (estado.pagina < estado.paginas) { estado.pagina++; cargar(); } });
    $("th[data-sort]").on("click", function () { var o = $(this).data("sort"); estado.direccion = estado.orden === o && estado.direccion === "DESC" ? "ASC" : "DESC"; estado.orden = o; cargar(); });
    $("#btnExportar").on("click", function () {
        var $f = $("#formExportar"); agregarCampos($f, filtro());
        $.each(Object.keys(seleccion), function (_, k) { $("<input>", { type: "hidden", name: "claves", value: k, "data-dinamico": "1" }).appendTo($f); });
        $f.trigger("submit");
    });
    $("#btnImprimir").on("click", function () {
        var claves = Object.keys(seleccion), maximo = Number($app.data("max-imprimir"));
        if (claves.length > maximo) { window.alert("Seleccione como máximo " + maximo + " borradores por lote."); return; }
        var $f = $("#formImprimir"); $f.find("input[data-dinamico]").remove();
        $.each(claves, function (_, k) { $("<input>", { type: "hidden", name: "claves", value: k, "data-dinamico": "1" }).appendTo($f); });
        $f.trigger("submit");
    });
    cargar();
})(window.jQuery);
