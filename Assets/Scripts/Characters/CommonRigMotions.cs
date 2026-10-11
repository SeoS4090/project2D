using UnityEngine;

namespace Game.Characters
{
    public enum RigUpperMotion { Idle, Walk, Slash, Reverse, Heavy, Charge, Spin }
    public sealed class CommonRigMotions : ScriptableObject
    {
        public string rigId = "rig.sd-cutout.v1";
        public AnimationClip[] idle, walk, slash, lowerIdle, lowerWalk, lowerDash;
        public AnimationClip[] reverse, heavy, charge, spin;
        public AvatarMask upperMask, lowerMask;
        public static bool Loops(RigUpperMotion motion) => motion == RigUpperMotion.Idle || motion == RigUpperMotion.Walk || motion == RigUpperMotion.Charge;
        public AnimationClip Upper(RigUpperMotion motion, int direction)
        {
            var clips = motion switch {
                RigUpperMotion.Slash => slash, RigUpperMotion.Reverse => reverse,
                RigUpperMotion.Heavy => heavy, RigUpperMotion.Charge => charge,
                RigUpperMotion.Spin => spin, RigUpperMotion.Walk => walk, _ => idle };
            return clips[direction];
        }
    }
}
