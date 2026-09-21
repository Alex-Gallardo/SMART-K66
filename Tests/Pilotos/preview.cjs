// Local visual preview of rendered Razor + fictitious models. No SQL or app configuration.
// First: Tests/Pilotos/run.ps1, then bin/RazorCompile.exe REPO_PATH --render.
const http = require('http');
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '../..');
const bin = path.join(__dirname, 'bin');
http.createServer((req, res) => {
    const url = new URL(req.url, 'http://127.0.0.1');
    const assets = {
        '/Content/pilotos.css': ['text/css', path.join(root, 'DiamDev.Give.UI/Content/pilotos.css')],
        '/Scripts/pilotos.js': ['text/javascript', path.join(root, 'DiamDev.Give.UI/Scripts/pilotos.js')]
    };
    if (req.method !== 'GET') { res.writeHead(405); res.end('Vista previa sin operaciones de escritura.'); return; }
    if (assets[url.pathname]) {
        res.setHeader('Content-Type', assets[url.pathname][0]);
        res.end(fs.readFileSync(assets[url.pathname][1])); return;
    }
    let page = 'index';
    if (url.pathname.startsWith('/Piloto/Detalle/')) page = url.searchParams.get('vista') === 'historial' ? 'detalle-historial' : 'detalle';
    else if (url.pathname === '/preview/cierre') page = 'detalle-cierre';
    else if (url.pathname === '/preview/fija') page = 'fija';
    else if (url.pathname === '/preview/vacia') page = 'vacia';
    else if (url.pathname === '/preview/error') page = 'error';
    else if (url.searchParams.get('vista') === 'historial') page = 'historial';
    else if (url.pathname !== '/' && url.pathname !== '/Piloto/Index' && url.pathname !== '/Piloto') { res.writeHead(404); res.end(); return; }
    res.setHeader('Content-Type', 'text/html; charset=utf-8');
    res.setHeader('Cache-Control', 'no-store');
    res.setHeader('X-Pilotos-Preview', 'Datos ficticios; sin SQL ni login real');
    res.end(fs.readFileSync(path.join(bin, page + '.html')));
}).listen(8770, '127.0.0.1', () => console.log('Razor con datos ficticios: http://127.0.0.1:8770'));
