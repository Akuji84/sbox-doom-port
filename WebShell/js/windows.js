    // Window management.
    // All window behavior (drag, controls, taskbar, desktop icons) is bound
    // once at the document level and resolved per-event, so any .window that
    // exists now or gets added later behaves the same without extra wiring.

    // Legacy static list - filesystem.js still references this. All shell
    // windows are static elements in index.html, so the list is complete.
    const windows = document.querySelectorAll('.window');
    const activationTracker = new WeakMap();
    const activationKeyTracker = new Map();
    const DOUBLE_ACTIVATION_MS = 425;

    function wasRecentlyActivated(target) {
        const now = Date.now();
        const last = activationTracker.get(target) || 0;
        activationTracker.set(target, now);
        return (now - last) <= DOUBLE_ACTIVATION_MS;
    }

    function wasRecentlyActivatedByKey(key) {
        const now = Date.now();
        const last = activationKeyTracker.get(key) || 0;
        activationKeyTracker.set(key, now);
        return (now - last) <= DOUBLE_ACTIVATION_MS;
    }

    function getAllWindows() {
        return document.querySelectorAll('.window');
    }

    // Show a window, bring it to front and reflect it on the taskbar.
    function activateWindow(windowEl) {
        if (!windowEl) return;
        getAllWindows().forEach(w => w.classList.remove('active'));
        windowEl.classList.add('active');
        windowEl.classList.remove('minimized');
        windowEl.style.display = 'block';
        updateTaskbar();
    }

    function closeWindowElement(windowEl) {
        windowEl.style.display = 'none';
        updateTaskbar();
    }

    function openStaticDesktopWindow(windowId, windowPath) {
        if (!windowId) return;

        if (windowPath && typeof window.win98OpenPath === 'function') {
            window.win98OpenPath(windowPath, '');
            return;
        }

        activateWindow(document.getElementById(windowId));
    }

    function openDoomFolderInExplorer() {
        openStaticDesktopWindow('myComputer', 'C:/DOOM');
    }

    // Window dragging (delegated)
    let dragWindowEl = null;
    let dragTitleBar = null;
    let isDraggingWindow = false;
    let activePointerId = null;
    let dragOffsetX = 0;
    let dragOffsetY = 0;

    function startWindowDrag(e) {
        const titleBar = e.target.closest ? e.target.closest('.window-titlebar') : null;
        if (!titleBar) return;
        if (e.target.classList.contains('window-control')) return;
        const windowEl = titleBar.closest('.window');
        if (!windowEl) return;

        // Bring window to front
        getAllWindows().forEach(w => w.classList.remove('active'));
        windowEl.classList.add('active');

        isDraggingWindow = true;
        dragWindowEl = windowEl;
        dragTitleBar = titleBar;

        const rect = windowEl.getBoundingClientRect();
        if (e.type === 'mousedown') {
            dragOffsetX = e.clientX - rect.left;
            dragOffsetY = e.clientY - rect.top;
        } else if (e.type === 'pointerdown') {
            activePointerId = e.pointerId;
            titleBar.setPointerCapture?.(e.pointerId);
            dragOffsetX = e.clientX - rect.left;
            dragOffsetY = e.clientY - rect.top;
            e.preventDefault();
        } else if (e.type === 'touchstart') {
            dragOffsetX = e.touches[0].clientX - rect.left;
            dragOffsetY = e.touches[0].clientY - rect.top;
        }
    }

    function dragWindow(e) {
        if (!isDraggingWindow || !dragWindowEl) return;

        let clientX, clientY;

        if (e.type === 'mousemove') {
            clientX = e.clientX;
            clientY = e.clientY;
        } else if (e.type === 'pointermove') {
            if (activePointerId !== null && e.pointerId !== activePointerId) return;
            clientX = e.clientX;
            clientY = e.clientY;
            e.preventDefault();
        } else if (e.type === 'touchmove') {
            clientX = e.touches[0].clientX;
            clientY = e.touches[0].clientY;
        } else {
            return;
        }

        let newX = (clientX - dragOffsetX);
        let newY = (clientY - dragOffsetY);

        // Keep window within bounds
        newX = Math.min(Math.max(newX, 0), window.innerWidth - dragWindowEl.offsetWidth);
        newY = Math.min(Math.max(newY, 0), window.innerHeight - dragWindowEl.offsetHeight);

        dragWindowEl.style.left = newX + 'px';
        dragWindowEl.style.top = newY + 'px';
        dragWindowEl.style.transform = 'none';
    }

    function endWindowDrag() {
        isDraggingWindow = false;
        activePointerId = null;
        dragWindowEl = null;
        dragTitleBar = null;
    }

    // Raise a window when clicked anywhere inside it, not just the titlebar.
    document.addEventListener('mousedown', function(e) {
        const windowEl = e.target.closest ? e.target.closest('.window') : null;
        if (!windowEl || windowEl.classList.contains('active')) return;
        getAllWindows().forEach(w => w.classList.remove('active'));
        windowEl.classList.add('active');
        updateTaskbar();
    }, true);

    document.addEventListener('mousedown', startWindowDrag);
    document.addEventListener('pointerdown', startWindowDrag);
    document.addEventListener('touchstart', startWindowDrag);

    document.addEventListener('mousemove', dragWindow);
    document.addEventListener('pointermove', dragWindow);
    document.addEventListener('touchmove', dragWindow, { passive: false });

    document.addEventListener('mouseup', endWindowDrag);
    document.addEventListener('pointerup', endWindowDrag);
    document.addEventListener('pointercancel', endWindowDrag);
    document.addEventListener('touchend', endWindowDrag);

    // Window resizing via the handles chrome.js attaches to every window.
    let resizeState = null;

    document.addEventListener('mousedown', function(e) {
        const handle = e.target.closest ? e.target.closest('.resize-handle') : null;
        if (!handle) return;
        const windowEl = handle.closest('.window');
        if (!windowEl || windowEl.classList.contains('maximized')) return;
        const rect = windowEl.getBoundingClientRect();
        // pin the current geometry so transform-centered windows resize sanely
        windowEl.style.left = rect.left + 'px';
        windowEl.style.top = rect.top + 'px';
        windowEl.style.transform = 'none';
        resizeState = {
            windowEl,
            dir: handle.dataset.resizeDir,
            startX: e.clientX,
            startY: e.clientY,
            rect
        };
        getAllWindows().forEach(w => w.classList.remove('active'));
        windowEl.classList.add('active');
        e.preventDefault();
        e.stopPropagation();
    }, true);

    document.addEventListener('mousemove', function(e) {
        if (!resizeState) return;
        const { windowEl, dir, startX, startY, rect } = resizeState;
        const minW = windowEl.id === 'minesweeperWindow' ? 120 : 300;
        const minH = 120;
        const dx = e.clientX - startX;
        const dy = e.clientY - startY;
        let left = rect.left;
        let top = rect.top;
        let width = rect.width;
        let height = rect.height;
        if (dir.includes('e')) width = Math.max(minW, rect.width + dx);
        if (dir.includes('s')) height = Math.max(minH, rect.height + dy);
        if (dir.includes('w')) {
            width = Math.max(minW, rect.width - dx);
            left = rect.left + rect.width - width;
        }
        if (dir.includes('n')) {
            height = Math.max(minH, rect.height - dy);
            top = rect.top + rect.height - height;
        }
        windowEl.style.left = left + 'px';
        windowEl.style.top = top + 'px';
        windowEl.style.width = width + 'px';
        windowEl.style.height = height + 'px';
    });

    document.addEventListener('mouseup', function() {
        resizeState = null;
    });

    // Win98 wireframe zoom shown when windows minimize/maximize/restore.
    function animateZoom(fromRect, toRect) {
        if (!fromRect || !toRect) return;
        const frame = document.createElement('div');
        frame.className = 'zoom-anim';
        frame.style.left = fromRect.left + 'px';
        frame.style.top = fromRect.top + 'px';
        frame.style.width = fromRect.width + 'px';
        frame.style.height = fromRect.height + 'px';
        document.body.appendChild(frame);
        // force layout so the transition runs
        void frame.offsetWidth;
        frame.style.left = toRect.left + 'px';
        frame.style.top = toRect.top + 'px';
        frame.style.width = toRect.width + 'px';
        frame.style.height = toRect.height + 'px';
        setTimeout(() => frame.remove(), 220);
    }

    function taskbarRectFor(windowId) {
        const item = document.querySelector(`.taskbar-item[data-window="${windowId}"]`);
        if (item) return item.getBoundingClientRect();
        return { left: 4, top: window.innerHeight - 26, width: 150, height: 22 };
    }

    // Window controls (delegated): one handler covers every window's
    // minimize/maximize/close buttons, including factory-built titlebars.
    document.addEventListener('click', function(e) {
        const control = e.target.closest ? e.target.closest('.window-control') : null;
        if (!control) return;
        const windowEl = control.closest('.window');
        if (!windowEl) return;

        if (control.classList.contains('minimize-btn')) {
            minimizeWindow(windowEl.id);
        } else if (control.classList.contains('maximize-btn')) {
            const before = windowEl.getBoundingClientRect();
            windowEl.classList.toggle('maximized');
            animateZoom(before, windowEl.getBoundingClientRect());
        } else if (control.classList.contains('close-btn')) {
            closeWindowElement(windowEl);
        }
    });

    const welcomeCloseButton = document.querySelector('#welcomeWindow .welcome-button');
    if (welcomeCloseButton) {
        welcomeCloseButton.addEventListener('click', function() {
            const welcomeWindow = document.getElementById('welcomeWindow');
            if (welcomeWindow) {
                closeWindowElement(welcomeWindow);
            }
        });
    }

    // Minimize window function
    function minimizeWindow(windowId) {
        const window = document.getElementById(windowId);
        if (window) {
            animateZoom(window.getBoundingClientRect(), taskbarRectFor(windowId));
            window.classList.add('minimized');
            window.style.display = 'none'; // Hide the window

            // Find the corresponding taskbar item and update its state
            const taskbarItems = document.querySelectorAll('.taskbar-item');
            taskbarItems.forEach(item => {
                if (item.getAttribute('data-window') === windowId) {
                    item.classList.remove('active');
                }
            });

            updateTaskbar(); // Update taskbar to reflect changes
        }
    }

    // Desktop icons (delegated). Filesystem-managed icons are handled by
    // filesystem.js; this only covers the static shell icons.
    function isFilesystemDesktopIcon(icon) {
        return icon && icon.dataset.filesystemDesktop === 'true';
    }

    function openStaticDesktopIcon(icon) {
        const staticOpenPath = icon.dataset.openPath;
        const windowId = icon.getAttribute('data-window');
        if (staticOpenPath && windowId) {
            openStaticDesktopWindow(windowId, staticOpenPath);
            return;
        }

        const staticPath = icon.dataset.path;
        if (staticPath) {
            if (typeof window.win98OpenPath === 'function') {
                window.win98OpenPath(staticPath, '');
            }
            return;
        }

        if (windowId === 'doomWindow') {
            openDoomFolderInExplorer();
        } else if (windowId) {
            activateWindow(document.getElementById(windowId));
        }
    }

    document.addEventListener('click', function(e) {
        const icon = e.target.closest ? e.target.closest('.desktop-icon') : null;
        if (!icon || isFilesystemDesktopIcon(icon)) return;

        const wasSelected = icon.classList.contains('active');
        document.querySelectorAll('.desktop-icon').forEach(other => other.classList.remove('active'));
        icon.classList.add('active');
        if (wasSelected || e.detail >= 2 || wasRecentlyActivated(icon)) {
            openStaticDesktopIcon(icon);
        }
        e.stopPropagation();
    });

    document.addEventListener('dblclick', function(e) {
        const icon = e.target.closest ? e.target.closest('.desktop-icon') : null;
        if (!icon || isFilesystemDesktopIcon(icon)) return;
        openStaticDesktopIcon(icon);
        e.stopPropagation();
    });

    document.addEventListener('touchstart', function(e) {
        const icon = e.target.closest ? e.target.closest('.desktop-icon') : null;
        if (!icon || isFilesystemDesktopIcon(icon)) return;
        e.preventDefault(); // Prevent mouse events from firing as well
        openStaticDesktopIcon(icon);
    }, { passive: false });

    // Clear static icon selection when clicking empty desktop space.
    document.addEventListener('click', function(e) {
        if (e.target.closest && e.target.closest('.desktop-icon')) return;
        document.querySelectorAll('.desktop-icon').forEach(icon => icon.classList.remove('active'));
    });

    // Start menu items
    const menuItems = document.querySelectorAll('.menu-item');

    menuItems.forEach(item => {
        item.addEventListener('click', function(e) {
            if (this.classList.contains('has-submenu')) return;

            const closeStartMenu = () => {
                document.getElementById('startMenu').classList.remove('active');
                document.getElementById('startBtn').classList.remove('active');
            };

            const staticPath = this.dataset.path;
            if (staticPath) {
                if (typeof window.win98OpenPath === 'function') {
                    window.win98OpenPath(staticPath, '');
                }
                closeStartMenu();
                e.stopPropagation();
                return;
            }

            const windowId = this.getAttribute('data-window');
            if (windowId === 'doomWindow') {
                openDoomFolderInExplorer();
            } else if (windowId) {
                activateWindow(document.getElementById(windowId));
            }

            closeStartMenu();
            e.stopPropagation();
        });
    });

    // Taskbar
    function updateTaskbar() {
        const taskbarItems = document.getElementById('taskbarItems');
        taskbarItems.innerHTML = '';

        getAllWindows().forEach(window => {
            if (window.style.display !== 'none' || window.classList.contains('minimized')) {
                const windowId = window.id;
                const windowTitle = window.querySelector('.window-title').textContent.trim();
                const windowIcon = window.querySelector('.window-title img');

                const taskbarItem = document.createElement('div');
                taskbarItem.className = 'taskbar-item';
                if (window.classList.contains('active') && !window.classList.contains('minimized')) {
                    taskbarItem.classList.add('active');
                }
                taskbarItem.setAttribute('data-window', windowId);

                if (windowIcon) {
                    const img = document.createElement('img');
                    img.src = windowIcon.src;
                    img.alt = windowIcon.alt;
                    taskbarItem.appendChild(img);
                }

                taskbarItem.appendChild(document.createTextNode(windowTitle));

                taskbarItem.addEventListener('click', function() {
                    const targetWindow = document.getElementById(windowId);
                    if (targetWindow.classList.contains('minimized') || !targetWindow.classList.contains('active')) {
                        const wasMinimized = targetWindow.classList.contains('minimized');
                        getAllWindows().forEach(w => w.classList.remove('active'));
                        targetWindow.classList.add('active');
                        targetWindow.classList.remove('minimized');
                        targetWindow.style.display = 'block'; // Make sure to show the window
                        if (wasMinimized) {
                            animateZoom(taskbarItem.getBoundingClientRect(), targetWindow.getBoundingClientRect());
                        }
                    } else {
                        animateZoom(targetWindow.getBoundingClientRect(), taskbarItem.getBoundingClientRect());
                        targetWindow.classList.add('minimized');
                        targetWindow.style.display = 'none'; // Hide the window
                    }
                    updateTaskbar();
                });

                taskbarItems.appendChild(taskbarItem);
            }
        });
    }

    // Minimize everything that is currently visible (Show Desktop).
    function minimizeAllWindows() {
        getAllWindows().forEach(w => {
            if (w.style.display !== 'none') {
                w.classList.add('minimized');
                w.classList.remove('active');
                w.style.display = 'none';
            }
        });
        updateTaskbar();
    }

    // Shared with desktop.js (quick launch, start menu extras).
    window.win98ActivateWindow = activateWindow;
    window.win98MinimizeAllWindows = minimizeAllWindows;
    window.win98MinimizeWindow = minimizeWindow;
    window.win98CloseWindow = closeWindowElement;

    // Initial taskbar update
    updateTaskbar();
