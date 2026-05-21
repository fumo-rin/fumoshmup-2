using FumoShmup2;
using rinCore;
using System.Collections;
using UnityEngine;

namespace MushilLike
{
    public partial class MushiLike
    {
        public class Stage4
        {
            public class Elites
            {
                [System.Serializable]
                public class BugFillScreen : UnitAttack
                {
                    public ProjectileDefineSO fillerProjectile;
                    public int repeats = 1;
                    [Range(7, 40)] public int steps = 40;
                    public bool waitWithLast = false;
                    public bool aimDown = true;
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        int last = steps;
                        for (int iteration = 0; iteration < repeats; iteration++)
                        {
                            bool isLast = iteration >= repeats - 1;
                            input.addedForward = 0.25f;
                            input.ReAimWithOptionalTarget(sender.CurrentPosition);
                            if (aimDown)
                                input.SetDirection(Vector2.down);
                            for (int i = 0; i < last + 1; i++)
                            {
                                for (int j = 0; j < 30; j++)
                                {
                                    if (i == 0)
                                    {
                                        Single(-180f, 5f + j.AsFloat(0.45f)).Spawn(input, fillerProjectile, out _);
                                    }
                                    else if (i == last)
                                    {
                                        Single(0f, 5f + j.AsFloat(0.45f)).Spawn(input, fillerProjectile, out _);
                                    }
                                    else
                                    {
                                        float angle = i.AsFloat().MapTo01(0f, last.AsFloat()).MapFrom01(0f, 180f);
                                        Single(-180f + angle, 5f + j.AsFloat(0.45f)).Spawn(input, fillerProjectile, out _);
                                        Single(-180f - angle, 5f + j.AsFloat(0.45f)).Spawn(input, fillerProjectile, out _);
                                    }
                                }
                                if (i % 2 == 0)
                                    yield return TICK.WaitForSeconds();
                            }
                            if (!isLast)
                                yield return TICK.WaitForSeconds(70);
                        }
                    }
                }
            }
        }
    }
}
