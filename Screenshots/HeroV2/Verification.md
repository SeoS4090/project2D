# Hero V2 verification — 2026-10-07

- Unity 6000.3.18f1 / URP; TrainingGround scene saved with V2 animation data.
- Aseprite source parsed: 64 frames, 7 layers, 12 correctly bounded tags; frame durations match JSON.
- Four directions each have six distinct walking poses. Attack duration is 340ms per direction.
- All 256 Unity sprite references checked: 64×64, 32 PPU, pixel pivot (32,32), Point filtering, no mipmaps, uncompressed.
- 84 animation checks passed: pose sampling, loop boundaries, attack-state synchronization, four-direction weapon sorting and reset.
- C# compilation passed.
- Play mode: actual attack logic reduced dummy health from 250 to 225. Body and FX both selected frame 61 for the right-facing impact.
- Play mode: simulated D input was read as +1; player moved from x=-3 to the right wall at x=11.65 while a walking sprite was active.
- Released the simulated key and exited Play mode after verification.
- `InGame.png` and `InGameAttack.png` capture the actual world camera. Overlay HUD is intentionally outside these camera-only captures.

The initial serial Unity import was slow; final installer batches importer changes and completed successfully. Earlier import/focus tool timeouts did not prevent the final scene save or validation.
