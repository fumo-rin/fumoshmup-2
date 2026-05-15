using UnityEngine;
using rinCore;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace FumoShmup2
{
    public enum ProjectileFaction
    {
        None,
        Enemy,
        Player
    }
    #region Projectile Mod Sets
    public partial class Projectile
    {
        public class ModSets
        {
            public static List<ProjectileMod> KetsuiAccelerateDecelerate(float targetSpeed, float startingSpeed)
            {
                List<ProjectileMod> mods = new();
                mods.Add(new ProjectileModAccelerate(new(0.5f, 0f), 0f, startingSpeed / 0.5f));
                mods.Add(new ProjectileModAccelerate(new(0.5f, 0.65f), targetSpeed, targetSpeed / 0.5f));
                return mods;
            }
        }
    }
    #endregion
    #region Projectile Mods
    public partial class Projectile
    {
        public void RunMods()
        {
            if (mods == null || mods.Count == 0)
            {
                return;
            }
            foreach (var item in mods)
            {
                item.RunMod(this);
            }
        }
    }
    public interface ProjectileMod
    {
        public void RunMod(Projectile p);
        public ProjectileMod Clone();
    }
    public struct ProjectileModSettings
    {
        public float duration;
        public float delay;
        public ProjectileModSettings(float duration, float delay)
        {
            this.duration = duration;
            this.delay = delay;
        }
    }
    public class ProjectileModGravity : ProjectileMod
    {
        float remainingDuration;
        float remainingDelay;
        float gravity;
        float maxDownSpeed;
        public ProjectileModGravity(ProjectileModSettings settings, float gravity, float maxDownSpeed)
        {
            this.remainingDuration = settings.duration;
            this.remainingDelay = settings.delay;
            this.gravity = gravity;
            this.maxDownSpeed = maxDownSpeed;
        }
        public ProjectileMod Clone()
        {
            return new ProjectileModGravity(new(remainingDuration, remainingDelay), gravity, maxDownSpeed);
        }
        public void RunMod(Projectile p)
        {
            if (remainingDelay > 0f)
            {
                remainingDelay -= Time.deltaTime;
                return;
            }
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f)
            {
                return;
            }
            Vector2 v = p.VelocityNotZero;
            v.y -= gravity * Time.deltaTime;
            if (v.y < -Mathf.Abs(maxDownSpeed))
            {
                v.y = -Mathf.Abs(maxDownSpeed);
            }
            Projectile.ModifyVelocity(p, v);
        }
    }
    public class ProjectileModRotate : ProjectileMod
    {
        float remainingDuration;
        float remainingDelay;
        float angle;
        public ProjectileModRotate(ProjectileModSettings settings, float angle)
        {
            remainingDelay = settings.delay;
            remainingDuration = settings.duration;
            this.angle = angle;
        }
        public void RunMod(Projectile p)
        {
            if (remainingDelay > 0)
            {
                remainingDelay -= Time.deltaTime;
                return;
            }
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0)
            {
                return;
            }
            float rotationAngle = angle * Time.deltaTime;
            Projectile.ModifyVelocity(p, p.VelocityNotZero.Rotate2D(rotationAngle));
        }
        public ProjectileMod Clone()
        {
            return new ProjectileModRotate(new(remainingDuration, remainingDelay), angle);
        }
    }
    public class ProjectileModChase : ProjectileMod
    {
        float remainingDuration;
        float remainingDelay;
        ShmupUnit chaseTarget;
        float maxRotationPerSecond;
        bool shouldChase => chaseTarget != null && remainingDuration > 0f;
        public ProjectileModChase(ProjectileModSettings settings, ShmupUnit target, float maxAnglePerSecond)
        {
            this.remainingDuration = settings.duration;
            this.remainingDelay = settings.delay;
            this.chaseTarget = target;
            this.maxRotationPerSecond = maxAnglePerSecond;
        }
        public ProjectileMod Clone()
        {
            return new ProjectileModChase(new(remainingDuration, remainingDelay), chaseTarget, maxRotationPerSecond);
        }
        public void RunMod(Projectile p)
        {
            if (remainingDelay > 0)
            {
                remainingDelay -= Time.deltaTime;
                return;
            }
            remainingDuration -= Time.deltaTime;

            if (remainingDuration <= 0)
            {
                return;
            }
            if (!shouldChase) return;
            if (!chaseTarget.IsAlive)
            {
                return;
            }
            Vector2 currentDir = p.VelocityNotZero.normalized;
            float speed = p.VelocityNotZero.magnitude;
            Vector2 targetDir = (chaseTarget.CurrentPosition - p.Position).normalized;
            float angleDiff = Vector2.SignedAngle(currentDir, targetDir);
            float maxStep = maxRotationPerSecond * Time.deltaTime;
            float rotation = Mathf.Clamp(angleDiff, -maxStep, maxStep);
            Vector2 newDirection = currentDir.Rotate2D(rotation);
            Projectile.ModifyVelocity(p, newDirection * speed);
        }
    }
    public class ProjectileModAccelerate : ProjectileMod
    {
        float remainingDuration;
        float remainingDelay;
        float targetSpeed;
        float acceleration;
        public ProjectileModAccelerate(ProjectileModSettings settings, float speed, float acceleration)
        {
            remainingDelay = settings.delay;
            remainingDuration = settings.duration;
            targetSpeed = speed;
            this.acceleration = acceleration;
        }
        public void RunMod(Projectile p)
        {
            if (remainingDelay > 0)
            {
                remainingDelay -= Time.deltaTime;
                return;
            }
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0)
            {
                return;
            }
            Projectile.ModifyVelocity(p, p.VelocityNotZero
                .MoveTowards(p.VelocityNotZero.ScaleToMagnitude(targetSpeed), acceleration));
        }
        public ProjectileMod Clone()
        {
            return new ProjectileModAccelerate(new(remainingDuration, remainingDelay), targetSpeed, acceleration);
        }
    }
    #endregion
    #region Spawning
    public partial class Projectile
    {
        static List<Projectile> iterationList;
        public struct InputSettings
        {
            public bool Flare;
            public List<ProjectileMod> mods;
            Projectile.ProjectileDamage damageInfo;
            public ShmupUnit Sender { get; private set; }
            public Vector2 Origin { get; private set; }
            public Vector2 Direction { get; private set; }
            public ShmupUnit OptionalTarget { get; private set; }
            public InputSettings SetOptionalTarget(ShmupUnit t)
            {
                OptionalTarget = t;
                return this;
            }
            public ProjectileFaction Faction { get; private set; }
            public float addedForward;
            public List<object> Extras { get; private set; }
            public bool TryGetExtra(int index, out object result)
            {
                return Extras.TryGetIndex(index, out result);
            }
            public void AddExtra(object extra)
            {
                Extras.Add(extra);
            }
            public void PlaySound(ACWrapper a)
            {
                if (a != null)
                {
                    a.Play(Origin);
                }
            }
            public InputSettings(Vector2 origin, ShmupUnit sender, Vector2 direction, Projectile.ProjectileDamage damageInfo, ProjectileFaction faction)
            {
                this.Flare = true;
                this.mods = null;
                this.damageInfo = damageInfo;
                this.Sender = sender;
                this.Origin = origin;
                this.Direction = direction;
                this.OptionalTarget = null;
                this.Faction = faction;
                this.addedForward = 0f;
                this.Extras = new();
            }
            public InputSettings SetMods(List<ProjectileMod> mods)
            {
                this.mods = new();
                foreach (var mod in mods)
                {
                    this.mods.Add(mod);
                }
                return this;
            }
            public InputSettings SetMods(params ProjectileMod[] mods)
            {
                SetMods(mods.ToList());
                return this;
            }
            public InputSettings Copy()
            {
                var item = new InputSettings()
                {
                    Flare = this.Flare,
                    Origin = this.Origin,
                    Direction = this.Direction,
                    OptionalTarget = this.OptionalTarget,
                    Faction = this.Faction,
                    Sender = this.Sender,
                    addedForward = this.addedForward
                };
                if (mods != null && mods.Count > 0)
                {
                    item.mods = new();
                    foreach (var mod in mods)
                    {
                        item.mods.Add(mod.Clone());
                    }
                }
                return item;
            }
            public InputSettings SetOrigin(Vector2 position)
            {
                Origin = position;
                return this;
            }
            public InputSettings SetDirectionToTarget(ShmupUnit Target)
            {
                if (Target == null)
                {
                    return this;
                }
                Direction = Target.CurrentPosition - Origin;
                return this;
            }
            public InputSettings ReAimWithOptionalTarget(Vector2? Origin = null)
            {
                if (Origin != null)
                {
                    SetOrigin(Origin.Value);
                }
                if (OptionalTarget != null)
                {
                    SetDirectionToTarget((ShmupUnit)OptionalTarget);
                }
                return this;
            }
            public InputSettings SetDirection(Vector2 direction)
            {
                Direction = direction;
                return this;
            }
            public InputSettings AimTo(ShmupUnit unit)
            {
                if (unit == null)
                    return this;
                SetDirection(unit.CurrentPosition - Origin);
                return this;
            }
            public InputSettings AssignTarget(ShmupUnit target)
            {
                OptionalTarget = target;
                return this;
            }
            public InputSettings Rotate(float r)
            {
                Direction = Direction.Rotate2D(r);
                return this;
            }
        }
        public struct SingleSettings
        {
            public SingleSettings(float addedAngle, float projectileSpeed)
            {
                this.AddedAngle = addedAngle;
                this.ProjectileSpeed = projectileSpeed;
            }
            public float AddedAngle;
            public float ProjectileSpeed;
            public bool Spawn(Projectile.InputSettings input, ProjectileDefineSO define, out Projectile output)
            {
                return SpawnSingle(define, input, this, out output);
            }
        }
        public struct ArcSettings
        {
            public static ArcSettings operator *(ArcSettings settings, float multiplier)
            {
                return new ArcSettings()
                {
                    StartingAngle = settings.StartingAngle,
                    EndingAngle = settings.EndingAngle,
                    ArcInterval = settings.ArcInterval / multiplier,
                    ProjectileSpeed = settings.ProjectileSpeed,
                    IsReverse = settings.IsReverse
                };
            }
            public ArcSettings Widen(float multiplier)
            {
                return new ArcSettings()
                {
                    StartingAngle = this.StartingAngle * multiplier,
                    EndingAngle = this.EndingAngle * multiplier,
                    ArcInterval = this.ArcInterval * multiplier,
                    ProjectileSpeed = this.ProjectileSpeed,
                    IsReverse = this.IsReverse
                };
            }
            public ArcSettings Speed(float multiplier)
            {
                return new ArcSettings()
                {
                    StartingAngle = this.StartingAngle,
                    EndingAngle = this.EndingAngle,
                    ArcInterval = this.ArcInterval,
                    ProjectileSpeed = this.ProjectileSpeed * multiplier,
                    IsReverse = this.IsReverse
                };
            }
            public ArcSettings Reverse()
            {
                return new ArcSettings()
                {
                    StartingAngle = this.StartingAngle,
                    EndingAngle = this.EndingAngle,
                    ArcInterval = this.ArcInterval,
                    ProjectileSpeed = this.ProjectileSpeed,
                    IsReverse = !this.IsReverse
                };
            }
            public ArcSettings(float startingAngle, float arcEndAngle, float arcInterval, float projectileSpeed)
            {
                this.StartingAngle = startingAngle;
                this.EndingAngle = arcEndAngle;
                this.ArcInterval = arcInterval;
                this.ProjectileSpeed = projectileSpeed;
                this.IsReverse = false;
            }
            public IEnumerable<Projectile> SpawnForeach(Projectile.InputSettings input, ProjectileDefineSO define)
            {
                if (SpawnArc(define, input, this, out iterationList))
                {
                    foreach (Projectile projectile in iterationList)
                    {
                        yield return projectile;
                    }
                }
            }
            public bool Spawn(Projectile.InputSettings input, ProjectileDefineSO define, out List<Projectile> output)
            {
                return SpawnArc(define, input, this, out output);
            }
            public float StartingAngle { get; private set; }
            public float EndingAngle { get; private set; }
            public float ArcInterval { get; private set; }
            public float ProjectileSpeed { get; private set; }
            public bool IsReverse { get; private set; }
        }
        public static bool SpawnSingle(ProjectileDefineSO define, InputSettings input, SingleSettings settings, out Projectile output)
        {
            bool spawnedBullet = CreateProjectile(define, input.Sender, input.Origin + input.Direction.ScaleToMagnitude(input.addedForward), input.Direction.normalized.Rotate2D(settings.AddedAngle).Multiply(settings.ProjectileSpeed), input.Faction, input.mods, out Projectile p);
            output = p;
            /*if (!spawnedBullet && define != null && define.useFlare)
            {
                Vector2 flareDirection = input.Direction.normalized * settings.ProjectileSpeed;
                ProjectileRunner.Flare(input.Origin + input.Direction.ScaleToMagnitude(input.addedForward), flareDirection, define.FlareColor);
            }*/
            if (spawnedBullet && input.Flare && p != null && define.useFlare)
            {
                ProjectileRunner.Flare(p.Position + input.Direction.ScaleToMagnitude(input.addedForward), p.EffectiveVelocity, define.FlareColor);
            }
            return spawnedBullet;
        }
        public static bool SpawnArc(ProjectileDefineSO define, InputSettings input, ArcSettings settings, out List<Projectile> output)
        {
            output = new();
            foreach (var item in settings.ArcInterval.StepFromTo(settings.StartingAngle, settings.EndingAngle))
            {
                float angle = item * settings.IsReverse.ToFloat(-1f, 1f);
                bool spawnedBullet = CreateProjectile(define, input.Sender, input.Origin + input.Direction.Rotate2D(angle).ScaleToMagnitude(input.addedForward), input.Direction.Rotate2D(angle).normalized.Multiply(settings.ProjectileSpeed), input.Faction, input.mods, out Projectile p);
                if (!spawnedBullet)
                {
                    /*if (define != null && define.useFlare)
                    {
                        Vector2 rotatedDir = input.Direction.Rotate2D(item * settings.IsReverse.ToFloat(-1f, 1f)).normalized;
                        Vector2 flareDirection = rotatedDir * settings.ProjectileSpeed;
                        Vector2 flareOrigin = input.Origin + rotatedDir.ScaleToMagnitude(input.addedForward);
                        ProjectileRunner.Flare(flareOrigin, flareDirection, define.FlareColor);
                    }*/
                    continue;
                }
                if (input.Flare && p != null && define.useFlare)
                {
                    Vector2 flareDirection = input.Direction.Rotate2D(item * settings.IsReverse.ToFloat(-1f, 1f)).normalized * settings.ProjectileSpeed;
                    ProjectileRunner.Flare(p.Position, flareDirection, define.FlareColor);
                }
                output.Add(p);
            }
            return output != null && output.Count > 0;
        }
        public struct CircleSettings
        {
            public CircleSettings(float startingAngle, int segments, float projectileSpeed)
            {
                StartingAngle = startingAngle;
                ArcInterval = 360f / (segments).Max(2);
                ProjectileSpeed = projectileSpeed;
            }

            public float StartingAngle { get; private set; }
            public float ArcInterval { get; private set; }
            public float ProjectileSpeed { get; private set; }
            public bool Spawn(Projectile.InputSettings input, ProjectileDefineSO define, out List<Projectile> output)
            {
                bool spawned = SpawnCircle(define, input, this, out output);
                return spawned;
            }
            public IEnumerable<Projectile> SpawnForeach(Projectile.InputSettings input, ProjectileDefineSO define)
            {
                SpawnCircle(define, input, this, out iterationList);
                foreach (var item in iterationList)
                {
                    yield return item;
                }
            }
        }
        public static bool SpawnCircle(ProjectileDefineSO define, InputSettings input, CircleSettings settings, out List<Projectile> output)
        {
            ArcSettings s = new ArcSettings(-360f + settings.StartingAngle, settings.StartingAngle, settings.ArcInterval, settings.ProjectileSpeed);
            return SpawnArc(define, input, s, out output);
        }
    }
    #endregion
    #region Cleanup
    public interface IProjectileWipeListener
    {
        void OnProjectileDestroyed(Projectile p);
    }
    public partial class Projectile
    {
        private static readonly List<IProjectileWipeListener> listeners = new();
        [Initialize(-20000)]
        private static void ReinitializeWipeSystem()
        {
            listeners.Clear();
        }
        public static void RegisterWipe(IProjectileWipeListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }
        public static void UnregisterWipe(IProjectileWipeListener listener)
        {
            listeners.Remove(listener);
        }
        private static void NotifyDestroyed(Projectile p)
        {
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnProjectileDestroyed(p);
        }
        public static void Wipe(Projectile p)
        {
            if (p == null)
            {
                return;
            }
            p.data = null;
            p.damageInfo = default;
            p.IsActive = false;
            Projectile.NotifyDestroyed(p);
            ProjectileRunner.Ungraze(p);
        }
    }
    #endregion
    #region Projectile Factory
    public class ProjectileFactory
    {
        public static Projectile.ArcSettings Arc(float centerAimAngle, float arcSize, int shotCount, float projectileSpeed)
        {
            var result = new Projectile.ArcSettings(centerAimAngle - arcSize.Half(), centerAimAngle + arcSize.Half(),
                arcSize / IntExtensions.Clamp((IntExtensions.Clamp(shotCount, 2, 999) - 1), 1, 9999).AsFloat(), projectileSpeed);
            return result;
        }
        public static Projectile.SingleSettings Single(float addedAngle, float projectileSpeed)
            => new Projectile.SingleSettings(addedAngle, projectileSpeed);
        public static Projectile.CircleSettings Circle(float addedAngle, int segments, float projectileSpeed)
            => new Projectile.CircleSettings(addedAngle, segments, projectileSpeed);
    }
    #endregion
    #region When Offscreen
    public partial class Projectile
    {
        public delegate void OffScreenAction(Projectile p, Vector2 edgeNormal);
        public event OffScreenAction WhenOffscreen;
        public void TriggerOffscreenEvent()
        {
            if (WhenOffscreen == null)
            {
                return;
            }
            Vector2 center = ShmupWorldspace.WorldSpace.center;
            Vector2 normal = (center - Position).QuantizeToStepSize(90f, ShmupWorldspace.WorldSpace).normalized;
            RinHelper.DrawLine2D(Position, Position + normal, Color.white);
            WhenOffscreen?.Invoke(this, normal);
        }
    }
    #endregion
    #region Collision Checks
    public partial class Projectile
    {
        public class HitPacket
        {
            public Collider2D hitCollider;
            public FumoUnit hitUnit;
        }
        public bool CollidesWith(FumoUnit other, out HitPacket collisionPacket)
        {
            collisionPacket = null;
            foreach (var item in other.Hitboxes)
            {
                Vector2 closest = item.ClosestPoint(Position);
                if (data.ColliderShape.OverlapsPoint(closest, data.HalfLength, Position, (Time.time - spawnTime) * data.spin + EffectiveAngle))
                {
                    collisionPacket = new()
                    {
                        hitCollider = item,
                        hitUnit = other
                    };
                    break;
                }
            }
            return collisionPacket != null && collisionPacket.hitCollider != null;
        }
    }
    #endregion
    public partial class Projectile
    {
        public struct ProjectileDamage
        {
            public ShmupUnit Owner;
            public float BaseDamage;
            private float BaseMultiplier;
            public float DamageMultiplier => /*owner multiplier times */ BaseMultiplier;
            public ProjectileDamage(ShmupUnit Owner)
            {
                this.Owner = Owner;
                this.BaseDamage = 1f;
                this.BaseMultiplier = 1f;
            }
            public ProjectileDamage(ShmupUnit Owner, float baseDamage, float baseMultiplier)
            {
                this.Owner = Owner;
                this.BaseDamage = baseDamage;
                this.BaseMultiplier = baseMultiplier;
            }
        }
        public void AddForward(float forward)
        {
            if (forward != 0f)
            {
                Vector2 diff = this.VelocityNotZero.ScaleToMagnitude(forward);
                SetNewPosition(diff + Position, true);
            }
        }
        public static void ModifyVelocity(Projectile p, Vector2 newVelocity)
        {
            p.Velocity = newVelocity;
        }
        public ProjectileDamage damageInfo;
        public Vector2 Position { get; private set; }
        public void SetNewPosition(Vector2 position, bool overrideLastPosition = false)
        {
            PreviousPosition = Position;
            if (overrideLastPosition)
            {
                PreviousPosition = position;
            }
            Position = position;
        }
        public ShmupUnit Sender { get; private set; }
        public Vector2 PreviousPosition { get; private set; }
        private Vector2 Velocity;
        public Vector2 EffectiveVelocity => VelocityNotZero + SecondaryVelocity;
        public float EffectiveAngle
        {
            get
            {
                Vector2 v = EffectiveVelocity;
                if (v.sqrMagnitude < 1e-8f)
                    return 0f;

                float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                if (angle < 0f)
                    angle += 360f;
                return angle;
            }
        }
        public Vector2 SecondaryVelocity;
        private Vector2 storedValidDirection;
        public bool IsActive { get; private set; }
        public bool isOnScreen => Projectile.IsOnScreen(this);
        public ProjectileFaction Faction;
        public float spawnTime;
        public float animationOffsetSeconds = 0f;
        List<ProjectileMod> mods;
        public Vector2 VelocityNotZero
        {
            get
            {
                if (Velocity != Vector2.zero)
                {
                    storedValidDirection = Velocity;
                    return storedValidDirection;
                }
                if (storedValidDirection == Vector2.zero)
                {
                    storedValidDirection = Vector2.down;
                }
                return storedValidDirection;
            }
        }
        public ProjectileDefineSO data;
        public float HalfLength => data == null ? 0f : data.HalfLength;
        public void SetActive(bool state)
        {
            this.IsActive = state;
        }
        public void SetDamage(ProjectileDamage damage)
        {
            this.damageInfo = damage;
        }
        private static bool CreateProjectile(ProjectileDefineSO define, ShmupUnit sender, Vector2 position, Vector2 direction, ProjectileFaction faction, List<ProjectileMod> mods, out Projectile p)
        {
            void Cancel(Vector2 position, Vector2 direction)
            {
                ProjectileRenderer.BulletCancelParticle(position, direction);
                if (faction != ProjectileFaction.Player && ShmupPlayer.Player != null && ShmupPlayer.Player.IsAlive && ShmupPlayer.Player.CurrentPosition.SquareDistanceToLessThan(position, 0.75f))
                {
                    ShmupGamemode.TriggerGraze(1);
                }
            }
            p = default;
            if (define == null)
            {
                return false;
            }
            if (faction != ProjectileFaction.Player && ProjectileRunner.IsSweeping)
            {
                if (ProjectileRunner.SweepLootChance > 0 && RNG.Byte255 < ProjectileRunner.SweepLootChance)
                {
                    PointItemRunner.SpawnPointItem(position + Random.insideUnitCircle);
                    Cancel(position, direction);
                }
                return false;
            }
            if (faction != ProjectileFaction.Player && ShmupPlayer.PlayerAs(out ShmupPlayer foundPlayer))
            {
                bool cancelCondition = ShmupPlayer.BlockProjectileSpawning || !foundPlayer.IsAlive;
                if (sender is EnemyUnit e)
                {
                    cancelCondition = !e.IsOnScreenAndAlive;
                }
                if (cancelCondition)
                {
                    Cancel(position, direction);
                    return false;
                }
            }
            p = new Projectile
            {
                data = define,
                Position = position,
                PreviousPosition = position,
                Velocity = direction,
                Faction = faction,
                spawnTime = Time.time,
                animationOffsetSeconds = (1f / define.animationSpeed) * (define.animationSpreadPercent.RandomPositiveNegativeRange().Multiply(0.01f)),
                mods = mods?.Select(m => m.Clone()).ToList(),
                IsActive = true
            };
            p.SetDamage(new(null, 1f, 1f));
            p.Sender = sender;
            ProjectileRunner.Bind(p);
            return true;
        }
        public Projectile SetFaction(ref Projectile p, ProjectileFaction faction)
        {
            p.Faction = faction;
            return this;
        }
        public Projectile Action_Bounce(Vector2 normal, float bounce)
        {
            float speed = VelocityNotZero.magnitude;
            ModifyVelocity(this, VelocityNotZero.Bounce(normal, bounce).ScaleToMagnitude(speed));
            return this;
        }
        public Projectile Action_AddRotation(float angle)
        {
            this.Velocity = VelocityNotZero.Rotate2D(angle);
            return this;
        }
        public Projectile Action_ModifySpeed(float multipler)
        {
            this.Velocity = VelocityNotZero.ScaleToMagnitude(VelocityNotZero.magnitude * multipler);
            return this;
        }
        public Projectile Action_ShiftForward(float distance)
        {
            Vector2 dist = VelocityNotZero.ScaleToMagnitude(distance);
            this.SetNewPosition(Position + dist, false);
            return this;
        }
        public static bool IsOnScreen(Projectile p)
        {
            return ShmupWorldspace.BiggerWorldSpace.Contains(p.Position);
        }
    }

}
