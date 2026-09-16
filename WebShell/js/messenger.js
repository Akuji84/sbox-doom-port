    // Messenger: shell-to-shell chat through the dashboard relay.
(function () {
    'use strict';

    const windowEl = document.getElementById('messengerWindow');
    if (!windowEl) return;
    const shellParams = new URLSearchParams(window.location.search);
    const channel = shellParams.get('channel') === 'beta' ? 'beta' : 'live';
    // in-game the launcher passes the player's Steam name; lock it in so
    // players can't impersonate each other
    const lockedName = (shellParams.get('player') || '').trim().slice(0, 24);
    const listEl = document.getElementById('msgrList');
    const inputEl = document.getElementById('msgrInput');
    const nameEl = document.getElementById('msgrName');
    const statusEl = document.getElementById('msgrStatus');

    let lastId = 0;
    let pollTimer = null;

    if (lockedName) {
        nameEl.value = lockedName;
        nameEl.readOnly = true;
        nameEl.title = 'Signed in with your Steam name';
    } else {
        try { nameEl.value = localStorage.getItem('win98ge.chatName') || ''; } catch (err) {}
    }

    function loadMuted() {
        try { return JSON.parse(localStorage.getItem('win98ge.chatMuted') || '[]'); } catch (err) { return []; }
    }

    function saveMuted(muted) {
        try { localStorage.setItem('win98ge.chatMuted', JSON.stringify(muted.slice(0, 50))); } catch (err) {}
    }

    function appendMessage(message) {
        if (loadMuted().includes(message.name)) return;
        const row = document.createElement('div');
        row.className = 'msgr-row';
        row.dataset.sender = message.name;
        const who = document.createElement('span');
        who.className = 'msgr-who';
        who.textContent = message.name + ' says:';
        const text = document.createElement('div');
        text.className = 'msgr-text';
        text.textContent = message.text;
        row.appendChild(who);
        row.appendChild(text);
        listEl.appendChild(row);
        while (listEl.children.length > 100) listEl.removeChild(listEl.firstChild);
        listEl.scrollTop = listEl.scrollHeight;
    }

    // right-click a message to ignore that player locally
    listEl.addEventListener('contextmenu', event => {
        const row = event.target.closest('.msgr-row');
        if (!row || typeof window.win98ShowContextMenuItems !== 'function') return;
        event.preventDefault();
        event.stopPropagation();
        const sender = row.dataset.sender || '';
        const muted = loadMuted();
        window.win98ShowContextMenuItems(event.clientX, event.clientY, [
            { label: 'Ignore ' + sender, action: () => {
                if (sender && !muted.includes(sender)) {
                    muted.push(sender);
                    saveMuted(muted);
                }
                listEl.querySelectorAll('.msgr-row').forEach(r => {
                    if (r.dataset.sender === sender) r.remove();
                });
                statusEl.textContent = 'Ignoring ' + sender;
            } },
            { separator: true },
            { label: 'Stop ignoring everyone', action: () => { saveMuted([]); statusEl.textContent = 'Ignore list cleared'; }, disabled: !muted.length }
        ]);
    });

    async function poll() {
        try {
            const response = await fetch('/api/win98-shell/' + channel + '/chat?since=' + lastId);
            if (!response.ok) throw new Error('bad status');
            const payload = await response.json();
            if (payload.enabled === false) {
                statusEl.textContent = 'Chat is currently disabled by the administrator.';
                return;
            }
            (payload.messages || []).forEach(message => {
                if (message.id > lastId) lastId = message.id;
                appendMessage(message);
            });
            statusEl.textContent = 'Connected';
        } catch (err) {
            statusEl.textContent = 'Service unavailable - dialing again...';
        }
    }

    async function send() {
        const name = lockedName || nameEl.value.trim() || 'Anonymous Marine';
        const text = inputEl.value.trim();
        if (!text) return;
        if (!lockedName) {
            try { localStorage.setItem('win98ge.chatName', name); } catch (err) {}
        }
        inputEl.value = '';
        try {
            const response = await fetch('/api/win98-shell/' + channel + '/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, text })
            });
            if (!response.ok) throw new Error('bad status');
            poll();
        } catch (err) {
            statusEl.textContent = 'Message could not be delivered.';
            window.win98PlaySound?.('error');
        }
    }

    document.getElementById('msgr-send-btn').addEventListener('click', send);
    inputEl.addEventListener('keydown', event => {
        event.stopPropagation();
        if (event.key === 'Enter') send();
    });
    nameEl.addEventListener('keydown', event => event.stopPropagation());

    function startPolling() {
        poll();
        if (pollTimer) clearInterval(pollTimer);
        pollTimer = setInterval(() => {
            if (windowEl.style.display !== 'none') poll();
        }, 4000);
    }

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.messengerWindow = function () {
        window.win98ActivateWindow?.(windowEl);
        startPolling();
        setTimeout(() => inputEl.focus(), 50);
    };

    document.getElementById('menuMessenger')?.addEventListener('click', () => window.win98AppOpenHooks.messengerWindow());
})();
