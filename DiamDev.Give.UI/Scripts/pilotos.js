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
        var documentImageAction = imageForm.action;
        var customerImageAction = imageForm.getAttribute("data-customer-action");
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
        Array.prototype.forEach.call(document.querySelectorAll(".image-upload-trigger,.client-image-upload-trigger"), function (trigger) {
            trigger.addEventListener("click", function () {
                if (trigger.disabled || trigger.hidden) return;
                imageTrigger = trigger;
                imageFocus = document.activeElement;
                imageForm.reset();
                clearPreview();
                imageStatus.textContent = "";
                var customer = trigger.classList.contains("client-image-upload-trigger");
                imageForm.action = customer ? customerImageAction : documentImageAction;
                imageForm.elements.rowId.value = customer ? "" : trigger.getAttribute("data-row-id");
                imageForm.elements.primerRowId.value = customer ? trigger.getAttribute("data-first-row-id") : "";
                imageForm.elements.entrega.value = customer ? "" : trigger.closest(".document-card").querySelector(".delivery-result:checked").value;
                var replacing = trigger.getAttribute("data-replace") === "true";
                document.getElementById("image-kind").textContent = customer ? "FOTO FINAL DEL CLIENTE" : "FOTO DEL DOCUMENTO";
                document.getElementById("image-title").textContent = customer ? (replacing ? "Reemplazar foto final" : "Subir foto final") : (replacing ? "Reemplazar imagen" : "Subir imagen");
                document.getElementById("image-document").textContent = customer ? "Foto opcional del cliente" : "Documento " + trigger.getAttribute("data-document");
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
            var customer = imageTrigger.classList.contains("client-image-upload-trigger");
            if (imageTrigger.getAttribute("data-replace") === "true" &&
                !window.confirm(customer ? "¿Reemplazar la foto final de este cliente?" : "¿Reemplazar la imagen actual de este documento?")) return;
            imageSubmit.disabled = true;
            imageSubmitLabel.textContent = "Guardando…";
            imageStatus.textContent = "Subiendo imagen…";
            var uploadPersisted = false;
            var imageData = new FormData(imageForm);
            if (customer) Array.prototype.forEach.call(imageTrigger.closest(".customer-group").querySelectorAll(".document-card"), function (card, i) {
                var value = snapshot(card);
                imageData.append("documentos[" + i + "].RowId", value.rowId);
                imageData.append("documentos[" + i + "].Visito", value.visit);
                imageData.append("documentos[" + i + "].Entrega", value.delivery);
                imageData.append("documentos[" + i + "].Motivo", value.reason);
                imageData.append("documentos[" + i + "].Observaciones", value.observation);
            });
            window.fetch(imageForm.action, { method:"POST", body:imageData, credentials:"same-origin", headers:{"Accept":"application/json"} })
                .then(function (response) {
                    if (!/application\/json/i.test(response.headers.get("Content-Type") || ""))
                        throw new Error("La sesión cambió o el servidor no respondió correctamente. Inicia sesión y vuelve a intentarlo.");
                    return response.json();
                })
                .then(function (result) {
                    if (!result.ok) throw new Error(result.mensaje || "No se pudo guardar la imagen.");
                    uploadPersisted = true;
                    var card = imageTrigger.closest(customer ? ".customer-group" : ".document-card");
                    var holder = card && card.querySelector(customer ? ".client-image-link" : ".document-image-link");
                    var notice = card && card.querySelector(customer ? ".group-status" : ".image-card-status");
                    var label = imageTrigger.querySelector(customer ? ".client-image-label" : ".image-upload-label");
                    if (!holder || !notice || !label || !result.url) throw new Error("La imagen se guardó, pero no se pudo actualizar la vista.");
                    holder.querySelector("a").href = result.url + (result.url.indexOf("?") < 0 ? "?" : "&") + "v=" + Date.now();
                    holder.hidden = false;
                    imageTrigger.setAttribute("data-replace", "true");
                    label.textContent = customer ? "Reemplazar foto final" : "Reemplazar imagen";
                    notice.textContent = customer ? "Foto final guardada. Completa el cliente cuando termines." : "Imagen guardada. El resultado de entrega se aplica al cerrar la ruta.";
                    imageSubmit.disabled = false;
                    imageSubmitLabel.textContent = "Guardar imagen";
                    closeImage();
                })
                .catch(function (error) {
                    imageStatus.textContent = uploadPersisted
                        ? "La imagen se guardó, pero no se pudo actualizar la vista. Recarga la ruta antes de volver a subirla."
                        : (error.message || "No se pudo guardar la imagen. Revisa tu conexión e inténtalo de nuevo.");
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
    var groups = form ? form.querySelectorAll(".customer-group") : [];
    function setGroupOpen(group, open) {
        var details = group.querySelector(".customer-documents");
        var toggle = group.querySelector(".client-toggle");
        details.hidden = !open;
        if (toggle) { toggle.setAttribute("aria-expanded", open ? "true" : "false"); toggle.textContent = group.getAttribute("data-saved") === "true" ? (open ? "Ocultar resumen" : "Ver resumen") : (open ? "Ocultar documentos" : "Ver documentos"); }
    }
    Array.prototype.forEach.call(groups, function (group) {
        var toggle = group.querySelector(".client-toggle");
        if (toggle) toggle.addEventListener("click", function () { setGroupOpen(group, group.querySelector(".customer-documents").hidden); });
    });
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
        var upload = card.querySelector(".image-upload-trigger");
        if (upload) upload.hidden = !hasProblem;
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
        updateClientPhoto(card.closest(".customer-group"));
    }
    function clientReady(group) {
        var cards=group.querySelectorAll(".document-card");
        return cards.length > 0 && Array.prototype.every.call(cards, function (card) {
            var result=selectedResult(card), visit=card.querySelector(".visit-result:checked");
            if (!result || !visit) return false;
            return (result.value === "ENTREGADO" && visit.value === "true") ||
                ((result.value === "NO ENTREGADO" || result.value === "INCIDENCIA") &&
                 !!card.querySelector(".delivery-reason").value && !!card.querySelector(".delivery-observation").value.trim());
        });
    }
    function updateClientPhoto(group) {
        if (!group) return;
        var button=group.querySelector(".client-image-upload-trigger");
        if (button) button.disabled = !clientReady(group);
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
        if (group.getAttribute("data-saved") === "true") return;
        group.setAttribute("data-saved", "false");
        group.classList.remove("is-complete");
        group.querySelector(".client-complete-badge").hidden = true;
        group.querySelector(".group-status").textContent = "Cambios sin guardar. Guarda este cliente antes de cerrar la ruta.";
        updateClientPhoto(group);
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
    function completeGroup(group,serverResults) {
        var details=group.querySelector(".customer-documents");
        var cards=group.querySelectorAll(".document-card");
        var photo=group.querySelector(".client-image-link a");
        var photoUrl=photo && !photo.parentNode.hidden ? photo.href : null;
        var summary=document.createElement("div"); summary.className="client-compact-summary";
        var canonical=[];
        var heading=document.createElement("p"), strong=document.createElement("strong");
        strong.textContent="Cliente completado. "; heading.appendChild(strong);
        heading.appendChild(document.createTextNode("Los resultados están bloqueados.")); summary.appendChild(heading);
        var list=document.createElement("ul");
        Array.prototype.forEach.call(cards,function(card) {
            var current=snapshot(card);
            var saved=serverResults && serverResults.filter(function(d){return d.RowId===current.rowId;})[0];
            var result=saved ? {rowId:saved.RowId,visit:String(saved.Visito).toLowerCase(),delivery:saved.Entrega,
                reason:saved.Motivo || "",observation:saved.Observaciones || ""} : current;
            var item=document.createElement("li");
            item.className="completed-document";
            item.setAttribute("data-document",card.getAttribute("data-document"));
            item.setAttribute("data-result",result.delivery);
            item.setAttribute("data-reason",result.reason);
            item.setAttribute("data-observation",result.observation);
            var name=document.createElement("span"), status=document.createElement("strong");
            name.textContent=card.getAttribute("data-document"); status.textContent=result.delivery;
            item.appendChild(name); item.appendChild(status);
            var imageLink=card.querySelector(".document-image-link a");
            if (imageLink && !imageLink.parentNode.hidden) {
                var link=imageLink.cloneNode(true); link.textContent="Ver foto"; item.appendChild(link);
            }
            list.appendChild(item);
            var inputName=card.querySelector("input[name$='.RowId']").name;
            var prefix=inputName.substring(0,inputName.length-"RowId".length);
            canonical.push([prefix+"RowId",result.rowId],[prefix+"Visito",result.visit],
                [prefix+"Entrega",result.delivery],[prefix+"Motivo",result.reason],
                [prefix+"Observaciones",result.observation]);
        });
        summary.appendChild(list);
        if (photoUrl) {
            var finalLink=document.createElement("a"); finalLink.className="client-photo-link";
            finalLink.href=photoUrl; finalLink.target="_blank"; finalLink.rel="noopener";
            finalLink.textContent="Ver foto final del cliente"; summary.appendChild(finalLink);
        }
        Array.prototype.forEach.call(details.querySelectorAll("input,select,textarea,button"),function(control) { control.disabled=true; });
        details.textContent="";
        details.appendChild(summary);
        canonical.forEach(function(pair) {
            var hidden=document.createElement("input"); hidden.type="hidden"; hidden.name=pair[0];
            hidden.value=pair[1]; details.appendChild(hidden);
        });
        group.setAttribute("data-saved","true"); group.classList.add("is-complete");
        group.querySelector(".client-complete-badge").hidden=false;
        setGroupOpen(group,false);
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
            if (!clientReady(group)) { group.querySelector(".group-status").textContent="Resuelve todos los documentos antes de completar el cliente."; return; }
            if (!window.confirm("¿Completar este cliente? Después de guardar no podrás editar sus documentos ni reemplazar su foto final.")) return;
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
            var controls=group.querySelectorAll(".customer-documents input,.customer-documents select,.customer-documents textarea,.customer-documents button");
            Array.prototype.forEach.call(controls,function(control) { control.disabled=true; });
            group.querySelector(".group-status").textContent = "Guardando en POS…";
            window.fetch(endpoint.getAttribute("data-url"), {method:"POST",body:data,credentials:"same-origin",headers:{"Accept":"application/json"}})
                .then(function (response) {
                    if (!/application\/json/i.test(response.headers.get("Content-Type") || "")) throw new Error("La sesión cambió. Inicia sesión y vuelve a guardar.");
                    return response.json();
                }).then(function (response) {
                    if (!response.ok) throw new Error(response.mensaje || "No se pudo guardar el cliente.");
                    completeGroup(group,response.documentos);
                }).catch(function (error) { group.querySelector(".group-status").textContent = error.message || "No se pudo guardar. Inténtalo de nuevo."; })
                .then(function () { if(group.getAttribute("data-saved")!=="true") {
                    Array.prototype.forEach.call(controls,function(control) { control.disabled=false; }); updateClientPhoto(group);
                } });
        });
    });
    function buildSummary() {
        var counts = { "ENTREGADO": 0, "NO ENTREGADO": 0, "INCIDENCIA": 0 };
        var list = document.querySelector("#exception-summary ul");
        list.textContent = "";
        Array.prototype.forEach.call(form.querySelectorAll(".document-card,.completed-document"), function (card) {
            var completed=card.classList.contains("completed-document");
            var result=completed ? card.getAttribute("data-result") : selectedResult(card).value;
            counts[result]++;
            if (result === "ENTREGADO") return;
            var item = document.createElement("li");
            var title = document.createElement("strong");
            title.textContent = card.getAttribute("data-document") + " · " + (result === "INCIDENCIA" ? "Incidencia" : "No entregado");
            var detail = document.createElement("span");
            detail.textContent = completed ?
                (card.getAttribute("data-reason") || "") + ": " + (card.getAttribute("data-observation") || "") :
                card.querySelector(".delivery-reason").value + ": " + card.querySelector(".delivery-observation").value;
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
