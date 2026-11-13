const { spawn } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");
const { pathToFileURL } = require("node:url");
const assert = require("node:assert/strict");
const [fixture, outputDir] = process.argv.slice(2);
const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const timeout = ms => new Promise(resolve => setTimeout(resolve, ms));
(async () => {
    const child = spawn(edge, ["--headless", "--disable-gpu", "--remote-debugging-port=0", "--no-first-run", "--user-data-dir="+path.join(outputDir,"cdp-profile")], {windowsHide:true, stdio:["ignore","ignore","pipe"]});
    let socket;
    try {
        const endpoint = await new Promise((resolve,reject) => {
            const timer=setTimeout(()=>reject(Error("Edge no publicó DevTools.")),15000);
            let log="";
            child.stderr.on("data",data=>{log+=data;const m=log.match(/DevTools listening on (ws:\/\/[^\s]+)/);if(m){clearTimeout(timer);resolve(m[1]);}});
            child.once("error",reject);
        });
        socket = new WebSocket(endpoint);
        await new Promise((resolve,reject)=>{socket.onopen=resolve;socket.onerror=reject;});
        const pending=new Map();let serial=0;
        socket.onmessage=event=>{const data=JSON.parse(event.data);if(pending.has(data.id)){const {resolve,reject}=pending.get(data.id);pending.delete(data.id);data.error?reject(Error(data.error.message)):resolve(data.result);}};
        const send=(method,params={},sessionId)=>new Promise((resolve,reject)=>{const id=++serial;pending.set(id,{resolve,reject});socket.send(JSON.stringify({id,method,params,sessionId}));});
        const cases=[{width:1440,compact:false,zoom:100},{width:1366,compact:true,zoom:100},{width:1024,compact:false,zoom:100},{width:768,compact:false,zoom:100},{width:390,compact:false,zoom:100},{width:1440,compact:false,zoom:125}];
        for(const c of cases) {
            const {targetId}=await send("Target.createTarget",{url:"about:blank"});
            const {sessionId}=await send("Target.attachToTarget",{targetId,flatten:true});
            await send("Page.enable",{},sessionId);
            await send("Page.bringToFront",{},sessionId);
            await send("Emulation.setFocusEmulationEnabled",{enabled:true},sessionId);
            await send("Emulation.setDeviceMetricsOverride",{width:c.width,height:1100,deviceScaleFactor:1,mobile:false},sessionId);
            await send("Page.addScriptToEvaluateOnNewDocument",{source:"window.layoutCase="+JSON.stringify(c)+";"},sessionId);
            await send("Page.navigate",{url:pathToFileURL(fixture).href},sessionId);
            let result="";
            for(let i=0;i<90;i++){
                await timeout(250);
                const evaluated=await send("Runtime.evaluate",{expression:"document.body && document.body.dataset.smoke",returnByValue:true},sessionId);
                result=evaluated.result.value||"";
                if(result)break;
            }
            assert.equal(result,"PASS","Layout "+JSON.stringify(c)+": "+result);
            await send("Runtime.evaluate",{expression:"document.querySelector('[data-view=\"general\"]').focus();"},sessionId);
            await send("Input.dispatchKeyEvent",{type:"keyDown",text:"\r",unmodifiedText:"\r",key:"Enter",code:"Enter",windowsVirtualKeyCode:13},sessionId);
            await send("Input.dispatchKeyEvent",{type:"keyUp",key:"Enter",code:"Enter",windowsVirtualKeyCode:13},sessionId);
            await timeout(450);
            let keyboard=await send("Runtime.evaluate",{expression:"document.querySelector('.gca-nav-link[aria-current=\"page\"]').dataset.view",returnByValue:true},sessionId);
            assert.equal(keyboard.result.value,"general","La tecla Enter cambia de vista.");
            await send("Runtime.evaluate",{expression:"document.querySelector('#detailFilters summary').focus();"},sessionId);
            await send("Input.dispatchKeyEvent",{type:"keyDown",text:"\r",unmodifiedText:"\r",key:"Enter",code:"Enter",windowsVirtualKeyCode:13},sessionId);
            await send("Input.dispatchKeyEvent",{type:"keyUp",key:"Enter",code:"Enter",windowsVirtualKeyCode:13},sessionId);
            keyboard=await send("Runtime.evaluate",{expression:"document.querySelector('#detailFilters').open",returnByValue:true},sessionId);
            assert.equal(keyboard.result.value,true,"Los filtros avanzados se abren por teclado.");
            await send("Runtime.evaluate",{expression:"document.querySelector('[data-view=\"dashboard\"]').click();"},sessionId);
            await timeout(450);
            const stem=c.width+"-"+(c.compact?"compact":"expanded")+"-"+c.zoom;
            const screen=await send("Page.captureScreenshot",{format:"png"},sessionId);
            fs.writeFileSync(path.join(outputDir,stem+"-dashboard.png"),Buffer.from(screen.data,"base64"));
            await send("Emulation.setEmulatedMedia",{media:"print"},sessionId);
            const print=await send("Runtime.evaluate",{expression:"({margin:getComputedStyle(document.querySelector('#main-content')).marginLeft,nav:getComputedStyle(document.querySelector('.gca-navigation')).display})",returnByValue:true},sessionId);
            assert.equal(print.result.value.margin,"0px","Impresión sin margen del menú global.");
            assert.equal(print.result.value.nav,"none","Navegación oculta al imprimir.");
            await send("Emulation.setEmulatedMedia",{media:"screen"},sessionId);
            for(const view of ["clientes","perfil-agente"]){
                await send("Runtime.evaluate",{expression:"document.querySelector('[data-view=\""+view+"\"]').click();"},sessionId);
                if(view==="perfil-agente")await send("Runtime.evaluate",{expression:"document.querySelector('#profileAgent').value='FAES|1';document.querySelector('#profileAgent').dispatchEvent(new Event('change'));"},sessionId);
                await timeout(450);
                const screen=await send("Page.captureScreenshot",{format:"png"},sessionId);
                fs.writeFileSync(path.join(outputDir,stem+"-"+view+".png"),Buffer.from(screen.data,"base64"));
            }
            await send("Target.closeTarget",{targetId});
            process.stdout.write("OK layout: "+stem+" · siete vistas, controles, contraste y geometría.\n");
        }
    } finally {
        if(socket)socket.close();
        child.kill();
    }
})().catch(error=>{console.error(error.stack);process.exitCode=1;});
