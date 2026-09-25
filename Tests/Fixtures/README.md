# Behavioral compatibility fixture

`behavior.sha256` contains 1,273 labeled SHA-256 checkpoints recorded from the implementation before the responsibility/namespace refactor (repository revision `73320a994ad8a35328857ddaddfb26dcf2bcfdbc`). The recording code was executed against that version, not against the reorganized implementation.

`BehaviorRegressionTests` replays fixed class, floor, seed, and command sequences. Each checkpoint serializes player resources, attributes, map tiles, visibility, enemies and awareness, pickups, equipment identity, merchant stock, confirmations, journal entries, and damage-effect state. The checks also protect the shared random sequence through its observable outcomes.

The fixture is intentionally readable as one scenario label and digest per line. Failure reports identify the first mismatching checkpoint. Keep test order deterministic; the regression suite prepares state through its existing scenarios before this audit.

Do not regenerate the file to silence a refactoring regression. When gameplay intentionally changes, review the affected scenario and expected state first, then record and review an explicitly approved replacement baseline.
