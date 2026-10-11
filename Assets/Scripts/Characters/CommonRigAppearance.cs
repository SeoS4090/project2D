using UnityEngine;

namespace Game.Characters
{
    /// <summary>Four views of static cutout parts. Contains no motion frames.</summary>
    public sealed class CommonRigAppearance : ScriptableObject
    {
        public string appearanceId;
        public string rigId = "rig.sd-cutout.v1";
        public Sprite[] head, torso, upperArm, forearm, hand, leg, weapon, shadow;
        public bool IsValid => Valid(head) && Valid(torso) && Valid(upperArm) && Valid(forearm) &&
            Valid(hand) && Valid(leg) && Valid(weapon) && Valid(shadow);
        private static bool Valid(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length != 4) return false;
            foreach (var sprite in sprites) if (sprite == null) return false;
            return true;
        }
    }
}
