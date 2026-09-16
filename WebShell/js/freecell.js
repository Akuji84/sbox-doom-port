    // FreeCell: 8 cascades, 4 free cells, 4 foundations. Runs move when
    // enough free cells/empty cascades allow it, like the real game.
(function () {
    'use strict';

    const windowEl = document.getElementById('freecellWindow');
    if (!windowEl) return;
    const tableEl = document.getElementById('fcTable');

    const SUITS = ['♠', '♥', '♦', '♣'];
    const RANKS = ['A', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K'];

    let cascades = [];
    let freeCells = [null, null, null, null];
    let foundations = [[], [], [], []];
    let drag = null;

    function isRed(card) {
        return card.suit === '♥' || card.suit === '♦';
    }

    function deal() {
        const deck = [];
        SUITS.forEach(suit => RANKS.forEach((rank, i) => deck.push({ suit, rank, value: i + 1, faceUp: true })));
        for (let i = deck.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [deck[i], deck[j]] = [deck[j], deck[i]];
        }
        cascades = [[], [], [], [], [], [], [], []];
        freeCells = [null, null, null, null];
        foundations = [[], [], [], []];
        deck.forEach((card, i) => cascades[i % 8].push(card));
        render();
    }

    function maxRun() {
        const emptyFree = freeCells.filter(c => c === null).length;
        const emptyCols = cascades.filter(c => c.length === 0).length;
        return (emptyFree + 1) * Math.pow(2, emptyCols);
    }

    function isRun(cards) {
        for (let i = 0; i < cards.length - 1; i++) {
            if (!(cards[i].value === cards[i + 1].value + 1 && isRed(cards[i]) !== isRed(cards[i + 1]))) return false;
        }
        return true;
    }

    function checkWin() {
        if (foundations.every(pile => pile.length === 13)) {
            window.win98PlaySound?.('chord');
            setTimeout(() => { if (confirm('You win! Deal again?')) deal(); }, 100);
        }
    }

    function cardEl(card, kind, pileIndex, cardIndex, offsetY) {
        const el = document.createElement('div');
        el.className = 'sol-card ' + (isRed(card) ? 'red' : 'black');
        el.style.top = offsetY + 'px';
        el.dataset.kind = kind;
        el.dataset.pile = pileIndex;
        el.dataset.card = cardIndex;
        el.innerHTML = '<div class="sol-corner">' + card.rank + '<br>' + card.suit + '</div>' +
            '<div class="sol-center">' + card.suit + '</div>';
        return el;
    }

    function render() {
        tableEl.innerHTML = '';
        const top = document.createElement('div');
        top.className = 'sol-row';
        freeCells.forEach((card, index) => {
            const el = document.createElement('div');
            el.className = 'sol-pile sol-foundation';
            el.dataset.kind = 'free';
            el.dataset.pile = index;
            if (card) el.appendChild(cardEl(card, 'free', index, 0, 0));
            top.appendChild(el);
        });
        const spacer = document.createElement('div');
        spacer.className = 'sol-spacer';
        spacer.style.width = '20px';
        top.appendChild(spacer);
        foundations.forEach((pile, index) => {
            const el = document.createElement('div');
            el.className = 'sol-pile sol-foundation';
            el.dataset.kind = 'foundation';
            el.dataset.pile = index;
            if (pile.length) el.appendChild(cardEl(pile[pile.length - 1], 'foundation', index, pile.length - 1, 0));
            top.appendChild(el);
        });
        tableEl.appendChild(top);

        const bottom = document.createElement('div');
        bottom.className = 'sol-row';
        cascades.forEach((pile, index) => {
            const el = document.createElement('div');
            el.className = 'sol-pile sol-tableau';
            el.dataset.kind = 'cascade';
            el.dataset.pile = index;
            pile.forEach((card, cardIndex) => {
                el.appendChild(cardEl(card, 'cascade', index, cardIndex, cardIndex * 17));
            });
            el.style.minHeight = (pile.length * 17 + 84) + 'px';
            bottom.appendChild(el);
        });
        tableEl.appendChild(bottom);
    }

    tableEl.addEventListener('mousedown', event => {
        const el = event.target.closest('.sol-card');
        if (!el) return;
        const kind = el.dataset.kind;
        const pileIndex = parseInt(el.dataset.pile, 10);
        const cardIndex = parseInt(el.dataset.card, 10);
        let cards;
        if (kind === 'free') {
            cards = [freeCells[pileIndex]];
        } else if (kind === 'cascade') {
            cards = cascades[pileIndex].slice(cardIndex);
            if (!isRun(cards) || cards.length > maxRun()) return;
        } else {
            return;
        }

        const ghost = document.createElement('div');
        ghost.className = 'sol-ghost';
        cards.forEach((c, i) => ghost.appendChild(cardEl(c, 'ghost', 0, 0, i * 17)));
        document.body.appendChild(ghost);
        drag = { cards, kind, pileIndex, cardIndex, ghost };
        moveGhost(event);
        event.preventDefault();
    });

    function moveGhost(event) {
        if (!drag) return;
        drag.ghost.style.left = (event.clientX - 30) + 'px';
        drag.ghost.style.top = (event.clientY - 12) + 'px';
    }

    document.addEventListener('mousemove', moveGhost);

    document.addEventListener('mouseup', event => {
        if (!drag) return;
        drag.ghost.remove();
        const state = drag;
        drag = null;
        const dropEl = document.elementFromPoint(event.clientX, event.clientY);
        const pileEl = dropEl ? dropEl.closest('#fcTable .sol-pile') : null;
        if (!pileEl) { render(); return; }
        const targetKind = pileEl.dataset.kind;
        const targetIndex = parseInt(pileEl.dataset.pile, 10);
        const moving = state.cards[0];
        let accepted = false;

        function take() {
            if (state.kind === 'free') freeCells[state.pileIndex] = null;
            else cascades[state.pileIndex].splice(state.cardIndex, state.cards.length);
        }

        if (targetKind === 'free' && state.cards.length === 1 && freeCells[targetIndex] === null) {
            take();
            freeCells[targetIndex] = moving;
            accepted = true;
        } else if (targetKind === 'foundation' && state.cards.length === 1) {
            const pile = foundations[targetIndex];
            const ok = pile.length === 0 ? moving.value === 1
                : pile[pile.length - 1].suit === moving.suit && pile[pile.length - 1].value === moving.value - 1;
            if (ok) {
                take();
                pile.push(moving);
                accepted = true;
                checkWin();
            }
        } else if (targetKind === 'cascade') {
            const pile = cascades[targetIndex];
            const ok = pile.length === 0
                ? state.cards.length <= maxRun()
                : pile[pile.length - 1].value === moving.value + 1 && isRed(pile[pile.length - 1]) !== isRed(moving);
            if (ok) {
                take();
                pile.push(...state.cards);
                accepted = true;
            }
        }

        if (accepted) window.win98PlaySound?.('click');
        render();
    });

    tableEl.addEventListener('dblclick', event => {
        const el = event.target.closest('.sol-card');
        if (!el) return;
        const kind = el.dataset.kind;
        const pileIndex = parseInt(el.dataset.pile, 10);
        let card = null;
        if (kind === 'free') card = freeCells[pileIndex];
        else if (kind === 'cascade' && parseInt(el.dataset.card, 10) === cascades[pileIndex].length - 1) {
            card = cascades[pileIndex][cascades[pileIndex].length - 1];
        }
        if (!card) return;
        for (let f = 0; f < 4; f++) {
            const pile = foundations[f];
            const ok = pile.length === 0 ? card.value === 1
                : pile[pile.length - 1].suit === card.suit && pile[pile.length - 1].value === card.value - 1;
            if (ok) {
                if (kind === 'free') freeCells[pileIndex] = null;
                else cascades[pileIndex].pop();
                pile.push(card);
                checkWin();
                render();
                return;
            }
        }
        // otherwise try a free cell
        const slot = freeCells.indexOf(null);
        if (slot >= 0 && kind === 'cascade') {
            freeCells[slot] = card;
            cascades[pileIndex].pop();
            render();
        }
    });

    document.getElementById('fcDealOption')?.addEventListener('click', () => deal());

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.freecellWindow = function () {
        window.win98ActivateWindow?.(windowEl);
    };

    deal();
})();
