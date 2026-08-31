// =============================================================================
// Backlog dashboard — shared rendering logic (used by both Index.cshtml and
// local.html, so the IT-deployed version and the local test file never drift).
//
// Visual system and chart components carried over from the earlier
// production_planning_dashboard_1.html report, per Luigi's request to go back
// to that look (plain SVG, no external chart library, light-first theme) for
// this dashboard instead of "Dashboard produccion"'s dark Chart.js style.
// Unlike that earlier report, this is a LIVE tool: data is loaded at view
// time (fetch from the server, or an uploaded export in local.html) and
// recomputed from scratch, not a one-time static snapshot — there is no
// stock-netting or backlog-vs-capacity section here (those need data sources
// this query doesn't provide); this is backlog + delivery status + item
// demand + trend, filterable by item, family, customer, and local/foreign.
//
// Expected row shape (from backlog.sql aliases):
//   OrderDocEntry, OrderNumber, OrderDate, DueDate, OrderStatus, CustomerCode,
//   CustomerName, Origen, LineNumber, ItemCode, ItemDescription, FamilyCode,
//   FamilyName, OrderedQty, OpenQty, LineStatus, LineShipDate
// =============================================================================

const BacklogViz = (() => {
  const $ = id => document.getElementById(id);

  const fmt = (n) => {
    if (n === null || n === undefined || isNaN(n)) return '—';
    return Math.round(n).toLocaleString('es-GT');
  };
  const compact = (n) => {
    if (n === null || n === undefined || isNaN(n)) return '—';
    if (Math.abs(n) >= 1e6) return (n / 1e6).toFixed(1) + 'M';
    if (Math.abs(n) >= 1e3) return (n / 1e3).toFixed(1) + 'K';
    return Math.round(n).toString();
  };

  function formatLocalDateISO(date) {
    const d = date instanceof Date ? date : new Date(date);
    if (isNaN(d)) return null;
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  function setDefaultDateRange(fromEl, toEl, todayOverride) {
    const today = todayOverride ? new Date(todayOverride) : new Date();
    const from = new Date(today);
    from.setDate(today.getDate() - 89);
    fromEl.value = formatLocalDateISO(from);
    toEl.value = formatLocalDateISO(today);
  }

  let raw = [];
  let activeBucket = null;
  let backlogSort = { key: 'daysLate', dir: -1 };
  let itemSort = { key: 'qty', dir: -1 };
  let resizeTimer = null;

  // ---------------------------------------------------------------------
  // Date normalization — SAP exports have shown up as DD/MM/YYYY text
  // rather than ISO before (see Producción v2's date-filter bug); normalize
  // once here so every downstream computation can assume ISO YYYY-MM-DD.
  // ---------------------------------------------------------------------
  function normalizeFechaToISO(v) {
    if (v === null || v === undefined || v === '') return null;
    const s = String(v).trim();
    if (/^\d{4}-\d{2}-\d{2}/.test(s)) return s.slice(0, 10);
    const m = s.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
    if (m) {
      const dd = m[1].padStart(2, '0');
      const mm = m[2].padStart(2, '0');
      return `${m[3]}-${mm}-${dd}`;
    }
    const d = new Date(s);
    if (!isNaN(d)) return formatLocalDateISO(d);
    return null;
  }

  function escapeHtml(s) {
    const d = document.createElement('div');
    d.textContent = s == null ? '' : String(s);
    return d.innerHTML;
  }

  // ---------------------------------------------------------------------
  // Tema por empresa — same mechanism as Producción v2 (data-empresa on
  // <html>, badge initials, palette swapped entirely via CSS variables in
  // backlog-viz.css). Charts here are plain SVG re-drawn from scratch on
  // every render(), so switching companies just needs a re-render — no
  // per-chart-instance color update like Chart.js needs.
  // ---------------------------------------------------------------------
  function applyPalette(empresa) {
    document.documentElement.setAttribute('data-empresa', empresa || 'GRACO');
    const badge = $('brandBadge');
    if (badge) {
      const initials = { GRACO: 'GP', BOLIK: 'BK', ESCOCESA: 'ES' };
      badge.textContent = initials[empresa] || '?';
    }
    if (raw.length) render();
  }

  // ---------------------------------------------------------------------
  // Bucketing helpers for the trend chart's Día/Semana/Mes/Año selector —
  // same ISO-week logic as Producción v2's bucketKey/bucketLabel, ported
  // here so the trend chart isn't locked to month granularity.
  // ---------------------------------------------------------------------
  const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

  function isoWeek(d) {
    const date = new Date(d);
    date.setUTCDate(date.getUTCDate() + 4 - (date.getUTCDay() || 7));
    const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
    const wk = Math.ceil((((date - yearStart) / 86400000) + 1) / 7);
    return date.getUTCFullYear() + '-W' + String(wk).padStart(2, '0');
  }

  function trendBucketKey(dateStr, bucket) {
    const d = String(dateStr).slice(0, 10);
    if (bucket === 'week') return isoWeek(dateStr);
    if (bucket === 'year') return d.slice(0, 4);
    return d.slice(0, 7); // month (default)
  }

  function trendBucketLabel(key, bucket) {
    if (bucket === 'week' || bucket === 'year') return key;
    const [y, m] = key.split('-').map(Number);
    return `${MESES[m - 1]} ${String(y).slice(2)}`;
  }

  function cssVar(name) {
    const v = getComputedStyle(document.querySelector('.viz-root')).getPropertyValue(name).trim();
    return v || '#2a78d6';
  }

  // ---------------------------------------------------------------------
  // Theme toggle
  // ---------------------------------------------------------------------
  function wireThemeToggle() {
    const root = document.documentElement;
    const btn = $('themeToggle');
    if (!btn) return;
    btn.addEventListener('click', () => {
      // Dark is the default now (matching Producción v2 — no data-theme
      // attribute at all means dark), so "currently light" is the only
      // explicit state to check for; anything else toggles to light.
      const isLight = root.getAttribute('data-theme') === 'light';
      if (isLight) { root.setAttribute('data-theme', 'dark'); btn.textContent = 'Cambiar a claro'; }
      else { root.setAttribute('data-theme', 'light'); btn.textContent = 'Cambiar a oscuro'; }
    });
  }

  // ---------------------------------------------------------------------
  // Tooltip
  // ---------------------------------------------------------------------
  function showTip(x, y, html) {
    const tip = $('tooltip');
    tip.innerHTML = html;
    tip.classList.add('show');
    const rect = tip.getBoundingClientRect();
    let left = x + 14, top = y - rect.height / 2;
    if (left + rect.width > window.innerWidth - 8) left = x - rect.width - 14;
    top = Math.max(8, Math.min(top, window.innerHeight - rect.height - 8));
    tip.style.left = left + 'px';
    tip.style.top = top + 'px';
  }
  function hideTip() { $('tooltip').classList.remove('show'); }

  // ---------------------------------------------------------------------
  // Generic chart components (plain SVG, no library)
  // ---------------------------------------------------------------------
  function horizontalBarChart(container, items, opts) {
    opts = opts || {};
    const width = container.clientWidth || 600;
    const barH = opts.barH || 22;
    const gap = opts.gap || 14;
    const leftPad = opts.leftPad || 170;
    const rightPad = 70;
    const topPad = 8;
    if (!items.length) { container.innerHTML = '<div class="empty-state">Sin datos para los filtros seleccionados.</div>'; return; }
    const maxVal = Math.max(...items.map(i => i.value), 1);
    const plotW = width - leftPad - rightPad;
    const height = topPad * 2 + items.length * (barH + gap) - gap;

    let svg = `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">`;
    items.forEach((it, i) => {
      const y = topPad + i * (barH + gap);
      const w = Math.max(2, (it.value / maxVal) * plotW);
      const color = it.color || cssVar('--series-1');
      svg += `<g class="bar-hit" data-idx="${i}">
        <text x="${leftPad - 10}" y="${y + barH / 2 + 4}" text-anchor="end" class="cat-label">${escapeHtml(it.label)}</text>
        <rect class="bar" x="${leftPad}" y="${y}" width="${w}" height="${barH}" rx="4" fill="${color}"></rect>
        <text x="${leftPad + w + 8}" y="${y + barH / 2 + 4}" class="value-label">${it.valueLabel || compact(it.value)}</text>
        <rect x="${leftPad}" y="${y}" width="${plotW}" height="${barH}" fill="transparent"></rect>
      </g>`;
    });
    svg += `</svg>`;
    container.innerHTML = svg;

    container.querySelectorAll('.bar-hit').forEach(g => {
      const idx = +g.dataset.idx;
      const it = items[idx];
      g.addEventListener('pointermove', (e) => {
        showTip(e.clientX, e.clientY, `<div class="t-title">${escapeHtml(it.label)}</div>${it.tooltip || ('<div class="t-row"><span>Valor</span><span class="t-val">' + fmt(it.value) + '</span></div>')}`);
      });
      g.addEventListener('pointerleave', hideTip);
    });
  }

  function lineChart(container, points, opts) {
    opts = opts || {};
    const width = container.clientWidth || 600;
    const height = opts.height || 220;
    const leftPad = 46, rightPad = 12, topPad = 16, botPad = 30;
    if (!points.length) { container.innerHTML = '<div class="empty-state">Sin datos para los filtros seleccionados.</div>'; return; }
    const plotW = width - leftPad - rightPad;
    const plotH = height - topPad - botPad;
    const maxVal = Math.max(...points.map(p => p.value), 1) * 1.12;
    const x = i => points.length > 1 ? leftPad + (i / (points.length - 1)) * plotW : leftPad + plotW / 2;
    const y = v => topPad + plotH - (v / maxVal) * plotH;

    const ticks = 4;
    let gridSvg = '';
    for (let t = 0; t <= ticks; t++) {
      const v = (maxVal / ticks) * t;
      const yy = y(v);
      gridSvg += `<line class="grid-line" x1="${leftPad}" x2="${width - rightPad}" y1="${yy}" y2="${yy}"></line>`;
      gridSvg += `<text class="axis-label" x="${leftPad - 8}" y="${yy + 3}" text-anchor="end">${compact(v)}</text>`;
    }

    let path = '';
    points.forEach((p, i) => { path += (i === 0 ? 'M' : 'L') + x(i) + ' ' + y(p.value) + ' '; });

    const step = Math.max(1, Math.ceil(points.length / 8));
    let labels = '';
    points.forEach((p, i) => {
      if (i % step === 0 || i === points.length - 1) {
        labels += `<text class="axis-label" x="${x(i)}" y="${height - 6}" text-anchor="middle">${p.month}</text>`;
      }
    });

    let dots = '';
    points.forEach((p, i) => {
      dots += `<circle data-idx="${i}" cx="${x(i)}" cy="${y(p.value)}" r="4" fill="${p.partial ? cssVar('--page-plane') : cssVar('--series-1')}" stroke="${cssVar('--series-1')}" stroke-width="2"></circle>`;
      dots += `<rect data-idx="${i}" x="${x(i) - 12}" y="${topPad}" width="24" height="${plotH}" fill="transparent" class="hit-col"></rect>`;
    });

    // ---------------------------------------------------------------------
    // Linear trendline — simple least-squares fit (x = interval index,
    // y = value) over the plotted intervals, showing the overall average
    // trend across the evaluated period. Recomputes automatically whenever
    // the underlying points change (date range, bucket size, or filters),
    // since this whole function re-runs on every render(). The current/
    // still-open interval (p.partial) is excluded from the FIT — it's
    // usually much lower just because it isn't finished yet, and including
    // it would drag the slope down artificially — but the drawn line still
    // extends across the full width, including that last point, for a
    // consistent visual reference.
    // ---------------------------------------------------------------------
    let trendPath = '';
    let trendPts = null;
    if (opts.showTrend) {
      const fitPoints = points.map((p, i) => ({ i, v: p.value })).filter((_, i) => !points[i].partial);
      const n = fitPoints.length;
      if (n >= 2) {
        const sumX = fitPoints.reduce((a, p) => a + p.i, 0);
        const sumY = fitPoints.reduce((a, p) => a + p.v, 0);
        const sumXY = fitPoints.reduce((a, p) => a + p.i * p.v, 0);
        const sumXX = fitPoints.reduce((a, p) => a + p.i * p.i, 0);
        const denom = (n * sumXX - sumX * sumX);
        const slope = denom !== 0 ? (n * sumXY - sumX * sumY) / denom : 0;
        const intercept = (sumY - slope * sumX) / n;
        const trendAt = i => intercept + slope * i;
        const lastI = points.length - 1;
        trendPts = { start: trendAt(0), end: trendAt(lastI), slope, avg: sumY / n };
        trendPath = `M${x(0)} ${y(Math.max(0, trendAt(0)))} L${x(lastI)} ${y(Math.max(0, trendAt(lastI)))}`;
      }
    }

    container.innerHTML = `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
      ${gridSvg}
      <path d="${path}" fill="none" stroke="${cssVar('--series-1')}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"></path>
      ${trendPath ? `<path d="${trendPath}" fill="none" stroke="${cssVar('--series-2')}" stroke-width="2" stroke-dasharray="6 4" stroke-linecap="round"></path>` : ''}
      ${dots}
      ${labels}
      <line class="axis-line" x1="${leftPad}" x2="${width - rightPad}" y1="${topPad + plotH}" y2="${topPad + plotH}"></line>
    </svg>`;

    container.querySelectorAll('.hit-col').forEach(el => {
      const idx = +el.dataset.idx;
      const p = points[idx];
      el.addEventListener('pointermove', (e) => {
        const trendRow = trendPts
          ? `<div class="t-row"><span>Tendencia lineal</span><span class="t-val">${fmt(trendPts.start + (trendPts.slope * idx))}</span></div>`
          : '';
        showTip(e.clientX, e.clientY, `<div class="t-title">${p.month}${p.partial ? ' (parcial)' : ''}</div>
          <div class="t-row"><span>Cantidad pedida</span><span class="t-val">${fmt(p.value)}</span></div>
          <div class="t-row"><span>Pedidos</span><span class="t-val">${fmt(p.orders)}</span></div>${trendRow}`);
      });
      el.addEventListener('pointerleave', hideTip);
    });
  }

  function verticalBarChart(container, items, opts) {
    opts = opts || {};
    const width = container.clientWidth || 400;
    const height = opts.height || 230;
    const leftPad = 46, topPad = 16, botPad = 30;
    const rp = 10;
    if (!items.length) { container.innerHTML = '<div class="empty-state">Sin datos.</div>'; return; }
    const plotW = width - leftPad - rp;
    const plotH = height - topPad - botPad;
    const maxVal = Math.max(...items.map(i => i.value), 1) * 1.12;
    const bw = Math.min(28, plotW / items.length - 8);
    const step = plotW / items.length;

    const ticks = 4;
    let gridSvg = '';
    for (let t = 0; t <= ticks; t++) {
      const v = (maxVal / ticks) * t;
      const yy = topPad + plotH - (v / maxVal) * plotH;
      gridSvg += `<line class="grid-line" x1="${leftPad}" x2="${width - rp}" y1="${yy}" y2="${yy}"></line>`;
      gridSvg += `<text class="axis-label" x="${leftPad - 8}" y="${yy + 3}" text-anchor="end">${compact(v)}</text>`;
    }

    let bars = '';
    items.forEach((it, i) => {
      const cx = leftPad + step * i + step / 2;
      const h = (it.value / maxVal) * plotH;
      const yy = topPad + plotH - h;
      bars += `<g class="bar-hit" data-idx="${i}">
        <rect class="bar" x="${cx - bw / 2}" y="${yy}" width="${bw}" height="${h}" rx="4" fill="${cssVar('--series-1')}"></rect>
        <rect x="${cx - step / 2}" y="${topPad}" width="${step}" height="${plotH}" fill="transparent"></rect>
        <text class="axis-label" x="${cx}" y="${height - 6}" text-anchor="middle">${it.label}</text>
      </g>`;
    });

    container.innerHTML = `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
      ${gridSvg}${bars}
      <line class="axis-line" x1="${leftPad}" x2="${width - rp}" y1="${topPad + plotH}" y2="${topPad + plotH}"></line>
    </svg>`;

    container.querySelectorAll('.bar-hit').forEach(g => {
      const idx = +g.dataset.idx;
      const it = items[idx];
      g.addEventListener('pointermove', (e) => {
        showTip(e.clientX, e.clientY, `<div class="t-title">${it.label}</div><div class="t-row"><span>Promedio pedido</span><span class="t-val">${fmt(it.value)}</span></div>`);
      });
      g.addEventListener('pointerleave', hideTip);
    });
  }

  // ---------------------------------------------------------------------
  // Sortable / filterable table helper
  // ---------------------------------------------------------------------
  function makeTable(tableEl, rowCountEl, columns, opts) {
    opts = opts || {};
    const tbody = tableEl.querySelector('tbody');
    const heads = tableEl.querySelectorAll('thead th');

    function render(rows) {
      tbody.innerHTML = '';
      const shown = rows.slice(0, opts.maxRows || 500);
      shown.forEach(r => {
        const tr = document.createElement('tr');
        columns.forEach(c => {
          const td = document.createElement('td');
          if (c.num) td.className = 'num';
          if (c.html) td.innerHTML = c.html(r);
          else td.textContent = c.render ? c.render(r) : (r[c.key] ?? '');
          tr.appendChild(td);
        });
        tbody.appendChild(tr);
      });
      if (rowCountEl) {
        rowCountEl.textContent = rows.length
          ? `Mostrando ${shown.length.toLocaleString('es-GT')} de ${rows.length.toLocaleString('es-GT')} filas` + (rows.length > shown.length ? ' (afina el filtro para ver más)' : '')
          : 'Sin filas para los filtros seleccionados.';
      }
    }

    heads.forEach(th => {
      th.addEventListener('click', () => {
        opts.onSort && opts.onSort(th.dataset.key);
        heads.forEach(h => h.querySelector('.sort-arrow') && h.querySelector('.sort-arrow').remove());
        const arrow = document.createElement('span');
        arrow.className = 'sort-arrow';
        arrow.textContent = opts.getSortDir ? (opts.getSortDir() === 1 ? '▲' : '▼') : '▼';
        th.appendChild(arrow);
      });
    });

    return { render };
  }

  function sortRows(rows, key, dir) {
    if (!key) return rows;
    return [...rows].sort((a, b) => {
      let av = a[key], bv = b[key];
      if (typeof av === 'string') { av = av.toLowerCase(); bv = (bv || '').toLowerCase(); }
      if (av == null && bv == null) return 0;
      if (av == null) return 1;
      if (bv == null) return -1;
      if (av < bv) return -1 * dir;
      if (av > bv) return 1 * dir;
      return 0;
    });
  }

  // ---------------------------------------------------------------------
  // Global filters (item / family / customer / origen) — applied to every
  // section, not just one table, per Luigi's original ask for backlog
  // slicers on item code, item family, customer name, and local/foreign.
  // ---------------------------------------------------------------------
  function populateGlobalFilterOptions() {
    const familias = new Set();
    raw.forEach(r => { if (r.FamilyName) familias.add(r.FamilyName); });
    const famSel = $('filFamilia');
    if (famSel) {
      const current = famSel.value;
      famSel.innerHTML = '<option value="">Todas las familias</option>' +
        [...familias].sort().map(f => `<option value="${escapeHtml(f)}">${escapeHtml(f)}</option>`).join('');
      if ([...familias].includes(current)) famSel.value = current;
    }

    const agentes = new Set();
    raw.forEach(r => { if (r.SalesAgent) agentes.add(r.SalesAgent); });
    const agentSel = $('filAgente');
    if (agentSel) {
      const current = agentSel.value;
      agentSel.innerHTML = '<option value="">Todos los vendedores</option>' +
        [...agentes].sort().map(a => `<option value="${escapeHtml(a)}">${escapeHtml(a)}</option>`).join('');
      if ([...agentes].includes(current)) agentSel.value = current;
    }

    const estados = new Set();
    raw.forEach(r => { if (r.CustomerState) estados.add(r.CustomerState); });
    const estadoSel = $('filEstado');
    if (estadoSel) {
      const current = estadoSel.value;
      estadoSel.innerHTML = '<option value="">Todos los departamentos</option>' +
        [...estados].sort().map(s => `<option value="${escapeHtml(s)}">${escapeHtml(s)}</option>`).join('');
      if ([...estados].includes(current)) estadoSel.value = current;
    }
  }

  // ---------------------------------------------------------------------
  // Suggestions for the Código de item / Código de cliente filters —
  // datalist options built from the actual loaded data, cascading against
  // whichever OTHER filters (family, origen, "solo abiertas", and the other
  // code field) are currently active — same cascading idea already used for
  // the Familia dropdown, extended to these two. Deliberately excludes the
  // field's own current text so the list doesn't shrink to just what's
  // already typed; the browser's native datalist matching handles narrowing
  // as the user types further.
  // ---------------------------------------------------------------------
  function populateCodeSuggestions() {
    const itemList = $('itemCodeList');
    const custList = $('customerCodeList');
    if (!itemList && !custList) return;
    const f = getGlobalFilters();

    const commonPass = (r) => {
      if (f.openOnly && !isOpenLine(r)) return false;
      if (f.familia && r.FamilyName !== f.familia) return false;
      if (f.origen && r.Origen !== f.origen) return false;
      if (f.agente && r.SalesAgent !== f.agente) return false;
      if (f.estado && r.CustomerState !== f.estado) return false;
      return true;
    };

    if (itemList) {
      const codes = new Set();
      raw.forEach(r => {
        if (!commonPass(r)) return;
        if (f.customerCode && !String(r.CustomerCode ?? '').toLowerCase().includes(f.customerCode)) return;
        if (r.ItemCode) codes.add(r.ItemCode);
      });
      itemList.innerHTML = [...codes].sort().slice(0, 500).map(c => `<option value="${escapeHtml(c)}">`).join('');
    }
    if (custList) {
      const codes = new Set();
      raw.forEach(r => {
        if (!commonPass(r)) return;
        if (f.itemCode && !String(r.ItemCode ?? '').toLowerCase().includes(f.itemCode)) return;
        if (r.CustomerCode) codes.add(r.CustomerCode);
      });
      custList.innerHTML = [...codes].sort().slice(0, 500).map(c => `<option value="${escapeHtml(c)}">`).join('');
    }
  }

  function getGlobalFilters() {
    const q = ($('globalSearch') && $('globalSearch').value.trim().toLowerCase()) || '';
    const familia = ($('filFamilia') && $('filFamilia').value) || '';
    const itemCode = ($('filItemCode') && $('filItemCode').value.trim().toLowerCase()) || '';
    const customerCode = ($('filCustomerCode') && $('filCustomerCode').value.trim().toLowerCase()) || '';
    const agente = ($('filAgente') && $('filAgente').value) || '';
    const estado = ($('filEstado') && $('filEstado').value) || '';
    const origen = window.__activeOrigen || '';
    const openOnly = $('filOpenOnly') ? $('filOpenOnly').checked : true;
    return { q, familia, itemCode, customerCode, agente, estado, origen, openOnly };
  }

  function applyGlobalFilters(rows, opts) {
    opts = opts || {};
    const f = getGlobalFilters();
    return rows.filter(r => {
      if (opts.openOnlyRelevant !== false && f.openOnly && !isOpenLine(r)) return false;
      if (f.familia && r.FamilyName !== f.familia) return false;
      if (f.origen && r.Origen !== f.origen) return false;
      if (f.agente && r.SalesAgent !== f.agente) return false;
      if (f.estado && r.CustomerState !== f.estado) return false;
      // Substring match (not exact) so "PT00" narrows to every item starting
      // with that prefix, and a partial customer code still filters — same
      // behavior as the free-text search, just scoped to one field.
      if (f.itemCode && !String(r.ItemCode ?? '').toLowerCase().includes(f.itemCode)) return false;
      if (f.customerCode && !String(r.CustomerCode ?? '').toLowerCase().includes(f.customerCode)) return false;
      if (f.q) {
        const hay = `${r.OrderNumber ?? ''} ${r.CustomerName ?? ''} ${r.CustomerCode ?? ''} ${r.ItemCode ?? ''} ${r.ItemDescription ?? ''}`.toLowerCase();
        if (!hay.includes(f.q)) return false;
      }
      return true;
    });
  }

  function wireGlobalFilters() {
    const rerender = () => render();
    if ($('globalSearch')) $('globalSearch').addEventListener('input', rerender);
    if ($('filFamilia')) $('filFamilia').addEventListener('change', rerender);
    if ($('filItemCode')) $('filItemCode').addEventListener('input', rerender);
    if ($('filCustomerCode')) $('filCustomerCode').addEventListener('input', rerender);
    if ($('filAgente')) $('filAgente').addEventListener('change', rerender);
    if ($('filEstado')) $('filEstado').addEventListener('change', rerender);
    if ($('filOpenOnly')) $('filOpenOnly').addEventListener('change', rerender);
    if ($('trendBucket')) $('trendBucket').addEventListener('change', rerender);

    const origenRow = $('origenChips');
    if (origenRow) {
      ['Local', 'Extranjero', 'Otro'].forEach(label => {
        const chip = document.createElement('button');
        chip.className = 'chip';
        chip.textContent = label;
        chip.type = 'button';
        chip.addEventListener('click', () => {
          window.__activeOrigen = (window.__activeOrigen === label) ? '' : label;
          [...origenRow.children].forEach(c => c.classList.toggle('active', c === chip && window.__activeOrigen));
          rerender();
        });
        origenRow.appendChild(chip);
      });
    }

    const clearBtn = $('clearFiltersBtn');
    if (clearBtn) {
      clearBtn.addEventListener('click', () => {
        if ($('globalSearch')) $('globalSearch').value = '';
        if ($('filFamilia')) $('filFamilia').value = '';
        if ($('filItemCode')) $('filItemCode').value = '';
        if ($('filCustomerCode')) $('filCustomerCode').value = '';
        if ($('filAgente')) $('filAgente').value = '';
        if ($('filEstado')) $('filEstado').value = '';
        window.__activeOrigen = '';
        if (origenRow) [...origenRow.children].forEach(c => c.classList.remove('active'));
        activeBucket = null;
        const bucketChips = $('bucketChips');
        if (bucketChips) [...bucketChips.children].forEach(c => c.classList.remove('active'));
        rerender();
      });
    }
  }

  // ---------------------------------------------------------------------
  // Aggregation
  // ---------------------------------------------------------------------
  function isoDateToUtcMs(iso) {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(iso || '')) return NaN;
    const parts = iso.split('-').map(Number);
    return Date.UTC(parts[0], parts[1] - 1, parts[2]);
  }

  function daysLateFor(r, todayOverride) {
    const dateStr = r.LineShipDate || r.DueDate;
    const iso = normalizeFechaToISO(dateStr);
    if (!iso) return null;
    const todayIso = todayOverride
      ? normalizeFechaToISO(todayOverride)
      : formatLocalDateISO(new Date());
    const today = isoDateToUtcMs(todayIso);
    const due = isoDateToUtcMs(iso);
    if (isNaN(today) || isNaN(due)) return null;
    return Math.round((today - due) / 86400000);
  }

  function isOpenLine(r) {
    return (+r.OpenQty || 0) > 0 &&
      (!r.LineStatus || String(r.LineStatus).toUpperCase() === 'O');
  }

  function isWithinDateRange(value, from, to) {
    const iso = normalizeFechaToISO(value);
    return !!iso && !!from && !!to && iso >= from && iso <= to;
  }

  function lineKey(r) {
    return `${r.OrderDocEntry ?? r.OrderNumber ?? ''}|${r.LineNumber ?? ''}`;
  }

  function compareAllocationOrder(a, b) {
    const ad = normalizeFechaToISO(a.OrderDate) || '';
    const bd = normalizeFechaToISO(b.OrderDate) || '';
    if (ad !== bd) return ad < bd ? -1 : 1;
    const ao = String(a.OrderDocEntry ?? a.OrderNumber ?? '');
    const bo = String(b.OrderDocEntry ?? b.OrderNumber ?? '');
    const orderCompare = ao.localeCompare(bo, undefined, { numeric: true });
    if (orderCompare) return orderCompare;
    return (+a.LineNumber || 0) - (+b.LineNumber || 0);
  }

  function computeTentativeAvailability(rows) {
    const byItem = {};
    (rows || []).filter(isOpenLine).forEach(r => {
      const item = r.ItemCode || 'Sin código';
      (byItem[item] = byItem[item] || []).push(r);
    });

    const result = new Map();
    Object.values(byItem).forEach(itemRows => {
      itemRows.sort(compareAllocationOrder);
      const knownStock = itemRows.find(r =>
        r.StockOnHand !== null &&
        r.StockOnHand !== undefined &&
        r.StockOnHand !== '');
      let remaining = knownStock ? (+knownStock.StockOnHand || 0) : null;

      itemRows.forEach(r => {
        result.set(
          lineKey(r),
          remaining === null ? null : Math.max(0, remaining));
        if (remaining !== null) {
          remaining = Math.max(0, remaining - (+r.OpenQty || 0));
        }
      });
    });
    return result;
  }

  function bucketFor(daysLate) {
    if (daysLate === null) return 'Sin fecha';
    if (daysLate > 0) return 'Vencido';
    if (daysLate >= -7) return 'Vence en 7 días';
    if (daysLate >= -30) return 'Vence en 30 días';
    return 'Vence después';
  }

  const bucketOrder = ['Vencido', 'Vence en 7 días', 'Vence en 30 días', 'Vence después', 'Sin fecha'];
  const bucketColorVar = { 'Vencido': '--critical', 'Vence en 7 días': '--serious', 'Vence en 30 días': '--warning', 'Vence después': '--good', 'Sin fecha': '--text-muted' };

  function computeAll(openRows, allRowsForTrend, allocation, options) {
    options = options || {};

    // ---- KPIs ----
    const totalOpenQty = openRows.reduce((a, r) => a + (+r.OpenQty || 0), 0);
    const withDaysLate = openRows.map(r => ({
      r,
      daysLate: daysLateFor(r, options.today)
    }));
    const vencidos = withDaysLate.filter(x => x.daysLate !== null && x.daysLate > 0);
    const due7 = withDaysLate.filter(x => x.daysLate !== null && x.daysLate <= 0 && x.daysLate >= -7);
    const clientes = new Set(openRows.map(r => r.CustomerName).filter(Boolean));
    const familias = new Set(openRows.map(r => r.FamilyName).filter(Boolean));
    const localQty = openRows.filter(r => r.Origen === 'Local').reduce((a, r) => a + (+r.OpenQty || 0), 0);
    const extQty = openRows.filter(r => r.Origen === 'Extranjero').reduce((a, r) => a + (+r.OpenQty || 0), 0);
    const localPct = (localQty + extQty) > 0 ? Math.round(localQty / (localQty + extQty) * 100) : null;

    const kpi = {
      openLines: openRows.length,
      openQty: totalOpenQty,
      vencidoLines: vencidos.length,
      vencidoQty: vencidos.reduce((a, x) => a + (+x.r.OpenQty || 0), 0),
      due7Lines: due7.length,
      due7Qty: due7.reduce((a, x) => a + (+x.r.OpenQty || 0), 0),
      clientes: clientes.size,
      familias: familias.size,
      localPct
    };

    // ---- bucket chart/table ----
    const bucketAgg = {};
    const backlogTable = [];
    withDaysLate.forEach(({ r, daysLate }) => {
      const b = bucketFor(daysLate);
      if (!bucketAgg[b]) bucketAgg[b] = { lines: 0, qty: 0 };
      bucketAgg[b].lines++;
      bucketAgg[b].qty += (+r.OpenQty || 0);
      const qtyOrdered = +r.OrderedQty || 0;
      const qtyPending = +r.OpenQty || 0;
      backlogTable.push({
        order: r.OrderNumber, customer: r.CustomerName, item: r.ItemCode, desc: r.ItemDescription,
        orderDocEntry: r.OrderDocEntry, lineNumber: r.LineNumber,
        origen: r.Origen, familia: r.FamilyName,
        orderDate: normalizeFechaToISO(r.OrderDate) || '—',
        agent: r.SalesAgent || '—',
        estado: r.CustomerState || '—',
        qtyOrdered,
        // Dispatched = ordered minus what's still pending. Clamped at 0 in
        // case a line's OpenQty ever exceeds Quantity (returns/edits) — that
        // shouldn't happen but showing -3 "dispatched" would be worse.
        qtyDispatched: Math.max(0, qtyOrdered - qtyPending),
        qty: qtyPending,
        due: normalizeFechaToISO(r.LineShipDate || r.DueDate) || '—',
        daysLate, bucket: b,
        // stockOnHand is an ITEM-level number (same value repeats on every
        // line for that item) — raw as pulled from OITW, unallocated.
        // tentativeAvailable is computed below, per item, in order-date
        // order — see the note there.
        stockOnHand: r.StockOnHand === null || r.StockOnHand === undefined || r.StockOnHand === '' ? null : (+r.StockOnHand || 0),
        // Raw "Bodega X: 200 · Bodega Y: 118" text from StockByWarehouse
        // (backlog.sql, STRING_AGG) — parsed into a tooltip on hover, see
        // wireWhTooltips() below. Null/blank just means no per-warehouse
        // breakdown available for that item.
        whBreakdown: r.StockByWarehouse || null,
        tentativeAvailable: allocation && allocation.has(lineKey(r))
          ? allocation.get(lineKey(r))
          : null
      });
    });
    const bucket = bucketOrder.filter(b => bucketAgg[b]).map(b => ({ label: b, lines: bucketAgg[b].lines, qty: bucketAgg[b].qty }));

    // ---- item demand ----
    const itemAgg = {};
    openRows.forEach(r => {
      const k = r.ItemCode || 'Sin código';
      if (!itemAgg[k]) itemAgg[k] = { desc: r.ItemDescription || '', qty: 0, orders: new Set() };
      itemAgg[k].qty += (+r.OpenQty || 0);
      if (r.OrderNumber) itemAgg[k].orders.add(r.OrderNumber);
    });
    const itemTable = Object.entries(itemAgg).map(([item, v]) => ({ item, desc: v.desc, qty: v.qty, orders: v.orders.size }));

    // ---- trend (Día/Semana/Mes/Año, selectable) + seasonality (uses
    // allRowsForTrend — not open-only, matching the original report's "all
    // statuses, trailing history"). This is also the "unidades vendidas por
    // tiempo, dado un item o familia" view Luigi asked for — it already
    // respects the item code / family filters above, since allRowsForTrend
    // is the same globally-filtered set every other section uses. ----
    const trendBucket = options.trendBucket ||
      (($('trendBucket') && $('trendBucket').value) || 'month');
    const trendAgg = {};
    allRowsForTrend.forEach(r => {
      const iso = normalizeFechaToISO(r.OrderDate);
      if (!iso) return;
      const key = trendBucketKey(iso, trendBucket);
      if (!trendAgg[key]) trendAgg[key] = { qty: 0, orders: new Set() };
      trendAgg[key].qty += (+r.OrderedQty || 0);
      if (r.OrderNumber) trendAgg[key].orders.add(r.OrderNumber);
    });
    const trendKeys = Object.keys(trendAgg).sort();
    const currentIso = options.today
      ? normalizeFechaToISO(options.today)
      : formatLocalDateISO(new Date());
    const nowKey = trendBucketKey(currentIso, trendBucket);
    const monthly = trendKeys.map(k => ({
      month: trendBucketLabel(k, trendBucket),
      value: trendAgg[k].qty, orders: trendAgg[k].orders.size, partial: k === nowKey
    }));

    const seasonAgg = {};
    allRowsForTrend.forEach(r => {
      const iso = normalizeFechaToISO(r.OrderDate);
      if (!iso) return;
      const m = +iso.slice(5, 7);
      if (!seasonAgg[m]) seasonAgg[m] = { sum: 0, months: new Set() };
      seasonAgg[m].sum += (+r.OrderedQty || 0);
      seasonAgg[m].months.add(iso.slice(0, 7));
    });
    const seasonality = Object.keys(seasonAgg).map(Number).sort((a, b) => a - b).map(m => ({
      month: MESES[m - 1],
      avgQty: seasonAgg[m].months.size ? seasonAgg[m].sum / seasonAgg[m].months.size : 0
    }));

    return { kpi, bucket, backlogTable, itemTable, monthly, seasonality };
  }

  // ---------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------
  function renderKpis(kpi) {
    const row = $('kpiRow');
    if (!row) return;
    row.innerHTML = '';
    const tiles = [
      { label: 'Líneas abiertas', value: fmt(kpi.openLines), sub: `${fmt(kpi.openQty)} unidades abiertas` },
      { label: 'Vencidas', value: fmt(kpi.vencidoLines), sub: `${fmt(kpi.vencidoQty)} unidades`, cls: 'tile-critical' },
      { label: 'Vencen en 7 días', value: fmt(kpi.due7Lines), sub: `${fmt(kpi.due7Qty)} unidades`, cls: 'tile-warning' },
      { label: 'Clientes distintos', value: fmt(kpi.clientes), sub: 'con líneas abiertas' },
      { label: 'Familias distintas', value: fmt(kpi.familias), sub: 'con líneas abiertas' },
      { label: '% Local', value: kpi.localPct === null ? '—' : kpi.localPct + '%', sub: 'de la cantidad abierta (CE vs CL)' },
    ];
    tiles.forEach(t => {
      const d = document.createElement('div');
      d.className = 'stat-tile' + (t.cls ? ' ' + t.cls : '');
      d.innerHTML = '<div class="stat-label"></div><div class="stat-value"></div><div class="stat-sub"></div>';
      d.querySelector('.stat-label').textContent = t.label;
      d.querySelector('.stat-value').textContent = t.value;
      d.querySelector('.stat-sub').textContent = t.sub;
      row.appendChild(d);
    });
  }

  let lastComputed = null;

  function render() {
    if (!$('kpiRow')) return;
    if (raw.length === 0) {
      renderKpis({ openLines: 0, openQty: 0, vencidoLines: 0, vencidoQty: 0, due7Lines: 0, due7Qty: 0, clientes: 0, familias: 0, localPct: null });
      ['bucketChart', 'itemChart', 'trendChart', 'seasonChart'].forEach(id => { if ($(id)) $(id).innerHTML = ''; });
      if ($('backlogTable')) $('backlogTable').querySelector('tbody').innerHTML = '';
      if ($('itemTable')) $('itemTable').querySelector('tbody').innerHTML = '';
      if ($('status')) $('status').textContent = 'Sin datos cargados.';
      return;
    }

    const from = $('from') ? $('from').value : '';
    const to = $('to') ? $('to').value : '';
    const trendRows = raw.filter(r =>
      isWithinDateRange(r.OrderDate, from, to));
    const allFiltered = applyGlobalFilters(
      trendRows,
      { openOnlyRelevant: false });
    const visibleRows = applyGlobalFilters(raw);
    const allocation = computeTentativeAvailability(raw);
    lastComputed = computeAll(visibleRows, allFiltered, allocation);
    populateCodeSuggestions();

    if ($('status')) {
      const onlyOpen = getGlobalFilters().openOnly;
      const baseCount = onlyOpen
        ? raw.filter(isOpenLine).length
        : raw.length;
      $('status').textContent =
        `${visibleRows.length.toLocaleString('es-GT')} de ${baseCount.toLocaleString('es-GT')} ` +
        `${onlyOpen ? 'líneas abiertas' : 'líneas cargadas'} · filtros aplicados`;
    }

    renderKpis(lastComputed.kpi);
    renderBucketChart();
    renderBacklogTable();
    renderItemChart();
    renderItemTable();
    lineChart($('trendChart'), lastComputed.monthly, { height: 230, showTrend: true });
    verticalBarChart($('seasonChart'), lastComputed.seasonality.map(s => ({ label: s.month, value: s.avgQty })), { height: 230 });
  }

  function renderBucketChart() {
    const items = lastComputed.bucket.map(b => ({
      label: b.label, value: b.qty, color: cssVar(bucketColorVar[b.label]),
      valueLabel: compact(b.qty) + ' unid.',
      tooltip: `<div class="t-row"><span>Líneas</span><span class="t-val">${fmt(b.lines)}</span></div><div class="t-row"><span>Cantidad abierta</span><span class="t-val">${fmt(b.qty)}</span></div>`
    }));
    horizontalBarChart($('bucketChart'), items, { barH: 26, gap: 16, leftPad: 140 });

    const bucketChips = $('bucketChips');
    if (bucketChips) {
      bucketChips.innerHTML = '';
      lastComputed.bucket.forEach(b => {
        const chip = document.createElement('button');
        chip.className = 'chip' +
          (activeBucket === b.label ? ' active' : '');
        chip.type = 'button';
        chip.textContent = b.label;
        chip.addEventListener('click', () => {
          activeBucket = (activeBucket === b.label) ? null : b.label;
          [...bucketChips.children].forEach(c => c.classList.toggle('active', c === chip && activeBucket));
          renderBacklogTable();
        });
        bucketChips.appendChild(chip);
      });
    }
  }

  const backlogCols = [
    { key: 'order' }, { key: 'customer' }, { key: 'item' }, { key: 'desc' },
    { key: 'origen', html: r => `<span class="badge badge-neutral">${escapeHtml(r.origen || '—')}</span>` },
    { key: 'orderDate' },
    { key: 'agent' },
    { key: 'estado' },
    { key: 'qtyOrdered', num: true, render: r => fmt(r.qtyOrdered) },
    { key: 'qtyDispatched', num: true, render: r => fmt(r.qtyDispatched) },
    { key: 'qty', num: true, render: r => fmt(r.qty) },
    { key: 'due' },
    {
      key: 'daysLate', num: true, html: r => {
        if (r.daysLate === null) return '<span class="badge badge-neutral">Sin fecha</span>';
        const cls = r.daysLate > 0 ? 'badge-critical' : (r.daysLate >= -7 ? 'badge-serious' : (r.daysLate >= -30 ? 'badge-warning' : 'badge-good'));
        const label = r.daysLate > 0 ? (r.daysLate + 'd tarde') : (Math.abs(r.daysLate) + 'd restantes');
        return `<span class="badge ${cls}">${label}</span>`;
      }
    },
    {
      key: 'stockOnHand', num: true, html: r => {
        if (r.stockOnHand === null || r.stockOnHand === undefined) return '—';
        // Hoverable when we have a per-warehouse breakdown (see
        // wireWhTooltips) — dotted underline hints it's interactive.
        return r.whBreakdown
          ? `<span class="wh-hover" data-wh="${escapeHtml(r.whBreakdown)}">${fmt(r.stockOnHand)}</span>`
          : fmt(r.stockOnHand);
      }
    },
    {
      key: 'tentativeAvailable', num: true, html: r => {
        const v = r.tentativeAvailable;
        if (v === null || v === undefined) return '—';
        // Visual cue: if the tentative available stock can't cover this
        // line's own pending quantity, this line is (tentatively) short.
        const short = v < r.qty;
        return `<span class="${short ? 'badge badge-warning' : ''}">${fmt(v)}</span>`;
      }
    },
  ];
  let backlogTableCtl = null;

  // ---------------------------------------------------------------------
  // Per-warehouse stock tooltip — hover over "Stock actual" to see the
  // breakdown. One delegated listener on the table (wired once in init())
  // rather than per-cell, since the table body gets rebuilt on every
  // render/sort/filter.
  // ---------------------------------------------------------------------
  function whTooltipHtml(dataWh) {
    const lines = dataWh.split(' · ').filter(Boolean).map(part => {
      const idx = part.lastIndexOf(':');
      const name = idx === -1 ? part : part.slice(0, idx).trim();
      const qty = idx === -1 ? '' : part.slice(idx + 1).trim();
      return `<div class="t-row"><span>${escapeHtml(name)}</span><span class="t-val">${escapeHtml(qty)}</span></div>`;
    }).join('');
    return `<div class="t-title">Stock por bodega</div>${lines}`;
  }

  function wireWhTooltips() {
    const table = $('backlogTable');
    if (!table || table.dataset.whWired) return;
    table.dataset.whWired = '1';
    table.addEventListener('pointermove', (e) => {
      const el = e.target.closest('.wh-hover');
      if (!el || !el.dataset.wh) return;
      showTip(e.clientX, e.clientY, whTooltipHtml(el.dataset.wh));
    });
    table.addEventListener('pointerout', (e) => {
      const el = e.target.closest('.wh-hover');
      const to = e.relatedTarget;
      if (el && (!to || !el.contains(to))) hideTip();
    });
  }

  function renderBacklogTable() {
    if (!backlogTableCtl) {
      backlogTableCtl = makeTable($('backlogTable'), $('backlogRowCount'), backlogCols, {
        maxRows: 500,
        onSort: (key) => { if (backlogSort.key === key) backlogSort.dir = -backlogSort.dir; else backlogSort = { key, dir: 1 }; renderBacklogTable(); },
        getSortDir: () => backlogSort.dir
      });
    }
    const searchEl = $('backlogSearch');
    const q = searchEl ? searchEl.value.trim().toLowerCase() : '';
    let rows = lastComputed.backlogTable;
    if (activeBucket) rows = rows.filter(r => r.bucket === activeBucket);
    if (q) rows = rows.filter(r => String(r.order).toLowerCase().includes(q) || (r.customer || '').toLowerCase().includes(q) || (r.item || '').toLowerCase().includes(q) || (r.desc || '').toLowerCase().includes(q));
    rows = sortRows(rows, backlogSort.key, backlogSort.dir);
    backlogTableCtl.render(rows);
  }

  function renderItemChart() {
    const top = [...lastComputed.itemTable].sort((a, b) => b.qty - a.qty).slice(0, 15);
    const items = top.map(it => ({
      label: it.item, value: it.qty, color: cssVar('--series-1'),
      valueLabel: compact(it.qty),
      tooltip: `<div class="t-title" style="max-width:220px;white-space:normal;">${escapeHtml(it.item)}${it.desc ? ' — ' + escapeHtml(it.desc) : ''}</div>
        <div class="t-row"><span>Cantidad abierta</span><span class="t-val">${fmt(it.qty)}</span></div>
        <div class="t-row"><span>Pedidos</span><span class="t-val">${fmt(it.orders)}</span></div>`
    }));
    horizontalBarChart($('itemChart'), items, { barH: 16, gap: 8, leftPad: 90 });
  }

  const itemCols = [
    { key: 'item' }, { key: 'desc' },
    { key: 'qty', num: true, render: r => fmt(r.qty) },
    { key: 'orders', num: true, render: r => fmt(r.orders) },
  ];
  let itemTableCtl = null;

  function renderItemTable() {
    if (!itemTableCtl) {
      itemTableCtl = makeTable($('itemTable'), $('itemRowCount'), itemCols, {
        maxRows: 500,
        onSort: (key) => { if (itemSort.key === key) itemSort.dir = -itemSort.dir; else itemSort = { key, dir: 1 }; renderItemTable(); },
        getSortDir: () => itemSort.dir
      });
    }
    const searchEl = $('itemSearch');
    const q = searchEl ? searchEl.value.trim().toLowerCase() : '';
    let rows = lastComputed.itemTable;
    if (q) rows = rows.filter(r => (r.item || '').toLowerCase().includes(q) || (r.desc || '').toLowerCase().includes(q));
    rows = sortRows(rows, itemSort.key, itemSort.dir);
    itemTableCtl.render(rows);
  }

  function wireTableToggles() {
    [['backlogTableToggle', 'backlogTableWrap'], ['itemTableToggle', 'itemTableWrap']].forEach(([btnId, wrapId]) => {
      const btn = $(btnId), wrap = $(wrapId);
      if (!btn || !wrap) return;
      btn.addEventListener('click', () => {
        const hidden = wrap.style.display === 'none';
        wrap.style.display = hidden ? '' : 'none';
        btn.textContent = hidden ? 'Ocultar tabla' : 'Mostrar tabla';
      });
    });
    if ($('backlogSearch')) $('backlogSearch').addEventListener('input', renderBacklogTable);
    if ($('itemSearch')) $('itemSearch').addEventListener('input', renderItemTable);
  }

  function setRawData(rows) {
    raw = (Array.isArray(rows) ? rows : []).map(r => ({
      ...r,
      OrderDate: normalizeFechaToISO(r.OrderDate),
      DueDate: normalizeFechaToISO(r.DueDate),
      LineShipDate: normalizeFechaToISO(r.LineShipDate)
    }));
    activeBucket = null;
    populateGlobalFilterOptions();
    render();
  }

  function init() {
    wireThemeToggle();
    wireGlobalFilters();
    wireTableToggles();
    wireWhTooltips();
    window.addEventListener('resize', () => {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(() => {
        if (raw.length) render();
      }, 120);
    });
  }

  return {
    setRawData,
    init,
    render,
    applyPalette,
    setDefaultDateRange,
    __test: {
      normalizeFechaToISO,
      formatLocalDateISO,
      isoWeek,
      trendBucketKey,
      trendBucketLabel,
      daysLateFor,
      bucketFor,
      isOpenLine,
      isWithinDateRange,
      lineKey,
      computeTentativeAvailability,
      computeAll
    }
  };
})();

if (typeof module !== 'undefined' && module.exports) {
  module.exports = BacklogViz;
}
