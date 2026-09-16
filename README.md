# s&Doom

s&Doom is a growing collection of Doom-compatible WADs brought together inside **s&box**, with single-player, co-op and competitive multiplayer. Explore levels on your own, team up with friends, or face off in PvP—all from one shared launcher.

The project brings classic Doom gameplay into s&box with a retro desktop-style interface, configurable controls, music and sound, local saves, leaderboards and in-game bug reporting. Development is ongoing, with room for more WADs, features and multiplayer improvements as the collection grows.

## Play your way

- **Single-player:** explore campaigns with classic Doom movement and combat.
- **Co-op:** tackle levels together with friends.
- **PvP:** compete in multiplayer matches.
- **One launcher:** choose a game and configure your controls from the retro desktop.
- **Save your progress:** local save/load support for single-player sessions.

## The collection

This source release includes **Freedoom Phase 1**, **Freedoom Phase 2** and **FreeDM**. The collection is intended to expand as additional WADs are integrated and their redistribution permissions are confirmed. Each WAD retains its own identity, credits and license.

## Project layout

Important paths:

- game project: [doom_port.sbproj](./doom_port.sbproj)
- main scene: [Assets/game.scene](./Assets/game.scene)
- main C# project: [Code/doom_port.csproj](./Code/doom_port.csproj)
- vendored Doom runtime: [Code/ManagedDoom](./Code/ManagedDoom)
- `s&box` host adapters: [Code/ManagedDoomHost](./Code/ManagedDoomHost)
- web launcher source: [WebShell](./WebShell)

The game runs on an adapted Managed Doom runtime, with s&box handling the host integration and MeltySynth providing music synthesis.

## Requirements

- `s&box`
- the project opened as an `s&box` game package

This repo is intended to run through the `s&box` editor/runtime rather than as a standalone .NET app.

## Running

1. Open the project in `s&box`.
2. Open [Assets/game.scene](./Assets/game.scene).
3. Press Play.

Choose a bundled WAD from the launcher. Game data and the music soundfont are stored in [Assets/doom](./Assets/doom). See [SOURCE_RELEASE.md](./SOURCE_RELEASE.md) for build instructions, hosted-service requirements and local testing.

## Notes on controls

- `Tab` is used as Doom menu escape because `s&box` reserves real `Escape`
- `M` opens the automap by default; controls can be configured in the launcher
- gameplay/input is tuned toward classic Doom behavior rather than modern freelook conventions

## Licensing

The original integrated game/host code is licensed under GPL-2.0-or-later.
See [LICENSING.md](LICENSING.md) for the explicit grant and third-party exceptions.
See [SOURCE_RELEASE.md](SOURCE_RELEASE.md) for the tagged source and build instructions.

See:

- [LICENSE](./LICENSE)
- [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md)

The bundled Freedoom and FreeDM 0.13.0 data uses BSD-3-Clause. Its notices and contributor/music credits are
in `Assets/doom` and the launcher's **Licenses & credits** window.

Before publishing, run `python tools/verify_release_assets.py` and follow
[SOURCE_RELEASE.md](SOURCE_RELEASE.md).
