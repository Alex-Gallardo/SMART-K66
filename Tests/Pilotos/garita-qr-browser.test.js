const assert = require('node:assert/strict');
const fs = require('node:fs');
const http = require('node:http');
const os = require('node:os');
const path = require('node:path');
const { spawn } = require('node:child_process');

const chromePath = process.argv[2];
if (!chromePath || !fs.existsSync(chromePath)) throw new Error('Indicar Chrome para probar el lector QR real.');
const root = path.join(__dirname, '../../DiamDev.Give.UI/Scripts/qr-scanner');
const fixture = path.join(__dirname, 'fixtures/garita-placa.png');
const html = `<!doctype html><html><head><meta charset="utf-8"></head><body>
<script src="/qr-scanner/qr-scanner.umd.min.js"></script><script>
QrScanner._disableBarcodeDetector=true;
QrScanner.scanImage('/garita-placa.png',{returnDetailedScanResult:true})
 .then(result=>document.documentElement.setAttribute('data-qr-integration',result.data))
 .catch(error=>document.documentElement.setAttribute('data-qr-integration','FAIL: '+String(error)));
</script></body></html>`;
let workerRequested = false;
const server = http.createServer((request, response) => {
    const files = {
        '/qr-scanner/qr-scanner.umd.min.js': [path.join(root, 'qr-scanner.umd.min.js'), 'application/javascript'],
        '/qr-scanner/qr-scanner-worker.min.js': [path.join(root, 'qr-scanner-worker.min.js'), 'application/javascript'],
        '/garita-placa.png': [fixture, 'image/png']
    };
    if (request.url === '/') { response.setHeader('Content-Type', 'text/html; charset=utf-8'); response.end(html); return; }
    if (request.url === '/qr-scanner/qr-scanner-worker.min.js') workerRequested = true;
    const resource = files[request.url];
    if (!resource) { response.writeHead(404).end(); return; }
    response.setHeader('Content-Type', resource[1]);
    fs.createReadStream(resource[0]).pipe(response);
});
const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
async function endpoint(profile) {
    const file = path.join(profile, 'DevToolsActivePort');
    for (let attempt = 0; attempt < 100; attempt++) {
        try { if (fs.existsSync(file)) return Number(fs.readFileSync(file, 'utf8').split(/\r?\n/)[0]); }
        catch (_) { /* Chrome todavía escribe el archivo. */ }
        await delay(100);
    }
    throw new Error('Chrome no abrió el puerto de depuración.');
}
async function target(port, pageUrl) {
    for (let attempt = 0; attempt < 100; attempt++) {
        try {
            const pages = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
            const page = pages.find(item => item.type === 'page' && item.url === pageUrl);
            if (page) return page.webSocketDebuggerUrl;
        } catch (_) { /* Chrome aún inicia. */ }
        await delay(100);
    }
    throw new Error('No se encontró la página QR en Chrome.');
}
async function connect(url) {
    const socket = new WebSocket(url);
    await new Promise((resolve, reject) => {
        socket.addEventListener('open', resolve, { once: true });
        socket.addEventListener('error', reject, { once: true });
    });
    let nextId = 0;
    return {
        socket,
        evaluate(expression) {
            const id = ++nextId;
            return new Promise((resolve, reject) => {
                const onMessage = event => {
                    const message = JSON.parse(event.data);
                    if (message.id !== id) return;
                    socket.removeEventListener('message', onMessage);
                    if (message.error) reject(new Error(message.error.message));
                    else resolve(message.result.result.value);
                };
                socket.addEventListener('message', onMessage);
                socket.send(JSON.stringify({ id, method: 'Runtime.evaluate', params: { expression, returnByValue: true } }));
            });
        }
    };
}

server.listen(0, '127.0.0.1', async () => {
    const profile = fs.mkdtempSync(path.join(os.tmpdir(), 'garita-qr-chrome-'));
    let chrome, devtools;
    try {
        const pageUrl = `http://127.0.0.1:${server.address().port}/`;
        chrome = spawn(chromePath, ['--headless=new', '--disable-gpu', '--disable-extensions', '--no-first-run', '--no-default-browser-check', '--remote-debugging-port=0', `--user-data-dir=${profile}`, pageUrl], { windowsHide: true });
        const port = await endpoint(profile);
        devtools = await connect(await target(port, pageUrl));
        let value;
        for (let attempt = 0; attempt < 100; attempt++) {
            value = await devtools.evaluate("document.documentElement.getAttribute('data-qr-integration')");
            if (value) break;
            await delay(100);
        }
        assert.equal(value, 'C-123ABC', value || 'El QR no produjo resultado.');
        assert.ok(workerRequested, 'El lector no cargó el worker incluido en la publicación.');
        console.log('OK Chrome QR garita: imagen real y worker local.');
    } catch (error) { console.error(error); process.exitCode = 1; }
    finally {
        if (devtools) devtools.socket.close();
        if (chrome) { chrome.kill(); await new Promise(resolve => chrome.once('exit', resolve)); }
        server.close();
        const resolved = path.resolve(profile);
        if (resolved.startsWith(path.resolve(os.tmpdir()) + path.sep) && path.basename(resolved).startsWith('garita-qr-chrome-'))
            fs.rmSync(resolved, { recursive: true, force: true });
    }
});
