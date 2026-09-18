    // DOOM launcher: program list, WAD loading, s&box launch bridge.
    const doomWadList = document.getElementById('doomWadList');
    const doomLaunchButton = document.getElementById('doom-launch-btn');
    const doomLaunchStatus = document.getElementById('doom-launch-status');
    const shellQuery = new URLSearchParams(window.location.search);
    const shellSessionId = shellQuery.get('session') || '';
    const isSboxClient = shellQuery.get('client') === 'sbox';
    const shellChannel = shellQuery.get('channel') === 'beta' ? 'beta' : 'live';
    const shellBuild = shellQuery.get('build') || 'unknown';
    let selectedDoomProgramPath = '';
    let doomPrograms = [];

    function sanitizeExeBaseName(name) {
        const raw = String(name || '')
            .toUpperCase()
            .replace(/[^A-Z0-9]+/g, '_')
            .replace(/^_+|_+$/g, '');

        return raw || 'DOOM';
    }

    function buildDoomProgramFileName(wad, index) {
        const fromPath = String(wad?.path || '').split('/').pop()?.split('.')[0] || '';
        const base = sanitizeExeBaseName(fromPath || wad?.displayName || `DOOM_${index + 1}`);
        return `${base}.EXE`;
    }

    function getDoomProgramIcon(program) {
        if (program.wadPath === "doom/fsfc1.wad") return "./assets/icons/freedomscoops_first.svg";
        if (program.wadPath === "doom/fssc1.wad") return "./assets/icons/freedomscoops_second.svg";
        if (program.wadPath === 'doom/doom.wad') {
            return './assets/icons/doom_program.png';
        }
        if (program.wadPath === 'doom/rekkrsa.wad') {
            return './assets/icons/rekkr_program.png';
        }
        if (program.wadPath === 'doom/hacx.wad') {
            return './assets/icons/hacx_program.png';
        }
        if (program.wadPath === 'doom/freedoom2.wad') {
            return './assets/icons/freedoom2_program.png';
        }
        if (program.wadPath === 'doom/freedm.wad') {
            return './assets/icons/freedm_program.png';
        }
        if (program.wadPath === 'doom/chex.wad') {
            return './assets/icons/chex_program.png';
        }
        if (program.wadPath === 'doom/freedoom2.wad;doom/harmonyc.wad') {
            return './assets/icons/harmony_program.png';
        }
        return './assets/icons/freedoom_program.png';
    }

    function normalizeProgramPath(path) {
        return String(path || '').replace(/\\/g, '/').trim().toUpperCase();
    }

    function getSelectedDoomProgram() {
        return doomPrograms.find(program => normalizeProgramPath(program.programPath) === normalizeProgramPath(selectedDoomProgramPath)) || null;
    }

    window.win98ShellDebugSnapshot = function() {
        return {
            shellSessionId,
            isSboxClient,
            selectedDoomProgramPath,
            doomPrograms: doomPrograms.map(program => ({
                fileName: program.fileName,
                programPath: program.programPath,
                wadPath: program.wadPath
            })),
            events: shellDebugEvents.slice()
        };
    };

    shellDebug('startup', 'shell program layer ready', { session: shellSessionId, isSboxClient, channel: shellChannel, build: shellBuild });

    function renderDoomPrograms() {
        if (!doomWadList) return;

        doomWadList.innerHTML = '';
        if (doomPrograms.length === 0) {
            doomWadList.innerHTML = '<div>No programs available.</div>';
            if (doomLaunchStatus) doomLaunchStatus.textContent = '0 program(s)';
            return;
        }

        doomPrograms.forEach(program => {
            const item = document.createElement('div');
            item.className = 'folder-item doom-program-item';
            item.dataset.programPath = program.programPath;
            if (selectedDoomProgramPath === program.programPath) {
                item.classList.add('selected');
            }
            item.innerHTML = `
                <img class="folder-icon" src="${program.icon}" alt="${program.fileName}">
                <div class="folder-name">${program.fileName}</div>
                <div class="folder-meta">${program.displayName}</div>
            `;
            doomWadList.appendChild(item);
        });

        if (doomLaunchStatus) {
            const selectedProgram = getSelectedDoomProgram();
            doomLaunchStatus.textContent = selectedProgram
                ? `${selectedProgram.fileName} selected`
                : `${doomPrograms.length} program(s)`;
        }
    }

    async function launchSelectedDoomProgram(programOverride) {
        const selectedProgram = programOverride || getSelectedDoomProgram();

        if (!selectedProgram) {
            doomLaunchStatus.textContent = 'Select a program first.';
            shellDebug('launch', 'no selected program');
            return;
        }

        if (!isSboxClient) {
            shellDebug('launch', 'blocked outside sbox', { programPath: selectedProgram.programPath });
            await showWindowsErrorDialog('Play s&Doom on s&box');
            return;
        }

        if (!shellSessionId) {
            doomLaunchStatus.textContent = 'Missing shell session.';
            shellDebug('launch', 'missing shell session', { programPath: selectedProgram.programPath });
            return;
        }

        doomLaunchStatus.textContent = 'Launching DOOM...';
        shellDebug('launch', 'posting launch request', { sessionId: shellSessionId, wadPath: selectedProgram.wadPath, programPath: selectedProgram.programPath, channel: shellChannel });

        try {
            const response = await fetch(`/api/win98-shell/${shellChannel}/launch`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    sessionId: shellSessionId,
                    wadPath: selectedProgram.wadPath,
                    // CONTROLS.EXE bindings/settings + tray volume travel in
                    // the launch config so the game can apply them.
                    config: {
                        controls: typeof window.win98GetDoomControls === 'function' ? window.win98GetDoomControls() : null,
                        shellVolume: typeof window.win98GetShellVolume === 'function' ? window.win98GetShellVolume() : null
                    }
                })
            });
            if (!response.ok) throw new Error('Launch request failed');
            const payload = await response.json();
            shellDebug('launch', 'launch request accepted', payload);
        } catch (error) {
            doomLaunchStatus.textContent = 'Launch request failed.';
            shellDebug('launch', 'launch request failed', { error: String(error) });
        }
    }

    async function loadDoomWads() {
        if (!doomWadList) return;

        doomWadList.innerHTML = '<div>Loading programs...</div>';
        shellDebug('wads', 'loading wad list');

        try {
            const query = new URLSearchParams();
            if (shellSessionId) query.set('session', shellSessionId);
            query.set('build', shellBuild);
            const response = await fetch(`/api/win98-shell/${shellChannel}/wads?${query.toString()}`);
            if (!response.ok) throw new Error('Failed to load WADs');
            const payload = await response.json();
            const wads = Array.isArray(payload.wads) ? payload.wads : [];
            doomPrograms = [];
            shellDebug('wads', 'wad response', { count: wads.length, wads });

            if (wads.length === 0) {
                doomWadList.innerHTML = '<div>No programs available.</div>';
                shellDebug('wads', 'wad list empty');
                return;
            }

            doomPrograms = wads.map((wad, index) => {
                const program = {
                    kind: 'wad',
                    wadPath: wad.path,
                    displayName: wad.displayName,
                    fileName: buildDoomProgramFileName(wad, index),
                    programPath: `C:/DOOM/${buildDoomProgramFileName(wad, index)}`,
                    icon: ''
                };
                program.icon = getDoomProgramIcon(program);
                return program;
            });
            selectedDoomProgramPath = doomPrograms[0].programPath;
            renderDoomPrograms();
            if (typeof window.win98SyncDoomPrograms === 'function') {
                window.win98SyncDoomPrograms(doomPrograms);
                shellDebug('wads', 'synced programs into fs', { count: doomPrograms.length, paths: doomPrograms.map(program => program.programPath) });
            } else {
                shellDebug('wads', 'fs sync bridge missing');
            }
            openDoomFolderInExplorer();
        } catch (error) {
            doomWadList.innerHTML = '<div>Unable to load program list.</div>';
            if (doomLaunchStatus) doomLaunchStatus.textContent = 'Unable to contact DOOM launcher.';
            shellDebug('wads', 'wad load failed', { error: String(error) });
        }
    }

    doomWadList?.addEventListener('click', function(event) {
        const item = event.target.closest('.doom-program-item');
        if (!item) return;
        selectedDoomProgramPath = item.dataset.programPath || '';
        if (doomLaunchStatus) doomLaunchStatus.textContent = '';
        if (wasRecentlyActivatedByKey(`doom:${selectedDoomProgramPath}`)) {
            renderDoomPrograms();
            launchSelectedDoomProgram();
            return;
        }
        renderDoomPrograms();
    });

    doomWadList?.addEventListener('dblclick', function(event) {
        const item = event.target.closest('.doom-program-item');
        if (!item) return;
        selectedDoomProgramPath = item.dataset.programPath || '';
        renderDoomPrograms();
        launchSelectedDoomProgram();
    });

    doomLaunchButton?.addEventListener('click', function() {
        launchSelectedDoomProgram();
    });

    // TODO: Implement window resize functionality
    // Windows 98 windows can be resized from edges and corners
    // This requires adding resize handles and implementing the resizing logic

    // TODO: Implement folder navigation in Explorer windows
    // Should handle clicks on folders to navigate into them
    // Should update address bar and history

    // TODO: Implement view options in Explorer windows
    // Should allow switching between different view modes (list, icons, details)

    // TODO: Implement context-sensitive help
    // Windows 98 has F1 help available in most applications and dialogs

    // TODO: Implement animations for window minimize/maximize
    // Windows 98 has simple animations when minimizing or maximizing windows

    // TODO: Implement desktop icon grid alignment
    // Windows 98 aligns desktop icons to an invisible grid

    // TODO: Implement system sounds
    // Windows 98 plays sounds for various events (opening/closing windows, errors)
