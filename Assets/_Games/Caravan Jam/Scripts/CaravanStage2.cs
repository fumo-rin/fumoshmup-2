using FumoShmup2;
using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Caravan
{
    public partial class CaravanAtatcks
    {
        public class Stage2
        {
            public class Boss
            {
                [Serializable]
                public class BossPhase1 : UnitAttack
                {
                    public ProjectileDefineSO wallsProj, ArcsProj;
                    bool flip;
                    public float duration = 7f;
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        flip = !flip;
                        int iteration = -20;
                        float remainingDuration = duration;
                        var input2 = input.Copy();
                        var ring = input.Copy();
                        input.SetDirection(Vector2.down);
                        while (sender != null && sender.IsAlive && remainingDuration > 0f)
                        {
                            float time = duration - remainingDuration;
                            void ArcWalls(Projectile.InputSettings input, float anglePerIteration, float distanceMod = 2f)
                            {
                                float distance = iteration.Clamp(0, 40).AsFloat(distanceMod * 0.025f);
                                float angle = anglePerIteration * iteration;
                                input.addedForward = 0f;
                                float arcSize = 150f - iteration.AsFloat().Clamp(0f, 135f);
                                float speed = 5f + iteration.AsFloat(0.1f).Clamp(0f, 10f);

                                Vector2 pos = sender.CurrentPosition + new Vector2(2.5f, 4f) + Vector2.right.Rotate2D(angle).ScaleToMagnitude(distance);
                                input.ReAimWithOptionalTarget(pos);
                                Arc(0f, arcSize, 2, speed).Spawn(input, wallsProj, out _);

                                pos = sender.CurrentPosition + new Vector2(-2.5f, 4f) + Vector2.right.Rotate2D(angle).ScaleToMagnitude(distance);

                                input.ReAimWithOptionalTarget(pos);
                                Arc(0f, arcSize, 2, speed).Spawn(input, wallsProj, out _);
                            }

                            int primeIteration = 0;
                            foreach (var item in 4.Primes(60))
                            {
                                ArcWalls(input2, (primeIteration % 2 == 0 ? item : -item) * 5 * TICK, 0.8f + primeIteration.AsFloat(0.65f));
                                primeIteration = primeIteration + 1;
                            }

                            if (iteration % 4 == 0)
                            {
                                input.addedForward = 0.15f;
                                input.SetOrigin(sender.CurrentPosition + new Vector2(4f, 1f));
                                Arc(iteration * 2f, 80f, 3, 5f).Spawn(input, ArcsProj, out _);
                                Arc(180f + iteration * 2f, 80f, 3, 7f).Spawn(input, ArcsProj, out _);

                                input.SetOrigin(sender.CurrentPosition + new Vector2(-4f, 1f));
                                Arc(iteration * -2f, 80f, 3, 5f).Spawn(input, ArcsProj, out _);
                                Arc(180f + iteration * -2f, 80f, 3, 7f).Spawn(input, ArcsProj, out _);
                            }
                            /*if (iteration % 12 == 0)
                            {
                                input.SetOrigin(sender.CurrentPosition + Vector2.up * 2f);
                                input.SetDirection(Vector2.down);
                                input.addedForward = 0.65f;
                                Circle(remainingDuration.Pow(1.35f) * 360f, 6, 4.5f).Spawn(input, rainProj, out _);
                            }*/

                            iteration = iteration + 1;
                            remainingDuration -= TICK;
                            yield return TICK.WaitForSeconds();
                        }
                        yield return TICK.WaitForSeconds(35);
                        ring.SetMods(new ProjectileModRotate(new(2f, 0.25f), flip.AsFloat(-30f, 30f)));
                        for (int j = 0; j < 5; j++)
                        {
                            for (int i = 0; i < 7; i++)
                            {
                                ring.SetOrigin(sender.CurrentPosition + new Vector2(0f, 3f));
                                ring.addedForward = 0.4f;
                                Circle(i.AsFloat(3f).Pow(1.35f) + j.AsFloat(-2f * j), 12, 4f + i.AsFloat(1.5f)).Spawn(ring, ArcsProj, out _);
                            }
                            yield return 0.15f.WaitForSeconds();
                        }
                        yield return 1.35f.WaitForSeconds();
                    }
                }
                [System.Serializable]
                public class BossPhase1Rage : UnitAttack
                {
                    public ProjectileDefineSO ringProjectile, ballProjectile;
                    List<Projectile> explodes = new();
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        explodes.Clear();
                        int step = 0;
                        var input2 = input.Copy();
                        input.SetMods(new ProjectileModAccelerate(new(3f, 0.25f), 0.1f, 12f));
                        for (int i = 0; i < 60; i++)
                        {
                            if (i == 0) continue;
                            Vector2 pos1 = sender.CurrentPosition + new Vector2(i.AsFloat(0.25f), i.AsFloat(0.05f));
                            Vector2 pos2 = sender.CurrentPosition + new Vector2(i.AsFloat(-0.25f), i.AsFloat(0.05f));
                            input.ReAimWithOptionalTarget(sender.CurrentPosition);
                            input.addedForward = 0f;
                            for (int j = 0; j < 4; j++)
                            {
                                input.SetOrigin(j % 2 == 0 ? pos1 : pos2);
                                float angleoffset = (j > 1 ? 90f : -90f) + step.AsFloat(3f).Pow(1.35f);
                                if (Single(angleoffset, (j % 2 == 0 ? 1f : 3f) + i.AsFloat(0.15f)).Spawn(input, ballProjectile, out Projectile p))
                                {
                                    explodes.Add(p);
                                    step++;
                                }
                            }
                            yield return TICK.WaitForSeconds(1);
                        }
                        yield return TICK.WaitForSeconds(1);
                        IEnumerator CO_Explode(Projectile.InputSettings input, List<Projectile> projList)
                        {
                            foreach (var item in projList)
                            {
                                if (item == null || !item.IsActive || !item.isOnScreen)
                                    continue;
                                input.ReAimWithOptionalTarget(item.Position);
                                float offset = RNG.FloatRange(-5f, 5f);
                                Circle(offset, 12, 4f).Spawn(input, ringProjectile, out _);
                                Circle(offset + (360f / 24), 12, 2.5f).Spawn(input, ringProjectile, out _);
                                ProjectileRenderer.BulletCancelParticle(item.Position, item.VelocityNotZero, 0.4f);
                                Projectile.Wipe(item);
                                yield return TICK.WaitForSeconds(1);
                            }
                            projList.Clear();
                        }
                        AttackRoutine(CO_Explode(input2, explodes.ToList()), sender);
                    }
                }
            }
        }
    }
}