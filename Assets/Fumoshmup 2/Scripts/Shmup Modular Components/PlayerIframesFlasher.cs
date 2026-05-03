using rinCore;
using UnityEngine;

namespace FumoShmup2
{
    public class PlayerIframesFlasher : SpriteFlashMaterial
    {
        void Activate(float duration)
        {
            TriggerFlashMaterial(duration);
        }
        private void OnEnable()
        {
            ShmupPlayer.WhenIframesActivatedGetDuration += Activate;
        }
        private void OnDisable()
        {
            ShmupPlayer.WhenIframesActivatedGetDuration -= Activate;
        }
    }
}
