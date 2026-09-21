(function () {
    "use strict";
    // Only scroll position is stored, scoped to the normalized list filters.
    var grid = document.querySelector(".route-grid");
    if (grid) {
        var key = "pilotos-scroll:" + grid.getAttribute("data-scroll-key");
        try {
            var saved = window.sessionStorage.getItem(key);
            window.sessionStorage.removeItem(key);
            if (saved !== null && /^\d+$/.test(saved)) {
                window.requestAnimationFrame(function () { window.scrollTo(0, Number(saved)); });
            }
        } catch (ignored) { /* Navigation still works when storage is unavailable. */ }
        grid.addEventListener("click", function (event) {
            if (!event.target.closest(".card-link")) return;
            try { window.sessionStorage.setItem(key, String(Math.round(window.scrollY))); } catch (ignored) {}
        });
    }
    var filter = document.querySelector(".date-filter");
    if (filter) {
        var filterButton = filter.querySelector("button[type=submit]");
        var label = filterButton.innerHTML;
        filter.addEventListener("submit", function () {
            filter.setAttribute("aria-busy", "true");
            filterButton.disabled = true;
            filterButton.textContent = "Consultando…";
        });
        window.addEventListener("pageshow", function () {
            filter.removeAttribute("aria-busy");
            filterButton.disabled = false;
            filterButton.innerHTML = label;
        });
    }
    var account = document.querySelector(".account-menu");
    if (account) {
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && account.open) {
                account.open = false;
                account.querySelector("summary").focus();
            }
        });
    }
    var form = document.getElementById("complete-route");
    if (!form || !document.getElementById("complete-button")) return;
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
        document.getElementById("complete-button").disabled = true;
        document.getElementById("submit-status").textContent = "Guardando resultados…";
    });
    window.addEventListener("pageshow", function () {
        document.getElementById("complete-button").disabled = false;
        document.getElementById("submit-status").textContent = "";
    });
}());
