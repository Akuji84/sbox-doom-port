    // Shell extras: Win98 tooltips, Date/Time Properties, Volume Control
    // mixer, logon screen, Disk Defragmenter, Find Files.
(function () {
    'use strict';

    // ------------------------------------------------------------ tooltips
    const tooltip = document.createElement('div');
    tooltip.className = 'w98-tooltip';
    document.body.appendChild(tooltip);
    let tipTimer = null;

    function hideTip() {
        if (tipTimer) clearTimeout(tipTimer);
        tipTimer = null;
        tooltip.style.display = 'none';
    }

    document.addEventListener('mouseover', event => {
        const el = event.target.closest ? event.target.closest('[title], [data-w98tip]') : null;
        if (!el) return;
        if (el.hasAttribute('title')) {
            el.dataset.w98tip = el.getAttribute('title');
            el.removeAttribute('title'); // suppress the native tooltip
        }
        const text = el.dataset.w98tip;
        if (!text) return;
        if (tipTimer) clearTimeout(tipTimer);
        tipTimer = setTimeout(() => {
            tooltip.textContent = text;
            tooltip.style.display = 'block';
            const rect = el.getBoundingClientRect();
            let x = rect.left;
            let y = rect.bottom + 4;
            if (y + 24 > window.innerHeight) y = rect.top - 22;
            if (x + tooltip.offsetWidth > window.innerWidth) x = window.innerWidth - tooltip.offsetWidth - 4;
            tooltip.style.left = x + 'px';
            tooltip.style.top = y + 'px';
        }, 500);
    });
    document.addEventListener('mouseout', hideTip);
    document.addEventListener('mousedown', hideTip, true);

    // ------------------------------------------------ date/time properties
    const dtWindow = document.getElementById('dateTimeWindow');
    let dtTimer = null;

    function renderCalendar() {
        const grid = document.getElementById('dtCalendar');
        const now = new Date();
        const year = now.getFullYear();
        const month = now.getMonth();
        document.getElementById('dtMonthLabel').textContent =
            now.toLocaleString('en-US', { month: 'long' }) + ' ' + year;
        grid.innerHTML = '';
        ['S', 'M', 'T', 'W', 'T', 'F', 'S'].forEach(d => {
            const cell = document.createElement('div');
            cell.className = 'dt-cell dt-head';
            cell.textContent = d;
            grid.appendChild(cell);
        });
        const first = new Date(year, month, 1).getDay();
        const days = new Date(year, month + 1, 0).getDate();
        for (let i = 0; i < first; i++) grid.appendChild(document.createElement('div'));
        for (let day = 1; day <= days; day++) {
            const cell = document.createElement('div');
            cell.className = 'dt-cell' + (day === now.getDate() ? ' dt-today' : '');
            cell.textContent = day;
            grid.appendChild(cell);
        }
    }

    function drawClock() {
        const canvas = document.getElementById('dtClock');
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        const r = canvas.width / 2;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#c0c0c0';
        ctx.strokeStyle = '#000000';
        ctx.beginPath();
        ctx.arc(r, r, r - 3, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
        for (let i = 0; i < 12; i++) {
            const a = i / 12 * Math.PI * 2;
            ctx.beginPath();
            ctx.moveTo(r + Math.sin(a) * (r - 8), r - Math.cos(a) * (r - 8));
            ctx.lineTo(r + Math.sin(a) * (r - 13), r - Math.cos(a) * (r - 13));
            ctx.stroke();
        }
        const now = new Date();
        const hourA = (now.getHours() % 12 + now.getMinutes() / 60) / 12 * Math.PI * 2;
        const minA = now.getMinutes() / 60 * Math.PI * 2;
        const secA = now.getSeconds() / 60 * Math.PI * 2;
        function hand(angle, length, width, color) {
            ctx.strokeStyle = color;
            ctx.lineWidth = width;
            ctx.beginPath();
            ctx.moveTo(r, r);
            ctx.lineTo(r + Math.sin(angle) * length, r - Math.cos(angle) * length);
            ctx.stroke();
        }
        hand(hourA, r * 0.45, 3, '#000080');
        hand(minA, r * 0.68, 2, '#000080');
        hand(secA, r * 0.75, 1, '#800000');
        ctx.lineWidth = 1;
        document.getElementById('dtDigital').textContent = now.toLocaleTimeString('en-US');
    }

    function openDateTime() {
        renderCalendar();
        drawClock();
        if (dtTimer) clearInterval(dtTimer);
        dtTimer = setInterval(drawClock, 1000);
        window.win98ActivateWindow?.(dtWindow);
    }

    document.querySelector('.tray-time')?.addEventListener('dblclick', openDateTime);
    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.dateTimeWindow = openDateTime;
    document.getElementById('dt-ok-btn')?.addEventListener('click', () => {
        if (dtTimer) clearInterval(dtTimer);
        dtWindow.querySelector('.close-btn')?.click();
    });

    // -------------------------------------------------- volume control mixer
    const mixerWindow = document.getElementById('volumeMixerWindow');

    function openMixer() {
        const settings = window.win98GetShellSettings ? window.win98GetShellSettings() : { volume: 80, muted: false };
        const master = document.getElementById('mixMaster');
        const masterMute = document.getElementById('mixMasterMute');
        master.value = settings.volume;
        masterMute.checked = settings.muted;
        window.win98ActivateWindow?.(mixerWindow);
    }

    document.getElementById('trayVolume')?.addEventListener('dblclick', event => {
        event.stopPropagation();
        openMixer();
    });
    document.getElementById('mixMaster')?.addEventListener('input', event => {
        window.win98SetShellVolume?.(parseInt(event.target.value, 10) || 0, undefined);
    });
    document.getElementById('mixMasterMute')?.addEventListener('change', event => {
        const settings = window.win98GetShellSettings ? window.win98GetShellSettings() : { volume: 80 };
        window.win98SetShellVolume?.(settings.volume, event.target.checked);
    });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.volumeMixerWindow = openMixer;

    // ------------------------------------------------------------ logon box
    try {
        if (sessionStorage.getItem('win98ge.showLogin') === '1') {
            sessionStorage.removeItem('win98ge.showLogin');
            const screen = document.createElement('div');
            screen.className = 'login-screen';
            screen.innerHTML =
                '<div class="dialog login-dialog" style="display:block; position:relative; top:auto; left:auto; transform:none; width:360px;">' +
                '<div class="dialog-titlebar">Welcome to Windows</div>' +
                '<div class="dialog-content">' +
                '<div class="shutdown-body">' +
                '<img class="shutdown-icon" src="./assets/icons/my_computer.png" alt="">' +
                '<div class="shutdown-question">Type a user name and password to log on to Windows.</div>' +
                '</div>' +
                '<div class="form-row"><div class="form-label">User name:</div><input type="text" class="form-input" id="loginUser" value="Player"></div>' +
                '<div class="form-row"><div class="form-label">Password:</div><input type="password" class="form-input" id="loginPass"></div>' +
                '</div>' +
                '<div class="dialog-buttons"><div class="btn" id="login-ok-btn">OK</div><div class="btn" id="login-cancel-btn">Cancel</div></div>' +
                '</div>';
            document.body.appendChild(screen);
            const dismiss = () => {
                screen.remove();
                window.win98PlaySound?.('startup');
            };
            screen.querySelector('#login-ok-btn').addEventListener('click', dismiss);
            screen.querySelector('#login-cancel-btn').addEventListener('click', dismiss);
            screen.querySelector('#loginPass').addEventListener('keydown', e => {
                if (e.key === 'Enter') dismiss();
                e.stopPropagation();
            });
        }
    } catch (err) {}

    // ------------------------------------------------------ disk defragmenter
    const defragWindow = document.getElementById('defragWindow');
    let defragTimer = null;

    function startDefrag() {
        const canvas = document.getElementById('defragCanvas');
        const ctx = canvas.getContext('2d');
        const statusEl = document.getElementById('defragStatus');
        const cols = Math.floor(canvas.width / 7);
        const rows = Math.floor(canvas.height / 7);
        const total = cols * rows;
        // 0 free, 1 data, 2 fragmented, 3 optimized
        const cells = [];
        for (let i = 0; i < total; i++) {
            const roll = Math.random();
            cells.push(roll < 0.18 ? 0 : roll < 0.62 ? 1 : 2);
        }
        let cursor = 0;
        const COLORS = ['#ffffff', '#0000a8', '#ff0000', '#00a8a8'];

        function draw() {
            for (let i = 0; i < total; i++) {
                ctx.fillStyle = COLORS[cells[i]];
                ctx.fillRect((i % cols) * 7, Math.floor(i / cols) * 7, 6, 6);
            }
        }

        if (defragTimer) clearInterval(defragTimer);
        draw();
        defragTimer = setInterval(() => {
            for (let step = 0; step < 14 && cursor < total; step++) {
                if (cells[cursor] === 2) {
                    // "move" the fragment somewhere later and optimize here
                    const swap = cursor + 1 + Math.floor(Math.random() * Math.max(1, total - cursor - 1));
                    if (swap < total) cells[swap] = cells[swap] === 0 ? 2 : cells[swap];
                }
                if (cells[cursor] !== 0) cells[cursor] = 3;
                cursor++;
            }
            draw();
            const pct = Math.floor(cursor / total * 100);
            statusEl.textContent = cursor >= total
                ? 'Defragmentation of drive C: is complete.'
                : 'Defragmenting drive C: ... ' + pct + '% complete';
            if (cursor >= total) {
                clearInterval(defragTimer);
                defragTimer = null;
                window.win98PlaySound?.('chord');
            }
        }, 90);
    }

    window.win98AppOpenHooks.defragWindow = function () {
        window.win98ActivateWindow?.(defragWindow);
        startDefrag();
    };
    document.getElementById('menuDefrag')?.addEventListener('click', () => window.win98AppOpenHooks.defragWindow());

    // ------------------------------------------------------------ find files
    function runFind() {
        const query = document.getElementById('findFilesInput').value;
        const resultsEl = document.getElementById('findFilesResults');
        resultsEl.innerHTML = '';
        const results = window.win98FsHelpers ? window.win98FsHelpers.find(query) : [];
        if (!results.length) {
            resultsEl.innerHTML = '<div class="find-empty">0 file(s) found</div>';
            return;
        }
        results.forEach(item => {
            const row = document.createElement('div');
            row.className = 'find-row';
            row.innerHTML = '<img src="./assets/icons/' +
                (item.type === 'folder' ? 'folder_closed.png' : item.type === 'app' ? 'executable.png' : 'document.png') +
                '" alt=""> <span class="find-name"></span> <span class="find-loc"></span>';
            row.querySelector('.find-name').textContent = item.name;
            row.querySelector('.find-loc').textContent = item.parent || item.path;
            row.addEventListener('dblclick', () => {
                document.getElementById('findFilesWindow').querySelector('.close-btn')?.click();
                window.win98OpenPath?.(item.path, '');
            });
            resultsEl.appendChild(row);
        });
        document.getElementById('findFilesStatus').textContent = results.length + ' file(s) found';
    }

    document.getElementById('menuFind')?.addEventListener('click', () => {
        window.win98ActivateWindow?.(document.getElementById('findFilesWindow'));
        setTimeout(() => document.getElementById('findFilesInput').focus(), 50);
    });
    document.getElementById('menuHelp')?.addEventListener('click', () => {
        window.win98ActivateWindow?.(document.getElementById('helpWindow'));
    });

    document.getElementById('findFilesGo')?.addEventListener('click', runFind);
    document.getElementById('findFilesInput')?.addEventListener('keydown', event => {
        if (event.key === 'Enter') runFind();
        event.stopPropagation();
    });

    // ------------------------------------------------ Welcome window tour
    const TOUR_SLIDES = [
        '<h2>Discover Windows 98</h2><p>Welcome to the s&amp;Doom desktop. This machine boots straight into the good part of 1998.</p><p>Everything here works: drag windows, resize them, right-click things, and press Ctrl+Alt+Del if you dare.</p>',
        '<h2>Playing DOOM</h2><p>Open the <b>DOOM</b> folder on the desktop (or C:\\DOOM in My Computer) and double-click a game to play it.</p><p>Run <b>CONTROLS.EXE</b> in the same folder first to set your keys - they carry into the game.</p>',
        '<h2>The Internet</h2><p>Internet Explorer actually browses. Try <b>leaderboards.akuji.org</b> for live world rankings, or search the web with WebFetcher.</p>',
        '<h2>Other people</h2><p><b>Network Neighborhood</b> shows who is online right now, and <b>Messenger</b> (Start &gt; Programs) lets you chat with them.</p><p>That is the end of the tour. Now go rip and tear.</p>'
    ];
    let tourIndex = -1;

    function renderTour() {
        const contentEl = document.querySelector('#welcomeWindow .welcome-content');
        if (!contentEl) return;
        contentEl.innerHTML = TOUR_SLIDES[tourIndex] +
            '<p style="margin-top:12px;"><a href="#" id="tourNext" style="color:#000080; font-weight:bold;">' +
            (tourIndex < TOUR_SLIDES.length - 1 ? 'Next &gt;' : 'Start over') + '</a></p>';
        contentEl.querySelector('#tourNext').addEventListener('click', event => {
            event.preventDefault();
            tourIndex = (tourIndex + 1) % TOUR_SLIDES.length;
            renderTour();
        });
    }

    document.querySelectorAll('#welcomeWindow .sidebar-item').forEach(item => {
        item.addEventListener('click', () => {
            document.querySelectorAll('#welcomeWindow .sidebar-item').forEach(other => other.classList.remove('active'));
            item.classList.add('active');
            const label = item.textContent.trim();
            const contentEl = document.querySelector('#welcomeWindow .welcome-content');
            if (label === 'Discover Windows 98') {
                tourIndex = 0;
                renderTour();
            } else if (label === 'Register Now') {
                contentEl.innerHTML = '<h2>Register Now</h2><p>Thank you for registering Windows 98!</p><p>Your registration card has been mailed to Redmond by carrier pigeon. Please allow 6-8 weeks.</p>';
            } else if (label === 'Connect to the Internet') {
                contentEl.innerHTML = '<h2>Connect to the Internet</h2><p>Good news: you are already connected.</p><p>Open Internet Explorer and visit <b>www.akuji.org</b> - no dial-up required (the modem sounds are purely decorative).</p>';
            } else if (label === 'Maintain Your Computer') {
                contentEl.innerHTML = '<h2>Maintain Your Computer</h2><p>Run <b>Disk Defragmenter</b> (Start &gt; Programs &gt; Accessories &gt; System Tools) and watch the little blocks. It is very soothing.</p><p>This computer has never needed maintenance and never will.</p>';
            }
        });
    });
})();
