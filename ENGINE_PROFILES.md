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
provides WASD movement (Shift to run), left/right arrows for turning, up/down arrows for looking,
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

Navigation follow-up: the preview samples input once per frame and retains short
use/center/land actions until the 35 Hz simulation consumes them. Separate use
presses retain a release edge; multiple taps before a tick are coalesced. Recovery
from a stalled frame is capped at 250 ms. Ground liquid clipping includes flowing
water and super lava. Step and hard-landing camera recovery and bobbing follow
the pinned Heretic reference. Automated checks cover frame rates from 30 to 240
FPS, action retention, terrain clipping and camera recovery. In-editor interaction
still needs a manual playtest.

## Chunk 4a: actor and weapon definitions

The first combat checkpoint imports all 1,208 states, 161 actor definitions and
nine normal/nine Tome-powered weapon definitions into immutable Heretic-owned
collections. Separate enums cover actors, states, sounds, actions, weapons, ammo
and both flag words. No entries are inserted into DoomInfo or its DeHackEd tables.
These are definitions, not implemented attacks or enemy AI.

`HereticActorState` follows actor scheduling in the pinned `p_mobj.c`: spawn
frames do not call actions; transitions call explicitly supported Heretic actions;
permanent states remain; S_NULL removes the actor; and action-driven redirection
is preserved. Unsupported actions throw instead of silently doing nothing.
Weapon overlays require their own `p_pspr.c` transition logic and must not use
this actor runner. The navigation scene now uses this runner for all three key
animations, including fullbright frames. No other map actors are activated yet.

`tools/import_heretic_definitions.py` reproduces the checked-in definitions from
Chocolate Doom commit `895f581c5d91497bdda0516612da803fe5843e28`. It verifies
SHA-256 hashes of six reference files, normalizing CRLF to LF, before parsing.
The generated C# includes upstream copyright and GPL notices. Normal builds use
the checked-in C# and do not require the reference checkout or Python.

```powershell
python tools/import_heretic_definitions.py C:/path/to/chocolate-doom --check
```

Omit `--check` to regenerate after checking out that exact reference commit.
The regression suite checks table references, every referenced Blasphemer sprite
frame, representative actor stats, both weapon ammo tables, state timing/removal,
unsupported action rejection, action redirection, and animated map keys. Doom
compatibility snapshots are checked again after the Heretic tests.

Remaining combat work is grouped into these checkpoints:

1. **4b — combat interaction:** family-owned actor instances, spawning/filtering,
   actor collision and targeting, damage/death and shared-renderer integration.
2. **4c — player weapons:** weapon overlays/input/ammo, normal and powered attacks,
   projectiles and impact effects, with per-weapon regression scenarios.
3. **4d — enemies and bosses:** AI/state actions, species attacks, transformations
   and boss triggers, with encounter and deterministic replay coverage.

The normal Heretic gameplay gate remains closed until the required systems are
implemented. This checkpoint does not claim a completed combat engine.

## Chunk 4b, first checkpoint: map actors and scenery

The navigation scene now loads supported map scenery alongside the three key
colors. It uses each Heretic definition's dimensions, sprite frames and spatial
flags, places hanging actors at ceiling minus height, and seeds animation timing
from the session's deterministic random stream. The editor's **Skill** property
selects the map difficulty before starting the scene.

Map classification applies the reference skill masks and single-player exclusion
flag. The classification API also covers network, deathmatch key exclusion and
no-monsters rules for later runtime integration; this does not enable network
play. Player starts, boss spots and ambient markers remain distinct from actors.
Unknown thing types and known but unsupported behaviors have separate counters
in the preview. Enemies, pickups other than keys, generators and scenery whose
animation requires an unimplemented action remain absent. A complete animation
loop is checked before spawning; a harmless first frame is not sufficient.

Supported solid scenery now participates in the shared blockmap collision path.
Sector movers keep Heretic bodies out of Doom's corpse/damage actions. This is
still the existing horizontal collision model: Heretic's over/under movement,
standing on actors, targeting, damage and actor deaths remain part of the next
4b checkpoint. No enemy combat or weapon attacks are enabled here.

Tests cover all skill masks, multiplayer/deathmatch/no-monsters classification,
unsupported actions later in animation loops, deterministic spawns and animation,
floor/ceiling placement, rendering in all 48 Blasphemer maps, and an actual solid
scenery movement rejection. The fixture covers 4,469 map actors, including 2,688
solid and 1,467 ceiling-hung actors. Existing Doom baselines still pass afterward.
In-editor playtesting remains outstanding.

## Chunk 4b: scenery height collision and aiming query

The navigation player now passes above and below solid scenery when its vertical
extent is clear. Exact contact with a top surface allows horizontal movement.
Vertical movement checks crossed scenery tops and undersides, so a fast fall or
flight step cannot tunnel through them. A stationary scenery top supports the
player, permits movement, and releases the player back to gravity when it no
longer overlaps. Map floor/ceiling heights remain separate from actor support.
This is not yet the full moving-actor stacking/pushing system.

`TraceAim` queries the nearest solid/shootable actor or blocking map line along
a bounded, height-aware ray. It checks openings between sectors and skips the
player itself. It has no damage, sound or line-activation side effects. It is an
obstruction-query foundation, not a weapon attack, autoaim implementation or
floor/ceiling impact-effect system. Actor damage, death behavior, moving actor
support and weapon integration remain outstanding in Chunk 4b and later chunks.

Regression fixtures cover overlapping versus separated actor heights, fast
landing, standing and moving on scenery, gravity after support is lost, underside
flight collision, aiming at/over an actor and stopping at map walls. All existing
Doom simulation/render hashes and 48-map Heretic checks remain required.

## Chunk 4b: sector riders and environmental death

A player standing on scenery now follows its vertical movement when a sector
floor raises or lowers it. Non-crushing planes roll back the sector, scenery and
player when rider headroom is insufficient; crushing planes apply the existing
Heretic environmental damage once per damage opportunity. This support applies
to sector-driven vertical movement, not horizontal actor pushing or arbitrary
multi-actor stacks.

Environmental damage now initializes ordinary player death once, clears flight,
removes solid/shootable flags and reduces the collision height. The death camera
lowers to six units while world animations and movers continue. Dead-player live
controls are ignored. This implements the ordinary camera/physics portion of
p_user.c and p_inter.c at the previously pinned GPL reference; corpse animation,
sounds, attacker-facing behavior, special deaths and respawning remain pending.
Enemy damage and attacks are still not active.

Regression tests drive real sector plane movement through rising, lowering,
blocked and crushing cases, then check lethal damage, repeated damage, input
suppression, death-camera rendering and continued simulation after death. All
Doom compatibility hashes and 48-map Heretic regression checks remain required.

## Chunk 4b: ordinary monster damage core

`HereticCombatant` owns ordinary non-boss monster damage without assigning Doom
actor definitions or calling Doom damage/state routines. It implements health
and overkill, normal/suppressed/powered-staff thrust, pain chance, reaction-time
reset, target thresholds, corpse flags/height, strict normal/extreme death
selection and initial death-tic variation. It retains the source for kill
accounting by its eventual owner. State changes dispatch through the supplied
Heretic action handler and synchronize the shared rendering frame fields.

Construction validates every action in the reachable spawn/see/pain/death chains
before allocating a body or consuming randomness. Bosses, players, destructible
props, negative damage and cross-session sources are explicitly rejected. This
API accepts already-resolved ordinary hits: attack-specific transformations,
whirlwinds, death-ball rules, rain scaling and player protections belong to
specialized handlers. Zero damage is ignored and consumes no randomness.

This checkpoint is the damage component, not active map combat. The owner must
supply real AI/death actions, link and move bodies, remove expired actors, and
handle sounds, drops and kill accounting. Regression tests use a clearly named
action spy to test dispatch; it is not installed in the game. Map enemies remain
unsupported until those behaviors are implemented. Existing scene gameplay and
weapon controls do not change in this checkpoint.

Tests cover pain, ordinary and powered thrust, source/target rules, overkill
threshold boundaries, ordinary-death fallback, repeat-kill protection, death
action timing, missing-action rejection and session isolation. The existing Doom
compatibility snapshots still pass after these tests.

## Chunk 4b: opt-in native Clink encounter

Open `Assets/scenes/heretic-preview.scene`, enable **Test Combat** before Play,
and use the existing movement/turn controls. Space now fires the normal Gold Wand
described below. The earlier fixed-damage debugging ray has been replaced.
The default navigation scene remains unchanged.

The encounter searches for a visible, collision-free Clink spawn near the player.
The enemy is linked into the real blockmap and sector rendering lists, uses the
Heretic state runner/damage component, detects the player, approaches with basic
collision-aware pursuit, and performs the reference 3–9 damage melee attack.
Pain and death states run; death releases solid collision and records one kill.
The pursuit routine is intentionally a limited test implementation, not the full
vanilla A_Chase (door use, sound propagation, patrol/turn tactics and full actor
movement remain pending). Normal map Clinks and other enemies remain disabled.

Sound actions emit `SoundRequested`; the reference Clink drop chance, amount and
initial velocity produce `DropRequested`. The preview now plays these encounter sounds locally. Collectible drops and
full audio/inventory integration remain pending. Telefragging and crusher damage are routed through Heretic combatant
damage, never Doom actor actions. Required-action validation now includes melee,
missile and crash chains as well as the previously checked states.

Automated checks cover the native rendered encounter, melee damage, test input,
pain/death completion, sound/drop requests, one-time kill accounting, telefragging,
crusher damage, short input retention and deterministic encounter replay. All
48-map and Doom baseline checks remain required. In-editor playtesting has not
been performed.

## Chunk 4c: normal Gold Wand in the test encounter

The opt-in **Test Combat** scene starts with the normal Gold Wand and 50 rounds.
Space holds attack. Its separate weapon-state runner processes zero-duration
psprite transitions correctly, raises/lowers the weapon, animates attacks and
handles refire/release without using Doom weapon definitions. Firing consumes one
round and deals 7–14 damage. The first shot is accurate; repeated shots use the
reference spread. It uses the shared read-only aim calculation, including the
three-angle target search and view-pitch fallback, then sends damage only through
the Heretic enemy path. Non-shootable scenery is ignored by the bullet query.

The bundled Blasphemer weapon sprites are drawn over the shared preview with
movement bobbing. Ammo is shown in the preview message. Death lowers the weapon;
empty ammo lowers the wand and raises the staff. Keys 1 and 2 select staff and wand. The normal Gold Wand state/damage rules come from the
pinned GPL p_pspr.c and starting ammo from g_game.c.

This is still a limited weapon checkpoint. Powered mode, the remaining weapons, inventory/ammo pickups are not connected. `ShotFired` exposes the
shot result for later effects/audio integration. The overlay now uses sector lighting and full-bright frame flags. Do not interpret this as the full Heretic weapon system or exact
whole-game random-stream compatibility with the reference.

Tests verify first-shot and 11-tick refire cadence, damage/ammo, release, last
round and empty ammo, death lowering, pitch fallback, real Clink hits/replay and
raised/firing sprite rendering. A generated 320x200 firing frame was visually
inspected. In-editor playtesting remains outstanding.

### 2026-09-22: staff and weapon switching

The test encounter now supports the normal staff using the pinned GPL weapon
states and melee damage/spread rules. It consumes no ammo, deals 5-20 damage to
linked test enemies in melee reach, and turns toward a struck target. Selection
waits for the current attack, lowers the old weapon and raises the selected one.
Empty wand ammo falls back to the staff. Unsupported weapons and an empty wand
cannot be selected. Selection taps survive frames shorter than one simulation tick.
Staff sprites use the existing weapon overlay; impact sounds now play locally.

### 2026-09-22: native weapon impact puffs

Staff and Gold Wand hits now create the bundled Blasphemer impact sprites in the
shared world renderer. Wall impacts back off four units and actor impacts ten
units along the shot. Sky walls suppress puffs. The normal Heretic state chains,
random vertical offset and spawn random draw are used; staff puffs rise, while
Gold Wand puffs remain stationary. Effects do not enter the collision blockmap
and unlink from the sector list when their animations finish, including after
player death. Existing effects advance before newly fired weapon effects.

Tests cover rendered pixels, impact placement, random use, sky suppression,
non-blocking flags, staff rise, lifetime and removal, plus both actual weapons
creating their respective effects. Positional audio remains pending; this is not full reference attack/RNG compatibility.

### 2026-09-22: hitscan blood splatter

Staff and wand hits now apply the reference blood chance (random byte below 192)
after the puff and before damage, skipping targets marked NoBlood. Blood uses
Blasphemer sprites, the pinned spawn state, target pointer, horizontal random
momentum and low gravity. It expires through its own Heretic state chain and
uses the terminal splatter frame when it meets a wall, floor or ceiling.

This is a cosmetic effects checkpoint. Horizontal wall clipping uses a center-line
geometry trace, not the full radius-based Heretic missile collision model; actor
collision and terrain/sky missile behavior remain for the projectile chunk.
Blood cannot deal damage or activate pickups/lines. Tests cover eligibility,
spawn random order, visible rendering, low gravity, floor impact and unlinking.

### 2026-09-22: impact-triggered map actions

Staff and Gold Wand weapon traces now activate Heretic line specials 24 (raise
floor), 46 (open door) and 47 (raise platform to nearest floor and change).
Activation occurs at each traversed line before testing its opening, following
the pinned p_map.c/p_spec.c order. Aim-only TraceAim queries remain read-only.
One-shot switches are consumed even when no tagged mover starts; repeatable door
switches retain their special and reset their texture after 35 ticks. Other line
specials, including use-only exits, are ignored by weapon activation.

Tests exercise all three actions, repeat reset, one-shot behavior, actual wand
fire and read-only aiming. Monster projectile triggers remain part of the future
projectile implementation. In-editor testing remains outstanding.

### 2026-09-22: weapon overlay lighting

Staff and Gold Wand overlays now use the shared renderer's weapon light table
for the player's sector. Full-bright state frames retain their brightness; fixed
colormaps take precedence when supplied by the renderer. Palette mapping applies
only to opaque patch pixels and supports flipped frames and screen clipping.
Existing DrawScreen callers retain their original unmapped behavior.

Regression checks cover dark versus bright sectors, full-bright override, mapped
normal/flipped patches and untouched background pixels. The full Doom render and
simulation baselines remain required. In-editor testing remains outstanding.

### 2026-09-22: native encounter sound playback

The opt-in Test Combat preview now plays Gold Wand fire, staff impact and Clink
sight/attack/pain/death sounds through s&box SoundStream. Seven enabled sound
names resolve directly to Blasphemer WAD lumps, without Doom sound identifiers.
DMX headers, rates and declared lengths are validated before subscribing to
session events; unsigned samples are converted to signed PCM after removing
DMX guard samples. Malformed or missing enabled assets fail preview setup.

SoundVolume controls local preview playback (default 0.4). Playback is bounded to
16 active voices and disposes its handles/subscription on teardown or preview
failure. This is local, non-positional audio: reference channel priority,
attenuation/panning, pitch variation, music and world/environment sounds remain
pending. In-editor listening and device-output testing remain outstanding.

Tests decode all seven bundled samples, check PCM conversion and malformed input,
and verify session events from real wand shots, staff impacts and Clink behavior.

### 2026-09-22: normal Dragon Claw

Enable Test Combat and Test Blaster in the preview, then press 4 to select the
Dragon Claw. The explicit test grant supplies 50 separate rounds. Normal attacks
use the pinned A_FireBlasterPL1 damage (4-32 in multiples of four), initial windup
and six-tick held-fire sequence, shared aiming, blood and shoot-switch activation.
Empty ammo falls back to the Gold Wand when available, otherwise the staff.
Wall hits use the small puff; actor hits use the larger puff and impact sound.
The two added sound samples are validated with the encounter audio assets.

Powered Dragon Claw projectiles, normal map weapon pickups and full inventory
remain disabled. Tests cover explicit ownership, switching, cadence, damage,
ammo, fallback and impact selection. In-editor testing remains outstanding.

### 2026-09-22: weapon fallback and ghost targets

Automatic empty-ammo selection now follows the pinned P_CheckAmmo order for
implemented weapons: owned Dragon Claw, Gold Wand, then staff. The reference
requires more than one shot in reserve for automatic selection; a final round
can still be selected manually. This also fixes an empty wand skipping a loaded
Dragon Claw. Unsupported weapons remain excluded from the selection table.

Physical staff traces now pass through MF_SHADOW actors, while ranged weapons
and read-only aiming still target them. This adds the interaction rule without
enabling ghost enemy AI or new map spawns. Tests cover reserve boundaries,
manual last-round selection and separate physical/ranged/aim trace behavior.

### 2026-09-22: implemented weapon ammo pickups

Starting Test Combat now spawns the map's small/large Gold Wand and Dragon Claw
ammo pickups after applying the existing skill and single-player filters. They
render and animate as Heretic actors; touch collection checks horizontal and
vertical overlap, removes accepted pickups and plays ITEMUP. Navigation-only
spawns stay unchanged, and starting another encounter does not duplicate ammo.

Gold Wand pickups give 10/50 rounds (cap 100); Dragon Claw gives 10/25 (cap 200).
Baby/Nightmare increase the grant by half, rounding down. Full-ammo pickups remain
in the map. Replenishing an empty owned weapon from staff selects it; collecting
ammo never grants weapon ownership. Backpacks, other ammo, collectible enemy
drops, multiplayer pickup rules and in-editor testing remain pending.

### 2026-09-22: Dragon Claw weapon pickup

Test Combat now also spawns filtered map thing 53 (Dragon Claw). Touching an
unowned weapon grants ownership, supplies 30 ammo (45 on Baby/Nightmare), selects
it over the staff or wand, removes the pickup and plays WPNUP. Duplicate weapons
grant ammo; an owned weapon remains in the map when ammo is full. An unowned
weapon can still be acquired with full ammo. Test Blaster remains an optional
editor shortcut rather than a requirement for acquiring the weapon.

The pickup uses single-player P_GiveWeapon rules. Cooperative weapon-stay and
deathmatch respawn behavior remain disabled with the multiplayer gameplay gate.
Tests cover real map collection, ownership, selection, sound and cap boundaries.

### 2026-09-22: healing potions

Test Combat now spawns filtered healing potions (map thing 81). They animate and
bob using the pinned fixed-point 64-phase offsets with a seeded starting phase.
Touching a potion restores 10 health up to 100, synchronizes the player and body
health, removes the accepted pickup and plays ITEMUP. Full-health players leave
it available; healing cannot revive a dead player. Navigation-only remains unchanged.

Tests cover collection, cap/retention, synchronized health, death rejection and
bob peak/wrap. Inventory healing artifacts, transformed-player health limits and
multiplayer pickup rules remain pending. In-editor testing is still outstanding.

### 2026-09-22: shield pickups and armor absorption

The combat preview now spawns Silver Shield and Enchanted Shield pickups using
the existing skill filters and reference item bobbing. They provide 100/200 armor
and use the pinned P_GiveArmor replacement threshold. Armor is displayed in the
preview status. Accepted pickups disappear and play ITEMUP.

Normal shield absorption is damage >> 1; enchanted absorption is (damage >> 1)
plus (damage >> 2), preserving reference rounding. Absorption is capped by remaining
armor, and depletion clears the armor type. The current player damage path covers
Clink melee and environmental damage; special attack modifiers, invulnerability
and transformed-player rules remain separate unfinished work.

Tests cover native pickup collection, replacement boundaries, odd-damage rounding,
depletion, health synchronization and lethal damage. In-editor testing remains
outstanding; multiplayer and save serialization are still gated.

### 2026-09-22: damage and pickup palette feedback

The preview now applies Heretic's PLAYPAL damage and pickup palettes to the
whole rendered frame, including the weapon overlay. Damage takes priority over
pickup feedback, uses post-armor damage, and caps its counter at 100. Successful
pickups add feedback; rejected full-cap pickups do not. Keys reset the pickup
counter to six, while ordinary pickups add six.

Palette selection follows pinned sb_bar.c. Preview counters decay once per
simulation tick, including after death; rendering never changes them. Detailed
attacker-facing death-camera behavior remains pending. Tests cover palette
rendering, precedence, armor absorption, timer decay and lethal damage.

### 2026-09-22: normal Gauntlets

Enable Test Combat and Test Gauntlets, then press 8 to select them. Normal attacks
use the pinned unpowered state sequence, 65-unit reach, 2-16 damage in multiples
of two, spread and overlay jitter. They consume no ammo, emit normal gauntlet
puffs and activation/use/hit/miss sounds, and drive the player's extra-light
flicker. A successful hit steers toward the target and requests the reference
single-tick forward command; release clears weapon lighting.

Gauntlets participate in empty-ammo fallback after available ranged weapons.
Powered life-steal and full projectile-based weapons remain pending.
Tests exercise ownership gating, cadence, actual melee damage, impact creation,
forward-command consumption and light reset. In-editor testing remains outstanding.

### 2026-09-22: Gauntlets map pickup

Test Combat now spawns filtered Gauntlets map pickups (thing 2005). Collection
grants ownership, removes the pickup, plays WPNUP and supplies pickup feedback.
Gauntlets consume and grant no ammo. Duplicate pickups remain available. The
reference weapon ranking switches automatically from staff, while leaving Gold
Wand and Dragon Claw selected. Press 8 to select owned Gauntlets manually.

Tests cover native collection, sound, duplicate rejection, ammo preservation and
staff-versus-ranged selection. Cooperative weapon-stay and powered mode remain
gated; in-editor playtesting remains outstanding.

### 2026-09-22: difficulty and melee-to-ranged ammo selection

The current player damage path now applies Baby difficulty's integer halving
before armor absorption and damage feedback, matching pinned P_DamageMobj order.
Other skills retain their incoming damage. A one-point Baby hit rounds to zero.
This does not enable automatic inventory healing or unfinished special attacks.

Collecting ammo for an empty owned ranged weapon now selects it from Gauntlets
as well as staff, following P_GiveAmmo. Ammo for an unowned weapon does not select
or grant that weapon. Tests cover all skill levels, odd damage, armor/feedback
order, lethal damage and Gauntlet ammo re-selection.

### 2026-09-22: encounter audio distance attenuation

Encounter playback now uses the bundled SNDCURVE and Heretic's approximate XY
distance, with a 1600-map-unit hearing cutoff. The preview normalizes curve gain
for its floating-point SoundVolume control. Existing voices update their gain
as listener/source positions or volume change, and non-finite volume is muted.
The sound-curve constructor validates length and keeps its own copy.

This adds attenuation, not the complete reference mixer: stereo panning, channel
priorities, pitch randomization, music and device listening tests remain pending.
Tests cover curve loading, distance lookup, local sounds, cutoff and overflow-safe
coordinate differences. Existing Doom audio and simulation are unchanged.

### 2026-09-22: stereo encounter sounds

Encounter sounds now play through two-channel SoundStream output. The initial
left/right separation follows pinned Heretic s_sound.c using the source bearing
relative to the player; local/player-origin sounds stay centered. Mono WAD samples
are converted to interleaved stereo with equal-power channel gains. This gain law
is a preview mixer choice, not a claim of bit-identical legacy backend output.

Panning is sampled when each sound starts. Distance attenuation and master volume
continue updating during playback. Continuous re-panning, original channel-priority
rules, pitch variation, music and in-editor listening tests remain pending.
Tests cover left/right placement, turning, local centering and PCM bounds.

### 2026-09-22: normal Crossbow and native projectiles

The opt-in combat preview now supports the normal Crossbow: map weapon and
small/large ammo pickups, ownership, ranked selection, 50-ammo capacity,
difficulty bonuses, one-ammo three-bolt volleys and empty-ammo fallback.
Enable TestCombat in the Heretic preview and collect the weapon, or also enable
TestCrossbow to grant it for testing. Press 3 to select an owned Crossbow.

Bolts use immutable Heretic actor definitions, sector-linked sprites, fixed-tick
motion, autoaim/pitch, owner and height exclusion, per-bolt ghost rules, damage,
impact states and sounds. They use isolated collision dispatch in the shared
movement engine without invoking Doom actor definitions. Sky impacts disappear;
other impacts animate and are unlinked when finished. Existing bolts continue
ticking independently of weapon selection and the player's attack button.

Movement uses collision substeps of at most eight map units to avoid crossing
narrow targets. This is a deliberate robustness choice, not bit-identical legacy
missile movement. Feet clipping, powered Crossbow, other projectile families,
full enemy damage rules, multiplayer and saves remain pending. The preview is
still gated and requires in-editor playtesting before release.

Regression coverage exercises three-bolt firing, actual Clink damage, rendering,
replay determinism, ammo/fallback, map weapon collection, bonuses/caps, owner and
ghost exclusion, vertical separation and sky/floor impact cleanup. The unchanged
five-WAD Doom simulation/render baselines remain required.

### 2026-09-22: normal Hellstaff

The combat preview adds the normal Hellstaff (Skull Rod in the engine tables)
through the existing native projectile layer. It consumes one ammo per shot,
fires every four ticks during its attack animation, and randomizes the initial
projectile frame after a successful spawn using the simulation RNG. Releasing
attack still completes the second shot of the current animation when ammo permits.
Normal projectile states include flight, direct-hit damage and impact cleanup;
no powered rain or explosive damage is enabled.

Map weapon pickups grant 50 ammo; small/large ammo grant 20/100, capped at 200,
with existing difficulty bonuses. Selection and fallback respect the reference
ranking without lower-ranked pickups replacing the selected Hellstaff. Enable
TestCombat and collect it, or enable TestSkullRod to grant it, then press 5.
Shot and impact sounds use licensed bundled WAD samples.

Regression checks cover actual Clink damage, replay RNG/positions/states,
firing cadence and release, last-shot fallback, map weapon and both ammo pickups,
capacity, difficulty bonuses, selection ranking and projectile cleanup. Existing
Doom baselines remain required. Powered Hellstaff, other remaining weapons,
full enemy behavior, campaign integration, saves and multiplayer remain gated;
in-editor playtesting is outstanding.

### 2026-09-22: normal Phoenix Rod

The native combat preview adds the normal Phoenix Rod, including five-tick
windup, one-ammo fireballs, four-unit recoil, animated lateral trails and impact
sounds. The projectile action runner dispatches Phoenix trails and explosions
without invoking Doom actor actions. Splash damage uses a 128-unit maximum-axis
radius minus the target radius, with visibility checks, and can hurt the shooter.
The preview routes splash through the existing player armor/health and registered
Clink damage paths. Boss immunities and other actors remain gated with those enemies.

Weapon pickups grant two ammo, small/large ammo grant one/ten, and capacity is 20.
Existing difficulty bonuses and reference selection/fallback priorities apply.
In the preview enable TestCombat and collect the weapon, or enable TestPhoenix
for a test grant; press 6 to select it. Powered flames remain disabled.

Tests exercise windup, final ammo, recoil, native damage, trails and cleanup,
self splash with armor, map visibility blocking, and weapon/ammo collection.
The five existing Doom simulation/render baselines remain required. Cosmetic
trail clipping uses shared path traversal; exact legacy trail physics and liquid
floor splash effects are not implemented. Full enemy behavior, campaign, saves,
multiplayer and in-editor playtesting remain outstanding.

### 2026-09-22: liquid-floor impact support

Added shared Heretic solid/water/lava/sludge classification using the five pinned
terrain flat names. Floor impacts above a liquid sector's actual floor return
solid without emitting particles or consuming RNG. Water and sludge produce a
base splash plus a low-gravity particle; lava produces a splash and rising smoke.
Water/lava sounds use licensed WAD samples. Particle launch randomness follows
the pinned P_HitFloor order and effects expire through Heretic actor states.

Phoenix explosions now invoke this floor-effect path after radius damage, closing
the previous missing liquid-splash checkpoint. As in the reference A_Explode,
the splash is emitted on the sector floor even for an explosion above it.
Cosmetic horizontal clipping uses the existing preview effect path; it is not
bit-identical legacy particle collision. Liquid effects do not damage actors or
activate lines. Firemace bounce/sink logic can use the returned floor type but
Firemace firing is not enabled by this checkpoint.

Tests cover all five terrain names, raised-edge exclusion without RNG changes,
spawn RNG counts, sound counts, chunk landing states, rendering, cleanup and
actual Phoenix impact integration. Existing Doom compatibility hashes remain
required. In-editor visual/audio testing remains outstanding.

### 2026-09-22: native Firemace projectile physics

Added the three normal Firemace projectile definitions to the isolated projectile
runner. Fast balls switch to seven-unit horizontal speed and low gravity after
sixteen state ticks. Small balls bounce once; lobbed balls bounce with three-quarter
vertical velocity and emit two owner-preserving lateral fragments while they have
enough upward speed. Weak bounces and actor/wall impacts enter death states. All
three types sink on liquid floors through the shared splash path.

Mace floor impacts preserve vertical momentum until their state action runs,
separately from the existing explosive missile path. Low gravity updates once per
simulation tick. Motion continues to use the preview's bounded collision substeps,
so it does not claim bit-identical legacy physics. Bounce and impact samples come
from the licensed bundled WAD. Powered death balls remain unsupported.

Regression tests cover drop timing, bounce restitution/limits, splitting and
fragment ownership/angles, all three liquid types for each projectile, actual
Clink damage, and cleanup. Existing Doom compatibility baselines remain required.
Firemace firing, randomized lob selection, pickups and the preview weapon binding
are the next milestone; they are not enabled by this physics checkpoint.

### 2026-09-22: normal Firemace weapon integration

The combat preview now connects the normal Firemace to its native projectile
physics. Firing uses the pinned four-tick initial windup and three-tick burst
shots, consumes one ammo per shot, and selects a lobbed ball when the simulation
random byte is below 28. Fast shots retain spread/weapon jitter and the sixteen-tick
drop timer; lobbed shots inherit half the player's horizontal momentum and use
pitch-dependent launch height/vertical speed. Normal shot sounds use the bundled
licensed WAD. Powered death balls remain disabled.

Preview weapon pickups grant 50 ammo, small/large ammo pickups grant 20/100,
capacity is 150, and existing difficulty bonuses apply. Selection and empty-ammo
fallback follow the reference rankings among implemented weapons. Enable
TestCombat and collect a Firemace, or also enable TestMace for a test grant, then
press 7. Map pickup locations are used directly for this isolated preview;
reference campaign random placement/absence and deathmatch relocation remain
pending. Player feet-clipping compatibility is also not complete.

Tests cover ownership, forced fast/lobbed branches, windup and burst timing,
last-ammo fallback, replay RNG/projectile states, map weapon/ammo pickups,
difficulty bonus, capacity and selection ranking. Existing five-WAD Doom
compatibility baselines remain required. In-editor playtesting, powered weapons,
full enemy behavior, campaign integration, saves and multiplayer are outstanding.

### 2026-09-22: collectible Clink ammo drops

Clink death drops now create native collectible Hellstaff ammo actors instead of
only emitting a test event. Accepted drops retain the reference spawn/velocity
RNG order, launch from the corpse midpoint, use gravity and ground friction,
and land with liquid-floor effects where appropriate. The drop event remains
available to regression observers. Drop chance remains the pinned 84 threshold.

Dropped ammo uses its stored amount through the existing ammo pickup path,
including difficulty bonuses, capacity, pickup sound/flash and actor unlinking.
A full ammo reserve leaves the item available; ammo does not grant its weapon.
Motion and animation continue after player death, while collection remains gated
by the existing living-player path. Preview drop movement uses shared collision
with stop-on-block behavior rather than full legacy item sliding.

Tests cover a real Clink kill creating exactly one drop, initial toss/landing,
RNG consumption and deterministic replay, full-cap retention and capped collection,
removal and sound. Existing Doom simulation/render baselines remain required.
Other enemy families, artifact drops, full campaign/saves/multiplayer and in-editor
playtesting remain outstanding.

### 2026-09-22: healing artifact inventory

The combat preview now spawns and collects Quartz Flasks and Mystic Urns into
separate stored counts, capped at sixteen of each. Collecting an artifact plays
ARTIUP, gives pickup feedback and runs the non-respawning DEADARTI animation
before unlinking its actor. Full inventory leaves the map artifact available.
Both artifact types use the existing item bob animation before collection.

Q uses one Quartz Flask (+25 health); U uses one Mystic Urn (+100 health), capped
at 100. Successful use synchronizes player/body health, consumes one stored item
and plays ARTIUSE. Full-health or dead-player use does not consume an item, and
dead players cannot acquire one. The preview shows both inventory counts. Artifact
commands retain short frame taps and are consumed only once during tick catch-up.

Regression coverage checks both map pickups, storage caps, full-health retention,
healing/capping, animated pickup cleanup, short-tap input and death gating. Existing
Doom compatibility baselines remain required. This is the manually used healing
subset: automatic emergency healing, the general inventory selector/HUD, other
artifacts, deathmatch respawning, persistence and networking remain outstanding.
In-editor playtesting is still required.

### 2026-09-22: Baby-difficulty emergency healing

Lethal incoming damage on Baby difficulty now attempts automatic healing after
difficulty halving and armor absorption. It uses the minimum sufficient Quartz
Flasks first, otherwise sufficient Mystic Urns, otherwise a sufficient combination.
Health/body values remain synchronized before normal damage/death handling. Auto
use retains the reference silent behavior rather than playing manual-use audio.
Other single-player difficulties do not auto-use these artifacts.

The reference mixed-inventory branch can overdraw flasks and remove the wrong
slot. This implementation deliberately corrects that behavior: it consumes only
owned quantities and decrements urns from their own count. Insufficient inventory
is left untouched. Calculations use wide intermediates for extreme damage.

Tests cover damage/armor order, flask/urn priority, mixed inventory, exact survival
thresholds, insufficient supplies, damage feedback, death flags, other difficulties
and extreme damage. Existing Doom compatibility hashes remain required. Deathmatch
and chicken-player behavior remain gated with those unfinished systems; in-editor
playtesting is outstanding.

### 2026-09-22: Wings of Wrath inventory and flight

Added collectible Wings of Wrath to the preview artifact inventory, with a
sixteen-item cap, bobbing map pickup, pickup animation, sound and stored count.
The artifact command enum now represents both healing items and Wings. Press G
to use Wings, R/F to rise/descend and End to land. Attempting to fly up without
active flight also uses a stored Wings item, following the reference behavior.

Successful use grants 2100 ticks (60 seconds), enables flight/no-gravity and gives
a grounded player an initial upward impulse. It refuses replacement while more
than 128 ticks remain, allowing a refresh during the reference blinking window.
Landing keeps the remaining power and permits resuming flight without consuming
another item. Expiration restores gravity and starts view centering when airborne;
death clears flight. The preview displays stored Wings and remaining seconds.

Tests cover map collection/caps, manual and fly-up use, full duration and refresh
threshold, takeoff, landing/resume, expiration flags, death and retained one-shot
input. Existing healing and Doom compatibility checks remain required. Other
artifacts, full inventory HUD, campaign persistence/networking and in-editor
playtesting remain outstanding.

### 2026-09-22: Ring of Invincibility

The native preview adds Ring map pickups, storage capped at sixteen, pickup
animation/sound and manual use with I. Successful use consumes one Ring and grants
1050 ticks (30 seconds) of invulnerability. Refresh is rejected above 128 remaining
ticks. The shared renderer uses the inverse colormap while protected and the
reference eight-tick blink bit near expiration; blinking off does not remove
protection. The preview reports stored Rings and remaining seconds.

Incoming damage below 1000 after difficulty halving is blocked before armor and
automatic healing, preserving health, armor, inventory and damage feedback. Larger
hits bypass protection. Normal expiration restores the colormap; the preview also
clears the power/view on death. This damage gate covers existing environment,
Clink melee and Phoenix self-splash paths; it does not prevent their independently
applied momentum effects.

Tests cover pickup/cap behavior, full duration, refresh, rendered inverse pixels,
blink phases, armor/healing preservation, post-expiration damage, Baby threshold
ordering, high-damage bypass and death cleanup. Existing Doom baselines remain
required. Other artifacts, full inventory HUD, campaign/network/save support and
in-editor visual/audio playtesting remain outstanding.

### 2026-09-22: Torch inventory and lighting

Added collectible Torches with sixteen-item storage, bobbing pickup, pickup
animation/sound and use through T in the combat preview. Successful use grants
4200 ticks (120 seconds); another Torch is rejected until the final 128 ticks.
The preview displays stored Torches and remaining time.

Torch lighting varies through colormaps 1-7 on simulation ticks and uses a
separate session-local visual random generator, preserving gameplay RNG. The
reference timing gate, target/delta flicker and final blinking phases are adapted
from P_PlayerThink. The preview starts at colormap 1 and retains a bounded Torch
map beneath Ring priority, so Ring expiration cannot leave Torch lighting stuck
on the inverse map. This is an intentional initialization/state-isolation choice,
not a claim of exact legacy cosmetic-random output. Rendering never advances it.

An active Ring overrides Torch lighting even during the Ring's dark blink. Torch
time continues to expire underneath it; lighting resumes when the Ring ends.
Normal expiration and death restore ordinary lighting. Tests cover visible pixels,
flicker, RNG isolation, render independence, Ring priority, full duration,
refresh/blink thresholds, death and map pickup/cap behavior. Existing Doom
baselines remain required. Other artifacts, full HUD, persistence/networking and
in-editor playtesting remain outstanding.

### 2026-09-22: Chaos Device and shared teleport effects

Added collectible Chaos Devices with a sixteen-item cap, pickup animation/sound
and manual use through H. In the single-player preview the device returns to the
player-one start, restores its facing, clears momentum and applies the teleport
reaction delay. Active flight retains height above the destination floor, clamped
to ceiling clearance; grounded arrival resets pitch.

Line teleports and Chaos Devices now share native departure/arrival fog actors,
TELEPT sound events, interpolation reset and registered-enemy telefrag handling.
Fog expires through its Heretic animation and is unlinked. Destination headroom
is checked before moving or harming an occupant. Unlike the reference artifact's
unconditional success path, missing/cramped destinations retain the item and do
not emit effects. This is an intentional failure-handling improvement.

Tests cover inventory/pickups, return position/facing/momentum, missing/cramped
starts with unchanged RNG/inventory, flight preservation, telefrag success and
rejection, fog/audio and cleanup. Existing Doom compatibility baselines remain
required. Deathmatch destination selection, chicken undo, full campaign/network/
save support and in-editor playtesting remain outstanding.

### 2026-09-22: Time Bomb of the Ancients

Added native Time Bomb inventory, map pickups, a sixteen-item cap and B-key use
in the combat preview. A bomb is placed 24 units ahead of the player and advances
through the Heretic fuse and explosion states: sound at tick 40, blast at tick 46,
and removal at tick 70. The blast uses the existing 128-unit radius damage path,
including visibility, armor, self-damage and registered Clink targets. Explosion
height, shadow removal and liquid-floor effects follow the reference actions.
Armed bombs continue ticking and unlink normally after their owner dies.

Regression checks cover placement, inventory limits, pickup retention/collection,
fuse timing, audio, player/enemy damage and cleanup after death. All five production
Doom WAD simulation/render baselines still pass. This remains preview-only;
general monster support, campaign saves/networking and editor playtesting are
not completed by this checkpoint.

### 2026-09-22: Shadowsphere inventory and ghost power

Added Shadowsphere map pickups, sixteen-item storage and J-key activation in the
combat preview. Ghost status lasts 2100 ticks, can refresh in its final 128 ticks,
and clears on expiration or death. It sets the shared Shadow flag used by existing
ghost-aware projectile collision; ordinary damage is still possible. The weapon
overlay uses Blasphemer's TINTTAB for translucency, sector lighting while ghosted,
and the reference final blink timing. Rendering does not advance the power.

Tests cover pickups/caps, activation and refresh, visible weapon translucency and
blink, full duration, damage and death cleanup. This does not implement additional
monster AI, enemy ranged attacks, multiplayer, saves or full campaign support.
World ghost sprites still use the shared renderer's existing shadow treatment;
Heretic world-sprite translucency and editor playtesting remain outstanding.

### 2026-09-22: Heretic world-sprite translucency

Heretic world sprites carrying the Shadow flag now blend their lit source pixels
with the framebuffer through the bundled TINTTAB. The renderer retains shared
sprite projection, scaling, flipping and wall/floor clipping. Doom continues to
use its original fuzz path. Map actors now preserve the native Shadow flag, which
makes Shadowsphere pickups translucent as well as armed bombs and shadow effects.
This completes the world-sprite rendering gap noted in the Shadowsphere checkpoint.

Regression coverage compares actual framebuffer pixels with the tint-table result
at multiple viewing angles, checks repeatable rendering and pickup flags, and
retains the five production Doom simulation/render baselines. In-editor visual
playtesting, full monster support, campaign, saves and multiplayer remain pending.

### 2026-09-22: Bag of Holding

Added collectible Bag of Holding map items to the native combat preview, including
item bobbing, pickup sound/flash, removal and preview status. The first bag
doubles all six ammo capacities; subsequent bags never multiply them again.
Each bag supplies 10 Gold Wand, 10 Dragon Claw, 5 Crossbow, 20 Hellstaff and 1 Phoenix
ammo before the existing Baby/Nightmare bonus and integer rounding. As in pinned
p_inter.c, bags grant no Firemace ammo and remain collectible at full capacity.
Ammo grants do not confer weapon ownership. Ordinary ammo and weapon pickups now
respect the increased capacities; dead players cannot collect bags.

Regression checks cover normal/doubled caps for all ammo types, repeat pickups,
reference grant amounts, difficulty scaling, map pickup feedback, ownership and
death gating. Existing Doom compatibility baselines remain required. This is still
an isolated preview checkpoint; campaign inventory persistence, multiplayer and
in-editor playtesting remain outstanding.

### 2026-09-22: Preview automap and Map Scroll

Added a native player-following automap to the Heretic preview. Tab toggles it;
Z zooms in and X zooms out, with bounded zoom independent of simulation timing.
It uses shared map geometry and DrawScreen line clipping/rasterization. Discovered
walls, floor/ceiling changes and locked doors use Heretic palette colors. Map Scroll
pickups reveal previously unseen lines in gray without exposing never-see lines or
changing their discovered flags. Duplicate scrolls remain on the map. Pickups bob,
emit item feedback and retain the reveal power for the current session.

The preview uses a flat parchment-colored background and compact directional
player marker. Textured parchment, reference antialiasing/sword marker, panning,
marks, full HUD, campaign persistence and multiplayer remain separate work. The
simulation continues while the automap is open. Rendering the map does not itself
reveal additional walls or advance gameplay RNG. Tests cover discovery/reveal,
hidden lines, visible rendering, zoom bounds, return to 3D and pickup/duplicate
behavior. In-editor input and visual playtesting remain outstanding.

### 2026-09-22: Automap free panning and numbered markers

Added O-key follow/free-pan switching, arrow-key panning and M/C marker controls.
Free panning is bounded to map vertices and scales with elapsed time and zoom.
Returning to follow immediately recenters on the player. While the map is open,
arrow input navigates the map instead of turning or pitching the player; WASD
movement and the running simulation remain available. A crosshair identifies the
free-pan center, and the player marker stays at its actual map position.

Up to ten numbered world-position marks use the bundled IN0-IN9 digit patches;
after ten, new marks replace slots cyclically. Clearing marks also resets the next
slot. Marks, pan and zoom remain local preview UI state across map toggles. Tests
cover pan bounds, frame-rate independence, follow behavior, marker cycling and
visible clearing without changes to discovery. Full HUD/artwork, campaign/save
integration and in-editor input/visual playtesting remain outstanding.

### 2026-09-22: Powered staff preview

Added the native level-two staff state chain and attack behind the explicit
Test Powered Staff editor property (requires Test Combat; select staff with 1).
It deals 18-81 damage, can hit ghosts, applies the dedicated ten-unit horizontal
thrust and five-unit upward thrust to gravity-affected targets, and uses the stationary
powered puff rather than the rising normal puff. Idle crackle and powered impact
sounds now decode from bundled assets. Other weapons retain their normal states.

This is a powered-weapon implementation checkpoint, not a completed Tome of Power:
no Tome pickup, duration/expiration or other powered weapons are enabled here.
Tests cover selection/ready states, damage against ghosts, thrust, puff/audio,
ammo independence, switching back to the ordinary wand and death lowering. Existing
Doom baselines and the full regression suite remain required. In-editor playtesting,
remaining powered attacks and campaign/save/multiplayer support remain outstanding.

### 2026-09-22: Powered Gauntlets preview

Added the powered Gauntlets state table and attack behind Test Powered Gauntlets
(requires Test Combat; select with 8). The attack retains 2-16 damage, increases
reach to 256 units, narrows random angular spread and uses the powered puff/sound.
Successful actor hits heal half the rolled damage, capped at 100 health, matching
the reference rather than limiting healing to the victim's remaining health.
Body and player health stay synchronized. Normal Gauntlets remain unchanged.

Tests cover extended-range hits versus normal misses, damage, healing and its cap,
puff/audio, ammo independence, switching and dead-player gating. The option grants
Gauntlet ownership only for the explicit preview test. Tome pickup, power duration/
expiration, other powered weapons, campaign/save/multiplayer integration and
in-editor playtesting remain unfinished.

### 2026-09-22: Powered Crossbow preview

Added Test Powered Crossbow (requires Test Combat; select with 3), using the native
level-two weapon states and five-bolt volley for one ammo. The three stronger bolts
use MT_CRBOWFX2; two outer bolts use MT_CRBOWFX3. Powered bolts execute A_BoltSpark
and spawn native spark animations with reference random offsets and low gravity.
Effects expire and unlink through the existing effect lifecycle. Normal Crossbow
firing remains unchanged. As with the existing preview projectiles, collision uses
bounded substeps rather than exact legacy movement; spark spatial links are set at
their final randomized position.

Tests cover bolt counts/types, ammo depletion and fallback, owner exclusion,
enemy damage, spark gravity/cleanup and deterministic spark replay. The full Doom
compatibility suite remains required. This is an explicit powered-weapon preview;
Tome pickup/duration, remaining powered weapons, campaign/save/multiplayer support
and in-editor playtesting remain outstanding.

### 2026-09-22: Powered Gold Wand preview

Added Test Powered Gold Wand (requires Test Combat; select with 2). Each powered
shot spends one ammo, launches two MT_GOLDWANDFX2 side missiles and fires five
1-8-damage traces in the reference fan. All use the bullet aiming slope; the side
missiles keep their explicit angles and source-height offset rather than acquiring
independent targets. Traces use powered impact puffs and the existing firing sound.
Normal weapon behavior stays on the original state table.

Tests cover trace/missile counts, angles, shared slope, damage/impacts, free-look
fallback, last-ammo switching, cleanup and dead-player gating. Existing bounded
projectile substeps remain in use; player feet clipping is still incomplete.
Tome pickup and duration, remaining powered attacks, campaign/save/multiplayer
support and in-editor playtesting remain outstanding.

### 2026-09-22: Powered Dragon Claw preview

Added Test Powered Blaster (requires Test Combat; select Dragon Claw with 4).
Powered shots cost five ammo and use native projectile states, an eighth-step
spawn advance, eight movement increments per simulation tick and random smoke.
Explosions spawn eight radial rippers with owner attribution. Rippers damage and
pass through eligible targets including ghosts, and emit blood
and the rip sound. Floor impacts trigger terrain effects. Low ammo prevents
selection/firing and falls back without consuming the remainder.

Tests cover ammo thresholds/cost/fallback, moving projectile damage and cleanup,
owner exclusion, eight-ripper spawning, piercing and ghost damage, blood,
smoke and normal-Claw isolation. The existing preview effect movement stops blood
at walls rather than reproducing every legacy cosmetic physics detail. General
pushable actors are not yet supported. Tome pickup/duration, remaining powered
weapons, campaign/save/multiplayer integration and editor playtesting remain open.

### 2026-09-22: Powered Phoenix Rod preview

Added Test Powered Phoenix (requires Test Combat; select with 6), using the native
windup, sustained-fire and shutdown state chain. The burst counter starts at 350,
emits up to 349 flames and charges one ammo at shutdown. Releasing or switching
finishes shutdown; continued holding does not automatically restart a finished
burst. Death interrupts firing and existing flame actors continue to expire.

Flames use native randomized spawn offsets, pitch and inherited horizontal player
momentum. Flame-end and impact-puff actions add upward motion, with collision,
actor damage, animation cleanup and powered firing sound. The preview remains
limited to registered ordinary combatants: player freezing, specialized fire death
animations and full feet clipping are not implemented by this checkpoint.

Tests cover windup, deferred ammo cost, maximum burst, release, switching, damage,
sound, owner exclusion, cleanup and death. Tome pickup/duration, remaining powered
weapons, campaign/save/multiplayer support and editor playtesting remain open.

### 2026-09-22: Powered Firemace preview

Added Test Powered Mace (requires Test Combat; select with 7). Each shot costs five
ammo and launches a native MT_MACEFX4 death ball. Surviving spawns inherit player
horizontal momentum and pitch-based vertical momentum, retain the initial aim
target and use low gravity. Solid-floor bounces redirect toward a live target or
search sixteen directions when no target is held; dead targets are discarded.
Liquid impacts remove the ball, while wall/actor impacts finish its death animation.

Death balls apply lethal damage only through the supported ordinary-enemy path.
Boss/iron-lich exceptions and multiplayer-player invulnerability/Chaos Device
escape require their respective future actor handlers and are not enabled here.
Tests cover five-ammo selection/cost/fallback, owner/target, ordinary lethal damage,
bounce seeking and sound, dead-target clearing and water/lava/sludge removal.
Tome pickup/duration, powered Hellstaff, campaign/save/multiplayer support and
in-editor playtesting remain unfinished.

### 2026-09-22: Hellstaff seeker foundation

Added the native Heretic homing calculation from the pinned GPL source: shortest
turn direction with the reference wraparound arithmetic, threshold/maximum turn,
fixed-point horizontal speed and vertical interception only when actor heights do
not overlap. Dead targets are cleared without altering flight momentum. The
Hellstaff wrapper preserves Heretic's ANG1_X constants rather than rounded degrees.

Regression fixtures cover both turn directions, threshold and cap behavior,
wraparound, overlapping heights, elevated/lowered targets, minimum travel time,
and dead/missing targets. This is a tested prerequisite; powered Hellstaff firing
remains disabled until rain creation, ownership limits, lifetime and impacts are
implemented. Existing weapon behavior is unchanged.

### 2026-09-22: Powered Hellstaff preview

Test Powered Skull Rod (with Test Combat; select 5) enables native powered
Hellstaff shots. Each costs five ammo and launches a seeking MT_HORNRODFX2.
Impact runs the original explosion frames, hides the actor above its ceiling and
produces red single-player rain for up to 140 storm ticks. Two storms are tracked;
a third shortens the older tracked storm to at most sixteen remaining ticks.
Falling rain inherits its firing owner, damages supported ordinary enemies and
uses separate airborne/floor impacts, including probabilistic liquid splashes.
Firing, impact and periodic rain sounds come from the bundled licensed WAD.

The new rain counter is deterministic and separate from the seeker target (the C
reference reused a union for those roles). Expired storm references are also
cleared after player death. The existing bounded projectile movement is retained.
Tests cover ammo/fallback, owner/target, homing action, storm replacement and
lifetime, rain collisions, liquid effects, sound cadence, deterministic replay
and cleanup after death. All five Doom compatibility fixtures remain unchanged.

Boss-specific rain damage, D'Sparil teleport avoidance and network player colors
await their respective actor/multiplayer support. Tome pickup and duration,
campaign progression, saves, multiplayer and in-editor playtesting remain open.

### 2026-09-22: Tome of Power preview

Map thing 86 (MT_ARTITOMEOFPOWER) now joins the opt-in combat inventory with the
sixteen-item cap, floating pickup presentation, artifact pickup animation and
licensed sounds. K uses a collected Tome. The preview displays inventory count
and remaining power time. Its 1400-tick duration is forty seconds at 35 Hz;
refresh is accepted only at 128 ticks or less. Rejected use retains inventory.

The timer selects powered state tables and ammo costs for all eight implemented
player weapons without granting ownership or ammunition. The shared Gauntlet
action also uses the timed power for range, healing, spread, impact and sound.
Staff/Gauntlet activation immediately selects their powered ready animation;
expiry schedules lowering and raising with normal animations. Phoenix expiry
interrupts an active powered cycle, charges its deferred ammo and clears refire;
idle expiry does not charge ammo. Ammo is clamped at zero on that transition.
Other attacks already in progress retain their state chain, as in the reference.
Already-fired powered projectiles finish independently of Tome expiry.

Death clears the timer. Explicit Test Powered weapon switches remain independent
preview overrides. Regression checks cover pickup/full inventory, activation,
refresh boundaries, exact duration, weapon ownership and powered selection,
ammo gates, melee transitions, Phoenix active/idle expiry, existing projectiles
and death. Existing Doom compatibility checks remain unchanged.

The Tome's chicken reversal awaits player morph support. Complete artifact HUD,
remaining actors/artifacts, campaign progression, saves, multiplayer and
in-editor playtesting are still unfinished; production Heretic loading is gated.

### 2026-09-22: Artifact inventory overview and Tome indicator

V toggles a native rendered inventory overview in the combat preview, including
over the automap. Nine fixed slots show the supported artifacts, their existing
shortcut keys (Q/U/G/I/T/H/B/J/K), and explicit counts from zero through sixteen.
The panel uses ARTIBOX, artifact icons, FONTA letters and SMALLIN digits from the
bundled Blasphemer WAD. This preview overview does not implement the original
seven-slot scrolling inventory selector.

An active Tome displays the reference SPINBK animation at the top right, advancing
every three simulation ticks and blinking during the final 128 ticks. Neither
opening the panel nor rendering it consumes inventory, advances a timer or draws
from gameplay randomness. The overlay clears after death or when closed, while
expired Tome indicators disappear. The software render path is shared with the
existing preview; no new artwork or third-party dependency is introduced.

Regression checks cover all nine slots and zero/one/two-digit counts, shortcut
artwork, animation/blink timing, read-only repeated rendering, panel closure,
automap overlay and death. The full health/ammo/status bar, flight indicator,
scrolling inventory selection, morphing and production Heretic integration remain
unfinished.

### 2026-09-22: Morph Ovum projectile foundation

Added an internal five-egg volley using native MT_EGGFX animation, speed and
per-shot autoaim. The center shot is followed by pairs at plus/minus ANG45/6 and
ANG45/3, matching P_UseArtifact/P_SPMAngle. Eggs preserve firing ownership, collide
with ghosts, respect vertical separation, and run their finite impact animation.
An egg hit consumes the reference missile damage roll and dispatches a morph
request without ordinary damage, thrust or kill accounting.

This checkpoint deliberately does not expose Morph Ovum pickups or activation.
The request hook awaits native actor replacement, chicken behavior, timed
restoration, blocked-restoration retry and player/boss rules. No normal game or
preview control can fire eggs until that work is integrated. Tests exercise the
internal volley, spread/speed/aim, owner/ghost/height handling, random ordering,
absence of ordinary damage, floor impacts and cleanup. Existing Doom behavior
remains protected by the five WAD compatibility fixtures.

### 2026-09-22: Registered enemy chicken lifecycle

Added an internal Clink-to-chicken lifecycle with native chicken state tables,
health/dimensions, sounds, attacks, pain/death feathers and ordinary damage.
Replacement preserves position, angle, target and initial ghost status, unlinks
the old body and removes its shootable/solid flags without recording a kill.
Chicken actions reduce the reference forty-second-plus-random duration. Expiry
probes the original Clink dimensions and restores spawn health if it fits;
blocked restoration retries after five seconds. Restoration preserves angle and
target and emits teleport fog. A dead chicken stays dead and records one kill;
it does not execute Clink's ammo drop.

The failed-fit path retains the existing chicken object and state atomically
instead of deleting/recreating it as the C reference does. It therefore avoids
that reference's failed-spawn RNG and momentum reset. Chicken pursuit shares the
existing limited Clink preview movement; full vanilla chase behavior remains
unfinished. Feather motion shares native low-gravity particle clipping/landing.

Opt-in internal egg dispatch exercises this lifecycle in regression checks.
Morph Ovum inventory/controls remain gated while player morphing and remaining
actor exclusions are integrated. Tests cover replacement and stale-body flags,
identity/target/ghost handling, timer bounds, blocked restoration/retry, egg
integration, state ticking, single kill accounting and feather cleanup. Existing
Doom compatibility results remain unchanged.

### 2026-09-22: Morph Ovum combat-preview integration

Map thing 30 (MT_ARTIEGG) is now collectible in the combat preview. Morph Ovum
uses the sixteen-item cap, full-inventory retention, floating artwork and normal
artifact pickup animation/audio. L consumes one collected item and fires the
native five-egg volley. Real egg collisions enable the registered Clink chicken
lifecycle, including timed restoration and blocked-space retry. Empty inventory,
death and navigation-only sessions reject activation without consuming an item.

The V inventory overview now has ten slots, with the licensed ARTIEGGC icon,
L shortcut and explicit zero/one/two-digit count. Slot spacing fits the complete
row inside the 320-pixel frame. Tests cover actual command-driven use, moving
eggs hitting a Clink, morph/restore, item consumption/audio, cap/pickup retention,
rejection paths and all ten HUD slots. No new external assets are introduced.

This enables enemy morphing only for the registered Clink preview encounter.
Player morphing, beak controls, Tome chicken reversal, additional monsters/bosses,
campaign progression, saves and multiplayer remain unfinished. Production
Heretic loading stays gated.

### 2026-09-22: Player beak weapon foundation

Added internal beak activation and native normal/powered attacks to the existing
weapon controller. Peck attacks use the reference 64-unit melee trace, normal
1-4 damage or powered 4-32 damage, target facing, rising MT_BEAKPUFF, one of three
licensed peck sounds and randomized attack-state duration. The peck counter
moves the weapon overlay and settles after release. Beak mode locks ordinary
weapon selection and uses no gun ammunition; death lowers and hides it.

The timed weapon power selects super-chicken attacks without an additional test
flag. Expiry returns subsequent attacks to the normal table. All beak entry
points remain internal until player body replacement/restoration is complete;
this does not yet expose player transformation or change the normal player's
body animation, camera height or movement. Existing Doom weapon paths are
unchanged. Regression checks cover activation/switch restrictions, both damage
ranges, ghost contacts, melee distance, timing, peck motion, sounds/puffs,
release, expiry and death.

### 2026-09-22: First-person player chicken lifecycle

Test Player Chicken (with Test Combat) starts the preview in chicken form.
Transformation replaces the linked player body, equips the beak, sets thirty
health and a 24-unit body height, removes armor/invisibility/Tome power, and
preserves flight. The viewpoint is twenty units lower and movement uses the
reference chicken thrust. Idle twitch/hop/noise behavior uses gameplay ticks.
The previous weapon is retained for restoration.

The forty-second timer attempts restoration to a normal 56-unit body and full
health. Successful restoration reapplies flight, restores the prior weapon and
adds the reference reaction delay/fog. A blocked fit retains the current chicken
body and retries after two seconds. Like the enemy path, failed-fit checks are
atomic and do not recreate the chicken or consume failed-spawn RNG. Invulnerability
prevents the initial morph; a repeat hit after the first second can grant powered
beak attacks. Healing is capped at thirty while transformed and Baby automatic
healing is disabled, following the reference rules.

A Tome reverses chicken form instead of starting its normal weapon-power timer.
If the normal body cannot fit, Tome use consumes the artifact and applies the
reference fatal-damage rule. Dead chickens do not restore themselves. The Tome
indicator is hidden for super-chicken power. Tests cover body/camera ownership,
health/armor/powers, flight, weapon restoration, exact timer expiry, blocked fits,
invulnerability and successful/fatal Tome reversal. Existing Doom compatibility
checks remain unchanged.

This is first-person preview integration. Network player targeting/color and
third-person player animation, full campaign transitions, saves and multiplayer
remain unfinished. Production Heretic loading remains gated.


## 2026-09-22: combat status HUD

The native Heretic combat preview now draws health, armor and current-weapon
ammo using Blasphemer's bundled font. Staff/Gauntlets show a dash for ammo;
chicken form shows BEAK. Death health is clamped to zero. The compact readout
sits above the artifact shortcuts and remains visible on the automap. It reads
simulation state without changing RNG or advancing time. This is a preview
layout, not a recreation of the original Heretic status bar.

Regression checks cover all six ammo pools, melee/chicken state, health/armor,
death, inventory spacing and read-only rendering. Production Doom remains
unchanged. Full monster/campaign, save and multiplayer integration is unfinished;
production Heretic loading remains gated.


## 2026-09-24: key and flight HUD indicators

The combat preview displays collected yellow, green and blue key icons above
its status readout, including on the automap. Wings of Wrath now show an active
flight icon with a three-tick frame cadence and the reference final-128-tick
blink rule. Landing freezes the wings immediately at frame 15; this intentionally
simplifies the reference renderer's frame-history-dependent settling animation.
Blasphemer 0.1.8 supplies identical artwork for all sixteen flight frames, so
the wings look static with this IWAD. The display reads simulation state and never advances gameplay or RNG. Death
hides the flight indicator. All artwork comes from the bundled Blasphemer IWAD.

Checks cover individual key slots and removal, airborne frame rendering, landed freeze,
expiry blinking, inventory/automap overlays and existing Doom compatibility.
Production Heretic remains gated; full monster/campaign, saves and multiplayer
integration are still unfinished.


## 2026-09-24: base-content Heretic profile detection

Renamed classic Heretic base WADs can now select the Heretic profile from the
combined E1M1, MUS_E1M1, M_HTIC, ARTIBOX and SPFLY0 markers. Only the first
WAD contributes to this signature: add-ons cannot change a Doom base into
Heretic, even when they supply a missing marker or the base is empty. Known
filenames remain a fallback and explicit Heretic selection remains available
for unusual compatible content. The signature is a conservative heuristic,
not validation of every map, asset, or mod feature.

Tests cover each incomplete signature, split-file signatures, explicit Doom
rejection, a renamed copy of the bundled IWAD, preview loading with an add-on,
and a real Doom base with Heretic markers in an add-on. The existing production
Heretic gate remains in place. General Heretic gameplay compatibility is not
complete; extended map formats and advanced mod features remain unsupported.


## 2026-09-24: Golems and authored map combat

The native preview now supports normal Golems (MT_MUMMY) and ghost Golems
(MT_MUMMYGHOST) alongside Clinks. The shared combat controller executes their
native attack, pain and death states, 2-16 melee damage, hit/miss sounds, rising
finite death souls, corpse collision release, and three-round Gold Wand drops.
Sound aliases follow pinned sounds.c, including mumact using mumsit. Player
weapon damage, kill accounting and ghost flags use the existing Heretic paths.
Chicken morphs now restore the original enemy type instead of always becoming
a Clink. Dropped Gold Wand pickups preserve their specified ammo amount.

To try authored encounters, open Assets/scenes/heretic-preview.scene and enable
Test Combat and Map Monsters on SboxHereticPreview. This replaces the synthetic
Clink encounter with supported monsters at map positions and angles, honoring
skill/single-player filters. Calling StartMapCombat twice does not duplicate
actors. Other monster types remain omitted and counted as unsupported; blocked
supported placements are reported separately. The default navigation preview
and existing synthetic Clink test remain available.

Tests cover normal/ghost Golem attacks, native weapon kills, morph restoration,
soul/corpse cleanup, drops, skill filtering, repeated initialization, deterministic
map combat and loading/ticking/rendering all 48 bundled Blasphemer maps.

Pursuit still uses the existing simplified preview movement, not complete vanilla
A_Chase/noise targeting. Nitrogolems, remaining monster families/bosses, campaign
completion, saves and multiplayer remain unfinished. Production Heretic loading
remains gated. This checkpoint does not change Doom rules or its production WADs.


## 2026-09-24: Nitrogolems and enemy homing missiles

Normal and ghost Nitrogolems (MT_MUMMYLEADER/MT_MUMMYLEADERGHOST) now participate
in the native map-combat preview. Their native attack states choose melee or
launch a homing skull. Missile selection checks sight, reaction delay, retaliation
and reference distance probability, with a recovery step after firing. Movement
still uses the simplified preview chase rather than vanilla direction selection.

Skulls launch from their shooter with reference speed, vertical aiming, liquid
feet clipping, invisible-target spread and initial half-step. They retain a
separate homing target, use the Nitrogolem 10/20 ANG1_X steering thresholds,
play MUMHED, and finish native impact animations. Owner contact passes through;
matching monster types block without damage. Player impacts apply 4-32 damage,
reference thrust, Baby scaling, armor, invulnerability and existing death handling.
Collateral monster damage records the shooter as its source. Full monster
infighting/retargeting remains unfinished; preview AI still pursues the player.

Both variants reuse Golem drops/souls and restore their original type after
chicken morphing. Enable Test Combat + Map Monsters in the Heretic preview to
try the expanded roster. Tests cover actual ranged attacks, homing, player
protection/thrust, same-type immunity, morph/death cleanup, 48-map combat smoke
coverage, deterministic replay and unchanged Doom compatibility. Unsupported
monsters and blocked placements remain reported. Campaign, remaining monsters
and bosses, saves and multiplayer are unfinished; production Heretic is gated.


## 2026-09-24: Beast combat and fireball trails

Beasts (MT_BEAST) now spawn in native map-combat mode. Their shared attack
sequence deals 3-24 melee damage up close or launches a straight MT_BEASTBALL
at reference speed with a randomized flame trail. Fireballs use the existing
enemy owner/collision/player-damage path without acquiring a homing target.
Trail effects preserve the reference random draw order, do not block actors,
and expire through native frames. Beast sight/attack/pain/death sounds are mapped.

The AI now checks whether a melee state exists before entering it: Beasts use
their missile attack sequence for both attack ranges. Missile probability also
applies the reference adjustment for actors without a dedicated melee state.
Normal and gib deaths release corpse collision and can drop ten crossbow rounds;
dropped-arrow pickups now honor that amount instead of always granting five.
Beasts restore their original type after chicken morphing.

Enable Test Combat + Map Monsters in the Heretic preview. Tests cover attacks,
fireball speed/ownership, flame trails and cleanup, rendered combat, morphs,
normal/gib death chains and actual ten-round pickups. The shared smoke check
loads, ticks and renders all 48 Blasphemer maps with the expanded roster.

Player deaths still use the existing preview death handling; specialized fire
burn-death animations are not implemented. Full vanilla pursuit/infighting,
remaining monsters and bosses, campaign completion, saves and multiplayer remain
unfinished. Unsupported actors and blocked map placements remain reported, and
production Heretic loading remains gated. Existing Doom rules are unchanged.


## 2026-09-24: Undead Warriors and axe projectiles

Normal and ghost Undead Warriors (MT_KNIGHT/MT_KNIGHTGHOST) now spawn in native
map-combat mode. The authored attack chain performs two attacks: 3-24 melee
damage at close range or axe throws at distance. Normal Warriors choose a red
axe on a random roll below 40/256; ghost Warriors always use red axes. The native
pain/death states, five-round crossbow drop and original-type chicken restoration
are connected to the existing combat lifecycle.

Both axes launch at height 36 (adjusted for liquid feet clipping), travel without
homing, and pass through ghosts. Green axes deal 2-16 damage and red axes 7-56.
Green axe flight sound and red axe blood drips use their native actions. Drips
preserve random draw order and fall with low gravity; existing ripper blood keeps
its separate no-gravity behavior. Impact animations and trails are cleaned up.
Warrior sounds include the active-to-sight alias from pinned sounds.c.

Enable Test Combat + Map Monsters in the Heretic preview. Tests cover normal and
ghost two-throw sequences, melee, both axe choices and damage multipliers, ghost
pass-through, low-gravity blood, morph/death/drop handling, rendered combat and
cleanup. The shared 48-map combat smoke and deterministic replay checks cover the
expanded roster. Unsupported monsters and blocked placements remain reported.
Full vanilla pursuit/infighting, remaining monsters/bosses, campaign completion,
saves and multiplayer remain unfinished; production Heretic loading stays gated.


## 2026-09-24: Ophidian combat

Ophidians (MT_SNAKE) now participate in native map combat. Their authored attack
sequence fires three MT_SNAKEPRO_A missiles at ticks 10, 14 and 18, followed by
MT_SNAKEPRO_B at tick 37. Both fly at speed 14 without homing; the small missile
deals 1-8 damage and the final missile 3-24. Each shot faces the player and emits
the native attack sound. Missing targets return the actor to its walk state
instead of spawning a shot. Sight, pain and death sounds are also mapped.

The native pain/death sequence, original-type chicken restoration and five-round
Phoenix ammo drop use the existing combat lifecycle. Dropped Phoenix ammunition
now honors its stored amount instead of granting the ordinary one-round pickup.
Both projectile types retain native impact animations and shared owner exclusion,
collision, armor, invulnerability and player damage handling.

Enable Test Combat + Map Monsters in the Heretic preview. Checks cover exact
attack timing, both projectile types/speeds/damage, owner exclusion, missing
targets, live attack detection, rendered combat, morph/death/cleanup and actual
five-round pickups. Shared tests load, tick and render the expanded roster across
all 48 bundled Blasphemer maps and check deterministic replay and Doom compatibility.
Full vanilla pursuit/infighting, remaining monsters/bosses, campaign, saves and
multiplayer remain unfinished. Unsupported actors and blocked placements remain
reported; production Heretic loading stays gated.


## 2026-09-24: Disciples and floating enemy combat

Disciples of D'Sparil (MT_WIZARD) now spawn in the native map-combat preview.
Their attack wind-up alternates normal/ghost visibility every four ticks and
ends visible, with either 4-32 melee damage or three MT_WIZFX1 projectiles. Side
shots use the center shot's angle plus/minus ANG45/8 and share its vertical
momentum. A blocked center launch suppresses the side shots. Missiles travel
at speed 18 and deal 3-24 damage through the shared enemy-missile path.

The shared actor tick now respects NoGravity and implements the reference
floating height adjustment toward a target. Death removes flight/ghost flags
and lets the corpse fall. Original-type chicken restoration restores the
Disciple's flight flags. Pain/death actions clear ghost visibility. Native
sounds, ten-round Dragon Claw ammo drops and the rare Tome drop are connected;
drops spawn even when no diagnostic event subscriber is present.

Enable Test Combat + Map Monsters in the Heretic preview. Regression checks
cover floating, visibility timing, three-shot spread/shared slope, projectile
and melee damage, airborne morph restoration, falling corpses, rare Tome and
ammo drops/collection, live combat and cleanup. Shared tests cover the expanded
roster in all 48 Blasphemer maps plus deterministic replay and Doom compatibility.

Horizontal pursuit and obstacle routing still use simplified preview AI; full
vanilla pursuit/infighting, remaining monsters/bosses, campaign completion, saves
and multiplayer remain unfinished. Blocked placements and unsupported actors
remain reported. Production Heretic loading stays gated.

## 2026-09-24: Fire Gargoyle combat and crash deaths

Fire Gargoyles (MT_IMPLEADER) now spawn in the native map-combat preview.
They fly, deal 5-12 melee damage or launch speed-10 fireballs dealing 1-8
damage, and use native animation and sound definitions. Normal and extreme
deaths fall into their respective crash sequences and create exactly two
debris chunks with gravity, floor friction and native animation expiry.
Chicken restoration retains the original enemy type and flight flags.

Regression checks cover melee/fireballs, owner exclusion, live attacks, morph
restoration, grounded/airborne normal and extreme deaths, kill accounting,
debris cleanup and rendered combat. The full suite passes, including all five
production Doom WAD simulation/render hashes. Map smoke tests cover 48 maps,
3,009 supported enemies and 28 reported blocked placements. Licensed asset
inventory and pinned actor-definition reproduction checks pass.

Normal Gargoyles (MT_IMP) remain gated pending charge/collision support.
Remaining monsters/bosses, full pursuit/infighting, campaign, saves and
multiplayer remain unfinished. Production Heretic loading stays gated;
this checkpoint has regression/render verification, not an editor playtest.

## 2026-09-24: Normal Gargoyle charge and melee

Normal Gargoyles (MT_IMP) now spawn in the native map-combat preview. Their
native attack states select a speed-12 charge with vertical aim toward the
target's center. Charge momentum bypasses ordinary friction; actor contact
ends the charge and wall contact recovers on the following tick. Contacts
use Heretic actor states without accessing Doom actor definitions. Native
MT_IMP impact damage is zero; its separate melee attack deals 5-12 damage.
Missing targets and rejected random attack rolls return to pursuit.

Both Gargoyle variants share crash/debris handling, and normal Gargoyles
restore correctly after chicken morph. Regression coverage includes charge
speed, contact, wall recovery, melee, attack rejection, morph and extreme
crash deaths. The expanded map smoke check covers 3,573 supported enemies
across 48 maps, with 28 blocked placements reported.

This remains the opt-in Test Combat + Map Monsters preview. Simplified pursuit,
remaining monsters/bosses, full campaign, saves and multiplayer are unfinished.
Production Heretic loading stays gated; editor playtesting is still required.

## 2026-09-24: Iron Lich ice and fire projectile systems

Added native MT_HEADFX1 ice balls, their eight radial MT_HEADFX2 impact shards,
and MT_HEADFX3 fire columns. Ice travels at speed 13; shards use speed 8,
45-degree spacing and native downward momentum. Ice balls pass through ghosts;
shards do not. Child projectiles preserve their caster and species identity.

The fire column has one active base and five initially harmless layers moving
at speed 10. The layers grow nine units per action for 2/4/6/8/10 actions before
switching to active fire states and native damage. Spawn checks, collision,
impact cleanup and ice/fire sounds use the shared Heretic projectile system.
Tests exercise native animation timing, growth heights, dormant/active damage,
owner/ghost contacts, ice impact splitting, rendered effects and cleanup.

This is an attack-system checkpoint, not Iron Lich roster activation. MT_HEAD
remains gated until whirlwind, attack selection and death behavior are ready.
The preview roster and production launcher are unchanged. General pursuit,
remaining monsters/bosses, campaign, saves and multiplayer remain unfinished.

## 2026-09-24: Iron Lich whirlwind and attack selection

Added native MT_WHIRLWIND spawning, speed-10 bounded seeking, invisible-target
tracking suppression, dead-target release, active sound timing and duration
expiry. Contacts spin/push targets, apply capped lift during the native time
window and deal three damage every eight simulation ticks through the existing
protection/armor path. The effects use the Heretic session clock. Repeated
impacts share the native sound/impact counter; resting at floor height with no
vertical motion is not treated as an impact. Expiry always ends the missile.

Iron Lich attack selection now chooses ice, fire or whirlwind using the native
close/far probabilities and 512-unit threshold, with a separate melee branch.
These routines are tested through controlled combat fixtures. Tests cover
owner exclusion, ghost/dead-target seeking, damage/protection, lift cap,
lifetime/sound cleanup, repeated impacts, floor resting and all six ranged
selection branches.

MT_HEAD remains gated. Its damage exceptions (including morph/death-ball
immunity and Dragon Claw resistance), drops and episode-specific death trigger
must be completed before map spawning is enabled. Existing map roster and
production loading are unchanged; campaign, saves and multiplayer remain
unfinished. No editor playtest is claimed by this checkpoint.

## 2026-09-24: Iron Lich map combat and death behavior

Iron Liches (MT_HEAD) now spawn in the native map-combat preview with all three
attack families and native actor states/sounds. Morph attempts are rejected;
powered Firemace uses ordinary damage instead of an instant kill, and powered
Dragon Claw projectiles/rippers use the reference zero-or-one damage roll.
Death drops use independent ten-round Dragon Claw ammo and Morph Ovum rolls.

The last Lich's death lowers tag-666 floors on E1M8/E4M8. E4M8 also kills
remaining supported monsters. Other maps/episodes do not trigger this action.
Living Liches and failed Lich placements prevent premature completion. The
preview still does not instantiate unsupported monster families, so its
massacre applies to the supported roster only.

Checks cover spawning/rendering, native attack dispatch, damage exceptions,
Ovum drops, multiple-boss/episode gates, scheduled floor movement, corpse
states and live combat. The shared map smoke check covers 3,609 enemies over
48 maps, with 29 blocked placements reported. Run the focused checks with
`dotnet run --project tests/Regression/Regression.csproj -- . --iron-lich`.

Use Test Combat + Map Monsters in the preview. Production Heretic loading
remains gated; remaining bosses, complete pursuit/infighting, campaign, saves
and multiplayer are unfinished. Regression rendering is not an editor playtest.

## 2026-09-24: Maulotaur projectile attacks

Added the five-shot MT_MNTRFX1 spread with speed 20, native 40-unit launch
height, side angles and a shared vertical aim. Added speed-14 MT_MNTRFX2
floor fire and its MT_MNTRFX3 trail, with native random offsets, ownership,
minimal contact-checking motion and finite animation lifetime. Traveling fire
can climb small steps; resting flames do not explode merely for touching the
floor. Impact actions use the reference splash damage (24 for the traveling
fire and 128 for a trail flame impact). Existing Phoenix defaults stay at 128.

Regression checks cover spread angles, speed, slope, height, damage and owner
exclusion, floor launch/step handling, emitted flame ownership/expiry, splash
radius isolation, rendering and cleanup. Focused checks are available with
`dotnet run --project tests/Regression/Regression.csproj -- . --maulotaur`.

MT_MINOTAUR remains gated pending charge, melee/attack decisions, boss damage
rules, drops and episode death handling. This checkpoint adds projectile
systems; it does not add Maulotaurs to the map roster. Production Heretic
loading remains gated. Regression rendering is not an editor playtest.

## 2026-09-24: Maulotaur charge and melee encounter

The isolated Maulotaur encounter now implements native attack decisions,
speed-13 charge momentum, seventeen charge actions with rising puffs, collision
slam/thrust, player reaction-time stun and ordinary-damage immunity during a
charge. Melee attacks apply the native damage and view-height impact. Ranged
actions dispatch the spread and floor fire; floor fire can repeat once.
Maulotaurs reject chicken morph. Charge contact and wall recovery share the
existing Heretic collision path without using Doom actor definitions.

An explicit StartEnemyTest(MT_MINOTAUR) fixture can instantiate the actor;
normal map spawning remains disabled. Tests cover charge selection/speed,
friction exclusion, immunity without RNG consumption, slam/stun/recovery,
timed puff creation/rise, melee view response, ranged dispatch, floor-fire
selection and its bounded repeat. The focused --maulotaur regression option
runs both projectile and charge checks.

Boss-wide damage rules, native drops and episode death handling remain
unfinished; the isolated encounter is not a finished boss implementation.
MT_MINOTAUR remains excluded from SupportsMapEnemy, and production Heretic
loading remains gated. No editor playtest is claimed.

## 2026-09-24: Maulotaur boss integration and map spawning

Maulotaurs (MT_MINOTAUR) now join the opt-in map-combat roster with their
projectile attacks, charge/slam, melee and native boss states. Boss handling
adds splash immunity, ordinary death-ball damage instead of instant kills,
reduced rain damage, no whirlwind lift and protection from whirlwind/slam
effects while charging. Monsters do not retarget a boss merely because it
hurt them. Morph immunity and charge damage immunity remain in place.

Native independent drop rolls produce a Mystic Urn and ten Phoenix rounds.
Shared episode-death handling preserves the Iron Lich rules on E1M8/E4M8 and
adds Maulotaur completion on E2M8/E5M8: wait for the last boss, kill remaining
supported monsters and lower tag-666 floors. Failed boss placements prevent
premature completion. Unsupported monster families remain outside the preview
massacre until their own implementations are enabled.

Regression checks cover damage protections, charging effect exclusion, boss
retaliation rules, drops and Urn collection, multi-boss/episode gates, floor
scheduling, corpse states and live combat. The map smoke check covers 48 maps,
3,612 supported enemies and 29 reported blocked placements. Rendering uses the
bundled Blasphemer artwork. The --maulotaur regression option runs projectile,
charge and boss checks together.

Production Heretic loading remains gated. D'Sparil, complete pursuit/infighting,
campaign, saves and multiplayer are still unfinished. An editor playtest is
still required; regression rendering is not a substitute for that check.

## 2026-09-24: D'Sparil fireballs and blue bolts

Added MT_SRCRFX1 mounted fireballs and MT_SOR2FX1 blue bolts to the shared
Heretic projectile path. Mounted fire launches at 48 units and speed 20, with
single or three-shot helpers; side shots use the native ANG1_X offsets and
share the center shot's vertical aim. Blue bolts use speed 20, direct damage,
two rising sparks per native action, and 80+(random&31) splash damage on impact.
Sparks retain native random momentum and finite animation lifetime. Attack
sounds use the licensed WAD mappings, including the mounted sound alias.

Tests cover launch height, speed/spread/slope, ownership, direct damage,
blue impact splash, spark movement, rendered effects and cleanup. The focused
`--sorcerer` regression option runs these checks.

This is a projectile-system checkpoint. Mounted health-based attack selection,
repeat timing, summoning, teleport destinations, phase transition and final
death behavior remain unfinished. Both D'Sparil actor types remain excluded
from map spawning. Production Heretic loading stays gated; no editor playtest
is claimed.

## 2026-09-24: D'Sparil Disciple summoning

Added the paired MT_SOR2FX2 summoning missiles with native launch angles,
vertical momentum and animation-driven summoning attempts. Successful attempts
create a Disciple at the missile's height, stop the missile and produce teleport
fog and sound. Blocked attempts retain the missile for its next native retry.
Explicit-height spawns validate actor overlap and floor/ceiling clearance before
linking the new enemy. Failed attempts preserve the native LastLook random draw.

Regression checks cover launch parameters, blocked placement, vertical clearance,
retry timing, single-enemy creation, teleport effects, cleanup and the summoned
Disciple entering combat. The focused --sorcerer checks and full regression suite
pass; all five production Doom simulation/render hashes remain unchanged. The
s&box project builds with zero errors and seven existing warnings. Asset inventory
and pinned Heretic definition checks pass. Regression rendering was inspected;
no editor playtest is claimed.

D'Sparil's health-based attack selection, repeat timing, teleporting, phase
transition and final death remain unfinished. Both boss forms remain excluded
from map spawning, and production Heretic loading remains gated.

## 2026-09-24: Mounted Sorcerer attack controller

Implemented A_Srcr1Attack health thresholds, native melee damage, and the
animation-driven low-health repeat. Health above 1332 produces one fireball;
health at or below 1332 produces three. Below 666, the shared native special1
counter permits one repeat through S_SRCR1_ATK4 and then clears. A_Sor1Pain
sets that same counter to 20; A_Sor1Chase shortens each affected walking state
by three tics before invoking the existing preview pursuit handler.

Focused regression tests exercise threshold boundaries, the timed repeat chain,
missing targets, pain/repeat interaction, 20 accelerated walking actions and
melee damage without a projectile. These controller tests use a supported
fixture body: they do not claim a complete mounted boss encounter. Both
D'Sparil forms remain excluded from map spawning until phase transition,
teleportation and final death behavior are implemented. Generic pursuit remains
the existing preview implementation. Production Heretic loading stays gated.

Validation: focused and full regressions pass, including unchanged production
Doom simulation/render hashes. Build: zero errors, seven existing warnings.
Licensed asset inventory and pinned definition checks pass. No editor playtest
is claimed.

## 2026-09-24: Isolated D'Sparil phase lifecycle

Mounted death now reaches A_SorcererRise and creates one native MT_SORCERER2
actor at the corpse's position with its angle and target, 3,500 health, native
rise frames and full-volume rise/sight sounds. The mount becomes non-solid.
Enemy ticking tolerates actors created during another actor's animation; a new
rider starts ticking on the following frame. Duplicate rise dispatch is guarded.
The isolated boss constructor is explicitly opted in; ordinary map spawning and
the generic combatant boss guard remain unchanged.

The second-form controller selects blue bolts or paired summoners at the native
health/probability boundaries, supports melee, and uses type-56 boss spots for
teleport decisions. Teleporting resets momentum, applies the destination angle,
plays sounds and leaves a finite fade at the old position. Occupied destinations
are rejected without telefragging. Headroom is checked and the destination scan
is bounded so malformed all-nearby spot lists cannot hang the simulation.

Second-form death immediately kills remaining supported preview monsters, plays
the native seven-loop animation and death sounds, and leaves a non-solid corpse
without ordinary ammo drops. Both forms reject chicken morphing. Twelve sound
mappings were added using the bundled licensed WAD audio.

Regression coverage includes a real mounted actor's death through rider creation,
position/target/health inheritance, rise timing and rendering, unique spawning,
attack chance boundaries, no-target behavior, absent/too-close teleport spots,
blocked teleport effects, fade cleanup, immediate massacre and seven death loops.

This is still an isolated lifecycle checkpoint. D'Sparil-specific damage reactions
and E3M8 completion need integration/verification before enabling map spawning.
Generic pursuit remains the preview implementation. Production Heretic loading,
campaign, saves and multiplayer remain gated; no editor playtest is claimed.

Validation: full regression suite passes, including unchanged simulation and
rendered-frame hashes for all five production Doom WADs. The game builds with
zero errors and seven existing warnings. Licensed asset inventory and pinned
Heretic definition checks pass.

## 2026-09-24: D'Sparil damage rules and E3M8 completion

Both Sorcerer forms now ignore radius damage, matching the native boss exclusions.
The second form rolls its native 96/256 escape chance when hit by normal Phoenix
or powered Hellstaff missiles, before ordinary damage/thrust. A successful roll
attempts a boss-spot teleport and skips the hit even if no usable destination
exists. This damage reaction bypasses the separate health-based attack-decision
roll. Projectile impact still occurs. The second form does not retarget a Disciple
that damages it; the mounted form retains ordinary retaliation.

The shared episode-death handler recognizes the second form on E3M8, waits for
other living second-form bosses, and schedules tag-666 floor lowering once.
The existing seven-loop death animation now reaches that completion handler.
Regression tests cover splash immunity, death-ball/rain resistance, phase-specific
Disciple retaliation, both damage-escape outcomes, absent spots, living-boss and
episode/map gates, repeated completion, and completion through the real death
animation. Other supported boss episodes retain their existing guards.

Automatic Sorcerer map spawning remains disabled pending map-placement and
boss-spot integration testing. These are isolated preview encounters; production
Heretic loading, campaign, saves and multiplayer remain gated. No editor playtest
is claimed.

Validation: focused and full regression suites pass; all five production Doom
simulation/render hashes remain unchanged. Build: zero errors, seven existing
warnings. Licensed asset inventory and pinned definition checks pass.

## 2026-09-24: D'Sparil preview map encounters

MT_SORCERER1 now joins the opt-in native map-combat roster. It uses the map's
position, angle and difficulty filtering, then creates its second form through
the existing death/rise sequence. MT_SORCERER2 remains a runtime-spawned actor,
not a separately enabled map placement. Repeated StartMapCombat calls do not
spawn duplicate bosses. Failed mounted boss placements prevent a false E3M8
completion, matching the existing safeguards for other supported boss families.

The bundled Blasphemer maps containing mounted bosses are E3M8, E6M1 and E6M3.
Regression encounters cover all three on easy, medium and hard: nine encounters
and 39 successful destination checks across the 13 authored teleport spots.
Tests exercise map placement, the mounted death/rider transition, actual
health-based teleport decisions selecting distant map spots, rendering, final
death, E3M8-only completion, repeated activation and a deliberately blocked
boss placement. E6 boss deaths do not trigger E3M8 completion.

Use the isolated Heretic preview scene with Navigation, TestCombat and MapMonsters
enabled to exercise map encounters. Production Heretic loading remains gated.
Full pursuit/infighting, campaign, saves and multiplayer integration still need
work; these regression checks do not constitute an s&box editor playtest.

Validation: full regression suite passes. Map smoke covers 48 maps with 3,615
supported enemy placements and 29 reported blocked placements. All five
production Doom simulation/render hashes remain unchanged. Build: zero errors,
seven existing warnings. Asset-license inventory and pinned definitions pass.

## 2026-09-24: Native eight-direction chase movement

Heretic enemies now use persistent MoveDir/MoveCount state and the native
P_NewChaseDir search order instead of trying five angles toward the player on
every walking action. Direction selection tries the direct diagonal, preferred
cardinal axes, the old direction, randomized alternatives and finally the reverse
direction. Successful new walks receive the native random 0-15 movement count.
A blocked enemy stops with no direction and retries on a later chase action.

Movement uses the native fixed-point cardinal and 47000 diagonal components.
Ground actors follow the new floor height and emit the existing floor-contact
effect when stepping down. Floating actors can adjust vertically after a failed
move when the shared collision layer permits it, retaining the native InFloat
flag. Doom movement is unchanged.

The focused --chase checks cover all eight step vectors, ground floor following,
diagonal priority and RNG/count, reverse-direction avoidance, complete blocking,
recovery when a blocker is removed, and missing-target/no-direction behavior.
The same checks run in the full regression suite.

This replaces the movement portion of preview pursuit. Monster door activation,
full A_Chase timing/turning, target selection and infighting still need integration.
Production Heretic loading, campaign, saves and multiplayer remain gated. No
s&box editor playtest is claimed.

Validation: focused and full regressions pass, including 48-map smoke coverage
and unchanged simulation/render hashes for all five production Doom WADs.
Build: zero errors, seven existing warnings. Licensed asset inventory and
pinned Heretic definition checks pass.

## 2026-09-24: Monster manual-door activation

Blocked Heretic chase moves now process the collision layer's touched special
lines in native reverse order. Eligible activation clears MoveDir and lets the
enemy wait/retry rather than treating every closed door as an impassable wall.
The native monster whitelist accepts specials 1, 32, 33 and 34, rejects secret
lines and rejects player-only switches, exits, platforms and teleports. Locked
manual doors still refuse to open for monsters, even if the player has keys;
the native accepted-use return value is retained without activating the lock.

Monsters can raise ordinary manual doors and reopen closing ones. They cannot
close doors that are opening or waiting. Monster activation does not alter the
player's key messages. Initial monster-opened doors play the licensed DOROPN
sound. Existing player door behavior retains its original path.

The --chase regression option now also tests monster doors: eligibility and
secret/locked restrictions, no borrowing player keys, opening/waiting/closing
states, opening sound, and a real map collision where an enemy opens a closed
door and walks through it after sufficient clearance develops. These checks
also run in the full suite.

Full A_Chase timing/turning, target selection and infighting remain unfinished.
Production Heretic loading, campaign, saves and multiplayer remain gated. No
s&box editor playtest is claimed.

Validation: focused and full regression suites pass, including unchanged
simulation/render hashes for all five production Doom WADs. Build: zero errors,
seven existing warnings. Licensed asset inventory and pinned definitions pass.
