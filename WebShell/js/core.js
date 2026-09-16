    // Shell core: start menu toggle, debug panel (?debug=1), clock.
// Start menu toggle
    document.getElementById('startBtn').addEventListener('click', function() {
        document.getElementById('startMenu').classList.toggle('active');
        this.classList.toggle('active');
    });

    // Close start menu when clicking elsewhere
    document.addEventListener('click', function(event) {
        const startMenu = document.getElementById('startMenu');
        const startBtn = document.getElementById('startBtn');

        if (!startMenu.contains(event.target) && !startBtn.contains(event.target)) {
            startMenu.classList.remove('active');
            startBtn.classList.remove('active');
        }
    });

    const shellUrlParams = new URLSearchParams(window.location.search);
    const shellDebugEnabled = shellUrlParams.get('debug') === '1';
    const shellDebugEvents = [];
    let shellDebugPanel = null;

    function ensureShellDebugPanel() {
        if (!shellDebugEnabled || shellDebugPanel) return shellDebugPanel;
        const panel = document.createElement('div');
        panel.id = 'shell-debug-panel';
        panel.style.cssText = 'position:fixed;right:8px;bottom:34px;width:360px;max-height:260px;overflow:auto;z-index:10000;background:#ffffe1;border:2px solid #000;padding:6px;font:11px \"MS Sans Serif\",sans-serif;color:#000;white-space:pre-wrap;';
        panel.innerHTML = '<div style="font-weight:bold;margin-bottom:4px;">WIN98 DEBUG</div><div id="shell-debug-lines"></div>';
        document.body.appendChild(panel);
        shellDebugPanel = panel;
        return panel;
    }

    function updateShellDebugPanel() {
        if (!shellDebugEnabled) return;
        const panel = ensureShellDebugPanel();
        const linesEl = panel.querySelector('#shell-debug-lines');
        if (!linesEl) return;
        linesEl.textContent = shellDebugEvents.slice(-18).map(entry => `${entry.time} [${entry.scope}] ${entry.message}`).join('\n');
    }

    function shellDebug(scope, message, data) {
        const entry = {
            time: new Date().toLocaleTimeString(),
            scope,
            message: data === undefined ? String(message) : `${message} ${JSON.stringify(data)}`
        };
        shellDebugEvents.push(entry);
        if (shellDebugEvents.length > 80) {
            shellDebugEvents.shift();
        }
        console.log(`[win98-debug][${scope}]`, message, data ?? '');
        updateShellDebugPanel();
    }

    // Update clock function with seconds
    function updateClock() {
        const now = new Date();
        let hours = now.getHours();
        const minutes = now.getMinutes();
        const ampm = hours >= 12 ? 'PM' : 'AM';

        hours = hours % 12;
        hours = hours ? hours : 12; // the hour '0' should be '12'

        // Win98 tray clock shows h:mm AM/PM, no seconds
        const timeString = hours + ':' +
            (minutes < 10 ? '0' + minutes : minutes) + ' ' +
            ampm;
        document.querySelector('.tray-time').textContent = timeString;
    }

    updateClock();
    setInterval(updateClock, 1000);
