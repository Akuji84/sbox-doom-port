    // Notepad app: menus, find/goto/save-as dialogs, editing helpers.
    // Notepad functionality
    const notepadMenuItems = document.querySelectorAll('.notepad-menu-item');
    const textarea = document.getElementById('notepad-textarea');
    const cursorPosition = document.getElementById('cursor-position');
    const overlay = document.getElementById('overlay');

    // Dialogs
    const findDialog = document.getElementById('find-dialog');
    const gotoDialog = document.getElementById('goto-dialog');
    const saveasDialog = document.getElementById('saveas-dialog');
    const aboutDialog = document.getElementById('about-dialog');

    // Global state
    let isWordWrap = false;
    let currentFindText = '';
    let lastSearchPosition = 0;
    let textContent = '';

    // Handle menu item clicks
    notepadMenuItems.forEach(menuItem => {
        menuItem.addEventListener('click', function() {
            // Close any open menus
            document.querySelectorAll('.notepad-menu-item.active').forEach(item => {
                if (item !== this) {
                    item.classList.remove('active');
                }
            });

            // Toggle active state
            this.classList.toggle('active');
        });
    });

    // Close menus when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.notepad-menu-item')) {
            notepadMenuItems.forEach(item => {
                item.classList.remove('active');
            });
        }
    });

    // Track cursor position
    if (textarea) {
        textarea.addEventListener('click', updateCursorPosition);
        textarea.addEventListener('keyup', updateCursorPosition);
    }

    function updateCursorPosition() {
        if (!textarea) return;

        const cursorIndex = textarea.selectionStart;
        const textBeforeCursor = textarea.value.substring(0, cursorIndex);
        const lines = textBeforeCursor.split('\n');
        const currentLine = lines.length;
        const currentColumn = lines[lines.length - 1].length + 1;

        cursorPosition.textContent = `Ln ${currentLine}, Col ${currentColumn}`;
    }

    // Word Wrap
    document.getElementById('wordwrap-option')?.addEventListener('click', function() {
        isWordWrap = !isWordWrap;
        textarea.style.whiteSpace = isWordWrap ? 'normal' : 'pre';
        this.style.fontWeight = isWordWrap ? 'bold' : 'normal';
    });

    // Find functionality
    document.getElementById('find-option')?.addEventListener('click', function() {
        overlay.style.display = 'block';
        findDialog.style.display = 'block';
        document.getElementById('find-input').value = getSelectedText();
        document.getElementById('find-input').focus();
    });

    document.getElementById('find-cancel-btn')?.addEventListener('click', function() {
        findDialog.style.display = 'none';
        overlay.style.display = 'none';
    });

    document.getElementById('find-next-btn')?.addEventListener('click', function() {
        findNext();
    });

    document.getElementById('findnext-option')?.addEventListener('click', function() {
        if (currentFindText) {
            findNext();
        } else {
            document.getElementById('find-option').click();
        }
    });

    function findNext() {
        const findText = document.getElementById('find-input').value;
        const matchCase = document.getElementById('match-case').checked;
        const searchUp = document.getElementById('search-up').checked;

        if (!findText) return;

        currentFindText = findText;

        const text = textarea.value;
        let start = textarea.selectionStart;

        // If we're searching up, start from the selection start
        // If we're searching down, start from the selection end
        if (searchUp) {
            start = searchUp ? start - 1 : textarea.selectionEnd;
        } else {
            start = textarea.selectionEnd;
        }

        // Handle wrapping around
        if (start < 0) start = text.length;
        if (start > text.length) start = 0;

        let searchText = text;
        let searchTerm = findText;

        // Case insensitive search
        if (!matchCase) {
            searchText = text.toLowerCase();
            searchTerm = findText.toLowerCase();
        }

        let foundIndex;

        if (searchUp) {
            // Search backwards from the current position
            foundIndex = searchText.lastIndexOf(searchTerm, start);

            // If not found, wrap around to the end of the text
            if (foundIndex === -1) {
                foundIndex = searchText.lastIndexOf(searchTerm);
            }
        } else {
            // Search forwards from the current position
            foundIndex = searchText.indexOf(searchTerm, start);

            // If not found, wrap around to the beginning of the text
            if (foundIndex === -1) {
                foundIndex = searchText.indexOf(searchTerm);
            }
        }

        if (foundIndex !== -1) {
            textarea.focus();
            textarea.setSelectionRange(foundIndex, foundIndex + searchTerm.length);
            lastSearchPosition = foundIndex;

            // Make sure the found text is visible
            textarea.blur();
            textarea.focus();
        } else {
            alert(`Cannot find "${findText}"`);
        }
    }

    // TODO: Implement Replace functionality
    // Similar to Find but with replace capabilities

    // TODO: Implement Print functionality
    // Should display a print dialog and handle printing operations

    // Go To Line functionality
    document.getElementById('goto-option')?.addEventListener('click', function() {
        overlay.style.display = 'block';
        gotoDialog.style.display = 'block';
        document.getElementById('goto-input').value = '';
        document.getElementById('goto-input').focus();
    });

    document.getElementById('goto-cancel-btn')?.addEventListener('click', function() {
        gotoDialog.style.display = 'none';
        overlay.style.display = 'none';
    });

    document.getElementById('goto-btn')?.addEventListener('click', function() {
        const lineNumber = parseInt(document.getElementById('goto-input').value);
        gotoLine(lineNumber);
        gotoDialog.style.display = 'none';
        overlay.style.display = 'none';
    });

    function gotoLine(lineNumber) {
        if (isNaN(lineNumber) || lineNumber < 1) {
            alert('Line number out of range');
            return;
        }

        const lines = textarea.value.split('\n');

        if (lineNumber > lines.length) {
            alert('Line number out of range');
            return;
        }

        let position = 0;
        for (let i = 0; i < lineNumber - 1; i++) {
            position += lines[i].length + 1; // +1 for the newline character
        }

        textarea.focus();
        textarea.setSelectionRange(position, position);

        // Make sure the cursor is visible
        textarea.blur();
        textarea.focus();
    }

    // Save As functionality
    document.getElementById('saveas-option')?.addEventListener('click', function() {
        overlay.style.display = 'block';
        saveasDialog.style.display = 'block';
    });

    document.getElementById('saveas-cancel-btn')?.addEventListener('click', function() {
        saveasDialog.style.display = 'none';
        overlay.style.display = 'none';
    });

    document.getElementById('save-btn')?.addEventListener('click', function() {
        const filename = document.getElementById('filename-input').value;
        if (filename.trim() === '' || filename === '*.txt') {
            alert('Please enter a filename');
            return;
        }

        // In a real application, we would save the file here
        // For this demo, we'll just update the window title and close the dialog
        updateWindowTitle(filename);
        saveasDialog.style.display = 'none';
        overlay.style.display = 'none';
    });

    // TODO: Implement File Open functionality
    // Should display a dialog and allow selecting a file to open

    // TODO: Implement drag and drop functionality
    // Windows 98 allows drag and drop operations between windows

    // About dialog
    document.getElementById('about-option')?.addEventListener('click', function() {
        overlay.style.display = 'block';
        aboutDialog.style.display = 'block';
    });

    document.getElementById('about-ok-btn')?.addEventListener('click', function() {
        aboutDialog.style.display = 'none';
        overlay.style.display = 'none';
    });

    // Edit menu functions
    document.getElementById('selectall-option')?.addEventListener('click', function() {
        textarea.select();
    });

    document.getElementById('timedate-option')?.addEventListener('click', function() {
        const now = new Date();
        const timeStr = now.toLocaleTimeString();
        const dateStr = now.toLocaleDateString();
        insertTextAtCursor(`${timeStr} ${dateStr}`);
    });

    document.getElementById('cut-option')?.addEventListener('click', function() {
        document.execCommand('cut');
    });

    document.getElementById('copy-option')?.addEventListener('click', function() {
        document.execCommand('copy');
    });

    document.getElementById('paste-option')?.addEventListener('click', function() {
        document.execCommand('paste');
    });

    document.getElementById('delete-option')?.addEventListener('click', function() {
        deleteSelectedText();
    });

    // File menu functions
    document.getElementById('new-option')?.addEventListener('click', function() {
        if (textarea.value.trim() !== '' && !confirm('Do you want to save changes?')) {
            return;
        }

        textarea.value = '';
        updateWindowTitle('Untitled');
        textContent = '';
    });

    document.getElementById('exit-option')?.addEventListener('click', function() {
        if (textarea.value.trim() !== '' && !confirm('Do you want to save changes?')) {
            return;
        }

        document.getElementById('notepadWindow').style.display = 'none';
        updateTaskbar();
    });

    // TODO: Implement keyboard shortcuts
    // Windows 98 has many keyboard shortcuts like:
    // - Alt+Tab for window switching
    // - Alt+F4 to close windows
    // - Ctrl+C, Ctrl+V for clipboard
    document.addEventListener('keydown', function(e) {
        // Implementation would go here
    });

    // Helper functions
    function getSelectedText() {
        if (!textarea) return '';
        return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
    }

    function insertTextAtCursor(text) {
        if (!textarea) return;

        const cursorPos = textarea.selectionStart;
        const textBefore = textarea.value.substring(0, cursorPos);
        const textAfter = textarea.value.substring(textarea.selectionEnd);

        textarea.value = textBefore + text + textAfter;
        textarea.selectionStart = textarea.selectionEnd = cursorPos + text.length;
        textarea.focus();
        updateCursorPosition();
    }

    function deleteSelectedText() {
        if (!textarea) return;

        if (textarea.selectionStart === textarea.selectionEnd) {
            // No text selected, delete next character
            const cursorPos = textarea.selectionStart;
            const textBefore = textarea.value.substring(0, cursorPos);
            const textAfter = textarea.value.substring(cursorPos + 1);

            textarea.value = textBefore + textAfter;
            textarea.selectionStart = textarea.selectionEnd = cursorPos;
        } else {
            // Delete selected text
            const cursorPos = textarea.selectionStart;
            const textBefore = textarea.value.substring(0, textarea.selectionStart);
            const textAfter = textarea.value.substring(textarea.selectionEnd);

            textarea.value = textBefore + textAfter;
            textarea.selectionStart = textarea.selectionEnd = cursorPos;
        }

        textarea.focus();
        updateCursorPosition();
    }

    function updateWindowTitle(filename) {
        const notepadWindow = document.getElementById('notepadWindow');
        const windowTitle = notepadWindow.querySelector('.window-title');

        if (filename === 'Untitled') {
            windowTitle.innerHTML = windowTitle.innerHTML.replace(/[^<]* - Notepad/, 'Untitled - Notepad');
        } else {
            windowTitle.innerHTML = windowTitle.innerHTML.replace(/[^<]* - Notepad/, `${filename} - Notepad`);
        }
    }
