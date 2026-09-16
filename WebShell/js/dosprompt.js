    // MS-DOS Prompt over the virtual filesystem.
(function () {
    'use strict';

    const windowEl = document.getElementById('dosWindow');
    if (!windowEl) return;
    const outputEl = document.getElementById('dosOutput');
    const inputEl = document.getElementById('dosInput');
    const promptEl = document.getElementById('dosPromptLabel');

    // Path must match the virtual FS entry key exactly (case-sensitive).
    let cwd = 'C:/Windows';

    function fs() {
        return window.win98FsHelpers || null;
    }

    function displayPath(path) {
        return path.replace(/\//g, '\\').toUpperCase().replace(/\\$/, '') || 'C:';
    }

    function print(text) {
        outputEl.textContent += text + '\n';
        outputEl.scrollTop = outputEl.scrollHeight;
    }

    function updatePrompt() {
        promptEl.textContent = displayPath(cwd) + '>';
    }

    function resolve(input) {
        let target = String(input || '').trim().replace(/\\/g, '/');
        if (!target) return cwd;
        if (/^[a-z]:/i.test(target)) {
            // absolute
        } else if (target === '..') {
            const parts = cwd.split('/').filter(Boolean);
            if (parts.length > 1) parts.pop();
            target = parts.join('/');
            if (!target.includes('/')) target += '/';
            return target;
        } else if (target === '.') {
            return cwd;
        } else {
            target = cwd.replace(/\/$/, '') + '/' + target;
        }
        return target;
    }

    function findChildCaseInsensitive(parent, name) {
        const helpers = fs();
        if (!helpers) return null;
        return helpers.children(parent).find(c => c.name.toLowerCase() === String(name).toLowerCase()) || null;
    }

    const COMMANDS = {
        help: () => {
            print('DIR      Lists files and directories');
            print('CD       Changes directory (CD .. to go up)');
            print('TYPE     Displays the contents of a text file');
            print('CLS      Clears the screen');
            print('VER      Shows the Windows version');
            print('MEM      Shows memory usage');
            print('ECHO     Displays a message');
            print('DOOM     Opens the DOOM folder');
            print('EXIT     Closes the MS-DOS Prompt');
        },
        ver: () => print('\nWindows 98 [Version 4.10.1998]\n'),
        cls: () => { outputEl.textContent = ''; },
        mem: () => {
            print('    655360 bytes total conventional memory');
            print('    655360 bytes available to MS-DOS');
            print('  66060288 bytes total extended memory (XMS)');
            print('\n  DOOM requires 4 MB. You have enough. Rip and tear.');
        },
        time: () => print('Current time is ' + new Date().toLocaleTimeString('en-US')),
        date: () => print('Current date is ' + new Date().toDateString()),
        win: () => print('Windows is already running.'),
        doom: () => { window.win98OpenPath?.('C:/DOOM', ''); print('Opening C:\\DOOM...'); },
        exit: () => windowEl.querySelector('.close-btn')?.click()
    };

    function runCommand(raw) {
        const line = String(raw || '').trim();
        print(promptEl.textContent + line);
        if (!line) return;
        const [cmd, ...rest] = line.split(/\s+/);
        const arg = rest.join(' ');
        const lower = cmd.toLowerCase();

        if (COMMANDS[lower]) { COMMANDS[lower](arg); return; }
        if (lower === 'echo') { print(arg || 'ECHO is on.'); return; }

        const helpers = fs();
        if (!helpers) { print('File system not available.'); return; }

        if (lower === 'dir') {
            const entry = helpers.entry(cwd);
            if (!entry) { print('Invalid directory'); return; }
            print(' Directory of ' + displayPath(cwd) + '\n');
            const children = helpers.children(cwd);
            let files = 0;
            let dirs = 0;
            children.forEach(child => {
                const name = child.name.toUpperCase();
                if (child.type === 'folder' || child.type === 'drive') {
                    print(name.padEnd(20) + '<DIR>');
                    dirs++;
                } else {
                    print(name.padEnd(20) + String(child.size || 0).padStart(10));
                    files++;
                }
            });
            print('\n        ' + files + ' file(s)');
            print('        ' + dirs + ' dir(s)   2,147,483,647 bytes free');
            return;
        }

        if (lower === 'cd' || lower === 'chdir') {
            if (!arg) { print(displayPath(cwd)); return; }
            let target;
            if (arg === '..' || arg === '.' || /^[a-z]:/i.test(arg.replace(/\\/g, '/'))) {
                target = resolve(arg);
            } else {
                const child = findChildCaseInsensitive(cwd, arg);
                target = child ? child.path : resolve(arg);
            }
            const entry = helpers.entry(target);
            if (entry && (entry.type === 'folder' || entry.type === 'drive')) {
                cwd = target;
                updatePrompt();
            } else {
                print('Invalid directory');
            }
            return;
        }

        if (lower === 'type') {
            if (!arg) { print('Required parameter missing'); return; }
            const child = findChildCaseInsensitive(cwd, arg);
            const entry = child ? helpers.entry(child.path) : helpers.entry(resolve(arg));
            if (entry && entry.type === 'text') {
                print(entry.content || '');
            } else {
                print('File not found - ' + arg.toUpperCase());
            }
            return;
        }

        if (lower === 'format') {
            print('WARNING, ALL DATA ON NON-REMOVABLE DISK');
            print('DRIVE C: WILL BE LOST!');
            print('Proceed with Format (Y/N)? n');
            print('\nFormat cancelled. (Nice try.)');
            return;
        }

        // launching an EXE from the prompt
        const exe = findChildCaseInsensitive(cwd, lower.endsWith('.exe') ? arg || cmd : cmd + '.exe') ||
            findChildCaseInsensitive(cwd, cmd);
        if (exe && exe.type === 'app') {
            window.win98OpenPath?.(exe.path, '');
            return;
        }

        print("Bad command or file name - '" + cmd.toUpperCase() + "'. Type HELP.");
    }

    inputEl.addEventListener('keydown', event => {
        event.stopPropagation();
        if (event.key === 'Enter') {
            const value = inputEl.value;
            inputEl.value = '';
            runCommand(value);
        }
    });

    windowEl.addEventListener('click', () => inputEl.focus());

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.dosWindow = function () {
        window.win98ActivateWindow?.(windowEl);
        setTimeout(() => inputEl.focus(), 50);
    };

    print('Microsoft(R) Windows 98');
    print('   (C)Copyright Microsoft Corp 1981-1998.');
    print('');
    updatePrompt();
})();
