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
