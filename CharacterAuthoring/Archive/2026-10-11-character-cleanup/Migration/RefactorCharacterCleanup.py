from pathlib import Path
import json, shutil
root=Path(__file__).resolve().parents[2]
def read(p): return (root/p).read_text(encoding='utf-8-sig')
def write(p,s): (root/p).write_text(s,encoding='utf-8')
p='Assets/Scripts/Training/TrainingPlayerView.cs'
old=read(p)
motion=old[old.index('    private static Game.Characters.RigUpperMotion RigMotion'):old.index('    public void SetState')].replace('        SetRigVisible(true);','        if (!commonRig.gameObject.activeSelf) commonRig.gameObject.SetActive(true);')
header='''using UnityEngine;
using Game.Characters;

/// <summary>Maps combat state to the shared rig. Animation never drives damage timing.</summary>
public sealed class TrainingPlayerView : MonoBehaviour
{
    [SerializeField] private CommonCharacterRig commonRig;
    public CommonCharacterRig CommonRig => commonRig;
    public int CurrentDirection { get; private set; }
    public string CurrentMotion { get; private set; } = "idle";
    public float RigPhase { get; private set; }
    private float locomotionElapsed;
    private bool wasMoving;
    private Vector2 lastAim = Vector2.down;

    public static readonly string[] ReviewClips = { "idle", "walk", "attack", "attack_reverse", "attack_heavy", "charge", "spin", "dash" };

    public void BindCommonRig(CommonCharacterRig rig)
    {
        commonRig = rig;
        ResetView();
    }

'''
tail='''    public void SetState(Vector2 move, Vector2 aim, bool attackPending, bool attackActive, Vector2 attackDirection, int attackSequence, float phaseRemaining, float windupDuration, float activeDuration,
        bool dashing = false, Vector2 dashDirection = default, float dashProgress = 0f,
        string attackClip = "attack", bool recovering = false, float recoveryProgress = 0f, bool charging = false, float chargeElapsed = 0f)
    {
        if (commonRig == null) return;
        SetRigState(move, aim, attackPending, attackActive, attackDirection, phaseRemaining, windupDuration,
            activeDuration, dashing, dashDirection, dashProgress, attackClip, recovering, recoveryProgress, charging, chargeElapsed);
    }

    public void ResetView()
    {
        locomotionElapsed = 0; wasMoving = false; lastAim = Vector2.down;
        CurrentMotion = "idle"; CurrentDirection = 0; RigPhase = 0;
        if (commonRig == null) return;
        if (!commonRig.gameObject.activeSelf) commonRig.gameObject.SetActive(true);
        commonRig.SampleContinuous(RigUpperMotion.Idle, Vector2.down, 0, 0, 0, false);
    }

    public AnimationClip ReviewClip(string name, int direction)
    {
        if (commonRig == null || commonRig.Motions == null || System.Array.IndexOf(ReviewClips, name) < 0) return null;
        return name == "dash" ? commonRig.Motions.lowerDash[Mathf.Clamp(direction, 0, 3)] : commonRig.Motions.Upper(RigMotion(name), 3);
    }

    public void PreviewClip(string name, int direction, float elapsed)
    {
        var clip = ReviewClip(name, direction);
        if (clip == null) return;
        direction = Mathf.Clamp(direction, 0, 3);
        var aim = direction == 0 ? Vector2.down : direction == 1 ? Vector2.up : direction == 2 ? Vector2.left : Vector2.right;
        bool loop = name != "dash" && CommonRigMotions.Loops(RigMotion(name));
        float time = loop ? Mathf.Repeat(elapsed, Mathf.Max(.0001f, clip.length)) : Mathf.Clamp(elapsed, 0, clip.length);
        commonRig.SampleContinuous(RigMotion(name), aim, direction, time, time, name == "walk", name == "dash");
        CurrentMotion = "preview / " + name; CurrentDirection = direction;
        RigPhase = time / Mathf.Max(.0001f, clip.length);
    }
}
'''
write(p,header+motion+tail)
p='Assets/Scripts/Training/TrainingMotionReview.cs';s=read(p)
s=s.replace('previews unsupported motions','previews shared rig motions')
s=s.replace('view.AnimationSet == null || !view.AnimationSet.HasNamedClips','view.CommonRig == null')
s=s.replace('view.AnimationSet.clips.Length','TrainingPlayerView.ReviewClips.Length')
s=s.replace('            var clip = view.AnimationSet.clips[ClipIndex];\n            float duration = view.AnimationSet.Duration(Direction, clip.first, clip.count);\n            elapsed = Mathf.Repeat(elapsed, duration);','            var clip = view.ReviewClip(TrainingPlayerView.ReviewClips[ClipIndex], Direction);\n            if (clip != null) elapsed = Mathf.Repeat(elapsed, Mathf.Max(.0001f, clip.length));')
s=s.replace('if (Reviewing) view.PreviewClip(view.AnimationSet.clips[ClipIndex].name, Direction, elapsed);','if (Reviewing && view != null) view.PreviewClip(TrainingPlayerView.ReviewClips[ClipIndex], Direction, elapsed);')
s=s.replace('if (Reviewing == enabled || view == null) return;','if (Reviewing == enabled || view == null || player == null || (enabled && view.CommonRig == null)) return;')
start=s.index('        var clip = view.AnimationSet',s.index('    public void Step()'))
end=s.index('\n    }',start)
s=s[:start]+'''        var clip = view != null ? view.ReviewClip(TrainingPlayerView.ReviewClips[ClipIndex], Direction) : null;
        if (clip == null) return;
        float step = 1f / Mathf.Max(1f, clip.frameRate);
        elapsed = elapsed + step > clip.length + .00001f ? 0f : Mathf.Min(elapsed + step, clip.length);'''+s[end:]
write(p,s)
p='Assets/Scripts/Training/TrainingGroundHud.cs';s=read(p).replace('상체 {playerView.CurrentFrame:D3}   다리 {playerView.CurrentLegFrame:D3}','리그 진행 {playerView.RigPhase:P0}').replace('. 한 프레임','. 클립 한 샘플');write(p,s)
p='Assets/Editor/TrainingGroundSceneBuilder.cs';s=read(p)
s=s.replace('    private const string HeroFramesPath = "Assets/Art/Characters/Frames/Generated";\n','').replace('    private static Sprite[] heroBodyFrames;\n','').replace('    private static Sprite[] heroSwordFrames;\n','').replace('        LoadHeroFrames();','        trainingDummySprite = LoadPixelSprite("Assets/Art/Characters/TrainingDummy.png");')
start=s.index('        SpriteRenderer playerBody');end=s.index('        player.transform.localScale',start);s=s[:start]+s[end:]
start=s.index('        Transform weaponTransform');end=s.index('\n        GameObject dummy',start)
s=s[:start]+'''        var playerView = player.AddComponent<TrainingPlayerView>();
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/AzureVanguard/AzureVanguard.prefab"));
        visual.name = "Character Visual"; visual.transform.SetParent(player.transform, false);
        player.AddComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder = 2;
        playerView.BindCommonRig(visual.GetComponent<Game.Characters.CommonCharacterRig>());
        SetPrivateField(controller, "playerView", playerView);
        player.AddComponent<TrainingMotionReview>().Bind(playerView);
'''+s[end:]
start=s.index('    private static void LoadHeroFrames()');end=s.index('    private static Sprite LoadPixelSprite',start);s=s[:start]+s[end:];write(p,s)
p='Assets/Editor/Characters/CommonRigLabBuilder.cs';s=read(p).replace('CommonRigLabBuilder','SharedRigBuilder').replace('Characters/CommonRigLab/','Characters/SharedRig/').replace('    public const string ScenePath = "Assets/Scenes/CommonRigLab.unity";\n','').replace('Generate Experiment','Generate Shared Motions').replace('        foreach (string id in new[] { "blue-tunic", "bronze-armor" }) ImportAppearance(id);\n','')
start=s.index('        foreach (string id in new[]');end=s.index('        AssetDatabase.SaveAssets();',start);s=s[:start]+s[end:]
start=s.index('    [MenuItem("Game/Characters/Common Rig/Open Play Lab")]');end=s.index('    private static AvatarMask MakeMask',start);s=s[:start]+s[end:]
start=s.index('    // [UNITY-SKILL:SPRITEATLAS]');s=s[:start]+'}\n'
for u in ['using UnityEditor.U2D;\n','using UnityEditor.U2D.Sprites;\n','using UnityEditor.SceneManagement;\n','using System.Linq;\n']:s=s.replace(u,'')
write(p,s)
p='Assets/Editor/Characters/CharacterAtlasBuilder.cs';s=read(p)
start=s.index('        GenerateGroup(');end=s.index('        GenerateAzureVanguard();',start);s=s[:start]+s[end:]
start=s.index('    private static void GenerateGroup');s=s[:start]+'}\n';s=s.replace('using System.IO;\n','');s=s.replace('if (sprites.Length == 0) return;','if (sprites.Length != 32) throw new BuildFailedException("Azure Vanguard requires exactly 32 registered sprites.");');write(p,s)
for p in ['Tools/Art/ImportAzureVanguard.cs','Tools/Art/ValidateAzureVanguard.cs']:
    write(p,read(p).replace('CommonRigLabBuilder','SharedRigBuilder'))
p='Tools/Art/InstallCommonRigTraining.cs';s=read(p).replace('CommonRigLabBuilder.Root + "blue-tunic.prefab"','"Assets/Art/Characters/AzureVanguard/AzureVanguard.prefab"').replace('"Common rig visual"','"Character Visual"')
s=s.replace('        var legacy = player.GetComponent<CharacterAppearance>();\n        if (legacy != null) EditorUtility.SetDirty(legacy);\n','');write(p,s)
p='Tools/Art/ValidateCommonRigCombat.cs';s=read(p).replace(' && !p.GetComponent<Game.Characters.CharacterAppearance>().enabled',' && p.GetComponent<SpriteRenderer>() == null').replace('Shared rig installed; previous sheet assembly disabled','Shared rig installed; legacy root renderer removed');write(p,s)
# Copy just the shadow source to authoring storage; no production dependency on archived comparison skins.
shutil.copy2(root/'Assets/Art/Characters/CommonRigLab/blue-tunic.aseprite',root/'CharacterAuthoring/AzureVanguard/shadow-source.aseprite')
p='Tools/Art/PrepareAzureVanguard.lua';write(p,read(p).replace('Assets/Art/Characters/CommonRigLab/blue-tunic.aseprite','CharacterAuthoring/AzureVanguard/shadow-source.aseprite'))
manifest=json.loads(read('CharacterAuthoring/Archive/2026-10-11-character-cleanup/manifest.json'))
for name in manifest['retiredTools']:
    p=(root/name).resolve()
    assert p.is_relative_to(root/'Tools/Art') and p.is_file()
    p.unlink()
print('Shared rig-only source refactor complete; Unity asset retirement follows.')
