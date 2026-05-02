using UnityEngine;
using rinCore;

namespace FumoShmup2
{
    public class SweepOnKill
    {
        [System.Serializable]
        public struct SweepData
        {
            public float duration;
            public byte lootWeight;
            [SerializeField] ACWrapper sweepsound;
            public void PlaySound(Vector2 pos)
            {
                sweepsound.Play(pos);
            }
            public SweepData(float duration, byte lootWeight, ACWrapper sweepSound)
            {
                this.duration = duration;
                this.lootWeight = lootWeight;
                this.sweepsound = sweepSound;
            }
        }
        public static void SweepStuff(EnemyUnit owner, SweepData sweep, bool forceKill)
        {
            if (forceKill)
            {
                return;
            }
            if (owner is EnemyUnit e && !e.IsBoss)
            {
                SweepFlash.TriggerFlash(0.1f);
            }
            ProjectileRunner.TriggerSweep(sweep.duration, sweep.lootWeight, !forceKill, out _);
            if (owner != null) sweep.PlaySound(owner.CurrentPosition);
        }
    }
}
