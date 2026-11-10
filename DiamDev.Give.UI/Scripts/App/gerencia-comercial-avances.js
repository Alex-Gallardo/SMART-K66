(function () {
    "use strict";
    var root = document.getElementById("gc");
    if (!root) return;
    var get = function (id) { return document.getElementById(id); };
    var today = new Date();
    var serial = 0;
    var state = { periods: [], data: null, mode: "bars", request: 0 };
    var palette = ["#176fa5", "#e74b59", "#19917e", "#8c69c4", "#d28726", "#3a79b9", "#b04a83", "#649b35", "#bd683d", "#4e70a6", "#cc4275", "#417c71"];
    function el(tag, className, content) { var e = document.createElement(tag); if (className) e.className = className; if (content !== undefined) e.textContent = content; return e; }
    function svg(tag, attributes, title) { var e = document.createElementNS("http://www.w3.org/2000/svg", tag); Object.keys(attributes || {}).forEach(function (key) { e.setAttribute(key, attributes[key]); }); if (title) { if (tag === "text") e.textContent = title; else { var t = document.createElementNS("http://www.w3.org/2000/svg", "title"); t.textContent = title; e.appendChild(t); } } return e; }
    function date(d) { return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0"); }
    function readable(d) { return d.slice(8, 10) + "/" + d.slice(5, 7) + "/" + d.slice(0, 4); }
    function money(n) { return get("gc-advance-currency").value + " " + Number(n || 0).toLocaleString("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function compact(n) { var a = Math.abs(Number(n || 0)); return (n < 0 ? "−" : "") + (a >= 1000000 ? (a / 1000000).toFixed(1) + " M" : a >= 1000 ? (a / 1000).toFixed(0) + " mil" : a.toFixed(0)); }
    function integer(n) { return Number(n || 0).toLocaleString("es-GT", { maximumFractionDigits: 1 }); }
    function pct(current, base) { return Number(base) === 0 ? null : 100 * (Number(current) - Number(base)) / Math.abs(Number(base)); }
    function percent(n) { return n === null || !isFinite(n) ? "Nuevo / sin base" : (n > 0 ? "+" : "") + Number(n).toLocaleString("es-GT", { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + "%"; }
    function days(p) { return Math.round((new Date(p.hasta + "T00:00:00") - new Date(p.desde + "T00:00:00")) / 86400000) + 1; }
    function cutoff(year) { var month = today.getMonth(), day = today.getDate(); return date(new Date(year, month, Math.min(day, new Date(year, month + 1, 0).getDate()))); }
    function setStatus(message, error) { var e = get("gc-advance-status"); e.textContent = message; e.classList.toggle("is-error", !!error); }
    function markStale() { state.request++; state.data = null; clearResults(); setStatus("Períodos modificados. Pulse «Comparar períodos» para actualizar los resultados.", false); }

    function addPeriod(desde, hasta, label) {
        if (state.periods.length >= 12) { setStatus("Se pueden comparar hasta 12 períodos a la vez.", true); return; }
        if (!desde || !hasta || desde > hasta || hasta > date(today)) { setStatus("Revise las fechas del período; no se admiten fechas futuras.", true); return; }
        if (state.periods.some(function (p) { return p.desde === desde && p.hasta === hasta; })) { setStatus("Ese período ya está incluido.", true); return; }
        state.periods.push({ id: ++serial, desde: desde, hasta: hasta, label: label || readable(desde) + " – " + readable(hasta) });
        state.periods.sort(function (a, b) { return a.desde.localeCompare(b.desde) || a.hasta.localeCompare(b.hasta); });
        renderPeriods(); markStale();
    }
    function inputDate(value, period, key) {
        var input = el("input"); input.type = "date"; input.value = value; input.required = true;
        input.addEventListener("change", function () { period[key] = input.value; if (period.desde && period.hasta) period.label = readable(period.desde) + " – " + readable(period.hasta); renderPeriods(); markStale(); });
        return input;
    }
    function renderPeriods() {
        var box = get("gc-advance-periods"); box.replaceChildren();
        state.periods.forEach(function (p, index) {
            var row = el("div", "gc-advance-period");
            var color = el("span", "gc-advance-swatch"); color.style.backgroundColor = palette[index]; row.appendChild(color);
            var title = el("input", "gc-advance-label"); title.type = "text"; title.maxLength = 40; title.value = p.label; title.setAttribute("aria-label", "Nombre del período " + (index + 1)); title.addEventListener("input", function () { p.label = title.value; if (state.data) render(); }); row.appendChild(title);
            var from = el("label", "", "Desde"); from.appendChild(inputDate(p.desde, p, "desde")); row.appendChild(from);
            var to = el("label", "", "Hasta"); to.appendChild(inputDate(p.hasta, p, "hasta")); row.appendChild(to);
            var actions = el("div", "gc-advance-period-actions");
            [["↑", "Subir período", -1], ["↓", "Bajar período", 1]].forEach(function (spec) { var button = el("button", "", spec[0]); button.type = "button"; button.disabled = index + spec[2] < 0 || index + spec[2] >= state.periods.length; button.setAttribute("aria-label", spec[1] + " " + (index + 1)); button.addEventListener("click", function () { var other = index + spec[2], moved = state.periods.splice(index, 1)[0]; state.periods.splice(other, 0, moved); renderPeriods(); markStale(); }); actions.appendChild(button); });
            var remove = el("button", "gc-advance-remove", "×"); remove.type = "button"; remove.setAttribute("aria-label", "Quitar período " + (index + 1)); remove.addEventListener("click", function () { state.periods.splice(index, 1); renderPeriods(); markStale(); }); actions.appendChild(remove); row.appendChild(actions); box.appendChild(row);
        });
    }
    function validate() {
        if (state.periods.length < 2 || state.periods.length > 12) throw new Error("Seleccione entre 2 y 12 períodos.");
        var seen = {};
        state.periods.forEach(function (p) {
            if (!/^\d{4}-\d{2}-\d{2}$/.test(p.desde) || !/^\d{4}-\d{2}-\d{2}$/.test(p.hasta) || p.desde > p.hasta || p.hasta > date(today) || days(p) > 366) throw new Error("Cada período debe tener fechas válidas y durar como máximo un año.");
            var key = p.desde + "," + p.hasta; if (seen[key]) throw new Error("Hay períodos duplicados."); seen[key] = true;
        });
    }
    async function load() {
        try { validate(); } catch (error) { setStatus(error.message, true); return; }
        var request = ++state.request;
        state.data = null;
        clearResults();
        setStatus("Consultando ventas y unidades de SAP HANA…", false);
        var query = new URLSearchParams({ empresa: get("gc-advance-company").value, moneda: get("gc-advance-currency").value, rangos: state.periods.map(function (p) { return p.desde + "," + p.hasta; }).join("|") });
        try {
            var response = await fetch(root.getAttribute("data-avances") + "?" + query.toString(), { credentials: "same-origin", headers: { Accept: "application/json" }, cache: "no-store" });
            if (response.redirected && response.url.toLowerCase().indexOf("login") !== -1) { window.location.href = response.url; throw new Error("La sesión terminó."); }
            var data; try { data = await response.json(); } catch (_) { throw new Error("No se pudo leer la respuesta del servidor."); }
            if (!response.ok) throw new Error(data.error || "No se pudo consultar SAP HANA.");
            if (request !== state.request) return;
            state.data = data; render();
            setStatus("Datos consultados: " + data.actualizado + (data.advertencia ? " · " + data.advertencia : ""), false);
        } catch (error) { if (request === state.request) { state.data = null; clearResults(); setStatus(error.message, true); } }
    }
    async function exportExcel() {
        try { validate(); } catch (error) { setStatus(error.message, true); return; }
        var button = get("gc-advance-export"); button.disabled = true;
        var query = new URLSearchParams({ empresa: get("gc-advance-company").value, moneda: get("gc-advance-currency").value, rangos: state.periods.map(function (p) { return p.desde + "," + p.hasta; }).join("|") });
        try {
            var response = await fetch(root.getAttribute("data-exportar-avances") + "?" + query.toString(), { credentials: "same-origin", cache: "no-store" });
            if (response.redirected && response.url.toLowerCase().indexOf("login") !== -1) { window.location.href = response.url; throw new Error("La sesión terminó."); }
            if (!response.ok) throw new Error("No se pudo exportar el comparativo.");
            var blob = await response.blob(), objectUrl = URL.createObjectURL(blob), link = el("a");
            link.href = objectUrl; link.download = "avances-" + get("gc-advance-company").value.toLowerCase() + ".xlsx";
            document.body.appendChild(link); link.click(); link.remove(); setTimeout(function () { URL.revokeObjectURL(objectUrl); }, 1000);
        } catch (error) { setStatus(error.message, true); }
        finally { button.disabled = false; }
    }
    function exportPdf(inline) {
        try { validate(); } catch (error) { setStatus(error.message, true); return; }
        var query = new URLSearchParams({ empresa: get("gc-advance-company").value, moneda: get("gc-advance-currency").value, rangos: state.periods.map(function (p) { return p.desde + "," + p.hasta; }).join("|"), inline: inline ? "true" : "false" });
        var address = root.getAttribute("data-pdf-avances") + "?" + query.toString();
        setStatus(inline ? "Abriendo el PDF para imprimir…" : "Descargando el informe PDF…", false);
        if (inline) window.open(address, "_blank");
        else window.location.href = address;
    }
    function model() {
        var n = state.periods.length, agents = {}, total = Array(n).fill(0);
        (state.data.ventas || []).forEach(function (r) {
            var code = String(r.Codigo), index = Number(r.Periodo);
            if (!agents[code]) agents[code] = { code: code, name: r.Nombre || "Sin agente", front: r.Frente || "Agente comercial", active: r.Activo, values: Array(n).fill(0), invoices: Array(n).fill(0), credits: Array(n).fill(0) };
            agents[code].values[index] = Number(r.Neto || 0); agents[code].invoices[index] = Number(r.Facturas || 0); agents[code].credits[index] = Number(r.NotasCredito || 0);
            total[index] += Number(r.Neto || 0);
        });
        var list = Object.keys(agents).map(function (key) { return agents[key]; });
        list.sort(function (a, b) { return b.values[n - 1] - a.values[n - 1] || a.name.localeCompare(b.name, "es"); });
        return { agents: list, totals: total };
    }
    function filtered(agents) {
        var search = get("gc-advance-search").value.trim().toLocaleLowerCase("es"), front = get("gc-advance-front").value;
        return agents.filter(function (a) { return (!front || a.front === front) && (!search || (a.name + " " + a.code).toLocaleLowerCase("es").indexOf(search) >= 0); });
    }
    function clearResults() {
        ["gc-advance-chart", "gc-advance-kpis", "gc-advance-units", "gc-advance-reading", "gc-advance-alerts", "gc-advance-head", "gc-advance-rows", "gc-advance-legend"].forEach(function (id) { get(id).replaceChildren(); });
        get("gc-advance-table-note").textContent = "";
    }
    function render() {
        if (!state.data) return;
        var data = model(), agents = filtered(data.agents);
        renderChart(agents); renderKpis(data); renderInsights(data); renderTable(agents);
    }
    function renderLegend() {
        var legend = get("gc-advance-legend"); legend.replaceChildren();
        state.periods.forEach(function (p, i) { var item = el("span"), dot = el("i"); dot.style.backgroundColor = palette[i]; item.appendChild(dot); item.appendChild(el("b", "", p.label || "Período " + (i + 1))); item.appendChild(el("small", "", readable(p.desde) + " al " + readable(p.hasta))); legend.appendChild(item); });
    }
    function renderChart(allAgents) {
        renderLegend();
        var target = get("gc-advance-chart"); target.replaceChildren();
        var shown = get("gc-advance-count").value === "all" ? allAgents : allAgents.slice(0, 12);
        get("gc-advance-chart-wrap").classList.toggle("is-scrollable", get("gc-advance-count").value === "all");
        if (!shown.length) { target.appendChild(el("p", "gc-empty", "No hay agentes para estos filtros.")); return; }
        var values = shown.reduce(function (a, r) { return a.concat(r.values); }, []), low = Math.min.apply(null, [0].concat(values)), high = Math.max.apply(null, [0].concat(values));
        if (low === high) high = low + 1;
        var left = 250, right = 105, width = 1200, plot = width - left - right, rowHeight = state.mode === "bars" ? Math.max(70, 24 + state.periods.length * 9) : 76, top = 48, height = top + shown.length * rowHeight + 40;
        var x = function (v) { return left + (Number(v) - low) * plot / (high - low); };
        var chart = svg("svg", { viewBox: "0 0 " + width + " " + height, width: width, height: height, role: "img", "aria-label": "Comparativa de ventas netas de " + shown.length + " agentes en " + state.periods.length + " períodos" });
        shown.forEach(function (_, row) { if (row % 2 === 0) chart.appendChild(svg("rect", { x: 0, y: top + row * rowHeight, width: width, height: rowHeight, fill: "#f7fafc" })); chart.appendChild(svg("line", { x1: 0, x2: width, y1: top + (row + 1) * rowHeight, y2: top + (row + 1) * rowHeight, stroke: "#edf2f6" })); });
        for (var tick = 0; tick <= 5; tick++) { var val = low + (high - low) * tick / 5, tx = x(val); chart.appendChild(svg("line", { x1: tx, x2: tx, y1: top - 8, y2: height - 31, stroke: "#e1eaf0", "stroke-width": 1 })); chart.appendChild(svg("text", { x: tx, y: 25, "text-anchor": tick === 0 ? "start" : tick === 5 ? "end" : "middle", class: "gc-advance-axis" }, compact(val))); }
        chart.appendChild(svg("text", { x: width - 3, y: height - 11, "text-anchor": "end", class: "gc-advance-axis" }, get("gc-advance-currency").value + " · venta neta"));
        chart.appendChild(svg("line", { x1: x(0), x2: x(0), y1: top - 8, y2: height - 31, stroke: "#7c91a4", "stroke-width": 1.5 }));
        shown.forEach(function (agent, row) {
            var cy = top + row * rowHeight + rowHeight / 2;
            chart.appendChild(svg("text", { x: left - 13, y: cy + 4, "text-anchor": "end", class: "gc-advance-agent" }, agent.name.length > 30 ? agent.name.slice(0, 29) + "…" : agent.name));
            if (state.mode === "bars") {
                var barHeight = Math.min(15, (rowHeight - 18) / state.periods.length - 1.5), group = state.periods.length * (barHeight + 1.5), start = cy - group / 2;
                agent.values.forEach(function (v, i) { var zero = x(0), end = x(v), y = start + i * (barHeight + 1.5); chart.appendChild(svg("rect", { x: Math.min(zero, end), y: y, width: Math.max(1.5, Math.abs(end - zero)), height: barHeight, rx: 3, fill: palette[i] }, agent.name + " · " + state.periods[i].label + " · " + money(v))); if (state.periods.length <= 4 && v !== 0) chart.appendChild(svg("text", { x: end + (v > 0 ? 7 : -7), y: y + barHeight - 2, "text-anchor": v > 0 ? "start" : "end", class: "gc-advance-bar-label", fill: palette[i] }, compact(v))); });
            } else {
                var spread = Math.min(5, 18 / Math.max(1, state.periods.length - 1)), points = agent.values.map(function (v, i) { return x(v).toFixed(1) + "," + (cy + (i - (state.periods.length - 1) / 2) * spread).toFixed(1); }).join(" ");
                chart.appendChild(svg("polyline", { points: points, fill: "none", stroke: "#64788b", "stroke-width": 2, "stroke-linejoin": "round", "stroke-linecap": "round" }, agent.name + " · trayectoria de ventas"));
                agent.values.forEach(function (v, i) { chart.appendChild(svg("circle", { cx: x(v), cy: cy + (i - (state.periods.length - 1) / 2) * spread, r: 5, fill: palette[i], stroke: "white", "stroke-width": 1.5 }, agent.name + " · " + state.periods[i].label + " · " + money(v))); if (i === agent.values.length - 1) chart.appendChild(svg("text", { x: x(v) + (v >= 0 ? 8 : -8), y: cy + (i - (state.periods.length - 1) / 2) * spread + 4, "text-anchor": v >= 0 ? "start" : "end", class: "gc-advance-bar-label", fill: palette[i] }, compact(v))); });
            }
        });
        target.appendChild(chart);
        get("gc-advance-chart").setAttribute("aria-label", "Comparativa " + (state.mode === "bars" ? "en barras" : "en líneas") + " de " + shown.length + " agentes; valores detallados en el panorama por agente.");
    }
    function card(title, figure, help, tone) { var c = el("div", "gc-advance-kpi " + (tone || "")); c.appendChild(el("small", "", title)); c.appendChild(el("strong", "", figure)); c.appendChild(el("span", "", help)); return c; }
    function kpiGroup(target, title) { var group = el("section", "gc-advance-kpi-group"), body = el("div", "gc-advance-kpi-group-body"); group.appendChild(el("h5", "", title)); group.appendChild(body); target.appendChild(group); return body; }
    function renderKpis(data) {
        var target = get("gc-advance-kpis"), units = get("gc-advance-units"); target.replaceChildren(); units.replaceChildren();
        var periods = kpiGroup(target, "Ventas netas por período"), changes = kpiGroup(target, "Crecimiento y variación");
        state.periods.forEach(function (p, i) { periods.appendChild(card(p.label || "Período " + (i + 1), money(data.totals[i]), readable(p.desde) + " al " + readable(p.hasta), "period")); });
        var last = data.totals.length - 1, before = last - 1, delta = data.totals[last] - data.totals[before];
        changes.appendChild(card("Variación · último vs. anterior", percent(pct(data.totals[last], data.totals[before])), state.periods[before].label + " → " + state.periods[last].label, delta >= 0 ? "up" : "down"));
        changes.appendChild(card("Cambio absoluto", (delta > 0 ? "+" : "") + money(delta), "Últimos dos períodos", delta >= 0 ? "up" : "down"));
        if (last > 1) changes.appendChild(card("Último vs. período base", percent(pct(data.totals[last], data.totals[0])), state.periods[0].label + " → " + state.periods[last].label, "base"));
        units.appendChild(el("div", "gc-advance-units-heading", "Volumen · separado por unidad de medida"));
        var unitGroups = {};
        (state.data.unidades || []).forEach(function (r) { var unit = r.Unidad || "Sin unidad", i = Number(r.Periodo); if (!unitGroups[unit]) unitGroups[unit] = Array(state.periods.length).fill(0); unitGroups[unit][i] += Number(r.Cantidad || 0); });
        var names = Object.keys(unitGroups).sort();
        if (names.length === 1) { var u = names[0], vals = unitGroups[u]; units.appendChild(el("strong", "", "Unidades netas (" + u + "): " + state.periods.map(function (p, i) { return p.label + " " + integer(vals[i]); }).join(" · "))); units.appendChild(el("span", "", "Variación de unidades: " + percent(pct(vals[last], vals[before])))); }
        else if (names.length > 1) { units.appendChild(el("strong", "", "Unidades por medida")); units.appendChild(el("span", "", names.slice(0, 5).map(function (u) { return u + ": " + integer(unitGroups[u][last]); }).join(" · ") + (names.length > 5 ? " · y " + (names.length - 5) + " más" : ""))); units.appendChild(el("small", "", "Se mantienen separadas para evitar sumar medidas distintas.")); }
        else units.appendChild(el("span", "", "Sin cantidades de productos en estos períodos."));
    }
    function copy(target, messages) {
        var box = get(target); box.replaceChildren();
        messages.forEach(function (item) {
            var finding = el("div", "gc-advance-finding " + (item.tone || "")), icon = el("span", "gc-advance-finding-icon"), content = el("div");
            icon.appendChild(el("i", "fa " + (item.icon || "fa-circle-o"))); finding.appendChild(icon);
            content.appendChild(el("strong", "", item.title)); content.appendChild(el("p", "", item.text)); finding.appendChild(content); box.appendChild(finding);
        });
    }
    function renderInsights(data) {
        var n = state.periods.length, previous = n - 2, current = n - 1, a = data.totals[previous], b = data.totals[current], change = b - a;
        var moved = data.agents.filter(function (agent) { return agent.values[previous] !== 0 || agent.values[current] !== 0; });
        var grown = moved.filter(function (agent) { return agent.values[current] > agent.values[previous]; });
        var fallen = moved.filter(function (agent) { return agent.values[current] < agent.values[previous]; });
        var ranked = moved.slice().sort(function (x, y) { return y.values[current] - x.values[current]; });
        var top3 = ranked.slice(0, 3).reduce(function (acc, agent) { return acc + agent.values[current]; }, 0), share = b > 0 ? 100 * top3 / b : null;
        var growth = pct(b, a);
        var firstReading = growth === null ? "El período anterior no tiene base de ventas; el último registra " + money(b) + "." : "Entre «" + state.periods[previous].label + "» y «" + state.periods[current].label + "», las ventas netas " + (change > 0 ? "crecieron " : change < 0 ? "disminuyeron " : "se mantuvieron en ") + percent(Math.abs(growth)).replace(/^\+/, "") + " (" + (change > 0 ? "+" : "") + money(change) + ").";
        var readings = [firstReading, grown.length + " de " + moved.length + " agentes con movimiento crecieron; " + fallen.length + " retrocedieron."];
        if (share !== null) readings.push("Los tres agentes de mayor venta concentran " + percent(share).replace(/^\+/, "") + " del último período.");
        var drivers = moved.slice().sort(function (x, y) { return (y.values[current] - y.values[previous]) - (x.values[current] - x.values[previous]); });
        if (drivers.length && drivers[0].values[current] > drivers[0].values[previous]) readings.push("Mayor impulso: " + drivers[0].name + " (" + money(drivers[0].values[current] - drivers[0].values[previous]) + ").");
        if (days(state.periods[previous]) !== days(state.periods[current])) readings.push("Los dos últimos períodos tienen distinta duración (" + days(state.periods[previous]) + " y " + days(state.periods[current]) + " días). La variación requiere considerar esa diferencia.");
        copy("gc-advance-reading", readings.map(function (message, index) {
            var title = index === 0 ? "Resultado general" : index === 1 ? "Cobertura comercial" : message.indexOf("concentran") >= 0 ? "Concentración" : message.indexOf("Mayor impulso") >= 0 ? "Impulso principal" : "Comparabilidad";
            return { title: title, text: message, tone: index === 0 ? change >= 0 ? "positive" : "critical" : title === "Comparabilidad" ? "warning" : "", icon: index === 0 ? "fa-line-chart" : index === 1 ? "fa-users" : "fa-info-circle" };
        }));
        var alerts = [];
        if (drivers.length && drivers[0].values[current] > drivers[0].values[previous]) alerts.push("Mayor alza absoluta: " + drivers[0].name + " (" + money(drivers[0].values[current] - drivers[0].values[previous]) + "). Identificar cuentas y productos que impulsaron el avance.");
        var relevantGrowth = grown.filter(function (agent) { return agent.values[previous] > 0 && agent.values[previous] >= Math.abs(a) * .01; }).sort(function (x, y) { return pct(y.values[current], y.values[previous]) - pct(x.values[current], x.values[previous]); });
        if (relevantGrowth.length) alerts.push("Mayor alza porcentual relevante: " + relevantGrowth[0].name + " (" + percent(pct(relevantGrowth[0].values[current], relevantGrowth[0].values[previous])) + ").");
        var declines = fallen.slice().sort(function (x, y) { return (x.values[current] - x.values[previous]) - (y.values[current] - y.values[previous]); });
        if (declines.length) alerts.push("Mayor caída absoluta: " + declines[0].name + " (" + money(declines[0].values[current] - declines[0].values[previous]) + "). Revisar sus clientes y pedidos recientes.");
        var material = declines.filter(function (agent) { return b > 0 && pct(agent.values[current], agent.values[previous]) <= -20 && Math.abs(agent.values[current] - agent.values[previous]) >= b * .01; });
        if (material.length) alerts.push(material.length + " agente" + (material.length === 1 ? " presenta" : "s presentan") + " una caída de al menos 20% y un impacto superior al 1% de las ventas del último período.");
        if (material.length) alerts.push("Prioridad inmediata: revisar el desempeño de " + material.slice(0, 3).map(function (agent) { return agent.name; }).join(", ") + " y definir acciones de recuperación.");
        if (b <= 0) alerts.push("El último período registra ventas netas no positivas; revisar facturas y notas de crédito antes de interpretar participaciones.");
        if (share !== null && share >= 60) alerts.push("Concentración elevada: los tres mayores agentes representan " + percent(share).replace(/^\+/, "") + " del resultado. Vigilar la dependencia de estas cuentas.");
        if (!alerts.length) alerts.push("No se detectaron caídas materiales ni concentración superior al umbral definido en estos períodos.");
        copy("gc-advance-alerts", alerts.map(function (message) {
            var colon = message.indexOf(":"), title = colon > 0 && colon < 42 ? message.slice(0, colon) : message.indexOf("agente") >= 0 ? "Caída material" : message.indexOf("Concentración") >= 0 ? "Concentración" : "Estado general";
            return { title: title, text: colon > 0 && colon < 42 ? message.slice(colon + 1).trim() : message, tone: /caída|no positivas|Prioridad/i.test(message) ? "critical" : /Concentración|20%/i.test(message) ? "warning" : /alza/i.test(message) ? "positive" : "", icon: /caída|Prioridad/i.test(message) ? "fa-exclamation-triangle" : /alza/i.test(message) ? "fa-arrow-up" : "fa-flag" };
        }));
    }
    function renderTable(allAgents) {
        var head = get("gc-advance-head"), body = get("gc-advance-rows"); head.replaceChildren(); body.replaceChildren();
        var tr = el("tr"); ["Agente", "Frente"].concat(state.periods.map(function (p) { return p.label || "Período"; })).concat(["Variación", "Cambio absoluto", "Participación última"]).forEach(function (label) { tr.appendChild(el("th", "", label)); }); head.appendChild(tr);
        var last = state.periods.length - 1, before = last - 1, total = model().totals[last];
        allAgents.forEach(function (a) { var row = el("tr"); row.appendChild(el("td", "", a.name + " · " + a.code)); row.appendChild(el("td", "", a.front)); a.values.forEach(function (v) { row.appendChild(el("td", "", money(v))); }); var delta = a.values[last] - a.values[before]; row.appendChild(el("td", delta < 0 ? "gc-negative" : "gc-positive", percent(pct(a.values[last], a.values[before])))); row.appendChild(el("td", delta < 0 ? "gc-negative" : "gc-positive", (delta > 0 ? "+" : "") + money(delta))); row.appendChild(el("td", "", total ? percent(100 * a.values[last] / total).replace(/^\+/, "") : "—")); body.appendChild(row); });
        if (!allAgents.length) { var empty = el("tr"), cell = el("td", "gc-empty", "No hay agentes para estos filtros."); cell.colSpan = state.periods.length + 5; empty.appendChild(cell); body.appendChild(empty); }
        get("gc-advance-table-note").textContent = allAgents.length + " agentes visibles · " + (get("gc-advance-count").value === "all" ? "gráfica completa" : "gráfica con los primeros 12") + ". Los importes se mantienen en " + get("gc-advance-currency").value + ".";
    }

    get("gc-advance-add-year").addEventListener("click", function () { var year = Number(get("gc-advance-year").value); if (!Number.isInteger(year) || year < today.getFullYear() - 10 || year > today.getFullYear()) { setStatus("Seleccione un año válido de los últimos diez años.", true); return; } var end = year === today.getFullYear() ? date(today) : year + "-12-31"; addPeriod(year + "-01-01", end, year === today.getFullYear() ? year + " · a la fecha" : String(year)); });
    get("gc-advance-add-cutoff").addEventListener("click", function () { var year = Number(get("gc-advance-year").value); if (!Number.isInteger(year) || year < today.getFullYear() - 10 || year > today.getFullYear()) { setStatus("Seleccione un año válido de los últimos diez años.", true); return; } addPeriod(year + "-01-01", cutoff(year), year + " · al " + cutoff(year).slice(8, 10) + "/" + cutoff(year).slice(5, 7)); });
    get("gc-advance-add-custom").addEventListener("click", function () { addPeriod(get("gc-advance-from").value, get("gc-advance-to").value); });
    get("gc-advance-form").addEventListener("submit", function (event) { event.preventDefault(); load(); });
    get("gc-advance-export").addEventListener("click", exportExcel);
    get("gc-advance-pdf").addEventListener("click", function () { exportPdf(false); });
    get("gc-advance-print").addEventListener("click", function () { exportPdf(true); });
    ["gc-advance-company", "gc-advance-currency"].forEach(function (id) { get(id).addEventListener("change", load); });
    root.querySelectorAll("[data-advance-mode]").forEach(function (button) { button.addEventListener("click", function () { state.mode = button.getAttribute("data-advance-mode"); root.querySelectorAll("[data-advance-mode]").forEach(function (b) { b.classList.toggle("is-active", b === button); b.setAttribute("aria-pressed", b === button ? "true" : "false"); }); if (state.data) renderChart(filtered(model().agents)); }); });
    ["gc-advance-search", "gc-advance-front", "gc-advance-count"].forEach(function (id) { get(id).addEventListener(id === "gc-advance-search" ? "input" : "change", function () { if (state.data) render(); }); });
    var current = today.getFullYear(), prior = current - 1;
    state.periods = [{ id: ++serial, desde: prior + "-01-01", hasta: cutoff(prior), label: prior + " · al mismo corte" }, { id: ++serial, desde: current + "-01-01", hasta: date(today), label: current + " · a la fecha" }];
    get("gc-advance-year").min = String(current - 10); get("gc-advance-year").max = String(current); get("gc-advance-year").value = String(current - 2);
    get("gc-advance-from").value = date(new Date(current - 2, 0, 1)); get("gc-advance-to").value = date(new Date(current - 2, 11, 31));
    renderPeriods(); load();
}());
