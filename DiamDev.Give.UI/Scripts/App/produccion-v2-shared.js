// =============================================================================
// Dashboard de Producción v2 — lógica compartida (usada por Index.cshtml y
// local.html, para que la versión que instala IT y la versión de prueba local
// de Luigi nunca se desalineen).
//
// Basado en el script original de "Dashboard produccion" — mismos cálculos de
// KPIs/eficiencia/tabla, sin tocar esa lógica. Sobre eso: filtros
// multi-selección de Producto, Recurso, Familia y Categoría, tema de color
// por empresa, tendencia en barras apiladas con referencia de capacidad
// disponible, y presets de fecha.
//
// Expected row shape (viene de dashboard_v2.sql):
//   Fecha, OT, PosicionOT, ItemID, DescripcionItem, UnidadMedida,
//   CodigoRecurso, DescripcionRecurso, FamilyCode, FamilyName, TipoItem,
//   EstadoOT, "Cantidad Planeada", "Cantidad Hecha", "Hora Plan",
//   "Hora Real Día", "Hora Real Rango", "Hora Real OT",
//   "Pieza*turnoPlan", "Pieza*turnoReal", "Eficiencia Dia", "Eficiencia Rango"
// =============================================================================

const ProduccionV2Dashboard = (() => {
    const $ = id => document.getElementById(id);

    const fmt = n => {
        if (n === null || n === undefined || isNaN(n)) return '—';
        return Number(n).toLocaleString('es-GT', { maximumFractionDigits: 2 });
    };

    const fmtPct = n => {
        if (n === null || n === undefined || isNaN(n)) return '—';
        return (Number(n)).toFixed(1) + '%';
    };

    const DOW = ['Do', 'Lu', 'Ma', 'Mi', 'Ju', 'Vi', 'Sá'];
    const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

    function dayLabel(dateStr) {
        const d = new Date(dateStr);
        if (isNaN(d)) return dateStr;
        const dd = String(d.getUTCDate()).padStart(2, '0');
        const mm = String(d.getUTCMonth() + 1).padStart(2, '0');
        return `${DOW[d.getUTCDay()]} ${dd}/${mm}`;
    }

    function isLeapYear(y) {
        return (y % 4 === 0 && y % 100 !== 0) || y % 400 === 0;
    }

    // ---- date normalization — the real SAP export gives "Fecha" as DD/MM/YYYY
    // text (e.g. "05/01/2026" = 5 de enero), NOT ISO. Every bit of this file
    // that compares/buckets dates assumes ISO YYYY-MM-DD, so we normalize once
    // here, right when data comes in, instead of re-guessing the format in every
    // function that touches a date. Handles ISO (passthrough), DD/MM/YYYY (the
    // format actually seen from datos_prueba.xls), and falls back to whatever
    // the JS Date parser can do for anything else. ----
    function normalizeFechaToISO(v) {
        if (v === null || v === undefined) return v;
        const s = String(v).trim();
        if (/^\d{4}-\d{2}-\d{2}/.test(s)) return s.slice(0, 10);
        const m = s.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
        if (m) {
            const dd = m[1].padStart(2, '0');
            const mm = m[2].padStart(2, '0');
            return `${m[3]}-${mm}-${dd}`;
        }
        const d = new Date(s);
        if (!isNaN(d)) return d.toISOString().slice(0, 10);
        return s;
    }

    // ---- bucket helpers (day/week/month/year) — used by drawTrend to group
    // the trend chart at a coarser grain when the day-by-day view gets busy ----
    function bucketKey(dateStr, bucket) {
        const d = String(dateStr).slice(0, 10);
        if (bucket === 'week') return isoWeek(dateStr);
        if (bucket === 'month') return d.slice(0, 7);
        if (bucket === 'year') return d.slice(0, 4);
        return d;
    }

    function bucketLabel(key, bucket) {
        if (bucket === 'month') {
            const [y, m] = key.split('-').map(Number);
            return `${MESES[m - 1]} ${y}`;
        }
        if (bucket === 'week' || bucket === 'year') return key;
        return dayLabel(key);
    }

    function bucketHours(key, bucket) {
        if (bucket === 'week') return 168;
        if (bucket === 'month') {
            const [y, m] = key.split('-').map(Number);
            return new Date(Date.UTC(y, m, 0)).getUTCDate() * 24;
        }
        if (bucket === 'year') {
            const y = Number(key);
            return (isLeapYear(y) ? 366 : 365) * 24;
        }
        return 24;
    }

    function formatLocalDateISO(date) {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    function resourceValue(row) {
        return String(row.CodigoRecurso || row.DescripcionRecurso || '—');
    }

    function resourceLabel(row) {
        const code = String(row.CodigoRecurso || '').trim();
        const description = String(row.DescripcionRecurso || '').trim();
        if (code && description && code !== description) return `${code} — ${description}`;
        return description || code || '—';
    }

    function dailyQuantity(row) {
        const value = Number(row["Cantidad Real Día"]);
        return Number.isFinite(value) ? value : 0;
    }

    function dailyProductionKey(row, includeResource) {
        const base = [
            normalizeFechaToISO(row.Fecha) || '',
            row.OT ?? '',
            row.PosicionOT ?? '',
            row.ItemID ?? ''
        ].join('|');
        return includeResource ? `${base}|${resourceValue(row)}` : base;
    }

    // La cantidad de IGN1 se repite por cada recurso de la misma OT/posición.
    // Para totales físicos se cuenta una vez por día/OT/posición/item; para la
    // gráfica por recurso se cuenta una vez adicional por recurso.
    function aggregateDailyQuantity(rows, groupSelector, includeResource) {
        const totals = {};
        const seen = new Set();

        rows.forEach(row => {
            const uniqueKey = dailyProductionKey(row, includeResource);
            if (seen.has(uniqueKey)) return;
            seen.add(uniqueKey);

            const group = String(groupSelector(row));
            totals[group] = (totals[group] || 0) + dailyQuantity(row);
        });

        return totals;
    }

    let raw = [];
    let charts = {};
    const msel = {}; // id -> MultiSelect instance

    function isoWeek(d) {
        const date = new Date(d);
        date.setUTCDate(date.getUTCDate() + 4 - (date.getUTCDay() || 7));
        const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
        const wk = Math.ceil((((date - yearStart) / 86400000) + 1) / 7);
        return date.getUTCFullYear() + '-W' + String(wk).padStart(2, '00');
    }

    function effClass(pct) {
        if (pct === null || pct === undefined || isNaN(pct)) return '';
        if (pct >= 90) return 'eff-good';
        if (pct >= 60) return 'eff-warn';
        return 'eff-bad';
    }

    function destroyCharts() {
        Object.values(charts).forEach(c => { if (c) c.destroy(); });
        charts = {};
    }

    // =========================================================================
    // TEMA POR EMPRESA — swaps only the brand accent color (buttons, badge,
    // KPI highlight, primary chart series). The dark background/panels and the
    // semantic good/warn/bad colors (efficiency, utilization) are left alone on
    // purpose — a full reskin per company (BOLIK black+yellow, Escocesa
    // red+gold) would fight with those meaning-carrying colors and with chart
    // legibility. See README for the reasoning; easy to revisit if you want a
    // bolder reskin instead.
    // =========================================================================
    function applyPalette(empresa) {
        document.documentElement.setAttribute('data-empresa', empresa || 'GRACO');
        const badge = $('brandBadge');
        if (badge) {
            const initials = { GRACO: 'GP', BOLIK: 'BK', ESCOCESA: 'ES' };
            badge.textContent = initials[empresa] || '?';
        }
    }

    function getAccent() {
        const v = getComputedStyle(document.documentElement).getPropertyValue('--accent').trim();
        return v || '#22d3ee';
    }

    // =========================================================================
    // MULTI-SELECT — dependency-free checkbox dropdown with search. An empty
    // selection means "no filter" (show everything), matching how the old
    // single "Todos" option worked; checking specific items narrows to those.
    // =========================================================================
    function createMultiSelect(containerId, onChangeCb) {
        const root = $(containerId);
        const btn = root.querySelector('.msel-btn');
        const panel = root.querySelector('.msel-panel');
        const search = root.querySelector('.msel-search');
        const optionsBox = root.querySelector('.msel-options');
        const clearBtn = root.querySelector('[data-act="clear"]');

        let allValues = [];   // [{value, label}]
        let selected = new Set();

        function renderOptions() {
            const q = (search.value || '').trim().toLowerCase();
            optionsBox.innerHTML = '';
            allValues
                .filter(o => !q || o.label.toLowerCase().includes(q))
                .forEach(o => {
                    const row = document.createElement('label');
                    row.className = 'msel-option';
                    row.innerHTML = `<input type="checkbox" ${selected.has(o.value) ? 'checked' : ''}> <span></span>`;
                    row.querySelector('span').textContent = o.label;
                    row.addEventListener('click', (e) => {
                        e.preventDefault();
                        if (selected.has(o.value)) selected.delete(o.value); else selected.add(o.value);
                        renderOptions();
                        updateButtonLabel();
                        onChangeCb();
                    });
                    optionsBox.appendChild(row);
                });
        }

        function updateButtonLabel() {
            if (selected.size === 0) {
                btn.textContent = 'Todos';
                btn.classList.remove('filtered');
            } else if (selected.size === 1) {
                const o = allValues.find(v => v.value === [...selected][0]);
                btn.textContent = o ? o.label : '1 seleccionado';
                btn.classList.add('filtered');
            } else {
                btn.textContent = `${selected.size} seleccionados`;
                btn.classList.add('filtered');
            }
        }

        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            document.querySelectorAll('.msel.open').forEach(m => { if (m !== root) m.classList.remove('open'); });
            root.classList.toggle('open');
        });

        document.addEventListener('click', (e) => {
            // Use composedPath (captured at dispatch time) instead of root.contains(e.target):
            // selecting a checkbox re-renders the options list (removing the clicked row from
            // the DOM) before this bubbles up, which would make root.contains(e.target) wrongly
            // report "outside" and close the panel on every selection.
            const path = typeof e.composedPath === 'function' ? e.composedPath() : [];
            if (!path.includes(root) && !root.contains(e.target)) root.classList.remove('open');
        });

        search.addEventListener('input', renderOptions);

        clearBtn.addEventListener('click', (e) => {
            e.preventDefault();
            selected.clear();
            renderOptions();
            updateButtonLabel();
            onChangeCb();
        });

        return {
            setOptions(values) {
                // values: array of {value, label}, or array of strings
                allValues = values.map(v => typeof v === 'string' ? { value: v, label: v } : v);
                // drop selections that no longer exist in the loaded data
                selected = new Set([...selected].filter(v => allValues.some(o => o.value === v)));
                renderOptions();
                updateButtonLabel();
            },
            getSelected() { return selected; },
            clear() { selected.clear(); updateButtonLabel(); }
        };
    }

    function onFilterChange() {
        populateFilterOptions();
        render();
    }

    function initMultiSelects() {
        ['mselItem', 'mselRecurso', 'mselFamilia', 'mselTipo'].forEach(id => {
            msel[id] = createMultiSelect(id, onFilterChange);
        });
    }

    // ---- date range (client-side) — narrows whatever rows are currently
    // loaded (from the server fetch on Index.cshtml, or from the uploaded
    // file on local.html) without needing a new query. Blank = no filter. ----
    function dateInRange(r) {
        const fromEl = $('from'), toEl = $('to');
        const from = fromEl && fromEl.value;
        const to = toEl && toEl.value;
        if (!from && !to) return true;
        const fecha = String(r.Fecha || '').slice(0, 10);
        if (from && fecha < from) return false;
        if (to && fecha > to) return false;
        return true;
    }

    function initDateRangeIfEmpty() {
        const fromEl = $('from'), toEl = $('to');
        if (!fromEl || !toEl) return;
        if (fromEl.value || toEl.value) return; // don't clobber a range already set (e.g. by Index.cshtml)
        if (raw.length === 0) return;
        const dates = raw.map(r => String(r.Fecha || '').slice(0, 10)).filter(Boolean).sort();
        fromEl.value = dates[0];
        toEl.value = dates[dates.length - 1];
    }

    // ---- filter control population — CASCADING: each select's own option
    // list is built from rows matching every OTHER active filter (but not
    // itself), so e.g. picking a Recurso narrows which Familia/Categoría
    // values even show up as choices. ----
    function rowsMatchingFilters(except) {
        const skip = new Set(except || []);
        const fItems = skip.has('item') ? new Set() : msel.mselItem.getSelected();
        const fRecursos = skip.has('recurso') ? new Set() : msel.mselRecurso.getSelected();
        const fFamilias = skip.has('familia') ? new Set() : msel.mselFamilia.getSelected();
        const fTipos = skip.has('tipo') ? new Set() : msel.mselTipo.getSelected();
        const fEstado = skip.has('estado') ? '' : ($('filEstado') ? $('filEstado').value : '');

        return raw.filter(r => {
            if (fItems.size && !fItems.has(r.ItemID)) return false;
            if (fRecursos.size && !fRecursos.has(resourceValue(r))) return false;
            if (fFamilias.size && !fFamilias.has(r.FamilyName)) return false;
            if (fTipos.size && !fTipos.has(r.TipoItem)) return false;
            if (fEstado && r.EstadoOT !== fEstado) return false;
            if (!skip.has('fecha') && !dateInRange(r)) return false;
            return true;
        });
    }

    function populateFilterOptions() {
        const items = new Map();   // code -> "code — desc"
        rowsMatchingFilters(['item']).forEach(r => {
            if (r.ItemID) items.set(r.ItemID, r.DescripcionItem ? `${r.ItemID} — ${r.DescripcionItem}` : r.ItemID);
        });
        msel.mselItem.setOptions([...items.entries()].sort((a, b) => a[0].localeCompare(b[0])).map(([value, label]) => ({ value, label })));

        const recursos = new Map();
        rowsMatchingFilters(['recurso']).forEach(r => {
            const value = resourceValue(r);
            if (value !== '—') recursos.set(value, resourceLabel(r));
        });
        msel.mselRecurso.setOptions(
            [...recursos.entries()]
                .sort((a, b) => a[1].localeCompare(b[1]))
                .map(([value, label]) => ({ value, label }))
        );

        const familias = new Set();
        rowsMatchingFilters(['familia']).forEach(r => { if (r.FamilyName) familias.add(r.FamilyName); });
        msel.mselFamilia.setOptions([...familias].sort());

        const tipos = new Set();
        rowsMatchingFilters(['tipo']).forEach(r => { if (r.TipoItem) tipos.add(r.TipoItem); });
        msel.mselTipo.setOptions([...tipos].sort());
    }

    function applyFilters() {
        return rowsMatchingFilters([]);
    }

    function render() {
        if (raw.length === 0) {
            ['kpiReal', 'kpiPlan', 'kpiCump', 'kpiEf', 'kpiParo', 'kpiEM']
                .forEach(id => $(id).textContent = '—');
            $('kpiRealSub').textContent = '';
            $('kpiPlanSub').textContent = '';
            $('kpiParoSub').textContent = '';
            $('tabla').querySelector('tbody').innerHTML = '';
            destroyCharts();
            $('status').textContent = 'Sin datos cargados.';
            return;
        }

        const filtered = applyFilters();
        $('status').textContent = `${filtered.length} de ${raw.length} filas · filtros aplicados`;

        renderKpis(filtered);
        drawTrend(filtered);
        drawQtyTrend(filtered);
        drawByResource(filtered);
        drawByResourceQty(filtered);
        drawUtil(filtered);
        drawByFamily(filtered);
        drawByItem(filtered);
        drawByTipo(filtered);
        renderTable(filtered);
    }

    function renderKpis(rows) {
        const sumReal = rows.reduce((a, r) => a + (+r["Hora Real Día"] || 0), 0);

        const planMap = {};
        rows.forEach(r => {
            const k = `${r.OT}|${r.PosicionOT}|${r.CodigoRecurso}`;
            if (!(k in planMap)) {
                planMap[k] = {
                    plan: +r["Hora Plan"] || 0,
                    realOT: +r["Hora Real OT"] || 0
                };
            }
        });

        const sumPlan = Object.values(planMap).reduce((a, o) => a + o.plan, 0);
        const sumRealOT = Object.values(planMap).reduce((a, o) => a + o.realOT, 0);

        const quantityTotals = aggregateDailyQuantity(rows, () => 'total', false);
        const sumCant = quantityTotals.total || 0;

        const recDiaMap = {};
        const recursosVistos = new Set();
        const diasVistos = new Set();
        rows.forEach(r => {
            const fecha = String(r.Fecha).slice(0, 10);
            const k = `${fecha}|${r.CodigoRecurso}`;
            recDiaMap[k] = (recDiaMap[k] || 0) + (+r["Hora Real Día"] || 0);
            recursosVistos.add(r.CodigoRecurso);
            diasVistos.add(fecha);
        });

        const sumParo = Object.values(recDiaMap).reduce((a, h) => a + Math.max(24 - h, 0), 0);
        const capacidadTotal = recursosVistos.size * diasVistos.size * 24;

        $('kpiReal').textContent = fmt(sumReal) + ' h';
        $('kpiRealSub').textContent = `${rows.length} registros`;

        $('kpiPlan').textContent = fmt(sumPlan) + ' h';
        $('kpiPlanSub').textContent = sumPlan ? `Δ ${fmt(sumReal - sumPlan)} h vs real` : '';

        $('kpiCump').textContent = sumPlan ? fmtPct(sumReal / sumPlan * 100) : '—';
        $('kpiEf').textContent = sumRealOT ? fmtPct(sumPlan / sumRealOT * 100) : '—';

        $('kpiParo').textContent = fmt(sumParo) + ' h';
        $('kpiParoSub').textContent = capacidadTotal
            ? `${Object.keys(recDiaMap).length} recurso·días · de ${fmt(capacidadTotal)} h posibles (${recursosVistos.size} recursos × ${diasVistos.size} días × 24h)`
            : `${Object.keys(recDiaMap).length} recurso·días`;

        $('kpiEM').textContent = fmt(sumCant);
    }

    function drawTrend(rows) {
        const bucket = $('bucket').value;
        const agg = {};
        const planSeen = new Set();

        rows.forEach(r => {
            const key = bucketKey(r.Fecha, bucket);

            if (!agg[key]) agg[key] = { real: 0, plan: 0, recDia: {}, recursos: new Set() };

            agg[key].real += (+r["Hora Real Día"] || 0);
            agg[key].recursos.add(r.CodigoRecurso);

            const planKey = `${key}|${r.OT}|${r.PosicionOT}|${r.CodigoRecurso}`;
            if (!planSeen.has(planKey)) {
                planSeen.add(planKey);
                agg[key].plan += (+r["Hora Plan"] || 0);
            }

            // Paro is always figured at day granularity (24h available per
            // resource per calendar day) and then summed into whichever
            // bucket the day falls into — that's what makes it comparable to
            // "Reales" regardless of day/week/month/year grouping.
            const rk = `${String(r.Fecha).slice(0, 10)}|${r.CodigoRecurso}`;
            agg[key].recDia[rk] = (agg[key].recDia[rk] || 0) + (+r["Hora Real Día"] || 0);
        });

        const keys = Object.keys(agg).sort();
        const labels = keys.map(k => bucketLabel(k, bucket));
        const real = keys.map(k => agg[k].real);
        const plan = keys.map(k => agg[k].plan);
        const paro = keys.map(k => Object.values(agg[k].recDia).reduce((a, h) => a + Math.max(24 - h, 0), 0));
        const capacidad = keys.map(k => agg[k].recursos.size * bucketHours(k, bucket));
        const accent = getAccent();

        // Normalized to % of capacidad disponible for that bucket, so the chart
        // reads the same whether a bucket has 1 resource or 20 — a 100%-stacked
        // view (Reales + Paro + lo que queda sin registrar) with Planificadas
        // overlaid as a % line. Raw hours are kept per-dataset for the tooltip,
        // so nothing is lost by normalizing the axis.
        const pct = (num, den) => den > 0 ? (num / den * 100) : 0;
        const realPct = keys.map((k, i) => pct(real[i], capacidad[i]));
        const paroPct = keys.map((k, i) => pct(paro[i], capacidad[i]));
        // Clamp at 0 — if Real+Paro adds up to more than Capacidad (e.g. more
        // resource codes turned out to be active that bucket than the capacity
        // line assumed — see the resource-rename caveat in dashboard_v2.sql),
        // don't draw a negative "disponible" segment.
        const restoPct = keys.map((k, i) => Math.max(0, 100 - realPct[i] - paroPct[i]));
        const restoHoras = keys.map((k, i) => Math.max(0, capacidad[i] - real[i] - paro[i]));
        const planPct = keys.map((k, i) => pct(plan[i], capacidad[i]));

        charts.trend?.destroy();
        charts.trend = new Chart($('chartTrend'), {
            data: {
                labels,
                datasets: [
                    { type: 'bar', label: 'Reales', data: realPct, backgroundColor: accent, stack: 'pct', rawHours: real },
                    { type: 'bar', label: 'Paro', data: paroPct, backgroundColor: '#ef444499', stack: 'pct', rawHours: paro },
                    { type: 'bar', label: 'Disponible / sin registrar', data: restoPct, backgroundColor: '#94a3b833', stack: 'pct', rawHours: restoHoras },
                    { type: 'line', label: 'Planificadas', data: planPct, borderColor: '#10b981', backgroundColor: '#10b98133', tension: .3, borderDash: [5, 5], pointRadius: 3, rawHours: plan }
                ]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: {
                    legend: { labels: { color: '#e2e8f0' } },
                    tooltip: {
                        callbacks: {
                            label: (ctx) => {
                                const rawArr = ctx.dataset.rawHours;
                                const raw = rawArr ? rawArr[ctx.dataIndex] : null;
                                const pctVal = ctx.parsed.y;
                                return `${ctx.dataset.label}: ${fmtPct(pctVal)}` + (raw !== null ? ` (${fmt(raw)} h)` : '');
                            },
                            afterBody: (items) => items.length ? [`Capacidad disponible: ${fmt(capacidad[items[0].dataIndex])} h`] : []
                        }
                    }
                },
                scales: {
                    x: {
                        stacked: true,
                        ticks: {
                            color: '#94a3b8', maxRotation: 35, minRotation: 35,
                            autoSkip: true,
                            // day view is the one that gets cluttered over a long range —
                            // cap how many labels it draws and let Chart.js thin them out
                            maxTicksLimit: bucket === 'day' ? 20 : undefined
                        },
                        grid: { color: '#27344966' }
                    },
                    y: {
                        stacked: true, min: 0, max: 100,
                        ticks: { color: '#94a3b8', callback: v => v + '%' },
                        grid: { color: '#27344966' }
                    }
                }
            }
        });
    }

    // Cantidad producida a lo largo del tiempo — mismo eje X/bucket que
    // Tendencia, pero en unidades producidas en vez de horas. Usa la cantidad
    // diaria y elimina las repeticiones causadas por los múltiples recursos de
    // una misma OT/posición para reconciliar con el KPI y los demás desgloses.
    function drawQtyTrend(rows) {
        const bucket = $('bucket').value;
        const agg = aggregateDailyQuantity(
            rows,
            r => bucketKey(r.Fecha, bucket),
            false);

        const keys = Object.keys(agg).sort();
        const labels = keys.map(k => bucketLabel(k, bucket));
        const qty = keys.map(k => agg[k]);

        charts.qty?.destroy();
        charts.qty = new Chart($('chartQty'), {
            type: 'bar',
            data: { labels, datasets: [{ label: 'Cantidad producida', data: qty, backgroundColor: '#f472b6' }] },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: {
                        ticks: {
                            color: '#94a3b8', maxRotation: 35, minRotation: 35,
                            autoSkip: true,
                            maxTicksLimit: bucket === 'day' ? 20 : undefined
                        },
                        grid: { color: '#27344966' }
                    },
                    y: { beginAtZero: true, ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } }
                }
            }
        });
    }

    function drawByResource(rows) {
        const agg = {};
        rows.forEach(r => {
            const k = resourceLabel(r);
            agg[k] = (agg[k] || 0) + (+r["Hora Real Día"] || 0);
        });
        const pairs = Object.entries(agg).sort((a, b) => b[1] - a[1]).slice(0, 15);
        const accent = getAccent();

        charts.rec?.destroy();
        charts.rec = new Chart($('chartRec'), {
            type: 'bar',
            data: { labels: pairs.map(p => p[0]), datasets: [{ label: 'Horas reales', data: pairs.map(p => p[1]), backgroundColor: accent }] },
            options: {
                responsive: true, maintainAspectRatio: false, indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: {
                    x: { beginAtZero: true, ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } },
                    y: { ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } }
                }
            }
        });
    }

    function drawByResourceQty(rows) {
        const agg = aggregateDailyQuantity(rows, resourceLabel, true);
        const pairs = Object.entries(agg).sort((a, b) => b[1] - a[1]).slice(0, 15);

        charts.recQty?.destroy();
        charts.recQty = new Chart($('chartRecQty'), {
            type: 'bar',
            data: { labels: pairs.map(p => p[0]), datasets: [{ label: 'Cantidad real', data: pairs.map(p => p[1]), backgroundColor: '#38bdf8' }] },
            options: {
                responsive: true, maintainAspectRatio: false, indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: {
                    x: { beginAtZero: true, ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } },
                    y: { ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } }
                }
            }
        });
    }

    function drawByItem(rows) {
        const quantities = aggregateDailyQuantity(
            rows,
            r => r.ItemID || 'Sin código',
            false);
        const descriptions = {};
        rows.forEach(r => {
            const key = r.ItemID || 'Sin código';
            if (!(key in descriptions)) descriptions[key] = r.DescripcionItem || '';
        });
        const pairs = Object.entries(quantities)
            .map(([key, qty]) => [key, { qty, desc: descriptions[key] || '' }])
            .sort((a, b) => b[1].qty - a[1].qty)
            .slice(0, 15);
        // Full label kept intact — it's what Chart.js's default tooltip title
        // shows on hover. Only the Y-axis TICK TEXT is shortened, via the
        // callback below, so a long item description no longer gets cut to the
        // same 40 characters in both places (previously the label itself was
        // truncated, so even the tooltip only ever had the short version).
        const labels = pairs.map(([k, v]) => (v.desc ? `${k} — ${v.desc}` : k));

        charts.item?.destroy();
        charts.item = new Chart($('chartItem'), {
            type: 'bar',
            data: { labels, datasets: [{ label: 'Cantidad hecha', data: pairs.map(([, v]) => v.qty), backgroundColor: '#f472b6' }] },
            options: {
                responsive: true, maintainAspectRatio: false, indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: {
                    x: { beginAtZero: true, ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } },
                    y: {
                        ticks: {
                            color: '#94a3b8',
                            callback: function (value) {
                                const label = this.getLabelForValue(value);
                                return label.length > 40 ? label.slice(0, 37) + '…' : label;
                            }
                        },
                        grid: { color: '#27344966' }
                    }
                }
            }
        });
    }

    function drawUtil(rows) {
        const map = {};
        rows.forEach(r => {
            const rec = resourceLabel(r);
            const fecha = String(r.Fecha).slice(0, 10);
            const dk = fecha + '|' + r.CodigoRecurso;

            if (!map[rec]) map[rec] = { real: {}, dias: new Set() };

            map[rec].real[dk] = (map[rec].real[dk] || 0) + (+r["Hora Real Día"] || 0);
            map[rec].dias.add(fecha);
        });

        const pairs = [];
        Object.entries(map).forEach(([rec, v]) => {
            const totReal = Object.values(v.real).reduce((a, h) => a + h, 0);
            const dispo = v.dias.size * 24;
            if (dispo > 0) pairs.push([rec, +(totReal / dispo * 100).toFixed(1)]);
        });

        pairs.sort((a, b) => b[1] - a[1]);
        const top = pairs.slice(0, 15);

        charts.util?.destroy();
        charts.util = new Chart($('chartUtil'), {
            type: 'bar',
            data: {
                labels: top.map(p => p[0]),
                datasets: [{
                    label: '% Utilización', data: top.map(p => p[1]),
                    backgroundColor: top.map(p => p[1] >= 70 ? '#10b981' : p[1] >= 40 ? '#f59e0b' : '#ef4444')
                }]
            },
            options: {
                responsive: true, maintainAspectRatio: false, indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: {
                    x: { min: 0, max: 100, ticks: { color: '#94a3b8', callback: v => v + '%' }, grid: { color: '#27344966' } },
                    y: { ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } }
                }
            }
        });
    }

    function drawByFamily(rows) {
        const agg = aggregateDailyQuantity(
            rows,
            r => r.FamilyName || 'Sin familia',
            false);
        const pairs = Object.entries(agg).sort((a, b) => b[1] - a[1]).slice(0, 15);

        charts.fam?.destroy();
        charts.fam = new Chart($('chartFamilia'), {
            type: 'bar',
            data: { labels: pairs.map(p => p[0]), datasets: [{ label: 'Cantidad hecha', data: pairs.map(p => p[1]), backgroundColor: '#a78bfa' }] },
            options: {
                responsive: true, maintainAspectRatio: false, indexAxis: 'y',
                plugins: { legend: { display: false } },
                scales: {
                    x: { beginAtZero: true, ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } },
                    y: { ticks: { color: '#94a3b8' }, grid: { color: '#27344966' } }
                }
            }
        });
    }

    function drawByTipo(rows) {
        const agg = aggregateDailyQuantity(
            rows,
            r => r.TipoItem || 'Otro',
            false);
        const labels = Object.keys(agg);
        const data = labels.map(k => agg[k]);

        charts.tipo?.destroy();
        charts.tipo = new Chart($('chartTipo'), {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{ data, backgroundColor: ['#22d3ee', '#f59e0b', '#94a3b8', '#10b981', '#a78bfa', '#f472b6'] }]
            },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { color: '#e2e8f0' } } }
            }
        });
    }

    function fillTable(rows) {
        const tb = $('tabla').querySelector('tbody');
        while (tb.firstChild) tb.removeChild(tb.firstChild);

        function appendCell(tr, value, className) {
            const td = document.createElement('td');
            if (className) td.className = className;
            td.textContent = value === null || value === undefined ? '' : String(value);
            tr.appendChild(td);
        }

        rows.slice(0, 500).forEach(r => {
            const efDia = +r["Eficiencia Dia"];
            const efRango = r["Eficiencia Rango"];
            const cls = effClass(efDia);
            const estadoCls = (r.EstadoOT || '').toLowerCase() === 'cerrada' ? 'estado-cerrada' : 'estado-abierta';

            const tr = document.createElement('tr');
            appendCell(tr, String(r.Fecha || '').slice(0, 10));
            appendCell(tr, r.OT);
            appendCell(tr, r.PosicionOT);
            appendCell(tr, r.ItemID);
            appendCell(tr, r.DescripcionItem);
            appendCell(tr, r.UnidadMedida);
            appendCell(tr, r.FamilyName);
            appendCell(tr, r.TipoItem);
            appendCell(tr, r.DescripcionRecurso);
            appendCell(tr, r.EstadoOT, estadoCls);
            appendCell(tr, fmt(r["Cantidad Planeada"]), 'num');
            appendCell(tr, fmt(r["Cantidad Hecha"]), 'num');
            appendCell(tr, fmt(r["Hora Plan"]), 'num');
            appendCell(tr, fmt(r["Hora Real Día"]), 'num');
            appendCell(tr, fmt(r["Hora Real Rango"]), 'num');
            appendCell(tr, fmt(r["Hora Real OT"]), 'num');
            appendCell(tr, fmt(r["Pieza*turnoPlan"]), 'num');
            appendCell(tr, fmt(r["Pieza*turnoReal"]), 'num');
            appendCell(tr, isNaN(efDia) ? '' : fmt(efDia) + '%', `num ${cls}`.trim());
            appendCell(tr, efRango != null ? fmt(efRango) + '%' : '', 'num');
            tb.appendChild(tr);
        });

        if (rows.length > 500) {
            const tr = document.createElement('tr');
            const td = document.createElement('td');
            td.colSpan = 20;
            td.style.textAlign = 'center';
            td.style.color = '#94a3b8';
            td.textContent = `Mostrando 500 de ${rows.length} filas`;
            tr.appendChild(td);
            tb.appendChild(tr);
        }
    }

    // ---- tabla: orden por columna, al hacer clic en el encabezado ----
    let tableSort = { key: null, dir: 1 };
    let lastTableRows = [];

    function sortTableRows(rows) {
        if (!tableSort.key) return rows;
        const key = tableSort.key, dir = tableSort.dir;
        return [...rows].sort((a, b) => {
            const av = a[key], bv = b[key];
            const an = Number(av), bn = Number(bv);
            const bothNumeric = av !== null && av !== undefined && av !== '' &&
                bv !== null && bv !== undefined && bv !== '' && !isNaN(an) && !isNaN(bn);
            if (bothNumeric) return (an - bn) * dir;
            const as = (av ?? '').toString().toLowerCase();
            const bs = (bv ?? '').toString().toLowerCase();
            if (as < bs) return -1 * dir;
            if (as > bs) return 1 * dir;
            return 0;
        });
    }

    function updateSortIndicators() {
        const table = $('tabla');
        if (!table) return;
        table.querySelectorAll('thead th').forEach(th => {
            th.classList.remove('sorted-asc', 'sorted-desc');
            if (th.dataset.key && th.dataset.key === tableSort.key) {
                th.classList.add(tableSort.dir === 1 ? 'sorted-asc' : 'sorted-desc');
            }
        });
    }

    function renderTable(rows) {
        lastTableRows = rows;
        fillTable(sortTableRows(rows));
        updateSortIndicators();
    }

    function wireTableSort() {
        const table = $('tabla');
        if (!table) return;
        table.querySelectorAll('thead th').forEach(th => {
            if (!th.dataset.key) return;
            th.classList.add('sortable');
            th.addEventListener('click', () => {
                if (tableSort.key === th.dataset.key) tableSort.dir = -tableSort.dir;
                else { tableSort.key = th.dataset.key; tableSort.dir = 1; }
                renderTable(lastTableRows);
            });
        });
    }

    // ---- aviso de recursos renombrados 2026-05-01 (ver dashboard_v2.sql) ----
    function checkResourceRelabelBanner(fromStr, toStr) {
        const cutoff = new Date('2026-05-01');
        const from = new Date(fromStr);
        const to = new Date(toStr);
        const banner = $('bannerRecurso');
        if (!banner) return;
        if (!isNaN(from) && !isNaN(to) && from < cutoff && to >= cutoff) {
            banner.classList.add('show');
        } else {
            banner.classList.remove('show');
        }
    }

    function setRawData(rows) {
        raw = (Array.isArray(rows) ? rows : []).map(r => (
            r && r.Fecha !== undefined ? { ...r, Fecha: normalizeFechaToISO(r.Fecha) } : r
        ));
        initDateRangeIfEmpty();
        populateFilterOptions();
        render();
    }

    function wireFilterControls(options) {
        const settings = options || {};
        initMultiSelects();
        wireTableSort();
        $('filEstado').addEventListener('change', onFilterChange);
        $('bucket').addEventListener('change', () => { if (raw.length > 0) render(); });
        // Client-side date range — narrows whatever is currently loaded and also
        // re-runs the cascading filter options (e.g. Familia options should
        // reflect only what happened within the selected dates).
        const fromEl = $('from'), toEl = $('to');
        const onDateChange = typeof settings.onDateChange === 'function'
            ? settings.onDateChange
            : onFilterChange;
        if (fromEl) fromEl.addEventListener('change', onDateChange);
        if (toEl) toEl.addEventListener('change', onDateChange);
    }

    // ---- date range presets ----
    function applyDatePreset(preset, fromEl, toEl, todayOverride) {
        const today = todayOverride ? new Date(todayOverride) : new Date();
        let from = new Date(today), to = new Date(today);

        switch (preset) {
            case 'hoy':
                break;
            case '7d':
                from.setDate(today.getDate() - 6);
                break;
            case '30d':
                from.setDate(today.getDate() - 29);
                break;
            case 'mes':
                from = new Date(today.getFullYear(), today.getMonth(), 1);
                break;
            case 'mes-pasado':
                from = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                to = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'anio':
                from = new Date(today.getFullYear(), 0, 1);
                break;
        }

        fromEl.value = formatLocalDateISO(from);
        toEl.value = formatLocalDateISO(to);
    }

    return {
        setRawData, wireFilterControls, render, checkResourceRelabelBanner,
        applyPalette, applyDatePreset,
        __test: {
            normalizeFechaToISO,
            bucketKey,
            bucketHours,
            isoWeek,
            formatLocalDateISO,
            resourceValue,
            resourceLabel,
            dailyProductionKey,
            aggregateDailyQuantity
        }
    };
})();

if (typeof module !== 'undefined' && module.exports) {
    module.exports = ProduccionV2Dashboard;
}
