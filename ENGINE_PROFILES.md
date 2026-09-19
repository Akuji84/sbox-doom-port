# Game profiles

The engine shares WAD access, map geometry, rendering infrastructure and s&box
adapters. Game-specific definitions and behavior are selected through an
immutable `GameProfile` owned by `Wad` and exposed by `GameContent` and `DoomGame`.
`GameMode`, `GameVersion` and `MissionPack` continue to describe Doom variants;
they are not substitutes for the Doom/Heretic family distinction.

All five bundled WADs select the Doom profile. Its definition initializer runs
the existing DeHackEd initialization unchanged. Heretic has a separate profile,
but gameplay is not implemented yet: normal content loading, dummy content
loading and runtime creation reject it before Doom gameplay can start. A failed
Heretic content load does not reset or patch the active Doom definitions.

Recognized Heretic base filenames are `heretic`, `heretic1`, `blasphem`,
`blasphemer` and `blasphdm`. An explicit profile may be supplied to `Wad` or
`GameContent` for renamed content. Add-on filenames do not choose the game family.
Filename recognition is routing, not content validation: the future Heretic
loader must validate its required lumps. Unknown names retain existing Doom
behavior unless a profile is explicitly supplied. A known Heretic base cannot
be forced onto the Doom profile.

## Extension boundaries

Paths below are relative to `Code/ManagedDoom`.

| Area | Existing Doom coupling | Heretic extension needed |
| --- | --- | --- |
| Definitions | `Doom/Info/DoomInfo.*`, `Doom/DeHackEd.cs` mutate static tables | Separate family-owned actor, state, weapon and item definitions; never patch Heretic data into Doom's global tables |
| Assets | `Doom/Graphics/SpriteLookup.cs`, `TextureAnimation.cs` use Doom names and animation tables | Heretic sprite, animation and asset catalog |
| Player | `Doom/Game/Player.cs`, `Doom/World/PlayerBehavior.cs`, weapon/ammo/power enums | Inventory, artifacts, flight, view pitch and family-specific player state |
| Combat | `Doom/World/WeaponBehavior.cs`, `MonsterBehavior.cs`, `ThingAllocation.cs`, `Mobj.cs`, `ItemPickup.cs` | Heretic actors, attacks, projectiles, pickups and state actions |
| Maps | `Doom/Map/Map.cs`, `Doom/World/MapInteraction.cs`, sector actions and specials | Keep geometry infrastructure; provide Heretic thing IDs, line specials, sky/music selection and map rules |
| UI and progression | `Doom/World/StatusBar.cs`, `Video/StatusBarRenderer.cs`, menu/intermission/finale classes, `Doom/Game/DoomGame.cs` | Heretic HUD, inventory UI, episodes, boss triggers and endings |
| Persistence | `Doom/Game/SaveAndLoad.cs` serializes fixed Doom arrays and enums | Versioned family-specific payload and family validation before loading |
| Host and network | `Doom/Game/TicCmd.cs` and `Code/ManagedDoomHost` adapters | Heretic input commands, deterministic synchronization and recovery |

This first boundary does not make those subsystems generic yet. Later chunks
introduce concrete behavior behind the profile as each subsystem becomes
testable. Doom's current global DeHackEd tables also still preclude running
different patched Doom definitions simultaneously in one process.

## Upstream references and licensing

The reference implementation for future Heretic behavior is Chocolate Doom's
`src/heretic` at commit `895f581c5d91497bdda0516612da803fe5843e28`:

- [Heretic source](https://github.com/chocolate-doom/chocolate-doom/tree/895f581c5d91497bdda0516612da803fe5843e28/src/heretic)
- [Weapon behavior and copyright/license header](https://github.com/chocolate-doom/chocolate-doom/blob/895f581c5d91497bdda0516612da803fe5843e28/src/heretic/p_pspr.c)
- [GPL license text](https://github.com/chocolate-doom/chocolate-doom/blob/895f581c5d91497bdda0516612da803fe5843e28/COPYING.md)

The inspected weapon source grants GPL version 2 or later and names id Software,
Raven Software and Simon Howard. This fits the project's GPL-2.0-or-later source
grant. Inspect each additional file and dependency when adapting it; retain its
copyright and license header, record the upstream revision, and add a dated
modification notice. Engine source permission does not cover proprietary Heretic
game data. No HereticXNA code or proprietary Heretic assets are used here.

## Compatibility checks

`tests/Regression/doom-compatibility.json` records simulation/save and rendered
frame hashes for every bundled WAD, captured at `55929a4` before game profiles
were introduced. The regression runner compares against those fixed baselines,
alongside its existing campaign, save, input and multiplayer checks. Synthetic
empty WAD fixtures exercise profile rejection before asset loading without
requiring any Heretic game assets.
