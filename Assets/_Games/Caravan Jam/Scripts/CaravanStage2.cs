using FumoShmup2;
using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Caravan
{
    [System.Serializable]
    public partial class FumoSharpParsedAttack : UnitAttack
    {
        #region Parser
        public class FumoSharpParser
        {
            #region Wait
            public class WaitCommand : IFumoSharpCommand
            {
                int ticks;

                public WaitCommand(int ticks)
                {
                    this.ticks = ticks;
                }

                public IEnumerator Execute(FumoSharpRuntime runtime)
                {
                    yield return TICK.WaitForSeconds(ticks);
                }
            }
            #endregion
            #region Forward
            public class ForwardCommand : IFumoSharpCommand
            {
                float amount;

                public ForwardCommand(float amount)
                {
                    this.amount = amount;
                }

                public IEnumerator Execute(FumoSharpRuntime runtime)
                {
                    runtime.input.addedForward = amount;
                    yield break;
                }
            }
            #endregion
            #region ReAim
            public class ReAimCommand : IFumoSharpCommand
            {
                public IEnumerator Execute(FumoSharpRuntime runtime)
                {
                    runtime.input.ReAimWithOptionalTarget(
                        runtime.sender.CurrentPosition);

                    yield break;
                }
            }
            #endregion
            #region Single
            public class SingleCommand : IFumoSharpCommand
            {
                float angle;
                float speed;
                string projectileName;

                public SingleCommand(
                    float angle,
                    float speed,
                    string projectileName)
                {
                    this.angle = angle;
                    this.speed = speed;
                    this.projectileName = projectileName;
                }

                public IEnumerator Execute(FumoSharpRuntime runtime)
                {
                    ProjectileDefineSO projectile = runtime.projectiles[projectileName];

                    new Projectile.SingleSettings(angle, speed).Spawn(runtime.input, projectile, out _);

                    yield break;
                }
            }
            #endregion
            #region Arc
            public class ArcCommand : IFumoSharpCommand
            {
                float offset;
                float arcSize;
                int count;
                float speed;
                string projectileName;

                public ArcCommand(
                    float offset,
                    float arcSize,
                    int count,
                    float speed,
                    string projectileName)
                {
                    this.offset = offset;
                    this.arcSize = arcSize;
                    this.count = count;
                    this.speed = speed;
                    this.projectileName = projectileName;
                }

                public IEnumerator Execute(FumoSharpRuntime runtime)
                {
                    ProjectileDefineSO projectile =
                        runtime.projectiles[projectileName];

                    ProjectileFactory.Arc(offset, arcSize, count, speed).Spawn(runtime.input, projectile, out _);

                    yield break;
                }
            }
            #endregion
            #region Circle
            public class CircleCommand : IFumoSharpCommand
            {
                float offset;
                int count;
                float speed;
                string projectileName;

                public CircleCommand(
                    float offset,
                    int count,
                    float speed,
                    string projectileName)
                {
                    this.offset = offset;
                    this.count = count;
                    this.speed = speed;
                    this.projectileName = projectileName;
                }

                public IEnumerator Execute(FumoSharpRuntime runtime)
                {
                    ProjectileDefineSO projectile = runtime.projectiles[projectileName];

                    ProjectileFactory.Circle(offset, count, speed).Spawn(runtime.input, projectile, out _);

                    yield break;
                }
            }
            #endregion
            public static List<IFumoSharpCommand> Parse(string text)
            {
                List<IFumoSharpCommand> commands = new();

                string[] lines = text.Split('\n');

                foreach (string raw in lines)
                {
                    string line = raw.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(' ');

                    switch (parts[0].ToLower())
                    {
                        case "wait":
                            commands.Add(new WaitCommand(
                                int.Parse(parts[1])));
                            break;

                        case "forward":
                            commands.Add(new ForwardCommand(
                                float.Parse(parts[1])));
                            break;

                        case "single":
                            commands.Add(new SingleCommand(
                                float.Parse(parts[1]),
                                float.Parse(parts[2]),
                                parts[3]));
                            break;

                        case "arc":
                            commands.Add(new ArcCommand(
                                float.Parse(parts[1]),
                                float.Parse(parts[2]),
                                int.Parse(parts[3]),
                                float.Parse(parts[4]),
                                parts[5]));
                            break;

                        case "circle":
                            commands.Add(new CircleCommand(
                                float.Parse(parts[1]),
                                int.Parse(parts[2]),
                                float.Parse(parts[3]),
                                parts[4]));
                            break;

                        case "reaim":
                            commands.Add(new ReAimCommand());
                            break;
                    }
                }

                return commands;
            }
        }
        #endregion
        public class FumoSharpRuntime
        {
            public ShmupUnit sender;
            public Projectile.InputSettings input;

            public Dictionary<string, ProjectileDefineSO> projectiles = new();

            public FumoSharpRuntime(
                ShmupUnit sender,
                Projectile.InputSettings input)
            {
                this.sender = sender;
                this.input = input;
            }
        }
        public interface IFumoSharpCommand
        {
            IEnumerator Execute(FumoSharpRuntime runtime);
        }
        public ProjectileDefineSO projectile1, projectile2, projectile3;
        [TextArea(10, 40)] public string textAttack;
        protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
        {
            List<IFumoSharpCommand> commands = FumoSharpParser.Parse(textAttack);
            FumoSharpRuntime runtime = new(sender, input.Copy());
            runtime.projectiles["p1"] = projectile1;
            runtime.projectiles["p2"] = projectile2;
            runtime.projectiles["p3"] = projectile3;
            foreach (var cmd in commands)
            {
                yield return cmd.Execute(runtime);
            }
        }
    }
    public partial class CaravanAtatcks
    {
        public partial class Testing
        {
            [System.Serializable]
            public class MovementTest : UnitAttack
            {
                public ProjectileDefineSO shot;
                protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                {
                    sender.StartMovement(ShmupUnit.Testing.CO_TestDash(sender), out WaitUntil w);
                    yield return w;
                    input.ReAimWithOptionalTarget(sender.CurrentPosition);
                    Arc(0f, 40f, 3, 7f).Spawn(input, shot, out _);
                }
            }
        }
        public class Stage2
        {
            public class Fodder
            {
                [System.Serializable]
                public class DoubleShot : UnitAttack
                {
                    public ProjectileDefineSO projectile;
                    public float ammoSeconds = 2.4f;
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        float elapsed = 0f;
                        input.addedForward = 0.35f;
                        while (elapsed < ammoSeconds)
                        {
                            input.ReAimWithOptionalTarget(sender.CurrentPosition);
                            for (int i = 0; i < 2; i++)
                            {
                                Single(0f, 4f).Spawn(input, projectile, out _);
                                Arc(0f, 60f, 3, 10f).Spawn(input, projectile, out _);
                            }
                            elapsed += TICK * 12;
                            yield return TICK.WaitForSeconds(12);
                        }
                    }
                }
            }
            public class Elite
            {
                [System.Serializable]
                public class LobsterFork : UnitAttack
                {
                    public ProjectileDefineSO projectile, entryProjectile;
                    bool firstShot;
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        if (!firstShot)
                        {
                            firstShot = true;
                            Circle(0f, 24, 9.5f).Spawn(input, entryProjectile, out _);
                            Circle(360f / 48f, 24, 7.5f).Spawn(input, entryProjectile, out _);
                            yield return TICK.WaitForSeconds(8);
                        }
                        input.addedForward = 0.35f;
                        void Shot(Projectile.InputSettings input)
                        {
                            input.SetMods(new ProjectileModAccelerate(new(1f, 0f), 7f, 3f));
                            for (int i = 0; i < 12; i++)
                            {
                                Arc(0f, 60f, 5, 7f + i.AsFloat(0.2f)).Spawn(input, projectile, out _);
                            }
                        }
                        input.SetOrigin(sender.CurrentPosition);
                        input.SetDirection(Vector2.down);
                        Shot(input);
                        input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(-1.35f, -0.45f));
                        Shot(input);
                        input.ReAimWithOptionalTarget(sender.CurrentPosition + new Vector2(1.35f, -0.45f));
                        Shot(input);
                        yield return TICK.WaitForSeconds(36);
                    }
                }
            }
            public class General
            {
                [System.Serializable]
                public class ExplosiveRailGun : UnitAttack
                {
                    public ProjectileDefineSO edgeProjectile, railgun;
                    public float postShotDelay = 1f;
                    public int repeats = 2;
                    private void EdgeExplosion(Projectile p, Vector2 normal)
                    {
                        if (p != null && p.IsActive)
                        {
                            var input = new Projectile.InputSettings(p.Position, p.Sender, normal, new Projectile.ProjectileDamage(p.Sender, 1f, 1f), p.Faction);
                            for (int i = 0; i < 8; i++)
                            {
                                Circle(i == 0 ? 0f : (360f / 48) * i.AsFloat(1f), 24, 3f + i.AsFloat(0.25f)).Spawn(input, edgeProjectile, out _);
                            }
                            Projectile.Wipe(p);
                        }
                    }
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        for (int i = 0; i < repeats; i++)
                        {
                            input.ReAimWithOptionalTarget();
                            if (Single(0f, 12f).Spawn(input, railgun, out Projectile p))
                            {
                                p.WhenOffscreen += EdgeExplosion;
                            }
                            yield return postShotDelay.WaitForSeconds();
                        }
                    }
                }
            }
            public class Midboss
            {
                [System.Serializable]
                public class ProGearBossLines : UnitAttack
                {
                    public ProjectileDefineSO lineProjectile;
                    bool flipped = false;
                    public int loops = 8;
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        for (int i = 0; i < loops; i++)
                        {
                            bool flip = flipped;
                            flipped = !flipped;
                            input.ReAimWithOptionalTarget(sender.CurrentPosition);
                            if (Single(0f, 9f).Spawn(input, lineProjectile, out Projectile p))
                            {
                                int number = 1;
                                yield return TICK.WaitForSeconds(4);
                                Vector2 position = sender.CurrentPosition + (flipped ? new Vector2(-number.AsFloat(0.2f), 0f) : new Vector2(number.AsFloat(0.2f), 0f));
                                while (ShmupWorldspace.WorldSpace.Contains(position))
                                {
                                    if (p != null && p.IsActive)
                                    {
                                        position = sender.CurrentPosition + (flipped ? new Vector2(-number.AsFloat(0.2f), 0f) : new Vector2(number.AsFloat(0.2f), 0f));
                                        input.SetOrigin(position);
                                        input.SetDirection(p.Position - position);
                                        Arc(0f, 120f, 7, 9f).Spawn(input, lineProjectile, out _);
                                        yield return TICK.WaitForSeconds();
                                        number++;
                                    }
                                    else
                                    {
                                        yield return TICK.WaitForSeconds(15);
                                        yield break;
                                    }
                                }
                                yield return TICK.WaitForSeconds(5);
                            }
                        }
                    }
                }
            }
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
                [System.Serializable]
                public class SpikesDoubleFan : UnitAttack
                {
                    [SerializeField] ProjectileDefineSO spikeProj, spikeProj2;
                    protected override IEnumerator CO_AttackPayload(ShmupUnit sender, Projectile.InputSettings input)
                    {
                        int iteration = -1;
                        input.addedForward = 0.45f;
                        input.SetMods(new ProjectileModAccelerate(new(2f, 0f), 4f, 8f));
                        bool alternate = false;
                        foreach (var prime in 72.Primes(30))
                        {
                            iteration++;
                            if (iteration % 24 == 0)
                            {
                                sender.StartMovement(ShmupUnit.Testing.CO_TestDash(sender), out WaitUntil w);
                                yield return w;
                                alternate = !alternate;
                            }
                            if (iteration % 24 > 4)
                            {
                                input.ReAimWithOptionalTarget(sender.CurrentPosition);
                                float speed = 2 + iteration.AsFloat(0.05f);
                                var arc = Arc(iteration.AsFloat(3.5f) * alternate.AsFloat(-1f, 1f), 360 + prime, 3 + iteration.Min(18), iteration % 12 + speed.Min(4f));
                                if (alternate)
                                {
                                    arc = arc.Reverse();
                                }
                                arc.Spawn(input, spikeProj, out _);
                            }
                            yield return TICK.WaitForSeconds(2);
                        }

                        iteration = -1;
                        foreach (var prime in 288.Primes(30))
                        {
                            iteration++;
                            if (iteration % 12 == 0)
                            {
                                sender.StartMovement(ShmupUnit.Testing.CO_TestDash(sender), out WaitUntil w2);
                                yield return w2;
                                alternate = !alternate;
                            }
                            if (iteration % 12 > 2)
                            {
                                input.ReAimWithOptionalTarget(sender.CurrentPosition);
                                float speed = 2 + iteration.AsFloat(0.05f);
                                var arc = Arc(iteration.AsFloat(-5.2f) * alternate.AsFloat(-1f, 1f), 360 + prime, 3 + iteration.Min(26), iteration % 12 + speed.Min(6f));
                                if (alternate)
                                {
                                    arc = arc.Reverse();
                                }
                                arc.Spawn(input, spikeProj2, out _);
                            }
                            yield return TICK.WaitForSeconds(3);
                        }
                        yield return TICK.WaitForSeconds(40);
                    }
                }
            }
        }
    }
}