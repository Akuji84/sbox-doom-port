# sbox-doom-port

`sbox-doom-port` is a single-player and multiplayer Doom/Freedoom port for `s&box`, built by hosting vendored Managed Doom runtime code inside an `s&box` game package.

Current features:
- Freedoom title screen, demos, and gameplay running inside `s&box`
- classic Doom-style keyboard and mouse controls
- sound effects and music
- local save/load support
- in-game bug report form with a live backend endpoint

## Project layout

Important paths:
- game project: [doom_port.sbproj](./doom_port.sbproj)
- main scene: [Assets/game.scene](./Assets/game.scene)
- main C# project: [Code/doom_port.csproj](./Code/doom_port.csproj)
- vendored Doom runtime: [Code/ManagedDoom](./Code/ManagedDoom)
- `s&box` host adapters: [Code/ManagedDoomHost](./Code/ManagedDoomHost)

## Requirements

- `s&box`
- the project opened as an `s&box` game package

This repo is intended to run through the `s&box` editor/runtime rather than as a standalone .NET app.

## Running

1. Open the project in `s&box`.
2. Open [Assets/game.scene](./Assets/game.scene).
3. Press Play.

The game uses the mounted Freedoom resource at:
- [Assets/doom/freedoom1.wad](./Assets/doom/freedoom1.wad)

Music synthesis uses:
- [Assets/doom/GeneralUser-GS.sf2](./Assets/doom/GeneralUser-GS.sf2)

## Notes on controls

- `Tab` is used as Doom menu escape because `s&box` reserves real `Escape`
- gameplay/input is tuned toward classic Doom behavior rather than modern freelook conventions

## Licensing

The original integrated game/host code is licensed under GPL-2.0-or-later.
See [LICENSING.md](LICENSING.md) for the explicit grant and third-party exceptions.
See [SOURCE_RELEASE.md](SOURCE_RELEASE.md) for the tagged source, build instructions
and verification limits.

See:
- [LICENSE](./LICENSE)
- [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md)

Third-party components currently noted:
- Managed Doom
- MeltySynth

The current bundled game data is limited to Freedoom Phase 1, Freedoom Phase 2
and FreeDM 0.13.0. Their BSD-3-Clause notices and contributor/music credits are
in `Assets/doom` and the launcher's **Licenses & credits** window.

Before publishing, run `python tools/verify_release_assets.py` and follow
[SOURCE_RELEASE.md](SOURCE_RELEASE.md). Removing files locally does not retract
an old published package. The native-engine dependency question remains unresolved;
source availability is not certification of full GPL or Play Fund compliance.
