    // WordPad: rich(ish) text editing, saves plain text into My Documents.
(function () {
    'use strict';

    const windowEl = document.getElementById('wordpadWindow');
    if (!windowEl) return;
    const editor = document.getElementById('wordpadEditor');
    const statusEl = document.getElementById('wordpadStatus');

    windowEl.querySelectorAll('[data-wp-cmd]').forEach(btn => {
        btn.addEventListener('click', () => {
            editor.focus();
            document.execCommand(btn.dataset.wpCmd, false, null);
        });
    });

    document.getElementById('wordpadFontSize').addEventListener('change', event => {
        editor.focus();
        document.execCommand('fontSize', false, event.target.value);
    });

    document.getElementById('wordpadNewOption')?.addEventListener('click', () => {
        editor.innerHTML = '';
        statusEl.textContent = 'New document';
    });

    document.getElementById('wordpadSaveOption')?.addEventListener('click', () => {
        const name = 'Document-' + new Date().toISOString().slice(11, 19).replace(/:/g, '') + '.txt';
        const text = editor.innerText || '';
        if (window.win98SaveFileToFs?.('C:/My Documents', name, text)) {
            statusEl.textContent = 'Saved ' + name + ' to My Documents (formatting is a memory)';
        } else {
            statusEl.textContent = 'Save failed';
        }
    });

    editor.addEventListener('keydown', event => event.stopPropagation());

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.wordpadWindow = function () {
        window.win98ActivateWindow?.(windowEl);
        setTimeout(() => editor.focus(), 50);
    };
})();
