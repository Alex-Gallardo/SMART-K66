(function ($) {
    "use strict";

    var $app = $("#bncDashboard");
    if (!$app.length) return;

    var estado = { pagina: 1, orden: "FECHA", direccion: "DESC", total: 0, paginas: 1 };
    var seleccion = {};
    var opcionesCargadas = false;
    var temporizador;
    var solicitudActual = null;
    var numeroSolicitud = 0;
    var ultimoFocoModal = null;

    function filtro() {
        return {
            Empresa: $("#fEmpresa").val(),
            Estado: $("#fEstado").val(),
            CampoFecha: $("#fCampoFecha").val(),
            Desde: $("#fDesde").val(),
            Hasta: $("#fHasta").val(),
            Agente: $("#fAgente").val(),
            Creador: $("#fCreador").val(),
            ResueltoPor: $("#fResuelto").val(),
            Moneda: $("#fMoneda").val(),
            ConAdjuntos: $("#fAdjuntos").val(),
            ConAntecedentesSap: $("#fAntecedentes").val(),
            Texto: $("#fTexto").val(),
            Pagina: estado.pagina,
            TamanoPagina: $("#tamanoPagina").val(),
            Orden: estado.orden,
            Direccion: estado.direccion
        };
    }

    function fecha(valor, hora) {
        if (!valor) return "—";
        var m = /\/Date\((-?\d+)/.exec(valor);
        var d = m ? new Date(parseInt(m[1], 10)) : new Date(valor);
        if (isNaN(d.getTime())) return "—";
        var resultado = ("0" + d.getDate()).slice(-2) + "/" +
            ("0" + (d.getMonth() + 1)).slice(-2) + "/" + d.getFullYear();
        return hora ? resultado + " " + ("0" + d.getHours()).slice(-2) + ":" +
            ("0" + d.getMinutes()).slice(-2) : resultado;
    }

    function esc(valor) { return $("<div>").text(valor == null ? "" : valor).html(); }
    function moneda(valor) { return Number(valor || 0).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function clave(item) { return item.IdEmpresa + "|" + item.IdBorrador; }
    function anunciar(mensaje) { $("#bncdLive").text(mensaje || ""); }

    function establecerCarga(cargando) {
        $app.toggleClass("is-loading", cargando).attr("aria-busy", cargando ? "true" : "false");
        $("#btnExportar").prop("disabled", cargando || estado.total === 0);
        if (cargando) $("#btnImprimir").prop("disabled", true);
        else refrescarSeleccion();
    }

    function pintarCargando() {
        $("#dashboardFilas").html(
            '<tr class="bncd-state-row"><td colspan="13" class="bncd-state-cell">' +
            '<div class="bncd-feedback is-loading"><span class="bncd-feedback-icon" aria-hidden="true"><i class="clip-spinner-2"></i></span>' +
            '<strong>Cargando borradores</strong><p>Estamos preparando el resumen y los resultados.</p></div></td></tr>');
    }

    function mostrarEstado(tipo, titulo, detalle) {
        var esError = tipo === "error";
        var icono = esError ? "clip-warning" : "clip-search";
        var accion = esError ? '<button type="button" class="bncd-btn" id="btnReintentar"><i class="clip-refresh" aria-hidden="true"></i> Reintentar</button>' : "";
        $("#dashboardFilas").html(
            '<tr class="bncd-state-row"><td colspan="13" class="bncd-state-cell">' +
            '<div class="bncd-feedback ' + (esError ? "is-error" : "is-empty") + '">' +
            '<span class="bncd-feedback-icon" aria-hidden="true"><i class="' + icono + '"></i></span>' +
            '<strong>' + esc(titulo) + '</strong><p>' + esc(detalle) + '</p>' + accion + '</div></td></tr>');
    }

    function mostrarResumenNoDisponible() {
        $("#kpiTotal,#kpiPendientes,#kpiAutorizados,#kpiRechazados,#kpiAnulados,#kpiPromedio")
            .removeClass("is-skeleton is-updated").text("—");
        $("#kpiVencidos").text("Sin información");
        $("#totalesMoneda").empty();
        estado.pagina = 1;
        estado.total = 0;
        estado.paginas = 1;
        $("#totalFilas").text("0");
        $("#rangoPagina").text("0–0 de 0");
        $("#paginaActual").text("1 / 1");
        $("#paginaAnterior,#paginaSiguiente").prop("disabled", true);
    }

    function cargar() {
        if (solicitudActual && solicitudActual.readyState !== 4) solicitudActual.abort();
        var solicitud = ++numeroSolicitud;
        establecerCarga(true);
        pintarCargando();
        anunciar("Cargando borradores.");

        solicitudActual = $.ajax({ url: $app.data("url-consultar"), data: filtro(), cache: false })
            .done(function (respuesta) {
                if (solicitud !== numeroSolicitud) return;
                if (!respuesta || !respuesta.ok) {
                    if (window.console && console.error) console.error("Dashboard BNC:", respuesta && respuesta.msg);
                    mostrarResumenNoDisponible();
                    mostrarEstado("error", "No pudimos cargar el Dashboard", "Intenta nuevamente. Si el problema continúa, comunícate con Sistemas.");
                    anunciar("No fue posible cargar el Dashboard.");
                    return;
                }
                pintar(respuesta.data);
            })
            .fail(function (xhr, estadoPeticion) {
                if (estadoPeticion === "abort" || solicitud !== numeroSolicitud) return;
                var prohibido = xhr && xhr.status === 403;
                mostrarResumenNoDisponible();
                mostrarEstado("error", prohibido ? "Acceso no autorizado" : "No pudimos cargar el Dashboard",
                    prohibido ? "Tu usuario no tiene acceso a esta información." : "Verifica tu conexión e intenta nuevamente.");
                anunciar(prohibido ? "Acceso no autorizado." : "No fue posible cargar el Dashboard.");
            })
            .always(function () {
                if (solicitud === numeroSolicitud) establecerCarga(false);
            });
    }

    function pintar(data) {
        data = data || {};
        var filas = data.Filas || [];
        estado.total = data.TotalFilas || 0;
        estado.paginas = Math.max(1, Math.ceil(estado.total / (data.TamanoPagina || 25)));

        if (!opcionesCargadas) {
            opciones($("#fEmpresa"), data.Empresas);
            opciones($("#fAgente"), data.Agentes);
            opciones($("#fCreador"), data.Creadores);
            opciones($("#fResuelto"), data.Resolutores);
            opciones($("#fMoneda"), data.Monedas);
            opcionesCargadas = true;
        }

        pintarResumen(data.Resumen || {});
        if (!filas.length) {
            mostrarEstado("empty", "No encontramos borradores", "Prueba limpiando los filtros o utiliza otros criterios de búsqueda.");
        } else {
            var html = "";
            $.each(filas, function (indice, item) {
                var llave = clave(item);
                var url = $app.data("url-ver") + "?empresa=" + encodeURIComponent(item.IdEmpresa) +
                    "&idBorrador=" + encodeURIComponent(item.IdBorrador);
                html += '<tr style="animation-delay:' + Math.min(indice * 18, 180) + 'ms">' +
                    '<td class="action" data-label="Acciones"><span class="bncd-row-actions">' +
                    '<a class="bncd-view" target="_blank" rel="noopener noreferrer" title="Ver e imprimir borrador" aria-label="Ver borrador ' + esc(item.IdBorrador) + '" href="' + url + '"><i class="clip-eye" aria-hidden="true"></i></a>' +
                    '<button type="button" class="bncd-view audit" title="Ver auditoría" aria-label="Ver auditoría de ' + esc(item.IdBorrador) + '" data-empresa="' + esc(item.IdEmpresa) + '" data-borrador="' + esc(item.IdBorrador) + '"><i class="clip-history" aria-hidden="true"></i></button>' +
                    '</span></td>' +
                    '<td class="check" data-label="Seleccionar"><input type="checkbox" class="fila-check" data-clave="' + esc(llave) + '" ' + (seleccion[llave] ? "checked" : "") + ' aria-label="Seleccionar ' + esc(item.IdBorrador) + '"></td>' +
                    '<td data-label="Borrador"><span class="bncd-main">' + esc(item.IdBorrador) + '</span><span class="bncd-sub">Creado por ' + esc(item.IdUsr) + '</span></td>' +
                    '<td data-label="Empresa">' + esc(item.IdEmpresa) + '</td>' +
                    '<td data-label="Fecha">' + fecha(item.Registro || item.Fecha, false) + '</td>' +
                    '<td data-label="Cliente"><span class="bncd-main">' + esc(item.Nombre) + '</span><span class="bncd-sub">' + esc(item.IdCliente) + (item.Nit ? " · " + esc(item.Nit) : "") + '</span></td>' +
                    '<td data-label="Agente">' + esc(item.Agente) + '</td>' +
                    '<td data-label="Estado"><span class="bncd-state ' + esc(item.Estado) + '">' + esc(item.Estado) + '</span></td>' +
                    '<td class="number" data-label="Total"><span class="bncd-main">' + esc(item.Moneda) + ' ' + moneda(item.Total) + '</span></td>' +
                    '<td data-label="Facturas"><span class="bncd-count"><i class="clip-file" aria-hidden="true"></i> ' + Number(item.Facturas || 0) + '</span>' +
                    (item.TieneAntecedentesSap ? '<span class="bncd-sub bncd-flag">Con antecedentes SAP</span>' : "") + '</td>' +
                    '<td data-label="Adjuntos"><span class="bncd-count"><i class="clip-attachment" aria-hidden="true"></i> ' + Number(item.Adjuntos || 0) + '</span></td>' +
                    '<td data-label="Creado por">' + esc(item.IdUsr) + '</td>' +
                    '<td data-label="Resolución"><span class="bncd-main">' + esc(item.ResueltoPor || "Pendiente") + '</span><span class="bncd-sub">' + fecha(item.FechaResolucion, true) + '</span></td></tr>';
            });
            $("#dashboardFilas").html(html);
        }

        var inicio = estado.total ? ((estado.pagina - 1) * (data.TamanoPagina || 25)) + 1 : 0;
        var fin = Math.min(estado.total, estado.pagina * (data.TamanoPagina || 25));
        $("#totalFilas").text(estado.total);
        $("#rangoPagina").text(inicio + "–" + fin + " de " + estado.total);
        $("#paginaActual").text(estado.pagina + " / " + estado.paginas);
        $("#paginaAnterior").prop("disabled", estado.pagina <= 1);
        $("#paginaSiguiente").prop("disabled", estado.pagina >= estado.paginas);
        refrescarSeleccion();
        anunciar(estado.total + " borradores encontrados.");
    }

    function actualizarKpi(selector, valor) {
        var $elemento = $(selector);
        $elemento.removeClass("is-skeleton is-updated").text(valor);
        if ($elemento[0]) void $elemento[0].offsetWidth;
        $elemento.addClass("is-updated");
    }

    function pintarResumen(resumen) {
        actualizarKpi("#kpiTotal", resumen.Total || 0);
        actualizarKpi("#kpiPendientes", resumen.Pendientes || 0);
        $("#kpiVencidos").text((resumen.PendientesVencidos || 0) + " vencidos");
        actualizarKpi("#kpiAutorizados", resumen.Autorizados || 0);
        actualizarKpi("#kpiRechazados", resumen.Rechazados || 0);
        actualizarKpi("#kpiAnulados", resumen.Anulados || 0);
        actualizarKpi("#kpiPromedio", resumen.HorasPromedioResolucion == null ? "—" : Number(resumen.HorasPromedioResolucion).toFixed(1) + " h");

        var html = "";
        $.each(resumen.TotalesPorMoneda || [], function (_, item) {
            html += "<span>" + esc(item.Moneda) + "<strong>" + moneda(item.Total) + "</strong></span>";
        });
        $("#totalesMoneda").html(html);
    }

    function opciones($select, valores) {
        var actual = $select.val();
        var primera = $select.find("option:first")[0];
        var html = primera ? primera.outerHTML : '<option value="">Todos</option>';
        $.each(valores || [], function (_, valor) {
            html += '<option value="' + esc(valor) + '">' + esc(valor) + "</option>";
        });
        $select.html(html).val(actual);
    }

    function refrescarSeleccion() {
        var cantidad = Object.keys(seleccion).length;
        $("#seleccionados").text(cantidad + (cantidad === 1 ? " seleccionado" : " seleccionados"));
        $("#btnImprimir").prop("disabled", $app.hasClass("is-loading") || cantidad === 0);
        var $checks = $(".fila-check");
        var marcados = $checks.filter(":checked").length;
        $("#seleccionarPagina").prop("checked", $checks.length > 0 && $checks.length === marcados)
            .prop("indeterminate", marcados > 0 && marcados < $checks.length);
    }

    function limpiarSeleccion() { seleccion = {}; refrescarSeleccion(); }

    function agregarCampos($formulario, datos) {
        $formulario.find("input[data-dinamico]").remove();
        $.each(datos, function (nombre, valor) {
            if (valor === "" || valor == null) return;
            $("<input>", { type: "hidden", name: nombre, value: valor, "data-dinamico": "1" }).appendTo($formulario);
        });
    }

    function actualizarFiltrosAvanzados() {
        var cantidad = 0;
        $.each(["#fAgente", "#fCreador", "#fResuelto", "#fMoneda", "#fAdjuntos", "#fAntecedentes"], function (_, selector) {
            if ($(selector).val()) cantidad++;
        });
        $("#contadorFiltros").prop("hidden", cantidad === 0).text(cantidad);
    }

    function alternarFiltros(forzar) {
        var $boton = $("#btnMasFiltros");
        var abrir = typeof forzar === "boolean" ? forzar : $boton.attr("aria-expanded") !== "true";
        $boton.attr("aria-expanded", abrir ? "true" : "false");
        var $panel = $("#filtrosAvanzados").prop("hidden", !abrir).removeClass("is-opening");
        if (abrir) {
            if ($panel[0]) void $panel[0].offsetWidth;
            $panel.addClass("is-opening");
        }
    }

    $(document).on("click", "#btnReintentar", cargar);
    $(document).on("change", ".fila-check", function () {
        var llave = $(this).data("clave");
        if (this.checked) seleccion[llave] = true;
        else delete seleccion[llave];
        refrescarSeleccion();
    });

    $(document).on("click", ".bncd-view.audit", function () {
        var empresa = $(this).data("empresa");
        var borrador = $(this).data("borrador");
        ultimoFocoModal = this;
        $("#subtituloBitacora").text(empresa + " · " + borrador);
        $("#listaBitacora").html('<div class="bncd-feedback is-loading"><span class="bncd-feedback-icon"><i class="clip-spinner-2"></i></span><strong>Cargando historial</strong></div>');
        $("#modalBitacora").prop("hidden", false);
        $("#cerrarBitacora").trigger("focus");

        $.getJSON($app.data("url-bitacora"), { empresa: empresa, idBorrador: borrador })
            .done(function (respuesta) {
                if (!respuesta || !respuesta.ok) {
                    $("#listaBitacora").html('<div class="bncd-feedback is-error"><span class="bncd-feedback-icon"><i class="clip-warning"></i></span><strong>No fue posible consultar el historial</strong></div>');
                    return;
                }
                var html = "";
                $.each(respuesta.data || [], function (_, item) {
                    var cambio = item.EstadoAnterior || item.EstadoNuevo ? " · " + esc(item.EstadoAnterior || "—") + " → " + esc(item.EstadoNuevo || "—") : "";
                    html += '<article class="bncd-event"><strong>' + esc(item.Evento) + cambio + '</strong><small>' +
                        fecha(item.Registro, true) + " · " + esc(item.Usuario) + (item.Ip ? " · " + esc(item.Ip) : "") +
                        '</small>' + (item.Detalle ? '<p>' + esc(item.Detalle) + '</p>' : "") + '</article>';
                });
                $("#listaBitacora").html(html || '<div class="bncd-feedback is-empty"><span class="bncd-feedback-icon"><i class="clip-clock"></i></span><strong>Sin eventos registrados</strong><p>Los borradores creados antes de habilitar la bitácora pueden no tener historial.</p></div>');
            })
            .fail(function () {
                $("#listaBitacora").html('<div class="bncd-feedback is-error"><span class="bncd-feedback-icon"><i class="clip-warning"></i></span><strong>No fue posible consultar el historial</strong></div>');
            });
    });

    function cerrarModal() {
        $("#modalBitacora").prop("hidden", true);
        if (ultimoFocoModal) $(ultimoFocoModal).trigger("focus");
    }

    $("#cerrarBitacora").on("click", cerrarModal);
    $("#modalBitacora").on("click", function (evento) { if (evento.target === this) cerrarModal(); });
    $(document).on("keydown", function (evento) { if (evento.key === "Escape" && !$("#modalBitacora").prop("hidden")) cerrarModal(); });
    $("#btnMasFiltros").on("click", function () { alternarFiltros(); });

    $("#seleccionarPagina").on("change", function () {
        var activo = this.checked;
        $(".fila-check").each(function () {
            this.checked = activo;
            var llave = $(this).data("clave");
            if (activo) seleccion[llave] = true;
            else delete seleccion[llave];
        });
        refrescarSeleccion();
    });

    $(".bncd-filter-grid select,.bncd-filter-grid input[type=date]").on("change", function () {
        estado.pagina = 1;
        limpiarSeleccion();
        actualizarFiltrosAvanzados();
        cargar();
    });
    $("#fTexto").on("input", function () {
        clearTimeout(temporizador);
        temporizador = setTimeout(function () { estado.pagina = 1; limpiarSeleccion(); cargar(); }, 350);
    });
    $(".bncd-kpi[data-state]").on("click", function () {
        $("#fEstado").val($(this).data("state"));
        $(".bncd-kpi[data-state]").removeClass("active").attr("aria-pressed", "false");
        $(this).addClass("active").attr("aria-pressed", "true");
        estado.pagina = 1;
        limpiarSeleccion();
        cargar();
    });
    $("#fEstado").on("change", function () {
        $(".bncd-kpi[data-state]").removeClass("active").attr("aria-pressed", "false")
            .filter('[data-state="' + $(this).val() + '"]').addClass("active").attr("aria-pressed", "true");
    });
    $("#btnLimpiar").on("click", function () {
        $(".bncd-filter-grid input").val("");
        $(".bncd-filter-grid select").prop("selectedIndex", 0);
        $(".bncd-kpi[data-state]").removeClass("active").attr("aria-pressed", "false").first().addClass("active").attr("aria-pressed", "true");
        actualizarFiltrosAvanzados();
        alternarFiltros(false);
        estado.pagina = 1;
        limpiarSeleccion();
        cargar();
    });
    $("#tamanoPagina").on("change", function () { estado.pagina = 1; cargar(); });
    $("#paginaAnterior").on("click", function () { if (estado.pagina > 1) { estado.pagina--; cargar(); } });
    $("#paginaSiguiente").on("click", function () { if (estado.pagina < estado.paginas) { estado.pagina++; cargar(); } });
    $("th[data-sort]").on("click", function () {
        var orden = $(this).data("sort");
        estado.direccion = estado.orden === orden && estado.direccion === "DESC" ? "ASC" : "DESC";
        estado.orden = orden;
        $("th[data-sort]").attr("aria-sort", "none");
        $(this).attr("aria-sort", estado.direccion === "ASC" ? "ascending" : "descending");
        cargar();
    });
    $("#btnExportar").on("click", function () {
        var $formulario = $("#formExportar");
        agregarCampos($formulario, filtro());
        $.each(Object.keys(seleccion), function (_, llave) {
            $("<input>", { type: "hidden", name: "claves", value: llave, "data-dinamico": "1" }).appendTo($formulario);
        });
        $formulario.trigger("submit");
    });
    $("#btnImprimir").on("click", function () {
        var claves = Object.keys(seleccion);
        var maximo = Number($app.data("max-imprimir"));
        if (claves.length > maximo) {
            window.alert("Seleccione como máximo " + maximo + " borradores por lote.");
            return;
        }
        var $formulario = $("#formImprimir");
        $formulario.find("input[data-dinamico]").remove();
        $.each(claves, function (_, llave) {
            $("<input>", { type: "hidden", name: "claves", value: llave, "data-dinamico": "1" }).appendTo($formulario);
        });
        $formulario.trigger("submit");
    });

    actualizarFiltrosAvanzados();
    cargar();
})(window.jQuery);
