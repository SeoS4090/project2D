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

The player has 4-direction idle and walk frames, plus windup, strike, and recovery poses with a separate greatsword layer. Editable Aseprite sources and the Lua generator are in `Assets/Art/Characters`. Run `ExportTrainingArt.ps1` with your Aseprite executable path to regenerate the source files and frame PNGs. Scene sprites use 32 PPU, point filtering, and the URP Pixel Perfect Camera.

## Design documents

The imported design vault is at `Assets/Documentation/RoguelikeDesign`. This implementation follows its Training Ground prototype order; combat tuning, progression, and run systems remain future work.
