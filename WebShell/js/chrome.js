    // Window chrome factory.
    // Windows in index.html declare data-window-title / data-window-icon /
    // data-window-controls and this builds the titlebar markup for them, so
    // every titlebar is generated from one place instead of copy-pasted HTML.
    // Markup produced here matches the original hand-written titlebars exactly
    // (same classes), so all existing CSS and taskbar code keeps working.
    function buildWindowChrome(windowEl) {
        if (!windowEl || windowEl.querySelector(':scope > .window-titlebar')) return;

        const title = windowEl.dataset.windowTitle || '';
        const icon = windowEl.dataset.windowIcon || '';
        const controls = (windowEl.dataset.windowControls || 'minimize,maximize,close')
            .split(',')
            .map(name => name.trim())
            .filter(Boolean);

        const titleBar = document.createElement('div');
        titleBar.className = 'window-titlebar';

        const titleEl = document.createElement('div');
        titleEl.className = 'window-title';
        if (icon) {
            const img = document.createElement('img');
            img.src = icon;
            img.alt = title;
            titleEl.appendChild(img);
        }
        titleEl.appendChild(document.createTextNode(title));

        const controlsEl = document.createElement('div');
        controlsEl.className = 'window-controls';
        const controlDefs = {
            minimize: { className: 'minimize-btn', glyph: '_' },
            maximize: { className: 'maximize-btn', glyph: '□' },
            close: { className: 'close-btn', glyph: '×' }
        };
        controls.forEach(name => {
            const def = controlDefs[name];
            if (!def) return;
            const btn = document.createElement('div');
            btn.className = 'window-control ' + def.className;
            btn.textContent = def.glyph;
            controlsEl.appendChild(btn);
        });

        titleBar.appendChild(titleEl);
        titleBar.appendChild(controlsEl);
        windowEl.insertBefore(titleBar, windowEl.firstChild);

        // resize handles on all edges and corners (drag logic in windows.js)
        ['n', 'e', 's', 'w', 'ne', 'se', 'sw', 'nw'].forEach(dir => {
            const handle = document.createElement('div');
            handle.className = 'resize-handle resize-' + dir;
            handle.dataset.resizeDir = dir;
            windowEl.appendChild(handle);
        });
    }

    document.querySelectorAll('.window').forEach(buildWindowChrome);

    // Windows created at runtime can opt in by setting the data attributes
    // and calling this before showing the element.
    window.win98BuildWindowChrome = buildWindowChrome;
