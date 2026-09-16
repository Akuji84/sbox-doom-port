    // Bug Report Wizard: posts through the dashboard to the bug server.
(function () {
    'use strict';

    const windowEl = document.getElementById('bugReportWindow');
    if (!windowEl) return;
    const channel = new URLSearchParams(window.location.search).get('channel') === 'beta' ? 'beta' : 'live';
    const statusEl = document.getElementById('bugWizardStatus');

    let step = 0;

    function showStep(next) {
        step = Math.max(0, Math.min(2, next));
        [0, 1, 2].forEach(i => {
            document.getElementById('bugStep' + i).style.display = i === step ? 'block' : 'none';
        });
        document.getElementById('bug-back-btn').classList.toggle('btn-disabled', step === 0);
        document.getElementById('bug-next-btn').textContent = step === 1 ? 'Send' : step === 2 ? 'Finish' : 'Next >';
    }

    async function send() {
        const details = document.getElementById('bugDetails').value.trim();
        if (!details) {
            statusEl.textContent = 'Please describe what happened first.';
            window.win98PlaySound?.('error');
            showStep(1);
            return false;
        }
        statusEl.textContent = 'Sending report...';
        try {
            const response = await fetch('/api/win98-shell/' + channel + '/bug', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    contact: document.getElementById('bugContact').value.trim(),
                    details: details,
                    map: 'win98-shell',
                    build: 'shell-' + channel
                })
            });
            if (!response.ok) throw new Error('bad status');
            statusEl.textContent = 'Report sent. Thank you, marine.';
            return true;
        } catch (err) {
            statusEl.textContent = 'Could not reach the bug server. Try again later.';
            window.win98PlaySound?.('error');
            return false;
        }
    }

    document.getElementById('bug-next-btn').addEventListener('click', async () => {
        if (step === 0) { showStep(1); return; }
        if (step === 1) {
            if (await send()) showStep(2);
            return;
        }
        windowEl.querySelector('.close-btn')?.click();
    });

    document.getElementById('bug-back-btn').addEventListener('click', () => showStep(step - 1));
    document.getElementById('bug-cancel-btn').addEventListener('click', () => windowEl.querySelector('.close-btn')?.click());

    ['bugContact', 'bugDetails'].forEach(id => {
        document.getElementById(id).addEventListener('keydown', e => e.stopPropagation());
    });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.bugReportWindow = function () {
        showStep(0);
        statusEl.textContent = '';
        window.win98ActivateWindow?.(windowEl);
    };

    document.getElementById('menuBugReport')?.addEventListener('click', () => window.win98AppOpenHooks.bugReportWindow());

    showStep(0);
})();
