    // System extras: Run dialog, Ctrl+Alt+Del Close Program (with BSOD),
    // and the dirty-shutdown flag that triggers ScanDisk during boot.
    // Loads before desktop.js, which consumes win98BootWasDirty.
(function () {
    'use strict';

    const DIRTY_KEY = 'win98ge.dirtyShutdown';

    // ------------------------------------------------- dirty shutdown flag
    let wasDirty = false;
    try {
        wasDirty = localStorage.getItem(DIRTY_KEY) === '1';
        localStorage.setItem(DIRTY_KEY, '1'); // session now in progress
    } catch (err) { /* storage unavailable */ }

    window.win98BootWasDirty = wasDirty;
    window.win98MarkCleanShutdown = function () {
        try { localStorage.setItem(DIRTY_KEY, '0'); } catch (err) {}
    };

    // Closing the tab normally counts as a clean shutdown; only hard kills
    // (crash, process kill) leave the flag dirty and trigger ScanDisk.
    window.addEventListener('pagehide', window.win98MarkCleanShutdown);

    const overlayEl = document.getElementById('overlay');

    function openDialog(id) {
        overlayEl.style.display = 'block';
        document.getElementById(id).style.display = 'block';
    }

    function closeDialog(id) {
        overlayEl.style.display = 'none';
        document.getElementById(id).style.display = 'none';
    }

    function showSystemError(message) {
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('error');
        document.getElementById('runErrorText').textContent = message;
        document.getElementById('run-dialog').style.display = 'none';
        openDialog('run-error-dialog');
    }

    // ------------------------------------------------------------ Run...
    const RUN_TARGETS = {
        notepad: 'notepadWindow',
        calc: 'calculatorWindow',
        calculator: 'calculatorWindow',
        winmine: 'minesweeperWindow',
        minesweeper: 'minesweeperWindow',
        sol: 'solitaireWindow',
        solitaire: 'solitaireWindow',
        mspaint: 'paintWindow',
        paint: 'paintWindow',
        pbrush: 'paintWindow',
        iexplore: 'internetExplorer',
        controls: 'controlsWindow'
    };

    function executeRunCommand(raw) {
        const command = String(raw || '').trim().toLowerCase().replace(/\.exe$/, '');
        if (!command) return;

        if (command === 'format c:' || command === 'format c') {
            showSystemError('Access is denied. The disk is in use by DOOM. (Nice try.)');
            return;
        }
        if (command === 'doom' || command === 'c:\\doom') {
            closeDialog('run-dialog');
            window.win98OpenPath?.('C:/DOOM', '');
            return;
        }
        if (command === 'explorer' || command === 'my computer') {
            closeDialog('run-dialog');
            window.win98ActivateWindow?.(document.getElementById('myComputer'));
            return;
        }

        const windowId = RUN_TARGETS[command];
        if (windowId) {
            closeDialog('run-dialog');
            if (typeof window.win98OpenShellApp === 'function' && window.win98OpenShellApp(windowId)) return;
            window.win98ActivateWindow?.(document.getElementById(windowId));
            return;
        }

        showSystemError("Cannot find the file '" + command + "' (or one of its components). " +
            'Make sure the path and filename are correct and that all required libraries are available.');
    }

    window.win98OpenRunDialog = function () {
        const input = document.getElementById('runInput');
        input.value = '';
        openDialog('run-dialog');
        setTimeout(() => input.focus(), 50);
    };

    document.getElementById('menuRun')?.addEventListener('click', () => window.win98OpenRunDialog());

    document.getElementById('run-ok-btn').addEventListener('click', () => {
        executeRunCommand(document.getElementById('runInput').value);
    });
    document.getElementById('run-cancel-btn').addEventListener('click', () => closeDialog('run-dialog'));
    document.getElementById('runInput').addEventListener('keydown', event => {
        if (event.key === 'Enter') executeRunCommand(event.target.value);
        if (event.key === 'Escape') closeDialog('run-dialog');
        event.stopPropagation();
    });
    document.getElementById('run-error-ok-btn').addEventListener('click', () => closeDialog('run-error-dialog'));

    // ------------------------------------------- Ctrl+Alt+Del / BSOD
    let cadSelection = null;

    function buildCadList() {
        const listEl = document.getElementById('cadTaskList');
        listEl.innerHTML = '';
        const tasks = [];
        document.querySelectorAll('.window').forEach(w => {
            if (w.style.display !== 'none' && !w.classList.contains('minimized')) {
                const title = w.querySelector('.window-title')?.textContent.trim();
                if (title) tasks.push({ label: title, windowId: w.id });
            }
        });
        tasks.push({ label: 'Explorer', windowId: '__explorer__' });
        tasks.push({ label: 'Systray', windowId: '__systray__' });

        cadSelection = tasks[0];
        tasks.forEach(task => {
            const item = document.createElement('div');
            item.className = 'cad-task' + (cadSelection === task ? ' selected' : '');
            item.textContent = task.label + (task.windowId === '__explorer__' ? ' [Not responding]' : '');
            item.addEventListener('click', () => {
                cadSelection = task;
                listEl.querySelectorAll('.cad-task').forEach(el => el.classList.remove('selected'));
                item.classList.add('selected');
            });
            listEl.appendChild(item);
        });
    }

    function showBsod() {
        closeDialog('cad-dialog');
        const bsod = document.createElement('div');
        bsod.className = 'bsod-screen';
        bsod.innerHTML =
            '<div class="bsod-inner">' +
            '<span class="bsod-title"> Windows </span>\n\n' +
            'A fatal exception 0E has occurred at 0028:C0011E36 in VXD VMM(01) +\n' +
            '00010E36. The current application will be terminated.\n\n' +
            '*  Press any key to terminate the current application.\n' +
            '*  Press CTRL+ALT+DEL again to restart your computer. You will\n' +
            '   lose any unsaved information in all applications.\n\n\n' +
            '<span class="bsod-continue">Press any key to continue _</span>' +
            '</div>';
        document.body.appendChild(bsod);
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('error');

        function dismiss() {
            bsod.remove();
            document.removeEventListener('keydown', dismiss, true);
            document.removeEventListener('mousedown', dismiss, true);
        }
        setTimeout(() => {
            document.addEventListener('keydown', dismiss, true);
            document.addEventListener('mousedown', dismiss, true);
        }, 400);
    }

    document.addEventListener('keydown', event => {
        if (event.ctrlKey && event.altKey && (event.key === 'Delete' || event.key === 'Del')) {
            event.preventDefault();
            buildCadList();
            openDialog('cad-dialog');
        }
    });

    // ------------------------------------------------------------ Alt+Tab
    let altTabState = null;

    function openWindowsForSwitcher() {
        return Array.from(document.querySelectorAll('.window')).filter(w =>
            w.style.display !== 'none' || w.classList.contains('minimized'));
    }

    function renderAltTab() {
        const box = document.getElementById('altTabBox');
        const iconsEl = document.getElementById('altTabIcons');
        const labelEl = document.getElementById('altTabLabel');
        iconsEl.innerHTML = '';
        altTabState.windows.forEach((w, index) => {
            const icon = document.createElement('div');
            icon.className = 'alttab-icon' + (index === altTabState.index ? ' selected' : '');
            const img = w.querySelector('.window-title img');
            if (img) {
                const clone = document.createElement('img');
                clone.src = img.src;
                icon.appendChild(clone);
            }
            iconsEl.appendChild(icon);
        });
        const current = altTabState.windows[altTabState.index];
        labelEl.textContent = current ? current.querySelector('.window-title').textContent.trim() : '';
        box.style.display = 'block';
    }

    function commitAltTab() {
        const box = document.getElementById('altTabBox');
        if (!altTabState) return;
        const target = altTabState.windows[altTabState.index];
        altTabState = null;
        box.style.display = 'none';
        if (target && typeof window.win98ActivateWindow === 'function') {
            window.win98ActivateWindow(target);
        }
    }

    document.addEventListener('keydown', event => {
        if (event.altKey && event.key === 'Tab') {
            event.preventDefault();
            if (!altTabState) {
                const windows = openWindowsForSwitcher();
                if (windows.length === 0) return;
                altTabState = { windows, index: 0 };
            }
            const dir = event.shiftKey ? -1 : 1;
            altTabState.index = (altTabState.index + dir + altTabState.windows.length) % altTabState.windows.length;
            renderAltTab();
        }
    }, true);

    document.addEventListener('keyup', event => {
        if (altTabState && event.key === 'Alt') {
            commitAltTab();
        }
    }, true);

    document.getElementById('cad-endtask-btn').addEventListener('click', () => {
        if (!cadSelection) return;
        if (cadSelection.windowId === '__explorer__' || cadSelection.windowId === '__systray__') {
            showBsod();
            return;
        }
        const w = document.getElementById(cadSelection.windowId);
        if (w) {
            w.style.display = 'none';
            w.classList.remove('active');
        }
        buildCadList();
    });
    document.getElementById('cad-shutdown-btn').addEventListener('click', () => {
        closeDialog('cad-dialog');
        document.getElementById('menuShutDown')?.click();
    });
    document.getElementById('cad-cancel-btn').addEventListener('click', () => closeDialog('cad-dialog'));
})();
