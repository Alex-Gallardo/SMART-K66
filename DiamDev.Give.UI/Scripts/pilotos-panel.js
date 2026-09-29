(function () {
    'use strict';
    var form = document.getElementById('panel-filters');
    var content = document.getElementById('panel-content');
    var message = document.getElementById('panel-message');
    var toast = document.getElementById('panel-toast');
    var busy = false, dirty = false, timer, toastTimer;
    function notify(text, error) {
        if (error && message) {
            message.querySelector('span').textContent = text;
            message.hidden = false;
        } else if (toast) {
            toast.textContent = text; toast.hidden = false;
            clearTimeout(toastTimer); toastTimer = setTimeout(function () { toast.hidden = true; }, 5000);
        }
    }
    if (message) message.querySelector('button').addEventListener('click', function () { message.hidden = true; });
    function dateText(date) {
        return date.getUTCFullYear() + '-' + ('0' + (date.getUTCMonth() + 1)).slice(-2) + '-' + ('0' + date.getUTCDate()).slice(-2);
    }
    function validDates(from, to) {
        if (!/^\d{4}-\d{2}-\d{2}$/.test(from) || !/^\d{4}-\d{2}-\d{2}$/.test(to)) return false;
        var a = new Date(from + 'T00:00:00Z'), b = new Date(to + 'T00:00:00Z');
        return !isNaN(a) && !isNaN(b) && a.toISOString().slice(0, 10) === from && b.toISOString().slice(0, 10) === to && b >= a && b - a < 31 * 86400000;
    }
    if (form) {
        var filters = document.getElementById('panel-filter-details');
        if (filters && window.matchMedia('(max-width: 700px)').matches) filters.open = false;
        form.addEventListener('input', function () { dirty = true; });
        form.addEventListener('change', function () { dirty = true; });
        form.addEventListener('submit', function (event) {
            var a = document.getElementById('panel-desde'), b = document.getElementById('panel-hasta');
            a.removeAttribute('aria-invalid'); b.removeAttribute('aria-invalid');
            if (!validDates(a.value, b.value)) {
                event.preventDefault(); a.setAttribute('aria-invalid', 'true'); b.setAttribute('aria-invalid', 'true');
                if (filters) filters.open = true;
                notify('Selecciona fechas válidas, en orden y con un máximo de 31 días.', true); a.focus();
            }
        });
        form.querySelectorAll('[data-period]').forEach(function (button) {
            button.addEventListener('click', function () {
                var end = new Date(Date.now() - 6 * 3600000), start = new Date(end);
                if (button.dataset.period === 'ayer') { end.setUTCDate(end.getUTCDate() - 1); start = new Date(end); }
                if (button.dataset.period === 'semana') start.setUTCDate(start.getUTCDate() - 6);
                document.getElementById('panel-desde').value = dateText(start);
                document.getElementById('panel-hasta').value = dateText(end);
                form.requestSubmit ? form.requestSubmit() : form.querySelector('[type=submit]').click();
            });
        });
    }
    async function serverError(response) {
        try { var data = await response.json(); if (data.mensaje) return data.mensaje; } catch (ignored) { /* La respuesta IIS puede no ser JSON. */ }
        return response.status === 403 ? 'Tu permiso o sesión ya no permite consultar el panel.' : 'No pudimos actualizar la consulta. Conservamos los datos anteriores; vuelve a intentarlo.';
    }
    async function refresh(automatic) {
        if (!content || busy || document.hidden) return;
        if (dirty) { if (!automatic) notify('Aplica los filtros modificados antes de actualizar.', true); return; }
        if (automatic && (content.contains(document.activeElement) || content.querySelector('details[open]') || document.getElementById('panel-photo-dialog') && !document.getElementById('panel-photo-dialog').hidden)) return;
        busy = true; content.setAttribute('aria-busy', 'true');
        var button = document.getElementById('panel-refresh'); if (button) button.disabled = true;
        try {
            var response = await fetch(content.dataset.refreshUrl, { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) throw new Error(await serverError(response));
            if (!(response.headers.get('content-type') || '').includes('text/html')) throw new Error('La respuesta no corresponde al panel. Revisa tu sesión.');
            var html = await response.text();
            if (response.redirected || !html.includes('class="panel-update"')) throw new Error('La sesión del panel no está disponible. Inicia sesión nuevamente.');
            // HTML generado por la vista Razor del mismo sitio, con los valores comerciales codificados.
            content.innerHTML = html;
            if (message) message.hidden = true;
            if (!automatic) notify('Panel actualizado.', false);
        } catch (error) { notify(error.message || 'No pudimos actualizar el panel.', true); }
        finally { busy = false; content.setAttribute('aria-busy', 'false'); if (button) button.disabled = false; }
    }
    var refreshButton = document.getElementById('panel-refresh');
    if (refreshButton) refreshButton.addEventListener('click', function () { refresh(false); });
    var automatic = document.getElementById('panel-auto');
    if (automatic) {
        automatic.addEventListener('change', function () {
            clearInterval(timer);
            if (automatic.checked) timer = setInterval(function () { refresh(true); }, 60000);
            notify(automatic.checked ? 'Actualización automática cada 60 segundos. Se pausa al consultar detalles.' : 'Actualización automática desactivada.', false);
        });
    }
    document.addEventListener('click', async function (event) {
        var link = event.target.closest('.panel-export');
        if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault(); if (link.getAttribute('aria-busy') === 'true') return;
        if (dirty) { notify('Aplica los filtros modificados antes de exportar.', true); return; }
        link.setAttribute('aria-busy', 'true');
        try {
            var response = await fetch(link.href, { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) throw new Error(await serverError(response));
            if (!(response.headers.get('content-type') || '').includes('text/csv')) throw new Error('No pudimos descargar el archivo. Revisa tu sesión.');
            var blob = await response.blob(), url = URL.createObjectURL(blob), download = document.createElement('a');
            download.href = url; download.download = link.href.includes('documentos') ? 'pilotos-documentos.csv' : 'pilotos-rutas.csv';
            document.body.appendChild(download); download.click(); download.remove();
            setTimeout(function () { URL.revokeObjectURL(url); }, 1000); notify('Exportación descargada con los filtros actuales.', false);
        } catch (error) { notify(error.message || 'No pudimos exportar los registros.', true); }
        finally { link.removeAttribute('aria-busy'); }
    });
    var dialog = document.getElementById('panel-photo-dialog'), photo = document.getElementById('panel-photo-image'), state = document.getElementById('panel-photo-state'), previousFocus, isolated = [];
    function closePhoto() {
        if (!dialog) return;
        dialog.hidden = true; photo.removeAttribute('src');
        document.body.style.overflow = '';
        isolated.forEach(function (item) { item.node.inert = item.inert; }); isolated = [];
        if (previousFocus) previousFocus.focus();
    }
    if (dialog) {
        document.getElementById('panel-photo-close').addEventListener('click', closePhoto);
        dialog.addEventListener('click', function (event) { if (event.target === dialog) closePhoto(); });
        document.addEventListener('keydown', function (event) {
            if (dialog.hidden) return;
            if (event.key === 'Escape') { event.preventDefault(); closePhoto(); }
            if (event.key === 'Tab') {
                var buttons = Array.from(dialog.querySelectorAll('button,a[href]')).filter(function (x) { return !x.disabled; });
                var first = buttons[0], last = buttons[buttons.length - 1];
                if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
                else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
            }
        });
        document.addEventListener('click', function (event) {
            var link = event.target.closest('.panel-photo');
            if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault(); previousFocus = link; dialog.hidden = false; photo.hidden = true; state.textContent = 'Cargando fotografía…';
            document.getElementById('panel-photo-original').href = link.href; document.body.style.overflow = 'hidden';
            Array.from(document.body.children).forEach(function (node) {
                if (node.contains(dialog)) Array.from(node.children).filter(function (n) { return n !== dialog; }).forEach(function (n) { isolated.push({ node: n, inert: n.inert }); n.inert = true; });
                else if (node.tagName !== 'SCRIPT') { isolated.push({ node: node, inert: node.inert }); node.inert = true; }
            });
            document.getElementById('panel-photo-close').focus();
            photo.onload = function () { photo.hidden = false; state.textContent = 'Fotografía guardada en POS.'; };
            photo.onerror = function () { photo.hidden = true; state.textContent = 'No pudimos cargar la fotografía. Revisa tu permiso, sesión o disponibilidad del archivo.'; };
            photo.src = link.href;
        });
    }
    window.PilotoPanelUI = { validDates: validDates, refresh: refresh };
})();
