# project2D

Unity 6.3 LTS (`6000.3.18f1`) 2D action roguelike prototype.

## Run the current prototype

Open `Assets/Scenes/TrainingGround.unity` and press Play. The Training Ground is also the first scene in Build Settings for this prototype.

- `WASD` or arrow keys: move
- Mouse: aim
- Left mouse button (hold or repeated presses): three-hit greatsword combo
- Right mouse button: hold to charge, release for a 360-degree greatsword spin
- `Space`: dash (charges recharge one at a time)
- `R`: reset the player, dummy, and damage log

The first implementation uses provisional combat values in `TrainingPlayerController`; move them into authored game data when the design tables are ready. Training Ground sessions do not initialize the account/save framework.

## Assets

The supplied Kenney Minimap Pack and Pixel Adventure UI Pack are in `Assets/Art/Kenney`. Both are CC0; original license files are included. Kenney credits are optional. The training HUD uses Pixel Adventure frames and badge markers. Its editable screen prefab is `Assets/UI/Training/TrainingHud.prefab`; dummy-owned world statistics use `TrainingDummyStatistics.prefab` beside it. Recent combat records, the lower-left motion status, bottom-center key guide and HUD buttons have been removed. Reset and motion review remain available on the keyboard. See the design vault note `07 Unity 기술/PixelAdventureUI 이미지 용도와 HUD 적용.md` and the current validation captures under `Screenshots/PixelAdventureUI/CleanHud*`.

TrainingGround reads the editable `Assets/Art/Runtime/TrainingDungeon/TrainingDungeon.tmx`, referencing the supplied CC0 Scribble Dungeons atlas. Its 64px images use 60px grid spacing (60 PPU / one world unit per cell) as in the original sample. Ground, transparent walls and objects render separately; Tiled H/V/diagonal flags set all four corners without rotating the floor pattern. The TMX also defines explicit wall/prop collision rectangles. The camera, mannequin and combat logic are retained. `Game > Training Ground > Apply Scribble Dungeon` applies saved TMX edits to the open scene; rebuilds also read it. Stair images currently show two down-stair variants; room transitions are future work. See the vault's `07 Unity 기술/Scribble Dungeons 타일 용도와 Tiled 적용.md` for all 154 atlas entries and actual verification under `Screenshots/TrainingDungeon`. The original Kenney sample remains unchanged.

The current player uses the [1.5-head SD mannequin](Assets/Art/Characters/MannequinMotion/README.md): 432 stored frames, four directions and ten motion clips. Body, legs, hands, weapon and shadow render separately; legs follow movement while the upper body follows aim or the active attack direction. The editable Aseprite source has nine layers. Cells are 128×128 with a neutral body 48 pixels high, at 32 PPU with point filtering. The existing camera and render pipeline are retained. This is an action test character.

`F6` toggles motion review, `F7` changes clip, `F8` changes direction, `F9` pauses, `.` steps one frame and `F10` selects 1×/0.25×/0.1× speed. Review disables control and damage; leaving review resets the player. Idle, walk, dash, three combo strikes, charge and spin use live input. Hurt and death remain visual studies. Combo length and charge/mana values are provisional training tuning.

The [verification record](Screenshots/MannequinMotion/Verification.md) includes real-input checks and a game-camera recording. Earlier ActionBlockout key poses and Hero V2 remain available.

Use the mannequin README for current regeneration and scene installation, or the V2 README for the earlier art. The original hero and its generator remain in `Assets/Art/Characters`; `ExportTrainingArt.ps1` regenerates that earlier version under `Frames/Generated`. Close Unity Editor before using that export script. Preserve manual edits before regenerating any source.

V2 previews and the previous implementation verification record are in [Screenshots/HeroV2](Screenshots/HeroV2/Verification.md).

## Design documents

The imported design vault is at `Assets/Documentation/RoguelikeDesign`. This implementation follows its Training Ground prototype order; combat tuning, progression, and run systems remain future work.

Current character-art discussions are organized as Obsidian notes in the vault:

- [Character proportions and pixel-art production](Assets/Documentation/RoguelikeDesign/07%20Unity%20기술/캐릭터%20비율과%20픽셀%20아트%20제작.md)
- [Separate character/weapon generation and prefab attachment](Assets/Documentation/RoguelikeDesign/07%20Unity%20기술/캐릭터와%20무기%20분리%20제작.md)

Detailed research notes remain available below:

- [Pixel-art techniques and image-generation plan](Docs/Art/pixel-art-research-2026-10-07.md)
- [Action-led character proportions: Sephiria, Isaac and Hades](Docs/Art/action-first-character-proportions-2026-10-08.md)

The latest trial implements the user's 1.5-head mannequin choice and detailed motion study in TrainingGround. The design vault records this trial in `07 Unity 기술/마네킹 모션 제작과 검증.md`. Final character identity, 8-direction art, interchangeable weapon prefabs and complete combat rules remain under review.
