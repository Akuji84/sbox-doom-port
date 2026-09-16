    // Paint: pencil/brush/eraser/fill/line/rectangle/ellipse with the classic
    // two-swatch color box. Save writes a PNG data URL into the virtual FS
    // under C:/My Documents/My Pictures.
(function () {
    'use strict';

    const windowEl = document.getElementById('paintWindow');
    if (!windowEl) return;
    const canvas = document.getElementById('paintCanvas');
    const ctx = canvas.getContext('2d');
    const statusEl = document.getElementById('paintStatus');

    const PALETTE = [
        '#000000', '#808080', '#800000', '#808000', '#008000', '#008080', '#000080', '#800080',
        '#ffffff', '#c0c0c0', '#ff0000', '#ffff00', '#00ff00', '#00ffff', '#0000ff', '#ff00ff'
    ];

    let tool = 'pencil';
    let primary = '#000000';
    let drawing = false;
    let start = null;
    let snapshot = null;

    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.lineCap = 'round';

    // palette
    const paletteEl = document.getElementById('paintPalette');
    PALETTE.forEach(color => {
        const swatch = document.createElement('div');
        swatch.className = 'paint-swatch';
        swatch.style.backgroundColor = color;
        swatch.addEventListener('click', () => {
            primary = color;
            document.getElementById('paintPrimary').style.backgroundColor = color;
        });
        paletteEl.appendChild(swatch);
    });

    windowEl.querySelectorAll('.paint-tool').forEach(btn => {
        btn.addEventListener('click', () => {
            windowEl.querySelectorAll('.paint-tool').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            tool = btn.dataset.tool;
            statusEl.textContent = btn.title;
        });
    });

    function canvasPoint(event) {
        const rect = canvas.getBoundingClientRect();
        return {
            x: Math.round((event.clientX - rect.left) * (canvas.width / rect.width)),
            y: Math.round((event.clientY - rect.top) * (canvas.height / rect.height))
        };
    }

    function strokeStyleForTool() {
        ctx.strokeStyle = tool === 'eraser' ? '#ffffff' : primary;
        ctx.fillStyle = ctx.strokeStyle;
        ctx.lineWidth = tool === 'brush' ? 4 : tool === 'eraser' ? 10 : 1;
    }

    function floodFill(x, y, hexColor) {
        const image = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = image.data;
        const w = canvas.width;
        const idx = (px, py) => (py * w + px) * 4;
        const target = data.slice(idx(x, y), idx(x, y) + 4);
        const r = parseInt(hexColor.slice(1, 3), 16);
        const g = parseInt(hexColor.slice(3, 5), 16);
        const b = parseInt(hexColor.slice(5, 7), 16);
        if (target[0] === r && target[1] === g && target[2] === b) return;

        const stack = [[x, y]];
        while (stack.length) {
            const [cx, cy] = stack.pop();
            if (cx < 0 || cy < 0 || cx >= w || cy >= canvas.height) continue;
            const i = idx(cx, cy);
            if (data[i] !== target[0] || data[i + 1] !== target[1] || data[i + 2] !== target[2]) continue;
            data[i] = r; data[i + 1] = g; data[i + 2] = b; data[i + 3] = 255;
            stack.push([cx + 1, cy], [cx - 1, cy], [cx, cy + 1], [cx, cy - 1]);
        }
        ctx.putImageData(image, 0, 0);
    }

    canvas.addEventListener('mousedown', event => {
        const p = canvasPoint(event);
        if (tool === 'fill') {
            floodFill(p.x, p.y, primary);
            return;
        }
        drawing = true;
        start = p;
        strokeStyleForTool();
        if (tool === 'pencil' || tool === 'brush' || tool === 'eraser') {
            ctx.beginPath();
            ctx.moveTo(p.x, p.y);
        } else {
            snapshot = ctx.getImageData(0, 0, canvas.width, canvas.height);
        }
        event.preventDefault();
    });

    canvas.addEventListener('mousemove', event => {
        if (!drawing) return;
        const p = canvasPoint(event);
        if (tool === 'pencil' || tool === 'brush' || tool === 'eraser') {
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
            return;
        }
        ctx.putImageData(snapshot, 0, 0);
        strokeStyleForTool();
        if (tool === 'line') {
            ctx.beginPath();
            ctx.moveTo(start.x, start.y);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
        } else if (tool === 'rect') {
            ctx.strokeRect(start.x, start.y, p.x - start.x, p.y - start.y);
        } else if (tool === 'ellipse') {
            ctx.beginPath();
            ctx.ellipse((start.x + p.x) / 2, (start.y + p.y) / 2,
                Math.abs(p.x - start.x) / 2, Math.abs(p.y - start.y) / 2, 0, 0, Math.PI * 2);
            ctx.stroke();
        }
    });

    document.addEventListener('mouseup', () => {
        drawing = false;
        snapshot = null;
    });

    document.getElementById('paintNewOption')?.addEventListener('click', () => {
        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        statusEl.textContent = 'New image';
    });

    document.getElementById('paintWallpaperCenterOption')?.addEventListener('click', () => {
        window.win98SetCustomWallpaper?.(canvas.toDataURL('image/png'), 'center');
        statusEl.textContent = 'Wallpaper set (centered)';
    });

    document.getElementById('paintWallpaperTileOption')?.addEventListener('click', () => {
        window.win98SetCustomWallpaper?.(canvas.toDataURL('image/png'), 'tile');
        statusEl.textContent = 'Wallpaper set (tiled)';
    });

    document.getElementById('paintSaveOption')?.addEventListener('click', () => {
        // store the drawing as a text entry holding a data URL; Win98 would
        // say .bmp, our Notepad can at least show what it is
        const name = 'untitled-' + new Date().toISOString().slice(11, 19).replace(/:/g, '') + '.png';
        const dataUrl = canvas.toDataURL('image/png');
        if (typeof window.win98SaveFileToFs === 'function') {
            window.win98SaveFileToFs('C:/My Documents/My Pictures', name, dataUrl);
            statusEl.textContent = 'Saved ' + name + ' to My Pictures';
        } else {
            statusEl.textContent = 'Save is not available';
        }
    });

    window.win98AppOpenHooks = window.win98AppOpenHooks || {};
    window.win98AppOpenHooks.paintWindow = function () {
        if (typeof window.win98ActivateWindow === 'function') window.win98ActivateWindow(windowEl);
    };
})();
