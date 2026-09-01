(function ($, window, document) {
    "use strict";

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : String(value)).html();
    }

    function formatoBytes(value) {
        var bytes = Number(value) || 0;
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(bytes < 10240 ? 1 : 0) + " KB";
        return (bytes / (1024 * 1024)).toFixed(1) + " MB";
    }

    function esArchivo(adjunto) {
        return String(adjunto && adjunto.Tipo || "").toUpperCase() === "ARCHIVO";
    }

    function icono(adjunto) {
        if (!esArchivo(adjunto)) return "icon-link";
        var tipo = String(adjunto.ContentType || "").toLowerCase();
        var extension = String(adjunto.Extension || "").toLowerCase();
        if (tipo.indexOf("image/") === 0) return "icon-picture";
        if (tipo === "application/pdf") return "icon-file-text";
        if (extension === ".xls" || extension === ".xlsx") return "icon-table";
        return "icon-file";
    }

    function urlArchivo(baseUrl, documento, adjunto, inline) {
        return baseUrl + (baseUrl.indexOf("?") >= 0 ? "&" : "?") +
            "empresa=" + encodeURIComponent(documento.IdEmpresa || "") +
            "&idBorrador=" + encodeURIComponent(documento.IdBorrador || "") +
            "&adjuntoId=" + encodeURIComponent(adjunto.AdjuntoId) +
            (inline ? "&inline=true" : "");
    }

    function asegurarModal() {
        if ($("#bncAttachmentPreviewModal").length) return;
        $("body").append(
            '<div class="modal fade bnc-modal bnc-attachment-preview" id="bncAttachmentPreviewModal" tabindex="-1" role="dialog" aria-labelledby="bncAttachmentPreviewTitle">' +
                '<div class="modal-dialog bnc-attachment-preview-dialog" role="document"><div class="modal-content">' +
                    '<div class="modal-header"><button type="button" class="close" data-dismiss="modal" aria-label="Cerrar"><span aria-hidden="true">&times;</span></button>' +
                    '<h4 class="modal-title" id="bncAttachmentPreviewTitle"><i class="icon-eye-open"></i> Vista previa</h4></div>' +
                    '<div class="modal-body"><div class="bnc-attachment-preview-body"></div></div>' +
                    '<div class="modal-footer"><a class="bnc-btn bnc-btn-ghost bnc-preview-download" href="#"><i class="icon-download-alt"></i> Descargar</a>' +
                    '<button class="bnc-btn bnc-btn-primary" type="button" data-dismiss="modal">Cerrar</button></div>' +
                '</div></div>' +
            '</div>');

        $("#bncAttachmentPreviewModal").on("hidden.bs.modal", function () {
            $(this).find(".bnc-attachment-preview-body").empty();
        });
    }

    function abrirVistaPrevia($control) {
        asegurarModal();
        var url = String($control.data("url") || "");
        var descarga = String($control.data("download") || url);
        var nombre = String($control.data("name") || "Adjunto");
        var tipo = String($control.data("type") || "").toLowerCase();
        var $modal = $("#bncAttachmentPreviewModal");
        var contenido;

        if (tipo.indexOf("image/") === 0) {
            contenido = '<img class="bnc-attachment-preview-image" src="' +
                escapeHtml(url) + '" alt="' + escapeHtml(nombre) + '" />';
        } else {
            contenido = '<iframe class="bnc-attachment-preview-frame" src="' +
                escapeHtml(url) + '" title="Vista previa de ' + escapeHtml(nombre) + '"></iframe>';
        }

        $modal.find("#bncAttachmentPreviewTitle").html(
            '<i class="icon-eye-open"></i> ' + escapeHtml(nombre));
        $modal.find(".bnc-attachment-preview-body").html(contenido);
        $modal.find(".bnc-preview-download").attr("href", descarga);
        $modal.modal("show");
    }

    function plantilla(documento, opciones) {
        opciones = opciones || {};
        var adjuntos = documento && documento.Adjuntos ? documento.Adjuntos : [];
        var titulo = opciones.titulo || "Documentación de respaldo";
        var subtitulo = opciones.subtitulo || "Archivos y enlaces agregados al crear el borrador.";
        var baseUrl = opciones.baseUrl || "";
        var archivos = 0;
        var enlaces = 0;
        var elementos = "";

        $.each(adjuntos, function (_, adjunto) {
            var archivo = esArchivo(adjunto);
            if (archivo) archivos++; else enlaces++;
            var meta = archivo
                ? [String(adjunto.Extension || "archivo").replace(".", "").toUpperCase(), formatoBytes(adjunto.Tamano)].join(" · ")
                : escapeHtml(adjunto.Url || "");
            var acciones;

            if (archivo) {
                var descarga = urlArchivo(baseUrl, documento, adjunto, false);
                var previa = urlArchivo(baseUrl, documento, adjunto, true);
                acciones = adjunto.EsVisualizable
                    ? '<button class="bnc-btn bnc-btn-ghost bnc-btn-compact" type="button" data-bnc-adjunto-preview data-url="' +
                        escapeHtml(previa) + '" data-download="' + escapeHtml(descarga) + '" data-name="' +
                        escapeHtml(adjunto.Nombre) + '" data-type="' + escapeHtml(adjunto.ContentType) +
                        '"><i class="icon-eye-open"></i> Ver</button>'
                    : "";
                acciones += '<a class="bnc-btn bnc-btn-ghost bnc-btn-compact" href="' + escapeHtml(descarga) +
                    '"><i class="icon-download-alt"></i> Descargar</a>';
            } else {
                acciones = '<a class="bnc-btn bnc-btn-ghost bnc-btn-compact" href="' +
                    escapeHtml(adjunto.Url) + '" target="_blank" rel="noopener noreferrer"><i class="icon-external-link"></i> Abrir</a>';
            }

            elementos += '<article class="bnc-support-item">' +
                '<span class="bnc-support-icon"><i class="' + icono(adjunto) + '"></i></span>' +
                '<div class="bnc-support-copy"><strong>' + escapeHtml(adjunto.Nombre || (archivo ? "Archivo" : "Enlace")) +
                '</strong><small title="' + (archivo ? escapeHtml(meta) : escapeHtml(adjunto.Url || "")) + '">' + meta +
                '</small></div><div class="bnc-support-actions">' + acciones + '</div></article>';
        });

        var resumen = adjuntos.length
            ? archivos + (archivos === 1 ? " archivo" : " archivos") + " · " +
                enlaces + (enlaces === 1 ? " enlace" : " enlaces")
            : "Sin adjuntos";
        var cuerpo = adjuntos.length
            ? '<div class="bnc-support-list">' + elementos + '</div>'
            : '<div class="bnc-support-empty"><i class="icon-paper-clip"></i><div><strong>Sin documentación adjunta</strong>' +
                '<span>Este borrador fue creado sin archivos ni enlaces de respaldo.</span></div></div>';

        return '<section class="bnc-support-section" aria-label="' + escapeHtml(titulo) + '">' +
            '<div class="bnc-follow-section-head"><span class="bnc-follow-section-icon"><i class="icon-paper-clip"></i></span>' +
            '<div class="bnc-follow-section-copy"><strong>' + escapeHtml(titulo) + '</strong><span>' +
            escapeHtml(subtitulo) + '</span></div><span class="bnc-support-summary"><i class="icon-ok-circle"></i> ' +
            escapeHtml(resumen) + '</span></div>' + cuerpo + '</section>';
    }

    $(document).on("click", "[data-bnc-adjunto-preview]", function () {
        abrirVistaPrevia($(this));
    });

    window.BorradorNcAdjuntos = {
        plantilla: plantilla,
        formatoBytes: formatoBytes
    };
})(window.jQuery, window, document);
