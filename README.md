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
  Fixtures/behavior.sha256   Pre-refactor deterministic behavior checkpoints
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
- `InventoryState`: backpack, three equipment slots, and consumable counts.
- `DungeonState`: map tiles, exploration/visibility masks, enemies, pickups, stair room, and merchant location. Spatial queries live alongside the map data.
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
| `EnemyAi` | Dispatch to registered enemy behavior policies |
| `EnemyNavigator` | Occupancy-aware breadth-first pathfinding |
| `MerchantService` | Offers, pricing, confirmed purchases and sales |

### Interfaces and extension points

- `IGameHost` isolates redraw requests, quitting, window visibility, and fullscreen changes.
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

## ASCII asset pipeline

`AsciiArt` loads `Art/*.txt` and corresponding `*.tone` files. Each printable character represents image detail; the tone map determines its brightness. `AsciiCanvas` caches glyph layers and draws them with the monospaced font. Menus use localized lighting modulation for the tower, campfire, and journal candle. Damage effects use character changes, color changes, and temporary portrait displacement. `ActionEffects` records outgoing actions before turn resolution, and `ActionEffectsRenderer` draws animated ASCII overlays: rotating blades, an expanding arcane wave, arrow trails, and shadow slashes. Basic staff/bow shots follow the actual firing path up to the first enemy, wall, or range limit, including misses. Effects never consume randomness or schedule turns, are clipped to visible walkable cells, pause with menus, and clear when a floor changes. Active action effects request redraws at up to 30 FPS; idle redraw timing remains unchanged.

The original raster illustrations in `ArtSources/` are offline conversion inputs. Gameplay draws the converted ASCII assets, not those raster images. `Tools/convert_ascii.py` uses Pillow to regenerate the text/tone assets. Preserve matching dimensions between each text file and its tone map. When creating an export preset, ensure runtime `.txt`, `.tone`, and font resources are included.

## Settings and persistence

The only persisted game preference is the language, stored through Godot `ConfigFile` at `user://settings.cfg`, section `display`, key `language` (`pt` or `en`). Missing settings retain the default Portuguese language. Expeditions and inventory are kept in memory; there is no save/load system.

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
- Platform-port substitution, AI policy dispatch, and architectural dependency boundaries.
- **1,273 frozen behavior checkpoints** recorded before the structural refactor, covering generated maps, actions, rewards, inventory, logs, effects, and merchant transactions.

The behavior fixture is a compatibility baseline, not an expected-output file to regenerate after a failure. Investigate mismatches first. It deliberately protects random consumption order as well as visible rules. Only an intentional gameplay change should justify updating it.

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
- `--view=home|classes|language|pause|inventory|journal|settings|help|confirm_exit|dead`: select a screen; combine with `--demo` for expedition screens.
- `--damage-demo`, `--ranged-demo`, `--inventory-demo`, `--merchant-demo`: deterministic feature scenarios.
- `--trade-demo`, `--room-demo`: modify the merchant scenario.
- `--capture=/absolute/path.png`: save a rendered frame and exit; keyboard input is disabled during capture.
- `--capture-delay=SECONDS`: wait before capture, clamped to 0–3 seconds.
- `--freeze-animation`: with capture, disable frame processing for repeatable visual comparisons.
- `--action-demo=warrior|mage|archer|rogue|bolt|arrow`: preview an outgoing action in a deterministic room.
- `--effect-time=SECONDS`: advance the action preview to a specific animation time before capture.

## Development conventions

- Keep rendering out of domain/rule classes and platform calls behind their ports.
- Pass explicit dependencies to new services; avoid retrieving the entire session from individual services.
- Keep turn scheduling and random draw ordering intact when refactoring.
- Preserve equipment object identity; equivalent item values do not make two inventory entries the same object.
- Keep Portuguese/English runtime text paired; this README is English-only.
- Add behavior policies at the composition root and keep their contracts small.
- Run the headless suite after changes to mechanics, state transitions, or generation. Inspect affected screens after layout changes.
- Do not commit `.godot/`, build outputs, or local capture artifacts.
