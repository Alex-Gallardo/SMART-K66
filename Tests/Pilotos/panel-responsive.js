// Prueba aislada con Chrome propio y Node 22. No usa la sesión del navegador del usuario.
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..');
const bin = path.join(__dirname, 'bin');
const profile = path.join(bin, 'chrome-panel-' + process.pid);
const chrome = process.env.CHROME_PATH || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
fs.mkdirSync(profile, { recursive: true });
const processChrome = spawn(chrome, ['--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check', '--remote-debugging-port=0', '--user-data-dir=' + profile, 'about:blank'], { windowsHide: true, stdio: 'ignore' });
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
let socket;
(async () => {
    try {
        let port;
        for (let i = 0; i < 100; i++) {
            const file = path.join(profile, 'DevToolsActivePort');
            if (fs.existsSync(file)) { port = fs.readFileSync(file, 'utf8').split('\n')[0]; break; }
            await wait(100);
        }
        if (!port) throw new Error('Chrome no inició el perfil de pruebas');
        const version = await (await fetch('http://127.0.0.1:' + port + '/json/version')).json();
        socket = new WebSocket(version.webSocketDebuggerUrl);
        await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });
        let id = 0; const pending = new Map();
        socket.onmessage = event => { const message = JSON.parse(event.data); if (message.id && pending.has(message.id)) { const item = pending.get(message.id); pending.delete(message.id); message.error ? item.reject(new Error(message.error.message)) : item.resolve(message.result); } };
        function call(method, params = {}, sessionId) {
            return new Promise((resolve, reject) => { const current = ++id; pending.set(current, { resolve, reject }); socket.send(JSON.stringify({ id: current, method, params, sessionId })); });
        }
        const target = await call('Target.createTarget', { url: 'about:blank' });
        const attached = await call('Target.attachToTarget', { targetId: target.targetId, flatten: true });
        const session = attached.sessionId;
        await call('Page.enable', {}, session);
        for (const width of [320, 390, 768, 1440]) {
            await call('Emulation.setDeviceMetricsOverride', { width, height: 950, deviceScaleFactor: 1, mobile: width === 390 }, session);
            await call('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] }, session);
            for (const name of ['panel-resumen', 'panel-rutas', 'panel-pilotos', 'panel-documentos', 'panel-actividad', 'panel-alertas', 'panel-detalle', 'panel-error', 'administracion', 'configurar', 'administracion-error']) {
                const isPanel = name.startsWith('panel-');
                let html = fs.readFileSync(path.join(bin, name + '.html'), 'utf8');
                for (const css of ['pilotos.css', 'pilotos-panel.css']) html = html.replace('/Content/' + css, pathToFileURL(path.join(root, 'DiamDev.Give.UI/Content', css)).href);
                html = html.replace('<script src="/Scripts/pilotos.js"></script>', '<script src="' + pathToFileURL(path.join(root, 'DiamDev.Give.UI/Scripts', isPanel ? 'pilotos-panel.js' : 'pilotos.js')).href + '"></script>');
                const file = path.join(bin, 'panel-responsive-preview.html'); fs.writeFileSync(file, html);
                await call('Page.navigate', { url: pathToFileURL(file).href + '?case=' + name + width }, session);
                let ready = false;
                for (let attempt = 0; attempt < 50; attempt++) {
                    const loaded = await call('Runtime.evaluate', { expression: 'document.readyState === "complete" && location.search === "?case=' + name + width + '" && !!document.querySelector(".pilot-admin-surface")', returnByValue: true }, session);
                    if (loaded.result.value) { ready = true; break; }
                    await wait(100);
                }
                if (!ready) throw new Error('La vista no terminó de cargar: ' + name + ' ' + width);
                const result = await call('Runtime.evaluate', { expression: 'JSON.stringify({width:innerWidth,scroll:document.documentElement.scrollWidth,motion:getComputedStyle(document.querySelector(".panel-surface,.admin-section,.pilot-main")).transitionDuration,shell:!!document.querySelector(".app-navbar")&&!!document.querySelector(".app-sidebar"),isolated:!!document.querySelector(".pilot-admin-surface"),heroClear:!document.querySelector(".pbo-filter-section")||document.querySelector(".pbo-hero").getBoundingClientRect().bottom<=document.querySelector(".pbo-filter-section").getBoundingClientRect().top+1})', returnByValue: true }, session);
                const value = JSON.parse(result.result.value);
                if (value.width !== width || value.scroll > width + 1 || parseFloat(value.motion) > 0.001 || !value.shell || !value.isolated || !value.heroClear) throw new Error(name + ': ' + JSON.stringify(value));
                if (name === 'panel-resumen') {
                    const expanded = await call('Runtime.evaluate', { expression: 'document.getElementById("panel-filter-toggle").click();JSON.stringify({open:!document.getElementById("panel-filter-body").hidden,expanded:document.getElementById("panel-filter-toggle").getAttribute("aria-expanded"),scroll:document.documentElement.scrollWidth,clear:document.querySelector(".pbo-filter-section").getBoundingClientRect().bottom<=document.querySelector(".panel-toolbar").getBoundingClientRect().top+1})', returnByValue: true }, session);
                    const filters = JSON.parse(expanded.result.value);
                    if (!filters.open || filters.expanded !== 'true' || filters.scroll > width + 1 || !filters.clear) throw new Error('Filtros expandidos ' + width + ': ' + JSON.stringify(filters));
                    if (width === 390 || width === 1440) {
                        const metrics = await call('Page.getLayoutMetrics', {}, session);
                        const shot = await call('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true, clip: { x: 0, y: 0, width, height: Math.min(3000, metrics.cssContentSize.height), scale: 1 } }, session);
                        fs.writeFileSync(path.join(bin, 'panel-filtros-' + width + '.png'), Buffer.from(shot.data, 'base64'));
                    }
                }
                if (['panel-resumen','panel-detalle','administracion','configurar'].includes(name)) {
                    const metrics = await call('Page.getLayoutMetrics', {}, session);
                    const shot = await call('Page.captureScreenshot', { format: 'png', captureBeyondViewport: true, clip: { x: 0, y: 0, width, height: Math.min(3500, metrics.cssContentSize.height), scale: 1 } }, session);
                    fs.writeFileSync(path.join(bin, name + '-' + width + '.png'), Buffer.from(shot.data, 'base64'));
                }
                console.log('OK ' + name + ': viewport ' + width + ', sin desbordamiento y movimiento reducido.');
            }
        }
        await call('Browser.close');
    } finally { if (socket) socket.close(); processChrome.kill(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
