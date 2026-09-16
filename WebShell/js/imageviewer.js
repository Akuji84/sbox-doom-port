    // Imaging Preview: renders data-URL images saved by Paint.
(function () {
    'use strict';

    const windowEl = document.getElementById('imageViewerWindow');
    if (!windowEl) return;
    const img = document.getElementById('imageViewerImg');
    const statusEl = document.getElementById('imageViewerStatus');

    window.win98ImageViewerOpen = function (name, dataUrl) {
        img.src = dataUrl;
        const titleEl = windowEl.querySelector('.window-title');
        if (titleEl) {
            const icon = titleEl.querySelector('img');
            titleEl.textContent = '';
            if (icon) titleEl.appendChild(icon);
            titleEl.appendChild(document.createTextNode((name || 'Untitled') + ' - Imaging Preview'));
        }
        img.onload = () => {
            statusEl.textContent = img.naturalWidth + ' x ' + img.naturalHeight + ' pixels';
        };
        window.win98ActivateWindow?.(windowEl);
    };
})();
