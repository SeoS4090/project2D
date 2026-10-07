# project2D

Unity 6.3 LTS (`6000.3.18f1`) 2D action roguelike prototype.

## Run the current prototype

Open `Assets/Scenes/TrainingGround.unity` and press Play. The Training Ground is also the first scene in Build Settings for this prototype.

- `WASD` or arrow keys: move
- Mouse: aim
- Left mouse button: sword attack
- `Space`: dash (charges recharge one at a time)
- `R`: reset the player, dummy, and damage log

The first implementation uses provisional combat values in `TrainingPlayerController`; move them into authored game data when the design tables are ready. Training Ground sessions do not initialize the account/save framework.

## Assets

The supplied Kenney Minimap Pack and Pixel Adventure UI Pack are in `Assets/Art/Kenney`. Both are CC0; original license files are included. Kenney credits are optional. The HUD uses the pixel UI frames, and the minimap uses pack icons for the player and target.

The current player uses the [Hero V2 prototype](Assets/Art/Characters/HeroV2/README.md): four directions, each with 4 idle, 6 walk and 6 attack frames. Its editable Aseprite source has seven layers; the game renders body, arms/weapon, shadow and effects separately. Current cells are 64×64 with a body about 32 pixels high, at 32 PPU with point filtering and the URP Pixel Perfect Camera. This is an intermediate art direction under review.

Use the V2 README for regeneration and scene installation. The original hero and its generator remain in `Assets/Art/Characters`; `ExportTrainingArt.ps1` regenerates that earlier version under `Frames/Generated` and does not generate Hero V2. Close Unity Editor before using that export script. Generators overwrite their generated Aseprite sources, so preserve manual edits before regenerating.

V2 previews and the previous implementation verification record are in [Screenshots/HeroV2](Screenshots/HeroV2/Verification.md).

## Design documents

The imported design vault is at `Assets/Documentation/RoguelikeDesign`. This implementation follows its Training Ground prototype order; combat tuning, progression, and run systems remain future work.

- [Pixel-art techniques and image-generation plan](Docs/Art/pixel-art-research-2026-10-07.md)
- [Action-led character proportions: Sephiria, Isaac and Hades](Docs/Art/action-first-character-proportions-2026-10-08.md)

The latest art proposal reopens anatomy, sprite resolution and weapon proportions. It proposes comparison designs; those new proportions have not yet been implemented in the scene.
