"use strict";

const assert = require("assert");
const fs = require("fs");
const path = require("path");

const raiz = path.resolve(__dirname, "..", "..");
const vista = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "Views", "BorradorNc", "Imprimir.cshtml"),
    "utf8");
const proyecto = fs.readFileSync(
    path.join(raiz, "DiamDev.Give.UI", "DiamDev.Give.UI.csproj"),
    "utf8");

[
    ["GRACO", "LogoGraco.svg"],
    ["FAES", "LogoFaes.svg"],
    ["BOLIK", "logoBolik.svg"]
].forEach(([empresa, logo]) => {
    assert(vista.includes(`{ "${empresa}",`),
        `La impresión debe reconocer la empresa ${empresa}.`);
    assert(vista.includes(`Logo = "${logo}"`),
        `La empresa ${empresa} debe usar ${logo}.`);
    assert(fs.existsSync(path.join(
        raiz, "DiamDev.Give.UI", "Content", "img", "logos", logo)),
        `El recurso ${logo} debe existir con la capitalización configurada.`);
    assert(proyecto.includes(`Content\\img\\logos\\${logo}`),
        `El proyecto debe publicar el recurso ${logo}.`);
});

assert(vista.includes("System.StringComparer.OrdinalIgnoreCase"),
    "La resolución de empresa debe tolerar diferencias de mayúsculas.");
assert(vista.includes('Url.Content("~/Content/img/logos/" + empresa.Logo)'),
    "El logo debe resolverse mediante una URL propia de la aplicación.");
assert(vista.includes('alt="Logo de @empresa.Nombre"'),
    "El logo debe incluir texto alternativo identificable.");
assert(vista.includes('class="brand-mark" aria-label="Smart K66"'),
    "Una empresa no configurada debe conservar el fallback K66.");
assert(vista.includes("background: @empresa.Color") &&
       vista.includes("border-bottom: 2px solid @empresa.Color"),
    "El membrete y la acción principal deben respetar el color corporativo.");
assert(vista.includes("<h1>@empresa.Nombre</h1>") &&
       vista.includes("<p>NIT @empresa.Nit</p>"),
    "El membrete debe mostrar razón social y NIT de la empresa.");

console.log("OK: membrete corporativo de BorradorNc validado para GRACO, FAES y BOLIK.");
