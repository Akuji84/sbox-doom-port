    // Controls property sheet (C:\DOOM\CONTROLS.EXE).
    // Key/mouse bindings + game settings, persisted to localStorage so the
    // game bridge can read them via window.win98GetDoomControls().
(function () {
    'use strict';

    const STORAGE_KEY = 'win98ge.doomControls.v1';

    const BINDINGS = [
        { id: 'forward', label: 'Move Forward', def: 'W' },
        { id: 'backward', label: 'Move Backward', def: 'S' },
        { id: 'strafeLeft', label: 'Strafe Left', def: 'A' },
        { id: 'strafeRight', label: 'Strafe Right', def: 'D' },
        { id: 'turnLeft', label: 'Turn Left', def: 'Left Arrow' },
        { id: 'turnRight', label: 'Turn Right', def: 'Right Arrow' },
        { id: 'fire', label: 'Fire', def: 'Mouse 1' },
        { id: 'use', label: 'Use / Open Door', def: 'E' },
        { id: 'run', label: 'Run (hold)', def: 'Shift' },
        { id: 'strafe', label: 'Strafe Mode (hold)', def: 'Alt' },
        { id: 'automap', label: 'Automap', def: 'M' },
        { id: 'weapon1', label: 'Weapon 1 (Fist/Chainsaw)', def: '1' },
        { id: 'weapon2', label: 'Weapon 2 (Pistol)', def: '2' },
        { id: 'weapon3', label: 'Weapon 3 (Shotgun)', def: '3' },
        { id: 'weapon4', label: 'Weapon 4 (Chaingun)', def: '4' },
        { id: 'weapon5', label: 'Weapon 5 (Rocket Launcher)', def: '5' },
        { id: 'weapon6', label: 'Weapon 6 (Plasma Rifle)', def: '6' },
        { id: 'weapon7', label: 'Weapon 7 (BFG 9000)', def: '7' }
    ];

    const DEFAULT_SETTINGS = {
        mouseSensitivity: 5,
        alwaysRun: true,
        showMessages: true,
        uncappedFps: false,
        music: true,
        sfx: true
    };

    function defaultBindings() {
        const map = {};
        BINDINGS.forEach(b => { map[b.id] = b.def; });
        return map;
    }

    // Keep capture aligned with the native mapper. Tab remains the Doom menu key.
    function supportedBinding(value) {
        return value === '' || (typeof value === 'string' && (/^[A-Z0-9]$/.test(value)
            || /^Mouse [1-5]$/.test(value)
            || ['Left Arrow', 'Right Arrow', 'Up Arrow', 'Down Arrow', 'Space', 'Shift', 'Ctrl', 'Alt', 'Enter', 'Backspace'].includes(value)));
    }

    function loadState() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            if (raw) {
                const parsed = JSON.parse(raw);
                const bindings = Object.assign(defaultBindings(), parsed.bindings || {});
                BINDINGS.forEach(b => { if (!supportedBinding(bindings[b.id])) bindings[b.id] = b.def; });
                return {
                    bindings,
                    settings: Object.assign({}, DEFAULT_SETTINGS, parsed.settings || {})
                };
            }
        } catch (err) { /* fall through to defaults */ }
        return { bindings: defaultBindings(), settings: Object.assign({}, DEFAULT_SETTINGS) };
    }

    let saved = loadState();
    let pending = null;
    let capturingId = null;

    window.win98GetDoomControls = function () {
        return JSON.parse(JSON.stringify(saved));
    };

    const windowEl = document.getElementById('controlsWindow');
    const bindList = document.getElementById('ctlBindList');
    const statusbar = document.getElementById('ctl-statusbar');

    function setStatus(text) {
        if (statusbar) statusbar.textContent = text;
    }

    function describeKeyEvent(event) {
        const key = event.key;
        if (key === ' ') return 'Space';
        if (key.length === 1) return key.toUpperCase();
        const names = {
            ArrowLeft: 'Left Arrow',
            ArrowRight: 'Right Arrow',
            ArrowUp: 'Up Arrow',
            ArrowDown: 'Down Arrow',
            Control: 'Ctrl'
        };
        return names[key] || key;
    }

    function renderBindings() {
        if (!bindList || !pending) return;
        bindList.innerHTML = '';
        BINDINGS.forEach(binding => {
            const row = document.createElement('div');
            row.className = 'ctl-bind-row';

            const label = document.createElement('div');
            label.className = 'ctl-bind-label';
            label.textContent = binding.label;

            const box = document.createElement('div');
            box.className = 'cfg-key-input';
            box.dataset.bindingId = binding.id;
            if (capturingId === binding.id) {
                box.classList.add('capturing');
                box.textContent = 'Press a key...';
            } else {
                box.textContent = pending.bindings[binding.id] || '(none)';
            }

            row.appendChild(label);
            row.appendChild(box);
            bindList.appendChild(row);
        });
    }

    function renderSettings() {
        if (!pending) return;
        document.getElementById('ctl-mouse-sensitivity').value = pending.settings.mouseSensitivity;
        document.getElementById('ctl-mouse-sensitivity-label').textContent = pending.settings.mouseSensitivity;
        document.getElementById('ctl-always-run').checked = pending.settings.alwaysRun;
        document.getElementById('ctl-show-messages').checked = pending.settings.showMessages;
        document.getElementById('ctl-uncapped-fps').checked = pending.settings.uncappedFps;
        document.getElementById('ctl-music-enabled').checked = pending.settings.music;
        document.getElementById('ctl-sfx-enabled').checked = pending.settings.sfx;
    }

    function collectSettings() {
        if (!pending) return;
        pending.settings.mouseSensitivity = parseInt(document.getElementById('ctl-mouse-sensitivity').value, 10) || DEFAULT_SETTINGS.mouseSensitivity;
        pending.settings.alwaysRun = document.getElementById('ctl-always-run').checked;
        pending.settings.showMessages = document.getElementById('ctl-show-messages').checked;
        pending.settings.uncappedFps = document.getElementById('ctl-uncapped-fps').checked;
        pending.settings.music = document.getElementById('ctl-music-enabled').checked;
        pending.settings.sfx = document.getElementById('ctl-sfx-enabled').checked;
    }

    function openControlsWindow() {
        pending = JSON.parse(JSON.stringify(saved));
        capturingId = null;
        renderBindings();
        renderSettings();
        setStatus('Ready');
        if (typeof window.win98ActivateWindow === 'function') {
            window.win98ActivateWindow(windowEl);
        }
    }

    function applyPending() {
        collectSettings();
        saved = JSON.parse(JSON.stringify(pending));
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(saved)); } catch (err) {}
        setStatus('Settings saved.');
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('click');
    }

    function closeWindow() {
        capturingId = null;
        windowEl.querySelector('.close-btn')?.click();
    }

    // shell_app registry: filesystem.js routes CONTROLS.EXE here
    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.controlsWindow = openControlsWindow;

    if (typeof window.win98OpenShellApp !== 'function') {
        window.win98OpenShellApp = function (windowId) {
            const hooks = window.win98AppOpenHooks || {};
            if (typeof hooks[windowId] === 'function') {
                hooks[windowId]();
                return true;
            }
            const el = document.getElementById(windowId);
            if (el && typeof window.win98ActivateWindow === 'function') {
                window.win98ActivateWindow(el);
                return true;
            }
            return false;
        };
    }

    // --------------------------------------------------------- capture flow
    function assignBinding(value) {
        if (!capturingId || !pending) return;
        if (!supportedBinding(value)) {
            setStatus(value === 'Tab' ? 'Tab is reserved for the game menu.' : 'That key is not supported by the game. Choose another.');
            return;
        }
        // unassign the value everywhere else, classic config behaviour
        Object.keys(pending.bindings).forEach(id => {
            if (id !== capturingId && pending.bindings[id] === value) {
                pending.bindings[id] = '';
            }
        });
        pending.bindings[capturingId] = value;
        setStatus(value + ' assigned.');
        capturingId = null;
        renderBindings();
    }

    bindList?.addEventListener('click', event => {
        const box = event.target.closest('.cfg-key-input');
        if (!box) return;
        if (capturingId === box.dataset.bindingId) return;
        // a click while another capture is armed assigns Mouse 1 to it,
        // handled by the document mousedown listener below
        capturingId = box.dataset.bindingId;
        setStatus('Press a key or mouse button...');
        renderBindings();
        event.stopPropagation();
    });

    document.addEventListener('keydown', event => {
        if (!capturingId) return;
        event.preventDefault();
        event.stopPropagation();
        if (event.key === 'Escape') {
            capturingId = null;
            setStatus('Cancelled.');
            renderBindings();
            return;
        }
        assignBinding(describeKeyEvent(event));
    }, true);

    document.addEventListener('mousedown', event => {
        if (!capturingId) return;
        const box = event.target.closest('.cfg-key-input');
        if (box && box.dataset.bindingId === capturingId) return; // arming click
        event.preventDefault();
        event.stopPropagation();
        assignBinding('Mouse ' + ([1, 3, 2, 4, 5][event.button] || 0));
    }, true);

    // ------------------------------------------------------------- controls
    document.querySelectorAll('[data-ctl-tab]').forEach(tab => {
        tab.addEventListener('click', () => {
            document.querySelectorAll('[data-ctl-tab]').forEach(t => t.classList.remove('dp-tab-active'));
            tab.classList.add('dp-tab-active');
            const target = tab.dataset.ctlTab;
            ['bindings', 'settings', 'help'].forEach(name => {
                document.getElementById('ctl-panel-' + name).style.display = name === target ? 'flex' : 'none';
            });
        });
    });

    document.getElementById('ctl-mouse-sensitivity').addEventListener('input', event => {
        document.getElementById('ctl-mouse-sensitivity-label').textContent = event.target.value;
    });

    document.getElementById('ctl-ok-btn').addEventListener('click', () => {
        applyPending();
        closeWindow();
    });
    document.getElementById('ctl-cancel-btn').addEventListener('click', closeWindow);
    document.getElementById('ctl-apply-btn').addEventListener('click', applyPending);
    document.getElementById('ctl-defaults-btn').addEventListener('click', () => {
        pending = { bindings: defaultBindings(), settings: Object.assign({}, DEFAULT_SETTINGS) };
        capturingId = null;
        renderBindings();
        renderSettings();
        setStatus('Defaults restored. Click Apply or OK to keep them.');
    });
})();
