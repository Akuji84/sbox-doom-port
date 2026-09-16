    // Minesweeper. Classic rules: first click is always safe, right-click
    // flags, smiley restarts, three difficulties from the Game menu.
(function () {
    'use strict';

    const LEVELS = {
        beginner: { cols: 9, rows: 9, mines: 10 },
        intermediate: { cols: 16, rows: 16, mines: 40 },
        expert: { cols: 30, rows: 16, mines: 99 }
    };
    const NUMBER_COLORS = ['', '#0000ff', '#008000', '#ff0000', '#000080', '#800000', '#008080', '#000000', '#808080'];

    const windowEl = document.getElementById('minesweeperWindow');
    if (!windowEl) return;
    const gridEl = document.getElementById('mineGrid');
    const mineCountEl = document.getElementById('mineCount');
    const mineTimerEl = document.getElementById('mineTimer');
    const smileyEl = document.getElementById('mineSmiley');

    let level = LEVELS.beginner;
    let levelId = 'beginner';
    let board = [];
    let started = false;
    let gameOver = false;
    let flags = 0;
    let revealedCount = 0;
    let timer = 0;
    let timerHandle = null;

    function pad3(value) {
        const clamped = Math.max(-99, Math.min(999, value));
        if (clamped < 0) return '-' + String(Math.abs(clamped)).padStart(2, '0');
        return String(clamped).padStart(3, '0');
    }

    function setSmiley(face) {
        smileyEl.textContent = face;
    }

    function stopTimer() {
        if (timerHandle) clearInterval(timerHandle);
        timerHandle = null;
    }

    function newGame(newLevel) {
        if (newLevel) level = newLevel;
        stopTimer();
        board = [];
        started = false;
        gameOver = false;
        flags = 0;
        revealedCount = 0;
        timer = 0;
        mineTimerEl.textContent = pad3(0);
        mineCountEl.textContent = pad3(level.mines);
        setSmiley('🙂');

        for (let y = 0; y < level.rows; y++) {
            const row = [];
            for (let x = 0; x < level.cols; x++) {
                row.push({ mine: false, revealed: false, flagged: false, count: 0 });
            }
            board.push(row);
        }
        renderGrid(true);
    }

    function placeMines(safeX, safeY) {
        let placed = 0;
        while (placed < level.mines) {
            const x = Math.floor(Math.random() * level.cols);
            const y = Math.floor(Math.random() * level.rows);
            const cell = board[y][x];
            if (cell.mine) continue;
            if (Math.abs(x - safeX) <= 1 && Math.abs(y - safeY) <= 1) continue;
            cell.mine = true;
            placed++;
        }
        forEachCell((cell, x, y) => {
            let count = 0;
            forEachNeighbor(x, y, n => { if (n.mine) count++; });
            cell.count = count;
        });
    }

    function forEachCell(fn) {
        for (let y = 0; y < level.rows; y++) {
            for (let x = 0; x < level.cols; x++) {
                fn(board[y][x], x, y);
            }
        }
    }

    function forEachNeighbor(x, y, fn) {
        for (let dy = -1; dy <= 1; dy++) {
            for (let dx = -1; dx <= 1; dx++) {
                if (dx === 0 && dy === 0) continue;
                const nx = x + dx;
                const ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < level.cols && ny < level.rows) {
                    fn(board[ny][nx], nx, ny);
                }
            }
        }
    }

    function reveal(x, y) {
        const cell = board[y][x];
        if (cell.revealed || cell.flagged || gameOver) return;

        if (!started) {
            started = true;
            placeMines(x, y);
            timerHandle = setInterval(() => {
                timer = Math.min(999, timer + 1);
                mineTimerEl.textContent = pad3(timer);
            }, 1000);
        }

        if (cell.mine) {
            cell.revealed = true;
            cell.exploded = true;
            lose();
            return;
        }

        const stack = [[x, y]];
        while (stack.length) {
            const [cx, cy] = stack.pop();
            const current = board[cy][cx];
            if (current.revealed || current.flagged) continue;
            current.revealed = true;
            revealedCount++;
            if (current.count === 0) {
                forEachNeighbor(cx, cy, (n, nx, ny) => {
                    if (!n.revealed && !n.mine) stack.push([nx, ny]);
                });
            }
        }

        if (revealedCount === level.cols * level.rows - level.mines) {
            win();
        }
        renderGrid();
    }

    function toggleFlag(x, y) {
        const cell = board[y][x];
        if (cell.revealed || gameOver) return;
        cell.flagged = !cell.flagged;
        flags += cell.flagged ? 1 : -1;
        mineCountEl.textContent = pad3(level.mines - flags);
        renderGrid();
    }

    function lose() {
        gameOver = true;
        stopTimer();
        setSmiley('😵');
        forEachCell(cell => { if (cell.mine) cell.revealed = true; });
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('error');
        renderGrid();
    }

    function win() {
        gameOver = true;
        stopTimer();
        setSmiley('😎');
        forEachCell(cell => { if (cell.mine && !cell.flagged) { cell.flagged = true; flags++; } });
        mineCountEl.textContent = pad3(level.mines - flags);
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('chord');
        try {
            const key = 'win98ge.mineBest.' + levelId;
            const best = parseInt(localStorage.getItem(key) || '999', 10);
            if (timer < best) localStorage.setItem(key, String(timer));
        } catch (err) {}
    }

    function showBestTimes() {
        const lines = ['beginner', 'intermediate', 'expert'].map(id => {
            let best = '999';
            try { best = localStorage.getItem('win98ge.mineBest.' + id) || '999'; } catch (err) {}
            return id.charAt(0).toUpperCase() + id.slice(1) + ': ' + best + ' seconds';
        });
        document.getElementById('mineBestText').textContent = lines.join('\n');
        document.getElementById('overlay').style.display = 'block';
        document.getElementById('mine-best-dialog').style.display = 'block';
    }

    document.getElementById('mineBestOption')?.addEventListener('click', showBestTimes);
    document.getElementById('mine-best-ok-btn')?.addEventListener('click', () => {
        document.getElementById('overlay').style.display = 'none';
        document.getElementById('mine-best-dialog').style.display = 'none';
    });

    function renderGrid(rebuild) {
        if (rebuild || gridEl.children.length !== level.cols * level.rows) {
            gridEl.innerHTML = '';
            gridEl.style.gridTemplateColumns = `repeat(${level.cols}, 16px)`;
            for (let y = 0; y < level.rows; y++) {
                for (let x = 0; x < level.cols; x++) {
                    const cellEl = document.createElement('div');
                    cellEl.className = 'mine-cell';
                    cellEl.dataset.x = x;
                    cellEl.dataset.y = y;
                    gridEl.appendChild(cellEl);
                }
            }
            // resize the window to fit the board
            windowEl.style.width = (level.cols * 16 + 40) + 'px';
            windowEl.style.height = (level.rows * 16 + 132) + 'px';
        }

        Array.from(gridEl.children).forEach(cellEl => {
            const x = parseInt(cellEl.dataset.x, 10);
            const y = parseInt(cellEl.dataset.y, 10);
            const cell = board[y][x];
            cellEl.className = 'mine-cell';
            cellEl.textContent = '';
            cellEl.style.color = '';
            if (cell.revealed) {
                cellEl.classList.add('revealed');
                if (cell.mine) {
                    cellEl.textContent = '✹';
                    if (cell.exploded) cellEl.classList.add('exploded');
                } else if (cell.count > 0) {
                    cellEl.textContent = cell.count;
                    cellEl.style.color = NUMBER_COLORS[cell.count];
                }
            } else if (cell.flagged) {
                cellEl.textContent = '⚑';
                cellEl.classList.add('flagged');
            }
        });
    }

    gridEl.addEventListener('click', event => {
        const cellEl = event.target.closest('.mine-cell');
        if (!cellEl) return;
        reveal(parseInt(cellEl.dataset.x, 10), parseInt(cellEl.dataset.y, 10));
    });

    gridEl.addEventListener('contextmenu', event => {
        event.preventDefault();
        event.stopPropagation();
        const cellEl = event.target.closest('.mine-cell');
        if (!cellEl) return;
        toggleFlag(parseInt(cellEl.dataset.x, 10), parseInt(cellEl.dataset.y, 10));
    });

    gridEl.addEventListener('mousedown', () => {
        if (!gameOver) setSmiley('😮');
    });
    document.addEventListener('mouseup', () => {
        if (!gameOver) setSmiley('🙂');
    });

    smileyEl.addEventListener('click', () => newGame());

    document.querySelectorAll('[data-mine-level]').forEach(item => {
        item.addEventListener('click', event => {
            levelId = item.dataset.mineLevel in LEVELS ? item.dataset.mineLevel : 'beginner';
            newGame(LEVELS[levelId]);
            event.stopPropagation();
        });
    });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.minesweeperWindow = function () {
        if (windowEl.style.display === 'none' || !board.length) newGame();
        if (typeof window.win98ActivateWindow === 'function') window.win98ActivateWindow(windowEl);
    };

    newGame();
})();
