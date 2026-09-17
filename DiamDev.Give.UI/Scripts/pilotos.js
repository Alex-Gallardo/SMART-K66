(function () {
    "use strict";
    var form = document.getElementById("complete-route");
    if (!form) return;
    var results = form.querySelectorAll(".delivery-result");
    function updateReason(select) {
        var reason = select.closest(".result-fields").querySelector(".delivery-reason");
        reason.required = select.value === "NO ENTREGADO" || select.value === "INCIDENCIA";
    }
    Array.prototype.forEach.call(results, function (select) {
        updateReason(select);
        select.addEventListener("change", function () { updateReason(select); });
    });
    form.addEventListener("submit", function (event) {
        if (!window.confirm("¿Completar esta ruta con los resultados indicados? La ruta quedará cerrada.")) { event.preventDefault(); return; }
        var button = document.getElementById("complete-button");
        if (button) button.disabled = true;
        document.getElementById("submit-status").textContent = "Guardando resultados…";
    });
    window.addEventListener("pageshow", function () {
        var button = document.getElementById("complete-button");
        if (button) button.disabled = false;
        var status = document.getElementById("submit-status");
        if (status) status.textContent = "";
    });
}());
