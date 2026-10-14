using UnityEngine;

namespace Game.Characters
{
    public enum RigUpperMotion { Idle, Walk, Slash, Reverse, Heavy, Charge, Spin }
    public sealed class CommonRigMotions : ScriptableObject
    {
        public void Validate()
        {
            if (rigId != "rig.sd-cutout.v1") throw new System.ArgumentException("Unsupported rig contract: " + rigId);
            if (upperMask == null || lowerMask == null) throw new System.ArgumentException("Both rig masks are required.");
            var groups = new[] { idle, walk, slash, reverse, heavy, charge, spin, lowerIdle, lowerWalk, lowerDash };
            var names = new[] { "idle", "walk", "slash", "reverse", "heavy", "charge", "spin", "lowerIdle", "lowerWalk", "lowerDash" };
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null || groups[i].Length != 4) throw new System.ArgumentException(names[i] + " requires four directions.");
                foreach (var clip in groups[i])
                    if (clip == null || clip.length <= 0) throw new System.ArgumentException(names[i] + " contains a missing or empty clip.");
            }
        }
        public string rigId = "rig.sd-cutout.v1";
        public AnimationClip[] idle, walk, slash, lowerIdle, lowerWalk, lowerDash;
        public AnimationClip[] reverse, heavy, charge, spin;
        public AvatarMask upperMask, lowerMask;
        public static bool Loops(RigUpperMotion motion) => motion == RigUpperMotion.Idle || motion == RigUpperMotion.Walk || motion == RigUpperMotion.Charge;
        public AnimationClip Upper(RigUpperMotion motion, int direction)
        {
            if (direction < 0 || direction > 3 || motion < RigUpperMotion.Idle || motion > RigUpperMotion.Spin)
                throw new System.ArgumentOutOfRangeException("Invalid motion or direction.");
            var clips = motion switch {
                RigUpperMotion.Slash => slash, RigUpperMotion.Reverse => reverse,
                RigUpperMotion.Heavy => heavy, RigUpperMotion.Charge => charge,
                RigUpperMotion.Spin => spin, RigUpperMotion.Walk => walk, _ => idle };
            return clips[direction];
        }
    }
}
