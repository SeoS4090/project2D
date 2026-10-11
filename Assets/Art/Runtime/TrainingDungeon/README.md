# Tiled training hall

`TrainingDungeon.tmx` is the editable map source. Open it in Tiled; its external tileset is the existing Kenney `scribble-dungeons/Tiled/sampleSheet.tsx` (CC0). No generated atlas is used.

The lower equipment hall connects to a narrower northern practice alcove. Grid: 25 × 17 cells, 60 px per cell. Original tiles are 64 px at 60 PPU, retaining the existing Tiled overlap convention.

## Layers

- Ground: floor only inside the room outline.
- At the two concave shoulder junctions, Ground uses GID 59 (`floor_inner_round`): unflipped on the left, horizontally flipped on the right. Leave the corresponding Walls cells empty because this tile already includes the connection and floor.
- Walls: perimeter and alcove/shoulder junctions, including Tiled flip flags.
- Display Bases: independent weapon platforms.
- Objects: original Kenney barrels, chest, stairs, rug and weapon sprites.
- Collision: editable rectangle footprints; Unity creates BoxCollider2D objects.
- Spawns: Player and Dummy point objects; Unity reads their positions from the map.

After editing and saving the TMX in Tiled, open TrainingGround in Unity and run **Game > Training Ground > Apply Scribble Dungeon**. This rebuilds only the environment and spawn positions; the minimal HUD remains unchanged. **Apply Scribble Hall and Minimal HUD** also reapplies the HUD layout.

`Tools/Art/CreateTrainingDungeonTmx.py` generates the initial layout. Running it again overwrites manual Tiled edits, so use it only to intentionally reset the map.

Weapon displays are decorative. Gameplay interactions have not been added.

## Dummy statistics

Two numeric lines: gold DPS / pink maximum single-hit damage, then cyan most recent hit damage. DPS sums damage within the last one second of game time and drops to zero after one second without hits. Leaving the dummy practice radius clears all three values. The default radius is 5 world units, configurable as Reset Radius on Training Ground Session; selecting that object shows the boundary gizmo. The player is not teleported or refilled. R is no longer bound to reset. The dummy has no HP or health display. Hit count and elapsed time are no longer displayed.
