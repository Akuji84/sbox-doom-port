    // Network Neighborhood: shows players currently online, fed by the
    // dashboard presence endpoint. Falls back to a quiet offline state.
(function () {
    'use strict';

    const windowEl = document.getElementById('networkNeighborhood');
    if (!windowEl) return;
    const contentsEl = windowEl.querySelector('.window-contents');
    const statusEl = windowEl.querySelector('.window-statusbar');
    const channel = new URLSearchParams(window.location.search).get('channel') === 'beta' ? 'beta' : 'live';
    let refreshTimer = null;

    function renderPlayers(players) {
        contentsEl.innerHTML = '';

        const entire = document.createElement('div');
        entire.className = 'folder-item';
        entire.innerHTML = '<img class="folder-icon" src="./assets/icons/entire_network.png" alt=""><div class="folder-name">Entire Network</div>';
        contentsEl.appendChild(entire);

        players.forEach(player => {
            const item = document.createElement('div');
            item.className = 'folder-item';
            const img = document.createElement('img');
            img.className = 'folder-icon';
            img.src = './assets/icons/my_computer.png';
            const name = document.createElement('div');
            name.className = 'folder-name';
            name.textContent = player.name || 'Unknown';
            const meta = document.createElement('div');
            meta.className = 'folder-meta';
            meta.textContent = player.map ? 'Playing ' + player.map : 'On the desktop';
            item.appendChild(img);
            item.appendChild(name);
            item.appendChild(meta);
            item.title = (player.name || 'Unknown') + ' - online now';
            contentsEl.appendChild(item);
        });

        statusEl.textContent = players.length
            ? players.length + ' marine(s) online'
            : '1 object(s) - nobody else is online right now';
    }

    let knownNames = null;

    function showOnlineBalloon(name) {
        const balloon = document.createElement('div');
        balloon.className = 'w98-balloon';
        balloon.textContent = '🖧 ' + name + ' is now online';
        document.body.appendChild(balloon);
        window.win98PlaySound?.('chord');
        setTimeout(() => balloon.classList.add('visible'), 30);
        setTimeout(() => {
            balloon.classList.remove('visible');
            setTimeout(() => balloon.remove(), 400);
        }, 5000);
    }

    async function refresh() {
        try {
            const response = await fetch('/api/win98-shell/' + channel + '/presence');
            if (!response.ok) throw new Error('bad status');
            const payload = await response.json();
            const players = Array.isArray(payload.players) ? payload.players : [];

            // "you've got mail" moment when somebody new appears
            const names = new Set(players.map(p => p.name));
            if (knownNames !== null) {
                names.forEach(name => {
                    if (!knownNames.has(name)) showOnlineBalloon(name);
                });
            }
            knownNames = names;

            renderPlayers(players);
        } catch (err) {
            renderPlayers([]);
            statusEl.textContent = '1 object(s) - network is unreachable';
        }
    }

    function startPolling() {
        refresh();
        if (refreshTimer) clearInterval(refreshTimer);
        refreshTimer = setInterval(() => {
            if (windowEl.style.display !== 'none') refresh();
        }, 30000);
    }

    // Refresh whenever the window is opened from anywhere.
    document.addEventListener('click', event => {
        if (event.target.closest('[data-window="networkNeighborhood"]')) {
            setTimeout(refresh, 100);
        }
    });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.networkNeighborhood = function () {
        window.win98ActivateWindow?.(windowEl);
        refresh();
    };

    startPolling();
})();
