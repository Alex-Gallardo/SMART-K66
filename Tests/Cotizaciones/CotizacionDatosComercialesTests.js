"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const leer = (...partes) => fs.readFileSync(path.join(raiz, ...partes), "utf8");
const vista = leer("DiamDev.Give.UI", "Views", "Cotizacion", "Index.cshtml");
const javascript = leer("DiamDev.Give.UI", "Scripts", "App", "Cotizacion-Index.js");
const modelo = leer("DiamDev.Give.UI", "Models", "CotizacionViewModel.cs");
const controlador = leer("DiamDev.Give.UI", "Controllers", "CotizacionController.cs");
const bll = leer("DiamDev.Give.BLL", "CotizacionBLL.cs");

function input(id) {
    const coincidencia = vista.match(new RegExp(`<input[^>]*id="${id}"[^>]*>`, "i"));
    assert(coincidencia, `Debe existir el input ${id}.`);
    return coincidencia[0];
}

[
    ["cotClienteNombre", "200"],
    ["cotNit", "50"],
    ["cotCorreo", "150"],
    ["cotDireccion", "300"]
].forEach(([id, maximo]) => {
    const etiqueta = input(id);
    assert(!/\breadonly\b/i.test(etiqueta), `${id} debe ser editable.`);
    assert(new RegExp(`maxlength="${maximo}"`, "i").test(etiqueta),
        `${id} debe respetar el límite ${maximo}.`);
});

assert(/\bdisabled\b/i.test(input("cotFecha")),
    "Emisión debe permanecer deshabilitada.");
assert(/\breadonly\b/i.test(input("cotClienteCodigo")),
    "El código SAP del cliente debe permanecer protegido.");

["NombreCliente", "Nit", "Direccion", "Correo"].forEach(propiedad => {
    assert(modelo.includes(`public string ${propiedad} { get; set; }`),
        `El contrato debe incluir ${propiedad}.`);
    assert(controlador.includes(`${propiedad} = request.${propiedad}`),
        `El controlador debe transportar ${propiedad}.`);
});

assert(javascript.includes('NombreCliente: $("#cotClienteNombre").val().trim()') &&
       javascript.includes('Nit: $("#cotNit").val().trim()') &&
       javascript.includes('Direccion: $("#cotDireccion").val().trim()') &&
       javascript.includes('Correo: $("#cotCorreo").val().trim()'),
    "El navegador debe enviar los valores comerciales editados.");
assert(javascript.includes('$("#cotMoneda").val(m && m !== "##" ? m : "")') &&
       javascript.includes('Moneda: $("#cotMoneda").val()'),
    "Seleccionar cliente debe restablecer moneda y guardar la elección final.");

assert(bll.includes("enc.NombreCliente = Limpiar(enc.NombreCliente);") &&
       bll.includes("enc.Nit = Limpiar(enc.Nit);") &&
       bll.includes("enc.Direccion = Limpiar(enc.Direccion);") &&
       bll.includes("enc.Correo = Limpiar(enc.Correo);"),
    "El BLL debe preservar la fotografía comercial enviada.");
assert(!bll.includes("enc.NombreCliente = Limpiar(cliente.CardName);") &&
       !bll.includes("enc.Nit = Limpiar(cliente.LicTradNum);"),
    "SAP no debe sobrescribir Nombre ni NIT después de guardar.");
assert(bll.includes("enc.Moneda = ResolverMoneda(enc.Moneda);") &&
       bll.includes("no está soportada"),
    "La moneda editable debe normalizarse y limitarse a valores soportados.");

console.log("OK: datos comerciales y moneda editables verificados de UI a persistencia.");
