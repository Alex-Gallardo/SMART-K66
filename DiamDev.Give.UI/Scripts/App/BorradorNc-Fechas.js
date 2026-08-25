(function (window) {
    "use strict";

    var patronMvc = /^\/Date\((-?\d+)(?:[+-]\d{4})?\)\/$/;
    var patronIso = /^(\d{4})-(\d{2})-(\d{2})(?:[T\s](\d{2}):(\d{2})(?::(\d{2}))?(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?$/;

    function dosDigitos(value) {
        return value < 10 ? "0" + value : String(value);
    }

    function fechaValida(anio, mes, dia) {
        var fecha = new Date(anio, mes - 1, dia);
        return fecha.getFullYear() === anio &&
            fecha.getMonth() === mes - 1 &&
            fecha.getDate() === dia;
    }

    function desdeDate(fecha, incluyeHora) {
        if (!(fecha instanceof Date) || isNaN(fecha.getTime())) return null;
        return {
            anio: fecha.getFullYear(),
            mes: fecha.getMonth() + 1,
            dia: fecha.getDate(),
            hora: fecha.getHours(),
            minuto: fecha.getMinutes(),
            incluyeHora: !!incluyeHora
        };
    }

    function partes(value) {
        if (value == null || value === "") return null;
        if (value instanceof Date) return desdeDate(value, true);
        if (typeof value === "number") return desdeDate(new Date(value), true);

        var texto = String(value).trim();
        var mvc = patronMvc.exec(texto);
        if (mvc) return desdeDate(new Date(parseInt(mvc[1], 10)), true);

        var iso = patronIso.exec(texto);
        if (iso) {
            var anio = parseInt(iso[1], 10);
            var mes = parseInt(iso[2], 10);
            var dia = parseInt(iso[3], 10);
            var hora = iso[4] == null ? 0 : parseInt(iso[4], 10);
            var minuto = iso[5] == null ? 0 : parseInt(iso[5], 10);
            var segundo = iso[6] == null ? 0 : parseInt(iso[6], 10);
            if (!fechaValida(anio, mes, dia) || hora > 23 || minuto > 59 || segundo > 59) return null;
            return {
                anio: anio,
                mes: mes,
                dia: dia,
                hora: hora,
                minuto: minuto,
                incluyeHora: iso[4] != null
            };
        }

        return null;
    }

    function paraInput(value) {
        var fecha = partes(value);
        if (!fecha) return "";
        return fecha.anio + "-" + dosDigitos(fecha.mes) + "-" + dosDigitos(fecha.dia);
    }

    function corta(value) {
        var fecha = partes(value);
        if (!fecha) return "—";
        return dosDigitos(fecha.dia) + "/" + dosDigitos(fecha.mes) + "/" + fecha.anio;
    }

    function fechaHora(value) {
        var fecha = partes(value);
        if (!fecha) return "—";
        var resultado = dosDigitos(fecha.dia) + "/" + dosDigitos(fecha.mes) + "/" + fecha.anio;
        if (fecha.incluyeHora)
            resultado += " " + dosDigitos(fecha.hora) + ":" + dosDigitos(fecha.minuto);
        return resultado;
    }

    window.BorradorNcFechas = {
        corta: corta,
        fechaHora: fechaHora,
        paraInput: paraInput
    };
}(window));
