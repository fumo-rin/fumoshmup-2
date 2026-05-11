using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FumoShmup2
{
    public class TestingAttacks
    {
        [System.Serializable]
        public class FutariS2BossRings : UnitAttack
        {
            public float duration = 20f;
            public ProjectileDefineSO ringProjectile, spamProjectile;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                float localDuration = duration;
                input.addedForward = 0.35f;
                int cycle = 0;
                float angle = RNG.FloatRange(-180f, 180f);
                float startTime = Time.time;
                float loopTime;
                while (localDuration > 0f && sender.IsAlive)
                {
                    input.SetDirection(Vector2.down);
                    loopTime = Time.time - startTime;
                    angle += RNG.FloatRange(-3f, 3f) * (cycle - 15).Clamp(0, 3);
                    if (cycle < 5)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            input.SetOrigin(sender.CurrentPosition);
                            Circle(angle + i + (cycle % 2 == 0 ? 360f / 40 : 0f), 20, 8.5f - cycle.AsFloat(1f).Clamp(0f, 4f)).Spawn(input, ringProjectile, out iterationList);
                        }
                        yield return 0.11f.WaitForSeconds();
                        if (cycle == 4)
                        {
                            yield return 0.20f.WaitForSeconds();
                        }
                        cycle++;
                        continue;
                    }
                    else
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            input.SetOrigin(sender.CurrentPosition);
                            Circle(angle + i + (cycle % 2 == 0 ? 360f / 40 : 0f), 20, 8.5f - cycle.AsFloat(1f).Clamp(0f, 4f)).Spawn(input, ringProjectile, out iterationList);
                        }
                    }
                    for (int i = 0; i < 7; i++)
                    {
                        loopTime = Time.time - startTime;
                        //float modifier = (loopTime.Multiply(90f).SineAmp(1.5f));
                        float modifier = (loopTime.Multiply(90f).SineAmp(1.5f));
                        int signedMod = modifier > 0 ? 1 : -1;
                        float finalMod = modifier.Absolute().Max(1f) * signedMod;
                        input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(-1.75f, 0.75f));
                        Circle((loopTime) * finalMod * 135f, 7, 6.5f).Spawn(input, spamProjectile, out iterationList);
                        input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(1.75f, 0.75f));
                        Circle(-(loopTime) * finalMod * 100f, 7, 6.5f).Spawn(input, spamProjectile, out iterationList);
                        localDuration -= 0.04f;
                        yield return 0.04f.WaitForSeconds();
                    }
                    cycle++;
                }
            }
        }
        [System.Serializable]
        public class ShotArc : UnitAttack
        {
            public ProjectileDefineSO shot;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                for (int i = 0; i < 13; i++)
                {
                    input.ReAimWithOptionalTarget(sender.CurrentPosition);
                    if (Arc(0f, 60f, 7, 6f).Spawn(input, shot, out _))
                    {
                        yield return 0.06f.WaitForSeconds();
                    }
                }
                yield return 0.1f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class ShotRandomFunny : UnitAttack
        {
            public ProjectileDefineSO proj;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                var input2 = input.Copy();
                input.SetMods(new ProjectileModAccelerate(new(0.5f, 0.5f), 3f, 12f), new ProjectileModAccelerate(new(0.5f, 1.5f), 12f, 12f));

                Vector2 a = new Vector2Shmup(0.25f, 0.7f).Vector2Now;
                Vector2 b = new Vector2Shmup(0.75f, 0.7f).Vector2Now;

                a.LineChop(b, 12, out List<Vector2> line);
                foreach (var item in line)
                {
                    input.SetOrigin(item);
                    input.ReAimWithOptionalTarget();
                    input.addedForward = 0.35f;
                    if (Circle(RNG.FloatRange(-3f, 3f), 15, 10f).Spawn(input, proj, out iterationList))
                    {
                        foreach (var projectile in iterationList)
                        {

                        }
                    }
                }

                for (int i = 0; i < 30; i++)
                {
                    //input.SetOrigin(sender.CurrentPosition);
                    /*if (ShmupPlayer.PlayerAs(out ShmupPlayer player))
                    {
                        input.SetDirection(player.CurrentPosition - input.Origin);
                    }*/

                    input.ReAimWithOptionalTarget();

                    Arc(RNG.FloatRange(-3f, 3f), 60f, 3, 12f).Spawn(input, proj, out _);
                    yield return 0.02f.WaitForSeconds();
                }

                yield return 0.35f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class ShotCircle : UnitAttack
        {
            public ProjectileDefineSO shot;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                Circle(0f, 16, 7.5f).Spawn(input, shot, out _);
                Circle(360f / 32, 16, 8.5f).Spawn(input, shot, out _);
                Circle(0f, 16, 9.5f).Spawn(input, shot, out _);
                Circle(360f / 32, 16, 10.5f).Spawn(input, shot, out _);
                yield return 0.2f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class StressTestSpam : UnitAttack
        {
            public ProjectileDefineSO shot;
            public int projectilePerSecond = 10000;
            public int seconds = 8;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                int repeats = 60;
                int shotCount = projectilePerSecond / repeats;
                int durationLeft = seconds;
                while (durationLeft > 0 && sender != null && sender.IsAlive)
                {
                    for (int i = 0; i < repeats; i++)
                    {
                        Circle(RNG.FloatRange(0f, 360f), shotCount, 11f).Spawn(input, shot, out _);
                        yield return 0.016f.WaitForSeconds();
                    }
                    durationLeft -= 1;
                }
            }
        }
        [System.Serializable]
        public class TestingBowap : UnitAttack
        {
            public ProjectileDefineSO bowapProjectile;
            public BowapSettings bowap = new()
            {
                angleSpin = -0.2f,
                anglePower = 1.75f,
                frameCount = 1,
                arms = 8,
                initialOffset = -20,
                startingSpeed = 7,
                maxSpeed = 11,
                speedRampTime = 6,
                duration = 20
            };
            [System.Serializable]
            public struct BowapSettings
            {
                public float angleSpin;
                public float anglePower;
                public int frameCount;
                public int arms;
                public float initialOffset;
                public float startingSpeed;
                public float maxSpeed;
                public float speedRampTime;
                public float duration;
            }
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                float lerpTime = 0f;
                int iteration = 0;
                while (lerpTime < bowap.duration && sender != null && sender.IsAlive)
                {
                    lerpTime = iteration * 0.01667f;
                    input.addedForward = 0.5f;
                    float ramp01 = lerpTime.MapTo01(0f, bowap.speedRampTime).Clamp(0f, 1f);
                    float angle = Mathf.Pow(iteration, bowap.anglePower) * bowap.angleSpin;
                    input.SetOrigin(sender.CurrentPosition);
                    if (iteration % bowap.frameCount.Max(1) == 0)
                    {
                        float lerpSpeed = bowap.startingSpeed.LerpUnclamped(bowap.maxSpeed, ramp01);
                        Circle(angle + bowap.initialOffset, bowap.arms, lerpSpeed).Spawn(input, bowapProjectile, out iterationList);
                    }
                    iteration += 1;
                    yield return 0.01667f.WaitForSeconds();
                }
            }
        }
    }
}
