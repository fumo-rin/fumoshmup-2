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
            public SweepData(float duration, byte lootWeight)
            {
                this.duration = duration;
                this.lootWeight = lootWeight;
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
        }
    }
}
