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
    var reviewButton = document.getElementById("review-button");
    var completeButton = document.getElementById("complete-button");
    var modal = document.getElementById("complete-modal");
    if (!form || !reviewButton || !completeButton || !modal) return;
    var results = form.querySelectorAll(".delivery-result");
    var previousFocus;
    function selectedResult(card) {
        return card.querySelector(".delivery-result:checked");
    }
    function updateCount(textarea) {
        var counter = textarea.parentNode.querySelector(".character-count");
        if (counter) counter.textContent = textarea.value.length + " / 500";
    }
    function updateCard(input) {
        var card = input.closest(".document-card");
        var selected = selectedResult(card);
        var fields = card.querySelector(".exception-fields");
        var reason = card.querySelector(".delivery-reason");
        var observation = card.querySelector(".delivery-observation");
        var hasProblem = selected && (selected.value === "NO ENTREGADO" || selected.value === "INCIDENCIA");
        fields.classList.toggle("is-visible", !!hasProblem);
        fields.setAttribute("aria-hidden", hasProblem ? "false" : "true");
        reason.required = !!hasProblem;
        observation.required = !!hasProblem;
        card.classList.toggle("has-delivered", !!selected && selected.value === "ENTREGADO");
        card.classList.toggle("has-problem", !!hasProblem);
        if (selected && selected.value === "ENTREGADO") {
            reason.value = "";
            observation.value = "";
            card.querySelector("input.visit-result[value=true]").checked = true;
            updateCount(observation);
        }
    }
    Array.prototype.forEach.call(results, function (input) {
        input.addEventListener("change", function () { updateCard(input); });
    });
    Array.prototype.forEach.call(form.querySelectorAll(".document-card"), function (card) {
        var result = selectedResult(card) || card.querySelector(".delivery-result");
        updateCard(result);
        var observation = card.querySelector(".delivery-observation");
        observation.addEventListener("input", function () { updateCount(observation); });
        updateCount(observation);
    });
    function buildSummary() {
        var counts = { "ENTREGADO": 0, "NO ENTREGADO": 0, "INCIDENCIA": 0 };
        var list = document.querySelector("#exception-summary ul");
        list.textContent = "";
        Array.prototype.forEach.call(form.querySelectorAll(".document-card"), function (card) {
            var result = selectedResult(card);
            counts[result.value]++;
            if (result.value === "ENTREGADO") return;
            var item = document.createElement("li");
            var title = document.createElement("strong");
            title.textContent = card.getAttribute("data-document") + " · " + (result.value === "INCIDENCIA" ? "Incidencia" : "No entregado");
            var detail = document.createElement("span");
            detail.textContent = card.querySelector(".delivery-reason").value + ": " + card.querySelector(".delivery-observation").value;
            item.appendChild(title); item.appendChild(detail); list.appendChild(item);
        });
        document.getElementById("summary-delivered").textContent = counts["ENTREGADO"];
        document.getElementById("summary-failed").textContent = counts["NO ENTREGADO"];
        document.getElementById("summary-incidents").textContent = counts["INCIDENCIA"];
        document.getElementById("exception-summary").hidden = list.children.length === 0;
    }
    function openModal() {
        if (!form.checkValidity()) {
            if (form.reportValidity) form.reportValidity();
            var invalid = form.querySelector(":invalid");
            if (invalid) invalid.focus();
            return;
        }
        buildSummary();
        previousFocus = document.activeElement;
        document.getElementById("confirm-reviewed").checked = false;
        completeButton.disabled = true;
        modal.hidden = false;
        document.body.classList.add("modal-open");
        modal.querySelector(".confirm-sheet").focus();
    }
    function closeModal() {
        modal.hidden = true;
        document.body.classList.remove("modal-open");
        if (previousFocus) previousFocus.focus();
    }
    reviewButton.addEventListener("click", openModal);
    Array.prototype.forEach.call(modal.querySelectorAll("[data-close-modal]"), function (button) { button.addEventListener("click", closeModal); });
    document.getElementById("confirm-reviewed").addEventListener("change", function (event) { completeButton.disabled = !event.target.checked; });
    document.addEventListener("keydown", function (event) {
        if (modal.hidden) return;
        if (event.key === "Escape") { event.preventDefault(); closeModal(); return; }
        if (event.key !== "Tab") return;
        var focusable = modal.querySelectorAll("button:not([disabled]),input:not([disabled])");
        if (!focusable.length) return;
        var first = focusable[0], last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    });
    form.addEventListener("submit", function (event) {
        if (modal.hidden || !document.getElementById("confirm-reviewed").checked) {
            event.preventDefault(); openModal(); return;
        }
        completeButton.disabled = true;
        reviewButton.disabled = true;
        modal.querySelector(".confirm-sheet").setAttribute("aria-busy", "true");
        document.getElementById("submit-status").textContent = "Guardando resultados…";
    });
    window.addEventListener("pageshow", function () {
        closeModal();
        reviewButton.disabled = false;
        completeButton.disabled = !document.getElementById("confirm-reviewed").checked;
        modal.querySelector(".confirm-sheet").removeAttribute("aria-busy");
        document.getElementById("submit-status").textContent = "";
    });
}());
