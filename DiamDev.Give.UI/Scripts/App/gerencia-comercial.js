(function () {
    "use strict";
    var root = document.getElementById("gc");
    if (!root) return;
    var byId = function (id) { return document.getElementById(id); };
    var state = { type: "ventas", page: 1, size: 25, total: 0, listRequest: 0, summaryRequest: 0, trendCurrency: "GTQ", trendDay: null, trendData: null };
    var companies = ["BOLIK", "FAES", "GRACO"];
    var currency = function (value, code) { return (code || "GTQ") + " " + Number(value || 0).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); };
    var integer = function (value) { return Number(value || 0).toLocaleString("es-GT"); };
    var dateInput = function (date) { return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0"); };
    var text = function (value) { return value === null || value === undefined || value === "" ? "—" : String(value); };
    var create = function (tag, className, content) { var e = document.createElement(tag); if (className) e.className = className; if (content !== undefined) e.textContent = content; return e; };
    var sum = function (items, key) { return (items || []).reduce(function (acc, item) { return acc + Number(item[key] || 0); }, 0); };

    function setPeriod(period) {
        var end = new Date(), start = new Date(end.getFullYear(), end.getMonth(), end.getDate());
        if (period === "week") { var day = (start.getDay() + 6) % 7; start.setDate(start.getDate() - day); }
        if (period === "month") start.setDate(1);
        if (period === "today") start = new Date(end.getFullYear(), end.getMonth(), end.getDate());
        if (period !== "custom") { byId("gc-desde").value = dateInput(start); byId("gc-hasta").value = dateInput(end); }
        root.querySelectorAll("[data-period]").forEach(function (b) { b.classList.toggle("is-active", b.getAttribute("data-period") === period); });
    }

    function params(extra) {
        var p = new URLSearchParams({ empresa: byId("gc-empresa").value, desde: byId("gc-desde").value, hasta: byId("gc-hasta").value, moneda: byId("gc-moneda").value, estado: byId("gc-estado").value, agente: byId("gc-agente").value.trim(), cliente: byId("gc-cliente").value.trim() });
        Object.keys(extra || {}).forEach(function (key) { p.set(key, extra[key]); });
        return p;
    }
    function url(kind, extra) { return root.getAttribute("data-" + kind) + "?" + params(extra).toString(); }
    function alertMessage(message, error) { var a = byId("gc-alert"); a.textContent = message || ""; a.hidden = !message; a.classList.toggle("is-error", !!error); }
    async function json(urlValue) {
        var res = await fetch(urlValue, { credentials: "same-origin", headers: { "Accept": "application/json" }, cache: "no-store" });
        if (res.redirected && res.url.toLowerCase().indexOf("login") !== -1) { window.location.href = res.url; throw new Error("La sesión terminó. Inicia sesión de nuevo."); }
        var data;
        try { data = await res.json(); } catch (_) { throw new Error("La respuesta del servidor no pudo leerse."); }
        if (!res.ok) throw new Error(data.error || "No fue posible cargar los datos.");
        return data;
    }

    function kpi(title, value, help, tone) {
        var card = create("article", "gc-kpi " + (tone || ""));
        card.appendChild(create("span", "", title)); card.appendChild(create("strong", "", value)); card.appendChild(create("small", "", help));
        return card;
    }
    function renderKpis(data) {
        var ventas = data.ventas || [], cobros = data.cobros || [], catalogos = data.catalogos || [], sql = data.pedidosK66 || [];
        var gtq = ventas.filter(function (v) { return v.Moneda === "GTQ"; });
        var usd = ventas.filter(function (v) { return v.Moneda === "USD"; });
        var box = byId("gc-kpis"); box.replaceChildren();
        box.appendChild(kpi("Ventas netas GTQ", currency(sum(gtq, "Neto"), "GTQ"), "Facturas menos notas de crédito · sin IVA", "green"));
        box.appendChild(kpi("Ventas netas USD", currency(sum(usd, "Neto"), "USD"), "Sin convertir ni mezclar monedas", ""));
        box.appendChild(kpi("Cobros SAP GTQ", currency(sum(cobros.filter(function (v) { return v.Moneda === "GTQ"; }), "Monto"), "GTQ"), "Pagos recibidos en el período", "green"));
        box.appendChild(kpi("Cobros SAP USD", currency(sum(cobros.filter(function (v) { return v.Moneda === "USD"; }), "Monto"), "USD"), "No se suman a ventas", ""));
        box.appendChild(kpi("Facturas del período", integer(sum(ventas, "Facturas")), "Notas de crédito: " + integer(sum(ventas, "NotasCredito")), "violet"));
        box.appendChild(kpi("Pedidos SAP del período", integer(sum(catalogos, "PedidosPeriodo")), "Documentos creados durante el rango", "amber"));
        box.appendChild(kpi("Pedidos SAP abiertos", integer(sum(catalogos, "PedidosAbiertos")), "Estado actual en SAP", "amber"));
        box.appendChild(kpi("Clientes SAP", integer(sum(catalogos, "Clientes")), "Catálogo de las empresas seleccionadas", ""));
        box.appendChild(kpi("Productos SAP", integer(sum(catalogos, "Productos")), "Catálogo completo, incluidos inactivos", "green"));
        box.appendChild(kpi("Agentes SAP", integer(sum(catalogos, "Agentes")), "Catálogo completo, incluidos inactivos", "violet"));
        box.appendChild(kpi("Pedidos K66 pendientes", integer(sum(sql, "Pendientes")), integer(sum(sql, "Pedidos")) + " pedidos en SQL durante el período", "amber"));
        var extraCodes = Array.from(new Set(ventas.concat(cobros).map(function (x) { return x.Moneda; }).filter(function (x) { return x && x !== "GTQ" && x !== "USD"; }))).sort();
        extraCodes.forEach(function (code) {
            box.appendChild(kpi("Ventas netas " + code, currency(sum(ventas.filter(function (x) { return x.Moneda === code; }), "Neto"), code), "Moneda separada", ""));
            box.appendChild(kpi("Cobros SAP " + code, currency(sum(cobros.filter(function (x) { return x.Moneda === code; }), "Monto"), code), "Pagos recibidos", "green"));
        });
    }

    function svgElement(tag, attributes, content) {
        var element = document.createElementNS("http://www.w3.org/2000/svg", tag);
        Object.keys(attributes || {}).forEach(function (name) { element.setAttribute(name, attributes[name]); });
        if (content !== undefined) element.textContent = content;
        return element;
    }
    function renderTrendDay(context, day) {
        state.trendDay = day;
        var index = context.days.indexOf(day), point = context.points[day], target = byId("gc-trend-day");
        if (index < 0) return;
        if (context.slider) context.slider.value = String(index);
        var x = context.x(index);
        context.marker.setAttribute("x1", x); context.marker.setAttribute("x2", x);
        context.dot.setAttribute("cx", x); context.dot.setAttribute("cy", context.y(point.net));
        target.replaceChildren();
        var head = create("div", "gc-trend-day-head");
        head.appendChild(create("strong", "", day));
        head.appendChild(create("b", "", currency(point.net, context.code)));
        target.appendChild(head);
        target.appendChild(create("p", "", integer(point.invoices) + " facturas · " + integer(point.credits) + " notas de crédito"));
        var breakdown = create("div", "gc-trend-breakdown");
        companies.forEach(function (company) {
            var value = point.companies[company];
            if (value !== undefined) breakdown.appendChild(create("span", "", company + " · " + currency(value, context.code)));
        });
        if (!breakdown.childNodes.length) breakdown.appendChild(create("span", "", "Sin movimientos en este día"));
        target.appendChild(breakdown);
    }
    function renderTrend(data) {
        state.trendData = data;
        var target = byId("gc-trend"), stats = byId("gc-trend-stats"), dayBox = byId("gc-trend-day");
        target.replaceChildren(); stats.replaceChildren(); dayBox.replaceChildren();
        var rows = data.ventas || [];
        var available = { GTQ: false, USD: false };
        rows.forEach(function (r) { if (available.hasOwnProperty(r.Moneda)) available[r.Moneda] = true; });
        if (!available[state.trendCurrency] && (available.GTQ || available.USD)) state.trendCurrency = available.GTQ ? "GTQ" : "USD";
        root.querySelectorAll("[data-trend-currency]").forEach(function (button) {
            var code = button.getAttribute("data-trend-currency"), selected = code === state.trendCurrency;
            button.disabled = !available[code]; button.classList.toggle("is-active", selected); button.setAttribute("aria-pressed", selected ? "true" : "false");
        });
        var code = state.trendCurrency;
        if (!available[code]) { target.appendChild(create("p", "gc-empty", "No hay ventas en este período.")); return; }
        var points = {}, days = [], cursor = new Date(data.desde + "T00:00:00"), end = new Date(data.hasta + "T00:00:00");
        while (cursor <= end) {
            var key = dateInput(cursor); days.push(key); points[key] = { net: 0, invoices: 0, credits: 0, companies: {} }; cursor.setDate(cursor.getDate() + 1);
        }
        rows.filter(function (r) { return r.Moneda === code; }).forEach(function (r) {
            var key = String(r.Dia || "").slice(0, 10), point = points[key]; if (!point) return;
            var amount = Number(r.Neto || 0); point.net += amount;
            point.invoices += Number(r.Facturas || 0); point.credits += Number(r.NotasCredito || 0);
            point.companies[r.Empresa] = (point.companies[r.Empresa] || 0) + amount;
        });
        if (!days.length) { target.appendChild(create("p", "gc-empty", "No hay días en el período seleccionado.")); return; }
        var values = days.map(function (d) { return points[d].net; });
        var min = Math.min(0, Math.min.apply(null, values)), max = Math.max(0, Math.max.apply(null, values));
        if (min === max) max = min + 1;
        var y = function (value) { return 210 - (value - min) * 190 / (max - min); };
        var x = function (index) { return days.length === 1 ? 375 : 70 + index * 610 / (days.length - 1); };
        var svg = svgElement("svg", { viewBox: "0 0 700 250", role: "img", "aria-label": "Ventas netas diarias en " + code + " del " + days[0] + " al " + days[days.length - 1] });
        for (var i = 0; i <= 4; i++) {
            var tick = min + (max - min) * i / 4, tickY = y(tick);
            svg.appendChild(svgElement("line", { x1: 70, x2: 680, y1: tickY, y2: tickY, stroke: Math.abs(tick) < 0.001 ? "#9fb3c1" : "#e7edf2", "stroke-width": Math.abs(tick) < 0.001 ? 1.5 : 1 }));
            svg.appendChild(svgElement("text", { x: 62, y: tickY + 4, "text-anchor": "end", class: "gc-axis-text" }, new Intl.NumberFormat("es-GT", { notation: "compact", maximumFractionDigits: 1 }).format(tick)));
        }
        var labelCount = Math.min(5, days.length), used = {};
        for (var j = 0; j < labelCount; j++) {
            var labelIndex = Math.round(j * (days.length - 1) / Math.max(1, labelCount - 1));
            if (used[labelIndex]) continue; used[labelIndex] = true;
            svg.appendChild(svgElement("text", { x: x(labelIndex), y: 238, "text-anchor": days.length === 1 ? "middle" : labelIndex === 0 ? "start" : labelIndex === days.length - 1 ? "end" : "middle", class: "gc-axis-text" }, days[labelIndex].slice(5)));
        }
        var color = code === "GTQ" ? "#2082af" : "#209a7b";
        if (days.length === 1) {
            svg.appendChild(svgElement("line", { x1: 375, x2: 375, y1: y(0), y2: y(values[0]), stroke: color, "stroke-width": 20, "stroke-linecap": "round" }));
        } else {
            svg.appendChild(svgElement("polyline", { points: days.map(function (d, index) { return x(index).toFixed(2) + "," + y(points[d].net).toFixed(2); }).join(" "), fill: "none", stroke: color, "stroke-width": 3, "stroke-linecap": "round", "stroke-linejoin": "round" }));
        }
        var marker = svgElement("line", { y1: 20, y2: 210, stroke: color, "stroke-dasharray": "4 4", opacity: .55 });
        var dot = svgElement("circle", { r: 5, fill: color, stroke: "white", "stroke-width": 2 });
        svg.appendChild(marker); svg.appendChild(dot);
        var context = { days: days, points: points, marker: marker, dot: dot, x: x, y: y, code: code };
        var hitArea = svgElement("rect", { x: 70, y: 20, width: 610, height: 190, fill: "transparent", class: "gc-chart-hit" });
        hitArea.addEventListener("mousemove", function (event) {
            var bounds = hitArea.getBoundingClientRect(), fraction = Math.max(0, Math.min(1, (event.clientX - bounds.left) / bounds.width));
            renderTrendDay(context, days[Math.round(fraction * (days.length - 1))]);
        });
        svg.appendChild(hitArea); target.appendChild(svg);
        var sliderLabel = create("label", "gc-trend-slider-label", "Explorar día");
        var selectedIndex = days.indexOf(state.trendDay);
        if (selectedIndex < 0) selectedIndex = days.length - 1;
        var slider = create("input", "gc-trend-slider"); slider.type = "range"; slider.min = "0"; slider.max = String(days.length - 1); slider.value = String(selectedIndex);
        slider.setAttribute("aria-label", "Día seleccionado en la gráfica");
        slider.addEventListener("input", function () { renderTrendDay(context, days[Number(slider.value)]); });
        context.slider = slider;
        sliderLabel.appendChild(slider); target.appendChild(sliderLabel);
        var total = values.reduce(function (a, b) { return a + b; }, 0), peak = days.reduce(function (best, d) { return points[d].net > points[best].net ? d : best; }, days[0]);
        [["Total del período", currency(total, code)], ["Promedio diario", currency(total / days.length, code)], ["Mejor día", peak + " · " + currency(points[peak].net, code)]].forEach(function (pair) {
            var item = create("div", "gc-trend-stat"); item.appendChild(create("small", "", pair[0])); item.appendChild(create("strong", "", pair[1])); stats.appendChild(item);
        });
        renderTrendDay(context, days[Number(slider.value)]);
    }

    function renderCompanies(data) {
        var target = byId("gc-companies"); target.replaceChildren();
        var selected = byId("gc-empresa").value;
        companies.filter(function (c) { return !selected || selected === c; }).forEach(function (name) {
            var c = (data.catalogos || []).find(function (x) { return x.Empresa === name; });
            var s = (data.pedidosK66 || []).find(function (x) { return x.Empresa === name; });
            var sales = (data.ventas || []).filter(function (x) { return x.Empresa === name; });
            var row = create("div", "gc-company"), head = create("div", "gc-company-head");
            head.appendChild(create("span", "", name)); head.appendChild(create("em", "", c ? "SAP conectado" : "SAP sin datos")); row.appendChild(head);
            var stats = create("div", "gc-company-stats");
            [["Ventas GTQ", currency(sum(sales.filter(function (x) { return x.Moneda === "GTQ"; }), "Neto"), "GTQ")], ["Ventas USD", currency(sum(sales.filter(function (x) { return x.Moneda === "USD"; }), "Neto"), "USD")], ["Pedidos del período", integer(c && c.PedidosPeriodo)], ["Pedidos abiertos", integer(c && c.PedidosAbiertos)], ["K66 pendientes", integer(s && s.Pendientes)]].forEach(function (pair) { var cell = create("span"); cell.appendChild(create("b", "", pair[0] + ": ")); cell.appendChild(document.createTextNode(pair[1])); stats.appendChild(cell); });
            row.appendChild(stats); target.appendChild(row);
        });
    }
    function renderRanking(targetId, rows, amountKey, formatter, separateCurrency) {
        var target = byId(targetId); target.replaceChildren();
        if (!rows || !rows.length) { target.appendChild(create("p", "gc-empty", "Sin datos en este período.")); return; }
        var groups = separateCurrency ? Array.from(new Set(rows.map(function (x) { return x.Moneda || "Otra"; }))).sort(function (a, b) { return (a === "GTQ" ? -1 : a === "USD" ? 0 : 1) - (b === "GTQ" ? -1 : b === "USD" ? 0 : 1); }) : [null];
        groups.forEach(function (code) {
            if (code) target.appendChild(create("div", "gc-rank-currency", code));
            rows.filter(function (r) { return !code || (r.Moneda || "Otra") === code; }).slice().sort(function (a, b) { return Number(b[amountKey] || 0) - Number(a[amountKey] || 0); }).slice(0, separateCurrency ? 3 : 6).forEach(function (r, index) {
                var row = create("div", "gc-rank-row"); row.appendChild(create("span", "gc-rank-number", String(index + 1)));
                var label = create("div", "gc-rank-name"); label.appendChild(create("span", "", text(r.Nombre))); label.appendChild(create("small", "gc-rank-sub", r.Empresa + " · " + text(r.Codigo))); row.appendChild(label);
                row.appendChild(create("b", "gc-rank-value", formatter(r))); target.appendChild(row);
            });
        });
    }

    async function loadSummary(force) {
        var request = ++state.summaryRequest;
        byId("gc-updated").textContent = "Actualizando…";
        try {
            var data = await json(url("resumen", force ? { forzar: "true" } : {})); if (request !== state.summaryRequest) return;
            renderKpis(data); renderTrend(data); renderCompanies(data);
            renderRanking("gc-products-top", data.productos, "Cantidad", function (r) { return integer(r.Cantidad); });
            renderRanking("gc-agents-top", data.agentes, "Neto", function (r) { return currency(r.Neto, r.Moneda); }, true);
            renderRanking("gc-clients-top", data.clientes, "Neto", function (r) { return currency(r.Neto, r.Moneda); }, true);
            byId("gc-updated").textContent = "Consulta: " + text(data.actualizado);
            alertMessage((data.advertencias || []).join(" "), false);
        } catch (err) { if (request === state.summaryRequest) { byId("gc-updated").textContent = "Sin actualizar"; alertMessage(err.message, true); byId("gc-kpis").replaceChildren(create("p", "gc-empty", "No se pudieron cargar los indicadores.")); } }
    }

    function tableCell(row, value, className) { var td = create("td"); if (className) td.className = className; td.textContent = text(value); row.appendChild(td); }
    function renderTable(data) {
        state.total = Number(data.total || 0); var body = byId("gc-rows"); body.replaceChildren();
        if (!data.filas || !data.filas.length) { var empty = create("tr"); var cell = create("td", "gc-empty", "No se encontraron registros para estos filtros."); cell.colSpan = 11; empty.appendChild(cell); body.appendChild(empty); }
        (data.filas || []).forEach(function (r) {
            var tr = create("tr"), badge = create("td"); badge.appendChild(create("span", "gc-company-badge " + r.Empresa, r.Empresa)); tr.appendChild(badge);
            tableCell(tr, r.Codigo); tableCell(tr, r.Nombre); tableCell(tr, r.Tipo); tableCell(tr, r.Fecha ? String(r.Fecha).slice(0, 10) : null); tableCell(tr, r.Estado === "Y" ? "Activo" : r.Estado === "N" ? "Inactivo" : r.Estado); tableCell(tr, r.Moneda);
            tableCell(tr, r.Importe === null || r.Importe === undefined ? null : currency(r.Importe, r.Moneda)); tableCell(tr, r.Secundario); tableCell(tr, r.Existencia === null || r.Existencia === undefined ? null : integer(r.Existencia));
            var actionCell = create("td"), button = create("button", "gc-detail-btn", "Ver detalle"); button.type = "button"; button.addEventListener("click", function () { showDetail(r); }); actionCell.appendChild(button); tr.appendChild(actionCell); body.appendChild(tr);
        });
        var first = state.total ? (state.page - 1) * state.size + 1 : 0, last = Math.min(state.total, state.page * state.size);
        byId("gc-count").textContent = integer(first) + "–" + integer(last) + " de " + integer(state.total) + " registros";
        byId("gc-page").textContent = String(state.page); byId("gc-prev").disabled = state.page <= 1; byId("gc-next").disabled = last >= state.total;
    }
    function configureAdvanced() {
        var sql = state.type.endsWith("Sql"), docs = state.type === "ventas" || state.type === "pedidos", orderSql = state.type === "pedidosSql";
        var options = orderSql ? [["", "Todos"], ["Pendiente", "Pendiente"], ["Sincronizado", "Sincronizado"]] : docs ? [["", "Todos"], ["O", "Abierto"], ["C", "Cerrado"]] : [["", "Todos"], ["Y", "Activo"], ["N", "Inactivo"]];
        var select = byId("gc-estado"); select.replaceChildren(); options.forEach(function (pair) { var o = create("option", "", pair[1]); o.value = pair[0]; select.appendChild(o); });
        byId("gc-agente-wrap").hidden = sql || state.type === "productos";
        byId("gc-cliente-wrap").hidden = !(docs || state.type === "clientes" || orderSql);
        byId("gc-agente").value = ""; byId("gc-cliente").value = "";
    }
    async function loadTable() {
        if (root.querySelector('[data-type="' + state.type + '"][data-unavailable]')) return;
        var request = ++state.listRequest, body = byId("gc-rows"); body.replaceChildren();
        var tr = create("tr"), td = create("td", "gc-empty", "Cargando registros…"); td.colSpan = 11; tr.appendChild(td); body.appendChild(tr);
        try { var data = await json(url("listar", { tipo: state.type, buscar: byId("gc-search").value.trim(), pagina: state.page, tamano: state.size })); if (request === state.listRequest) renderTable(data); }
        catch (err) { if (request === state.listRequest) { state.total = 0; body.replaceChildren(); var row = create("tr"), cell = create("td", "gc-empty", err.message); cell.colSpan = 11; row.appendChild(cell); body.appendChild(row); alertMessage(err.message, true); } }
    }

    var lastFocus = null;
    function renderSalesDetail(body, sections) {
        var header = (sections.Principal || [])[0];
        if (!header) { body.appendChild(create("p", "gc-empty", "No se encontró el documento.")); return; }
        var section = create("section", "gc-modal-section"); section.appendChild(create("h3", "", "Documento y cliente"));
        var grid = create("div", "gc-details-grid");
        [["Documento", header.Documento], ["Fecha", String(header.Fecha || "").slice(0, 10)], ["Vencimiento", String(header.Vencimiento || "").slice(0, 10)], ["Estado", header.Estado], ["Cliente", header.Cliente], ["Código de cliente", header.CodigoCliente], ["Agente", header.Agente], ["Referencia del cliente", header.ReferenciaCliente]].forEach(function (pair) {
            if (pair[1] === null || pair[1] === undefined || pair[1] === "") return;
            var field = create("div", "gc-detail-field"); field.appendChild(create("b", "", pair[0])); field.appendChild(create("span", "", text(pair[1]))); grid.appendChild(field);
        });
        section.appendChild(grid); body.appendChild(section);
        var totals = create("div", "gc-sales-totals");
        [["Subtotal", header.Subtotal], ["Impuesto", header.Impuesto], ["Total", header.Total]].forEach(function (pair) {
            var item = create("div"); item.appendChild(create("span", "", pair[0])); item.appendChild(create("strong", "", currency(pair[1], header.Moneda))); totals.appendChild(item);
        });
        body.appendChild(totals);
        var lines = sections.Productos || [], products = create("section", "gc-modal-section");
        products.appendChild(create("h3", "", "Productos (" + integer(lines.length) + ")"));
        if (!lines.length) products.appendChild(create("p", "gc-empty", "Sin productos relacionados."));
        else {
            var wrap = create("div", "gc-modal-table"), table = create("table"), head = create("thead"), tr = create("tr");
            ["Código", "Producto", "Cantidad", "Unidad", "Precio unitario", "Importe"].forEach(function (label) { tr.appendChild(create("th", "", label)); }); head.appendChild(tr); table.appendChild(head);
            var tbody = create("tbody"); lines.forEach(function (line) {
                var row = create("tr"); [line.CodigoProducto, line.Producto, integer(line.Cantidad), line.Unidad, currency(line.PrecioUnitario, header.Moneda), currency(line.Importe, header.Moneda)].forEach(function (value) { row.appendChild(create("td", "", text(value))); }); tbody.appendChild(row);
            }); table.appendChild(tbody); wrap.appendChild(table); products.appendChild(wrap);
            if (lines.length === 100) products.appendChild(create("p", "gc-modal-more", "Se muestran los primeros 100 productos del documento."));
        }
        body.appendChild(products);
    }
    async function showDetail(row) {
        lastFocus = document.activeElement;
        var modal = byId("gc-modal"), body = byId("gc-modal-body"); modal.hidden = false; document.body.style.overflow = "hidden";
        byId("gc-modal-subtitle").textContent = row.Empresa + " · " + row.Fuente;
        byId("gc-modal-title").textContent = state.type === "ventas" ? text(row.Tipo) : text(row.Nombre) + " · " + text(row.Codigo);
        body.replaceChildren(create("p", "gc-empty", "Cargando detalle…")); modal.querySelector(".gc-modal-close").focus();
        try {
            var detailType = state.type;
            var data = await json(url("detalle", { tipo: detailType, empresa: row.Empresa, codigo: row.Codigo, documento: String(row.Tipo || "").indexOf("Nota de crédito") === 0 ? "nota" : "factura" }));
            if (modal.hidden) return; body.replaceChildren();
            if (detailType === "ventas") { renderSalesDetail(body, data.secciones || {}); return; }
            Object.keys(data.secciones || {}).forEach(function (name) {
                var records = data.secciones[name] || [], section = create("section", "gc-modal-section"); section.appendChild(create("h3", "", name + " (" + integer(records.length) + ")"));
                if (!records.length) { section.appendChild(create("p", "gc-empty", "Sin registros relacionados.")); body.appendChild(section); return; }
                if (name === "Principal") {
                    var grid = create("div", "gc-details-grid"); Object.keys(records[0]).forEach(function (key) { var field = create("div", "gc-detail-field"); field.appendChild(create("b", "", key)); field.appendChild(create("span", "", text(records[0][key]))); grid.appendChild(field); }); section.appendChild(grid);
                } else {
                    var wrap = create("div", "gc-modal-table"), table = create("table"), thead = create("thead"), hrow = create("tr"), keys = Object.keys(records[0]);
                    keys.forEach(function (key) { hrow.appendChild(create("th", "", key)); }); thead.appendChild(hrow); table.appendChild(thead);
                    var tbody = create("tbody"); records.forEach(function (record) { var r = create("tr"); keys.forEach(function (key) { r.appendChild(create("td", "", text(record[key]))); }); tbody.appendChild(r); }); table.appendChild(tbody); wrap.appendChild(table); section.appendChild(wrap);
                    if (records.length === 100) section.appendChild(create("p", "gc-modal-more", "Se muestran los primeros 100 registros relacionados."));
                }
                body.appendChild(section);
            });
        } catch (err) { if (!modal.hidden) body.replaceChildren(create("p", "gc-empty", err.message)); }
    }
    function closeDetail() { byId("gc-modal").hidden = true; document.body.style.overflow = ""; if (lastFocus && lastFocus.focus) lastFocus.focus(); }

    async function exportTable() {
        if (root.querySelector('[data-type="' + state.type + '"][data-unavailable]')) return;
        var button = byId("gc-export"); button.disabled = true; button.textContent = "Preparando Excel…";
        try {
            var response = await fetch(url("exportar", { tipo: state.type, buscar: byId("gc-search").value.trim() }), { credentials: "same-origin", cache: "no-store" });
            if (!response.ok) throw new Error((await response.text()).replace(/<[^>]+>/g, " ").trim().slice(0, 200) || "No se pudo exportar.");
            var blob = await response.blob(), link = document.createElement("a"), objectUrl = URL.createObjectURL(blob);
            link.href = objectUrl; link.download = "gerencia-comercial-" + state.type + "-" + dateInput(new Date()).replace(/-/g, "") + ".xlsx";
            document.body.appendChild(link); link.click(); link.remove(); setTimeout(function () { URL.revokeObjectURL(objectUrl); }, 1000);
        } catch (err) { alertMessage(err.message, true); }
        finally { button.disabled = false; button.innerHTML = '<i class="fa fa-file-excel-o" aria-hidden="true"></i> Exportar Excel'; }
    }

    root.querySelectorAll("[data-trend-currency]").forEach(function (button) { button.addEventListener("click", function () { if (!button.disabled && state.trendData) { state.trendCurrency = button.getAttribute("data-trend-currency"); state.trendDay = null; renderTrend(state.trendData); } }); });
    root.querySelectorAll("[data-period]").forEach(function (button) { button.addEventListener("click", function () { setPeriod(button.getAttribute("data-period")); if (button.getAttribute("data-period") !== "custom") { state.page = 1; loadSummary(); loadTable(); } }); });
    ["gc-desde", "gc-hasta"].forEach(function (id) { byId(id).addEventListener("change", function () { root.querySelectorAll("[data-period]").forEach(function (b) { b.classList.toggle("is-active", b.getAttribute("data-period") === "custom"); }); }); });
    byId("gc-filters").addEventListener("submit", function (event) { event.preventDefault(); if (byId("gc-desde").value > byId("gc-hasta").value) { alertMessage("La fecha inicial debe ser anterior a la final.", true); return; } state.page = 1; loadSummary(); loadTable(); });
    byId("gc-refresh").addEventListener("click", function () { loadSummary(true); loadTable(); });
    root.querySelectorAll("[data-type]").forEach(function (tab) { tab.addEventListener("click", function () {
        state.type = tab.getAttribute("data-type"); state.page = 1;
        root.querySelectorAll("[data-type]").forEach(function (t) { var selected = t === tab; t.classList.toggle("is-active", selected); t.setAttribute("aria-selected", selected ? "true" : "false"); });
        var unavailable = !!tab.getAttribute("data-unavailable");
        byId("gc-explorer-content").hidden = unavailable; byId("gc-unavailable").hidden = !unavailable;
        if (unavailable) { state.listRequest++; byId("gc-unavailable-title").textContent = "Sección de " + tab.getAttribute("data-unavailable") + " en desarrollo"; return; }
        configureAdvanced(); loadTable();
    }); });
    byId("gc-advanced-apply").addEventListener("click", function () { state.page = 1; loadTable(); });
    byId("gc-advanced-clear").addEventListener("click", function () { byId("gc-estado").value = ""; byId("gc-agente").value = ""; byId("gc-cliente").value = ""; state.page = 1; loadTable(); });
    var searchTimer; byId("gc-search").addEventListener("input", function () { clearTimeout(searchTimer); searchTimer = setTimeout(function () { state.page = 1; loadTable(); }, 350); });
    byId("gc-size").addEventListener("change", function () { state.size = Number(this.value); state.page = 1; loadTable(); });
    byId("gc-prev").addEventListener("click", function () { if (state.page > 1) { state.page--; loadTable(); } });
    byId("gc-next").addEventListener("click", function () { if (state.page * state.size < state.total) { state.page++; loadTable(); } });
    byId("gc-export").addEventListener("click", exportTable);
    byId("gc-modal").addEventListener("click", function (event) { if (event.target.getAttribute("data-close") === "true") closeDetail(); });
    document.addEventListener("keydown", function (event) { if (event.key === "Escape" && !byId("gc-modal").hidden) closeDetail(); });
    setPeriod("month"); configureAdvanced(); loadSummary(); loadTable();
}());
