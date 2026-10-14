using Game.Characters;
using UnityEngine;

namespace ProjectAr.CharacterRig.Sample
{
    [RequireComponent(typeof(CommonCharacterRig))]
    public sealed class MotionPreview : MonoBehaviour
    {
        private CommonCharacterRig rig;
        private float time;
        void OnEnable() { rig = GetComponent<CommonCharacterRig>(); time = 0; }
        void Update()
        {
            time += Time.deltaTime;
            int sequence = Mathf.FloorToInt(time / 3) % 8;
            var motion = sequence < 7 ? (RigUpperMotion)sequence : RigUpperMotion.Idle;
            Vector2 aim = new Vector2(Mathf.Cos(time * .5f), Mathf.Sin(time * .5f));
            rig.SampleContinuous(motion, aim, (Mathf.FloorToInt(time) % 4), time % .6f, time % .2f,
                sequence == 1, sequence == 7);
        }
    }
}
