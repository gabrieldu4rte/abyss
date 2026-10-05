# Game media

[Back to Abyss](../../README.md)

These screenshots and animations were captured from the actual Godot viewport at **1280 × 800**, with the interface in English. They illustrate the game presentation in the main README.

## Capture context

The captures use deterministic diagnostic scenarios in an isolated copy of the project. Encounters, inventory contents, advancement choices, and bestiary discoveries are prepared for demonstration. The four biome overviews reveal exploration fog so that room layouts can be seen. Normal expeditions retain their lighting and line-of-sight rules.

No gameplay code or player save data was changed in the original project to produce these images. GIFs contain rendered game frames, without audio or added visual effects.

## Screenshots

The [capture manifest](capture-manifest.json) records the scenario arguments for each image. After building and importing the project, a screenshot can be reproduced with the existing capture command:

```bash
"$GODOT_BIN" --path . -- --english --view=home --capture=/absolute/path/title.png
```

Run this from the project root with `GODOT_BIN` set to the Godot .NET executable. Replace `--view=home` with the relevant manifest arguments for other scenes.

## Animations

| File | Scene |
| --- | --- |
| [Arcane nova](arcane-nova.gif) | The Mage's expanding ability effect and impact feedback. |
| [Arrow shot](arrow-shot.gif) | A ranged projectile moving toward its target. |
| [Warden fire](warden-fire.gif) | The Forge Warden's furnace cross. |

A temporary capture harness in the isolated copy advanced the game simulation by 1/15 second and saved the viewport after each rendered frame, for 36 source frames per animation. This harness is not part of the playable project. GIFs use a shared palette within each animation, 70 ms frame delays, and a one-second final hold. Repeated frames may be merged by the encoder while preserving their duration.
