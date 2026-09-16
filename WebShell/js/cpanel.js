    // Control Panel applet routing, System Properties, Add/Remove Programs.
(function () {
    'use strict';

    const APPLETS = {
        __CPL_DISPLAY__: () => window.win98OpenDisplayProperties?.(),
        __CPL_DATETIME__: () => window.win98OpenShellApp?.('dateTimeWindow'),
        __CPL_SOUNDS__: () => window.win98OpenShellApp?.('volumeMixerWindow'),
        __CPL_APPS__: () => window.win98OpenShellApp?.('addRemoveWindow'),
        __CPL_NETWORK__: () => window.win98OpenShellApp?.('networkNeighborhood'),
        __CPL_GAME__: () => window.win98OpenShellApp?.('controlsWindow')
    };

    window.win98OpenControlPanelApplet = function (id) {
        const open = APPLETS[id];
        if (open) open();
    };

    // ------------------------------------------------------ System Properties
    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.sysPropsWindow = function () {
        window.win98ActivateWindow?.(document.getElementById('sysPropsWindow'));
    };
    document.getElementById('sysprops-ok-btn')?.addEventListener('click', () => {
        document.getElementById('sysPropsWindow').querySelector('.close-btn')?.click();
    });

    // --------------------------------------------------- Add/Remove Programs
    const PROGRAMS = [
        { name: 'DOOM (all installed games)', size: '42.1 MB', required: true },
        { name: 'Internet Explorer 4.0', size: '18.7 MB', required: true },
        { name: 'Minesweeper', size: '0.2 MB' },
        { name: 'Solitaire', size: '0.3 MB' },
        { name: 'FreeCell', size: '0.3 MB' },
        { name: 'Paint', size: '1.1 MB' },
        { name: 'Calculator', size: '0.1 MB' },
        { name: 'CD Player', size: '0.4 MB' },
        { name: 'WordPad', size: '1.8 MB' },
        { name: 'Windows 98 (uninstall)', size: '318 MB', required: true }
    ];
    let selectedProgram = PROGRAMS[0];

    function renderPrograms() {
        const listEl = document.getElementById('addRemoveList');
        if (!listEl) return;
        listEl.innerHTML = '';
        PROGRAMS.forEach(program => {
            const row = document.createElement('div');
            row.className = 'cad-task' + (selectedProgram === program ? ' selected' : '');
            row.textContent = program.name + '   (' + program.size + ')';
            row.addEventListener('click', () => {
                selectedProgram = program;
                renderPrograms();
            });
            listEl.appendChild(row);
        });
    }

    window.win98AppOpenHooks.addRemoveWindow = function () {
        renderPrograms();
        window.win98ActivateWindow?.(document.getElementById('addRemoveWindow'));
    };

    document.getElementById('addremove-remove-btn')?.addEventListener('click', () => {
        const statusEl = document.getElementById('addRemoveStatus');
        window.win98PlaySound?.('error');
        if (selectedProgram?.required) {
            statusEl.textContent = selectedProgram.name.startsWith('DOOM')
                ? 'This program is required by Windows and cannot be removed. (It came pre-installed on every PC in 1998. This is historical fact.)'
                : 'This program is required by Windows and cannot be removed.';
        } else {
            statusEl.textContent = 'Setup could not find INSTALL.LOG. The uninstaller has been uninstalled.';
        }
    });
    document.getElementById('addremove-ok-btn')?.addEventListener('click', () => {
        document.getElementById('addRemoveWindow').querySelector('.close-btn')?.click();
    });
})();
