    // CD Player: WebAudio step sequencer playing original chiptune loops.
(function () {
    'use strict';

    const windowEl = document.getElementById('cdPlayerWindow');
    if (!windowEl) return;
    const lcdEl = document.getElementById('cdLcd');
    const trackListEl = document.getElementById('cdTrackList');

    // note numbers are semitones from A4 (440); null = rest
    const TRACKS = [
        {
            name: 'Rip and Tear (Tribute)', bpm: 180, type: 'square', level: 0.05,
            steps: [-29, -29, -17, -29, -29, -19, -29, -29, -21, -29, -29, -22, -24, -22, -21, -19]
        },
        {
            name: 'Teal Skies', bpm: 70, type: 'triangle', level: 0.07,
            steps: [-9, -5, -2, 3, -2, -5, -9, null, -10, -7, -2, 2, -2, -7, -10, null]
        },
        {
            name: 'Corridor of Cells', bpm: 132, type: 'sawtooth', level: 0.035,
            steps: [-21, -14, -9, -14, -21, -12, -9, -12, -21, -14, -9, -14, -19, -12, -7, -12]
        }
    ];

    let audioCtx = null;
    let currentTrack = 0;
    let playing = false;
    let stepIndex = 0;
    let stepTimer = null;
    let startedAt = 0;

    function ensureCtx() {
        if (!audioCtx) {
            const Ctx = window.AudioContext || window.webkitAudioContext;
            audioCtx = Ctx ? new Ctx() : null;
        }
        if (audioCtx && audioCtx.state === 'suspended') audioCtx.resume().catch(() => {});
        return audioCtx;
    }

    function shellLevel() {
        return typeof window.win98GetShellVolume === 'function' ? window.win98GetShellVolume() : 0.8;
    }

    function playStep() {
        const track = TRACKS[currentTrack];
        const note = track.steps[stepIndex % track.steps.length];
        stepIndex++;
        updateLcd();
        if (note === null) return;
        const ctx = ensureCtx();
        if (!ctx) return;
        const level = track.level * shellLevel();
        if (level <= 0) return;
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.type = track.type;
        osc.frequency.value = 440 * Math.pow(2, note / 12);
        const stepDur = 60 / track.bpm / 2;
        gain.gain.setValueAtTime(level, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + stepDur * 0.9);
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.start();
        osc.stop(ctx.currentTime + stepDur);
    }

    function updateLcd() {
        const elapsed = playing ? Math.floor((Date.now() - startedAt) / 1000) : 0;
        const mm = String(Math.floor(elapsed / 60)).padStart(2, '0');
        const ss = String(elapsed % 60).padStart(2, '0');
        lcdEl.textContent = '[' + String(currentTrack + 1).padStart(2, '0') + '] ' + mm + ':' + ss;
    }

    function renderTracks() {
        trackListEl.innerHTML = '';
        TRACKS.forEach((track, index) => {
            const row = document.createElement('div');
            row.className = 'cad-task' + (index === currentTrack ? ' selected' : '');
            row.textContent = String(index + 1).padStart(2, '0') + '. ' + track.name;
            row.addEventListener('dblclick', () => { selectTrack(index); play(); });
            row.addEventListener('click', () => selectTrack(index));
            trackListEl.appendChild(row);
        });
    }

    function selectTrack(index) {
        currentTrack = (index + TRACKS.length) % TRACKS.length;
        stepIndex = 0;
        renderTracks();
        updateLcd();
        if (playing) startedAt = Date.now();
    }

    function play() {
        stop(true);
        const track = TRACKS[currentTrack];
        playing = true;
        startedAt = Date.now();
        stepTimer = setInterval(playStep, 60000 / track.bpm / 2);
        updateLcd();
    }

    function stop(silent) {
        playing = false;
        if (stepTimer) clearInterval(stepTimer);
        stepTimer = null;
        stepIndex = 0;
        if (!silent) updateLcd();
    }

    document.getElementById('cd-play-btn').addEventListener('click', play);
    document.getElementById('cd-stop-btn').addEventListener('click', () => stop(false));
    document.getElementById('cd-prev-btn').addEventListener('click', () => { selectTrack(currentTrack - 1); if (playing) play(); });
    document.getElementById('cd-next-btn').addEventListener('click', () => { selectTrack(currentTrack + 1); if (playing) play(); });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.cdPlayerWindow = function () {
        renderTracks();
        updateLcd();
        window.win98ActivateWindow?.(windowEl);
    };

    // stop the music when the window closes
    windowEl.addEventListener('click', event => {
        if (event.target.closest('.close-btn')) stop(false);
    });

    renderTracks();
    updateLcd();
})();
