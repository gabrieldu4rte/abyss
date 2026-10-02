# Abyss

Abyss is a turn-based ASCII dungeon crawler built with Godot .NET and C#. Runtime visuals—including portraits, menus, map tiles, frames, and damage effects—are drawn as monochrome font glyphs with per-character colors. The game supports Portuguese and English.

## Requirements

- **Godot .NET 4.7.2**: use the .NET-enabled editor, not the standard Godot build.
- **.NET SDK 10**: the project targets `net10.0` and references `Godot.NET.Sdk/4.7.2`.
- A desktop environment supporting Godot's OpenGL Compatibility renderer for interactive play.
- Python 3 with Pillow only when regenerating ASCII art or running image comparisons; it is not a game dependency.

## Build and run

Run these commands from the project directory. Set `GODOT_BIN` to the executable from your Godot .NET installation:

```bash
export GODOT_BIN="/absolute/path/to/godot-dotnet"
dotnet build Abyss.csproj --nologo
"$GODOT_BIN" --headless --editor --path . --import
"$GODOT_BIN" --path .
```

The repository also includes a Linux launcher:

```bash
GODOT_BIN="/absolute/path/to/godot-dotnet" ./play.sh
```

The launcher builds C#, imports Godot resources, and starts the game. Its fallback executable path is specific to the original development machine; override `GODOT_BIN` on other machines. Alternatively, import `project.godot` into the Godot .NET editor, build the C# project, and run `Main.tscn`.

## Project layout

```text
Main.cs                     Godot scene adapter and startup/capture lifecycle
Main.tscn                   Single Node2D entry scene
project.godot               Viewport, renderer, and assembly configuration
Abyss.csproj               Godot SDK and .NET target
GlobalUsings.cs             Shared project namespaces
Scripts/
  Domain/                   Character, equipment, dungeon, and expedition state
  Rules/                    Dice rules and unchanged progression formulas
  Application/              Turn coordination, commands, combat, AI, loot, trade
  Presentation/             ASCII drawing, per-screen renderers, UI state, text
  Ports/                    Small interfaces for platform operations and policies
  Infrastructure/           Godot adapters, resource loading, launch scenarios
Tests/
  RegressionSuite.cs        Headless test entry point and shared test helpers
  *Tests.cs                 Tests grouped by subsystem
  Fixtures/                 Current environmental baseline and historical refactor checkpoints
Art/                        Runtime ASCII text and tone maps
ArtSources/                 Source illustrations; not rendered by the game
Tools/convert_ascii.py      Offline raster-to-ASCII converter
Mono.ttf                    Monospaced runtime font
FONT-LICENSE.txt            Font license
```

Namespaces follow the responsibility folders: `Abyss.Domain`, `Abyss.Rules`, `Abyss.Application`, `Abyss.Presentation`, `Abyss.Ports`, `Abyss.Infrastructure`, and `Abyss.Tests`. `Main` remains at the project root and in the global namespace so the existing scene script reference stays stable.

## Architecture and responsibilities

### Scene adapter and composition

`Main` forwards Godot callbacks to application objects. It constructs `GameSession`, loads the font, handles launch arguments, and optionally captures a frame. Gameplay services do not inherit from `Node` and do not own a scene tree.

`GameSession` is the composition root and expedition coordinator. It constructs the shared state and explicitly supplies each service with its dependencies. It owns starting an expedition, descending, completing a turn, and advancing presentation time. The project uses constructor injection without a dependency-injection framework.

### State and domain models

- `PlayerState`: class selection, position, base attributes, health, energy, level, experience, gold, and kill count.
- `InventoryState`: backpack, three gear slots, active torch fuel, and stacked consumable counts.
- `DungeonState`: map tiles, exploration/visibility masks, enemies, pickups, stair room, and merchant location. Spatial queries live alongside the map data.
- `EnvironmentState`: biome, decorative terrain, fixtures, oil, temporary fire, and poison duration.
- `RunState`: seed, turn number, current screen, and aiming state.
- `ExpeditionJournal`: localized event history and the last combat/healing rolls.
- `Gear`, `Enemy`, and `Offer`: equipment definitions, enemy combat/awareness state, and merchant stock entries.
- `HeroCombatStats`: computed combat values from player and equipment state; it receives only these two models.

State is owned by a session rather than static global variables. Mutable collections are exposed within the assembly because generation, combat, and trade cooperate on the same expedition. Domain code uses Godot's `Vector2I` and `Rect2I` as spatial value types, but does not draw or perform scene-tree operations.

### Application services

| Component | Responsibility |
| --- | --- |
| `GameInput` | Key dispatch, held movement, focus-loss cancellation |
| `MenuController` | Menu navigation, confirmation flow, pause tabs |
| `PlayerActions` | Movement, shooting, skills, potions, pickup, interaction |
| `CombatService` | Attack resolution, damage, defeat, reward coordination |
| `ProgressionService` | Experience thresholds and level-up changes |
| `InventoryService` | Initial equipment, requirements, equipment changes, energy potions |
| `LootService` | Chest contents, rarity rolls, enemy gold, equipment rewards |
| `DungeonGenerator` | Procedural floors, merchant rooms, visibility updates |
| `EnvironmentGenerator` | Seeded biome details and safe fixture placement |
| `FloorEventGenerator` | Independent rare floor modifiers and single-elite promotion |
| `EnvironmentService` | Torch throwing, barrel spills, ignition, poison, and turn-based fuel |
| `EnemyAi` | Dispatch to registered enemy behavior policies |
| `EnemyNavigator` | Occupancy-aware breadth-first pathfinding |
| `MerchantService` | Offers, pricing, confirmed purchases and sales |

### Interfaces and extension points

- `IGameHost` isolates redraw requests, quitting, window visibility, fullscreen mode, and resolution changes.
- `IAsciiCanvas` isolates glyph drawing and canvas transforms.
- `ILanguageSettings` isolates persistence of the language preference.
- `ITurnScheduler` exposes turn completion to action services without exposing the whole session.
- `IRunLifecycle` exposes start/descent commands to controllers.
- `IEnemyBehavior` supports independently implemented AI policies. `WardenBehavior` and `RoamingBehavior` preserve the existing behaviors and are registered in the composition root.

These boundaries separate reasons to change, provide substitution points, and keep interfaces focused. A new enemy behavior can be registered without modifying the AI dispatcher. The Godot adapters implement the platform interfaces; tests can substitute implementations. Equipment formulas and the four existing class definitions remain explicit rules rather than introducing a generic rules engine.

### Presentation

`GameRenderer` dispatches the active screen. `MenuRenderer`, `HudRenderer`, `PauseRenderer`, `InventoryRenderer`, and `MerchantRenderer` own their respective layouts. `AsciiCanvas` handles reusable drawing primitives and glyph-picture caches; `UiComponents` supplies shared menu/footer components.

`Localization` provides bilingual labels and descriptions. Journal entries retain both language versions so changing the language also changes the displayed history. `JournalFormatter` wraps entries for the journal panel. `VisualEffects` tracks focused targets and temporary damage effects without advancing gameplay turns.

## Runtime flow

1. Godot forwards a key event to `GameInput`.
2. Menu commands are routed to `MenuController`; expedition commands go to the relevant action service.
3. An action validates its conditions, consumes any resources, and applies its result.
4. Actions that use a turn call `ITurnScheduler.EndTurn`.
5. The session updates visibility, lets enemies act in their existing order, applies periodic energy recovery, and updates visibility again.
6. Godot draws the active screen from the updated session state.

The game remains turn-based. `_Process` advances animation time and the held-key timer, not autonomous enemy turns. Held movement performs an immediate step, waits **0.30 seconds**, and then repeats every **0.16 seconds**, at most once per frame. Releasing the key, another key press, opening a menu, aiming, or losing application focus cancels repetition according to the existing input flow.

## Procedural generation and randomness

- The map is a **64 × 27** tile grid with rectangular rooms connected by corridors.
- Exploration persists within a floor; current visibility uses line-of-sight checks and a radius of ten tiles.
- Floors are unbounded. Every fifth floor contains a Warden that blocks the stairs.
- Eligible non-boss floors after floor one have a 10% merchant encounter chance. A merchant floor is a single safe room with the merchant and stairs.
- `RandomStream` shares one `System.Random` sequence across generation, AI, combat, and rewards. Starting with a fixed seed reproduces a run when supplied with the same actions.
- Random draw order is part of the compatibility contract. Reordering apparently independent rolls can change later encounters and is covered by regression checkpoints.

`RoamingBehavior` patrols, detects the player through line of sight, pursues, and searches the last observed position. `WardenBehavior` activates when the player enters the stair room and remains confined there. Pathfinding preserves occupied-tile restrictions and cardinal movement. Enemy attacks require adjacency; movement and attack are separate enemy actions.

## Combat, equipment, and progression

`TabletopRules` contains attribute modifiers, d20 resolution, critical hits, damage dice, potion rolls, and starting/monster attributes. `GameRules` contains map constants, cycle calculations, chest probabilities, and experience formulas. `HeroCombatStats` combines these rules with equipped gear.

- Sword attacks use Strength; daggers and bows use Dexterity; staves use Intelligence.
- Class abilities use Strength for warriors, Dexterity for archers/rogues, and Intelligence for mages.
- Defense is equipped armor class plus the Constitution modifier; physical and magical attacks use the same defense.
- Compatible staff/bow basic attacks cost no energy. Equipped mages/archers use ranged basics; unarmed attacks retain the existing melee behavior and smaller damage dice.
- Health potions roll 2d10; energy potions roll 2d6. Healing is capped by the missing resource.
- Equipment slots are weapon, armor, and accessory. Rarities are Common, Rare, Epic, and Legendary; requirements and special effects live with equipment and combat logic.
- Progression remains grouped into five-floor cycles. Chest probabilities, additional rewards, enemy gold, and guaranteed Warden equipment keep the existing formulas and caps.

Trade uses the same gear objects as the inventory. Equipped-item identity is preserved when selling or removing equipment. Every purchase/sale is confirmed before resources change; trading itself does not advance turns. Returning to the main menu also requires confirmation and does not save the expedition.

## Biomes and environmental simulation

### Generation and visuals

Every five-floor cycle changes the biome in this repeating order: Ancient Ruins, Forgotten Cisterns, Fungal Caves, Ember Forges. Terrain remains walkable and the existing room/corridor connectivity is preserved. Water appears on 65% of cistern floors and 25% of other floors, in one small patch (occasionally two in cisterns). Barrels appear on 50% of floors and biome traps on 35%, with at most two of each. Merchant rooms have no pools or hazards. These features are optional, leaving most rooms clear. Each biome has its own palette and details: rubble and bones, connected shallow pools, fungi, or ash. Wall edges use ASCII outlines. Water ripples, torch flames, fire, and warm lighting animate using glyphs and color changes only.

`EnvironmentGenerator` uses a separate random stream derived from the expedition seed and floor, so cosmetic generation does not consume combat or loot rolls. Fixtures avoid the player, stairs, enemies, and pickups at generation time. Hazard placement also avoids the arrival area. Merchant rooms contain decorative details and collectible lights, with no generated barrels or traps; ignition is disabled in the refuge.

### Torches and visibility

- A new expedition starts with one lit torch containing 100 turns of fuel. Torches appear once under consumables, with a total count including the active torch and its remaining fuel. Fuel labels show only the current value.
- The equipped torch restores the original radius of 10 cells. Without it, personal sight falls to 3 cells; nearby fixed torches and fire illuminate a small area in direct line of sight. Walls still block visibility. Explored terrain remains dimly remembered.
- Only completed gameplay turns consume fuel. Menu navigation, aiming, cancellation, and real-time animation do not. Stowing preserves remaining fuel. When a torch burns out, the next spare lights automatically without an extra action or loss of vision. Each replacement starts with 100 turns; light expires only when no spare remains.
- Select the torch consumable in inventory: Enter lights/stows, and T opens directional throw aiming on the map. Escape returns to inventory without spending anything. A valid throw consumes one torch and one action, travels up to five visible cells, and stops at a wall, fixture, enemy, or oil. A blocked throw consumes nothing.
- Throwing uses the active flame first and automatically lights a replacement if available; otherwise it throws a fresh spare. Water extinguishes a torch that lands in it. The active partially used torch cannot be sold as a fresh spare.
- Bumping or shooting a fixed torch knocks it onto the floor as `t`; stepping onto it collects a fresh spare. Chests can contain torches. Merchants stock four fresh torches for 8 gold each and buy spares for 4 gold, with the existing confirmation flow.

### Hazards and turn order

- Hitting an oil barrel (`O`) spills oil (`o`) onto its tile and adjacent dry walkable tiles. A thrown torch ignites it; connected oil and nearby barrels can chain together. Water and walls stop propagation. Stairs remain protected.
- Fire lasts four completed turns and deals `3 + min(5, cycleIndex)` damage per affected actor per turn, including the ignition turn. It can hurt both the player and enemies.
- Each biome has one trap type, always single-use under either actor: ruin spikes (`^`) deal `5 + min(4, cycleIndex)` direct damage; cistern discharges (`Z`) deal `3 + min(4, cycleIndex)` to the triggering cell and adjacent actors in line of sight; fungal spores (`%`) deal 2 poison damage for three turns; forge jets (`V`) ignite a cross of dry floor cells. Triggered plates become spent (`_`). Poison continues after leaving the trap or changing floors. Flames obey the normal water, wall and stair safeguards.
- Environmental effects resolve once after enemy actions, followed by a visibility refresh. Environmental enemy kills use the normal defeat/reward path exactly once. Lethal environmental damage ends the expedition.
- Wall torches, barrel spills, and hazards are separate from the base map tiles. `EnvironmentAppearance` samples their visual state without advancing turns or changing resources.

## Rare enemies and floor modifiers

`FloorEventGenerator` runs after normal content and environment placement, using its own stream derived from the run seed and depth. It never adds rare events to merchant refuges. The modifier and elite rolls are independent.

### Elite encounters

Each ordinary or guardian floor has an 8% chance to promote one existing non-boss enemy. Promotion is limited to one elite per generated floor; Wardens retain their existing role and stats. Elites have 40% more health (rounded up), +1 attack, +1 defense and +1 damage. One or two unique titles are selected from Cruel, Ironbound, Relentless, Dread, Profane and Ancient. Titles have distinct colors; elites use uppercase map glyphs and a colored portrait frame. Combat and reward logs identify their titles.

Defeating an elite has a 35% chance to grant one equipment item of Rare quality or better, using the depth-scaled rare-or-better rarity distribution. Environmental kills use the same reward path. Repeated damage against an already defeated enemy cannot duplicate rewards. The elite still awards normal species experience and gold.

### Floor variants

There is a 12% total chance for one floor modifier, selected from eligible variants:

| Variant | Effect |
| --- | --- |
| Lightless Depths | Sight radius is two cells, regardless of equipped torches, fixed lights or fire; walls still occlude sight. |
| Infestation | Every regular enemy is the same randomly selected species. This variant is excluded from natural guardian-floor generation. |
| Hot Draft | Newly ignited fire lasts six turns instead of four. |
| Thin Air | Natural energy recovery occurs every twelve turns instead of six; active waiting and consumables keep their normal effects. |
| Hidden Caches | Up to two additional chests are placed on unoccupied, reachable floor cells away from arrival and stairs. |

The HUD and entry journal describe the modifier in the selected language. Effects are scoped to their floor and reset on generation of the next floor. The adventure compendium includes the biome traps, elites and altered lands.

## Startup and screen transitions

Normal startup plays a short localized terminal-style story at 42 characters per second. Enter or Space reveals the remaining text, then continues; Escape skips it. The main menu appears automatically two seconds after the story finishes. Saved language preferences apply before the introduction.

`OpeningStory` owns presentation timing. `ScreenTransitions` records ASCII drawing commands and replays the outgoing screen while fading out, then fades in the destination. Each half lasts 180 ms with smooth interpolation. Screen navigation, pause tabs, and shop modes share this path. Transition input is suppressed and held movement is cleared; presentation timing never advances combat turns or consumes random numbers. Capture scenarios disable navigation fades for deterministic screenshots.

The main menu's Quit action opens a confirmation with No selected. Escape cancels; the host only closes after an explicit Yes confirmation. Returning from an expedition retains its separate progress-loss confirmation.

## Health-dependent hero portraits

Each class has its original portrait and a matching exhausted critical-health variant. `HeroPortrait.Select` uses the critical asset at 25% health or below and immediately restores the original above that threshold. HUD, character sheet, and the static end-of-run portrait share the selector; class selection always shows the original portrait. Portrait selection never changes gameplay state or adds damage animations to the death screen.

Critical source illustrations are stored in `ArtSources/*_critical.png`, with the integrated image-generation prompts in `ArtSources/critical-portrait-prompts.json`. The converter produces matching 100-column ASCII grids and tone maps. No raster portrait is displayed at runtime.

## ASCII asset pipeline

`AsciiArt` loads `Art/*.txt` and corresponding `*.tone` files. Each printable character represents image detail; the tone map determines its brightness. `AsciiCanvas` caches glyph layers and draws them with the monospaced font. Menus use localized lighting modulation for the tower, campfire, and journal candle. Damage effects use character changes, color changes, and temporary portrait displacement. `ActionEffects` records outgoing actions before turn resolution, and `ActionEffectsRenderer` draws animated ASCII overlays: rotating blades, an expanding arcane wave, arrow trails, and shadow slashes. Basic staff/bow shots follow the actual firing path up to the first enemy, wall, or range limit, including misses. Effects never consume randomness or schedule turns, are clipped to visible walkable cells, pause with menus, and clear when a floor changes. Active action effects request redraws at up to 30 FPS; idle redraw timing remains unchanged.

The original raster illustrations in `ArtSources/` are offline conversion inputs. Gameplay draws the converted ASCII assets, not those raster images. `Tools/convert_ascii.py` uses Pillow to regenerate the text/tone assets. Preserve matching dimensions between each text file and its tone map. When creating an export preset, ensure runtime `.txt`, `.tone`, and font resources are included.

## Settings and persistence

Preferences are stored through Godot `ConfigFile`: language in `user://settings.cfg` (`display/language`: `pt` or `en`), independent music/effect volumes in `user://audio.cfg`, and fullscreen/resolution in `user://display.cfg`. Missing preferences retain Portuguese, 50% music, 75% effects, and a 1280 x 800 window. Expeditions and inventory are kept in memory; there is no save/load system.

## Testing and validation

After building and importing resources:

```bash
"$GODOT_BIN" --headless --path . -- --self-test
```

A successful run prints subsystem audit messages and `SELF-TEST PASS`, then exits with status zero. Failures exit with status one. Tests compile in the same assembly to exercise internal services without making the game API public.

Coverage includes:

- 550 generated floors, including depths up to 1,000: connectivity, occupancy, deterministic generation, boss placement, and endless descent.
- Dice probabilities, critical hits, healing, attribute scaling, equipment requirements/effects, and melee/ranged restrictions.
- Patrol, detection, search, pathfinding, and Warden confinement.
- Merchant generation, stock persistence, purchase/sale confirmation, currency, and equipped-item sales.
- Menu navigation, localization persistence, journal pagination, ASCII asset validity, and damage animations.
- Held movement timing, release, pause, focus loss, and aiming.
- Skill/projectile animation lifecycle, shot directions, wall/range clipping, fog, and cosmetic-only timing.
- Localized introduction timing, skip/continue, automatic completion, bidirectional fades, transition input gating, and quit confirmation.
- Platform-port substitution, AI policy dispatch, and architectural dependency boundaries.
- 800 rare-generation scenarios plus four trap effects, five floor variants, title/color diversity, elite stat bonuses, optional rare rewards, and safe guardian/refuge handling.
- 420 environmental generation scenarios plus torch lifetime, lighting/occlusion, inventory-only throwing, safe rooms, fire chains, water, poison, environmental defeat, and torch economy.
- Frozen environmental gameplay checkpoints covering maps, actions, rewards, inventory, light, terrain, hazards, logs, effects, and merchant transactions. The original 1,273 pre-refactor checkpoints remain archived in `Tests/Fixtures/behavior.sha256`.

The active `environment-behavior.sha256` fixture records the intentional environmental gameplay update. The historical `behavior.sha256` is retained unchanged. A behavior fixture is a compatibility baseline, not an expected-output file to regenerate after a failure. Investigate mismatches first. It deliberately protects random consumption order as well as visible rules. Only an intentional gameplay change should justify updating it.

## Capture and diagnostic commands

Flags after `--` are handled by the game:

```bash
"$GODOT_BIN" --path . -- --portuguese --view=classes --capture=/absolute/path/classes.png
"$GODOT_BIN" --path . -- --english --demo --capture=/absolute/path/game.png
"$GODOT_BIN" --path . -- --english --demo --view=pause --capture=/absolute/path/sheet.png
"$GODOT_BIN" --path . -- --merchant-demo --trade-demo --capture=/absolute/path/trade.png
```

- `--english` / `--portuguese`: override the displayed language for the process.
- `--demo`: start the seeded mage scenario.
- `--view=intro|home|classes|language|main-settings|pause|inventory|journal|settings|help|confirm_exit|confirm_quit|dead`: select a screen; combine with `--demo` for expedition screens.
- `--damage-demo`, `--ranged-demo`, `--inventory-demo`, `--merchant-demo`: deterministic feature scenarios.
- `--trade-demo`, `--room-demo`: modify the merchant scenario.
- `--capture=/absolute/path.png`: save a rendered frame and exit; keyboard input is disabled during capture.
- `--capture-delay=SECONDS`: wait before capture, clamped to 0–3 seconds.
- `--freeze-animation`: with capture, disable frame processing for repeatable visual comparisons.
- `--action-demo=warrior|mage|archer|rogue|bolt|arrow`: preview an outgoing action in a deterministic room.
- `--effect-time=SECONDS`: advance the action preview to a specific animation time before capture.
- `--rare-demo=None|Blackout|Infestation|HotDraft|ThinAir|HiddenCache`: preview a floor variant with a visible elite.
- `--critical-demo=0|1|2|3`: preview a critically injured warrior, mage, archer, or rogue; supports `--view=pause` and `--view=dead`.
- `--biome-demo=0|1|2|3`: seeded full-map environmental preview (diagnostics only).
- `--fog-demo` / `--dark-demo`: use ordinary torch visibility or reduced unlit visibility in a biome preview.
- `--torch-inventory` / `--fire-demo`: show torch inventory details or ignite a preview barrel.

## Development conventions

- Keep rendering out of domain/rule classes and platform calls behind their ports.
- Pass explicit dependencies to new services; avoid retrieving the entire session from individual services.
- Keep turn scheduling and random draw ordering intact when refactoring.
- Preserve equipment object identity; equivalent item values do not make two inventory entries the same object.
- Keep Portuguese/English runtime text paired; this README is English-only.
- Add behavior policies at the composition root and keep their contracts small.
- Run the headless suite after changes to mechanics, state transitions, or generation. Inspect affected screens after layout changes.
- Do not commit `.godot/`, build outputs, or local capture artifacts.

## Biome bestiary and Warden abilities

`EnemyCatalog` defines immutable combat profiles, biome rosters and portrait keys. `Enemy` carries instance state; `EnemyText` supplies bilingual names. Spawning and infestation both select from the current biome roster. Elite promotion remains limited to one regular enemy per floor, and safe merchant floors remain empty of enemies.

| Biome | Regular species | Warden | Ability |
| --- | --- | --- | --- |
| Ruins | Skeleton, Goblin, Revenant | Ruin Warden | Seismic impact: radius-two shockwave |
| Cistern | Rat, Drowned, Giant leech | Tide Warden | Flood wave: five-tile reach, three-tile width; creates water, removes oil/fire and drains 2 energy on hit |
| Fungal caves | Sporeling, Cave crawler, Myconid | Spore Sovereign | Spore burst: radius-one cloud at the marked player position; three poison ticks on hit |
| Ember forge | Cinder hound, Ember imp, Forged sentinel | Forge Warden | Furnace cross: four-tile arms; leaves fire for two ticks, four in hot drafts |

- Each Warden is tied to its biome and still guards the stair room. Leaving the room cancels a prepared ability.
- Abilities cost 4 energy from a pool of 6, restore 1 per active turn and have four recovery turns. Wardens begin with two recovery turns.
- Preparation marks fixed tiles with colored `!` glyphs. The player has two actions to escape before resolution; the ability replaces the Warden's normal action.
- Ability attacks use d20 against the unified defense. Seismic impact uses Strength; the other abilities use Intelligence. Existing tier scaling, critical hits and armor reduction apply.
- `WardenAbilities` owns turn-based preparation and resolution. `ActionEffects` renders separate shock, wave, spore and flame animations without advancing gameplay or consuming combat randomness.
- `BestiaryTests` checks all rosters and portraits, 120 generated boss floors, ability costs, windup, escape, room boundaries, status effects and animation timing.
- `--warden-demo=0|1|2|3` previews a prepared ability. Add `--warden-cast` to inspect the release animation, optionally with `--capture` and `--freeze-animation`.

## Exit integrity

- `DungeonConnectivity.EnsureExit` restores the stair tile after floor decoration and modifiers, verifies reachable walkable regions and carves a deterministic emergency corridor if a disconnected region is found. Normal connected maps retain their layout and random sequence.
- Discovered stairs retain a readable gold marker outside current sight. An actor or action effect occupying the exit alternates with the stair marker; undiscovered exits remain hidden.
- `ExitTests` covers 720 infested maps across biome cycles, every floor modifier, merchant floors, disconnected-region recovery and occupied/hidden stair markers.
- Environmental tests also verify three consecutive torches last exactly 300 turns, automatic replacements preserve vision, stowing preserves fuel and throwing switches to an available spare.

## Persistent bestiary

- Pause → Compendium → Bestiary opens the collection. Left/right selects a biome; up/down selects a creature. Escape returns to the compendium. A/D and Tab retain pause-tab navigation.
- Each biome lists its three regular species and its Warden. Locked pages show `???` and a generic illustration. Defeating a species reveals its existing ASCII portrait, bilingual lore and cumulative kill count. Combat attributes and ability statistics are omitted.
- `BestiaryState` tracks stable portrait/species identifiers. `BestiaryProgress` loads known identifiers and records confirmed kills through `CombatService.Hit`; elite variants share their species count, while biome Wardens have distinct entries. Environmental kills use the same path.
- `IBestiaryStore` separates progression from storage. `GodotBestiaryStore` writes `user://bestiary.cfg` through a temporary file followed by replacement. Discoveries survive new expeditions and application restarts; this does not save an expedition. Failed writes retain in-memory progress and report the issue in the journal.
- Captures, diagnostic demos and automated tests use isolated in-memory progression, so previews cannot unlock the player's collection. Existing play history predating this feature cannot be reconstructed.
- `BestiaryProgressTests` covers all sixteen entries, bilingual lore, nonlethal hits, duplicate death handling, elite aggregation, environmental kills, navigation, persistence and real file replacement.
- `--bestiary-demo` previews an unlocked page; add `--bestiary-locked` to preview a new collection.

## Music and sound

- `Audio/` contains seven original forty-second ambient loops and twenty-eight short chiptune effects. The score uses low drones, sparse modal notes and soft echoes for the menu, four biomes, merchant refuge and Warden encounters.
- `Tools/generate_audio.py` reproducibly synthesizes the assets using Python's standard library. Effects combine pulse/triangle oscillators, stepped pitch and sample-and-hold noise with short envelopes. Assets are mono PCM16 WAV at 22,050 Hz; `Audio/manifest.json` lists durations and measured peaks.
- `SoundEffects` queues bounded cosmetic cues. `GameAudioController` selects music from screen/biome/encounter state, coalesces identical cues per frame and never consumes combat randomness or turns. `IGameAudio` allows isolated tests.
- `GodotGameAudio` uses ten effect voices and two music players with 1.8-second crossfades. WAV data is cached, music loops at sample boundaries and streams are disposed when the scene exits. Pausing retains the current ambient track.
- F7 cycles music volume; F8 cycles effects volume through 0/25/50/75/100 percent. These legacy shortcuts remain available across screens; settings menus expose arrow-key volume controls instead. Preferences persist in `user://audio.cfg`; diagnostic modes do not overwrite them.
- `--audio-check` loads/schedules every asset through the Godot backend and exits. Headless self-tests validate encoding, levels, cue routing, movement, music selection and volume controls. Captures and ordinary unit tests do not play audio.
- Include `Audio/*.wav` as original non-resource files in any export preset, alongside the existing text-art assets: the backend reads the canonical WAV bytes directly rather than imported audio resources.

### Floor transitions

Successful descent preserves the outgoing frame, fades to black over 0.4 seconds, holds the black frame until the last footfall finishes (including its 65 ms tail), then reveals the new floor over 0.4 seconds. Three short 8-bit footfalls accompany the transition, spaced 0.35 seconds apart. Input is gated immediately, failed descent does not trigger effects, and animation/audio time consumes no gameplay turns. The same path covers ordinary, Warden and merchant floors.

## Named equipment

Twelve named items extend `Gear` with a stable `ItemId`. They retain the base kind's dice, attribute scaling, rarity, level requirement and value. Their unique property replaces the generic epic/legendary impact, life drain, protection or focus effect. Named robes/hoods are wearable by every class; weapon restrictions remain unchanged. After rolling rarity, equipment generation has a 40% chance to select an available named item of that rarity. This applies to chest/elite/Warden rewards and merchant stock. Each named legendary is issued at most once per expedition, including shop stock; selling it transfers the same object rather than generating a duplicate.

| Rarity | Item | Effect |
| --- | --- | --- |
| Rare | Spark Sword | Physical hits against targets on oil have a 50% chance to ignite it without another action. |
| Rare | Venom Bow | Natural 20 poisons surviving targets for three ticks of two damage; fungal biome enemies are immune. |
| Rare | Insulating Leather | Blocks discharge splash from other cells; reduces periodic fire damage by one. Standing on the discharge itself remains dangerous. |
| Rare | Camp Ring | Fresh torches cost five gold; a full torch lasts 120 turns while worn. Fuel is stored as a remaining fraction, so equipment changes cannot refill it. |
| Epic | Staff of Fluid Control | Basic projectiles extinguish fire and leave two-turn water along their traversed cells. Original terrain is restored; existing water remains. |
| Epic | Precise Throwing Gauntlets | Torch throws traverse the visible line until a wall, hit each enemy once for one fire damage and push it one cell. Oil along the path can ignite. |
| Epic | Deep Breath Mantle | Restores six-turn recovery in Thin Air; energy potions roll three d6 and sum the highest two. |
| Epic | Executioner's Blade | Rogue attacks automatically pay one direct HP before rolling and gain one d6 on hit. Misses still pay; lethal payment aborts the attack. |
| Legendary | Spellforge Blade | Critical hits cause a nonrecursive radius-two seismic burst for 2d4 + STR and a one-cell push, without energy cost. |
| Legendary | Revenge Hunter's Bow | Rolls two d20 and keeps the higher against elites/Wardens; removes the normal long-range accuracy penalty and retains full damage. |
| Legendary | Sandflower Relic | Actual healing blinds enemies within three cells for five enemy responses and permanently marks nearby walls. Floor-descent recovery is excluded. |
| Legendary | Hood of Shadow Legends | Lethal damage without an active torch triggers shadow birds and escape to the nearest unoccupied stair-room cell with one HP and zero energy. The hood is removed from equipment/backpack and the remaining damage response is interrupted. |

`HeroVitals` centralizes healing and lethal damage, while `NamedEquipmentEffects` handles hit-triggered properties. Pushes respect occupied cells, walls and Warden-room confinement. `TemporaryWater` preserves prior terrain. Blindness pauses enemy movement and ability preparation. Diagnostics remain turn-neutral and audio/animations use existing assets. `NamedItemTests` exercises all twelve properties and loot availability; `--item-demo=ItemId` previews an item in inventory.

## Merchant capacity and Lost Blacksmith

- Merchant visits begin with five offers: two equipment pieces, health potions, energy potions, and torches. Consumables retain stack quantities. Reopening the shop never rerolls inventory.
- Sold items fill vacant stock slots. With five active offers, the merchant still buys items but does not list additional resale offers. Depleted entries are removed before adding a new offer.
- The Lost Blacksmith has a 3% chance on ordinary floors after floor one. Merchant and Warden floors are excluded. A separate seed-derived random stream selects a room excluding arrival and stairs; corridors and exits remain intact.
- Teal rune tiles mark his sanctuary; `F` marks the NPC. Monsters, pickups and hazards cannot spawn inside. Enemy navigation and knockback cannot enter; hostile attacks and environmental damage cannot hurt the player there. Fire and oil cannot spread inside.
- Stand beside him and press `E`. Arrows select equipment, Enter opens confirmation, and Escape cancels or leaves. Confirmation defaults to cancellation.
- Generic equipment improves one rarity per transaction: Common to Rare costs 60 gold, Rare to Epic costs 180, and Epic to Legendary costs 450. Kind and grade remain unchanged; existing rarity statistics, effects and level requirements apply.
- All twelve named items (`Special != ItemId.None`) are excluded, as is legendary equipment. Transactions revalidate ownership and gold and consume each pending request once.
- Equipped upgrades replace the exact reference in both backpack and equipment. If the new level requirement is not met, the item remains unequipped in the backpack; confirmation explains this beforehand. Upgrades do not advance turns.
- `BlacksmithService` owns eligibility, prices and transactions; `BlacksmithRenderer` owns the bilingual ASCII interface. `DungeonState.IsSanctuary` is shared by generation, navigation, displacement, combat and environment services.
- `--blacksmith-demo` previews the menu; add `--upgrade-confirm` for confirmation or `--view=game` for the room. Self-tests cover occurrence, protection, confirmation, costs, equipment identity, exclusions and stock capacity.


## Shared settings menu

- The main menu Settings entry and pause Settings tab use `SettingsRenderer` and `SettingsController`, sharing the same live preferences.
- Up/Down or W/S selects language, music, sound effects, fullscreen, resolution, or the return action. Left/Right or A/D adjusts values. Music and effects move independently in 5% steps, clamp to 0-100%, and accept held-key repeats; 0% mutes the selected channel.
- Enter opens the language picker or toggles fullscreen. F11 remains synchronized with the fullscreen setting. Language and audio changes apply immediately and persist across launches.
- In pause settings, horizontal arrows adjust values instead of changing tabs. Tab or 1-5 switches tabs; Escape resumes the expedition. Returning to the main menu retains its existing unsaved-progress confirmation. Settings never advance a turn or consume torch fuel.
- Resolution presets: 1024 x 640, 1280 x 720, 1280 x 800, 1366 x 768, 1600 x 900, 1920 x 1080, 1920 x 1200, 2560 x 1440, and 3840 x 2160.
- The chosen resolution defines the viewport render target in both windowed and fullscreen modes. `GodotGameHost` uniformly scales and centers the logical 1280 x 800 ASCII layout inside that target. Aspect-preserving viewport scaling letterboxes when needed. Fullscreen uses the desktop display mode; window dimensions are capped to the current monitor's usable area so the window stays reachable even with a larger render target.
- `DisplayPreferences` holds display values, `IDisplaySettings` isolates persistence, and `GodotDisplaySettings` stores them in `user://display.cfg`. Unsupported saved resolutions fall back to 1280 x 800. Diagnostic previews do not overwrite preferences.
- `--view=main-settings` previews the main settings screen; `--demo --view=settings` previews the pause tab. Add `--display-demo=1920x1080` to exercise real viewport scaling and `--fullscreen-preview` for fullscreen.
- Settings tests cover both entry points, immediate audio values, language persistence, volume bounds and key repeats, all resolution presets, F11 synchronization, disk reload, invalid-resolution fallback, save failures, and turn-free navigation.


## Level-ten class advancement

- At level 10 or higher, surviving characters without an advanced class receive a two-option choice after the current action animation. The menu pauses the expedition and requires confirmation, defaulting to No. Escape defers the choice; `C` in the pause character sheet reopens it.
- Advancement is chosen once per expedition. `ClassIndex` remains the original class for equipment eligibility, attribute growth and basic attacks; `AdvancedClass` selects upgraded skills, localized names and portraits. A new expedition resets advancement and temporary defense.
- Every specialization replaces the normal and critical portraits with a distinct 100 x 60 ASCII image and tone map. Critical health retains the existing 25% threshold. Sixteen imagegen sources and exact prompts are retained in `ArtSources/advancement-prompts.json`.
- `Q` retains the base skill's energy progression and targeting style while using the specialization's damage dice and range. Damage still includes the primary attribute, level scaling and weapon power.
- `R` invokes the new ability. Its initial cost decreases by one per four levels after level 10, to a minimum of four energy. Damaging new abilities add the primary attribute, weapon power and one damage per two levels after level 10. They use the existing d20 and equipment effect pipeline. No-target and insufficient-energy attempts do not consume a turn.

| Base | Advancement | Improved Q | New R / initial energy |
|---|---|---|---|
| Warrior | Sentinel | 2d8 whirlwind, radius 3 | Bastion: +6 defense for three enemy responses / 6 |
| Warrior | Berserker | 3d6 whirlwind, radius 2 | Crushing blow: adjacent 4d8 strike, pushes a surviving target on hit / 7 |
| Mage | Pyromancer | 3d6 nova, radius 5 | Fireburst: 3d6 burst around a selected target within 6 cells, ignites its cross-shaped area using environmental fire rules / 9 |
| Mage | Cryomancer | 2d8 nova, radius 6 | Frozen prison: 1d6 against visible foes within 3 cells, freezes surviving hit targets for two enemy responses / 9 |
| Archer | Ranger | 2d10 arrow, range 10 | Arrow rain: 2d6 against visible foes within 6 cells / 8 |
| Archer | Deadeye | 3d8 arrow, range 12 | Death mark: 4d8 selected-target shot, range 12, +4 accuracy / 9 |
| Rogue | Assassin | 3d8 adjacent strike and blink | Venom blade: adjacent 3d6 strike, adds three ticks of two poison damage on surviving hits; fungi are immune / 7 |
| Rogue | Shadowblade | 3d6 adjacent strike and blink; successful blink grants +4 defense for two responses | Shadow veil: blinds visible foes within 3 cells for two responses / 8 |

- Buff/status durations include the immediate response to the action that created them. Blindness and freezing expire concurrently, without stacking skipped responses. Each new ability has its own ASCII animation, reusing appropriate sound cues. Animations do not consume turns or randomness.
- The rogue's Q now requires an adjacent, visible target in line of sight. A hit attempts to blink to the cell directly beyond the target along the attack direction. A wall, creature, NPC, oil barrel or fixed torch blocks relocation without cancelling the attack. A miss or lethal self-cost never blinks. Successful relocation collects floor items and processes environmental effects through the normal turn scheduler.
- `AdvancementCatalog` holds specialization rules; `ClassAdvancementService` owns unlock/choice; `AdvancedAbilityService` executes new abilities; `AdvancementRenderer` and `AdvancementText` present the choices. The original class-selection screen remains unchanged.
- Preview the four choice screens with `--advancement-demo=0` through `3`. Use `--advanced-hero-demo=Shadowblade`, optionally `--critical-preview` and `--view=pause`, to inspect an advanced character.
- Tests cover level gating, confirmation, deferral, one-time choice, resets, all sixteen portraits, all eight new abilities, energy/turn accounting, poison immunity, temporary defense, animation timing, and rogue hit/miss/blocked-destination behavior.

### Elemental specialization and terrain reactions

- Pyromancer staff attacks and Q use fire; Cryomancer staff attacks and Q use ice, including projectile/nova colors and localized labels. Unarmed attacks retain ordinary melee behavior. Existing damage dice, energy costs and hit rolls remain in use.
- Projectiles react along their path; Q reacts on visible terrain in range with line of sight. Successful hits ignite dry ground or freeze targets for one enemy response (two on water/ice). R shares the same reaction rules.
- Fire ignites oil, melts ice into water, and produces temporary blinding steam on water. Hitting a frozen target with fire removes the freeze and deals two additional damage.
- Cold extinguishes fire and freezes water temporarily. Frozen cells suppress traps until thawing. Ice lasts four environmental ticks; steam lasts three, including the casting turn. Reactions clear on floor generation.
- Exits and safe merchant/blacksmith areas remain protected. Environmental fire can hurt the caster. The fluid-control staff retains its water effect, applied after the projectile's terrain reaction.
- Portrait status labels share the measured center of the ASCII frame and remain separate from damage numbers. Active poison, frozen, blind, burning and guard effects are shown where applicable.
- The temporary F9/clickable level-up control and its input path have been removed.
