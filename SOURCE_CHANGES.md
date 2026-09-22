# s&Doom source modifications

Managed Doom is adapted for the s&box runtime, rendering, input, audio, menus,
save storage and multiplayer. Original upstream copyright and GPL grants are
preserved. Modified upstream files list project revision dates from Git history.

Changes implemented September 9, 2026 include validated temporary-world save
loading, content identity and integrity checks, more complete save restoration,
same-map rematch synchronization, non-compounding shell volume, explicit key
unbinding, automap input, frame-consistent mouse input and stalled co-op recovery.

September 16, 2026: restrict packaged WADs to Freedoom Phase 1, Phase 2 and
FreeDM 0.13.0; preserve their license/credits; render menu labels from bundled
fonts; add the license viewer and dated upstream modification notices; make
source licensing explicit and SDK build paths configurable. Preserve MIT and
other upstream component notices.

See LICENSING.md for source and component licenses.

September 18, 2026: add Freedom Scoops campaign identification, five-map
progression and endings, shell launch entries and artwork, and upstream content
notices. Secret exits stay within the finished campaign; missing co-op starts
use existing multiplayer spawn locations. Add campaign rendering, save/load,
four-player simulation and WAD-switching regression coverage.

September 18, 2026: introduce immutable Doom/Heretic game profiles and route
definition initialization through the Doom profile. Reject unimplemented Heretic
runtime loading before changing Doom definitions. Add pre-profile simulation and
rendering baselines and profile-isolation checks. See ENGINE_PROFILES.md for the
extension boundaries and upstream source references.

### 2026-09-18 — Heretic asset and map preview

Added an explicit asset-only Heretic loader, family-specific sprite/animation
catalog, classic map validation, a geometry-only World and a standalone s&box
preview scene. Shared renderer and animation code remain used by Doom; fixed
Doom compatibility hashes are retained. Adapted asset tables identify their
GPL-2.0-or-later Chocolate Doom source and retain original copyright notices.

### 2026-09-18 — Heretic navigation checkpoint

Added isolated Heretic player state and fixed-tic navigation, view shifting,
flight, environmental movement/damage, key pickups and line activation tables.
Shared collision and sector movers now expose the small entry points needed by
Heretic. Doom paths retain their defaults. The standalone preview panel supports
keyboard navigation; the public game launcher still excludes unfinished Heretic
runtime support. Adapted GPL code retains the pinned Chocolate Doom notices.

### 2026-09-18 - Heretic navigation follow-up

Retain brief preview input actions across simulation ticks, add Shift running,
and bound stalled-frame catch-up. Correct camera recovery on steps and hard
landings, fixed-angle bobbing, and the full five-entry Heretic liquid terrain
classification, following the previously pinned GPL Heretic reference. Add
frame scheduling, terrain and camera recovery regression checks.

### 2026-09-18 - Heretic actor definition checkpoint

Import family-owned immutable actor/state/weapon definitions from the pinned
GPL reference, retaining its copyright and license header. Add a reproducible
hash-checked importer and a Heretic actor state runner. Use it for key animation
in the navigation scene. Add definition/asset coverage, action dispatch, timing,
removal and in-world key checks. Attacks and enemy AI remain unimplemented.

### 2026-09-18 - Heretic map actor checkpoint

Add map thing classification/filtering based on the pinned GPL p_mobj.c reference,
family-owned map actor instances, deterministic animation phases, floor/ceiling
placement and explicit unsupported/unknown reporting. Spawn action-free scenery
and keys in the navigation scene. Keep Heretic sector-height clipping out of Doom
corpse/damage actions. Add spawn/filter/collision/render checks across all 48 maps.

### 2026-09-22 - Heretic scenery height collision and aiming

Add Heretic-only player/scenery height dispatch before Doom contact actions,
swept vertical scenery collision, stationary actor support, and a side-effect-free
actor/map-line aiming query. Add height, support and targeting regression cases;
update the older collision fixture to explicitly overlap the obstacle vertically.

### 2026-09-22 - Heretic sector riders and environmental death

Carry scenery riders through vertical sector moves and roll back non-crushing
moves that would trap the rider. Add ordinary player death camera/physics from
the pinned GPL p_user.c and p_inter.c reference; preserve ongoing world ticks
when environmental damage kills the player. Add sector-movement and death tests.

### 2026-09-22 - Heretic ordinary monster damage component

Adapt ordinary non-boss monster damage/kill rules from the pinned GPL p_inter.c
reference into a separate Heretic combatant. Validate required state actions
before construction; retain health, thrust, pain and death state without Doom
actor definitions. Add nonlethal, normal/extreme death, action and guard tests.
Map enemy activation and real AI/death-action implementations remain pending.

### 2026-09-22 - Native Clink test encounter

Connect the Heretic combatant to an opt-in native preview encounter. Adapt Clink
melee, pain/death sound requests and drop requests from the pinned GPL p_enemy.c;
add explicitly limited preview pursuit, linked rendering/collision, test-ray
input and Heretic crusher/telefrag dispatch. Expand action-chain validation and
add native encounter and deterministic replay regression checks.

### 2026-09-22 - Normal Gold Wand checkpoint

Adapt normal Gold Wand weapon-state timing, damage, spread, aiming and starting
ammo from pinned GPL p_pspr.c/g_game.c. Replace the opt-in encounter test ray with
this weapon and render Blasphemer weapon frames. Keep weapon states separate from
Doom, expose shot results for later effects, and add ammo/timing/rendering tests.

### 2026-09-22 - Staff and weapon switching checkpoint

Adapt normal staff damage, spread and weapon state transitions from the pinned
Chocolate Doom Heretic p_pspr.c. Add staff/wand selection, lowering/raising,
empty-ammo fallback and retained selection input to the opt-in encounter.
Regression coverage includes actual melee damage, ammo conservation, switching,
rejected unavailable weapons and selection taps between simulation ticks.

### 2026-09-22 - Native weapon impact effects

Adapt normal staff/Gold Wand puff placement and spawning from pinned GPL
Heretic p_map.c and p_mobj.c. Link action-free Heretic effects to the shared
renderer with separate lifetime management, sky suppression and no blocking
collision. Preserve upstream notices; add impact rendering/lifetime tests.

### 2026-09-22 - Native hitscan blood effects

Adapt the blood chance and P_BloodSplatter spawn rules from pinned GPL Heretic
p_map.c/p_mobj.c, plus low gravity. Keep effects in the isolated Heretic lifecycle
with cosmetic wall clipping and terminal impact states; add regression coverage.

### 2026-09-22 - Heretic impact-triggered lines

Adapt the player P_ShootSpecialLine table from pinned GPL p_spec.c and the weapon
trace dispatch order from p_map.c. Reuse isolated Heretic sector/switch handlers,
keep aiming read-only, and add regression coverage for all three impact actions.

### 2026-09-22 - Heretic weapon lighting

Reuse the shared weapon light table for native Heretic overlays. Add optional
palette mapping to patch drawing while preserving existing callers, with dated
modification notices and lighting/transparency regression checks.

### 2026-09-22 - Native Heretic encounter audio

Add validated DMX decoding, explicit enabled Heretic sound names, session sound
events and a bounded s&box preview playback adapter. Use bundled licensed WAD
samples; preserve isolated Doom audio behavior. Add decoding/event regressions.

### 2026-09-22 - Normal Dragon Claw checkpoint

Adapt normal A_FireBlasterPL1 and distinct impact rules from pinned GPL Heretic
p_pspr.c/p_map.c. Add explicit preview loadout, separate ammo, held-attack states,
small/large effects and licensed sound samples; retain powered-mode gating.

### 2026-09-22 - Weapon fallback and ghost interaction

Apply pinned GPL P_CheckAmmo priority/reserve rules to implemented weapons and
p_map.c staff ghost pass-through. Add targeted selection and trace regressions.

### 2026-09-22 - Gold Wand and Dragon Claw ammo collection

Adapt implemented ammo types from pinned GPL p_inter.c P_GiveAmmo and the actor
pickup values. Add opt-in filtered map spawning, collection, caps, difficulty
bonus and pickup sound with regression coverage; preserve navigation-only spawns.

### 2026-09-22 - Dragon Claw map pickup

Adapt the single-player P_GiveWeapon path from pinned GPL p_inter.c for Dragon
Claw. Add filtered spawning, ownership/ammo grants, pickup sound and regressions.

### 2026-09-22 - Healing potion pickup and bobbing

Adapt normal P_GiveBody and healing potion touch rules from pinned GPL p_inter.c,
and item bob offsets/phase from p_mobj.c. Add healing and animation regressions.

### 2026-09-22 - Heretic shields and damage absorption

Adapt P_GiveArmor and the normal armor absorption block from pinned GPL p_inter.c.
Add both filtered map pickups, bobbing, status display and boundary regressions.

### 2026-09-22 - Heretic damage and pickup feedback

Adapt pinned GPL sb_bar.c palette selection and p_inter.c damage/pickup counters.
Apply PLAYPAL feedback in the native preview with simulation-driven decay and
rendering/priority/death regressions.

### 2026-09-22 - Normal Gauntlets

Adapt unpowered A_GauntletAttack and weapon activation sounds from pinned GPL
p_pspr.c, puff motion from p_mobj.c and forward movement from p_user.c. Add an
explicit preview grant, sounds, fallback and native melee regressions.

### 2026-09-22 - Gauntlets map pickup

Adapt pinned GPL single-player P_GiveWeapon and WeaponValue behavior for Gauntlets.
Add filtered map collection, weapon ranking, sound and regression coverage.

### 2026-09-22 - Heretic difficulty and ammo re-selection

Apply pinned p_inter.c Baby damage scaling before armor. Complete P_GiveAmmo's
Gauntlet-to-ranged selection rule, with skill/rounding and ownership regressions.

### 2026-09-22 - Heretic audio attenuation

Adapt pinned GPL s_sound.c approximate distance/SNDCURVE lookup for native preview
playback. Add live volume updates and regression coverage for curve validation,
hearing bounds and wide coordinate differences.
