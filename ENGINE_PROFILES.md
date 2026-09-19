# Game profiles

The engine shares WAD access, map geometry, rendering infrastructure and s&box
adapters. Game-specific definitions and behavior are selected through an
immutable `GameProfile` owned by `Wad` and exposed by `GameContent` and `DoomGame`.
`GameMode`, `GameVersion` and `MissionPack` continue to describe Doom variants;
they are not substitutes for the Doom/Heretic family distinction.

The five playable Doom-family WADs select the Doom profile. Its definition initializer runs
the existing DeHackEd initialization unchanged. Heretic has a separate profile,
but gameplay is not implemented yet: normal content loading, dummy content
loading and runtime creation reject it before Doom gameplay can start. A failed
Heretic content load does not reset or patch the active Doom definitions.

Recognized Heretic base filenames are `heretic`, `heretic1`, `blasphem`,
`blasphemer` and `blasphdm`. An explicit profile may be supplied to `Wad` or
`GameContent` for renamed content. Add-on filenames do not choose the game family.
Filename recognition is routing, not content validation: the Heretic preview
loader validates its required lumps. Unknown names retain existing Doom
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
frame hashes for each playable Doom-family WAD, captured at `55929a4` before game profiles
were introduced. The regression runner compares against those fixed baselines,
alongside its existing campaign, save, input and multiplayer checks. Synthetic
empty WAD fixtures exercise profile rejection before asset loading without
requiring any Heretic game assets.

## Chunk 2: Heretic assets and geometry preview

Open `Assets/scenes/heretic-preview.scene` from this branch in the s&box editor
and press Play. The `SboxHereticPreview` component displays Blasphemer E1M1 using
the shared software renderer. Set Episode and Map before Play; adjust Yaw to
inspect other directions. The normal game scene and web launcher are unchanged.

`GameContent.CreateHereticPreview` validates the palette, lighting and required
asset markers, loads the Heretic sprite catalog and eight animation cycles,
and leaves Doom definitions untouched. `HereticMapPreview` creates an isolated
geometry-only world and returns row-major RGBA frames. Binary map lump order
and record lengths are checked before map construction; Hexen/UDMF maps are
rejected. This is not a general untrusted-WAD sanitizer.

The preview animates textures and flats at 35 tics per second. It deliberately
omits actors, special-line behavior, combat, movement, audio, inventory, saves
and multiplayer; the on-screen report lists omitted things and specials.
Normal gameplay constructors still reject Heretic. Sprite assets are decoded
and checked, but are not yet connected to Heretic actor definitions.

Blasphemer 0.1.8 is pinned by SHA-256 in `tools/verify_release_assets.py`, with
its release-tag BSD license and credits included in the in-game notices.
Regression checks render all 48 map slots in four directions, exercise preview
validation and animation, and retain the existing Doom compatibility baselines.

## Chunk 3: single-player navigation checkpoint

The standalone preview scene now defaults to navigation mode. `SboxHereticPreview`
provides WASD movement, left/right arrows for turning, up/down arrows for looking,
E to use, Home to center the view, and End to land. Enable the editor's **Test
Flight** property to supply flight power; R/F then ascend/descend. Actual artifact
acquisition belongs to the inventory chunk. Disable **Navigation** to retain the
Chunk 2 stationary geometry preview.

`HereticWorldSession` owns its player state and command type. It uses shared
fixed-point collision, blockmaps, BSP rendering, sector movers and lighting;
Heretic movement, keys and activation tables are separate. The renderer supports
Heretic's shifted horizon without changing Doom's default projection. The
session advances at 35 tics/second and never runs Doom actor or weapon states.

Implemented here: solid-wall sliding, 24-unit step limit, gravity, look centering,
flight/landing, ice, wind, currents, hazardous floors, secret sectors, three key
pickups, keyed/manual/tagged doors, switches, stairs, platforms and teleporters.
Heretic line dispatch comes from the pinned Chocolate Doom `p_switch.c` and
`p_spec.c`, including the differing 100/105/106/107 actions. Exit actions report
an exit request; campaign transitions are deferred to Chunk 6.

The test scene still omits enemies and other actor definitions, combat, sound,
artifact inventory, saves and networking. It is a mechanics checkpoint, not a
complete playable campaign. Actor-to-actor collision and transformations must be
integrated with the actors in the combat chunk. `DoomGame` still rejects Heretic.

The regression suite tests individual movement/environment/interaction mechanics,
runs repeatable navigation and extreme look-angle renders in all 48 Blasphemer
map slots, and checks Doom's original state/image baselines again afterward.
