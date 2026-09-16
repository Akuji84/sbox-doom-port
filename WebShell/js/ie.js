    // Internet Explorer: a real (tiny) browser over a curated 1998 intranet.
    // Address bar + back/forward/stop/refresh/home + history all work; pages
    // render in a same-origin srcdoc iframe so in-page links route back
    // through the shell. Unknown addresses get the classic DNS error page.
(function () {
    'use strict';

    const windowEl = document.getElementById('internetExplorer');
    if (!windowEl) return;
    const addressInput = document.getElementById('ieAddressInput');
    const frame = document.getElementById('ieFrame');
    const statusEl = document.getElementById('ieStatus');
    const ieChannel = new URLSearchParams(window.location.search).get('channel') === 'beta' ? 'beta' : 'live';
    const FAV_KEY = 'win98ge.ieFavorites.v1';

    function loadFavorites() {
        try { return JSON.parse(localStorage.getItem(FAV_KEY) || '[]'); } catch (err) { return []; }
    }

    function saveFavorites(favs) {
        try { localStorage.setItem(FAV_KEY, JSON.stringify(favs.slice(0, 20))); } catch (err) {}
    }

    const BASE_STYLE =
        '<style>' +
        'body { font-family: "Times New Roman", serif; margin: 10px; background: #ffffff; color: #000000; }' +
        'a { color: #0000ee; } a:visited { color: #551a8b; }' +
        'h1, h2 { font-family: Arial, sans-serif; }' +
        'hr { border: 0; border-top: 1px solid #808080; }' +
        '.construction { color: #ff0000; font-weight: bold; }' +
        'table.retro { border: 2px outset #c0c0c0; background: #c0c0c0; }' +
        'td { font-size: 14px; }' +
        'marquee { background: #000080; color: #ffff00; padding: 2px; }' +
        '.counter { font-family: "Courier New", monospace; background: #000; color: #0f0; padding: 1px 4px; letter-spacing: 2px; }' +
        '</style>';

    function searchResultsPage(query) {
        const q = query ? String(query) : '';
        return {
            title: 'WebFetcher Search',
            html:
                '<h1><font color="#008080">Web</font><font color="#800080">Fetcher</font></h1>' +
                '<form data-search><input name="q" size="40" value="' + q.replace(/"/g, '&quot;') + '"> <input type="submit" value="Fetch!"></form><hr>' +
                (q ? '<p>Results <b>1-4</b> of about <b>2,147,483,647</b> for <b>' + q + '</b>:</p>' : '<p>Type something and click Fetch!</p>') +
                '<p><a href="http://www.akuji.org">Akuji dot Org - Your home on the Information Superhighway</a><br><small>www.akuji.org - 4k - cached</small></p>' +
                '<p><a href="http://www.doomshrine.net">*** THE UNOFFICIAL DOOM SHRINE ***</a><br><small>www.doomshrine.net - 9k - cached</small></p>' +
                '<p><a href="http://leaderboards.akuji.org">DOOM World Rankings - who rips, who tears</a><br><small>leaderboards.akuji.org - live!</small></p>' +
                '<p><a href="http://weather.akuji.org">Weather98 - forecast for your desktop</a><br><small>weather.akuji.org - 2k</small></p>' +
                '<p><a href="http://downloads.akuji.org">FreeFileZone - 100% FREE DOWNLOADS</a><br><small>downloads.akuji.org - 6k - popup free*</small></p>' +
                '<p><a href="http://members.geocities.com/~doomguy">Doomguy\'s Home Page - UNDER CONSTRUCTION</a><br><small>members.geocities.com - 2k - cached</small></p>'
        };
    }

    const SITES = {
        'www.akuji.org': () => ({
            title: 'Akuji dot Org',
            html:
                '<marquee>*** Welcome to AKUJI DOT ORG - best viewed in 800x600 with 256 colors ***</marquee>' +
                '<center><h1><font color="#800000">~ A K U J I . O R G ~</font></h1>' +
                '<p><i>Your home on the Information Superhighway since 1998</i></p></center><hr>' +
                '<h2>Hot Links</h2><ul>' +
                '<li><a href="http://www.doomshrine.net">The UNOFFICIAL Doom Shrine</a> - rip and tear!</li>' +
                '<li><a href="http://leaderboards.akuji.org">DOOM World Rankings</a> - live leaderboards!</li>' +
                '<li><a href="http://www.webfetcher.com">WebFetcher</a> - search the entire World Wide Web</li>' +
                '<li><a href="http://weather.akuji.org">Weather98</a> - today\'s forecast</li>' +
                '<li><a href="http://downloads.akuji.org">FreeFileZone</a> - totally legit free downloads</li>' +
                '<li><a href="http://members.geocities.com/~doomguy">Doomguy\'s home page</a> (always under construction)</li>' +
                '</ul><hr>' +
                '<h2>News</h2>' +
                '<p><b>1998-07-05</b> - This machine now runs Windows 98! Double-click the DOOM folder on the desktop to play.</p>' +
                '<p><b>1998-06-25</b> - Windows 98 released! Upgraded from 95, only crashed twice.</p><hr>' +
                '<center><p>You are visitor number <span class="counter">004217</span></p>' +
                '<p class="construction">&gt;&gt;&gt; This page is ALWAYS under construction &lt;&lt;&lt;</p>' +
                '<p><small>Sign my guestbook | webmaster@akuji.org | Made with Notepad</small></p></center>'
        }),
        'akuji.org': () => SITES['www.akuji.org'](),
        'www.webfetcher.com': (query) => searchResultsPage(query),
        'webfetcher.com': (query) => searchResultsPage(query),
        'www.doomshrine.net': () => ({
            title: '*** THE UNOFFICIAL DOOM SHRINE ***',
            html:
                '<body bgcolor="#000000"><style>body{background:#000;color:#c0c0c0}h1{color:#f00}a{color:#ff8000 !important}</style>' +
                '<center><h1>*** THE UNOFFICIAL DOOM SHRINE ***</h1>' +
                '<p><font color="#ff0000" size="5"><b>RIP AND TEAR</b></font></p></center><hr>' +
                '<h2><font color="#ff4000">Why DOOM rules</font></h2>' +
                '<ul><li>The shotgun. Enough said.</li>' +
                '<li>E1M1 has the greatest MIDI riff ever composed by man or demon.</li>' +
                '<li>IDDQD and IDKFA (don\'t tell anyone)</li>' +
                '<li>Secret walls! Press SPACE on everything!</li></ul><hr>' +
                '<h2><font color="#ff4000">Tips from the masters</font></h2>' +
                '<p>Strafe, don\'t backpedal. Circle-strafe the Barons. Save your cells for the Cyberdemon.</p>' +
                '<p>Play it right now: it\'s in the <b>C:\\DOOM</b> folder of this very computer.</p><hr>' +
                '<center><p><a href="http://www.akuji.org">Back to akuji.org</a></p>' +
                '<p><small>This site is not affiliated with id Software. Obviously.</small></p></center></body>'
        }),
        'weather.akuji.org': () => {
            const temps = [68, 72, 75, 71, 66];
            const day = new Date().getDay() % temps.length;
            return {
                title: 'Weather98',
                html:
                    '<center><h1><font color="#008080">Weather</font><font color="#ff8000">98</font></h1>' +
                    '<p><i>Your desktop forecast, updated every 56k</i></p></center><hr>' +
                    '<center><table class="retro" cellpadding="8"><tr>' +
                    '<td><b>Today</b><br>Sunny<br><font size="6">' + temps[day] + '&deg;F</font></td>' +
                    '<td><b>Tomorrow</b><br>Sunny<br><font size="6">' + temps[(day + 1) % temps.length] + '&deg;F</font></td>' +
                    '<td><b>Hell</b><br>Fire and brimstone<br><font size="6">666&deg;F</font></td>' +
                    '</tr></table>' +
                    '<p><small>Forecast accuracy guaranteed for weather occurring in 1998 only.</small></p></center>'
            };
        },
        'leaderboards.akuji.org': () => {
            return fetch('/api/win98-shell/' + ieChannel + '/leaderboards')
                .then(r => { if (!r.ok) throw new Error('bad status'); return r.json(); })
                .then(data => {
                    const players = Array.isArray(data.players) ? data.players.slice(0, 20) : [];
                    const rows = players.map((p, i) =>
                        '<tr><td>' + (i + 1) + '.</td><td><b></b></td><td align="right">' + (p.kills || 0) +
                        '</td><td align="right">' + (p.deaths || 0) + '</td><td align="right">' + (p.levels || 0) + '</td></tr>');
                    const page = {
                        title: 'DOOM World Rankings',
                        html:
                            '<center><h1><font color="#800000">DOOM World Rankings</font></h1>' +
                            '<p><i>Live from the akuji.org data center</i></p></center><hr>' +
                            (players.length
                                ? '<center><table class="retro" cellpadding="4"><tr><th></th><th>Marine</th><th>Kills</th><th>Deaths</th><th>Levels</th></tr>' +
                                  rows.join('') + '</table></center>'
                                : '<center><p>No marines on the board yet. Be the first!</p></center>') +
                            '<hr><center><p><small>Updated in real time. Deaths are nothing to be ashamed of.</small></p></center>'
                    };
                    // player names injected via textContent to avoid HTML injection
                    setTimeout(() => {
                        const doc = frame.contentDocument;
                        if (!doc) return;
                        const cells = doc.querySelectorAll('table b');
                        players.forEach((p, i) => { if (cells[i]) cells[i].textContent = p.name || 'Unknown'; });
                    }, 150);
                    return page;
                })
                .catch(() => ({
                    title: 'DOOM World Rankings',
                    html: '<h2 style="font-family:Arial">The rankings server is not responding</h2>' +
                        '<p>The hamster powering the leaderboard modem appears to be asleep. Try again later.</p>'
                }));
        },
        'downloads.akuji.org': (query, url) => (url && url.pathname === '/dl' ? {
            title: 'Download Error',
            html:
                '<h2 style="font-family:Arial">Connection interrupted</h2>' +
                '<p>Someone picked up the phone and the download was lost at 97%.</p>' +
                '<p><a href="http://downloads.akuji.org">Return to FreeFileZone</a> and start over. This is authentic 1998.</p>'
        } : ({
            title: 'FreeFileZone - 100% FREE DOWNLOADS',
            html:
                '<body bgcolor="#ffffee">' +
                '<center><marquee>FREE DOWNLOADS FREE DOWNLOADS FREE DOWNLOADS - NO VIRUSES WE PROMISE</marquee>' +
                '<h1><font color="#ff0000">FreeFileZone</font></h1></center><hr>' +
                '<table class="retro" cellpadding="6" width="100%">' +
                '<tr><td><b>doom_shareware.zip</b> (2.2 MB)</td><td><a href="http://downloads.akuji.org/dl?f=doom">DOWNLOAD NOW!!</a></td></tr>' +
                '<tr><td><b>winamp_it_whips.exe</b> (0.9 MB)</td><td><a href="http://downloads.akuji.org/dl?f=winamp">DOWNLOAD NOW!!</a></td></tr>' +
                '<tr><td><b>dancing_baby.avi</b> (14 MB!!)</td><td><a href="http://downloads.akuji.org/dl?f=baby">DOWNLOAD NOW!!</a></td></tr>' +
                '<tr><td><b>totally_not_a_virus.exe</b> (666 KB)</td><td><a href="http://downloads.akuji.org/dl?f=virus">DOWNLOAD NOW!!</a></td></tr>' +
                '</table><hr>' +
                '<center><p><small>* Estimated download time on your modem: 4 hours 12 minutes. Do not pick up the phone.</small></p></center></body>'
        })),
        'members.geocities.com': () => ({
            title: "Doomguy's Home Page",
            html:
                '<body bgcolor="#ffffcc">' +
                '<center><h1>Doomguy\'s Totally Radical Home Page</h1>' +
                '<p class="construction">!!! UNDER CONSTRUCTION !!!</p>' +
                '<p>[insert dancing baby gif here when I figure out how]</p><hr>' +
                '<p>Hi, welcome to my page. I like: computers, DOOM, pizza.</p>' +
                '<p>Here is a picture of my computer: [broken image]</p>' +
                '<p><a href="http://www.akuji.org">back to akuji.org</a></p><hr>' +
                '<p><small>This page hosted by GeoCities. Get your own Free Home Page!</small></p></center></body>'
        })
    };

    const HOME_URL = 'http://www.akuji.org/';

    let history = [];
    let historyIndex = -1;
    let pendingNav = null;

    function setStatus(text) {
        if (statusEl) statusEl.textContent = text;
    }

    function setTitle(text) {
        const titleEl = windowEl.querySelector('.window-title');
        if (titleEl) {
            const img = titleEl.querySelector('img');
            titleEl.textContent = '';
            if (img) titleEl.appendChild(img);
            titleEl.appendChild(document.createTextNode(text + ' - Microsoft Internet Explorer'));
        }
        if (typeof window.win98RefreshAllExplorerWindows === 'function') {
            // taskbar label picks the new title up on the next taskbar update
        }
    }

    function parseUrl(raw) {
        let value = String(raw || '').trim();
        if (!value) return null;
        if (!/^[a-z]+:\/\//i.test(value)) value = 'http://' + value;
        try {
            return new URL(value);
        } catch (err) {
            return null;
        }
    }

    function errorPage(host) {
        return {
            title: 'Cannot find server',
            html:
                '<h2 style="font-family:Arial">The page cannot be displayed</h2>' +
                '<p>The page you are looking for is currently unavailable. The Web site might be experiencing technical difficulties, or you may need to adjust your browser settings.</p><hr>' +
                '<p>Please try the following:</p><ul>' +
                '<li>Click the <a href="__refresh__">Refresh</a> button, or try again later.</li>' +
                '<li>If you typed the page address in the Address bar, make sure that it is spelled correctly.</li>' +
                '<li>This computer\'s modem only reaches a very small internet. Try <a href="http://www.webfetcher.com">searching</a> instead.</li>' +
                '</ul><hr><p>Cannot find server or DNS Error<br>Internet Explorer</p>'
        };
    }

    function renderPage(page, url) {
        const doc = BASE_STYLE + page.html;
        frame.srcdoc = doc;
        frame.dataset.currentUrl = url;
        setTitle(page.title);
        setStatus('Done');
    }

    function navigate(rawUrl, options) {
        const opts = options || {};
        const url = parseUrl(rawUrl);
        if (!url) return;
        const display = url.href;

        if (pendingNav) clearTimeout(pendingNav);
        addressInput.value = display;
        setStatus('Finding site: ' + url.hostname + '...');
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('click');

        pendingNav = setTimeout(() => {
            pendingNav = null;
            setStatus('Opening page ' + display + '...');
            const builder = SITES[url.hostname.toLowerCase()];
            const query = url.searchParams.get('q') || '';
            const result = builder ? builder(query, url) : errorPage(url.hostname);

            if (!opts.noHistory) {
                history = history.slice(0, historyIndex + 1);
                history.push(display);
                historyIndex = history.length - 1;
            }
            Promise.resolve(result)
                .then(page => renderPage(page, display))
                .catch(() => renderPage(errorPage(url.hostname), display));
        }, 350 + Math.random() * 550);
    }

    frame.addEventListener('load', () => {
        const doc = frame.contentDocument;
        if (!doc) return;
        doc.addEventListener('click', event => {
            const a = event.target.closest ? event.target.closest('a') : null;
            if (!a) return;
            event.preventDefault();
            const href = a.getAttribute('href');
            if (href === '__refresh__') {
                navigate(frame.dataset.currentUrl, { noHistory: true });
            } else if (href) {
                navigate(href);
            }
        });
        doc.addEventListener('submit', event => {
            event.preventDefault();
            const input = event.target.querySelector('input[name="q"]');
            navigate('http://www.webfetcher.com/?q=' + encodeURIComponent(input ? input.value : ''));
        });
    });

    // ------------------------------------------------------------- toolbar
    document.getElementById('ie-back').addEventListener('click', () => {
        if (historyIndex > 0) {
            historyIndex--;
            navigate(history[historyIndex], { noHistory: true });
        }
    });
    document.getElementById('ie-forward').addEventListener('click', () => {
        if (historyIndex < history.length - 1) {
            historyIndex++;
            navigate(history[historyIndex], { noHistory: true });
        }
    });
    document.getElementById('ie-stop').addEventListener('click', () => {
        if (pendingNav) {
            clearTimeout(pendingNav);
            pendingNav = null;
            setStatus('Stopped');
        }
    });
    document.getElementById('ie-refresh').addEventListener('click', () => {
        if (frame.dataset.currentUrl) navigate(frame.dataset.currentUrl, { noHistory: true });
    });
    document.getElementById('ie-home').addEventListener('click', () => navigate(HOME_URL));
    document.getElementById('ie-search').addEventListener('click', () => navigate('http://www.webfetcher.com/'));

    addressInput.addEventListener('keydown', event => {
        if (event.key === 'Enter') navigate(addressInput.value);
        event.stopPropagation();
    });

    // ------------------------------------------------------------- menubar
    windowEl.querySelector('.window-menubar')?.addEventListener('click', event => {
        const item = event.target.closest('.window-menu-item');
        if (!item || typeof window.win98ShowContextMenuItems !== 'function') return;
        const rect = item.getBoundingClientRect();
        const label = item.textContent.trim().toLowerCase();
        let items = null;

        if (label === 'file') {
            items = [
                { label: 'New Window', disabled: true },
                { separator: true },
                { label: 'Close', action: () => windowEl.querySelector('.close-btn')?.click() }
            ];
        } else if (label === 'view') {
            items = [
                { label: 'Refresh', action: () => { if (frame.dataset.currentUrl) navigate(frame.dataset.currentUrl, { noHistory: true }); } },
                { label: 'Stop', action: () => document.getElementById('ie-stop').click() },
                { separator: true },
                { label: 'Source', action: viewSource }
            ];
        } else if (label === 'favorites') {
            const favs = loadFavorites();
            items = [
                { label: 'Add to Favorites...', action: addCurrentToFavorites },
                { separator: true }
            ];
            if (favs.length) {
                favs.forEach(fav => items.push({ label: fav.title || fav.url, action: () => navigate(fav.url) }));
            } else {
                items.push({ label: '(empty)', disabled: true });
            }
        } else if (label === 'tools') {
            items = [{ label: 'Internet Options...', disabled: true }];
        } else if (label === 'help') {
            items = [{ label: 'About Internet Explorer', action: () => navigate(HOME_URL) }];
        } else if (label === 'edit') {
            items = [{ label: 'Cut', disabled: true }, { label: 'Copy', disabled: true }, { label: 'Paste', disabled: true }];
        }

        if (items) {
            window.win98ShowContextMenuItems(rect.left, rect.bottom + 1, items);
            event.stopPropagation();
        }
    });

    function addCurrentToFavorites() {
        const url = frame.dataset.currentUrl;
        if (!url) return;
        const favs = loadFavorites().filter(fav => fav.url !== url);
        const title = windowEl.querySelector('.window-title').textContent.replace(' - Microsoft Internet Explorer', '').trim();
        favs.unshift({ url, title });
        saveFavorites(favs);
        setStatus('Added to Favorites');
    }

    function viewSource() {
        const doc = frame.contentDocument;
        const textarea = document.getElementById('notepad-textarea');
        if (!doc || !textarea) return;
        textarea.value = '<!-- ' + (frame.dataset.currentUrl || '') + ' -->\n' + doc.documentElement.outerHTML;
        window.win98ActivateWindow?.(document.getElementById('notepadWindow'));
    }

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.internetExplorer = function () {
        if (typeof window.win98ActivateWindow === 'function') window.win98ActivateWindow(windowEl);
    };

    // Start menu Favorites entries open IE at a given address.
    window.win98IENavigate = function (url) {
        window.win98AppOpenHooks.internetExplorer();
        navigate(url);
    };

    navigate(HOME_URL, { noHistory: false });
})();
