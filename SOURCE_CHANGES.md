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

### 2026-09-22 - Stereo Heretic encounter output

Adapt pinned GPL s_sound.c separation calculation. Add equal-power interleaved
stereo conversion and native two-channel playback, verified against installed
SDK metadata and orientation/sample-boundary regressions.

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
