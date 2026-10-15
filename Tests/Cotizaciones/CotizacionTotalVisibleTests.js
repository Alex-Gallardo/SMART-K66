"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const vm = require("vm");

const script = fs.readFileSync(path.join(__dirname, "..", "..",
    "DiamDev.Give.UI", "Scripts", "App", "Cotizacion-Index.js"), "utf8");
function extraer(inicio, fin) {
    const a = script.indexOf(inicio);
    const b = script.indexOf(fin, a + inicio.length);
    assert(a >= 0 && b > a, `No se encontró ${inicio}`);
    return script.slice(a, b);
}

const calculos = [
    extraer("function numero(valor)", "function moneda(valor, codigo)"),
    extraer("function calcularLinea(x)", "function renderLineas()")
].join("\n");
const contexto = vm.createContext({});
vm.runInContext(calculos, contexto);

const ejemplo = contexto.calcularLinea({
    Cantidad: 144,
    PrecioUnitario: 13.116042,
    DescuentoPorcentaje: 0,
    ImpuestoPorcentaje: 12
});
assert.strictEqual(contexto.redondear(13.116042), 13.12);
assert.strictEqual(ejemplo.bruto, 1889.28);
assert.strictEqual(ejemplo.impuesto, 0);
assert.strictEqual(ejemplo.total, 1889.28);

const descuentoAdicional = contexto.calcularLinea({
    Cantidad: 2, PrecioUnitario: 112, DescuentoPorcentaje: 10,
    ImpuestoPorcentaje: 12
});
assert.strictEqual(descuentoAdicional.total, 201.6);
assert.strictEqual(descuentoAdicional.impuesto, 0);
console.log("OK: vista previa multiplica el precio visible y no agrega IVA.");
