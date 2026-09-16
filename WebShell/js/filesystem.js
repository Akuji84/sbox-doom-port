    // Virtual filesystem: explorer windows, desktop icons, recycle bin,
    // context menus and modal dialogs. Self-contained IIFE.
    (() => {
        const FS_STORAGE_KEY = 'win98ge.virtualFs.v1';
        const DISK_CAPACITY_BYTES = 4 * 1024 * 1024 * 1024;
        const FLOPPY_CAPACITY_BYTES = 1440 * 1024;
        const CDROM_CAPACITY_BYTES = 650 * 1024 * 1024;
        const desktopContainer = document.querySelector('.desktop-icons');
        const overlayEl = document.getElementById('overlay');
        const saveDialogFileList = document.querySelector('#saveas-dialog .dialog-file-list');
        const saveDialogFolderInput = document.querySelector('#saveas-dialog .form-input[disabled]');
        const createTextEncoder = () => new TextEncoder();
        const textEncoder = createTextEncoder();
        const builtinDesktopWindows = new Set(['myComputer', 'myDocuments', 'networkNeighborhood', 'internetExplorer', 'recycle', 'notepadWindow', 'doomWindow']);
        const explorerWindowIds = ['myComputer', 'myDocuments', 'recycle'];
        const explorerState = {
            myComputer: createExplorerState('__MY_COMPUTER__'),
            myDocuments: createExplorerState('C:/My Documents'),
            recycle: createExplorerState('__RECYCLE__')
        };
        const notepadState = {
            currentPath: '',
            currentFolderPath: 'C:/My Documents',
            dirty: false
        };
        let clipboardState = null;
        let selectedDesktopPath = '';
        let fsState = loadFsState();
        let contextMenuState = null;
        const contextMenu = createContextMenu();

        injectFilesystemStyles();
        patchNotepadHandlers();
        bindDesktopShellHandlers();
        bindExplorerWindowHandlers();
        bindTaskbarRefreshHooks();
        renderDesktopFilesystemIcons();
        refreshAllExplorerWindows();
        updateRecycleWindow();

        function createExplorerState(initialPath) {
            return {
                currentPath: initialPath,
                history: [initialPath],
                historyIndex: 0,
                selectedPath: ''
            };
        }

        function loadFsState() {
            try {
                const raw = window.localStorage.getItem(FS_STORAGE_KEY);
                if (raw) {
                    const parsed = JSON.parse(raw);
                    if (parsed && parsed.version === 1 && parsed.entries) {
                        ensureFsDefaults(parsed);
                        return parsed;
                    }
                }
            } catch (error) {
                console.warn('win98 fs load failed', error);
            }

            const defaults = createDefaultFsState();
            persistFsState(defaults);
            return defaults;
        }

        function persistFsState(state = fsState) {
            window.localStorage.setItem(FS_STORAGE_KEY, JSON.stringify(state));
        }

        function ensureFsDefaults(state) {
            state.entries ||= {};
            if (!state.entries['C:/DOOM']) {
                state.entries['C:/DOOM'] = { type: 'folder', name: 'DOOM', parent: 'C:/', readOnly: true, special: 'doom' };
            }
            if (state.entries['C:/Windows/Desktop/DOOM']?.special === 'doom_shortcut') {
                delete state.entries['C:/Windows/Desktop/DOOM'];
            }
            state.recycleBin ||= [];
            state.lastNotepadFolderPath ||= 'C:/My Documents';
            persistFsState(state);
        }

        function syncDoomProgramsIntoFs(programs) {
            const doomRoot = 'C:/DOOM';
            const existingChildren = listChildPaths(doomRoot);
            existingChildren.forEach(path => {
                const entry = getEntry(path);
                if (entry?.special === 'doom_program') {
                    delete fsState.entries[path];
                }
            });

            (programs || []).forEach(program => {
                const programPath = normalizePath(program.programPath);
                fsState.entries[programPath] = {
                    type: 'app',
                    name: program.fileName,
                    parent: doomRoot,
                    readOnly: true,
                    special: 'doom_program',
                    wadPath: program.wadPath || '',
                    displayName: program.displayName || '',
                    icon: program.icon || ''
                };
            });

            ensureShellAppEntries();

            persistFsState();
            shellDebug('fs', 'synced DOOM fs entries', {
                count: listChildPaths(doomRoot).length,
                paths: listChildPaths(doomRoot)
            });
            refreshAllExplorerWindows();
        }

        function createDefaultFsState() {
            return {
                version: 1,
                entries: {
                    'C:/': { type: 'drive', name: 'Local Disk (C:)', parent: '', readOnly: false },
                    'A:/': { type: 'drive', name: '3½ Floppy (A:)', parent: '', readOnly: false, removable: true },
                    'D:/': { type: 'drive', name: 'CD-ROM (D:)', parent: '', readOnly: true, removable: true },
                    'C:/DOOM': { type: 'folder', name: 'DOOM', parent: 'C:/', readOnly: true, special: 'doom' },
                    'C:/My Documents': { type: 'folder', name: 'My Documents', parent: 'C:/' },
                    'C:/My Documents/My Music': { type: 'folder', name: 'My Music', parent: 'C:/My Documents' },
                    'C:/My Documents/My Pictures': { type: 'folder', name: 'My Pictures', parent: 'C:/My Documents' },
                    'C:/My Documents/Readme.txt': {
                        type: 'text',
                        name: 'Readme.txt',
                        parent: 'C:/My Documents',
                        content: 'Welcome to your Windows 98 documents folder.\r\n\r\nThis virtual file system is stored locally in your browser.'
                    },
                    'C:/Windows': { type: 'folder', name: 'Windows', parent: 'C:/' },
                    'C:/Windows/Desktop': { type: 'folder', name: 'Desktop', parent: 'C:/Windows' },
                    'C:/Program Files': { type: 'folder', name: 'Program Files', parent: 'C:/' },
                    'C:/Temp': { type: 'folder', name: 'Temp', parent: 'C:/' },
                    'D:/SETUP.TXT': {
                        type: 'text',
                        name: 'SETUP.TXT',
                        parent: 'D:/',
                        readOnly: true,
                        content: 'Windows 98 CD-ROM placeholder.\r\n\r\nThis drive is read-only.'
                    }
                },
                recycleBin: [],
                lastNotepadFolderPath: 'C:/My Documents'
            };
        }

        function normalizePath(path) {
            const value = (path || '').replace(/\\/g, '/').trim();
            if (!value) return '';

            if (/^[A-Z]:\/?$/i.test(value)) {
                return value.slice(0, 2).toUpperCase() + '/';
            }

            const normalized = value
                .replace(/^([A-Z]):/i, (_, drive) => drive.toUpperCase() + ':/')
                .replace(/\/{2,}/g, '/')
                .replace(/\/$/, '');

            return normalized;
        }

        function isDrivePath(path) {
            return /^[A-Z]:\/$/i.test(path);
        }

        function getParentPath(path) {
            const normalized = normalizePath(path);
            if (!normalized || isDrivePath(normalized)) {
                return '';
            }

            const lastSlash = normalized.lastIndexOf('/');
            if (lastSlash <= 2) {
                return normalized.slice(0, 3);
            }

            return normalized.slice(0, lastSlash);
        }

        function joinPath(parentPath, name) {
            const parent = normalizePath(parentPath);
            if (!parent) return normalizePath(name);
            if (isDrivePath(parent)) {
                return normalizePath(parent + name);
            }
            return normalizePath(parent + '/' + name);
        }

        function getEntry(path) {
            return fsState.entries[normalizePath(path)] || null;
        }

        function listChildPaths(parentPath) {
            const normalizedParent = normalizePath(parentPath);
            return Object.keys(fsState.entries)
                .filter(path => fsState.entries[path].parent === normalizedParent)
                .sort(compareFsPaths);
        }

        function compareFsPaths(leftPath, rightPath) {
            const left = fsState.entries[leftPath];
            const right = fsState.entries[rightPath];
            const leftRank = fsSortRank(left);
            const rightRank = fsSortRank(right);
            if (leftRank !== rightRank) {
                return leftRank - rightRank;
            }
            return left.name.localeCompare(right.name, undefined, { sensitivity: 'base' });
        }

        // Folders and drives first, then shell utilities (CONTROLS.EXE stays
        // at the top of the games folder no matter what WADs are installed),
        // then everything else alphabetically.
        function fsSortRank(entry) {
            if (entry.type === 'folder' || entry.type === 'drive') {
                return 0;
            }
            return entry.special === 'shell_app' ? 1 : 2;
        }

        function getDescendantPaths(path) {
            const normalized = normalizePath(path);
            return Object.keys(fsState.entries)
                .filter(candidate => candidate === normalized || candidate.startsWith(normalized + '/'))
                .sort((a, b) => a.length - b.length);
        }

        function computeDriveUsageBytes(rootPath) {
            const normalizedRoot = normalizePath(rootPath);
            return Object.entries(fsState.entries).reduce((sum, [path, entry]) => {
                if ((path === normalizedRoot || path.startsWith(normalizedRoot)) && entry.type === 'text') {
                    return sum + getFileSizeBytes(entry);
                }
                return sum;
            }, 0);
        }

        function calculateTreeTextBytes(path) {
            return getDescendantPaths(path).reduce((sum, descendantPath) => {
                const entry = getEntry(descendantPath);
                return sum + getFileSizeBytes(entry);
            }, 0);
        }

        function getDriveCapacityBytes(rootPath) {
            const normalized = normalizePath(rootPath);
            if (normalized === 'A:/') return FLOPPY_CAPACITY_BYTES;
            if (normalized === 'D:/') return CDROM_CAPACITY_BYTES;
            return DISK_CAPACITY_BYTES;
        }

        function getFileSizeBytes(entry) {
            if (!entry || entry.type !== 'text') {
                return 0;
            }

            return textEncoder.encode(entry.content || '').length;
        }

        function formatBytes(bytes) {
            if (bytes >= 1024 * 1024 * 1024) {
                return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
            }
            if (bytes >= 1024 * 1024) {
                return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
            }
            if (bytes >= 1024) {
                return `${Math.max(1, Math.round(bytes / 1024))} KB`;
            }
            return `${bytes} bytes`;
        }

        function getFreeDriveSpaceBytes(rootPath) {
            return Math.max(0, getDriveCapacityBytes(rootPath) - computeDriveUsageBytes(rootPath));
        }

        function ensureDriveHasSpace(rootPath, additionalBytes) {
            if (additionalBytes <= 0) {
                return true;
            }

            if (getFreeDriveSpaceBytes(rootPath) >= additionalBytes) {
                return true;
            }

            void showInfoDialog('Low Disk Space', `There is not enough free space on ${rootPath.slice(0, 2)} to complete this operation.`);
            return false;
        }

        function sanitizeFileName(name, fallbackPrefix) {
            const cleaned = (name || '')
                .replace(/[\\/:*?"<>|]/g, '')
                .replace(/\s+/g, ' ')
                .trim();

            return cleaned || fallbackPrefix;
        }

        function splitExtension(fileName) {
            const index = fileName.lastIndexOf('.');
            if (index <= 0) {
                return { base: fileName, ext: '' };
            }

            return {
                base: fileName.slice(0, index),
                ext: fileName.slice(index)
            };
        }

        function makeUniqueChildPath(parentPath, desiredName) {
            const parent = normalizePath(parentPath);
            const { base, ext } = splitExtension(desiredName);
            let attempt = 0;
            let candidateName = desiredName;

            while (findChildByName(parent, candidateName)) {
                attempt += 1;
                candidateName = `${base} (${attempt})${ext}`;
            }

            return joinPath(parent, candidateName);
        }

        function findChildByName(parentPath, childName) {
            const normalizedParent = normalizePath(parentPath);
            const desired = childName.toLowerCase();
            return listChildPaths(normalizedParent).find(path => {
                const entry = getEntry(path);
                return entry && entry.name.toLowerCase() === desired;
            }) || '';
        }

        function buildExplorerItems(viewPath) {
            if (viewPath === '__MY_COMPUTER__') {
                return [
                    {
                        path: 'C:/',
                        type: 'drive',
                        icon: './assets/icons/hard_drive.png',
                        label: 'Local Disk (C:)',
                        meta: `${formatBytes(getFreeDriveSpaceBytes('C:/'))} free of ${formatBytes(getDriveCapacityBytes('C:/'))}`
                    },
                    {
                        path: 'A:/',
                        type: 'drive',
                        icon: './assets/icons/floppy_drive.png',
                        label: '3½ Floppy (A:)',
                        meta: `${formatBytes(getDriveCapacityBytes('A:/'))} removable disk`
                    },
                    {
                        path: 'D:/',
                        type: 'drive',
                        icon: './assets/icons/cd_drive.png',
                        label: 'CD-ROM (D:)',
                        meta: `${formatBytes(getDriveCapacityBytes('D:/'))} compact disc`
                    },
                    {
                        path: 'C:/DOOM',
                        type: 'folder',
                        icon: './assets/icons/folder_open.png',
                        label: 'DOOM',
                        meta: `${doomPrograms.length || 0} application(s)`
                    },
                    {
                        path: '__CONTROL_PANEL__',
                        type: 'folder',
                        icon: './assets/icons/control_panel.png',
                        label: 'Control Panel',
                        meta: 'System settings'
                    }
                ];
            }

            if (viewPath === '__CONTROL_PANEL__') {
                return [
                    { path: '__CPL_DISPLAY__', type: 'app', icon: './assets/icons/control_panel.png', label: 'Display', meta: 'Wallpaper, screen saver, appearance' },
                    { path: '__CPL_DATETIME__', type: 'app', icon: './assets/icons/control_panel.png', label: 'Date/Time', meta: 'Clock and calendar' },
                    { path: '__CPL_SOUNDS__', type: 'app', icon: './assets/icons/tray_volume.png', label: 'Sounds and Multimedia', meta: 'Volume control' },
                    { path: '__CPL_APPS__', type: 'app', icon: './assets/icons/executable_gear.png', label: 'Add/Remove Programs', meta: 'Installed software' },
                    { path: '__CPL_NETWORK__', type: 'app', icon: './assets/icons/entire_network.png', label: 'Network', meta: 'Who is online' },
                    { path: '__CPL_GAME__', type: 'app', icon: './assets/icons/executable.png', label: 'Game Controllers', meta: 'DOOM controls' }
                ];
            }

            if (viewPath === '__RECYCLE__') {
                return fsState.recycleBin.map(item => ({
                    path: item.originalPath,
                    recycleId: item.id,
                    type: item.entry.type,
                    icon: getEntryIcon(item.entry.type, item.originalPath),
                    label: item.entry.name,
                    meta: item.entry.type === 'text' ? formatBytes(getFileSizeBytes(item.entry)) : 'File Folder'
                }));
            }

            const childPaths = listChildPaths(viewPath);
            if (viewPath === 'C:/DOOM') {
                shellDebug('fs', 'buildExplorerItems for DOOM', {
                    count: childPaths.length,
                    paths: childPaths
                });
            }

            return childPaths.map(path => {
                const entry = getEntry(path);
                return {
                    path,
                    type: entry.type,
                    icon: getEntryIcon(entry.type, path),
                    label: entry.name,
                    meta: entry.type === 'text' ? formatBytes(getFileSizeBytes(entry)) : (entry.type === 'folder' ? 'File Folder' : '')
                };
            });
        }

        function getEntryIcon(type, path) {
            const entry = getEntry(path);
            if (type === 'drive') {
                if (path === 'A:/') return './assets/icons/floppy_drive.png';
                if (path === 'D:/') return './assets/icons/cd_drive.png';
                return './assets/icons/hard_drive.png';
            }

            if (type === 'app') {
                if (entry?.icon) {
                    return entry.icon;
                }
                const program = doomPrograms.find(item => normalizePath(item.programPath) === normalizePath(path));
                return program ? program.icon : './assets/icons/executable.png';
            }
            if (entry?.special === 'doom_shortcut') return './assets/icons/folder_open.png';
            if (type === 'folder') return './assets/icons/folder_closed.png';
            return './assets/icons/document.png';
        }

        function updateExplorerSelection(windowId, path) {
            if (!explorerState[windowId]) return;
            explorerState[windowId].selectedPath = normalizePath(path);
            renderExplorerWindow(windowId);
        }

        function renderExplorerWindow(windowId) {
            const state = explorerState[windowId];
            const windowEl = document.getElementById(windowId);
            if (!state || !windowEl) return;

            const contentsEl = windowEl.querySelector('.window-contents');
            const addressEl = windowEl.querySelector('.address-input');
            const statusEl = windowEl.querySelector('.window-statusbar');
            if (!contentsEl || !addressEl || !statusEl) return;

            const items = buildExplorerItems(state.currentPath);
            if (state.currentPath === 'C:/DOOM') {
                shellDebug('explorer', 'render explorer DOOM window', {
                    windowId,
                    itemCount: items.length,
                    selectedPath: state.selectedPath
                });
            }
            contentsEl.innerHTML = '';
            contentsEl.classList.remove('view-list', 'view-details');
            if (state.viewMode === 'list') contentsEl.classList.add('view-list');
            if (state.viewMode === 'details') contentsEl.classList.add('view-details');

            if (items.length === 0) {
                const emptyState = document.createElement('div');
                emptyState.className = 'fs-empty-state';
                emptyState.textContent = state.currentPath === '__RECYCLE__'
                    ? 'The Recycle Bin is empty'
                    : 'This folder is empty';
                contentsEl.appendChild(emptyState);
            } else {
                items.forEach(item => {
                    const tile = document.createElement('div');
                    tile.className = 'folder-item virtual-folder-item';
                    if (state.currentPath === '__RECYCLE__') {
                        tile.dataset.recycleId = item.recycleId || '';
                    } else {
                        tile.dataset.path = item.path;
                    }
                    tile.dataset.window = windowId;
                    tile.dataset.type = item.type;
                    if (state.selectedPath && state.selectedPath === normalizePath(item.path)) {
                        tile.classList.add('selected');
                    }

                    tile.innerHTML = `
                        <img class="folder-icon" src="${item.icon}" alt="${escapeHtml(item.label)}">
                        <div class="folder-name">${escapeHtml(item.label)}</div>
                        <div class="folder-meta">${escapeHtml(item.meta || '')}</div>
                    `;

                    contentsEl.appendChild(tile);
                });
            }

            addressEl.textContent = formatAddress(state.currentPath);
            statusEl.textContent = buildStatusText(state.currentPath, items.length, state.selectedPath);
            updateWindowTitleForPath(windowId, state.currentPath);
        }

        function refreshAllExplorerWindows() {
            explorerWindowIds.forEach(renderExplorerWindow);
            updateRecycleBinIcon();
        }

        // Swap the recycle bin artwork between empty and full everywhere.
        function updateRecycleBinIcon() {
            const src = fsState.recycleBin.length
                ? './assets/icons/recycle_bin_full.png'
                : './assets/icons/recycle_bin_empty.png';
            const desktopIcon = document.querySelector('.desktop-icon[data-window="recycle"] .icon-img');
            if (desktopIcon) desktopIcon.src = src;
            const titleIcon = document.querySelector('#recycle .window-title img');
            if (titleIcon) titleIcon.src = src;
        }

        // CONTROLS.EXE lives with the games and opens the controls property
        // sheet instead of launching a WAD. Created unconditionally so it is
        // present even when the WAD list cannot be fetched.
        function ensureShellAppEntries() {
            fsState.entries['C:/DOOM/CONTROLS.EXE'] = {
                type: 'app',
                name: 'CONTROLS.EXE',
                parent: 'C:/DOOM',
                readOnly: true,
                special: 'shell_app',
                appWindow: 'controlsWindow',
                displayName: 'Controls',
                icon: './assets/icons/executable_gear.png'
            };
            persistFsState();
        }

        window.win98RefreshAllExplorerWindows = refreshAllExplorerWindows;
        window.win98SyncDoomPrograms = syncDoomProgramsIntoFs;
        ensureShellAppEntries();
        loadDoomWads();

        function updateWindowTitleForPath(windowId, path) {
            const windowEl = document.getElementById(windowId);
            const titleEl = windowEl?.querySelector('.window-title');
            if (!titleEl) return;

            let titleText = 'My Computer';
            if (windowId === 'myDocuments') {
                titleText = getDisplayNameForPath(path);
            } else if (windowId === 'recycle') {
                titleText = 'Recycle Bin';
            } else if (windowId === 'myComputer' && path !== '__MY_COMPUTER__') {
                titleText = getDisplayNameForPath(path);
            }

            const imgEl = titleEl.querySelector('img');
            titleEl.innerHTML = '';
            if (imgEl) {
                titleEl.appendChild(imgEl);
            }
            titleEl.append(document.createTextNode(' ' + titleText));
        }

        function formatAddress(path) {
            if (path === '__MY_COMPUTER__') return 'My Computer';
            if (path === '__RECYCLE__') return 'Recycle Bin';
            if (path === '__CONTROL_PANEL__') return 'Control Panel';
            if (path === 'C:/DOOM') return 'My Computer/DOOM';
            return path;
        }

        function getDisplayNameForPath(path) {
            if (path === '__MY_COMPUTER__') return 'My Computer';
            if (path === '__RECYCLE__') return 'Recycle Bin';
            if (path === '__CONTROL_PANEL__') return 'Control Panel';
            if (path === 'C:/') return 'Local Disk (C:)';
            if (path === 'A:/') return '3½ Floppy (A:)';
            if (path === 'D:/') return 'CD-ROM (D:)';
            const entry = getEntry(path);
            return entry?.name || path;
        }

        function getVirtualObjectCount(path) {
            return listChildPaths(path).length;
        }

        function buildStatusText(path, itemCount, selectedPath) {
            if (selectedPath) {
                const selectedEntry = getEntry(selectedPath);
                if (selectedEntry) {
                    const sizeText = selectedEntry.type === 'text'
                        ? formatBytes(getFileSizeBytes(selectedEntry))
                        : selectedEntry.type === 'folder'
                            ? `${getVirtualObjectCount(selectedPath)} object(s)`
                            : `${formatBytes(getFreeDriveSpaceBytes(selectedPath))} free`;
                    if (selectedEntry.type === 'app') {
                        return `${selectedEntry.name}   ${selectedEntry.displayName || 'Application'}`;
                    }
                    return `${selectedEntry.name}   ${sizeText}`;
                }
            }

            if (path === '__MY_COMPUTER__') {
                return `${itemCount} object(s)`;
            }

            if (path === '__RECYCLE__') {
                return `${itemCount} object(s)`;
            }

            const drivePath = isDrivePath(path) ? path : getRootPath(path);
            return `${itemCount} object(s)   ${formatBytes(getFreeDriveSpaceBytes(drivePath))} free`;
        }

        function getRootPath(path) {
            const normalized = normalizePath(path);
            return normalized.slice(0, 3);
        }

        function escapeHtml(value) {
            return String(value)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function renderDesktopFilesystemIcons() {
            if (!desktopContainer) return;

            desktopContainer.querySelectorAll('[data-filesystem-desktop="true"]').forEach(node => node.remove());
            const desktopPaths = listChildPaths('C:/Windows/Desktop');
            desktopPaths.forEach(path => {
                const entry = getEntry(path);
                const icon = document.createElement('div');
                icon.className = 'desktop-icon';
                icon.dataset.path = path;
                icon.dataset.filesystemDesktop = 'true';
                if (selectedDesktopPath === path) {
                    icon.classList.add('active');
                }
                icon.innerHTML = `
                    <img class="icon-img" src="${getEntryIcon(entry.type, path)}" alt="${escapeHtml(entry.name)}">
                    <div class="icon-text">${escapeHtml(entry.name)}</div>
                `;
                desktopContainer.appendChild(icon);
            });
        }

        function openPath(path, windowId) {
            const normalized = normalizePath(path);
            const entry = getEntry(normalized);
            const targetPath = entry?.targetPath ? normalizePath(entry.targetPath) : '';
            if (normalized === 'C:/DOOM' || entry?.type === 'app') {
                shellDebug('open', 'openPath called', {
                    path: normalized,
                    windowId,
                    entryType: entry?.type || '',
                    entryName: entry?.name || ''
                });
            }

            if (targetPath) {
                openPath(targetPath, windowId);
                return;
            }

            if (normalized === 'C:/DOOM' || normalized === '__CONTROL_PANEL__') {
                if (windowId && explorerState[windowId]) {
                    navigateExplorer(windowId, normalized, true);
                } else {
                    openWindowById('myComputer');
                    navigateExplorer('myComputer', normalized, true);
                }
                return;
            }

            if (normalized.startsWith('__CPL_')) {
                if (typeof window.win98OpenControlPanelApplet === 'function') {
                    window.win98OpenControlPanelApplet(normalized);
                }
                return;
            }

            if (entry?.special === 'shell_app') {
                if (typeof window.win98OpenShellApp === 'function') {
                    window.win98OpenShellApp(entry.appWindow || '');
                }
                return;
            }

            if (entry?.type === 'app') {
                const doomProgram = doomPrograms.find(program => normalizePath(program.programPath) === normalized)
                    || {
                        programPath: normalized,
                        wadPath: entry.wadPath || '',
                        fileName: entry.name || normalized.split('/').pop() || 'DOOM.EXE',
                        displayName: entry.displayName || 'DOOM',
                        icon: entry.icon || './assets/icons/executable.png'
                    };
                selectedDoomProgramPath = doomProgram.programPath;
                renderDoomPrograms();
                launchSelectedDoomProgram(doomProgram);
                return;
            }

            if (!entry) return;
            if (entry.type === 'text') {
                openTextFile(normalized);
                return;
            }

            if (windowId && explorerState[windowId]) {
                navigateExplorer(windowId, normalized, true);
                return;
            }

            if (normalized === 'C:/My Documents') {
                openWindowById('myDocuments');
                navigateExplorer('myDocuments', normalized, true);
                return;
            }

            openWindowById('myComputer');
            navigateExplorer('myComputer', normalized, true);
        }

        window.win98OpenPath = function(path, windowId) {
            openPath(path, windowId || '');
        };

        // Read-only helpers for the DOS prompt and Find Files.
        window.win98FsHelpers = {
            entry: function(path) {
                const e = getEntry(normalizePath(path));
                return e ? { type: e.type, name: e.name, content: e.content || '', readOnly: !!e.readOnly } : null;
            },
            children: function(path) {
                return listChildPaths(normalizePath(path)).map(p => {
                    const e = getEntry(p);
                    return { path: p, name: e.name, type: e.type, size: e.type === 'text' ? getFileSizeBytes(e) : 0 };
                });
            },
            find: function(query) {
                const q = String(query || '').toLowerCase().trim();
                if (!q) return [];
                return Object.keys(fsState.entries)
                    .filter(p => (fsState.entries[p].name || '').toLowerCase().includes(q))
                    .slice(0, 200)
                    .map(p => ({ path: p, name: fsState.entries[p].name, type: fsState.entries[p].type, parent: fsState.entries[p].parent || '' }));
            }
        };

        // Used by Paint (and future apps) to drop a file into the FS.
        window.win98SaveFileToFs = function(parentPath, name, content) {
            const parent = normalizePath(parentPath);
            if (!getEntry(parent) || getEntry(parent).readOnly) return false;
            const path = makeUniqueChildPath(parent, name);
            fsState.entries[path] = {
                type: 'text',
                name: path.split('/').pop(),
                parent,
                content: String(content || '')
            };
            persistFsState();
            refreshAllExplorerWindows();
            return true;
        };

        function openWindowById(windowId) {
            const targetWindow = document.getElementById(windowId);
            if (!targetWindow) return;
            windows.forEach(w => w.classList.remove('active'));
            targetWindow.classList.add('active');
            targetWindow.classList.remove('minimized');
            targetWindow.style.display = 'block';
            updateTaskbar();
        }

        function navigateExplorer(windowId, path, pushHistory) {
            const state = explorerState[windowId];
            if (!state) return;

            const normalized = normalizePath(path);
            state.currentPath = normalized;
            state.selectedPath = '';

            if (pushHistory) {
                state.history = state.history.slice(0, state.historyIndex + 1);
                state.history.push(normalized);
                state.historyIndex = state.history.length - 1;
            }

            renderExplorerWindow(windowId);
        }

        function goBack(windowId) {
            const state = explorerState[windowId];
            if (!state || state.historyIndex <= 0) return;
            state.historyIndex -= 1;
            state.currentPath = state.history[state.historyIndex];
            state.selectedPath = '';
            renderExplorerWindow(windowId);
        }

        function goForward(windowId) {
            const state = explorerState[windowId];
            if (!state || state.historyIndex >= state.history.length - 1) return;
            state.historyIndex += 1;
            state.currentPath = state.history[state.historyIndex];
            state.selectedPath = '';
            renderExplorerWindow(windowId);
        }

        function goUp(windowId) {
            const state = explorerState[windowId];
            if (!state) return;
            if (state.currentPath === '__MY_COMPUTER__' || state.currentPath === '__RECYCLE__') return;
            if (isDrivePath(state.currentPath)) {
                navigateExplorer(windowId, '__MY_COMPUTER__', true);
                return;
            }
            navigateExplorer(windowId, getParentPath(state.currentPath), true);
        }

        function bindDesktopShellHandlers() {
            desktopContainer?.addEventListener('click', event => {
                const icon = event.target.closest('.desktop-icon[data-filesystem-desktop="true"]');
                if (!icon) return;
                const path = normalizePath(icon.dataset.path);
                const wasSelected = selectedDesktopPath === path;
                selectedDesktopPath = normalizePath(icon.dataset.path);
                const shouldOpen = wasSelected || event.detail >= 2 || wasRecentlyActivatedByKey(`desktop:${selectedDesktopPath}`);
                renderDesktopFilesystemIcons();
                if (shouldOpen) {
                    openPath(icon.dataset.path, '');
                }
                event.stopPropagation();
            });

            desktopContainer?.addEventListener('dblclick', event => {
                const icon = event.target.closest('.desktop-icon[data-filesystem-desktop="true"]');
                if (!icon) return;
                openPath(icon.dataset.path, '');
                event.stopPropagation();
            });

            desktopContainer?.addEventListener('contextmenu', event => {
                const icon = event.target.closest('.desktop-icon[data-filesystem-desktop="true"]');
                if (icon) {
                    selectedDesktopPath = normalizePath(icon.dataset.path);
                    renderDesktopFilesystemIcons();
                    showContextMenu(event.clientX, event.clientY, buildItemContextMenu(selectedDesktopPath, 'desktop'));
                } else {
                    showContextMenu(event.clientX, event.clientY, buildBackgroundContextMenu('desktop', 'C:/Windows/Desktop'));
                }
                event.preventDefault();
            });

            document.body.addEventListener('click', event => {
                if (!event.target.closest('.desktop-icon[data-filesystem-desktop="true"]')) {
                    selectedDesktopPath = '';
                    renderDesktopFilesystemIcons();
                }
                hideContextMenu();
            });
        }

        function bindExplorerWindowHandlers() {
            explorerWindowIds.forEach(windowId => {
                const windowEl = document.getElementById(windowId);
                const contentsEl = windowEl?.querySelector('.window-contents');
                if (!contentsEl) return;

                contentsEl.addEventListener('click', event => {
                    const itemEl = event.target.closest('.virtual-folder-item');
                    if (!itemEl) {
                        explorerState[windowId].selectedPath = '';
                        renderExplorerWindow(windowId);
                        return;
                    }

                    let wasSelected = false;
                    if (windowId === 'recycle') {
                        const recycleId = itemEl.dataset.recycleId;
                        const recycleEntry = fsState.recycleBin.find(item => item.id === recycleId);
                        wasSelected = explorerState[windowId].selectedPath === (recycleEntry?.originalPath || '');
                        explorerState[windowId].selectedPath = recycleEntry?.originalPath || '';
                    } else {
                        const normalizedItemPath = normalizePath(itemEl.dataset.path);
                        wasSelected = explorerState[windowId].selectedPath === normalizedItemPath;
                        explorerState[windowId].selectedPath = normalizePath(itemEl.dataset.path);
                    }
                    const activationKey = windowId === 'recycle'
                        ? `recycle:${itemEl.dataset.recycleId || ''}`
                        : `explorer:${normalizePath(itemEl.dataset.path)}`;
                    const shouldOpen = wasSelected || event.detail >= 2 || wasRecentlyActivatedByKey(activationKey);
                    renderExplorerWindow(windowId);
                    if (shouldOpen) {
                        if (windowId === 'recycle') {
                            const recycleEntry = fsState.recycleBin.find(item => item.id === itemEl.dataset.recycleId);
                            if (recycleEntry) {
                                if (recycleEntry.entry.type === 'text') {
                                    openRecoveredTextFile(recycleEntry);
                                } else {
                                    void restoreRecycleEntry(recycleEntry.id);
                                }
                            }
                        } else {
                            openPath(itemEl.dataset.path, windowId);
                        }
                    }
                    event.stopPropagation();
                });

                contentsEl.addEventListener('dblclick', event => {
                    const itemEl = event.target.closest('.virtual-folder-item');
                    if (!itemEl) return;

                    if (windowId === 'recycle') {
                        const recycleEntry = fsState.recycleBin.find(item => item.id === itemEl.dataset.recycleId);
                        if (recycleEntry) {
                            if (recycleEntry.entry.type === 'text') {
                                openRecoveredTextFile(recycleEntry);
                            } else {
                                void restoreRecycleEntry(recycleEntry.id);
                            }
                        }
                        return;
                    }

                    openPath(itemEl.dataset.path, windowId);
                    event.stopPropagation();
                });

                contentsEl.addEventListener('contextmenu', event => {
                    const itemEl = event.target.closest('.virtual-folder-item');
                    if (itemEl) {
                        if (windowId === 'recycle') {
                            const recycleEntry = fsState.recycleBin.find(item => item.id === itemEl.dataset.recycleId);
                            if (recycleEntry) {
                                showContextMenu(event.clientX, event.clientY, buildRecycleItemContextMenu(recycleEntry.id));
                            }
                        } else {
                            const path = normalizePath(itemEl.dataset.path);
                            explorerState[windowId].selectedPath = path;
                            renderExplorerWindow(windowId);
                            showContextMenu(event.clientX, event.clientY, buildItemContextMenu(path, windowId));
                        }
                    } else {
                        showContextMenu(event.clientX, event.clientY, buildBackgroundContextMenu(windowId, explorerState[windowId].currentPath));
                    }
                    event.preventDefault();
                    event.stopPropagation();
                });

                bindToolbarButtons(windowId);
                bindExplorerMenubar(windowId);
            });
        }

        function setExplorerViewMode(windowId, mode) {
            explorerState[windowId].viewMode = mode;
            renderExplorerWindow(windowId);
        }

        // File/Edit/View/Go menubar dropdowns, rendered with the shared
        // context-menu machinery just below the clicked menu label.
        function bindExplorerMenubar(windowId) {
            const windowEl = document.getElementById(windowId);
            const menubar = windowEl?.querySelector('.window-menubar');
            if (!menubar || menubar.dataset.fsBound === 'true') return;
            menubar.dataset.fsBound = 'true';
            menubar.addEventListener('click', event => {
                const item = event.target.closest('.window-menu-item');
                if (!item) return;
                const state = explorerState[windowId];
                if (!state) return;
                const rect = item.getBoundingClientRect();
                const label = item.textContent.trim().toLowerCase();
                const current = state.currentPath;
                const isVirtual = current === '__MY_COMPUTER__' || current === '__RECYCLE__';
                const readOnlyHere = isVirtual || getEntry(current)?.readOnly === true;
                const sel = state.selectedPath;
                const mode = state.viewMode || 'icons';
                const check = flag => (flag ? '● ' : '   ');
                let items = null;

                if (label === 'file') {
                    items = [
                        { label: 'New Folder', action: () => createNewFolder(current), disabled: readOnlyHere },
                        { label: 'New Text Document', action: () => createNewTextDocument(current), disabled: readOnlyHere },
                        { separator: true },
                        { label: 'Delete', action: () => deleteEntry(sel), disabled: !sel },
                        { label: 'Rename', action: () => renameEntry(sel), disabled: !sel },
                        { separator: true },
                        { label: 'Close', action: () => windowEl.querySelector('.close-btn')?.click() }
                    ];
                } else if (label === 'edit') {
                    items = [
                        { label: 'Cut', action: () => cutSelectedItem(windowId), disabled: !sel },
                        { label: 'Copy', action: () => copySelectedItem(windowId), disabled: !sel },
                        { label: 'Paste', action: () => pasteIntoCurrentFolder(windowId), disabled: !clipboardState }
                    ];
                } else if (label === 'view') {
                    items = [
                        { label: check(mode === 'icons') + 'Large Icons', action: () => setExplorerViewMode(windowId, 'icons') },
                        { label: check(mode === 'list') + 'List', action: () => setExplorerViewMode(windowId, 'list') },
                        { label: check(mode === 'details') + 'Details', action: () => setExplorerViewMode(windowId, 'details') },
                        { separator: true },
                        { label: 'Refresh', action: () => renderExplorerWindow(windowId) }
                    ];
                } else if (label === 'go') {
                    items = [
                        { label: 'Back', action: () => goBack(windowId) },
                        { label: 'Forward', action: () => goForward(windowId) },
                        { label: 'Up One Level', action: () => goUp(windowId) }
                    ];
                }

                if (items) {
                    showContextMenu(rect.left, rect.bottom + 1, items);
                    event.stopPropagation();
                }
            });
        }

        function bindToolbarButtons(windowId) {
            const windowEl = document.getElementById(windowId);
            const toolbarButtons = [...(windowEl?.querySelectorAll('.window-toolbar-button') || [])];
            toolbarButtons.forEach(button => {
                if (button.dataset.fsBound === 'true') return;
                button.dataset.fsBound = 'true';
                const label = button.textContent.replace(/\s+/g, ' ').trim().toLowerCase();
                button.addEventListener('click', async () => {
                    switch (label) {
                        case 'back':
                            goBack(windowId);
                            break;
                        case 'forward':
                            goForward(windowId);
                            break;
                        case 'up':
                            goUp(windowId);
                            break;
                        case 'cut':
                            await cutSelectedItem(windowId);
                            break;
                        case 'copy':
                            await copySelectedItem(windowId);
                            break;
                        case 'paste':
                            await pasteIntoCurrentFolder(windowId);
                            break;
                        case 'properties':
                            await showPropertiesForSelection(windowId);
                            break;
                        case 'empty recycle bin':
                            await emptyRecycleBin();
                            break;
                    }
                });
            });
        }

        function bindTaskbarRefreshHooks() {
            const originalUpdateTaskbar = updateTaskbar;
            updateTaskbar = function patchedUpdateTaskbar() {
                originalUpdateTaskbar();
                renderDesktopFilesystemIcons();
            };
        }

        function patchNotepadHandlers() {
            if (!textarea) return;

            const saveOption = replaceElementToClearListeners('save-option');
            const saveAsOption = replaceElementToClearListeners('saveas-option');
            const saveButton = replaceElementToClearListeners('save-btn');
            const newOption = replaceElementToClearListeners('new-option');
            const exitOption = replaceElementToClearListeners('exit-option');

            textarea.addEventListener('input', () => {
                notepadState.dirty = true;
            });

            saveOption?.addEventListener('click', async () => {
                if (notepadState.currentPath) {
                    saveTextFileToPath(notepadState.currentPath, textarea.value);
                    return;
                }

                openSaveAsDialog();
            });

            saveAsOption?.addEventListener('click', () => {
                openSaveAsDialog();
            });

            saveButton?.addEventListener('click', async () => {
                await saveCurrentNotepadDocument(true);
            });

            newOption?.addEventListener('click', async () => {
                if (notepadState.dirty) {
                    const confirmed = await showConfirmDialog('Notepad', 'Discard the current document?');
                    if (!confirmed) return;
                }
                openNewTextDocument();
            });

            exitOption?.addEventListener('click', async () => {
                if (notepadState.dirty) {
                    const confirmed = await showConfirmDialog('Notepad', 'Close Notepad without saving?');
                    if (!confirmed) return;
                }
                document.getElementById('notepadWindow').style.display = 'none';
                updateTaskbar();
            });

            replaceElementToClearListeners('saveas-cancel-btn')?.addEventListener('click', () => {
                closeSaveAsDialog();
            });

            saveDialogFileList?.addEventListener('dblclick', event => {
                const itemEl = event.target.closest('.dialog-file-item[data-path]');
                if (!itemEl) return;
                const path = normalizePath(itemEl.dataset.path);
                const entry = getEntry(path);
                if (entry?.type === 'folder') {
                    notepadState.currentFolderPath = path;
                    refreshSaveAsDialog();
                }
            });

            openNewTextDocument();
        }

        function replaceElementToClearListeners(id) {
            const element = document.getElementById(id);
            if (!element || !element.parentNode) return element;
            const clone = element.cloneNode(true);
            element.parentNode.replaceChild(clone, element);
            return clone;
        }

        function openNewTextDocument() {
            textarea.value = '';
            notepadState.currentPath = '';
            notepadState.currentFolderPath = fsState.lastNotepadFolderPath || 'C:/My Documents';
            notepadState.dirty = false;
            updateNotepadWindowTitle('Untitled.txt');
            updateCursorPosition();
        }

        function openTextFile(path) {
            const entry = getEntry(path);
            if (!entry || entry.type !== 'text') return;

            // Paint saves images as data URLs; open those in the viewer.
            if ((entry.content || '').startsWith('data:image/') && typeof window.win98ImageViewerOpen === 'function') {
                window.win98ImageViewerOpen(entry.name, entry.content);
                return;
            }

            openWindowById('notepadWindow');
            textarea.value = entry.content || '';
            notepadState.currentPath = normalizePath(path);
            notepadState.currentFolderPath = getParentPath(path) || 'C:/My Documents';
            notepadState.dirty = false;
            fsState.lastNotepadFolderPath = notepadState.currentFolderPath;
            persistFsState();
            updateNotepadWindowTitle(entry.name);
            updateCursorPosition();
        }

        function openRecoveredTextFile(recycleEntry) {
            openWindowById('notepadWindow');
            textarea.value = recycleEntry.entry.content || '';
            notepadState.currentPath = '';
            notepadState.currentFolderPath = recycleEntry.originalParent || 'C:/My Documents';
            notepadState.dirty = false;
            updateNotepadWindowTitle(recycleEntry.entry.name);
            updateCursorPosition();
        }

        function updateNotepadWindowTitle(fileName) {
            const titleEl = document.querySelector('#notepadWindow .window-title');
            const iconEl = titleEl?.querySelector('img');
            if (!titleEl) return;
            titleEl.innerHTML = '';
            if (iconEl) titleEl.appendChild(iconEl);
            titleEl.append(document.createTextNode(` ${fileName} - Notepad`));
        }

        function openSaveAsDialog() {
            overlayEl.style.display = 'block';
            document.getElementById('saveas-dialog').style.display = 'block';
            refreshSaveAsDialog();
            const nameInput = document.getElementById('filename-input');
            if (nameInput) {
                const currentName = notepadState.currentPath ? getEntry(notepadState.currentPath)?.name : 'Untitled.txt';
                nameInput.value = currentName || 'Untitled.txt';
                nameInput.focus();
                nameInput.select();
            }
        }

        function closeSaveAsDialog() {
            document.getElementById('saveas-dialog').style.display = 'none';
            overlayEl.style.display = 'none';
        }

        function refreshSaveAsDialog() {
            const folderPath = notepadState.currentFolderPath || 'C:/My Documents';
            if (saveDialogFolderInput) {
                saveDialogFolderInput.value = folderPath;
            }
            if (!saveDialogFileList) return;

            saveDialogFileList.innerHTML = '';
            buildExplorerItems(folderPath).forEach(item => {
                const li = document.createElement('li');
                li.className = 'dialog-file-item';
                li.dataset.path = item.path;
                li.innerHTML = `<img src="${item.icon}" alt="${escapeHtml(item.label)}">${escapeHtml(item.label)}`;
                saveDialogFileList.appendChild(li);
            });
        }

        async function saveCurrentNotepadDocument(forceDialog) {
            const requestedName = sanitizeFileName(document.getElementById('filename-input')?.value || '', 'Untitled.txt');

            if (!forceDialog && notepadState.currentPath) {
                return saveTextFileToPath(notepadState.currentPath, textarea.value);
            }

            const folderPath = notepadState.currentFolderPath || 'C:/My Documents';
            let targetName = requestedName;
            if (!/\.txt$/i.test(targetName)) {
                targetName += '.txt';
            }

            let targetPath = joinPath(folderPath, targetName);
            if (!notepadState.currentPath || normalizePath(notepadState.currentPath) !== targetPath) {
                targetPath = makeUniqueChildPath(folderPath, targetName);
            }

            const saved = saveTextFileToPath(targetPath, textarea.value);
            if (saved) {
                closeSaveAsDialog();
            }
        }

        function saveTextFileToPath(path, content) {
            const targetPath = normalizePath(path);
            const parentPath = getParentPath(targetPath);
            const existing = getEntry(targetPath);
            const nextBytes = textEncoder.encode(content || '').length;
            const currentBytes = existing ? getFileSizeBytes(existing) : 0;
            const delta = Math.max(0, nextBytes - currentBytes);
            const rootPath = getRootPath(targetPath);

            if (!ensureDriveHasSpace(rootPath, delta)) {
                return false;
            }

            fsState.entries[targetPath] = {
                type: 'text',
                name: targetPath.split('/').pop(),
                parent: parentPath,
                content: content || ''
            };
            persistFsState();

            notepadState.currentPath = targetPath;
            notepadState.currentFolderPath = parentPath;
            fsState.lastNotepadFolderPath = parentPath;
            notepadState.dirty = false;
            updateNotepadWindowTitle(fsState.entries[targetPath].name);
            refreshSaveAsDialog();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
            return true;
        }

        async function createNewFolder(parentPath) {
            const name = await showPromptDialog('New Folder', 'Folder name:', 'New Folder');
            if (!name) return;
            const safeName = sanitizeFileName(name, 'New Folder');
            const newPath = makeUniqueChildPath(parentPath, safeName);
            fsState.entries[newPath] = {
                type: 'folder',
                name: newPath.split('/').pop(),
                parent: normalizePath(parentPath)
            };
            persistFsState();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
        }

        async function createNewTextDocument(parentPath) {
            const name = await showPromptDialog('New Text Document', 'File name:', 'New Text Document.txt');
            if (!name) return;
            let safeName = sanitizeFileName(name, 'New Text Document.txt');
            if (!/\.txt$/i.test(safeName)) safeName += '.txt';
            const path = makeUniqueChildPath(parentPath, safeName);
            fsState.entries[path] = {
                type: 'text',
                name: path.split('/').pop(),
                parent: normalizePath(parentPath),
                content: ''
            };
            persistFsState();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
        }

        async function renameEntry(path) {
            const entry = getEntry(path);
            if (!entry || isDrivePath(path)) return;
            const name = await showPromptDialog('Rename', 'Enter a new name:', entry.name);
            if (!name) return;
            let safeName = sanitizeFileName(name, entry.name);
            if (entry.type === 'text' && !/\.[^./]+$/.test(safeName)) {
                const existingExt = splitExtension(entry.name).ext;
                safeName += existingExt || '.txt';
            }
            const targetPath = makeUniqueChildPath(getParentPath(path), safeName);
            moveEntryPath(path, targetPath);
            persistFsState();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
        }

        function moveEntryPath(sourcePath, targetPath) {
            const source = normalizePath(sourcePath);
            const target = normalizePath(targetPath);
            const descendants = getDescendantPaths(source);
            const replacementEntries = {};
            descendants.forEach(oldPath => {
                const entry = { ...fsState.entries[oldPath] };
                const suffix = oldPath.slice(source.length);
                const newPath = normalizePath(target + suffix);
                replacementEntries[newPath] = entry;
            });
            descendants.sort((a, b) => b.length - a.length).forEach(path => delete fsState.entries[path]);
            Object.entries(replacementEntries).forEach(([newPath, entry]) => {
                entry.name = newPath.split('/').pop() || entry.name;
                if (!isDrivePath(newPath)) {
                    entry.parent = getParentPath(newPath);
                }
                fsState.entries[newPath] = entry;
            });
            if (notepadState.currentPath === sourcePath || notepadState.currentPath.startsWith(sourcePath + '/')) {
                const suffix = notepadState.currentPath.slice(sourcePath.length);
                notepadState.currentPath = normalizePath(targetPath + suffix);
            }
        }

        async function deleteEntry(path) {
            const normalized = normalizePath(path);
            const entry = getEntry(normalized);
            if (!entry || isDrivePath(normalized)) return;
            const confirmed = await showConfirmDialog('Delete', `Move ${entry.name} to the Recycle Bin?`);
            if (!confirmed) return;

            const snapshot = JSON.parse(JSON.stringify(entry));
            fsState.recycleBin.unshift({
                id: `recycle-${Date.now()}-${Math.random().toString(16).slice(2)}`,
                originalPath: normalized,
                originalParent: getParentPath(normalized),
                entry: snapshot,
                descendants: getDescendantPaths(normalized)
                    .filter(path => path !== normalized)
                    .map(path => ({
                        relativePath: path.slice(normalized.length),
                        entry: JSON.parse(JSON.stringify(fsState.entries[path]))
                    }))
            });

            getDescendantPaths(normalized).sort((a, b) => b.length - a.length).forEach(path => delete fsState.entries[path]);
            if (notepadState.currentPath === normalized || (notepadState.currentPath && notepadState.currentPath.startsWith(normalized + '/'))) {
                openNewTextDocument();
            }
            persistFsState();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
            updateRecycleWindow();
        }

        async function restoreRecycleEntry(recycleId) {
            const index = fsState.recycleBin.findIndex(item => item.id === recycleId);
            if (index < 0) return;
            const item = fsState.recycleBin[index];
            const targetPath = makeUniqueChildPath(item.originalParent || 'C:/My Documents', item.entry.name);
            fsState.entries[targetPath] = {
                ...item.entry,
                parent: getParentPath(targetPath),
                name: targetPath.split('/').pop()
            };
            item.descendants.forEach(descendant => {
                const descendantTarget = normalizePath(targetPath + descendant.relativePath);
                fsState.entries[descendantTarget] = {
                    ...descendant.entry,
                    parent: getParentPath(descendantTarget),
                    name: descendantTarget.split('/').pop()
                };
            });
            fsState.recycleBin.splice(index, 1);
            persistFsState();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
            updateRecycleWindow();
        }

        async function emptyRecycleBin() {
            if (!fsState.recycleBin.length) {
                await showInfoDialog('Recycle Bin', 'The Recycle Bin is already empty.');
                return;
            }
            const confirmed = await showConfirmDialog('Empty Recycle Bin', 'Permanently delete all items in the Recycle Bin?');
            if (!confirmed) return;
            fsState.recycleBin = [];
            persistFsState();
            updateRecycleWindow();
        }

        function updateRecycleWindow() {
            explorerState.recycle.selectedPath = '';
            renderExplorerWindow('recycle');
            const recycleIcon = document.querySelector('.desktop-icon[data-window=\"recycle\"] img');
            if (recycleIcon) {
                recycleIcon.src = fsState.recycleBin.length ? './assets/icons/recycle_bin_empty.png' : './assets/icons/recycle_bin_empty.png';
            }
        }

        async function copySelectedItem(windowId) {
            const selectedPath = explorerState[windowId]?.selectedPath || '';
            if (!selectedPath) return;
            clipboardState = { mode: 'copy', path: selectedPath };
            await showInfoDialog('Clipboard', `${getDisplayNameForPath(selectedPath)} copied to the clipboard.`);
        }

        async function cutSelectedItem(windowId) {
            const selectedPath = explorerState[windowId]?.selectedPath || '';
            if (!selectedPath) return;
            clipboardState = { mode: 'cut', path: selectedPath };
            await showInfoDialog('Clipboard', `${getDisplayNameForPath(selectedPath)} will be moved when you paste it.`);
        }

        async function pasteIntoCurrentFolder(windowId) {
            const state = explorerState[windowId];
            if (!state || !clipboardState || state.currentPath === '__MY_COMPUTER__' || state.currentPath === '__RECYCLE__') return;
            if (getEntry(state.currentPath)?.readOnly) return;

            const sourcePath = normalizePath(clipboardState.path);
            const sourceEntry = getEntry(sourcePath);
            if (!sourceEntry) return;

            const targetPath = makeUniqueChildPath(state.currentPath, sourceEntry.name);
            const targetRoot = getRootPath(targetPath);
            const sourceRoot = getRootPath(sourcePath);
            if (clipboardState.mode === 'copy' && !ensureDriveHasSpace(targetRoot, calculateTreeTextBytes(sourcePath))) {
                return;
            }
            if (clipboardState.mode === 'cut' && sourceRoot !== targetRoot && !ensureDriveHasSpace(targetRoot, calculateTreeTextBytes(sourcePath))) {
                return;
            }
            if (clipboardState.mode === 'copy') {
                cloneEntryTree(sourcePath, targetPath);
            } else {
                moveEntryPath(sourcePath, targetPath);
                clipboardState = null;
            }

            persistFsState();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
        }

        function cloneEntryTree(sourcePath, targetPath) {
            const source = normalizePath(sourcePath);
            const target = normalizePath(targetPath);
            const descendants = getDescendantPaths(source);
            descendants.forEach(oldPath => {
                const suffix = oldPath.slice(source.length);
                const newPath = normalizePath(target + suffix);
                fsState.entries[newPath] = JSON.parse(JSON.stringify(fsState.entries[oldPath]));
                fsState.entries[newPath].parent = getParentPath(newPath);
                fsState.entries[newPath].name = newPath.split('/').pop();
            });
        }

        async function showPropertiesForSelection(windowId) {
            const state = explorerState[windowId];
            if (!state) return;
            const path = state.selectedPath || state.currentPath;

            if (path === '__MY_COMPUTER__') {
                await showInfoDialog('My Computer Properties', `Local Disk (C:)\n${formatBytes(getFreeDriveSpaceBytes('C:/'))} free of ${formatBytes(getDriveCapacityBytes('C:/'))}`);
                return;
            }

            if (path === '__RECYCLE__') {
                await showInfoDialog('Recycle Bin Properties', `${fsState.recycleBin.length} item(s) in the Recycle Bin.`);
                return;
            }

            const entry = getEntry(path);
            if (!entry) return;

            const size = entry.type === 'text'
                ? formatBytes(getFileSizeBytes(entry))
                : entry.type === 'drive'
                    ? `${formatBytes(getFreeDriveSpaceBytes(path))} free of ${formatBytes(getDriveCapacityBytes(path))}`
                    : `${listChildPaths(path).length} object(s)`;

            await showInfoDialog(`${entry.name} Properties`, `Type: ${entry.type}\nLocation: ${entry.parent || 'Computer'}\nSize: ${size}`);
        }

        function buildBackgroundContextMenu(scope, parentPath) {
            const normalizedParent = normalizePath(parentPath);

            // The desktop gets the Win98 desktop menu, ending in the
            // Display Properties sheet handled by desktop.js.
            if (scope === 'desktop') {
                const desktopEntry = getEntry(normalizedParent);
                const desktopReadOnly = desktopEntry?.readOnly === true;
                return [
                    { label: 'Arrange Icons', action: () => window.win98ArrangeIcons?.() },
                    { label: 'Line Up Icons', action: () => window.win98LineUpIcons?.() },
                    { label: 'Refresh', action: () => { refreshAllExplorerWindows(); renderDesktopFilesystemIcons(); } },
                    { separator: true },
                    { label: 'Paste', action: () => pasteClipboardIntoPath(normalizedParent), disabled: desktopReadOnly || !clipboardState },
                    { label: 'Paste Shortcut', disabled: true },
                    { separator: true },
                    { label: 'New Folder', action: () => createNewFolder(normalizedParent), disabled: desktopReadOnly },
                    { label: 'New Text Document', action: () => createNewTextDocument(normalizedParent), disabled: desktopReadOnly },
                    { separator: true },
                    { label: 'Properties', action: () => window.win98OpenDisplayProperties?.() }
                ];
            }

            if (normalizedParent === '__MY_COMPUTER__') {
                return [
                    { label: 'Properties', action: () => {
                        if (typeof window.win98OpenShellApp === 'function' && window.win98OpenShellApp('sysPropsWindow')) return;
                        showInfoDialog('Properties', 'My Computer');
                    } }
                ];
            }

            if (normalizedParent === '__RECYCLE__') {
                return [
                    { label: 'Properties', action: () => showInfoDialog('Properties', formatAddress(normalizedParent)) }
                ];
            }

            const parentEntry = getEntry(normalizedParent);
            const isReadOnly = parentEntry?.readOnly === true;
            const items = [
                { label: 'New Folder', action: () => createNewFolder(normalizedParent), disabled: isReadOnly },
                { label: 'New Text Document', action: () => createNewTextDocument(normalizedParent), disabled: isReadOnly }
            ];

            if (clipboardState && scope !== 'recycle') {
                items.push({ label: 'Paste', action: () => pasteClipboardIntoPath(normalizedParent), disabled: isReadOnly });
            }

            items.push({ label: 'Properties', action: () => showInfoDialog('Properties', `${formatAddress(normalizedParent)}\n${formatBytes(getFreeDriveSpaceBytes(getRootPath(normalizedParent || 'C:/')))} free`) });
            return items;
        }

        function buildItemContextMenu(path, scope) {
            const entry = getEntry(path);
            if (!entry) return [];

            return [
                { label: 'Open', action: () => openPath(path, scope === 'desktop' ? '' : scope) },
                { label: 'Rename', action: () => renameEntry(path), disabled: isDrivePath(path) || entry.readOnly },
                { label: 'Delete', action: () => deleteEntry(path), disabled: isDrivePath(path) || entry.readOnly },
                { label: 'Cut', action: () => { clipboardState = { mode: 'cut', path }; }, disabled: isDrivePath(path) || entry.readOnly },
                { label: 'Copy', action: () => { clipboardState = { mode: 'copy', path }; } },
                { label: 'Properties', action: () => showPropertiesDialogForPath(path) }
            ];
        }

        function buildRecycleItemContextMenu(recycleId) {
            return [
                { label: 'Restore', action: () => restoreRecycleEntry(recycleId) },
                { label: 'Delete', action: async () => permanentlyDeleteRecycleEntry(recycleId) },
                { label: 'Properties', action: () => showRecycleEntryProperties(recycleId) }
            ];
        }

        async function permanentlyDeleteRecycleEntry(recycleId) {
            const index = fsState.recycleBin.findIndex(item => item.id === recycleId);
            if (index < 0) return;
            const confirmed = await showConfirmDialog('Delete', 'Permanently remove this item from the Recycle Bin?');
            if (!confirmed) return;
            fsState.recycleBin.splice(index, 1);
            persistFsState();
            updateRecycleWindow();
        }

        async function showRecycleEntryProperties(recycleId) {
            const item = fsState.recycleBin.find(entry => entry.id === recycleId);
            if (!item) return;
            const size = item.entry.type === 'text' ? formatBytes(getFileSizeBytes(item.entry)) : `${item.descendants.length} child object(s)`;
            await showInfoDialog(`${item.entry.name} Properties`, `Original location: ${item.originalParent}\nSize: ${size}`);
        }

        async function showPropertiesDialogForPath(path) {
            const entry = getEntry(path);
            if (!entry) return;
            const size = entry.type === 'text'
                ? formatBytes(getFileSizeBytes(entry))
                : entry.type === 'folder'
                    ? `${getVirtualObjectCount(path)} object(s)`
                    : `${formatBytes(getFreeDriveSpaceBytes(path))} free of ${formatBytes(getDriveCapacityBytes(path))}`;
            await showInfoDialog(`${entry.name} Properties`, `Type: ${entry.type}\nLocation: ${entry.parent || 'Computer'}\nSize: ${size}`);
        }

        async function pasteClipboardIntoPath(parentPath) {
            if (!clipboardState) return;
            if (getEntry(parentPath)?.readOnly) return;
            const sourcePath = normalizePath(clipboardState.path);
            const entry = getEntry(sourcePath);
            if (!entry) return;
            const targetPath = makeUniqueChildPath(parentPath, entry.name);
            const targetRoot = getRootPath(targetPath);
            const sourceRoot = getRootPath(sourcePath);
            if (clipboardState.mode === 'copy' && !ensureDriveHasSpace(targetRoot, calculateTreeTextBytes(sourcePath))) {
                return;
            }
            if (clipboardState.mode === 'cut' && sourceRoot !== targetRoot && !ensureDriveHasSpace(targetRoot, calculateTreeTextBytes(sourcePath))) {
                return;
            }
            if (clipboardState.mode === 'copy') {
                cloneEntryTree(sourcePath, targetPath);
            } else {
                moveEntryPath(sourcePath, targetPath);
                clipboardState = null;
            }
            persistFsState();
            refreshAllExplorerWindows();
            renderDesktopFilesystemIcons();
        }

        function showContextMenu(x, y, items) {
            hideContextMenu();
            contextMenu.innerHTML = '';
            contextMenuState = { items };
            items.forEach(item => {
                if (item.separator) {
                    const sep = document.createElement('div');
                    sep.className = 'win98-context-separator';
                    contextMenu.appendChild(sep);
                    return;
                }
                const node = document.createElement('div');
                node.className = 'win98-context-item';
                node.textContent = item.label;
                if (item.disabled) {
                    node.classList.add('disabled');
                } else {
                    node.addEventListener('click', async event => {
                        event.stopPropagation();
                        hideContextMenu();
                        await item.action?.();
                    });
                }
                contextMenu.appendChild(node);
            });
            contextMenu.style.left = `${x}px`;
            contextMenu.style.top = `${y}px`;
            contextMenu.style.display = 'block';
        }

        function hideContextMenu() {
            contextMenu.style.display = 'none';
            contextMenu.innerHTML = '';
            contextMenuState = null;
        }

        function createContextMenu() {
            const menu = document.createElement('div');
            menu.className = 'win98-context-menu';
            document.body.appendChild(menu);
            return menu;
        }

        // desktop.js uses this for right-clicks on empty desktop space
        // (outside the .desktop-icons container).
        window.win98ShowDesktopContextMenu = function(x, y) {
            showContextMenu(x, y, buildBackgroundContextMenu('desktop', 'C:/Windows/Desktop'));
        };

        // Generic menu for other modules (taskbar menu, explorer menubars).
        window.win98ShowContextMenuItems = showContextMenu;

        function injectFilesystemStyles() {
            const style = document.createElement('style');
            style.textContent = `
                .virtual-folder-item.selected,
                .desktop-icon[data-filesystem-desktop="true"].active,
                .doom-program-item.selected {
                    background-color: rgba(11, 36, 106, 0.7);
                }
                .folder-meta {
                    font-size: 10px;
                    color: #555;
                    margin-top: 2px;
                    text-align: center;
                    min-height: 12px;
                }
                .doom-program-item {
                    cursor: pointer;
                }
                .fs-empty-state {
                    text-align: center;
                    color: #808080;
                    padding: 48px 12px;
                }
                .win98-context-menu {
                    position: fixed;
                    display: none;
                    min-width: 160px;
                    background: var(--w98-face, #c0c0c0);
                    border: 1px solid;
                    border-top-color: #ffffff;
                    border-left-color: #ffffff;
                    border-right-color: #808080;
                    border-bottom-color: #808080;
                    box-shadow: 2px 2px 0 #404040;
                    z-index: 5000;
                    padding: 2px;
                }
                .win98-context-item {
                    padding: 4px 8px;
                    font-size: 11px;
                    cursor: pointer;
                    white-space: nowrap;
                }
                .win98-context-item:hover {
                    background: #0b246a;
                    color: #fff;
                }
                .win98-context-item.disabled {
                    color: #808080;
                    cursor: default;
                }
                .win98-context-item.disabled:hover {
                    background: transparent;
                    color: #808080;
                }
                .win98-context-separator {
                    height: 1px;
                    background: #808080;
                    border-bottom: 1px solid #ffffff;
                    margin: 3px 2px;
                }
                .win98-fs-dialog .dialog-content {
                    white-space: pre-wrap;
                    font-size: 11px;
                    line-height: 1.4;
                }
                .win98-fs-dialog .form-input {
                    width: 100%;
                    margin-top: 8px;
                }
                .win98-error-dialog .dialog-buttons {
                    justify-content: center;
                }
                .win98-error-body {
                    display: flex;
                    align-items: flex-start;
                    gap: 14px;
                    padding: 6px 0 4px;
                }
                .win98-error-icon {
                    width: 32px;
                    height: 32px;
                    flex: 0 0 32px;
                    image-rendering: pixelated;
                }
                .win98-error-message {
                    flex: 1;
                    font-size: 12px;
                    line-height: 1.35;
                    color: #000;
                    padding-top: 3px;
                }
            `;
            document.head.appendChild(style);
        }

        function createModalDialog({ title, message, defaultValue = '', showInput = false, okText = 'OK', cancelText = 'Cancel', showCancel = true }) {
            return new Promise(resolve => {
                overlayEl.style.display = 'block';
                const dialog = document.createElement('div');
                dialog.className = 'dialog win98-fs-dialog';
                dialog.style.display = 'block';
                dialog.style.width = '320px';
                dialog.style.left = '50%';
                dialog.style.top = '50%';
                dialog.style.transform = 'translate(-50%, -50%)';
                dialog.innerHTML = `
                    <div class="dialog-titlebar">${escapeHtml(title)}</div>
                    <div class="dialog-content">${escapeHtml(message)}</div>
                    <div class="dialog-buttons">
                        <div class="btn win98-fs-ok">${escapeHtml(okText)}</div>
                        ${showCancel ? `<div class="btn win98-fs-cancel">${escapeHtml(cancelText)}</div>` : ''}
                    </div>
                `;

                if (showInput) {
                    const input = document.createElement('input');
                    input.type = 'text';
                    input.className = 'form-input';
                    input.value = defaultValue;
                    dialog.querySelector('.dialog-content').appendChild(input);
                    window.requestAnimationFrame(() => {
                        input.focus();
                        input.select();
                    });
                }

                document.body.appendChild(dialog);

                const cleanup = value => {
                    dialog.remove();
                    overlayEl.style.display = 'none';
                    resolve(value);
                };

                dialog.querySelector('.win98-fs-ok').addEventListener('click', () => {
                    const input = dialog.querySelector('.form-input');
                    cleanup(input ? input.value : true);
                });

                dialog.querySelector('.win98-fs-cancel')?.addEventListener('click', () => cleanup(null));
            });
        }

        function showWindowsErrorDialog(message) {
            return new Promise(resolve => {
                if (typeof window.win98PlaySound === 'function') window.win98PlaySound('error');
                overlayEl.style.display = 'block';
                const dialog = document.createElement('div');
                dialog.className = 'dialog win98-fs-dialog win98-error-dialog';
                dialog.style.display = 'block';
                dialog.style.width = '360px';
                dialog.style.left = '50%';
                dialog.style.top = '50%';
                dialog.style.transform = 'translate(-50%, -50%)';
                dialog.innerHTML = `
                    <div class="dialog-titlebar">Error</div>
                    <div class="dialog-content">
                        <div class="win98-error-body">
                            <img class="win98-error-icon" src="./assets/icons/msg_error.png" alt="Error">
                            <div class="win98-error-message">${escapeHtml(message)}</div>
                        </div>
                    </div>
                    <div class="dialog-buttons">
                        <div class="btn win98-fs-ok">OK</div>
                    </div>
                `;

                document.body.appendChild(dialog);

                const cleanup = () => {
                    dialog.remove();
                    overlayEl.style.display = 'none';
                    resolve(true);
                };

                dialog.querySelector('.win98-fs-ok').addEventListener('click', cleanup);
            });
        }

        function showPromptDialog(title, message, defaultValue) {
            return createModalDialog({ title, message, defaultValue, showInput: true });
        }

        async function showConfirmDialog(title, message) {
            const result = await createModalDialog({ title, message, okText: 'Yes', cancelText: 'No' });
            return result !== null;
        }

        function showInfoDialog(title, message) {
            return createModalDialog({ title, message, showCancel: false });
        }
    })();
