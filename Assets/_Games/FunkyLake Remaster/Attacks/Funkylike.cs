using FumoShmup2;
using rinCore;
using System.Collections;
using UnityEngine;

public partial class Funkylike
{
    public partial class Stage1
    {
        public partial class Stage
        {
            [System.Serializable]
            public class FishFodder : UnitAttack
            {
                public ProjectileDefineSO armProjectile, spamProjectile;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {
                    input.addedForward = 0.2f;
                    Arc(0f, 15f, 2, 9.5f).Spawn(input, armProjectile, out _);
                    Single(0f, 10.5f).Spawn(input, armProjectile, out _);
                    for (int i = 0; i < 6; i++)
                    {
                        input.ReAimWithOptionalTarget(sender.CurrentPosition);
                        Arc(0f, 215f, 9, 6f + i.AsFloat(0.5f)).Spawn(input, armProjectile, out _);
                        yield return TICK.WaitForSeconds();
                    }
                    for (int i = 0; i < 12; i++)
                    {
                        Single(RNG.FloatRange(-2f, 2f), 10.5f.Spread(3f)).Spawn(input, spamProjectile, out _);
                    }
                }
            }
            [System.Serializable]
            public class FishArrowHead : UnitAttack
            {
                public ProjectileDefineSO projectile, arrowProjectile;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {
                    input.addedForward = 0.45f;
                    for (int i = 0; i < 5; i++)
                    {
                        Circle(i.AsFloat(360f / 48), 24, 6f + i.AsFloat()).Spawn(input, projectile, out _);
                    }
                    yield return TICK.WaitForSeconds(12);
                    for (int i = 0; i < 9; i++)
                    {
                        input.SetOrigin(sender.CurrentPosition);
                        int count = i * 2;
                        if (i == 0)
                        {
                            Single(0f, 8f).Spawn(input, arrowProjectile, out _);
                            yield return TICK.WaitForSeconds(4);
                            continue;
                        }
                        Arc(0f, 4f * count, 1 + count, 8f).Spawn(input, arrowProjectile, out _);
                        yield return TICK.WaitForSeconds(4);
                    }
                }
            }
            [System.Serializable]
            public class FishArrowRage : UnitAttack
            {
                public ProjectileDefineSO projectile;
                public bool Flip;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {
                    for (int i = 0; i < 240; i++)
                    {
                        input.SetOrigin(sender.CurrentPosition);
                        float speed = 9f - i.AsFloat(0.05f).Clamp(0f, 4f);
                        float angle = Flip.AsFloat(-1f, 1f) * i.AsFloat(2f) + RNG.FloatRange(-5f, 5f);
                        angle += Flip.AsFloat(20f, -20f);
                        if (!Circle(angle, 3, speed).Spawn(input, projectile, out _))
                        {
                            i--;
                            yield return TICK.WaitForSeconds();
                            continue;
                        }
                        yield return TICK.WaitForSeconds();
                    }
                }
            }
            [System.Serializable]
            public class FishBuff : UnitAttack
            {
                public ProjectileDefineSO projectile;
                public int repeats;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {
                    input.addedForward = 0.25f;
                    for (int l = 0; l < repeats; l++)
                    {
                        input.ReAimWithOptionalTarget(sender.CurrentPosition);
                        for (int i = 0; i < 3; i++)
                        {
                            Arc(0f, 100f, 5, 11f + i.AsFloat(1.5f)).Spawn(input, projectile, out _);
                        }
                        yield return TICK.WaitForSeconds(12);
                        input.SetOrigin(sender.CurrentPosition);
                        for (int i = 0; i < 3; i++)
                        {
                            Arc(0f, 80f, 4, 11f + i.AsFloat(1.5f)).Spawn(input, projectile, out _);
                        }
                        yield return TICK.WaitForSeconds(32);
                    }
                }
            }
            [System.Serializable]
            public class FishBuffEntry : UnitAttack
            {
                public ProjectileDefineSO projectile;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {
                    foreach (var angle in 60f.StepFromTo(-120f, 120f))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(-1.75f, 0f));
                            Arc(angle, 26f, 11, 10f + i.AsFloat(0.5f)).Spawn(input, projectile, out _);
                            input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(1.75f, 0f));
                            Arc(angle, 26f, 11, 10f + i.AsFloat(0.5f)).Spawn(input, projectile, out _);
                        }
                    }
                    foreach (var angle in 60f.StepFromTo(-90f, 90f))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(-1.75f, 0f));
                            Arc(angle, 15f, 7, 8f + i.AsFloat(0.35f)).Spawn(input, projectile, out _);
                            input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(1.75f, 0f));
                            Arc(angle, 15f, 7, 8f + i.AsFloat(0.35f)).Spawn(input, projectile, out _);
                        }
                    }
                    yield break;
                }
            }
            [System.Serializable]
            public class FrogReclined : UnitAttack
            {
                public ProjectileDefineSO projectile;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {

                    yield break;
                }
            }
        }
        public partial class Boss
        {

        }
    }
}
