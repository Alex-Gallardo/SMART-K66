// =============================================================================
// OTIF dashboard — shared rendering logic (used by Index.cshtml).
//
// Visual system and chart primitives follow backlog-viz.js so this tool
// feels like the same suite —
// same dark-by-default theme, same per-empresa accent, same table/export
// behavior. Only the OTIF-specific computation and the customer detail
// panel (styled after the slide deck we built earlier) are new.
//
// Unlike Backlog, this tool evaluates all order lines and their individual
// delivery events (via ODLN/DLN1, see otif.sql). A line enters the OTIF
// denominator once it has an actual delivery record. Lines SAP marks Closed with
// no linked delivery, and lines still Open past their promised date, are
// tracked separately and never silently counted as a success or failure.
//
// Expected row shape (from otif.sql aliases):
//   OrderDocEntry, OrderNumber, CustomerCode, CustomerName, Origen, SalesAgentCode,
//   SalesAgent, LineNumber, ItemCode, ItemDescription, FamilyCode,
//   FamilyName, OrderDate, DueDate, LineShipDate, OrderedQty, OpenQty,
//   LineStatus, DeliveryDocEntry, DeliveryLineNumber, DeliveryDate, DeliveryQty,
//   UnitOfMeasure
// =============================================================================

const OTIFViz = (() => {
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
  const pctStr = (p) => {
    if (p === null || p === undefined || isNaN(p)) return '—';
    const v = p * 100;
    const s = v.toFixed(1);
    return (s.endsWith('.0') ? s.slice(0, -2) : s) + '%';
  };

  let raw = [];
  let compareRaw = null; // set when compare mode is on and the prior-period fetch has returned
  let custSort = { key: 'total', dir: -1 };
  let itemSort = { key: 'total', dir: -1 };

  // ---------------------------------------------------------------------
  // Date normalization — ported from backlog-viz.js verbatim.
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
    if (!isNaN(d)) return d.toISOString().slice(0, 10);
    return null;
  }

  function escapeHtml(s) {
    const d = document.createElement('div');
    d.textContent = s == null ? '' : String(s);
    return d.innerHTML.replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  function localTodayISO() {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  function addDaysISO(iso, days) {
    const d = new Date(iso + 'T00:00:00Z');
    d.setUTCDate(d.getUTCDate() + days);
    return d.toISOString().slice(0, 10);
  }

  function mondayOfISO(iso) {
    const d = new Date(iso + 'T00:00:00Z');
    const dow = d.getUTCDay(); // 0=Sun..6=Sat
    const diff = dow === 0 ? -6 : 1 - dow;
    d.setUTCDate(d.getUTCDate() + diff);
    return d.toISOString().slice(0, 10);
  }

  // ---------------------------------------------------------------------
  // Tema por empresa — same mechanism as Backlog.
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

  const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

  // ---------------------------------------------------------------------
  // Rango de fechas mostrado en la tarjeta de detalle. El host (Index.cshtml
  // o local.html) es quien sabe cuál es el rango real — via.setDateRange —
  // este módulo solo lo guarda y lo muestra.
  // ---------------------------------------------------------------------
  let currentRange = { from: null, to: null, compareFrom: null, compareTo: null };

  function fmtDateNice(iso) {
    if (!iso) return '';
    const [y, m, d] = String(iso).slice(0, 10).split('-').map(Number);
    if (!y || !m || !d) return iso;
    return `${d} ${MESES[m - 1]} ${y}`;
  }

  function setDateRange(from, to, compareFrom, compareTo) {
    currentRange = { from: from || null, to: to || null, compareFrom: compareFrom || null, compareTo: compareTo || null };
    if (raw.length) render();
  }

  function dateBoundsOf(rows) {
    // Coincide con el filtro de otif.sql: fecha PROMETIDA (ShipDate de la
    // línea, si no DocDueDate del pedido), no fecha de creación del pedido.
    const dates = rows.map(r => r.LineShipDate || r.DueDate).filter(Boolean).sort();
    return dates.length ? { from: dates[0], to: dates[dates.length - 1] } : { from: null, to: null };
  }

  function getDateBounds(which) {
    return which === 'compare' ? dateBoundsOf(compareRaw || []) : dateBoundsOf(raw);
  }

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
  // Theme toggle — ported verbatim.
  // ---------------------------------------------------------------------
  function wireThemeToggle() {
    const root = document.documentElement;
    const btn = $('themeToggle');
    if (!btn) return;
    btn.addEventListener('click', () => {
      const isLight = root.getAttribute('data-theme') === 'light';
      if (isLight) { root.setAttribute('data-theme', 'dark'); btn.textContent = 'Cambiar a claro'; }
      else { root.setAttribute('data-theme', 'light'); btn.textContent = 'Cambiar a oscuro'; }
    });
  }

  // ---------------------------------------------------------------------
  // Tooltip — ported verbatim.
  // ---------------------------------------------------------------------
  function showTip(x, y, html) {
    const tip = $('tooltip');
    if (!tip) return;
    tip.innerHTML = html;
    tip.classList.add('show');
    const rect = tip.getBoundingClientRect();
    let left = x + 14, top = y - rect.height / 2;
    if (left + rect.width > window.innerWidth - 8) left = x - rect.width - 14;
    top = Math.max(8, Math.min(top, window.innerHeight - rect.height - 8));
    tip.style.left = left + 'px';
    tip.style.top = top + 'px';
  }
  function hideTip() { const t = $('tooltip'); if (t) t.classList.remove('show'); }

  // ---------------------------------------------------------------------
  // Line chart — ported from backlog-viz.js, generalized to plot any
  // 0..1 ratio (OTIF%) instead of a raw quantity: pass opts.isPct=true to
  // format the axis/tooltip as a percentage instead of compact(n).
  // ---------------------------------------------------------------------
  function lineChart(container, points, opts) {
    opts = opts || {};
    const width = container.clientWidth || 600;
    const height = opts.height || 220;
    const leftPad = 46, rightPad = 12, topPad = 16, botPad = 30;
    if (!points.length) { container.innerHTML = '<div class="empty-state">Sin datos para los filtros seleccionados.</div>'; return; }
    const plotW = width - leftPad - rightPad;
    const plotH = height - topPad - botPad;
    const maxVal = opts.isPct ? 1 : Math.max(...points.map(p => p.value), 1) * 1.12;
    const x = i => points.length > 1 ? leftPad + (i / (points.length - 1)) * plotW : leftPad + plotW / 2;
    const y = v => topPad + plotH - (v / maxVal) * plotH;
    const fmtAxis = v => opts.isPct ? Math.round(v * 100) + '%' : compact(v);

    const ticks = 4;
    let gridSvg = '';
    for (let t = 0; t <= ticks; t++) {
      const v = (maxVal / ticks) * t;
      const yy = y(v);
      gridSvg += `<line class="grid-line" x1="${leftPad}" x2="${width - rightPad}" y1="${yy}" y2="${yy}"></line>`;
      gridSvg += `<text class="axis-label" x="${leftPad - 8}" y="${yy + 3}" text-anchor="end">${fmtAxis(v)}</text>`;
    }

    let path = '';
    points.forEach((p, i) => { path += (i === 0 ? 'M' : 'L') + x(i) + ' ' + y(p.value) + ' '; });

    const step = Math.max(1, Math.ceil(points.length / 10));
    let labels = '';
    points.forEach((p, i) => {
      if (i % step === 0 || i === points.length - 1) {
        labels += `<text class="axis-label" x="${x(i)}" y="${height - 6}" text-anchor="middle">${p.label}</text>`;
      }
    });

    let dots = '';
    points.forEach((p, i) => {
      const color = p.color || cssVar('--series-1');
      dots += `<circle data-idx="${i}" cx="${x(i)}" cy="${y(p.value)}" r="4" fill="${p.partial ? cssVar('--page-plane') : color}" stroke="${color}" stroke-width="2"></circle>`;
      dots += `<rect data-idx="${i}" x="${x(i) - 12}" y="${topPad}" width="24" height="${plotH}" fill="transparent" class="hit-col"></rect>`;
    });

    container.innerHTML = `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
      ${gridSvg}
      <path d="${path}" fill="none" stroke="${cssVar('--series-1')}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"></path>
      ${dots}
      ${labels}
      <line class="axis-line" x1="${leftPad}" x2="${width - rightPad}" y1="${topPad + plotH}" y2="${topPad + plotH}"></line>
    </svg>`;

    container.querySelectorAll('.hit-col').forEach(el => {
      const idx = +el.dataset.idx;
      const p = points[idx];
      el.addEventListener('pointermove', (e) => {
        showTip(e.clientX, e.clientY, `<div class="t-title">${p.label}${p.partial ? ' (parcial)' : ''}</div>
          <div class="t-row"><span>${opts.metricLabel || 'Valor'}</span><span class="t-val">${opts.isPct ? pctStr(p.value) : fmt(p.value)}</span></div>
          ${p.n !== undefined ? `<div class="t-row"><span>Cumplidas</span><span class="t-val">${fmt(p.n)}</span></div>` : ''}`);
      });
      el.addEventListener('pointerleave', hideTip);
    });
  }

  // ---------------------------------------------------------------------
  // Sortable/filterable table helper — ported verbatim.
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
        if (opts.rowClick) {
          tr.style.cursor = 'pointer';
          tr.addEventListener('click', () => opts.rowClick(r));
        }
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
  // Global filters
  // ---------------------------------------------------------------------
  function populateGlobalFilterOptions() {
    const familias = new Set();
    raw.forEach(r => { if (r.FamilyName) familias.add(r.FamilyName); });
    const famSel = $('filFamilia');
    if (famSel) {
      const current = famSel.value;
      famSel.replaceChildren(new Option('Todas las familias', ''), ...[...familias].sort().map(f => new Option(f, f)));
      if ([...familias].includes(current)) famSel.value = current;
    }
    const agentes = new Set();
    raw.forEach(r => { if (r.SalesAgent) agentes.add(r.SalesAgent); });
    const agentSel = $('filAgente');
    if (agentSel) {
      const current = agentSel.value;
      agentSel.replaceChildren(new Option('Todos los vendedores', ''), ...[...agentes].sort().map(a => new Option(a, a)));
      if ([...agentes].includes(current)) agentSel.value = current;
    }
  }

  function populateCodeSuggestions() {
    const itemList = $('itemCodeList');
    const custList = $('customerCodeList');
    if (!itemList && !custList) return;
    const f = getGlobalFilters();
    const commonPass = (r) => {
      if (f.familia && r.FamilyName !== f.familia) return false;
      if (f.origen && r.Origen !== f.origen) return false;
      if (f.agente && r.SalesAgent !== f.agente) return false;
      return true;
    };
    if (itemList) {
      const codes = new Set();
      raw.forEach(r => {
        if (!commonPass(r)) return;
        if (f.customerCode && !String(r.CustomerCode ?? '').toLowerCase().includes(f.customerCode)) return;
        if (r.ItemCode) codes.add(r.ItemCode);
      });
      itemList.replaceChildren(...[...codes].sort().slice(0, 500).map(c => new Option('', c)));
    }
    if (custList) {
      const codes = new Set();
      raw.forEach(r => {
        if (!commonPass(r)) return;
        if (f.itemCode && !String(r.ItemCode ?? '').toLowerCase().includes(f.itemCode)) return;
        if (r.CustomerCode) codes.add(r.CustomerCode);
      });
      custList.replaceChildren(...[...codes].sort().slice(0, 500).map(c => new Option('', c)));
    }
  }

  function getGlobalFilters() {
    const q = ($('globalSearch') && $('globalSearch').value.trim().toLowerCase()) || '';
    const familia = ($('filFamilia') && $('filFamilia').value) || '';
    const itemCode = ($('filItemCode') && $('filItemCode').value.trim().toLowerCase()) || '';
    const customerCode = ($('filCustomerCode') && $('filCustomerCode').value.trim().toLowerCase()) || '';
    const agente = ($('filAgente') && $('filAgente').value) || '';
    const origen = window.__otifActiveOrigen || '';
    const fillValue = $('fillThreshold') && $('fillThreshold').value !== '' ? Number($('fillThreshold').value) : 100;
    const toleranceValue = $('toleranceDays') ? Number($('toleranceDays').value) : 0;
    const fillThreshold = Math.min(1, Math.max(0, Number.isFinite(fillValue) ? fillValue / 100 : 1));
    const toleranceDays = Math.min(365, Math.max(0, Number.isFinite(toleranceValue) ? Math.trunc(toleranceValue) : 0));
    const mode = ($('otifMode') && $('otifMode').value === 'order') ? 'order' : 'line';
    return { q, familia, itemCode, customerCode, agente, origen, fillThreshold, toleranceDays, mode };
  }

  function applyGlobalFilters(rows, f) {
    return rows.filter(r => {
      if (f.familia && r.FamilyName !== f.familia) return false;
      if (f.origen && r.Origen !== f.origen) return false;
      if (f.agente && r.SalesAgent !== f.agente) return false;
      if (f.itemCode && !String(r.ItemCode ?? '').toLowerCase().includes(f.itemCode)) return false;
      if (f.customerCode && !String(r.CustomerCode ?? '').toLowerCase().includes(f.customerCode)) return false;
      if (f.q) {
        const hay = `${r.OrderNumber ?? ''} ${r.CustomerName ?? ''} ${r.CustomerCode ?? ''} ${r.ItemCode ?? ''} ${r.ItemDescription ?? ''}`.toLowerCase();
        if (!hay.includes(f.q)) return false;
      }
      return true;
    });
  }

  function orderKey(r) { return String(r.OrderDocEntry); }
  function promisedDate(r) { return r.LineShipDate || r.DueDate; }
  function inRange(date, range) {
    return !!date && (!range.from || date >= range.from) && (!range.to || date <= range.to);
  }

  function selectRowsForMode(rows, f, range) {
    if (f.mode !== 'order') {
      const selected = applyGlobalFilters(rows.filter(r => inRange(promisedDate(r), range)), f);
      return { rows: selected, itemRows: selected };
    }
    const matchingOrders = new Set(applyGlobalFilters(rows, f).map(orderKey));
    const latestPromise = new Map();
    rows.forEach(r => {
      const key = orderKey(r), date = promisedDate(r);
      if (date && (!latestPromise.has(key) || date > latestPromise.get(key))) latestPromise.set(key, date);
    });
    const selected = rows.filter(r => matchingOrders.has(orderKey(r)) && inRange(latestPromise.get(orderKey(r)), range));
    return { rows: selected, itemRows: applyGlobalFilters(selected, f) };
  }

  function wireGlobalFilters() {
    const rerender = () => render();
    ['globalSearch', 'filItemCode', 'filCustomerCode', 'fillThreshold', 'toleranceDays'].forEach(id => {
      if ($(id)) $(id).addEventListener('input', rerender);
    });
    ['filFamilia', 'filAgente', 'trendBucket', 'otifMode'].forEach(id => {
      if ($(id)) $(id).addEventListener('change', rerender);
    });

    const origenRow = $('origenChips');
    if (origenRow) {
      ['Local', 'Extranjero', 'Otro'].forEach(label => {
        const chip = document.createElement('button');
        chip.className = 'chip';
        chip.textContent = label;
        chip.type = 'button';
        chip.addEventListener('click', () => {
          window.__otifActiveOrigen = (window.__otifActiveOrigen === label) ? '' : label;
          [...origenRow.children].forEach(c => c.classList.toggle('active', c === chip && window.__otifActiveOrigen));
          rerender();
        });
        origenRow.appendChild(chip);
      });
    }

    const clearBtn = $('clearFiltersBtn');
    if (clearBtn) {
      clearBtn.addEventListener('click', () => {
        ['globalSearch', 'filItemCode', 'filCustomerCode'].forEach(id => { if ($(id)) $(id).value = ''; });
        if ($('filFamilia')) $('filFamilia').value = '';
        if ($('filAgente')) $('filAgente').value = '';
        window.__otifActiveOrigen = '';
        if (origenRow) [...origenRow.children].forEach(c => c.classList.remove('active'));
        rerender();
      });
    }
  }

  // ---------------------------------------------------------------------
  // Per-row OTIF computation. The on-time quantity must reach the configured
  // threshold by the promise date plus tolerance; lifetime fill is separate.
  // ---------------------------------------------------------------------
  function computeRow(r, f, todayISO) {
    const promised = r.LineShipDate || r.DueDate;
    if (!promised) return { ...r, promised: null, month: null, week: null, cumplida: false, onTime: null, fillPct: null, complete: null, otif: null, closedNoDelivery: false, openOverdue: false };

    const deliveries = r.deliveries || [];
    const cumplida = deliveries.some(d => d.qty > 0);
    let onTime = null, complete = null, otif = null, fillPct = null;
    if (cumplida) {
      const deadline = addDaysISO(promised, f.toleranceDays);
      onTime = deliveries.some(d => d.qty > 0 && d.date <= deadline);
      const ordered = +r.OrderedQty || 0;
      const delivered = +r.DeliveredQty || 0;
      const deliveredByDeadline = deliveries.filter(d => d.date <= deadline).reduce((sum, d) => sum + d.qty, 0);
      fillPct = ordered > 0 ? delivered / ordered : null;
      complete = fillPct !== null ? fillPct >= f.fillThreshold : null;
      otif = ordered > 0 && onTime && deliveredByDeadline / ordered >= f.fillThreshold;
    }
    const closedNoDelivery = r.LineStatus === 'C' && !cumplida;
    const openOverdue = r.LineStatus === 'O' && (+r.OpenQty || 0) > 0 && promised < todayISO;

    return {
      ...r, promised,
      month: promised.slice(0, 7),
      week: mondayOfISO(promised),
      cumplida, onTime, fillPct, complete, otif, closedNoDelivery, openOverdue
    };
  }

  // fillRatePct is quantity-weighted (SUM entregado / SUM ordenado), distinct
  // from completePct (% de líneas/pedidos que llegaron al 100%) — una entrega
  // parcial de 90% suma 0.9 aquí en vez de contar como fallo binario. Medido
  // solo sobre lo Cumplido, igual que %A Tiempo/%Completo/%OTIF, para que
  // todos los % de esta pantalla compartan el mismo denominador.
  function summarize(rows) {
    const total = rows.length;
    const cumplidas = rows.filter(r => r.cumplida);
    const onTimeN = cumplidas.filter(r => r.onTime).length;
    const completeN = cumplidas.filter(r => r.complete).length;
    const otifN = cumplidas.filter(r => r.otif).length;
    const closedNoDelivery = rows.filter(r => r.closedNoDelivery).length;
    const openOverdue = rows.filter(r => r.openOverdue).length;
    // Tope por línea: entregar de más (ej. pedido 2000, entregado 3000) NO
    // compensa lo que se entregó de menos en otra línea. Sin el tope, en datos
    // reales el Fill Rate salía 99.4% cuando con tope es 93.5% (7.6% de las
    // líneas entregadas venían con sobre-entrega).
    const orderedSum = cumplidas.reduce((a, r) => a + (+r.OrderedQty || 0), 0);
    const deliveredSum = cumplidas.reduce((a, r) => a + Math.min(+r.DeliveredQty || 0, +r.OrderedQty || 0), 0);
    return {
      total, cumplidasN: cumplidas.length, onTimeN, completeN, otifN, closedNoDelivery, openOverdue,
      onTimePct: cumplidas.length ? onTimeN / cumplidas.length : null,
      completePct: cumplidas.length ? completeN / cumplidas.length : null,
      otifPct: cumplidas.length ? otifN / cumplidas.length : null,
      fillRatePct: orderedSum > 0 ? deliveredSum / orderedSum : null,
      orderedSum, deliveredSum
    };
  }

  // ---------------------------------------------------------------------
  // Agrega líneas ya computadas a nivel de PEDIDO — "perfect order": el
  // pedido solo cuenta Cumplida cuando TODAS sus líneas tienen entrega; A
  // Tiempo/Completo/OTIF exigen que TODAS las líneas cumplan (una sola
  // línea tarde o incompleta hace fallar el pedido completo), que es como
  // muchos clientes evalúan OTIF de verdad, en vez de por línea suelta.
  // ---------------------------------------------------------------------
  function aggregateToOrders(computedLines) {
    const byOrder = new Map();
    computedLines.forEach(r => {
      const k = orderKey(r);
      if (!byOrder.has(k)) byOrder.set(k, { OrderDocEntry: k, OrderNumber: r.OrderNumber, CustomerCode: r.CustomerCode, CustomerName: r.CustomerName, lines: [] });
      byOrder.get(k).lines.push(r);
    });
    return [...byOrder.values()].map(o => {
      const allCumplida = o.lines.every(l => l.cumplida);
      const anyClosedNoDelivery = o.lines.some(l => l.closedNoDelivery);
      const anyOpenOverdue = o.lines.some(l => l.openOverdue);
      const promiseds = o.lines.map(l => l.promised).filter(Boolean).sort();
      let onTime = null, complete = null, otif = null;
      if (allCumplida) {
        onTime = o.lines.every(l => l.onTime === true);
        complete = o.lines.every(l => l.complete === true);
        otif = o.lines.every(l => l.otif === true);
      }
      // OrderedQty/DeliveredQty del pedido: el tope de sobre-entrega se aplica
      // LÍNEA por línea antes de sumar (no sobre el total del pedido), para que
      // una línea entregada de más no tape a otra entregada de menos.
      const orderedQty = o.lines.reduce((a, l) => a + (+l.OrderedQty || 0), 0);
      const deliveredQty = o.lines.reduce((a, l) => a + Math.min(+l.DeliveredQty || 0, +l.OrderedQty || 0), 0);
      return {
        OrderDocEntry: o.OrderDocEntry, OrderNumber: o.OrderNumber, CustomerCode: o.CustomerCode, CustomerName: o.CustomerName,
        ItemCode: null, ItemDescription: null,
        promised: promiseds[promiseds.length - 1] || null,
        cumplida: allCumplida, onTime, complete, otif,
        closedNoDelivery: !allCumplida && anyClosedNoDelivery,
        openOverdue: !allCumplida && anyOpenOverdue,
        OrderedQty: orderedQty, DeliveredQty: deliveredQty,
        lineCount: o.lines.length
      };
    });
  }

  function tableRowFromSummary(code, name, s) {
    return {
      code, name, desc: name, total: s.total, cumplidas: s.cumplidasN, onTimePct: s.onTimePct,
      completePct: s.completePct, otifPct: s.otifPct, fillRatePct: s.fillRatePct,
      openOverdue: s.openOverdue, closedNoDelivery: s.closedNoDelivery
    };
  }

  function computeAll(filteredRawRows, f, itemRawRows, todayOverride) {
    const todayISO = todayOverride || localTodayISO();
    const computed = filteredRawRows.map(r => computeRow(r, f, todayISO));

    // f.mode = 'order': "perfect order" — el pedido completo cuenta como
    // una sola unidad (ver aggregateToOrders). f.mode = 'line' (default):
    // cada línea de pedido cuenta por separado, como antes.
    const baseRows = f.mode === 'order' ? aggregateToOrders(computed) : computed;
    const kpi = summarize(baseRows);

    // ---- por cliente (respeta el toggle línea/pedido) ----
    const custAgg = {};
    baseRows.forEach(r => {
      const k = r.CustomerCode || 'Sin código';
      if (!custAgg[k]) custAgg[k] = { name: r.CustomerName || k, rows: [] };
      custAgg[k].rows.push(r);
    });
    const custTable = Object.entries(custAgg).map(([code, v]) => tableRowFromSummary(code, v.name, summarize(v.rows)));

    // ---- por item — SIEMPRE a nivel de línea, con o sin el toggle en
    // "pedido": un pedido puede tener varios items distintos, así que no
    // existe un "OTIF de pedido" por item individual. ----
    const itemAgg = {};
    const itemKeys = new Set((itemRawRows || filteredRawRows).map(r => `${orderKey(r)}|${r.LineNumber}`));
    const itemComputed = computed.filter(r => itemKeys.has(`${orderKey(r)}|${r.LineNumber}`));
    itemComputed.forEach(r => {
      const k = r.ItemCode || 'Sin código';
      if (!itemAgg[k]) itemAgg[k] = { desc: r.ItemDescription || '', rows: [] };
      itemAgg[k].rows.push(r);
    });
    const itemTable = Object.entries(itemAgg).map(([code, v]) => tableRowFromSummary(code, v.desc, summarize(v.rows)));

    // ---- tendencia (Semana/Mes/Año, selectable) — "vista de mayor
    // alcance" (bigger picture): siempre cubre todo el rango cargado, no
    // solo los últimos 3 meses. Respeta el toggle línea/pedido. ----
    const trendBucket = ($('trendBucket') && $('trendBucket').value) || 'month';
    const trendAgg = {};
    baseRows.forEach(r => {
      if (!r.promised) return;
      const key = trendBucketKey(r.promised, trendBucket);
      if (!trendAgg[key]) trendAgg[key] = [];
      trendAgg[key].push(r);
    });
    const trendKeys = Object.keys(trendAgg).sort();
    const nowKey = trendBucketKey(todayISO, trendBucket);
    const trend = trendKeys.map(k => {
      const s = summarize(trendAgg[k]);
      return { label: trendBucketLabel(k, trendBucket), value: s.otifPct, n: s.cumplidasN, partial: k === nowKey };
    }).filter(p => p.value !== null || p.n > 0);

    return { kpi, custTable, itemTable, trend, computed, itemComputed, baseRows, mode: f.mode };
  }

  // ---------------------------------------------------------------------
  // KPI tiles — hero OTIF% colored like the slide deck (verde ≥80,
  // ambar 60-79, rojo <60).
  // ---------------------------------------------------------------------
  function colorForPct(p) {
    if (p === null || p === undefined) return 'var(--text-muted)';
    if (p >= 0.8) return 'var(--good)';
    if (p >= 0.6) return 'var(--warning)';
    return 'var(--critical)';
  }

  function deltaBadge(curr, prev) {
    if (curr === null || prev === null || curr === undefined || prev === undefined) return '';
    const d = (curr - prev) * 100;
    if (Math.abs(d) < 0.05) return '<span class="delta delta-flat">= vs. período anterior</span>';
    const cls = d > 0 ? 'delta-up' : 'delta-down';
    const arrow = d > 0 ? '▲' : '▼';
    return `<span class="delta ${cls}">${arrow} ${Math.abs(d).toFixed(1)} pp vs. período anterior</span>`;
  }

  function renderKpis(kpi, compareKpi, mode) {
    const row = $('kpiRow');
    if (!row) return;
    row.innerHTML = '';
    const unit = mode === 'order' ? 'pedidos' : 'líneas';
    const unitCap = mode === 'order' ? 'Pedidos' : 'Líneas';
    const tiles = [
      { label: `${unitCap} totales`, value: fmt(kpi.total), sub: `${fmt(kpi.cumplidasN)} cumplidos (con entrega)` },
      { label: 'Abiertas y vencidas', value: fmt(kpi.openOverdue), sub: `riesgo — ${unit} con cantidad pendiente`, cls: kpi.openOverdue > 0 ? 'tile-critical' : '' },
      { label: 'Cerradas sin entrega', value: fmt(kpi.closedNoDelivery), sub: 'no cuentan como éxito ni fallo', cls: 'tile-warning' },
      { label: '% A Tiempo', value: pctStr(kpi.onTimePct), sub: 'de lo cumplido', color: colorForPct(kpi.onTimePct), delta: compareKpi ? deltaBadge(kpi.onTimePct, compareKpi.onTimePct) : '' },
      { label: '% En su Totalidad', value: pctStr(kpi.completePct), sub: 'de lo cumplido — todo o nada', color: colorForPct(kpi.completePct), delta: compareKpi ? deltaBadge(kpi.completePct, compareKpi.completePct) : '' },
      { label: 'Fill Rate', value: pctStr(kpi.fillRatePct), sub: 'entregado ÷ pedido (tope 100% por línea)', color: colorForPct(kpi.fillRatePct), delta: compareKpi ? deltaBadge(kpi.fillRatePct, compareKpi.fillRatePct) : '' },
      { label: '% OTIF', value: pctStr(kpi.otifPct), sub: 'de lo cumplido', color: colorForPct(kpi.otifPct), big: true, delta: compareKpi ? deltaBadge(kpi.otifPct, compareKpi.otifPct) : '' },
    ];
    tiles.forEach(t => {
      const d = document.createElement('div');
      d.className = 'stat-tile' + (t.cls ? ' ' + t.cls : '') + (t.big ? ' tile-hero' : '');
      d.innerHTML = '<div class="stat-label"></div><div class="stat-value"></div><div class="stat-sub"></div><div class="stat-delta"></div>';
      d.querySelector('.stat-label').textContent = t.label;
      const valEl = d.querySelector('.stat-value');
      valEl.textContent = t.value;
      if (t.color) valEl.style.color = t.color;
      d.querySelector('.stat-sub').textContent = t.sub;
      if (t.delta) d.querySelector('.stat-delta').innerHTML = t.delta;
      row.appendChild(d);
    });
  }

  // ---------------------------------------------------------------------
  // Tables (cliente / item)
  // ---------------------------------------------------------------------
  const custCols = [
    { key: 'code' }, { key: 'name' },
    { key: 'total', num: true, render: r => fmt(r.total) },
    { key: 'cumplidas', num: true, render: r => fmt(r.cumplidas) },
    { key: 'onTimePct', num: true, html: r => `<span style="color:${colorForPct(r.onTimePct)};font-weight:600">${pctStr(r.onTimePct)}</span>` },
    { key: 'completePct', num: true, html: r => `<span style="color:${colorForPct(r.completePct)};font-weight:600">${pctStr(r.completePct)}</span>` },
    { key: 'fillRatePct', num: true, html: r => `<span style="color:${colorForPct(r.fillRatePct)};font-weight:600">${pctStr(r.fillRatePct)}</span>` },
    { key: 'otifPct', num: true, html: r => `<span style="color:${colorForPct(r.otifPct)};font-weight:700">${pctStr(r.otifPct)}</span>` },
    { key: 'openOverdue', num: true, render: r => fmt(r.openOverdue) },
    { key: 'closedNoDelivery', num: true, render: r => fmt(r.closedNoDelivery) },
  ];
  let custTableCtl = null;
  let lastCustRows = [];

  function renderCustTable() {
    if (!$('custTable')) return;
    if (!custTableCtl) {
      custTableCtl = makeTable($('custTable'), $('custRowCount'), custCols, {
        maxRows: 500,
        onSort: (key) => { if (custSort.key === key) custSort.dir = -custSort.dir; else custSort = { key, dir: -1 }; renderCustTable(); },
        getSortDir: () => custSort.dir,
        rowClick: (r) => { if ($('filCustomerCode')) { $('filCustomerCode').value = r.code; render(); } }
      });
    }
    const q = $('custSearch') ? $('custSearch').value.trim().toLowerCase() : '';
    let rows = lastComputed.custTable;
    if (q) rows = rows.filter(r => (r.code || '').toLowerCase().includes(q) || (r.name || '').toLowerCase().includes(q));
    rows = sortRows(rows, custSort.key, custSort.dir);
    lastCustRows = rows;
    custTableCtl.render(rows);
  }

  const itemCols = [
    { key: 'code' }, { key: 'desc' },
    { key: 'total', num: true, render: r => fmt(r.total) },
    { key: 'cumplidas', num: true, render: r => fmt(r.cumplidas) },
    { key: 'onTimePct', num: true, html: r => `<span style="color:${colorForPct(r.onTimePct)};font-weight:600">${pctStr(r.onTimePct)}</span>` },
    { key: 'completePct', num: true, html: r => `<span style="color:${colorForPct(r.completePct)};font-weight:600">${pctStr(r.completePct)}</span>` },
    { key: 'fillRatePct', num: true, html: r => `<span style="color:${colorForPct(r.fillRatePct)};font-weight:600">${pctStr(r.fillRatePct)}</span>` },
    { key: 'otifPct', num: true, html: r => `<span style="color:${colorForPct(r.otifPct)};font-weight:700">${pctStr(r.otifPct)}</span>` },
    { key: 'openOverdue', num: true, render: r => fmt(r.openOverdue) },
    { key: 'closedNoDelivery', num: true, render: r => fmt(r.closedNoDelivery) },
  ];
  let itemTableCtl = null;
  let lastItemRows = [];

  function renderItemTable() {
    if (!$('itemTable')) return;
    if (!itemTableCtl) {
      itemTableCtl = makeTable($('itemTable'), $('itemRowCount'), itemCols, {
        maxRows: 500,
        onSort: (key) => { if (itemSort.key === key) itemSort.dir = -itemSort.dir; else itemSort = { key, dir: -1 }; renderItemTable(); },
        getSortDir: () => itemSort.dir,
        rowClick: (r) => { if ($('filItemCode')) { $('filItemCode').value = r.code; render(); } }
      });
    }
    const q = $('itemSearch') ? $('itemSearch').value.trim().toLowerCase() : '';
    let rows = lastComputed.itemTable;
    if (q) rows = rows.filter(r => (r.code || '').toLowerCase().includes(q) || (r.desc || '').toLowerCase().includes(q));
    rows = sortRows(rows, itemSort.key, itemSort.dir);
    lastItemRows = rows;
    itemTableCtl.render(rows);
  }

  // ---------------------------------------------------------------------
  // Customer detail panel — styled after the slide deck: hero OTIF%,
  // "Con oportunidad de mejora" table (todas las que no llegan a 100%,
  // partida en dos columnas si son muchas) + "Con 100% OTIF" chips.
  // Shows only when the customer-code filter narrows to exactly one code.
  // ---------------------------------------------------------------------
  let lastDetailLabel = 'OTIF';

  function renderDetailPanel(computedRows, custCode, mode, itemComputedRows) {
    const panel = $('detailPanel');
    if (!panel) return;
    if (!custCode) { panel.style.display = 'none'; return; }

    const rowsForCust = computedRows.filter(r => r.CustomerCode === custCode);
    if (!rowsForCust.length) { panel.style.display = 'none'; return; }

    const custName = rowsForCust[0].CustomerName || custCode;
    // Hero/KPIs respetan el toggle línea/pedido; el desglose por producto
    // más abajo se queda siempre a nivel de línea (ver nota en computeAll).
    const heroRows = mode === 'order' ? aggregateToOrders(rowsForCust) : rowsForCust;
    const s = summarize(heroRows);
    const unit = mode === 'order' ? 'pedidos' : 'líneas';

    // por item, dentro de este cliente, con >2 líneas (mismo umbral que el deck)
    const itemAgg = {};
    (itemComputedRows || computedRows).filter(r => r.CustomerCode === custCode).forEach(r => {
      const k = r.ItemCode || 'Sin código';
      if (!itemAgg[k]) itemAgg[k] = { desc: r.ItemDescription || '', rows: [] };
      itemAgg[k].rows.push(r);
    });
    const perItem = Object.entries(itemAgg).map(([code, v]) => ({ code, desc: v.desc, ...summarize(v.rows) }))
      .filter(x => x.total > 2);
    const eligible = perItem.filter(x => x.cumplidasN > 0);
    const not100 = eligible.filter(x => x.otifPct !== null && x.otifPct < 1).sort((a, b) => b.total - a.total);
    const is100 = eligible.filter(x => x.otifPct === 1).sort((a, b) => b.total - a.total);

    panel.style.display = '';
    lastDetailLabel = `OTIF - ${custName} (${custCode})`;
    $('detailTitle').textContent = `${custName} (${custCode})`;
    if ($('detailSub0')) {
      let periodTxt = currentRange.from && currentRange.to
        ? `Período analizado: ${fmtDateNice(currentRange.from)} – ${fmtDateNice(currentRange.to)}`
        : '';
      if (currentRange.compareFrom && currentRange.compareTo) {
        periodTxt += periodTxt ? ' · ' : '';
        periodTxt += `comparando vs. ${fmtDateNice(currentRange.compareFrom)} – ${fmtDateNice(currentRange.compareTo)}`;
      }
      $('detailSub0').textContent = periodTxt;
    }
    $('detailHero').textContent = pctStr(s.otifPct);
    $('detailHero').style.color = colorForPct(s.otifPct);
    $('detailSub1').textContent = `${fmt(s.total)} ${unit} · ${fmt(s.cumplidasN)} cumplidos`;
    $('detailSub2').textContent = `${pctStr(s.onTimePct)} a tiempo · ${pctStr(s.completePct)} en su totalidad · ${pctStr(s.fillRatePct)} fill rate`;
    $('detailSub3').textContent = `${fmt(s.openOverdue)} abiertas y vencidas · ${fmt(s.closedNoDelivery)} cerradas sin entrega`;

    function tableHtml(rows) {
      const trs = rows.map(x => {
        const c = colorForPct(x.otifPct);
        return `<tr>
          <td>${escapeHtml(x.desc || x.code)}</td>
          <td class="num">${fmt(x.total)}</td>
          <td class="num" style="color:${colorForPct(x.onTimePct)};font-weight:600">${pctStr(x.onTimePct)}</td>
          <td class="num" style="color:${colorForPct(x.completePct)};font-weight:600">${pctStr(x.completePct)}</td>
          <td class="num" style="color:${c};font-weight:700">${pctStr(x.otifPct)}</td>
        </tr>`;
      }).join('');
      return `<table class="detail-table"><thead><tr>
        <th>Producto</th><th class="num">Líneas</th><th class="num">A Tiempo</th><th class="num">Completo</th><th class="num">OTIF</th>
      </tr></thead><tbody>${trs}</tbody></table>`;
    }

    const oportunidadWrap = $('detailOportunidad');
    if (!not100.length) {
      oportunidadWrap.innerHTML = '<p class="empty-state">Todos los productos con más de 2 líneas lograron 100% OTIF.</p>';
    } else {
      oportunidadWrap.innerHTML = tableHtml(not100);
    }
    $('detailNotCount').textContent = `${not100.length} de ${eligible.length} productos con más de 2 líneas`;

    const chipsWrap = $('detailChips');
    chipsWrap.innerHTML = is100.length
      ? is100.map(x => `<span class="chip-good" title="${escapeHtml(x.desc || x.code)}">${escapeHtml((x.desc || x.code).length > 34 ? (x.desc || x.code).slice(0, 33) + '…' : (x.desc || x.code))}</span>`).join('')
      : '<span class="empty-state" style="padding:0;">Ninguno con más de 2 líneas en este período.</span>';
    $('detailChipsCount').textContent = `(${is100.length})`;
  }

  // ---------------------------------------------------------------------
  // Excel export
  // ---------------------------------------------------------------------
  function writeExcelFile(data, sheetName, filenamePrefix) {
    if (typeof XLSX === 'undefined') {
      alert('La librería de exportación a Excel no cargó (revisa la conexión a internet) — no se puede exportar en este momento.');
      return;
    }
    if (!data || data.length === 0) {
      alert('No hay filas para exportar con los filtros actuales.');
      return;
    }
    // Keep user-controlled SAP text literal when a workbook is opened.
    const safeData = data.map(row => Object.fromEntries(Object.entries(row).map(([key, value]) =>
      [key, typeof value === 'string' && /^[\s]*[=+\-@]/.test(value) ? "'" + value : value])));
    const ws = XLSX.utils.json_to_sheet(safeData);
    ws['!cols'] = Object.keys(data[0]).map(k => ({ wch: Math.max(10, Math.min(40, k.length + 4)) }));
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, sheetName);
    const now = new Date();
    const pad = n => String(n).padStart(2, '0');
    const stamp = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}`;
    XLSX.writeFile(wb, `${filenamePrefix}_${stamp}.xlsx`);
  }

  function exportCustToExcel() {
    const unitLabel = (lastComputed && lastComputed.mode === 'order') ? 'Pedidos' : 'Líneas';
    writeExcelFile(lastCustRows.map(r => ({
      'Código cliente': r.code, 'Nombre': r.name, [`${unitLabel} totales`]: r.total, 'Cumplidos': r.cumplidas,
      '% A Tiempo': r.onTimePct === null ? '' : r.onTimePct, '% Completo': r.completePct === null ? '' : r.completePct,
      'Fill Rate': r.fillRatePct === null ? '' : r.fillRatePct,
      '% OTIF': r.otifPct === null ? '' : r.otifPct, 'Abiertas y vencidas': r.openOverdue, 'Cerradas sin entrega': r.closedNoDelivery
    })), 'OTIF por Cliente', 'OTIF_Cliente');
  }
  function exportItemToExcel() {
    writeExcelFile(lastItemRows.map(r => ({
      'Código item': r.code, 'Descripción': r.desc, 'Líneas totales': r.total, 'Cumplidas': r.cumplidas,
      '% A Tiempo': r.onTimePct === null ? '' : r.onTimePct, '% Completo': r.completePct === null ? '' : r.completePct,
      'Fill Rate': r.fillRatePct === null ? '' : r.fillRatePct,
      '% OTIF': r.otifPct === null ? '' : r.otifPct, 'Abiertas y vencidas': r.openOverdue, 'Cerradas sin entrega': r.closedNoDelivery
    })), 'OTIF por Item', 'OTIF_Item');
  }

  // ---------------------------------------------------------------------
  // Exportar la tarjeta de detalle a PDF — usa el "Guardar como PDF" del
  // diálogo de impresión del navegador en vez de sumar una librería nueva
  // (jsPDF/html2canvas): cero dependencias nuevas, y el resultado respeta
  // los mismos colores/tablas que ya se ven en pantalla. body.printing-
  // detail-only en otif-viz.css oculta todo lo demás mientras se imprime.
  // ---------------------------------------------------------------------
  function exportDetailToPdf() {
    const panel = $('detailPanel');
    if (!panel || panel.style.display === 'none') {
      alert('Primero filtra por un código de cliente para ver su tarjeta de detalle.');
      return;
    }
    const prevTitle = document.title;
    document.title = lastDetailLabel;
    document.body.classList.add('printing-detail-only');
    const cleanup = () => {
      document.body.classList.remove('printing-detail-only');
      document.title = prevTitle;
      window.removeEventListener('afterprint', cleanup);
    };
    window.addEventListener('afterprint', cleanup);
    window.print();
    // respaldo por si el navegador no dispara afterprint (poco común)
    setTimeout(cleanup, 3000);
  }

  function wireTableToggles() {
    [['custTableToggle', 'custTableWrap'], ['itemTableToggle', 'itemTableWrap']].forEach(([btnId, wrapId]) => {
      const btn = $(btnId), wrap = $(wrapId);
      if (!btn || !wrap) return;
      btn.addEventListener('click', () => {
        const hidden = wrap.style.display === 'none';
        wrap.style.display = hidden ? '' : 'none';
        btn.textContent = hidden ? 'Ocultar tabla' : 'Mostrar tabla';
      });
    });
    if ($('custSearch')) $('custSearch').addEventListener('input', renderCustTable);
    if ($('itemSearch')) $('itemSearch').addEventListener('input', renderItemTable);
    if ($('btnExportCust')) $('btnExportCust').addEventListener('click', exportCustToExcel);
    if ($('btnExportItem')) $('btnExportItem').addEventListener('click', exportItemToExcel);
    if ($('btnExportDetailPdf')) $('btnExportDetailPdf').addEventListener('click', exportDetailToPdf);
  }

  // ---------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------
  let lastComputed = null;

  function render() {
    if (!$('kpiRow')) return;
    if (raw.length === 0) {
      lastComputed = null;
      lastCustRows = [];
      lastItemRows = [];
      populateGlobalFilterOptions();
      populateCodeSuggestions();
      renderKpis({ total: 0, cumplidasN: 0, onTimeN: 0, completeN: 0, otifN: 0, closedNoDelivery: 0, openOverdue: 0, onTimePct: null, completePct: null, otifPct: null, fillRatePct: null }, null, 'line');
      if ($('trendChart')) $('trendChart').innerHTML = '';
      ['custTable', 'itemTable'].forEach(id => {
        const table = $(id);
        if (table && table.querySelector('tbody')) table.querySelector('tbody').replaceChildren();
      });
      ['custRowCount', 'itemRowCount'].forEach(id => { if ($(id)) $(id).textContent = ''; });
      if ($('status')) $('status').textContent = 'Sin datos cargados.';
      if ($('detailPanel')) $('detailPanel').style.display = 'none';
      return;
    }

    const f = getGlobalFilters();
    const selected = selectRowsForMode(raw, f, currentRange);
    const filtered = selected.rows;
    lastComputed = computeAll(filtered, f, selected.itemRows);
    populateCodeSuggestions();

    let compareComputed = null;
    if (compareRaw && compareRaw.length) {
      const selectedCompare = selectRowsForMode(compareRaw, f, { from: currentRange.compareFrom, to: currentRange.compareTo });
      compareComputed = computeAll(selectedCompare.rows, f, selectedCompare.itemRows);
    }

    if ($('status')) {
      const unit = f.mode === 'order' ? 'pedidos' : 'líneas';
      const compareNote = compareComputed ? ` · comparando contra ${compareComputed.kpi.cumplidasN} ${unit} cumplidos del período anterior` : '';
      $('status').textContent = `${filtered.length.toLocaleString('es-GT')} de ${raw.length.toLocaleString('es-GT')} líneas cargadas` +
        (f.mode === 'order' ? ` (${lastComputed.kpi.total} pedidos)` : '') + ` · filtros aplicados${compareNote}`;
    }

    renderKpis(lastComputed.kpi, compareComputed ? compareComputed.kpi : null, f.mode);
    renderCustTable();
    renderItemTable();
    lineChart($('trendChart'), lastComputed.trend, { height: 230, isPct: true, metricLabel: '% OTIF' });

    const f2 = getGlobalFilters();
    const distinctCustCodes = new Set(filtered.map(r => r.CustomerCode).filter(Boolean));
    if (f2.customerCode && distinctCustCodes.size === 1) {
      renderDetailPanel(lastComputed.computed, [...distinctCustCodes][0], f2.mode, lastComputed.itemComputed);
    } else if ($('detailPanel')) {
      $('detailPanel').style.display = 'none';
    }
  }

  // ---------------------------------------------------------------------
  // Column-name normalization — defensive safety net. otif.sql now quotes
  // every alias so HANA preserves exact case, but a manual export run
  // through a different tool/path could still fold headers to uppercase
  // (this happened with an earlier unquoted-alias query in this same
  // project — see chat history). Remap case-insensitively so a row keyed
  // "CUSTOMERCODE" or "customercode" still lands on r.CustomerCode.
  // ---------------------------------------------------------------------
  const CANONICAL_FIELDS = [
    'OrderDocEntry', 'OrderNumber', 'CustomerCode', 'CustomerName', 'Origen', 'SalesAgentCode',
    'SalesAgent', 'LineNumber', 'ItemCode', 'ItemDescription', 'FamilyCode',
    'FamilyName', 'OrderDate', 'DueDate', 'LineShipDate', 'OrderedQty',
    'OpenQty', 'LineStatus', 'DeliveryDocEntry', 'DeliveryLineNumber',
    'DeliveryDate', 'DeliveryQty', 'UnitOfMeasure'
  ];
  const CANONICAL_LOOKUP = {};
  CANONICAL_FIELDS.forEach(f => { CANONICAL_LOOKUP[f.toLowerCase()] = f; });

  // ---------------------------------------------------------------------
  // Known override: SAP Business One's own Query Generator tool ignores
  // the SQL alias for certain "recognized" standard fields and substitutes
  // its own built-in Spanish caption instead — confirmed 2026-09 on real
  // exports of otif.sql, where ItemCode/OpenQty/LineStatus came back as
  // these captions despite being quoted aliases (CustomerCode, OrderDate,
  // etc. were NOT affected, so this isn't the case-folding issue below —
  // it's a different, tool-specific override). Mapped explicitly here,
  // with the unaccented ASCII spelling too in case of encoding drift
  // between exports.
  // ---------------------------------------------------------------------
  const KNOWN_CAPTION_OVERRIDES = {
    'número de artículo': 'ItemCode',
    'numero de articulo': 'ItemCode',
    'cantidad abierta restante': 'OpenQty',
    'status de la línea': 'LineStatus',
    'status de la linea': 'LineStatus',
    'estado de la línea': 'LineStatus',
    'estado de la linea': 'LineStatus'
  };
  Object.keys(KNOWN_CAPTION_OVERRIDES).forEach(k => { CANONICAL_LOOKUP[k] = KNOWN_CAPTION_OVERRIDES[k]; });

  // Fields the rest of the app cannot function without — if normalization
  // still can't find one of these after both the case-fold and the known-
  // caption-override lookups above, something in the export changed again
  // and every downstream aggregate would silently be wrong (exactly what
  // happened with ItemCode before this fix). Fail LOUDLY instead.
  const CRITICAL_FIELDS = ['OrderDocEntry', 'LineNumber', 'CustomerCode', 'ItemCode', 'LineStatus', 'OrderedQty', 'DueDate', 'DeliveryDate', 'DeliveryQty'];

  function checkCriticalFields(rows) {
    if (!rows.length) return [];
    const present = new Set();
    rows.slice(0, 50).forEach(r => Object.keys(r).forEach(k => present.add(k)));
    return CRITICAL_FIELDS.filter(f => !present.has(f));
  }

  function warnMissingFields(missing, rows) {
    if (!missing.length) return;
    const sampleHeaders = rows.length ? Object.keys(rows[0]).join(', ') : '(sin filas)';
    console.warn('OTIF: columnas críticas no encontradas tras normalizar encabezados:', missing, '— encabezados originales:', sampleHeaders);
    const status = $('status');
    if (status) {
      status.textContent = `⚠ Faltan columnas de OTIF: ${missing.join(', ')}. Verifica que otif.sql y el dashboard sean de la misma versión.`;
    }
  }

  function normalizeRowKeys(row) {
    const out = {};
    Object.keys(row).forEach(k => {
      const canon = CANONICAL_LOOKUP[k.trim().toLowerCase()];
      out[canon || k] = row[k];
    });
    return out;
  }

  let missingCriticalFields = [];

  function normalizeDataRows(rows) {
    const normalized = (Array.isArray(rows) ? rows : []).map(normalizeRowKeys);
    const missing = checkCriticalFields(normalized);
    if (missing.length) return { rows: [], missing, normalized };
    const lines = new Map();
    normalized.forEach(r => {
      const key = `${r.OrderDocEntry}|${r.LineNumber}`;
      if (!lines.has(key)) lines.set(key, {
        ...r,
        OrderDate: normalizeFechaToISO(r.OrderDate),
        DueDate: normalizeFechaToISO(r.DueDate),
        LineShipDate: normalizeFechaToISO(r.LineShipDate),
        FirstDeliveryDate: null, LastDeliveryDate: null, DeliveredQty: 0,
        deliveries: [], deliveryKeys: new Set()
      });
      const line = lines.get(key);
      const date = normalizeFechaToISO(r.DeliveryDate);
      const qty = Number(r.DeliveryQty);
      const eventKey = `${r.DeliveryDocEntry}|${r.DeliveryLineNumber}`;
      if (r.DeliveryDocEntry != null && date && Number.isFinite(qty) && !line.deliveryKeys.has(eventKey)) {
        line.deliveryKeys.add(eventKey);
        line.deliveries.push({ date, qty });
        line.DeliveredQty += qty;
      }
    });
    const result = [...lines.values()];
    result.forEach(line => {
      line.deliveries.sort((a, b) => a.date.localeCompare(b.date));
      const positive = line.deliveries.filter(d => d.qty > 0);
      line.FirstDeliveryDate = positive.length ? positive[0].date : null;
      line.LastDeliveryDate = positive.length ? positive[positive.length - 1].date : null;
      delete line.deliveryKeys;
    });
    return { rows: result, missing: [], normalized };
  }

  function setRawData(rows) {
    const result = normalizeDataRows(rows);
    missingCriticalFields = result.missing;
    raw = result.rows;
    if (missingCriticalFields.length) { render(); warnMissingFields(missingCriticalFields, result.normalized); return; }
    populateGlobalFilterOptions();
    render();
  }

  function setCompareRawData(rows) {
    const result = normalizeDataRows(rows);
    compareRaw = result.rows;
    if (result.missing.length) { render(); warnMissingFields(result.missing, result.normalized); return; }
    render();
  }

  function clearCompareRawData() {
    compareRaw = null;
    render();
  }

  function init() {
    wireThemeToggle();
    wireGlobalFilters();
    wireTableToggles();
  }

  return { setRawData, setCompareRawData, clearCompareRawData, init, render, applyPalette, setDateRange, getDateBounds,
    __test: { normalizeDataRows, computeRow, aggregateToOrders, selectRowsForMode, computeAll } };
})();

if (typeof module !== 'undefined' && module.exports) module.exports = OTIFViz;
