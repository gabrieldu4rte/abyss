# Art sources

All 17 illustrations in this directory were created with the integrated **imagegen** tool. The original prompts are stored in [prompts.json](prompts.json); the merchant portrait uses [merchant-prompt.txt](merchant-prompt.txt).

- Heroes: `warrior.png`, `mage.png`, `archer.png`, `rogue.png`.
- Monsters: `rat.png`, `skeleton.png`, `goblin.png`, `warden.png`.
- Menus: `tower.png`, `camp.png`, `globe.png`, `book.png`, `grave.png`, `crown.png`.
- Supporting illustrations: `unknown.png`, `torch.png`.
- Merchant: `merchant.png`, converted into a 100 × 60 character portrait.

The sources are detailed monochrome fantasy illustrations. [convert_ascii.py](../Tools/convert_ascii.py) converts luminance and edges into ASCII characters, compensates for glyph proportions, and writes character grids and eight-level tone maps to `Art/`.

These sources are retained for future conversions. `.gdignore` prevents Godot from importing the PNG files as visual resources. The game renders the converted text rather than these raster images.
