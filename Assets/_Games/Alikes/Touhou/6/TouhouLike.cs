using FumoShmup2;
using rinCore;
using System.Collections;
using UnityEngine;

namespace TouhouLike
{
    public partial class Touhou
    {
        public partial class EOSD
        {
            public class Stage4
            {
                [System.Serializable]
                public class BooksAttack : UnitAttack
                {
                    public ProjectileDefineSO bookProjectile1, bookProjectile2;
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        input.addedForward = 0.3f;
                        var input2 = input.Copy();
                        input.SetMods(new ProjectileModAccelerate(new(0.5f, 0f), 6f, 14f));

                        int segments = 16;
                        float rng = RNG.FloatRange(0f, 360f);
                        for (int i = 0; i < 2; i++)
                        {
                            Circle(rng, segments, 12f + i.AsFloat(1.5f)).Spawn(input, bookProjectile1, out iterationList);
                            Circle(rng - (360f / (segments * 3f)), segments, 6f + i.AsFloat(1.5f)).Spawn(input2, bookProjectile2, out iterationList);
                        }

                        yield return TICK.WaitForSeconds(38);

                        input.ReAimWithOptionalTarget(sender.CurrentPosition);
                        input2.ReAimWithOptionalTarget(sender.CurrentPosition);
                        for (int i = 0; i < 0; i++)
                        {
                            Circle(RNG.FloatRange(0f, 360f), segments, 12f + i.AsFloat(1.5f)).Spawn(input, bookProjectile1, out iterationList);
                            Circle(RNG.FloatRange(-3f, 3f), segments, 6f + i.AsFloat(1.5f)).Spawn(input2, bookProjectile2, out iterationList);
                        }
                        float rng2 = RNG.FloatRange(0f, 360f);
                        for (int i = 0; i < 2; i++)
                        {
                            Circle(rng2, segments, 12f + i.AsFloat(1.5f)).Spawn(input, bookProjectile1, out iterationList);
                            Circle(rng2 - (360f / (segments * 3f)), segments, 6f + i.AsFloat(1.5f)).Spawn(input2, bookProjectile2, out iterationList);
                        }

                        yield return TICK.WaitForSeconds(24);
                    }
                }
            }
        }
    }
}
