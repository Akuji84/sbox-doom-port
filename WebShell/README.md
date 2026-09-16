# s&Doom web shell source

Editable HTML, CSS and JavaScript copied from the project's local shell
development directory for the September 16 source release. This is a modified
adaptation of Windows 98 GE; upstream terms and attribution are preserved in
`UPSTREAM-LICENSE.txt` and the HTML header. Original s&Doom modifications are
available under GPL-2.0-or-later as described in `../LICENSING.md`.

Serve this directory over HTTP at `/shell/live/` or `/shell/beta/`, with the
corresponding `/api/` services on the same origin. The JavaScript files contain
the API routes and payloads. It is plain HTML/CSS/JavaScript; no bundler needed.

## Assets

Supply icons, backgrounds and fonts under `assets/` at the paths referenced
by the HTML, CSS and JavaScript.
