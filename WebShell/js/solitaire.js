    // Klondike Solitaire: draw-1 stock, drag and drop between piles,
    // double-click to send a card to its foundation.
(function () {
    'use strict';

    const windowEl = document.getElementById('solitaireWindow');
    if (!windowEl) return;
    const tableEl = document.getElementById('solTable');

    const SUITS = ['♠', '♥', '♦', '♣'];
    const RANKS = ['A', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'J', 'Q', 'K'];

    let stock = [];
    let waste = [];
    let foundations = [[], [], [], []];
    let tableau = [[], [], [], [], [], [], []];
    let drag = null;
    let drawCount = 1;
    let backStyle = 'blue';

    function isRed(card) {
        return card.suit === '♥' || card.suit === '♦';
    }

    function deal() {
        const deck = [];
        SUITS.forEach(suit => {
            RANKS.forEach((rank, index) => {
                deck.push({ suit, rank, value: index + 1, faceUp: false });
            });
        });
        for (let i = deck.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [deck[i], deck[j]] = [deck[j], deck[i]];
        }

        stock = deck;
        waste = [];
        foundations = [[], [], [], []];
        tableau = [[], [], [], [], [], [], []];
        for (let col = 0; col < 7; col++) {
            for (let row = 0; row <= col; row++) {
                const card = stock.pop();
                card.faceUp = row === col;
                tableau[col].push(card);
            }
        }
        render();
    }

    function drawFromStock() {
        if (stock.length === 0) {
            // recycle the waste
            stock = waste.reverse().map(card => { card.faceUp = false; return card; });
            waste = [];
        } else {
            for (let i = 0; i < drawCount && stock.length; i++) {
                const card = stock.pop();
                card.faceUp = true;
                waste.push(card);
            }
        }
        if (typeof window.win98PlaySound === 'function') window.win98PlaySound('click');
        render();
    }

    function canDropOnTableau(card, pile) {
        if (pile.length === 0) return card.value === 13; // king on empty
        const top = pile[pile.length - 1];
        return top.faceUp && top.value === card.value + 1 && isRed(top) !== isRed(card);
    }

    function canDropOnFoundation(card, pile, cardsMoving) {
        if (cardsMoving > 1) return false;
        if (pile.length === 0) return card.value === 1;
        const top = pile[pile.length - 1];
        return top.suit === card.suit && top.value === card.value - 1;
    }

    function sourcePile(kind, index) {
        if (kind === 'waste') return waste;
        if (kind === 'foundation') return foundations[index];
        return tableau[index];
    }

    function afterMove(fromKind, fromIndex) {
        if (fromKind === 'tableau') {
            const pile = tableau[fromIndex];
            const top = pile[pile.length - 1];
            if (top && !top.faceUp) top.faceUp = true;
        }
        if (foundations.every(pile => pile.length === 13)) {
            if (typeof window.win98PlaySound === 'function') window.win98PlaySound('chord');
            setTimeout(() => {
                const again = window.confirm ? confirm('You win! Deal again?') : true;
                if (again) deal();
            }, 100);
        }
    }

    function tryAutoFoundation(kind, index) {
        const pile = sourcePile(kind, index);
        const card = pile[pile.length - 1];
        if (!card || !card.faceUp) return;
        for (let f = 0; f < 4; f++) {
            if (canDropOnFoundation(card, foundations[f], 1)) {
                pile.pop();
                foundations[f].push(card);
                afterMove(kind, index);
                render();
                return;
            }
        }
    }

    // ------------------------------------------------------------ rendering
    function cardEl(card, kind, pileIndex, cardIndex, offsetY) {
        const el = document.createElement('div');
        el.className = 'sol-card' + (card.faceUp ? (isRed(card) ? ' red' : ' black') : ' facedown sol-back-' + backStyle);
        el.style.top = offsetY + 'px';
        el.dataset.kind = kind;
        el.dataset.pile = pileIndex;
        el.dataset.card = cardIndex;
        if (card.faceUp) {
            el.innerHTML = '<div class="sol-corner">' + card.rank + '<br>' + card.suit + '</div>' +
                '<div class="sol-center">' + card.suit + '</div>';
        }
        return el;
    }

    function render() {
        tableEl.innerHTML = '';

        const top = document.createElement('div');
        top.className = 'sol-row';

        const stockEl = document.createElement('div');
        stockEl.className = 'sol-pile sol-stock';
        stockEl.dataset.kind = 'stock';
        if (stock.length) {
            const back = document.createElement('div');
            back.className = 'sol-card facedown';
            stockEl.appendChild(back);
        } else {
            stockEl.classList.add('sol-empty');
            stockEl.textContent = '⟳';
        }
        top.appendChild(stockEl);

        const wasteEl = document.createElement('div');
        wasteEl.className = 'sol-pile';
        wasteEl.dataset.kind = 'waste';
        wasteEl.dataset.pile = '0';
        const fan = waste.slice(-Math.min(drawCount, 3));
        fan.forEach((card, i) => {
            const el = cardEl(card, 'waste', 0, waste.length - fan.length + i, 0);
            el.style.left = (i * 14) + 'px';
            wasteEl.appendChild(el);
        });
        top.appendChild(wasteEl);

        const spacer = document.createElement('div');
        spacer.className = 'sol-spacer';
        top.appendChild(spacer);

        foundations.forEach((pile, index) => {
            const el = document.createElement('div');
            el.className = 'sol-pile sol-foundation';
            el.dataset.kind = 'foundation';
            el.dataset.pile = index;
            if (pile.length) {
                el.appendChild(cardEl(pile[pile.length - 1], 'foundation', index, pile.length - 1, 0));
            }
            top.appendChild(el);
        });

        tableEl.appendChild(top);

        const bottom = document.createElement('div');
        bottom.className = 'sol-row';
        tableau.forEach((pile, index) => {
            const el = document.createElement('div');
            el.className = 'sol-pile sol-tableau';
            el.dataset.kind = 'tableau';
            el.dataset.pile = index;
            let offset = 0;
            pile.forEach((card, cardIndex) => {
                el.appendChild(cardEl(card, 'tableau', index, cardIndex, offset));
                offset += card.faceUp ? 16 : 4;
            });
            el.style.minHeight = (offset + 84) + 'px';
            bottom.appendChild(el);
        });
        tableEl.appendChild(bottom);
    }

    // ------------------------------------------------------------- dragging
    tableEl.addEventListener('mousedown', event => {
        const pileEl = event.target.closest('.sol-pile');
        if (!pileEl) return;
        if (pileEl.dataset.kind === 'stock') {
            drawFromStock();
            return;
        }

        const el = event.target.closest('.sol-card');
        if (!el) return;
        const kind = el.dataset.kind;
        const pileIndex = parseInt(el.dataset.pile, 10);
        const cardIndex = parseInt(el.dataset.card, 10);
        const pile = sourcePile(kind, pileIndex);
        const card = pile[cardIndex];
        if (!card || !card.faceUp) return;
        if (kind === 'waste' && cardIndex !== pile.length - 1) return;

        // in a tableau you may pick up a valid run
        if (kind === 'tableau') {
            for (let i = cardIndex; i < pile.length - 1; i++) {
                const upper = pile[i];
                const lower = pile[i + 1];
                if (!(upper.value === lower.value + 1 && isRed(upper) !== isRed(lower))) return;
            }
        }

        const cards = pile.slice(cardIndex);
        const ghost = document.createElement('div');
        ghost.className = 'sol-ghost';
        let offset = 0;
        cards.forEach(c => {
            const g = cardEl(c, 'ghost', 0, 0, offset);
            ghost.appendChild(g);
            offset += 16;
        });
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

    document.addEventListener('mousemove', event => moveGhost(event));

    document.addEventListener('mouseup', event => {
        if (!drag) return;
        drag.ghost.remove();
        const dropEl = document.elementFromPoint(event.clientX, event.clientY);
        const pileEl = dropEl ? dropEl.closest('.sol-pile') : null;
        const state = drag;
        drag = null;

        if (!pileEl) { render(); return; }
        const targetKind = pileEl.dataset.kind;
        const targetIndex = parseInt(pileEl.dataset.pile || '0', 10);
        if (targetKind === state.kind && targetIndex === state.pileIndex) { render(); return; }

        const moving = state.cards[0];
        let accepted = false;
        if (targetKind === 'tableau' && canDropOnTableau(moving, tableau[targetIndex])) {
            accepted = true;
            const from = sourcePile(state.kind, state.pileIndex);
            from.splice(state.cardIndex, state.cards.length);
            tableau[targetIndex].push(...state.cards);
        } else if (targetKind === 'foundation' && canDropOnFoundation(moving, foundations[targetIndex], state.cards.length)) {
            accepted = true;
            const from = sourcePile(state.kind, state.pileIndex);
            from.splice(state.cardIndex, state.cards.length);
            foundations[targetIndex].push(moving);
        }

        if (accepted) {
            afterMove(state.kind, state.pileIndex);
            if (typeof window.win98PlaySound === 'function') window.win98PlaySound('click');
        }
        render();
    });

    tableEl.addEventListener('dblclick', event => {
        const el = event.target.closest('.sol-card');
        if (!el) return;
        const kind = el.dataset.kind;
        if (kind !== 'waste' && kind !== 'tableau') return;
        const pileIndex = parseInt(el.dataset.pile, 10);
        const pile = sourcePile(kind, pileIndex);
        if (parseInt(el.dataset.card, 10) !== pile.length - 1) return;
        tryAutoFoundation(kind, pileIndex);
    });

    document.getElementById('solDealOption')?.addEventListener('click', () => deal());
    document.getElementById('solDrawOneOption')?.addEventListener('click', () => { drawCount = 1; deal(); });
    document.getElementById('solDrawThreeOption')?.addEventListener('click', () => { drawCount = 3; deal(); });
    document.getElementById('solCardBackOption')?.addEventListener('click', () => {
        backStyle = backStyle === 'blue' ? 'red' : backStyle === 'red' ? 'green' : 'blue';
        render();
    });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.solitaireWindow = function () {
        if (typeof window.win98ActivateWindow === 'function') window.win98ActivateWindow(windowEl);
    };

    deal();
})();
