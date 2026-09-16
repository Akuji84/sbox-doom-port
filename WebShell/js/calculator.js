    // Calculator, Win98 standard mode.
(function () {
    'use strict';

    const windowEl = document.getElementById('calculatorWindow');
    if (!windowEl) return;
    const displayEl = document.getElementById('calcDisplay');

    let current = '0';
    let accumulator = null;
    let pendingOp = null;
    let justEvaluated = false;
    let memory = 0;

    function show() {
        displayEl.textContent = current.length > 16 ? Number(current).toExponential(8) : current;
    }

    function inputDigit(d) {
        if (justEvaluated || current === 'E') {
            current = '0';
            justEvaluated = false;
        }
        if (d === '.' && current.includes('.')) return;
        current = current === '0' && d !== '.' ? d : current + d;
        show();
    }

    function compute(a, b, op) {
        switch (op) {
            case '+': return a + b;
            case '-': return a - b;
            case '*': return a * b;
            case '/': return b === 0 ? NaN : a / b;
        }
        return b;
    }

    function formatResult(value) {
        if (!isFinite(value) || isNaN(value)) return 'E';
        return String(Math.round(value * 1e12) / 1e12);
    }

    function applyOp(op) {
        const value = parseFloat(current);
        if (pendingOp !== null && accumulator !== null && !justEvaluated) {
            accumulator = compute(accumulator, value, pendingOp);
            current = formatResult(accumulator);
        } else {
            accumulator = value;
        }
        pendingOp = op;
        justEvaluated = true;
        show();
    }

    function equals() {
        if (pendingOp === null || accumulator === null) return;
        current = formatResult(compute(accumulator, parseFloat(current), pendingOp));
        accumulator = null;
        pendingOp = null;
        justEvaluated = true;
        show();
    }

    const ACTIONS = {
        'C': () => { current = '0'; accumulator = null; pendingOp = null; justEvaluated = false; show(); },
        'CE': () => { current = '0'; show(); },
        'Back': () => { current = current.length > 1 ? current.slice(0, -1) : '0'; show(); },
        '=': equals,
        '+/-': () => { current = current.startsWith('-') ? current.slice(1) : (current !== '0' ? '-' + current : current); show(); },
        'sqrt': () => { current = formatResult(Math.sqrt(parseFloat(current))); justEvaluated = true; show(); },
        '%': () => { if (accumulator !== null) { current = formatResult(accumulator * parseFloat(current) / 100); show(); } },
        '1/x': () => { current = formatResult(1 / parseFloat(current)); justEvaluated = true; show(); },
        'MC': () => { memory = 0; },
        'MR': () => { current = formatResult(memory); justEvaluated = true; show(); },
        'MS': () => { memory = parseFloat(current) || 0; },
        'M+': () => { memory += parseFloat(current) || 0; }
    };

    windowEl.querySelectorAll('.calc-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            const key = btn.dataset.key;
            if (/^[0-9.]$/.test(key)) inputDigit(key);
            else if (['+', '-', '*', '/'].includes(key)) applyOp(key);
            else if (ACTIONS[key]) ACTIONS[key]();
            if (typeof window.win98PlaySound === 'function') window.win98PlaySound('click');
        });
    });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.calculatorWindow = function () {
        if (typeof window.win98ActivateWindow === 'function') window.win98ActivateWindow(windowEl);
    };

    show();
})();
