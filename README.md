# sbox-doom-port

`sbox-doom-port` is a singleplayer Doom/Freedoom port for `s&box`, built by hosting vendored Managed Doom runtime code inside an `s&box` game package.

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

This project includes GPL-covered Doom runtime code and should be treated as a GPL source release.

See:
- [LICENSE](./LICENSE)
- [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md)

Third-party components currently noted:
- Managed Doom
- MeltySynth
