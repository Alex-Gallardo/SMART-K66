(function ($) {
    "use strict";
    var $root = $("#bncDetalleDashboard");
    if (!$root.length) return;
    var borrador = { IdEmpresa: $root.attr("data-empresa"), IdBorrador: $root.attr("data-borrador") };
    var indicador = window.BorradorNcSapIndicadores.crear({
        root: $root, origen: "dashboard",
        cambio: function (cache) {
            var estado = cache[(borrador.IdEmpresa + "|" + borrador.IdBorrador).toUpperCase()];
            $root.find(".bncdd-link").each(function () {
                var $badge = $(this).find(".bncdd-link-status");
                var entry = Number($(this).attr("data-doc-entry"));
                if (!estado || estado.error || !estado.data) {
                    $badge.removeClass("is-current is-cancelled").addClass("is-unknown").text("Sin verificar ahora");
                    return;
                }
                var documento = (estado.data.Documentos || []).filter(function (x) { return Number(x.DocEntry) === entry; })[0];
                $badge.removeClass("is-current is-cancelled is-unknown");
                if (!documento) $badge.addClass("is-unknown").text("No hallada ahora en SAP");
                else if (documento.Cancelado) $badge.addClass("is-cancelled").text("Cancelada en SAP");
                else $badge.addClass("is-current").text("Vigente en SAP");
            });
        }
    });
    $("#bncddLiveSap").html(window.BorradorNcSapIndicadores.plantilla(borrador, true));
    indicador.mostrar([borrador]);
})(window.jQuery);
