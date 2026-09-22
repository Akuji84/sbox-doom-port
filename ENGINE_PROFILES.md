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
