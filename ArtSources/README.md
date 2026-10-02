# Art sources

All illustrations in this directory were created with the integrated **imagegen** tool. The original prompts are stored in [prompts.json](prompts.json); the merchant portrait uses [merchant-prompt.txt](merchant-prompt.txt).

- Heroes: `warrior.png`, `mage.png`, `archer.png`, `rogue.png`.
- Monsters: `rat.png`, `skeleton.png`, `goblin.png`, `warden.png`.
- Menus: `tower.png`, `camp.png`, `globe.png`, `book.png`, `grave.png`, `crown.png`.
- Supporting illustrations: `unknown.png`, `torch.png`.
- Merchant: `merchant.png`, converted into a 100 × 60 character portrait.

The sources are detailed monochrome fantasy illustrations. [convert_ascii.py](../Tools/convert_ascii.py) converts luminance and edges into ASCII characters, compensates for glyph proportions, and writes character grids and eight-level tone maps to `Art/`.

These sources are retained for future conversions. `.gdignore` prevents Godot from importing the PNG files as visual resources. The game renders the converted text rather than these raster images.

Critical-health variants: `warrior_critical.png`, `mage_critical.png`, `archer_critical.png`, and `rogue_critical.png`. These were edited from the original portraits with the integrated imagegen tool, preserving identity and equipment while showing exhaustion and superficial injuries. Exact prompts are in `critical-portrait-prompts.json`.

Biome bestiary: twelve new source portraits were generated with the integrated imagegen tool. Exact prompts are stored in [bestiary-prompts.json](bestiary-prompts.json). Nine regular enemies and three Wardens join the existing rat, skeleton, goblin and ruin Warden. Each new portrait is converted to a 100-column ASCII grid and matching tone map.

Lost Blacksmith: `lost_blacksmith.png`, generated with the integrated imagegen tool using the merchant as a style reference. The exact prompt is in [lost-blacksmith-prompt.txt](lost-blacksmith-prompt.txt). Converted to a 100-column ASCII portrait with an eight-level tone map, displayed through the same portrait frame as the merchant.

Class advancement: sixteen normal/critical portraits for Sentinel, Berserker, Pyromancer, Cryomancer, Ranger, Deadeye, Assassin and Shadowblade. Generated with the integrated imagegen tool from the original heroes, retaining their identities. Final prompts are in [advancement-prompts.json](advancement-prompts.json). Each is converted into a 100 x 60 ASCII grid and tone map.
