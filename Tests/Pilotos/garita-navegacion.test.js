const assert = require('node:assert/strict');
const fs = require('node:fs');
const http = require('node:http');
const path = require('node:path');
const { execFile } = require('node:child_process');
const { promisify } = require('node:util');

const chrome = process.argv[2];
const runChrome = promisify(execFile);
const prefix = '/pedidosap';
const index = prefix + '/Garita/Index';
const detail = index + '/PP26-GARITA';
const preview = name => fs.readFileSync(path.join(__dirname, 'bin', 'garita-' + name + '.html'), 'utf8');
const assets = new Map([
    ['/Content/garita.css', 'text/css'],
    ['/Content/garita-refresh.css', 'text/css'],
    ['/Scripts/garita-ui.js', 'application/javascript']
]);
const site = path.join(__dirname, '..', '..', 'DiamDev.Give.UI');

async function main() {
    let backQuery;
    const server = http.createServer((request, response) => {
        const url = new URL(request.url, 'http://localhost');
        const asset = url.pathname.slice(prefix.length);
        if (url.pathname.startsWith(prefix + '/') && assets.has(asset)) {
            response.setHeader('Content-Type', assets.get(asset));
            response.end(fs.readFileSync(path.join(site, asset.slice(1))));
            return;
        }
        response.setHeader('Content-Type', 'text/html; charset=utf-8');
        if (url.pathname === detail && !url.searchParams.has('seccion')) {
            response.end(preview('en-ruta').replace('</body>', '<script>document.querySelector(".search-back-link").click();</script></body>'));
        } else if (url.pathname === index) {
            backQuery = Object.fromEntries(url.searchParams);
            response.end(preview('lista-regreso'));
        } else {
            response.writeHead(404).end('Ruta inesperada');
        }
    });

    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    try {
        const url = 'http://127.0.0.1:' + server.address().port + detail + '?placa=C-123ABC';
        const { stdout } = await runChrome(chrome, ['--headless=new', '--disable-gpu', '--no-first-run',
            '--virtual-time-budget=2000', '--dump-dom', url], { timeout: 25000, windowsHide: true });
        assert.deepEqual(backQuery, {
            placa: 'C-123ABC',
            seccion: 'en-ruta',
            pagina: '2',
            paginaAbiertas: '1',
            paginaCerradas: '1'
        }, 'El clic debe navegar al listado bajo /pedidosap y conservar placa, sección y páginas sin id.');
        assert.match(stdout, /class="route-tabs"/);
        assert.match(stdout, /aria-current="page"[^>]*seccion=en-ruta/);
        assert.match(stdout, /Página 2 de 2/);
        assert.doesNotMatch(stdout, /id="garita-route-title"/);
        console.log('OK Chrome Garita: clic de regreso desde una URL con id hacia la página y sección de la placa.');
    } finally {
        server.close();
    }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
