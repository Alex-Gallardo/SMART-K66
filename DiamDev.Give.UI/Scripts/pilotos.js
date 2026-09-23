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
    var vehicleFilter = document.getElementById("vehicle-filter");
    if (vehicleFilter) {
        vehicleFilter.addEventListener("input", function () {
            var query = vehicleFilter.value.toLocaleLowerCase().trim();
            Array.prototype.forEach.call(document.querySelectorAll("#vehicle-options [data-option]"), function (option) {
                option.hidden = query.length > 0 && option.getAttribute("data-option").indexOf(query) < 0;
            });
        });
    }
    var adminForm = document.getElementById("pilot-admin-form");
    if (adminForm) {
        adminForm.addEventListener("submit", function (event) {
            if (!adminForm.checkValidity()) return;
            var active = adminForm.querySelector("input[name=Activo][type=checkbox]").checked;
            var message = active
                ? "¿Confirmas este vínculo, sus centros y vehículos autorizados?"
                : "¿Confirmas que deseas desactivar el acceso de este piloto?";
            if (!window.confirm(message)) { event.preventDefault(); return; }
            var button = adminForm.querySelector("button[type=submit]");
            button.disabled = true; button.textContent = "Guardando…";
        });
    }
    var imageModal = document.getElementById("image-modal");
    var imageForm = document.getElementById("image-upload-form");
    if (imageModal && imageForm) {
        var imageFile = document.getElementById("image-file");
        var imagePreview = document.getElementById("image-preview");
        var imageStatus = document.getElementById("image-status");
        var imageSubmit = document.getElementById("image-submit");
        var imageSubmitLabel = document.getElementById("image-submit-label");
        var imageTrigger, imageFocus, previewUrl;
        function clearPreview() {
            if (previewUrl) window.URL.revokeObjectURL(previewUrl);
            previewUrl = null;
            imagePreview.hidden = true;
            imagePreview.removeAttribute("src");
        }
        function closeImage() {
            if (imageSubmit.disabled) return;
            imageModal.hidden = true;
            document.body.classList.remove("modal-open");
            imageForm.reset();
            clearPreview();
            if (imageFocus) imageFocus.focus();
        }
        Array.prototype.forEach.call(document.querySelectorAll(".image-upload-trigger"), function (trigger) {
            trigger.addEventListener("click", function () {
                imageTrigger = trigger;
                imageFocus = document.activeElement;
                imageForm.reset();
                clearPreview();
                imageStatus.textContent = "";
                imageForm.elements.rowId.value = trigger.getAttribute("data-row-id");
                var replacing = trigger.getAttribute("data-replace") === "true";
                document.getElementById("image-title").textContent = replacing ? "Reemplazar imagen" : "Subir imagen";
                document.getElementById("image-document").textContent = "Documento " + trigger.getAttribute("data-document");
                imageModal.hidden = false;
                document.body.classList.add("modal-open");
                imageFile.focus();
            });
        });
        Array.prototype.forEach.call(imageModal.querySelectorAll("[data-close-image]"), function (button) {
            button.addEventListener("click", closeImage);
        });
        imageFile.addEventListener("change", function () {
            clearPreview();
            imageStatus.textContent = "";
            var file = imageFile.files && imageFile.files[0];
            if (!file) return;
            if (file.size > 10 * 1024 * 1024) {
                imageStatus.textContent = "La imagen supera el límite de 10 MB.";
                imageFile.value = "";
                return;
            }
            if (window.URL && window.URL.createObjectURL) {
                previewUrl = window.URL.createObjectURL(file);
                imagePreview.src = previewUrl;
                imagePreview.hidden = false;
            }
        });
        imageForm.addEventListener("submit", function (event) {
            event.preventDefault();
            if (!imageForm.checkValidity()) { imageForm.reportValidity(); return; }
            if (imageTrigger.getAttribute("data-replace") === "true" &&
                !window.confirm("¿Reemplazar la imagen actual de este documento?")) return;
            imageSubmit.disabled = true;
            imageSubmitLabel.textContent = "Guardando…";
            imageStatus.textContent = "Subiendo imagen…";
            window.fetch(imageForm.action, { method:"POST", body:new FormData(imageForm), credentials:"same-origin", headers:{"Accept":"application/json"} })
                .then(function (response) {
                    if (!/application\/json/i.test(response.headers.get("Content-Type") || ""))
                        throw new Error("La sesión cambió o el servidor no respondió correctamente. Inicia sesión y vuelve a intentarlo.");
                    return response.json();
                })
                .then(function (result) {
                    if (!result.ok) throw new Error(result.mensaje || "No se pudo guardar la imagen.");
                    var card = imageTrigger.closest(".document-card");
                    var holder = card.querySelector(".document-image-link");
                    if (!holder) {
                        holder = document.createElement("p");
                        holder.className = "document-image-link";
                        var link = document.createElement("a");
                        link.target = "_blank";
                        link.rel = "noopener";
                        link.textContent = "Ver imagen adjunta";
                        holder.appendChild(link);
                        card.insertBefore(holder, imageTrigger);
                    }
                    holder.querySelector("a").href = result.url + (result.url.indexOf("?") < 0 ? "?" : "&") + "v=" + Date.now();
                    imageTrigger.setAttribute("data-replace", "true");
                    imageTrigger.lastChild.nodeValue = "Reemplazar imagen";
                    imageSubmit.disabled = false;
                    imageSubmitLabel.textContent = "Guardar imagen";
                    closeImage();
                    var notice = card.querySelector(".image-card-status");
                    if (!notice) {
                        notice = document.createElement("p");
                        notice.className = "image-card-status";
                        notice.setAttribute("role", "status");
                        card.insertBefore(notice, imageTrigger.nextSibling);
                    }
                    notice.textContent = "Imagen guardada. Los resultados de entrega siguen pendientes hasta cerrar la ruta.";
                })
                .catch(function (error) {
                    imageStatus.textContent = error.message || "No se pudo guardar la imagen. Revisa tu conexión e inténtalo de nuevo.";
                    imageSubmit.disabled = false;
                    imageSubmitLabel.textContent = "Guardar imagen";
                });
        });
        document.addEventListener("keydown", function (event) {
            if (imageModal.hidden) return;
            if (event.key === "Escape") { event.preventDefault(); closeImage(); return; }
            if (event.key !== "Tab") return;
            var focusable = imageModal.querySelectorAll("button:not([disabled]),input:not([disabled])");
            if (!focusable.length) return;
            var first = focusable[0], last = focusable[focusable.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
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
        input.addEventListener("change", function () {
            updateCard(input);
            var status = input.closest(".customer-group").querySelector(".group-status");
            if (status) status.textContent = "";
        });
    });
    Array.prototype.forEach.call(form.querySelectorAll(".document-card"), function (card) {
        var result = selectedResult(card) || card.querySelector(".delivery-result");
        updateCard(result);
        var observation = card.querySelector(".delivery-observation");
        observation.addEventListener("input", function () { updateCount(observation); });
        updateCount(observation);
    });
    Array.prototype.forEach.call(form.querySelectorAll(".bulk-delivered"), function (button) {
        button.addEventListener("click", function () {
            var group = button.closest(".customer-group");
            var cards = group.querySelectorAll(".document-card");
            var overwrites = Array.prototype.some.call(cards, function (card) {
                var result = selectedResult(card);
                return result && (result.value === "NO ENTREGADO" || result.value === "INCIDENCIA");
            });
            if (overwrites && !window.confirm("Esta acción borrará los motivos y observaciones de las facturas con problemas de este cliente. ¿Continuar?")) return;
            Array.prototype.forEach.call(cards, function (card) {
                var delivered = card.querySelector(".delivery-result[value='ENTREGADO']");
                delivered.checked = true;
                updateCard(delivered);
            });
            group.querySelector(".group-status").textContent = cards.length +
                (cards.length === 1 ? " documento marcado" : " documentos marcados") + ". Se guardarán al cerrar la ruta.";
        });
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
