using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace Game.Characters
{
    /// <summary>Shared Transform animation. Appearance sprites change only when appearance/view changes.</summary>
    [RequireComponent(typeof(Animator), typeof(SortingGroup))]
    public sealed class CommonCharacterRig : MonoBehaviour
    {
        public const float Ppu = 32f;
        [SerializeField] private CommonRigMotions motions;
        [SerializeField] private CommonRigAppearance appearance;
        [SerializeField] private Material material;
        private PlayableGraph graph;
        private AnimationClipPlayable upperPlayable, lowerPlayable;
        private AnimationLayerMixerPlayable mixer;
        private AnimationClip upperClip, lowerClip;
        private SpriteRenderer head, torso, mainArm, offArm, mainForearm, offForearm, mainHand, offHand, leftLeg, rightLeg, blade, shadow;
        private Transform mainTip, offTip, mainGrip, offGrip;
        private int upperDirection = -1, lowerDirection = -1;
        public CommonRigMotions Motions => motions;
        public CommonRigAppearance Appearance => appearance;
        public float MainGripErrorPixels => Vector3.Distance(mainTip.position, mainGrip.position) * Ppu;
        public float OffGripErrorPixels => Vector3.Distance(offTip.position, offGrip.position) * Ppu;
        public int SpriteAssignments { get; private set; }
        public float AimAngle { get; private set; }
        public float ReachCorrectionPixels { get; private set; }
        public int ViewDirection => upperDirection;
        public static int Direction(Vector2 direction) => Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x > 0 ? 3 : 2) : (direction.y > 0 ? 1 : 0);

        public void Initialize(CommonRigMotions sharedMotions, CommonRigAppearance skin, Material spriteMaterial)
        {
            if (sharedMotions == null || skin == null || !skin.IsValid || sharedMotions.rigId != skin.rigId)
                throw new InvalidOperationException("공통 리그/외형 호환 실패");
            motions = sharedMotions; appearance = skin; material = spriteMaterial;
            EnsureHierarchy(); SetAppearance(skin);
        }

        private Transform Node(string path)
        {
            var current = transform;
            foreach (var name in path.Split('/'))
            {
                var child = current.Find(name);
                if (child == null) { child = new GameObject(name).transform; child.SetParent(current, false); }
                current = child;
            }
            return current;
        }
        private SpriteRenderer Piece(string path)
        {
            var node = Node(path);
            var renderer = node.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = node.gameObject.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }
        private void EnsureHierarchy()
        {
            head = Piece("Upper/Head"); torso = Piece("Upper/Torso");
            mainArm = Piece("Upper/MainArm"); offArm = Piece("Upper/OffArm");
            mainForearm = Piece("Upper/MainArm/Forearm"); offForearm = Piece("Upper/OffArm/Forearm");
            mainTip = Node("Upper/MainArm/Forearm/Tip"); offTip = Node("Upper/OffArm/Forearm/Tip");
            mainTip.localPosition = offTip.localPosition = Vector3.right * 8 / Ppu;
            blade = Piece("Upper/Weapon/Blade");
            mainGrip = Node("Upper/Weapon/MainGrip"); offGrip = Node("Upper/Weapon/OffGrip");
            offGrip.localPosition = Vector3.left * 5 / Ppu;
            mainHand = Piece("Upper/Weapon/MainGrip/Hand"); offHand = Piece("Upper/Weapon/OffGrip/Hand");
            leftLeg = Piece("Lower/LeftLeg"); rightLeg = Piece("Lower/RightLeg"); shadow = Piece("Shadow");
            shadow.sortingOrder = -20; leftLeg.sortingOrder = -11; rightLeg.sortingOrder = -10;
            torso.sortingOrder = 4; head.sortingOrder = 8;
            mainArm.sortingOrder = mainForearm.sortingOrder = 6;
            offArm.sortingOrder = offForearm.sortingOrder = 2;
        }

        public void SetAppearance(CommonRigAppearance skin)
        {
            if (skin == null || !skin.IsValid || skin.rigId != motions.rigId) throw new InvalidOperationException("리그 외형 호환 실패");
            if (head == null) EnsureHierarchy();
            appearance = skin;
            int upper = Math.Max(0, upperDirection), lower = Math.Max(0, lowerDirection);
            upperDirection = lowerDirection = -1;
            SetViews(upper, lower);
        }

        private void Assign(SpriteRenderer renderer, Sprite sprite) { renderer.sprite = sprite; SpriteAssignments++; }
        private void SetViews(int upper, int lower)
        {
            if (upper != upperDirection)
            {
                Assign(head, appearance.head[upper]); Assign(torso, appearance.torso[upper]);
                Assign(mainArm, appearance.upperArm[upper]); Assign(offArm, appearance.upperArm[upper]);
                Assign(mainForearm, appearance.forearm[upper]); Assign(offForearm, appearance.forearm[upper]);
                Assign(mainHand, appearance.hand[upper]); Assign(offHand, appearance.hand[upper]);
                Assign(blade, appearance.weapon[upper]); Assign(shadow, appearance.shadow[upper]);
                blade.sortingOrder = upper == 1 ? 1 : 9;
                mainHand.sortingOrder = offHand.sortingOrder = upper == 1 ? 3 : 10;
                upperDirection = upper;
            }
            if (lower != lowerDirection)
            { Assign(leftLeg, appearance.leg[lower]); Assign(rightLeg, appearance.leg[lower]); lowerDirection = lower; }
        }

        public void Sample(RigUpperMotion motion, int aimDirection, int moveDirection, float upperTime, float lowerTime,
            bool moving, bool dash = false, bool snapRoot = false)
        {
            SamplePose(motion, aimDirection, aimDirection, moveDirection, upperTime, lowerTime, moving, dash, snapRoot);
            blade.sortingOrder = aimDirection == 1 ? 1 : 9;
            mainHand.sortingOrder = offHand.sortingOrder = aimDirection == 1 ? 3 : 10;
            mainArm.sortingOrder = mainForearm.sortingOrder = 6;
            offArm.sortingOrder = offForearm.sortingOrder = 2;
        }

        private void SamplePose(RigUpperMotion motion, int viewDirection, int clipDirection, int moveDirection,
            float upperTime, float lowerTime, bool moving, bool dash, bool snapRoot)
        {
            if (head == null) EnsureHierarchy();
            if (viewDirection < 0 || viewDirection > 3 || moveDirection < 0 || moveDirection > 3) throw new ArgumentOutOfRangeException();
            SetViews(viewDirection, moveDirection);
            var nextUpper = motions.Upper(motion, clipDirection);
            var nextLower = (dash ? motions.lowerDash : moving ? motions.lowerWalk : motions.lowerIdle)[moveDirection];
            if (!graph.IsValid()) CreateGraph();
            if (upperClip != nextUpper) { ReplaceClip(ref upperPlayable, nextUpper, 0); upperClip = nextUpper; }
            if (lowerClip != nextLower) { ReplaceClip(ref lowerPlayable, nextLower, 1); lowerClip = nextLower; }
            upperPlayable.SetTime(CommonRigMotions.Loops(motion) ? Mathf.Repeat(upperTime, nextUpper.length) : Mathf.Clamp(upperTime, 0, nextUpper.length));
            lowerPlayable.SetTime(dash ? Mathf.Clamp(lowerTime, 0, nextLower.length) : Mathf.Repeat(lowerTime, nextLower.length));
            graph.Evaluate(0);
            // Root snapping changes presentation only, never the actor's logical position or grip offsets.
            if (snapRoot)
            {
                var upper = transform.Find("Upper"); var lower = transform.Find("Lower");
                upper.localPosition = Snap(upper.localPosition); lower.localPosition = Snap(lower.localPosition);
            }
        }

        /// <summary>Canonical shared clip plus visual-only aim correction. Never feeds back into combat direction.</summary>
        public void SampleContinuous(RigUpperMotion motion, Vector2 aim, int moveDirection, float upperTime,
            float lowerTime, bool moving, bool dash = false)
        {
            if (aim.sqrMagnitude < .000001f) aim = Vector2.down;
            AimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            SamplePose(motion, Direction(aim), 3, moveDirection, upperTime, lowerTime, moving, dash, false);
            var weapon = mainGrip.parent;
            var pivot = new Vector3(0, 17 / Ppu, 0);
            var rotation = Quaternion.Euler(0, 0, AimAngle);
            weapon.localPosition = pivot + rotation * (weapon.localPosition - pivot);
            weapon.localRotation = rotation * weapon.localRotation;
            // Both grips stay rigidly attached to the weapon. This conservative reach envelope works at every angle.
            var midpoint = (mainGrip.localPosition + offGrip.localPosition) * .5f;
            var center = weapon.localPosition + weapon.localRotation * midpoint;
            var safe = pivot + Vector3.ClampMagnitude(center - pivot, 9.4f / Ppu);
            ReachCorrectionPixels = Vector3.Distance(safe, center) * Ppu;
            weapon.localPosition += safe - center;
            SolveArm(mainArm.transform, mainForearm.transform, mainGrip, 1);
            SolveArm(offArm.transform, offForearm.transform, offGrip, -1);
            // A spinning weapon crosses front/back independently of the body's facing sprite.
            bool behind = safe.y > pivot.y;
            blade.sortingOrder = behind ? 1 : 9;
            mainHand.sortingOrder = offHand.sortingOrder = behind ? 3 : 10;
            mainArm.sortingOrder = mainForearm.sortingOrder = behind ? 2 : 6;
            offArm.sortingOrder = offForearm.sortingOrder = 2;
        }

        private static void SolveArm(Transform arm, Transform forearm, Transform grip, int bend)
        {
            Vector2 shoulder = arm.localPosition;
            Vector2 target = arm.parent.InverseTransformPoint(grip.position);
            var delta = target - shoulder;
            float angle = Mathf.Atan2(delta.y, delta.x) + bend * Mathf.Acos(Mathf.Clamp(delta.magnitude * Ppu / 16, 0, 1));
            Vector2 elbow = shoulder + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (8 / Ppu);
            arm.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            forearm.localPosition = Vector3.right * (8 / Ppu);
            forearm.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(target.y - elbow.y, target.x - elbow.x) * Mathf.Rad2Deg - angle * Mathf.Rad2Deg);
        }
        private static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x * Ppu) / Ppu, Mathf.Round(p.y * Ppu) / Ppu, p.z);
        private void CreateGraph()
        {
            var animator = GetComponent<Animator>(); animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create("Shared cutout rig"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationLayerMixerPlayable.Create(graph, 2);
            mixer.SetLayerMaskFromAvatarMask(0, motions.upperMask); mixer.SetLayerMaskFromAvatarMask(1, motions.lowerMask);
            var output = AnimationPlayableOutput.Create(graph, "Character pose", animator); output.SetSourcePlayable(mixer);
            graph.Play();
        }
        private void ReplaceClip(ref AnimationClipPlayable playable, AnimationClip clip, int input)
        {
            if (playable.IsValid()) { graph.Disconnect(mixer, input); graph.DestroyPlayable(playable); }
            playable = AnimationClipPlayable.Create(graph, clip); playable.SetSpeed(0);
            playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false);
            graph.Connect(playable, 0, mixer, input); mixer.SetInputWeight(input, 1);
        }
        private void OnDisable()
        {
            if (graph.IsValid()) graph.Destroy();
            upperClip = lowerClip = null;
        }
    }
}
