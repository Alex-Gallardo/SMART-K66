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
    function updateCard(input, clearDelivered) {
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
        if (selected && selected.value === "ENTREGADO" && clearDelivered) {
            reason.value = "";
            observation.value = "";
            card.querySelector("input.visit-result[value=true]").checked = true;
            updateCount(observation);
        }
        var label = card.querySelector(".document-result-label");
        if (label) label.textContent = selected ? selected.value : "Pendiente";
        var toggle = card.querySelector(".document-toggle");
        if (toggle) toggle.textContent = card.querySelector(".document-edit").hidden ?
            (selected ? "Editar resultado" : "Registrar resultado") : "Ocultar detalle";
    }
    var groups = form.querySelectorAll(".customer-group");
    function setGroupOpen(group, open) {
        var details = group.querySelector(".customer-documents");
        var toggle = group.querySelector(".client-toggle");
        details.hidden = !open;
        if (toggle) { toggle.setAttribute("aria-expanded", open ? "true" : "false"); toggle.textContent = open ? "Ocultar cliente" : "Revisar o editar"; }
    }
    function setDocumentOpen(card, open) {
        var details = card.querySelector(".document-edit");
        var toggle = card.querySelector(".document-toggle");
        if (!toggle || !details) return;
        details.hidden = !open;
        toggle.setAttribute("aria-expanded", open ? "true" : "false");
        var selected = selectedResult(card);
        toggle.textContent = open ? "Ocultar detalle" : (selected ? "Editar resultado" : "Registrar resultado");
    }
    function markDirty(group) {
        group.setAttribute("data-saved", "false");
        group.classList.remove("is-complete");
        group.querySelector(".client-complete-badge").hidden = true;
        group.querySelector(".group-status").textContent = "Cambios sin guardar. Guarda este cliente antes de cerrar la ruta.";
        group._revision = (group._revision || 0) + 1;
        var check = group.querySelector(".bulk-delivered");
        if (check && check.checked) check.indeterminate = Array.prototype.some.call(group.querySelectorAll(".document-card"), function (card) {
            var result = selectedResult(card);
            return !result || result.value !== "ENTREGADO";
        });
    }
    function snapshot(card) {
        var visit = card.querySelector(".visit-result:checked");
        var delivery = selectedResult(card);
        return { rowId: Number(card.querySelector("input[name$='.RowId']").value), visit: visit ? visit.value : "",
            delivery: delivery ? delivery.value : "", reason: card.querySelector(".delivery-reason").value,
            observation: card.querySelector(".delivery-observation").value };
    }
    function restore(card, value) {
        Array.prototype.forEach.call(card.querySelectorAll(".visit-result,.delivery-result"), function (radio) {
            radio.checked = radio.value === (radio.classList.contains("visit-result") ? value.visit : value.delivery);
        });
        card.querySelector(".delivery-reason").value = value.reason;
        var observation = card.querySelector(".delivery-observation");
        observation.value = value.observation;
        updateCount(observation);
        updateCard(card.querySelector(".delivery-result"), false);
    }
    Array.prototype.forEach.call(results, function (input) {
        input.addEventListener("change", function () {
            updateCard(input, true);
            markDirty(input.closest(".customer-group"));
        });
    });
    Array.prototype.forEach.call(form.querySelectorAll(".document-card"), function (card) {
        var result = selectedResult(card) || card.querySelector(".delivery-result");
        updateCard(result, false);
        var observation = card.querySelector(".delivery-observation");
        observation.addEventListener("input", function () { updateCount(observation); markDirty(card.closest(".customer-group")); });
        updateCount(observation);
        var toggle = card.querySelector(".document-toggle");
        if (toggle) toggle.addEventListener("click", function () { setDocumentOpen(card, card.querySelector(".document-edit").hidden); });
        Array.prototype.forEach.call(card.querySelectorAll(".visit-result,.delivery-reason"), function (input) {
            input.addEventListener("change", function () { markDirty(card.closest(".customer-group")); });
        });
    });
    Array.prototype.forEach.call(groups, function (group) {
        var groupToggle = group.querySelector(".client-toggle");
        if (groupToggle) groupToggle.addEventListener("click", function () { setGroupOpen(group, group.querySelector(".customer-documents").hidden); });
        var check = group.querySelector(".bulk-delivered");
        if (!check) return;
        if (check.checked) group._beforeBulk = Array.prototype.map.call(group.querySelectorAll(".document-card"), function (card) {
            return {rowId:Number(card.querySelector("input[name$='.RowId']").value), visit:card.getAttribute("data-before-visit") || "",
                delivery:card.getAttribute("data-before-delivery") || "", reason:card.getAttribute("data-before-reason") || "",
                observation:card.getAttribute("data-before-observation") || ""};
        });
        check.addEventListener("change", function () {
            var cards = group.querySelectorAll(".document-card");
            if (check.checked) {
                group._beforeBulk = Array.prototype.map.call(cards, snapshot);
                Array.prototype.forEach.call(cards, function (card) {
                    var delivered = card.querySelector(".delivery-result[value='ENTREGADO']");
                    delivered.checked = true;
                    updateCard(delivered, true);
                });
            } else {
                Array.prototype.forEach.call(cards, function (card, i) { restore(card, group._beforeBulk[i]); });
                group._beforeBulk = null;
                check.indeterminate = false;
            }
            markDirty(group);
        });
        var save = group.querySelector(".save-client");
        save.addEventListener("click", function () {
            var invalid = group.querySelector(":invalid");
            if (invalid) { setGroupOpen(group, true); setDocumentOpen(invalid.closest(".document-card"), true); invalid.reportValidity(); invalid.focus(); return; }
            var endpoint = document.getElementById("draft-endpoint");
            var data = new FormData();
            data.append("__RequestVerificationToken", form.querySelector("input[name='__RequestVerificationToken']").value);
            data.append("RutaId", form.elements.RutaId.value);
            data.append("Version", form.elements.Version.value);
            data.append("PrimerRowId", group.getAttribute("data-first-row-id"));
            data.append("MasivoActivo", check.checked ? "true" : "false");
            Array.prototype.forEach.call(group.querySelectorAll(".document-card"), function (card, i) {
                var value = snapshot(card);
                data.append("Documentos[" + i + "].RowId", value.rowId);
                data.append("Documentos[" + i + "].Visito", value.visit);
                data.append("Documentos[" + i + "].Entrega", value.delivery);
                data.append("Documentos[" + i + "].Motivo", value.reason);
                data.append("Documentos[" + i + "].Observaciones", value.observation);
            });
            if (check.checked) Array.prototype.forEach.call(group._beforeBulk || [], function (value, i) {
                data.append("Anteriores[" + i + "].RowId", value.rowId);
                data.append("Anteriores[" + i + "].Visito", value.visit);
                data.append("Anteriores[" + i + "].Entrega", value.delivery);
                data.append("Anteriores[" + i + "].Motivo", value.reason);
                data.append("Anteriores[" + i + "].Observaciones", value.observation);
            });
            var revision = group._revision || 0;
            save.disabled = true;
            group.querySelector(".group-status").textContent = "Guardando en POS…";
            window.fetch(endpoint.getAttribute("data-url"), {method:"POST",body:data,credentials:"same-origin",headers:{"Accept":"application/json"}})
                .then(function (response) {
                    if (!/application\/json/i.test(response.headers.get("Content-Type") || "")) throw new Error("La sesión cambió. Inicia sesión y vuelve a guardar.");
                    return response.json();
                }).then(function (response) {
                    if (!response.ok) throw new Error(response.mensaje || "No se pudo guardar el cliente.");
                    if ((group._revision || 0) !== revision) { group.querySelector(".group-status").textContent = "Se guardó una versión anterior. Vuelve a guardar tus cambios."; return; }
                    group.setAttribute("data-saved", "true");
                    group.classList.add("is-complete");
                    group.querySelector(".client-complete-badge").hidden = false;
                    group.querySelector(".group-status").textContent = "Cliente guardado en POS.";
                    setGroupOpen(group, false);
                }).catch(function (error) { group.querySelector(".group-status").textContent = error.message || "No se pudo guardar. Inténtalo de nuevo."; })
                .then(function () { save.disabled = false; });
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
        var pending = Array.prototype.filter.call(groups, function (group) { return group.getAttribute("data-saved") !== "true"; });
        if (pending.length) {
            setGroupOpen(pending[0], true);
            pending[0].querySelector(".group-status").textContent = "Guarda este cliente antes de revisar el cierre.";
            pending[0].scrollIntoView({behavior:"smooth",block:"start"});
            return;
        }
        if (!form.checkValidity()) {
            var invalid = form.querySelector(":invalid");
            if (invalid) {
                setGroupOpen(invalid.closest(".customer-group"), true);
                setDocumentOpen(invalid.closest(".document-card"), true);
                invalid.reportValidity(); invalid.focus();
            }
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
    Array.prototype.forEach.call(modal.querySelectorAll("[data-close-modal]"), function (button) { button.addEventListener("click", function () {
        closeModal();
        if (button.textContent.indexOf("Volver a revisar") >= 0) Array.prototype.forEach.call(groups, function (group) {
            setGroupOpen(group, true);
            Array.prototype.forEach.call(group.querySelectorAll(".document-card"), function (card) { setDocumentOpen(card, true); });
        });
    }); });
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
