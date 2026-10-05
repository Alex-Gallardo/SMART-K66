const assert = require('assert');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { spawnSync } = require('child_process');
const { pathToFileURL } = require('url');

const root = path.join(__dirname, '../..');
const view = fs.readFileSync(path.join(root, 'DiamDev.Give.UI/Views/Pedido_K66/Crear.cshtml'), 'utf8');
const start = view.indexOf('function actualizarFechaEntrega()');
const end = view.indexOf('\n    });', start);
assert(start !== -1 && end !== -1, 'No se encontró la lógica de Fecha Entrega.');
const script = view.slice(start, end);
const jquery = pathToFileURL(path.join(root, 'DiamDev.Give.UI/Scripts/jquery-1.10.2.js')).href;
const datepicker = pathToFileURL(path.join(root, 'DiamDev.Give.UI/Content/assets/plugins/bootstrap-datepicker/js/bootstrap-datepicker.js')).href;
const chrome = 'C:/Program Files/Google/Chrome/Application/chrome.exe';
assert(fs.existsSync(chrome), 'Chrome no está disponible para la prueba del calendario.');

const testDir = fs.mkdtempSync(path.join(os.tmpdir(), 'k66-fecha-browser-'));
const fixture = path.join(testDir, 'fecha.html');
const html = `<!doctype html><html><body>
<select id="TipoPedidoId"><option value="">Seleccione</option><option value="N1">Normal</option><option value="P">Temporada</option></select>
<div id="divFechaEntrega"><input id="FechaEntrega" class="date-picker" data-date-format="yyyy-mm-dd" value="2026-10-05"></div>
<input id="FechaPrometida" value="2026-10-05">
<script src="${jquery}"></script><script src="${datepicker}"></script>
<script>
var fechaActual = '2026-10-05', fechaMinimaNormal = '2026-10-07';
$(function () {
    try {
        $('#FechaEntrega').datepicker({ autoclose: true });
        ${script}
        $('#TipoPedidoId').val('N1').change();
        var input = $('#FechaEntrega');
        var calendario = input.data('datepicker');
        if (input.val() !== '2026-10-07' || $('#FechaPrometida').val() !== '2026-10-07') throw Error('Fecha mínima o dato enviado incorrectos.');
        if (!input.prop('readonly') || !calendario.o.startDate || calendario.o.startDate.getUTCDate() !== 7) throw Error('Restricción del calendario no aplicada.');
        input.datepicker('show');
        var dias = calendario.picker.find('td.day:not(.old):not(.new)');
        function dia(numero) { return dias.filter(function () { return $(this).text() === String(numero); }); }
        if (!dia(5).hasClass('disabled') || !dia(6).hasClass('disabled') || dia(7).hasClass('disabled')) throw Error('El calendario no bloqueó las fechas anteriores.');
        input.datepicker('hide');
        $('#TipoPedidoId').val('P').change();
        if (input.prop('readonly') || calendario.o.startDate !== -Infinity || !$('#divFechaEntrega').is(':visible')) throw Error('Temporada debe quedar sin restricción.');
        document.body.setAttribute('data-test', 'OK');
    } catch (error) { document.body.setAttribute('data-test', 'FAIL: ' + error.message); }
});
</script></body></html>`;
fs.writeFileSync(fixture, html);

try {
    const result = spawnSync(chrome, [
        '--headless=new', '--disable-gpu', '--no-first-run', '--disable-extensions',
        '--disable-background-networking', '--virtual-time-budget=2000',
        '--user-data-dir=' + path.join(testDir, 'profile'), '--dump-dom', pathToFileURL(fixture).href
    ], { encoding: 'utf8', timeout: 30000 });
    assert.strictEqual(result.status, 0, result.stderr || 'Chrome no pudo ejecutar la prueba.');
    const status = /data-test="([^"]*)"/.exec(result.stdout);
    assert(status, 'La prueba del navegador no terminó.');
    assert.strictEqual(status[1], 'OK');
    console.log('FechaEntregaCalendarioBrowserTests: OK');
} finally {
    const resolved = path.resolve(testDir);
    assert(resolved.startsWith(path.resolve(os.tmpdir()) + path.sep), 'Directorio temporal inesperado.');
    fs.rmSync(resolved, { recursive: true, force: true });
}
