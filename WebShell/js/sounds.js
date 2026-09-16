    // Sound scheme: WebAudio-synthesized UI sounds, no audio files.
    // window.win98PlaySound(name) with names: click, error, startup,
    // shutdown, chord. Honors the tray volume/mute via win98GetShellVolume.
(function () {
    'use strict';

    let audioCtx = null;

    function getContext() {
        if (audioCtx) return audioCtx;
        try {
            const Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return null;
            audioCtx = new Ctx();
        } catch (err) {
            return null;
        }
        return audioCtx;
    }

    function masterLevel() {
        const shell = typeof window.win98GetShellVolume === 'function' ? window.win98GetShellVolume() : 0.8;
        return Math.max(0, Math.min(1, shell));
    }

    // Play a set of tones: [{freq, at, dur, level, type}]
    function playTones(tones, overallLevel) {
        const ctx = getContext();
        if (!ctx) return;
        if (ctx.state === 'suspended') {
            ctx.resume().catch(() => {});
            if (ctx.state === 'suspended') return; // autoplay blocked, stay quiet
        }
        const gainScale = masterLevel() * (overallLevel || 1);
        if (gainScale <= 0) return;
        const now = ctx.currentTime;
        tones.forEach(tone => {
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = tone.type || 'sine';
            osc.frequency.value = tone.freq;
            const start = now + (tone.at || 0);
            const dur = tone.dur || 0.2;
            const peak = (tone.level || 0.1) * gainScale;
            gain.gain.setValueAtTime(0, start);
            gain.gain.linearRampToValueAtTime(peak, start + Math.min(0.02, dur / 4));
            gain.gain.exponentialRampToValueAtTime(0.0001, start + dur);
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start(start);
            osc.stop(start + dur + 0.05);
        });
    }

    const SOUNDS = {
        // soft UI tick (menus, IE navigation)
        click: () => playTones([
            { freq: 1400, dur: 0.03, level: 0.06, type: 'square' }
        ]),
        // the error ding
        error: () => playTones([
            { freq: 830, dur: 0.35, level: 0.12 },
            { freq: 620, at: 0.0, dur: 0.35, level: 0.10 }
        ]),
        // "chord" - question/exclamation
        chord: () => playTones([
            { freq: 523, dur: 0.3, level: 0.09 },
            { freq: 659, dur: 0.3, level: 0.09 },
            { freq: 784, dur: 0.3, level: 0.09 }
        ]),
        // rising welcome swell
        startup: () => playTones([
            { freq: 164.81, dur: 2.8, level: 0.05 },
            { freq: 246.94, at: 0.1, dur: 2.7, level: 0.05 },
            { freq: 329.63, at: 0.2, dur: 2.6, level: 0.05 },
            { freq: 415.30, at: 0.3, dur: 2.5, level: 0.05 }
        ], 1),
        // descending farewell
        shutdown: () => playTones([
            { freq: 415.30, dur: 1.6, level: 0.05 },
            { freq: 329.63, at: 0.15, dur: 1.5, level: 0.05 },
            { freq: 246.94, at: 0.3, dur: 1.4, level: 0.05 },
            { freq: 164.81, at: 0.45, dur: 1.4, level: 0.05 }
        ], 1),
        // minimize/restore blip
        minimize: () => playTones([
            { freq: 500, dur: 0.05, level: 0.05, type: 'triangle' },
            { freq: 320, at: 0.05, dur: 0.06, level: 0.05, type: 'triangle' }
        ]),
        maximize: () => playTones([
            { freq: 320, dur: 0.05, level: 0.05, type: 'triangle' },
            { freq: 500, at: 0.05, dur: 0.06, level: 0.05, type: 'triangle' }
        ])
    };

    window.win98PlaySound = function (name) {
        const play = SOUNDS[name];
        if (play) {
            try { play(); } catch (err) { /* never let audio break the shell */ }
        }
    };

    // Default scheme hooks that need no cooperation from other modules.
    document.addEventListener('click', event => {
        if (event.target.closest('.window-control')) {
            const control = event.target.closest('.window-control');
            if (control.classList.contains('minimize-btn')) window.win98PlaySound('minimize');
            else if (control.classList.contains('maximize-btn')) window.win98PlaySound('maximize');
            else window.win98PlaySound('click');
            return;
        }
        if (event.target.closest('#startBtn')) {
            window.win98PlaySound('click');
        }
    }, true);
})();
