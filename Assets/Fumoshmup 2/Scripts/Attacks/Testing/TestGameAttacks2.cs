using UnityEngine;
using rinCore;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
namespace FumoShmup2
{
    public class TestGameAttacks2
    {
        [System.Serializable]
        public class ShowcaseVitalityPojjer : UnitAttack
        {
            public ProjectileDefineSO proj, frontalProj;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                var spiral = RinHelper.Vec2List.GenerateSpiral(50, 7, -8f, 0.25f, 0f, false);
                int iteration = 0;
                var input2 = input.Copy();
                input.SetMods(new ProjectileModAccelerate(new(2f, 0.65f), 6f, 2f));
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    input2.ReAimWithOptionalTarget(sender.CurrentPosition);
                    for (int i = 0; i < 35; i++)
                    {
                        for (int j = 0; j < 11; j++)
                        {
                            float angle = -(j - 5).AsFloat(1f / 3f) * 30f;
                            input2.SetMods(new ProjectileModRotate(new(2f, 0f), angle));
                            Single((-5 + j).AsFloat(4f), 4f + (i).AsFloat(0.16f)).Spawn(input2, frontalProj, out Projectile p);
                        }
                    }
                    if (repeat < 1)
                        yield return 1f.WaitForSeconds();
                }
                foreach (var item in spiral.Sequence())
                {
                    input.SetOrigin(item.point + sender.CurrentPosition);
                    input.SetDirection(item.tangent);
                    if (input.OptionalTarget != null && input.OptionalTarget.CurrentPosition.SquareDistanceToLessThan(input.Origin, 3f))
                    {
                        iteration++;
                        yield return 0.01667f.WaitForSeconds();
                        continue;
                    }
                    Circle(iteration.AsFloat(3f), 4 + iteration.AsFloat(0.35f).ToInt(), 2f).Spawn(input, proj, out iterationList);
                    iteration++;
                    yield return 0.01667f.WaitForSeconds();
                }
                yield return 1f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class ShowcaseVitalityPojjerV2 : UnitAttack
        {
            public ProjectileDefineSO proj, frontalProj;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                var spiral = RinHelper.Vec2List.GenerateSpiral(50, 7, -8f, 0.25f, 0f, false);
                int iteration = 0;
                var input2 = input.Copy();
                input.SetMods(new ProjectileModAccelerate(new(2f, 0.65f), 6f, 2f));
                input2.ReAimWithOptionalTarget(sender.CurrentPosition);

                for (int i = 0; i < 17; i++)
                {
                    for (int j = 0; j < 23; j++)
                    {
                        if (j - 11 == 0)
                            continue;

                        float angle = -(j - 11).AsFloat(1f / 6f) * 30f;
                        float speed = 6f;
                        float targetSpeed = 2f + i.AsFloat(0.4f);
                        input2.SetMods(new ProjectileModRotate(new(1f, 0.4f), angle * i.AsFloat(1.03f)),
                            new ProjectileModAccelerate(new(0.4f, 0f), 0f, speed * 2.5f),
                            new ProjectileModAccelerate(new(0.5f, 0.4f), targetSpeed, targetSpeed * 2f));
                        Single(0f, speed).Spawn(input2, frontalProj, out Projectile p);
                    }
                }
                yield return 1f.WaitForSeconds();
                foreach (var item in spiral.Sequence())
                {
                    input.SetOrigin(item.point + sender.CurrentPosition);
                    input.SetDirection(item.tangent);
                    if (input.OptionalTarget != null && input.OptionalTarget.CurrentPosition.SquareDistanceToLessThan(input.Origin, 3f))
                    {
                        iteration++;
                        yield return 0.01667f.WaitForSeconds();
                        continue;
                    }
                    Circle(iteration.AsFloat(3f), 4 + iteration.AsFloat(0.35f).ToInt(), 2f).Spawn(input, proj, out iterationList);
                    iteration++;
                    yield return 0.01667f.WaitForSeconds();
                }
                yield return 1.6f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class DoremyTestCharge : UnitAttack
        {
            public ProjectileDefineSO pellet;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                for (int i = 0; i < 50; i++)
                {
                    input.SetOrigin(sender.CurrentPosition);
                    Arc(0f, 30f + (i * i).AsFloat(0.45f).Mod(210f), 2, 4f + i.AsFloat(0.4f).Min(11f)).Spawn(input, pellet, out iterationList);
                    if (i > 30)
                    {
                        Circle(-20f + (i - 35).AsFloat(3f), 5, 4f).Spawn(input, pellet, out iterationList);
                        Circle(355f + (i - 35).AsFloat(-4f), 5, 3.15f).Spawn(input, pellet, out iterationList);
                        Circle(-80f + (i - 37).AsFloat(7f + i), 5, 5.55f).Spawn(input, pellet, out iterationList);
                    }
                    yield return 0.01667f.WaitForSeconds();
                }
            }
        }
        [System.Serializable]
        public class DoremyTestAttack : UnitAttack
        {
            public ProjectileDefineSO pellet, ball;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                bool flip = false;
                var input2 = input.Copy();
                if (input.OptionalTarget != null)
                    input.SetMods(new ProjectileModAccelerate(new(1.35f, 0f), 3f, 6f));
                input2.SetMods(new ProjectileModGravity(new(8f, 0f), 1.5f, 3f));

                for (int i = 0; i < 240; i++)
                {
                    input.SetOrigin(sender.CurrentPosition);
                    input2.SetOrigin(sender.CurrentPosition);
                    float angle = (-45f - i.AsFloat(0.5f)) + (i.AsFloat(0.15f * i.AsFloat(0.15f)) * 2f).Mod(90f + i.AsFloat());
                    input.addedForward = 2.5f - i.AsFloat(0.1f).Clamp(0f, 2f);
                    Arc(flip.AsFloat(-angle, angle), 360 - i.AsFloat(2f), 5, 8f - i.AsFloat(0.035f).Clamp(0f, 4f)).Spawn(input, pellet, out iterationList);
                    if (i % 18 < 8)
                    {
                        input.addedForward = (i % 8 * 0.3f) + 0.3f;
                        input.SetOrigin(sender.CurrentPosition + Vector2.right.Rotate2D(120f));
                        Circle(i.AsFloat(2f), 8, 7f).Spawn(input, ball, out iterationList);
                        input.SetOrigin(sender.CurrentPosition + Vector2.right.Rotate2D(240f));
                        Circle(-i.AsFloat(2f), 8, 6f).Spawn(input, ball, out iterationList);
                        input.SetOrigin(sender.CurrentPosition + Vector2.right);
                        Circle(i.AsFloat(2f), 8, 5f).Spawn(input, ball, out iterationList);
                    }
                    if (i % 36 == 0)
                    {

                        input2.addedForward = 1f;
                        Circle(flip.AsFloat(-angle, angle) * 3f, 11, 2f).Spawn(input2, ball, out iterationList);
                        input2.addedForward = 1.5f;
                        Circle(flip.AsFloat(-angle, angle) * 5f, 19, 3f).Spawn(input2, ball, out iterationList);
                        input2.addedForward = 2f;
                        Circle(flip.AsFloat(-angle, angle) * 8f, 30, 4f).Spawn(input2, ball, out iterationList);
                        input2.addedForward = 2.5f;
                        Circle(flip.AsFloat(-angle, angle) * 11f, 49, 5f).Spawn(input2, ball, out iterationList);
                        if (i > 0)
                        {
                            input.mods = null;
                            flip = !flip;
                            yield return 0.12f.WaitForSeconds();
                        }
                    }
                    yield return 0.03f.WaitForSeconds();
                }
            }
        }
        [System.Serializable]
        public class TriplePagoda : UnitAttack
        {
            public ProjectileDefineSO trailingShot, mazeShot;
            public int repeats = 8;
            public float delayBetweenRepeats = 2.25f;
            public int mazeArms = 2;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                bool flip = false;
                float startTime = Time.time;
                IEnumerator Secondary(Projectile p, float startSpin, bool flip)
                {
                    IEnumerator SpawnAfter(Projectile.InputSettings input, float delay, float spin, bool flip, float projectileSpeed)
                    {
                        yield return delay.WaitForSeconds();
                        Circle(flip.AsFloat(-spin, spin), mazeArms, projectileSpeed).Spawn(input, mazeShot, out iterationList);
                    }
                    Projectile.InputSettings secondaryInput = input.Copy();
                    secondaryInput.SetMods(new ProjectileModGravity(new ProjectileModSettings(4f, 2.5f), 4f, 4f));
                    float spin = startSpin;
                    secondaryInput.addedForward = 0.15f;
                    float projSpeed = 6f;
                    while (p != null && p.IsActive && p.isOnScreen)
                    {
                        secondaryInput.SetOrigin(p.Position);
                        secondaryInput.SetDirection(p.VelocityNotZero);
                        spin += (Time.time - startTime).Mod(1f).SineAmp(1800f).Max(5f);
                        SpawnAfter(secondaryInput, 0.15f, spin, flip, projSpeed.Max(2f)).RunRoutine();
                        projSpeed -= 0.35f;
                        yield return 0.03f.WaitForSeconds();
                    }
                }
                input.addedForward = 0.65f;
                for (int i = 0; i < repeats && sender != null && sender.IsAlive; i++)
                {
                    input.ReAimWithOptionalTarget(sender.CurrentPosition);
                    if (Arc(0f, 135f, 4, 8f).Spawn(input, trailingShot, out iterationList))
                    {
                        int iteration = 0;
                        foreach (var item in iterationList)
                        {
                            Secondary(item, iteration.AsFloat(240f), flip).RunRoutine(null, false);
                            iteration += 1;
                        }
                    }
                    flip = !flip;
                    yield return delayBetweenRepeats.WaitForSeconds();
                }
            }
        }
        [System.Serializable]
        public class TriplePagodaAlternate : UnitAttack
        {
            public ProjectileDefineSO trailingShot, mazeShot;
            public int mazeArms = 2;
            public int arcShots = 6;
            public float arcAngle = 200f;
            public float endStallDuration = 1.45f;
            public float delayUntilSecondaryShots = 0.75f;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                float startTime = Time.time;
                bool flip = true;
                IEnumerator Secondary(Projectile p, float startSpin, bool flip)
                {
                    IEnumerator SpawnAfter(Projectile.InputSettings input, float delay, float spin, bool flip, float projectileSpeed)
                    {
                        yield return delay.WaitForSeconds();
                        Circle(flip.AsFloat(-spin, spin), mazeArms, projectileSpeed).Spawn(input, mazeShot, out iterationList);
                    }
                    Projectile.InputSettings secondaryInput = input.Copy();
                    float spin = startSpin;
                    secondaryInput.addedForward = 0.15f;
                    float projSpeed = 3f;
                    float totalDelay = delayUntilSecondaryShots;
                    while (p != null && p.IsActive && p.isOnScreen && totalDelay > -0.35f)
                    {
                        secondaryInput.SetOrigin(p.Position);
                        secondaryInput.SetDirection(p.VelocityNotZero);
                        spin += (Time.time - startTime).Mod(1f).SineAmp(1800f).Max(5f);
                        SpawnAfter(secondaryInput, totalDelay, spin, flip, projSpeed).RunRoutine();
                        totalDelay -= 0.04f;
                        yield return 0.03f.WaitForSeconds();
                    }
                    Projectile.Wipe(p);
                }
                input.addedForward = 0.65f;
                input.ReAimWithOptionalTarget(sender.CurrentPosition);
                Projectile.InputSettings arcInput = input.Copy();
                float targetSpeed = 4f;
                arcInput.SetMods((List<ProjectileMod>)new()
                {
                    new ProjectileModAccelerate(new(delayUntilSecondaryShots, 0.1f), targetSpeed, (12f - targetSpeed)/ delayUntilSecondaryShots)
                });
                if (Arc(0f, arcAngle, arcShots, 8f).Spawn(arcInput, trailingShot, out iterationList))
                {
                    int iteration = 0;
                    foreach (var item in iterationList)
                    {
                        Secondary(item, iteration.AsFloat(240f), flip).RunRoutine(null, false);
                        iteration += 1;
                    }
                }
                flip = !flip;
                yield return endStallDuration.WaitForSeconds();
            }
        }
        public class Imported
        {
            [System.Serializable]
            public class SQ_Fodder : UnitAttack
            {
                public ProjectileDefineSO arrowheadSpawner, arrowHead, spinnyProjectile;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {
                    IEnumerator Co_ShootWithDelay(Projectile.InputSettings input, float delay)
                    {
                        yield return delay.WaitForSeconds();
                        if (input.Sender == null || !input.Sender.IsAlive)
                            yield break;
                        input.ReAimWithOptionalTarget(input.Sender.CurrentPosition);
                        if (Single(0f, 12f).Spawn(input, arrowheadSpawner, out Projectile p))
                        {
                            ArrowHead(input, p, new(0.45f, 4f, 0.035f, 9f, 14));
                        }
                    }
                    var arrowInput = input.Copy();
                    arrowInput.mods = new()
                    {
                        new ProjectileModAccelerate(new(1f,0f),4f,18f)

                    };
                    AttackRoutine(Co_ShootWithDelay(arrowInput, 0.75f), sender);
                    AttackRoutine(Co_ShootWithDelay(arrowInput, 0f), sender);

                    Spinny(input, new(1.25f, 0f, 540f, 7f, 0.15f, 50, 3, new(-1.65f, 0.75f)));
                    Spinny(input, new(1.25f, 0f, -540f, 7f, 0.15f, 50, 3, new(1.65f, 0.75f)));

                    Spinny(input, new(1.5f, 1.25f, 270f, 7f, 0.15f, 30, 3, new(-1.65f, 2.75f)));
                    Spinny(input, new(1.5f, 1.25f, -270f, 7f, 0.15f, 30, 3, new(1.65f, 2.75f)));
                    yield return 3.25f.WaitForSeconds();
                }
                struct SpinnyData
                {
                    public float duration, delay, rotationPerSecond, projectileSpeed, repeatSpeedMultiPercent;
                    public int shotRepeats, repeatProjectiles;
                    public Vector2 offset;
                    public SpinnyData(float duration, float delay, float rotationPerSecond, float projectileSpeed, float repeatSpeedMulti, int shotRepeats, int repeatProjectiles, Vector2 offset)
                    {
                        this.duration = duration;
                        this.delay = delay;
                        this.rotationPerSecond = rotationPerSecond;
                        this.projectileSpeed = projectileSpeed;
                        this.repeatSpeedMultiPercent = repeatSpeedMulti;
                        this.shotRepeats = shotRepeats;
                        this.repeatProjectiles = repeatProjectiles;
                        this.offset = offset;
                    }
                }
                void Spinny(Projectile.InputSettings input, SpinnyData spinny)
                {
                    IEnumerator CO_Spinny()
                    {
                        float runningDuration = spinny.duration;
                        float timePerShot = spinny.duration / spinny.shotRepeats;
                        float rotation = 0f;
                        yield return spinny.delay.WaitForSeconds();
                        input.ReAimWithOptionalTarget(input.Sender.CurrentPosition);
                        input.addedForward = 0.35f;
                        while (runningDuration > 0 && input.Sender != null)
                        {
                            input.SetOrigin(input.Sender.CurrentPosition + spinny.offset);

                            for (int i = 0; i < spinny.repeatProjectiles; i++)
                            {
                                float extraAdded = i.AsFloat() * 2f * rotation.Sign();
                                if (Arc(90f + extraAdded + rotation, 180f, 2, spinny.projectileSpeed * (1f + i.AsFloat() * spinny.repeatSpeedMultiPercent)).Spawn(input, spinnyProjectile, out iterationList))
                                {

                                }
                            }

                            runningDuration -= timePerShot;
                            rotation += spinny.rotationPerSecond * timePerShot;
                            yield return timePerShot.WaitForSeconds();
                        }
                        yield break;
                    }
                    AttackRoutine(CO_Spinny(), input.Sender);
                }
                struct ArrowHeadData
                {
                    public float delay, angleIncrement, repeatDelay, projSpeed;
                    public int repeats;
                    public ArrowHeadData(float delay, float angleIncrement, float repeatDelay, float projSpeed, int repeats)
                    {
                        this.delay = delay;
                        this.angleIncrement = angleIncrement;
                        this.repeatDelay = repeatDelay;
                        this.projSpeed = projSpeed;
                        this.repeats = repeats;
                    }
                }
                void ArrowHead(Projectile.InputSettings input, Projectile p, ArrowHeadData data)
                {
                    IEnumerator CO_Arrow()
                    {
                        yield return data.delay.WaitForSeconds();
                        if (p == null || !p.IsActive || !p.isOnScreen)
                        {
                            yield break;
                        }
                        input.SetOrigin(p.Position);
                        input.SetDirection(p.EffectiveVelocity);
                        input.addedForward = 0.35f;
                        Projectile.Wipe(p);
                        if (Single(0f, data.projSpeed).Spawn(input, arrowHead, out Projectile arrow1))
                        {

                            yield return data.repeatDelay.WaitForSeconds();
                        }
                        for (int i = 0; i < data.repeats; i++)
                        {
                            if (Arc(0f, (i + 1).AsFloat().Multiply(data.angleIncrement), i + 1, data.projSpeed).Spawn(input, arrowHead, out iterationList))
                            {

                            }
                            yield return data.repeatDelay.WaitForSeconds();
                        }
                    }
                    AttackRoutine(CO_Arrow(), input.Sender);
                }
            }
        }
        [System.Serializable]
        public class Bowap : UnitAttack
        {
            public ProjectileDefineSO bowapShot;
            public float duration = 20f;
            public float maxSpeed = 14f;
            public float minSpeed = 8f;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                yield return 1.25f.WaitForSeconds();
                float end = Time.time + duration;
                float angle = -30f;
                float startTime = Time.time;
                int iteration = 0;
                float speedDelta = maxSpeed - minSpeed;
                float speedIncrement = speedDelta / 90f;
                input.SetDirection(Vector2.down);
                input.addedForward = 0.65f;
                while (Time.time < end)
                {
                    input.SetOrigin(sender.CurrentPosition);
                    Circle(angle - (iteration * iteration).AsFloat(0.065f), 8, minSpeed + iteration.AsFloat(speedIncrement).Min(speedDelta)).Spawn(input, bowapShot, out iterationList);
                    yield return 0.01667f.WaitForSeconds();
                    iteration += 1;
                }
            }
        }
        [System.Serializable]
        public class DoubleRingWithObstacles : UnitAttack
        {
            public int ringCount = 16;
            public float duration = 11f;
            public int ringsPerObstacle = 4;
            public ProjectileDefineSO ringProjectile, obstacleProjectile;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                float repeatDelayThird = duration / (ringCount.AsFloat() * 3);
                input.addedForward = 0.35f;
                bool flippedObstacles = false;
                for (int i = 0; i < ringCount; i++)
                {
                    bool flippedPosition = i % 2 == 1;
                    Vector2 offset = new Vector2(flippedPosition ? -2.25f : 2.25f, 1.5f) + RNG.SeededRandomVector2;
                    input.ReAimWithOptionalTarget(sender.CurrentPosition + offset);
                    Circle(RNG.FloatRange(-4f, 4f), 24, 7f).Spawn(input, ringProjectile, out iterationList);
                    yield return repeatDelayThird.WaitForSeconds();
                    if (i % ringsPerObstacle == 0 && i > 0)
                    {
                        input.ReAimWithOptionalTarget(sender.CurrentPosition);
                        for (int j = 1; j < 6; j++)
                        {
                            float addedAngle = j.AsFloat(flippedObstacles.AsFloat(-1.35f, 1.35f)) + (flippedObstacles ? -45f : 45f);
                            if (Arc(addedAngle, 210f, 24, 4f + j.AsFloat(1.25f)).Spawn(input, obstacleProjectile, out iterationList))
                            {
                                float lerp = 0f;
                                foreach (var iteration in iterationList)
                                {
                                    lerp += 1f / (iterationList.Count - 1);
                                    float modifiedSpeed = flippedObstacles ? (1.65f - lerp) : (0.65f + lerp);
                                    iteration.Action_ModifySpeed(modifiedSpeed);
                                }
                            }
                        }
                    }
                    flippedObstacles = !flippedObstacles;
                    yield return (repeatDelayThird * 2).WaitForSeconds();
                }
            }
        }
        [System.Serializable]
        public class MockupRevengeAttackExplosion : UnitAttack
        {
            [SerializeField] ProjectileDefineSO projectile;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                input.addedForward = 0.85f;
                float iteration = 360f.RandomPositiveNegativeRange();
                for (int i = 0; i < 10; i++)
                {
                    Circle(iteration, 18, 4f + i.AsFloat(0.35f)).Spawn(input, projectile, out iterationList);
                    iteration += RNG.RandomFloatRange(-3f, 3f) + 4f;
                }
                yield return new RevengeAttack.WaitForSweepOrTime(0.35f);
                for (int i = 0; i < 10; i++)
                {
                    Circle(iteration, 18, 6f + i.AsFloat(0.65f)).Spawn(input, projectile, out iterationList);
                    iteration += RNG.RandomFloatRange(-3f, 3f) - 4f;
                }
            }
        }
        [System.Serializable]
        public class MushiS4Lines : UnitAttack
        {
            public ProjectileDefineSO define;
            public ProjectileDefineSO secondaryProjectile;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                void ArcShot(float arcSize, float speed, int segments, float delay)
                {
                    input.addedForward = 0.35f;
                    input.Flare = define.useFlare;
                    if (Arc(0f, arcSize, segments, speed).Spawn(input, define, out iterationList))
                    {
                        SecondaryAfter(delay, iterationList, input);
                    }
                }
                void SecondaryAfter(float delay, List<Projectile> projectiles, Projectile.InputSettings input)
                {
                    IEnumerator CO_Run(Projectile.InputSettings input)
                    {
                        if (input.OptionalTarget == null)
                            yield break;
                        yield return delay.WaitForSeconds();
                        foreach (var item in projectiles.ToList())
                        {
                            if (item == null || !item.IsActive || !item.isOnScreen)
                                continue;
                            input.ReAimWithOptionalTarget(item.Position);
                            input.addedForward = 0f;
                            input.Flare = false;
                            Single(0f, 9f).Spawn(input, secondaryProjectile, out Projectile p);
                        }
                    }
                    StageRoutines.StartRoutine("s4 mushi runner", CO_Run(input), false);
                }

                yield return 0.25f.WaitForSeconds();
                for (int i = 0; i < 17; i++)
                {
                    input.ReAimWithOptionalTarget(sender.CurrentPosition);
                    ArcShot((80f - i.AsFloat(3f)).Max(40f) * 1.65f, 18f, 3, 0.2f);
                    yield return 0.03f.WaitForSeconds();
                }
                yield return 0.6f.WaitForSeconds();
                for (int i = 0; i < 48; i++)
                {
                    input.ReAimWithOptionalTarget(sender.CurrentPosition);
                    ArcShot((65f - i.AsFloat(2f)).Max(25f) * 1.65f, 15f, 5, 0.35f);
                    yield return 0.03f.WaitForSeconds();
                }
                yield return 0.6f.WaitForSeconds();
                for (int i = 0; i < 76; i++)
                {
                    input.ReAimWithOptionalTarget(sender.CurrentPosition);
                    ArcShot((50f - i.AsFloat(0.5f)).Max(12f) * 3f, 12f, 7, 0.4f);
                    yield return 0.03f.WaitForSeconds();
                }
                yield return 0.75f.WaitForSeconds();
            }
        }
        [System.Serializable]
        public class TestLarsaSpam : UnitAttack
        {
            public ProjectileDefineSO define;
            public ACWrapper shotSound;
            public float durationInSeconds = 13f;
            public float delayBetweenShots = 0.06f;
            public float switchRngEverySeconds = 0.5f;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                int remainingShots = (durationInSeconds / delayBetweenShots).ToInt();
                float nextRng;
                int seed, seed2, seed3;
                void NewRNG()
                {
                    nextRng = switchRngEverySeconds;
                    seed = RNG.Int255.Add(67).Mod(11) + 1;
                    seed2 = RNG.Int255.Add(48).Mod(13) + 1;
                    seed3 = RNG.Int255.Add(73).Mod(7) + 1;
                }
                NewRNG();
                input.addedForward = 0.65f;
                for (int i = 0; i < remainingShots; i++)
                {
                    nextRng -= delayBetweenShots;
                    if (nextRng <= 0f)
                    {
                        NewRNG();
                    }
                    input.SetOrigin(sender.CurrentPosition);
                    input.SetDirection(Vector2.down.Rotate2D(Time.time * 15f % 360f));
                    Circle((seed * (i + 3)).AsFloat(11f), 14, 6f).Spawn(input, define, out iterationList);
                    Circle((seed * (i + 3)).AsFloat(-11f), 14, 6f).Spawn(input, define, out iterationList);
                    Circle((-seed2 * (i + 6)).AsFloat(13f), 14, 8f).Spawn(input, define, out iterationList);
                    Circle((-seed3 * (i + 8)).AsFloat(19f), 14, 4f).Spawn(input, define, out iterationList);
                    yield return delayBetweenShots.WaitForSeconds();
                    remainingShots -= 1;
                }
            }
        }
        [System.Serializable]
        public class TestSDOJInbachi : UnitAttack
        {
            public ProjectileDefineSO define;
            public ProjectileDefineSO rageOverlap;
            public ACWrapper shotSound;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                float random = RNG.RandomFloatRange(0f, 360f).ReverseQuantize(60f);
                int flip = RNG.RandomSign;
                IEnumerator CO_Sidechain(Projectile.InputSettings input)
                {
                    for (int i = 0; i < 119; i++)
                    {
                        RinHelper.Vec2List.PolygonN(9, 1f, i * flip, out List<Vector2> ring);
                        foreach (var item in ring)
                        {
                            input.SetOrigin(item.ScaleToMagnitude(i.AsFloat(360f / 65f).SineAmp(3.5f).Absolute() - 1f) + sender.CurrentPosition);
                            input.SetDirection(item);
                            float spin = random + 150f * flip - (i.AsFloat(6.5f * flip) - (8f * flip));
                            Single(0 + spin, 12.5f).Spawn(input, i > 95 ? rageOverlap : define, out Projectile p);
                        }
                        if (i == 99)
                        {
                            TimeSlowHandler.AddSlow("inbachi slow", 0.75f, 1.55f, 0.65f);
                        }
                        yield return 0.055f.WaitForSeconds();
                    }
                }
                AttackRoutine(CO_Sidechain(input), sender);
                yield return (99 * 0.055f).WaitForSeconds();
            }
        }
        [System.Serializable]
        public class TestSpiralOverlap : UnitAttack
        {
            public ProjectileDefineSO define;
            public ACWrapper shotSound;
            bool flip = false;
            protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
            {
                input.addedForward = 0.4f;
                for (int i = 0; i < 11; i++)
                {
                    if (i == 8)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            var input2 = input.Copy();
                            input2.addedForward = 0.15f;
                            input2.ReAimWithOptionalTarget(sender.CurrentPosition);
                            if (Arc(RNG.FloatRange(-1.5f, 1.5f), 135f, 7, j.AsFloat(1.25f) + 6f).Spawn(input2, define, out iterationList))
                            {
                                shotSound.Play(input.Origin);
                            }
                        }
                    }
                    flip = !flip;
                    for (int j = 0; j < 6; j++)
                    {
                        input.ReAimWithOptionalTarget(sender.CurrentPosition);
                        if (Circle(flip ? -40f + j.AsFloat(-8f) : 40f + j.AsFloat(8f), 7, 5f + j.AsFloat(1.35f)).Spawn(input, define, out iterationList))
                        {
                            shotSound.Play(input.Origin);
                        }
                    }
                    yield return 0.15f.WaitForSeconds();
                }
                yield return 0.35f.WaitForSeconds();
            }
        }
    }
}