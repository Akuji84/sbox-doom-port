    // Desktop shell extras: boot sequence, wallpaper + Display Properties,
    // system tray (volume/network), quick launch, empty-desktop context menu,
    // Shut Down / Log Off flows, Flying Windows screensaver and CRT effect.
    // Loaded last; talks to windows.js and filesystem.js through the
    // window.win98* hooks they expose.
(function () {
    'use strict';

    const SETTINGS_KEY = 'win98ge.desktopSettings.v1';

    const WALLPAPERS = [
        { id: 'none', label: '(None)', url: '' },
        { id: 'clouds', label: 'Clouds', url: './assets/wallpapers/clouds.png' },
        { id: 'rivets', label: 'Blue Rivets', url: './assets/wallpapers/blue_rivets.png', defaultMode: 'tile' },
        { id: 'doomplate', label: 'DOOM Plate', url: './assets/wallpapers/doom_plate.png', defaultMode: 'tile' }
    ];

    const DEFAULT_SETTINGS = {
        wallpaper: 'none',
        wallpaperMode: 'stretch',
        screensaver: 'flying',
        screensaverWaitMinutes: 10,
        marqueeText: 's&DOOM - RIP AND TEAR',
        scheme: 'standard',
        crt: false,
        volume: 80,
        muted: false
    };

    // Appearance color schemes (approximations of the Win98 classics).
    const SCHEMES = {
        standard: { label: 'Windows Standard', face: '#c0c0c0', desktop: '#008080', title1: '#000080', title2: '#1084d0', titleI1: '#808080', titleI2: '#b5b5b5', hilight: '#000080' },
        brick: { label: 'Brick', face: '#c0c0c0', desktop: '#808000', title1: '#800000', title2: '#c05030', titleI1: '#808080', titleI2: '#b0a090', hilight: '#800000' },
        desert: { label: 'Desert', face: '#d5ccbb', desktop: '#a28d68', title1: '#6b5f43', title2: '#b8a888', titleI1: '#9a8f78', titleI2: '#cfc4a9', hilight: '#6b5f43' },
        eggplant: { label: 'Eggplant', face: '#c0c0c0', desktop: '#3f6060', title1: '#604080', title2: '#a080c0', titleI1: '#808080', titleI2: '#b5a5c5', hilight: '#604080' },
        rainyday: { label: 'Rainy Day', face: '#c0c0c0', desktop: '#5f6f7f', title1: '#00007b', title2: '#8399b1', titleI1: '#808080', titleI2: '#a9b5c1', hilight: '#00007b' },
        rose: { label: 'Rose', face: '#cfc4c4', desktop: '#806060', title1: '#9f4a62', title2: '#e0a0b0', titleI1: '#a08888', titleI2: '#d0b8b8', hilight: '#9f4a62' }
    };

    function applyScheme() {
        const scheme = SCHEMES[settings.scheme] || SCHEMES.standard;
        const root = document.documentElement.style;
        root.setProperty('--w98-face', scheme.face);
        root.setProperty('--w98-desktop', scheme.desktop);
        root.setProperty('--w98-title1', scheme.title1);
        root.setProperty('--w98-title2', scheme.title2);
        root.setProperty('--w98-title-i1', scheme.titleI1);
        root.setProperty('--w98-title-i2', scheme.titleI2);
        root.setProperty('--w98-hilight', scheme.hilight);
    }

    function loadSettings() {
        try {
            const raw = localStorage.getItem(SETTINGS_KEY);
            if (raw) return Object.assign({}, DEFAULT_SETTINGS, JSON.parse(raw));
        } catch (err) { /* corrupted state falls back to defaults */ }
        return Object.assign({}, DEFAULT_SETTINGS);
    }

    const settings = loadSettings();

    function saveSettings() {
        try { localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings)); } catch (err) {}
    }

    // The game bridge can poll this for the shell mixer level (0..1).
    window.win98GetShellVolume = function () {
        return settings.muted ? 0 : settings.volume / 100;
    };

    // ------------------------------------------------------------ wallpaper
    function wallpaperById(id) {
        return WALLPAPERS.find(wp => wp.id === id) || WALLPAPERS[0];
    }

    function applyWallpaper() {
        const wp = wallpaperById(settings.wallpaper);
        const style = document.body.style;
        if (!wp.url) {
            style.backgroundImage = '';
            style.backgroundRepeat = '';
            style.backgroundSize = '';
            style.backgroundPosition = '';
            return;
        }
        style.backgroundImage = "url('" + wp.url + "')";
        if (settings.wallpaperMode === 'tile') {
            style.backgroundRepeat = 'repeat';
            style.backgroundSize = 'auto';
            style.backgroundPosition = '0 0';
        } else if (settings.wallpaperMode === 'center') {
            style.backgroundRepeat = 'no-repeat';
            style.backgroundSize = 'auto';
            style.backgroundPosition = 'center';
        } else { // stretch
            style.backgroundRepeat = 'no-repeat';
            style.backgroundSize = '100% 100%';
            style.backgroundPosition = '0 0';
        }
    }

    function applyCrt() {
        document.body.classList.toggle('crt-enabled', !!settings.crt);
    }

    const crtOverlay = document.createElement('div');
    crtOverlay.className = 'crt-overlay';
    document.body.appendChild(crtOverlay);

    // Paint's "Set As Wallpaper" stores a data URL; surface it in the list.
    try {
        const custom = localStorage.getItem('win98ge.customWallpaper');
        if (custom) WALLPAPERS.push({ id: 'custom', label: 'Paint Drawing', url: custom });
    } catch (err) {}

    window.win98SetCustomWallpaper = function (dataUrl, mode) {
        try { localStorage.setItem('win98ge.customWallpaper', dataUrl); } catch (err) {}
        const existing = WALLPAPERS.find(wp => wp.id === 'custom');
        if (existing) existing.url = dataUrl;
        else WALLPAPERS.push({ id: 'custom', label: 'Paint Drawing', url: dataUrl });
        settings.wallpaper = 'custom';
        settings.wallpaperMode = mode || 'center';
        saveSettings();
        applyWallpaper();
    };

    window.win98SetShellVolume = function (volume, muted) {
        settings.volume = Math.max(0, Math.min(100, Math.round(volume)));
        if (muted !== undefined) settings.muted = !!muted;
        saveSettings();
        const slider = document.getElementById('trayVolumeSlider');
        const mute = document.getElementById('trayVolumeMute');
        if (slider) slider.value = settings.volume;
        if (mute) mute.checked = settings.muted;
    };

    window.win98GetShellSettings = function () {
        return JSON.parse(JSON.stringify(settings));
    };

    applyWallpaper();
    applyCrt();
    applyScheme();

    // ---------------------------------------------------- display properties
    const dpWindow = document.getElementById('displayProperties');
    let dpPending = null;

    function openDisplayProperties() {
        dpPending = {
            wallpaper: settings.wallpaper,
            wallpaperMode: settings.wallpaperMode,
            screensaver: settings.screensaver,
            screensaverWaitMinutes: settings.screensaverWaitMinutes,
            marqueeText: settings.marqueeText,
            scheme: settings.scheme,
            crt: settings.crt
        };
        renderDisplayProperties();
        if (typeof window.win98ActivateWindow === 'function') {
            window.win98ActivateWindow(dpWindow);
        }
    }
    window.win98OpenDisplayProperties = openDisplayProperties;

    function renderDisplayProperties() {
        if (!dpPending) return;
        const list = document.getElementById('dpWallpaperList');
        list.innerHTML = '';
        WALLPAPERS.forEach(wp => {
            const item = document.createElement('div');
            item.className = 'dp-list-item' + (dpPending.wallpaper === wp.id ? ' selected' : '');
            const img = document.createElement('img');
            img.src = './assets/icons/document.png';
            img.alt = '';
            item.appendChild(img);
            item.appendChild(document.createTextNode(wp.label));
            item.addEventListener('click', () => {
                dpPending.wallpaper = wp.id;
                if (wp.defaultMode) dpPending.wallpaperMode = wp.defaultMode;
                renderDisplayProperties();
            });
            list.appendChild(item);
        });

        document.getElementById('dpWallpaperMode').value = dpPending.wallpaperMode;
        document.getElementById('dpSaverSelect').value = dpPending.screensaver;
        document.getElementById('dpSaverWait').value = dpPending.screensaverWaitMinutes;
        document.getElementById('dpMarqueeText').value = dpPending.marqueeText || '';
        document.getElementById('dpCrt').checked = !!dpPending.crt;

        // appearance
        const schemeSelect = document.getElementById('dpSchemeSelect');
        if (!schemeSelect.options.length) {
            Object.keys(SCHEMES).forEach(id => {
                const option = document.createElement('option');
                option.value = id;
                option.textContent = SCHEMES[id].label;
                schemeSelect.appendChild(option);
            });
        }
        schemeSelect.value = dpPending.scheme || 'standard';
        const preview = SCHEMES[dpPending.scheme] || SCHEMES.standard;
        const pv = document.getElementById('dpSchemePreview');
        pv.style.backgroundColor = preview.desktop;
        pv.querySelector('.dp-pv-window').style.backgroundColor = preview.face;
        pv.querySelector('.dp-pv-title').style.background = 'linear-gradient(90deg, ' + preview.title1 + ', ' + preview.title2 + ')';
        pv.querySelector('.dp-pv-title-i').style.background = 'linear-gradient(90deg, ' + preview.titleI1 + ', ' + preview.titleI2 + ')';
        pv.querySelector('.dp-pv-hilight').style.backgroundColor = preview.hilight;

        // monitor previews
        const wp = wallpaperById(dpPending.wallpaper);
        const screen = document.getElementById('dpMonitorScreen');
        if (wp.url) {
            screen.style.backgroundImage = "url('" + wp.url + "')";
            screen.style.backgroundSize = dpPending.wallpaperMode === 'tile' ? '25%' : '100% 100%';
            screen.style.backgroundRepeat = dpPending.wallpaperMode === 'tile' ? 'repeat' : 'no-repeat';
        } else {
            screen.style.backgroundImage = '';
        }
        const saverScreen = document.getElementById('dpSaverScreen');
        saverScreen.style.backgroundColor = dpPending.screensaver === 'flying' ? '#000000' : '#008080';
    }

    function collectDpInputs() {
        dpPending.wallpaperMode = document.getElementById('dpWallpaperMode').value;
        dpPending.screensaver = document.getElementById('dpSaverSelect').value;
        const wait = parseInt(document.getElementById('dpSaverWait').value, 10);
        dpPending.screensaverWaitMinutes = isNaN(wait) ? DEFAULT_SETTINGS.screensaverWaitMinutes : Math.min(60, Math.max(1, wait));
        dpPending.marqueeText = document.getElementById('dpMarqueeText').value || DEFAULT_SETTINGS.marqueeText;
        dpPending.scheme = document.getElementById('dpSchemeSelect').value || 'standard';
        dpPending.crt = document.getElementById('dpCrt').checked;
    }

    function applyDpPending() {
        collectDpInputs();
        Object.assign(settings, dpPending);
        saveSettings();
        applyWallpaper();
        applyCrt();
        applyScheme();
        resetIdleTimer();
    }

    document.querySelectorAll('.dp-tab').forEach(tab => {
        tab.addEventListener('click', () => {
            document.querySelectorAll('.dp-tab').forEach(t => t.classList.remove('dp-tab-active'));
            tab.classList.add('dp-tab-active');
            const target = tab.dataset.dpTab;
            ['background', 'screensaver', 'appearance', 'effects'].forEach(name => {
                document.getElementById('dp-panel-' + name).style.display = name === target ? 'flex' : 'none';
            });
        });
    });

    ['dpWallpaperMode', 'dpSaverSelect', 'dpSaverWait', 'dpMarqueeText', 'dpSchemeSelect', 'dpCrt'].forEach(id => {
        document.getElementById(id).addEventListener('change', () => {
            collectDpInputs();
            renderDisplayProperties();
        });
    });

    document.getElementById('dp-ok-btn').addEventListener('click', () => {
        applyDpPending();
        dpWindow.querySelector('.close-btn')?.click();
    });
    document.getElementById('dp-cancel-btn').addEventListener('click', () => {
        dpWindow.querySelector('.close-btn')?.click();
    });
    document.getElementById('dp-apply-btn').addEventListener('click', () => {
        applyDpPending();
        renderDisplayProperties();
    });
    document.getElementById('dpSaverPreview').addEventListener('click', event => {
        event.stopPropagation();
        collectDpInputs();
        if (dpPending.screensaver !== 'none') {
            startScreensaver(dpPending.screensaver);
        }
    });

    // ------------------------------------------------- desktop context menu
    document.addEventListener('contextmenu', event => {
        if (event.target.closest('.desktop-icons, .window, .taskbar, .start-menu, .dialog, .win98-context-menu, .volume-popup, .overlay, .boot-screen, .screensaver-canvas, .poweroff-screen')) {
            return;
        }
        event.preventDefault();
        if (typeof window.win98ShowDesktopContextMenu === 'function') {
            window.win98ShowDesktopContextMenu(event.clientX, event.clientY);
        }
    });

    // ------------------------------------------------- draggable desktop icons
    const ICON_POS_KEY = 'win98ge.iconPositions.v1';
    const iconsContainer = document.querySelector('.desktop-icons');

    function loadIconPositions() {
        try { return JSON.parse(localStorage.getItem(ICON_POS_KEY) || '{}'); } catch (err) { return {}; }
    }

    function saveIconPositions(positions) {
        try { localStorage.setItem(ICON_POS_KEY, JSON.stringify(positions)); } catch (err) {}
    }

    function iconKey(icon) {
        return icon.dataset.path || icon.dataset.window ||
            (icon.querySelector('.icon-text')?.textContent || '').trim();
    }

    function applyIconPositions() {
        const positions = loadIconPositions();
        document.querySelectorAll('.desktop-icon').forEach(icon => {
            const saved = positions[iconKey(icon)];
            if (saved) {
                icon.style.position = 'absolute';
                icon.style.left = saved.x + 'px';
                icon.style.top = saved.y + 'px';
            }
        });
    }

    window.win98ArrangeIcons = function () {
        saveIconPositions({});
        document.querySelectorAll('.desktop-icon').forEach(icon => {
            icon.style.position = '';
            icon.style.left = '';
            icon.style.top = '';
        });
    };

    window.win98LineUpIcons = function () {
        const positions = loadIconPositions();
        Object.keys(positions).forEach(key => {
            positions[key].x = Math.round(positions[key].x / 75) * 75;
            positions[key].y = Math.round(positions[key].y / 75) * 75;
        });
        saveIconPositions(positions);
        applyIconPositions();
    };

    let iconDrag = null;

    document.addEventListener('mousedown', event => {
        if (event.button !== 0) return;
        const icon = event.target.closest('.desktop-icon');
        if (!icon) return;
        iconDrag = {
            icon,
            startX: event.clientX,
            startY: event.clientY,
            rect: icon.getBoundingClientRect(),
            moved: false
        };
    });

    document.addEventListener('mousemove', event => {
        if (!iconDrag) return;
        const dx = event.clientX - iconDrag.startX;
        const dy = event.clientY - iconDrag.startY;
        if (!iconDrag.moved && Math.abs(dx) + Math.abs(dy) < 6) return;
        iconDrag.moved = true;
        const parentRect = iconsContainer.getBoundingClientRect();
        iconDrag.icon.style.position = 'absolute';
        iconDrag.icon.style.left = Math.max(0, iconDrag.rect.left - parentRect.left + dx) + 'px';
        iconDrag.icon.style.top = Math.max(0, iconDrag.rect.top - parentRect.top + dy) + 'px';
    });

    document.addEventListener('mouseup', () => {
        if (!iconDrag) return;
        const state = iconDrag;
        iconDrag = null;
        if (!state.moved) return;
        const positions = loadIconPositions();
        positions[iconKey(state.icon)] = {
            x: parseInt(state.icon.style.left, 10) || 0,
            y: parseInt(state.icon.style.top, 10) || 0
        };
        saveIconPositions(positions);
        suppressNextClick = true; // don't let the drop open the icon
    });

    // filesystem.js re-renders its desktop icons; re-apply saved positions
    if (iconsContainer) {
        new MutationObserver(() => applyIconPositions()).observe(iconsContainer, { childList: true });
    }
    applyIconPositions();

    // ------------------------------------------- dynamic Start menu submenus
    function buildStartMenuEntry(iconSrc, label, onOpen) {
        const item = document.createElement('div');
        item.className = 'menu-item';
        const img = document.createElement('img');
        img.src = iconSrc;
        img.alt = '';
        item.appendChild(img);
        item.appendChild(document.createTextNode(label));
        item.addEventListener('click', event => {
            document.getElementById('startMenu').classList.remove('active');
            document.getElementById('startBtn').classList.remove('active');
            event.stopPropagation();
            onOpen();
        });
        return item;
    }

    function populateStartDynamicMenus() {
        const docsEl = document.getElementById('menuDocsDynamic');
        if (docsEl && window.win98FsHelpers) {
            docsEl.innerHTML = '';
            const docs = window.win98FsHelpers.children('C:/My Documents')
                .filter(child => child.type === 'text')
                .slice(0, 8);
            docs.forEach(doc => {
                docsEl.appendChild(buildStartMenuEntry('./assets/icons/document.png', doc.name,
                    () => window.win98OpenPath?.(doc.path, '')));
            });
        }

        const favsEl = document.getElementById('menuFavsDynamic');
        if (favsEl) {
            favsEl.innerHTML = '';
            let favorites = [];
            try { favorites = JSON.parse(localStorage.getItem('win98ge.ieFavorites.v1') || '[]'); } catch (err) {}
            favorites.slice(0, 8).forEach(fav => {
                favsEl.appendChild(buildStartMenuEntry('./assets/icons/internet_explorer.png',
                    fav.title || fav.url,
                    () => window.win98IENavigate?.(fav.url)));
            });
        }
    }

    document.getElementById('startBtn')?.addEventListener('click', populateStartDynamicMenus);

    // ------------------------------------------------- rubber-band selection
    const selectionBox = document.querySelector('.selection-box');
    let bandStart = null;
    let bandActive = false;
    let suppressNextClick = false;

    document.addEventListener('mousedown', event => {
        if (event.button !== 0 || !selectionBox) return;
        if (event.target.closest('.desktop-icon, .window, .taskbar, .start-menu, .dialog, .win98-context-menu, .volume-popup, .overlay, .boot-screen, .screensaver-canvas, .poweroff-screen, .bsod-screen')) return;
        bandStart = { x: event.clientX, y: event.clientY };
        bandActive = false;
    });

    document.addEventListener('mousemove', event => {
        if (!bandStart || !selectionBox) return;
        const dx = event.clientX - bandStart.x;
        const dy = event.clientY - bandStart.y;
        if (!bandActive && Math.abs(dx) + Math.abs(dy) < 5) return;
        bandActive = true;
        const left = Math.min(event.clientX, bandStart.x);
        const top = Math.min(event.clientY, bandStart.y);
        const width = Math.abs(dx);
        const height = Math.abs(dy);
        selectionBox.style.display = 'block';
        selectionBox.style.left = left + 'px';
        selectionBox.style.top = top + 'px';
        selectionBox.style.width = width + 'px';
        selectionBox.style.height = height + 'px';
        document.querySelectorAll('.desktop-icon').forEach(icon => {
            const rect = icon.getBoundingClientRect();
            const hit = rect.left < left + width && rect.right > left && rect.top < top + height && rect.bottom > top;
            icon.classList.toggle('active', hit);
        });
    });

    document.addEventListener('mouseup', () => {
        if (!bandStart) return;
        const dragged = bandActive;
        bandStart = null;
        bandActive = false;
        if (selectionBox) selectionBox.style.display = 'none';
        if (dragged) suppressNextClick = true; // keep the selection visible
    });

    document.addEventListener('click', event => {
        if (suppressNextClick) {
            suppressNextClick = false;
            event.stopPropagation();
            event.preventDefault();
        }
    }, true);

    // ------------------------------------------------- taskbar context menu
    function visibleWindows() {
        return Array.from(document.querySelectorAll('.window')).filter(w => w.style.display !== 'none');
    }

    function arrangeWindows(mode) {
        const wins = visibleWindows();
        if (!wins.length) return;
        const availH = window.innerHeight - 28;
        wins.forEach((w, i) => {
            w.classList.remove('maximized');
            w.style.transform = 'none';
            if (mode === 'cascade') {
                w.style.left = (30 + i * 26) + 'px';
                w.style.top = (30 + i * 26) + 'px';
            } else if (mode === 'tileH') {
                const h = Math.max(140, Math.floor(availH / wins.length));
                w.style.left = '0px';
                w.style.top = (i * h) + 'px';
                w.style.width = window.innerWidth + 'px';
                w.style.height = h + 'px';
            } else if (mode === 'tileV') {
                const wWidth = Math.max(300, Math.floor(window.innerWidth / wins.length));
                w.style.left = (i * wWidth) + 'px';
                w.style.top = '0px';
                w.style.width = wWidth + 'px';
                w.style.height = availH + 'px';
            }
        });
    }

    document.querySelector('.taskbar')?.addEventListener('contextmenu', event => {
        event.preventDefault();
        event.stopPropagation();
        if (typeof window.win98ShowContextMenuItems !== 'function') return;

        // per-window menu when right-clicking a taskbar button
        const taskbarItem = event.target.closest('.taskbar-item');
        if (taskbarItem) {
            const target = document.getElementById(taskbarItem.getAttribute('data-window') || '');
            if (target) {
                window.win98ShowContextMenuItems(event.clientX, Math.max(40, event.clientY - 120), [
                    { label: 'Restore', action: () => window.win98ActivateWindow?.(target) },
                    { label: 'Minimize', action: () => window.win98MinimizeWindow?.(target.id) },
                    { separator: true },
                    { label: 'Close', action: () => window.win98CloseWindow?.(target) }
                ]);
                return;
            }
        }

        window.win98ShowContextMenuItems(event.clientX, Math.max(40, event.clientY - 150), [
            { label: 'Cascade Windows', action: () => arrangeWindows('cascade') },
            { label: 'Tile Windows Horizontally', action: () => arrangeWindows('tileH') },
            { label: 'Tile Windows Vertically', action: () => arrangeWindows('tileV') },
            { separator: true },
            { label: 'Minimize All Windows', action: () => window.win98MinimizeAllWindows?.() },
            { separator: true },
            { label: 'Properties', action: () => window.win98OpenDisplayProperties?.() }
        ]);
    });

    // ------------------------------------------------------------ tray icons
    const volumePopup = document.createElement('div');
    volumePopup.className = 'volume-popup';
    volumePopup.innerHTML =
        '<div class="volume-label">Volume</div>' +
        '<input type="range" id="trayVolumeSlider" min="0" max="100">' +
        '<label class="volume-mute"><input type="checkbox" id="trayVolumeMute"> Mute</label>';
    document.body.appendChild(volumePopup);

    const volumeSlider = volumePopup.querySelector('#trayVolumeSlider');
    const volumeMute = volumePopup.querySelector('#trayVolumeMute');
    volumeSlider.value = settings.volume;
    volumeMute.checked = settings.muted;

    volumeSlider.addEventListener('input', () => {
        settings.volume = parseInt(volumeSlider.value, 10) || 0;
        saveSettings();
    });
    volumeMute.addEventListener('change', () => {
        settings.muted = volumeMute.checked;
        saveSettings();
    });

    document.getElementById('trayVolume').addEventListener('click', event => {
        event.stopPropagation();
        volumePopup.classList.toggle('active');
    });

    document.addEventListener('click', event => {
        if (!event.target.closest('.volume-popup') && event.target.id !== 'trayVolume') {
            volumePopup.classList.remove('active');
        }
    });

    // ---------------------------------------------------------- quick launch
    document.querySelectorAll('.quick-launch-btn').forEach(btn => {
        btn.addEventListener('click', event => {
            event.stopPropagation();
            if (btn.id === 'quickShowDesktop') {
                if (typeof window.win98MinimizeAllWindows === 'function') {
                    window.win98MinimizeAllWindows();
                }
                return;
            }
            const windowId = btn.dataset.window;
            if (windowId === 'doomWindow' && typeof window.win98OpenPath === 'function') {
                window.win98OpenPath('C:/DOOM', '');
                return;
            }
            if (windowId && typeof window.win98ActivateWindow === 'function') {
                window.win98ActivateWindow(document.getElementById(windowId));
            }
        });
    });

    // -------------------------------------------------- shut down / log off
    const ditherOverlay = document.createElement('div');
    ditherOverlay.className = 'dither-overlay';
    document.body.appendChild(ditherOverlay);

    const overlayEl = document.getElementById('overlay');

    function openSystemDialog(dialogId) {
        ditherOverlay.classList.add('active');
        overlayEl.style.display = 'block';
        document.getElementById(dialogId).style.display = 'block';
    }

    function closeSystemDialog(dialogId) {
        ditherOverlay.classList.remove('active');
        overlayEl.style.display = 'none';
        document.getElementById(dialogId).style.display = 'none';
    }

    document.getElementById('menuShutDown').addEventListener('click', () => openSystemDialog('shutdown-dialog'));
    document.getElementById('menuLogOff').addEventListener('click', () => openSystemDialog('logoff-dialog'));

    document.getElementById('shutdown-cancel-btn').addEventListener('click', () => closeSystemDialog('shutdown-dialog'));
    document.getElementById('shutdown-help-btn').addEventListener('click', () => closeSystemDialog('shutdown-dialog'));
    document.getElementById('logoff-no-btn').addEventListener('click', () => closeSystemDialog('logoff-dialog'));

    function showPowerOffScreen() {
        const screen = document.createElement('div');
        screen.className = 'poweroff-screen';
        screen.textContent = "It's now safe to turn off\nyour computer.";
        screen.style.whiteSpace = 'pre-wrap';
        document.body.appendChild(screen);
        // Real hardware stayed here forever; a click brings the machine back.
        setTimeout(() => {
            screen.addEventListener('click', () => {
                try { sessionStorage.removeItem('win98ge.bootShown'); } catch (err) {}
                window.location.reload();
            });
        }, 1500);
    }

    function showDosPromptThenReload() {
        const screen = document.createElement('div');
        screen.className = 'poweroff-screen';
        screen.style.color = '#c0c0c0';
        screen.style.fontSize = '15px';
        screen.style.alignItems = 'flex-start';
        screen.style.justifyContent = 'flex-start';
        screen.style.padding = '18px 22px';
        screen.style.textAlign = 'left';
        screen.textContent = 'Microsoft(R) Windows 98\n   (C)Copyright Microsoft Corp 1981-1998.\n\nC:\\WINDOWS>';
        screen.style.whiteSpace = 'pre-wrap';
        document.body.appendChild(screen);
        setTimeout(() => { try { sessionStorage.removeItem('win98ge.bootShown'); } catch (err) {} window.location.reload(); }, 1800);
    }

    function beginShutdown(choice) {
        closeSystemDialog('shutdown-dialog');
        window.win98MarkCleanShutdown?.();
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('shutdown');
        const shuttingDown = document.createElement('div');
        shuttingDown.className = 'poweroff-screen';
        shuttingDown.style.color = '#ffffff';
        shuttingDown.style.fontSize = '18px';
        shuttingDown.textContent = 'Windows is shutting down...';
        document.body.appendChild(shuttingDown);
        setTimeout(() => {
            shuttingDown.remove();
            if (choice === 'shutdown') {
                showPowerOffScreen();
            } else if (choice === 'dos') {
                showDosPromptThenReload();
            } else {
                // Restart replays the boot sequence like real hardware.
                try { sessionStorage.removeItem('win98ge.bootShown'); } catch (err) {}
                window.location.reload();
            }
        }, 1400);
    }

    document.getElementById('shutdown-ok-btn').addEventListener('click', () => {
        const checked = document.querySelector('input[name="shutdownChoice"]:checked');
        beginShutdown(checked ? checked.value : 'shutdown');
    });

    document.getElementById('logoff-yes-btn').addEventListener('click', () => {
        closeSystemDialog('logoff-dialog');
        window.win98MarkCleanShutdown?.();
        const screen = document.createElement('div');
        screen.className = 'poweroff-screen';
        screen.style.color = '#ffffff';
        screen.style.fontSize = '18px';
        screen.textContent = 'Logging off...';
        document.body.appendChild(screen);
        setTimeout(() => {
            try { sessionStorage.setItem('win98ge.showLogin', '1'); } catch (err) {}
            window.location.reload();
        }, 1200);
    });

    // ------------------------------------------------------------- startup
    function playStartupChime() {
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('startup');
    }

    const BIOS_LINES = [
        'UAC BIOS v4.51PG, An Energy Star Ally',
        'Copyright (C) 1984-98, Union Aerospace Corp.',
        '',
        'PENTIUM II-MMX CPU at 266MHz',
        'Memory Test :  {MEM}K OK',
        '',
        'Detecting IDE Primary Master ... DOOM HDD',
        'Detecting IDE Primary Slave  ... CD-ROM',
        '',
        'Booting from Hard Disk...',
        'Starting Windows 98...'
    ];

    function shouldBoot() {
        const params = new URLSearchParams(window.location.search);
        if (params.get('boot') === '0') return false;
        if (params.get('boot') === '1') return true;
        try {
            return sessionStorage.getItem('win98ge.bootShown') !== '1';
        } catch (err) {
            return true;
        }
    }

    function runBootSequence() {
        try { sessionStorage.setItem('win98ge.bootShown', '1'); } catch (err) {}

        const boot = document.createElement('div');
        boot.className = 'boot-screen';
        const bios = document.createElement('div');
        bios.className = 'boot-bios';
        const splash = document.createElement('div');
        splash.className = 'boot-splash';
        const logo = document.createElement('img');
        logo.src = './assets/icons/windows98_logo.png';
        logo.alt = 'Windows 98';
        const bar = document.createElement('div');
        bar.className = 'boot-splash-bar';
        splash.appendChild(logo);
        splash.appendChild(bar);
        boot.appendChild(bios);
        boot.appendChild(splash);
        document.body.appendChild(boot);

        let finished = false;
        const timers = [];

        function finishBoot() {
            if (finished) return;
            finished = true;
            timers.forEach(clearTimeout);
            boot.style.transition = 'opacity 0.45s';
            boot.style.opacity = '0';
            setTimeout(() => boot.remove(), 500);
            playStartupChime();
            document.removeEventListener('keydown', finishBoot, true);
        }

        boot.addEventListener('click', finishBoot);
        document.addEventListener('keydown', finishBoot, true);

        // Type BIOS lines out, animating the memory counter.
        let elapsed = 200;
        const lines = [];
        BIOS_LINES.forEach(line => {
            if (line.includes('{MEM}')) {
                // count 0 -> 65536 in steps
                const steps = 8;
                for (let s = 1; s <= steps; s++) {
                    const value = Math.round(65536 * s / steps);
                    const snapshot = lines.concat(line.replace('{MEM}', String(value)));
                    timers.push(setTimeout(() => { bios.textContent = snapshot.join('\n'); }, elapsed));
                    elapsed += 110;
                }
                lines.push(line.replace('{MEM}', '65536'));
                return;
            }
            lines.push(line);
            const snapshot = lines.slice();
            timers.push(setTimeout(() => { bios.textContent = snapshot.join('\n'); }, elapsed));
            elapsed += line === '' ? 90 : 260;
        });

        // If the last session ended uncleanly, run ScanDisk before the splash.
        let scanDelay = 0;
        if (window.win98BootWasDirty) {
            const scan = document.createElement('div');
            scan.className = 'scandisk-screen';
            scan.innerHTML =
                '<div class="scandisk-title">Microsoft ScanDisk</div>' +
                '<div class="scandisk-body">Because Windows was not properly shut down, one or more of your\n' +
                'disk drives may have errors on it.\n\n' +
                'To avoid seeing this message again, always shut down your computer\n' +
                'from the Shut Down menu.\n\n' +
                'ScanDisk is now checking drive C: for errors:</div>' +
                '<div class="scandisk-bar"><div class="scandisk-fill" id="scandiskFill"></div></div>' +
                '<div class="scandisk-status" id="scandiskStatus">Checking file allocation tables...</div>';
            scan.style.display = 'none';
            boot.appendChild(scan);

            const SCAN_STEPS = [
                [0, 'Checking file allocation tables...'],
                [18, 'Checking directory structure...'],
                [42, 'Checking file system...'],
                [66, 'Checking free space...'],
                [88, 'Checking surface...'],
                [100, 'ScanDisk found no errors on this drive.']
            ];
            timers.push(setTimeout(() => { scan.style.display = 'flex'; }, elapsed + 300));
            SCAN_STEPS.forEach(([percent, label], index) => {
                timers.push(setTimeout(() => {
                    const fill = document.getElementById('scandiskFill');
                    const status = document.getElementById('scandiskStatus');
                    if (fill) fill.style.width = percent + '%';
                    if (status) status.textContent = label;
                }, elapsed + 500 + index * 650));
            });
            scanDelay = 500 + SCAN_STEPS.length * 650 + 500;
            timers.push(setTimeout(() => { scan.style.display = 'none'; }, elapsed + 300 + scanDelay));
        }

        // Splash after the BIOS (and ScanDisk) finishes, desktop after the splash.
        timers.push(setTimeout(() => { splash.style.display = 'flex'; }, elapsed + 300 + scanDelay));
        timers.push(setTimeout(finishBoot, elapsed + 300 + scanDelay + 2600));
    }

    // -------------------------------------------------- flying windows saver
    let saverCanvas = null;
    let saverRaf = null;
    let saverStartPoint = null;
    let idleTimer = null;
    const flagImage = new Image();
    flagImage.src = './assets/icons/start_flag.png';

    // Each screensaver returns a frame() function drawing one frame.
    const SAVERS = {
        flying: function (canvas, ctx) {
            const flags = [];
            const newFlag = seedDepth => ({
                angle: Math.random() * Math.PI * 2,
                dist: seedDepth ? Math.random() * 0.9 : 0.02,
                speed: 0.003 + Math.random() * 0.006
            });
            for (let i = 0; i < 28; i++) flags.push(newFlag(true));
            return function frame() {
                const w = canvas.width;
                const h = canvas.height;
                ctx.fillStyle = '#000000';
                ctx.fillRect(0, 0, w, h);
                const reach = Math.max(w, h) * 0.75;
                flags.forEach((flag, i) => {
                    flag.dist += flag.speed * (1 + flag.dist * 4);
                    if (flag.dist > 1.15) flags[i] = newFlag(false);
                    const x = w / 2 + Math.cos(flag.angle) * flag.dist * reach;
                    const y = h / 2 + Math.sin(flag.angle) * flag.dist * reach;
                    const size = Math.max(8, flag.dist * 72);
                    if (flagImage.complete) {
                        ctx.drawImage(flagImage, x - size / 2, y - size / 2, size, size);
                    }
                });
            };
        },
        starfield: function (canvas, ctx) {
            const stars = [];
            const newStar = seedDepth => ({
                angle: Math.random() * Math.PI * 2,
                dist: seedDepth ? Math.random() : 0.01,
                speed: 0.002 + Math.random() * 0.008
            });
            for (let i = 0; i < 140; i++) stars.push(newStar(true));
            return function frame() {
                const w = canvas.width;
                const h = canvas.height;
                ctx.fillStyle = '#000000';
                ctx.fillRect(0, 0, w, h);
                const reach = Math.max(w, h) * 0.72;
                ctx.fillStyle = '#ffffff';
                stars.forEach((star, i) => {
                    star.dist += star.speed * (1 + star.dist * 5);
                    if (star.dist > 1.2) stars[i] = newStar(false);
                    const x = w / 2 + Math.cos(star.angle) * star.dist * reach;
                    const y = h / 2 + Math.sin(star.angle) * star.dist * reach;
                    const size = Math.max(1, star.dist * 3.2);
                    ctx.fillRect(x, y, size, size);
                });
            };
        },
        marquee: function (canvas, ctx) {
            let x = canvas.width;
            const text = settings.marqueeText || DEFAULT_SETTINGS.marqueeText;
            return function frame() {
                ctx.fillStyle = '#000000';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.font = 'bold 64px "Times New Roman", serif';
                ctx.fillStyle = '#ff0000';
                ctx.textBaseline = 'middle';
                const width = ctx.measureText(text).width;
                ctx.fillText(text, x, canvas.height / 2);
                x -= 3;
                if (x < -width) x = canvas.width;
            };
        },
        mystify: function (canvas, ctx) {
            function newPoly(color) {
                const points = [];
                for (let i = 0; i < 4; i++) {
                    points.push({
                        x: Math.random() * canvas.width,
                        y: Math.random() * canvas.height,
                        vx: (Math.random() * 4 + 2) * (Math.random() < 0.5 ? -1 : 1),
                        vy: (Math.random() * 4 + 2) * (Math.random() < 0.5 ? -1 : 1)
                    });
                }
                return { points, color, trail: [] };
            }
            const polys = [newPoly('#00ffff'), newPoly('#ff00ff')];
            ctx.fillStyle = '#000000';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            return function frame() {
                ctx.fillStyle = 'rgba(0, 0, 0, 0.10)';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                polys.forEach(poly => {
                    poly.points.forEach(p => {
                        p.x += p.vx;
                        p.y += p.vy;
                        if (p.x < 0 || p.x > canvas.width) { p.vx *= -1; p.x = Math.max(0, Math.min(canvas.width, p.x)); }
                        if (p.y < 0 || p.y > canvas.height) { p.vy *= -1; p.y = Math.max(0, Math.min(canvas.height, p.y)); }
                    });
                    ctx.strokeStyle = poly.color;
                    ctx.beginPath();
                    poly.points.forEach((p, i) => {
                        if (i === 0) ctx.moveTo(p.x, p.y); else ctx.lineTo(p.x, p.y);
                    });
                    ctx.closePath();
                    ctx.stroke();
                });
            };
        }
    };

    function startScreensaver(saverId) {
        if (saverCanvas) return;
        if (document.querySelector('.boot-screen') || document.querySelector('.poweroff-screen') || document.querySelector('.bsod-screen')) return;
        const makeFrame = SAVERS[saverId || settings.screensaver];
        if (!makeFrame) return;

        saverCanvas = document.createElement('canvas');
        saverCanvas.className = 'screensaver-canvas';
        saverCanvas.width = window.innerWidth;
        saverCanvas.height = window.innerHeight;
        document.body.appendChild(saverCanvas);
        saverStartPoint = null;

        const ctx = saverCanvas.getContext('2d');
        ctx.imageSmoothingEnabled = false;
        const drawFrame = makeFrame(saverCanvas, ctx);
        function loop() {
            drawFrame();
            saverRaf = requestAnimationFrame(loop);
        }
        saverRaf = requestAnimationFrame(loop);
    }

    function stopScreensaver() {
        if (!saverCanvas) return;
        cancelAnimationFrame(saverRaf);
        saverCanvas.remove();
        saverCanvas = null;
        saverStartPoint = null;
    }

    function resetIdleTimer() {
        if (idleTimer) clearTimeout(idleTimer);
        if (settings.screensaver === 'none') return;
        idleTimer = setTimeout(() => startScreensaver(), settings.screensaverWaitMinutes * 60 * 1000);
    }

    function onActivity(event) {
        if (saverCanvas) {
            if (event.type === 'mousemove') {
                // ignore tiny jitter right after the saver starts
                if (!saverStartPoint) {
                    saverStartPoint = { x: event.clientX, y: event.clientY };
                } else if (Math.abs(event.clientX - saverStartPoint.x) + Math.abs(event.clientY - saverStartPoint.y) > 5) {
                    stopScreensaver();
                }
            } else {
                stopScreensaver();
            }
        }
        resetIdleTimer();
    }

    ['mousemove', 'mousedown', 'keydown', 'wheel', 'touchstart'].forEach(type => {
        document.addEventListener(type, onActivity, { passive: true, capture: true });
    });

    window.addEventListener('resize', () => {
        if (saverCanvas) {
            saverCanvas.width = window.innerWidth;
            saverCanvas.height = window.innerHeight;
        }
    });

    resetIdleTimer();

    if (shouldBoot()) {
        runBootSequence();
    }
})();
