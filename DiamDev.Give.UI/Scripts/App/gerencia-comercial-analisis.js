(function () {
    "use strict";
    var root = document.getElementById("gc");
    if (!root) return;
    var get = function (id) { return document.getElementById(id); };
    var state = { dimension: "agentes", page: 1, size: 25, total: 0, serverTotal: 0, request: 0, rows: [], previousFrom: "", previousTo: "" };
    var node = function (tag, className, value) { var n = document.createElement(tag); if (className) n.className = className; if (value !== undefined) n.textContent = value; return n; };
    var number = function (n) { return Number(n || 0).toLocaleString("es-GT", { maximumFractionDigits: 2 }); };
    var money = function (n, code) { return (code || "GTQ") + " " + Number(n || 0).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); };
    var percent = function (n) { return n === null || n === undefined ? "Nuevo / sin base" : Number(n).toLocaleString("es-GT", { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + "%"; };
    var value = function (n) { return n === null || n === undefined || n === "" ? "—" : String(n); };

    function filters(extra) {
        var p = new URLSearchParams({ dimension: state.dimension, empresa: get("gc-empresa").value, desde: get("gc-desde").value, hasta: get("gc-hasta").value, moneda: get("gc-moneda").value, buscar: get("gc-analysis-search").value.trim(), orden: get("gc-analysis-order").value, pagina: state.page, tamano: state.dimension === "agentes" ? 1000 : state.size });
        Object.keys(extra || {}).forEach(function (key) { p.set(key, extra[key]); });
        return p;
    }
    function endpoint(kind, extra) { return root.getAttribute("data-" + kind) + "?" + filters(extra).toString(); }
    async function read(kind, extra) {
        var response = await fetch(endpoint(kind, extra), { credentials: "same-origin", headers: { Accept: "application/json" }, cache: "no-store" });
        if (response.redirected && response.url.toLowerCase().indexOf("login") !== -1) { window.location.href = response.url; throw new Error("La sesión terminó."); }
        var result;
        try { result = await response.json(); } catch (_) { throw new Error("No se pudo leer la respuesta."); }
        if (!response.ok) throw new Error(result.error || "No se pudo consultar el análisis.");
        return result;
    }
    function status(message, error) { var e = get("gc-analysis-status"); e.textContent = message; e.classList.toggle("is-error", !!error); }
    function td(row, content, className) { var cell = node("td", className, content); row.appendChild(cell); return cell; }
    function changeLabel(row) {
        var label = percent(row.Variacion);
        if (row.Variacion !== null && row.Variacion !== undefined) label = (Number(row.Variacion) > 0 ? "+" : "") + label;
        return label;
    }
    function renderFronts(rows, total) {
        var box = get("gc-fronts"); box.replaceChildren();
        if (state.dimension !== "agentes") { box.hidden = true; return; }
        box.hidden = false;
        if (total > rows.length) box.appendChild(node("p", "gc-analysis-note", "Se muestran los primeros 1,000 agentes; aplique empresa o búsqueda para completar los frentes."));
        var groups = {};
        rows.forEach(function (r) {
            var key = r.Empresa + "|" + r.Moneda + "|" + r.Frente;
            if (!groups[key]) groups[key] = { Empresa: r.Empresa, Moneda: r.Moneda, Frente: r.Frente, Actual: 0, Anterior: 0, Agentes: 0 };
            groups[key].Actual += Number(r.Actual || 0); groups[key].Anterior += Number(r.Anterior || 0); groups[key].Agentes++;
        });
        Object.keys(groups).sort().forEach(function (key) {
            var g = groups[key], card = node("article", "gc-front-card");
            card.appendChild(node("small", "", g.Empresa + " · " + g.Moneda));
            card.appendChild(node("strong", "", g.Frente));
            card.appendChild(node("b", "", money(g.Actual, g.Moneda)));
            card.appendChild(node("span", "", (g.Anterior ? percent(100 * (g.Actual - g.Anterior) / Math.abs(g.Anterior)) : "Sin base anterior") + " · " + number(g.Agentes) + " agentes"));
            box.appendChild(card);
        });
    }
    function render(data) {
        var rows = data.filas || [], front = get("gc-analysis-front").value;
        var selected = state.dimension === "agentes" && front ? rows.filter(function (r) { return r.Frente === front; }) : rows;
        var display = state.dimension === "agentes" ? selected.slice((state.page - 1) * state.size, state.page * state.size) : rows;
        state.serverTotal = Number(data.total || 0); state.total = state.dimension === "agentes" ? selected.length : state.serverTotal; state.rows = rows;
        state.previousFrom = data.anteriorDesde || state.previousFrom; state.previousTo = data.anteriorHasta || state.previousTo;
        var body = get("gc-analysis-rows"); body.replaceChildren();
        renderFronts(rows, state.serverTotal);
        if (!display.length) { var empty = node("tr"); var cell = td(empty, "No hay movimientos para estos filtros.", "gc-empty"); cell.colSpan = 10; body.appendChild(empty); }
        display.forEach(function (r) {
            var tr = node("tr");
            td(tr, r.Empresa); var name = td(tr, value(r.Nombre)); name.insertBefore(node("small", "gc-analysis-code", value(r.Codigo) + " · "), name.firstChild);
            td(tr, state.dimension === "agentes" ? value(r.Frente) : state.dimension === "productos" ? value(r.Familia) : "Cliente");
            td(tr, r.Moneda); td(tr, money(r.Anterior, r.Moneda)); td(tr, money(r.Actual, r.Moneda));
            td(tr, changeLabel(r), Number(r.Delta) < 0 ? "gc-negative" : "gc-positive");
            td(tr, percent(r.Participacion)); td(tr, state.dimension === "productos" ? number(r.CantidadActual) : "—");
            var action = td(tr, ""); var button = node("button", "gc-detail-btn", "Ver histórico"); button.type = "button"; button.addEventListener("click", function () { showHistory(r); }); action.appendChild(button); body.appendChild(tr);
        });
        var first = state.total ? (state.page - 1) * state.size + 1 : 0, last = Math.min(state.total, state.page * state.size);
        get("gc-analysis-count").textContent = number(first) + "–" + number(last) + " de " + number(state.total);
        get("gc-analysis-page").textContent = String(state.page);
        get("gc-analysis-prev").disabled = state.page <= 1; get("gc-analysis-next").disabled = last >= state.total;
        status("Comparación: " + state.previousFrom + " a " + state.previousTo + " vs. " + get("gc-desde").value + " a " + get("gc-hasta").value + (data.advertencia ? " · " + data.advertencia : ""), false);
    }
    async function load() {
        var request = ++state.request;
        status("Consultando SAP HANA…", false);
        try { var data = await read("comparativo"); if (request === state.request) render(data); }
        catch (error) { if (request === state.request) { get("gc-analysis-rows").replaceChildren(); get("gc-fronts").replaceChildren(); status(error.message, true); } }
    }
    function historyClose() { get("gc-history-modal").hidden = true; document.body.style.overflow = ""; }
    async function showHistory(row) {
        var modal = get("gc-history-modal"), body = get("gc-history-body");
        get("gc-history-title").textContent = row.Empresa + " · " + value(row.Nombre) + " · " + value(row.Codigo);
        modal.hidden = false; document.body.style.overflow = "hidden"; body.replaceChildren(node("p", "gc-empty", "Cargando histórico…"));
        try {
            var data = await read("historico", { dimension: state.dimension, empresa: row.Empresa, codigo: row.Codigo, hasta: get("gc-hasta").value, moneda: row.Moneda });
            if (modal.hidden) return;
            body.replaceChildren();
            var rows = data.filas || [], byMonth = {};
            rows.forEach(function (r) { byMonth[r.Mes] = Number(r.Neto || 0); });
            var recent = rows.slice(-12), max = Math.max.apply(null, recent.map(function (r) { return Math.abs(Number(r.Neto || 0)); }).concat([1]));
            var chart = node("div", "gc-history-chart");
            recent.forEach(function (r) { var col = node("div", "gc-history-bar-col"), bar = node("span", "gc-history-bar"); bar.style.height = Math.max(2, 100 * Math.abs(Number(r.Neto || 0)) / max) + "%"; bar.title = r.Mes + " · " + money(r.Neto, r.Moneda); col.appendChild(bar); col.appendChild(node("small", "", String(r.Mes).slice(5))); chart.appendChild(col); });
            body.appendChild(node("p", "gc-analysis-note", "Ventas netas mensuales; las notas de crédito se descuentan. Las unidades solo se muestran para productos."));
            if (recent.length) body.appendChild(chart);
            var wrap = node("div", "gc-modal-table"), table = node("table"), head = node("thead"), hr = node("tr");
            ["Mes", "Venta neta", "Variación anual", "Unidades netas"].forEach(function (v) { hr.appendChild(node("th", "", v)); }); head.appendChild(hr); table.appendChild(head);
            var tbody = node("tbody");
            rows.slice().reverse().forEach(function (r) { var tr = node("tr"), prev = byMonth[String(Number(String(r.Mes).slice(0, 4)) - 1) + String(r.Mes).slice(4)], yoy = prev ? 100 * (Number(r.Neto || 0) - prev) / Math.abs(prev) : null;
                [r.Mes, money(r.Neto, r.Moneda), percent(yoy), state.dimension === "productos" ? number(r.Cantidad) : "—"].forEach(function (v) { tr.appendChild(node("td", "", v)); }); tbody.appendChild(tr);
            }); table.appendChild(tbody); wrap.appendChild(table); body.appendChild(wrap);
            if (!rows.length) body.replaceChildren(node("p", "gc-empty", "No hay movimientos en los últimos cinco años."));
        } catch (error) { if (!modal.hidden) body.replaceChildren(node("p", "gc-empty", error.message)); }
    }
    function reportTable(title, rows, mode, dimension) {
        var section = node("section", "gc-report-section"); section.appendChild(node("h2", "", title));
        var table = node("table"), head = node("thead"), tr = node("tr");
        var headings = [dimension === "agentes" ? "Agente / frente" : dimension === "clientes" ? "Cliente" : "Producto / familia", mode === "money" ? "Base anterior" : "Tendencia", mode === "money" ? "Resultado actual" : "Participación", "Variación"];
        if (dimension === "productos") headings.push("Unidades netas");
        headings.forEach(function (v) { tr.appendChild(node("th", "", v)); }); head.appendChild(tr); table.appendChild(head);
        var body = node("tbody"); rows.forEach(function (r) { var row = node("tr"), label = value(r.Nombre) + " · " + value(r.Codigo) + (dimension === "agentes" ? " · " + value(r.Frente) : dimension === "productos" ? " · " + value(r.Familia) : "");
            var cells = [label, mode === "money" ? money(r.Anterior, r.Moneda) : (Number(r.Delta) >= 0 ? "Crecimiento" : "Caída"), mode === "money" ? money(r.Actual, r.Moneda) : percent(r.Participacion), changeLabel(r)];
            if (dimension === "productos") cells.push(number(r.CantidadActual));
            cells.forEach(function (v) { row.appendChild(node("td", "", v)); }); body.appendChild(row);
        }); table.appendChild(body); section.appendChild(table); return section;
    }
    async function report(mode) {
        var company = get("gc-empresa").value, code = get("gc-moneda").value;
        if (!company || !code) { status("Seleccione una empresa y una moneda para generar el informe ejecutivo.", true); get("gc-empresa").focus(); return; }
        var button = get(mode === "money" ? "gc-report-money" : "gc-report-percent"); button.disabled = true; status("Preparando informe ejecutivo…", false);
        try {
            var requests = [
                read("comparativo", { dimension: "agentes", buscar: "", orden: "actual", pagina: 1, tamano: 1000 }),
                read("comparativo", { dimension: "clientes", buscar: "", orden: "delta", pagina: 1, tamano: 8 }),
                read("comparativo", { dimension: "clientes", buscar: "", orden: "caida", pagina: 1, tamano: 8 }),
                read("comparativo", { dimension: "productos", buscar: "", orden: "delta", pagina: 1, tamano: 8 }),
                read("comparativo", { dimension: "productos", buscar: "", orden: "caida", pagina: 1, tamano: 8 })
            ];
            var all = await Promise.all(requests), agents = all[0].filas || [];
            if (all[0].total > agents.length) throw new Error("El informe supera 1,000 agentes. Reduzca los filtros.");
            var current = agents.reduce(function (a, r) { return a + Number(r.Actual || 0); }, 0), previous = agents.reduce(function (a, r) { return a + Number(r.Anterior || 0); }, 0);
            var reportBox = get("gc-report"); reportBox.replaceChildren();
            var header = node("header", "gc-report-header"); header.appendChild(node("small", "", "GRUPO K66 · GERENCIA COMERCIAL")); header.appendChild(node("h1", "", company + " · Informe ejecutivo de ventas"));
            header.appendChild(node("p", "", get("gc-desde").value + " al " + get("gc-hasta").value + " · Comparado con " + all[0].anteriorDesde + " al " + all[0].anteriorHasta + " · " + code));
            header.appendChild(node("p", "", mode === "money" ? "Versión monetaria · ventas netas sin impuesto" : "Versión porcentual · sin importes monetarios")); reportBox.appendChild(header);
            var summary = node("div", "gc-report-summary");
            if (mode === "money") { summary.appendChild(node("strong", "", "Base anterior: " + money(previous, code))); summary.appendChild(node("strong", "", "Resultado actual: " + money(current, code))); summary.appendChild(node("strong", "", "Cambio absoluto: " + money(current - previous, code))); }
            summary.appendChild(node("strong", "", "Variación: " + percent(previous ? 100 * (current - previous) / Math.abs(previous) : null)));
            summary.appendChild(node("strong", "", "Agentes con movimiento: " + number(agents.length))); reportBox.appendChild(summary);
            var notes = node("section", "gc-report-section"), list = node("ul"); notes.appendChild(node("h2", "", "Lectura ejecutiva y seguimiento"));
            list.appendChild(node("li", "", "El resultado " + (current >= previous ? "creció " : "disminuyó ") + percent(previous ? Math.abs(100 * (current - previous) / previous) : null) + " frente al mismo período del año anterior."));
            var leader = agents.slice().sort(function (a, b) { return Number(b.Actual || 0) - Number(a.Actual || 0); })[0];
            if (leader) list.appendChild(node("li", "", "Mayor participación: " + value(leader.Nombre) + " (" + value(leader.Frente) + "), " + percent(leader.Participacion) + " del resultado de " + company + "."));
            [["Cliente con mayor avance", all[1].filas], ["Cliente para seguimiento", all[2].filas], ["Producto con mayor avance", all[3].filas], ["Producto para seguimiento", all[4].filas]].forEach(function (pair, index) {
                var item = (pair[1] || [])[0]; if (!item || (index % 2 === 0 && Number(item.Delta) <= 0) || (index % 2 === 1 && Number(item.Delta) >= 0)) return;
                list.appendChild(node("li", "", pair[0] + ": " + value(item.Nombre) + " (" + value(item.Codigo) + "), variación " + changeLabel(item) + (mode === "money" ? ", cambio " + money(item.Delta, code) : "") + "."));
            });
            notes.appendChild(list); reportBox.appendChild(notes);
            reportBox.appendChild(reportTable("Resultado por agente y frente", agents, mode, "agentes"));
            reportBox.appendChild(reportTable("Clientes con mayor crecimiento", all[1].filas || [], mode, "clientes"));
            reportBox.appendChild(reportTable("Clientes con mayor caída", all[2].filas || [], mode, "clientes"));
            reportBox.appendChild(reportTable("Productos con mayor crecimiento", all[3].filas || [], mode, "productos"));
            reportBox.appendChild(reportTable("Productos con mayor caída", all[4].filas || [], mode, "productos"));
            reportBox.appendChild(node("footer", "", "Fuente: SAP HANA · Facturas menos notas de crédito · Fecha de documento · " + new Date().toLocaleString("es-GT") + ". GTQ y USD se analizan por separado. Productos: importes de líneas sin impuesto; pueden diferir del encabezado por ajustes."));
            reportBox.hidden = false; status("Informe listo. Use el diálogo para imprimirlo o guardarlo como PDF.", false);
            window.print();
        } catch (error) { status(error.message, true); }
        finally { button.disabled = false; }
    }
    async function exportAnalysis() {
        var button = get("gc-analysis-export"); button.disabled = true;
        try {
            var response = await fetch(endpoint("exportar-comparativo"), { credentials: "same-origin", cache: "no-store" });
            if (!response.ok) throw new Error("No se pudo exportar el comparativo. Aplique filtros si supera 10,000 filas.");
            var blob = await response.blob(), url = URL.createObjectURL(blob), link = node("a"); link.href = url; link.download = "comparativo-" + state.dimension + "-" + get("gc-hasta").value + ".xlsx"; document.body.appendChild(link); link.click(); link.remove(); setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
        } catch (error) { status(error.message, true); } finally { button.disabled = false; }
    }
    root.querySelectorAll("[data-analysis]").forEach(function (button) { button.addEventListener("click", function () { state.dimension = button.getAttribute("data-analysis"); state.page = 1; get("gc-analysis-front-label").hidden = state.dimension !== "agentes"; root.querySelectorAll("[data-analysis]").forEach(function (tab) { tab.classList.toggle("is-active", tab === button); tab.setAttribute("aria-selected", tab === button ? "true" : "false"); }); get("gc-analysis-order").value = state.dimension === "productos" ? "cantidad" : "actual"; load(); }); });
    get("gc-filters").addEventListener("submit", function () { state.page = 1; load(); });
    root.querySelectorAll("[data-period]").forEach(function (button) { button.addEventListener("click", function () { if (button.getAttribute("data-period") !== "custom") { state.page = 1; setTimeout(load, 0); } }); });
    get("gc-refresh").addEventListener("click", load);
    var timer; get("gc-analysis-search").addEventListener("input", function () { clearTimeout(timer); timer = setTimeout(function () { state.page = 1; load(); }, 350); });
    get("gc-analysis-order").addEventListener("change", function () { state.page = 1; load(); });
    get("gc-analysis-front").addEventListener("change", function () { state.page = 1; if (state.dimension === "agentes") render({ filas: state.rows, total: state.serverTotal }); });
    get("gc-analysis-size").addEventListener("change", function () { state.size = Number(this.value); state.page = 1; load(); });
    get("gc-analysis-prev").addEventListener("click", function () { if (state.page > 1) { state.page--; if (state.dimension === "agentes") render({ filas: state.rows, total: state.serverTotal }); else load(); } });
    get("gc-analysis-next").addEventListener("click", function () { if (state.page * state.size < state.total) { state.page++; if (state.dimension === "agentes") render({ filas: state.rows, total: state.serverTotal }); else load(); } });
    get("gc-analysis-export").addEventListener("click", exportAnalysis);
    get("gc-report-money").addEventListener("click", function () { report("money"); });
    get("gc-report-percent").addEventListener("click", function () { report("percent"); });
    get("gc-report-closed").addEventListener("click", function () {
        var now = new Date(), start = new Date(now.getFullYear(), now.getMonth() - 1, 1), end = new Date(now.getFullYear(), now.getMonth(), 0);
        function date(d) { return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0"); }
        get("gc-desde").value = date(start); get("gc-hasta").value = date(end);
        get("gc-filters").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        report("money");
    });
    get("gc-history-modal").addEventListener("click", function (event) { if (event.target.getAttribute("data-history-close") === "true") historyClose(); });
    document.addEventListener("keydown", function (event) { if (event.key === "Escape" && !get("gc-history-modal").hidden) historyClose(); });
    load();
}());
