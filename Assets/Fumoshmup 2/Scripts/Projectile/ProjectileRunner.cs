using UnityEngine;
using System.Collections.Generic;
using rinCore;
using TMPro;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FumoShmup
{

    #region Slowdown
    public partial class ProjectileRunner
    {
        public static float GetTargetSlowdown(int requiredProjectiles = 400)
        {
            if (!ShmupPlayer.PlayerAs<ShmupPlayer>(out ShmupPlayer player))
                return 1f;
            if (!player.IsAlive)
            {
                return 1.25f;
            }

            float slowdownIdeal = 0.666f;
            float slowdownMax = 0.45f;
            float slowdownNone = 1f;

            float overloadStart = requiredProjectiles * 2f;
            float overloadEnd = requiredProjectiles * 4f;
            int halfRequired = (requiredProjectiles * 0.5f).ToInt();

            int bulletCount = BulletCount;

            if (bulletCount <= halfRequired)
            {
                return slowdownNone;
            }
            else if (bulletCount <= requiredProjectiles)
            {
                float t = (bulletCount - halfRequired) / (float)(requiredProjectiles - halfRequired);
                return Mathf.Lerp(slowdownNone, slowdownIdeal, t);
            }
            else if (bulletCount <= overloadStart)
            {
                return slowdownIdeal;
            }
            else
            {
                float t = Mathf.InverseLerp(overloadStart, overloadEnd, bulletCount);
                return Mathf.Lerp(slowdownIdeal, slowdownMax, t);
            }
        }
    }
    #endregion

    #region Sweeping
    public partial class ProjectileRunner
    {
        static float SweepEndTime;
        public static bool IsSweeping => Time.time < SweepEndTime;
        public static byte SweepLootChance;
        private static void SetSweepLoot(byte loot)
        {
            SweepLootChance = loot;
        }
        private static void SetSweepTime(float duration)
        {
            SweepEndTime = SweepEndTime.Max(Time.time + duration);
        }
        [Initialize(-505)]
        private static void ResetSweeping()
        {
            SweepEndTime = -2f;
        }
    }
    #endregion

    #region Bitmap Collision
    public partial class ProjectileRunner
    {
        public enum CollisionBitmask
        {
            DefaultProjectile,
            EnemyProjectile,
            PlayerProjectile
        }

        public static readonly (int, int) COLLISION_BITMAP_SIZE = (180, 240);
        static Dictionary<CollisionBitmask, int[]> collisionLookup;

        [Initialize(-99999)]
        private static void ResetCollisionLookup()
        {
            collisionLookup = null;
        }
        private static void WriteCollisionPixel(int pixel, CollisionBitmask mask)
        {
            if (collisionLookup == null)
                collisionLookup = new();

            int size = COLLISION_BITMAP_SIZE.Item1 * COLLISION_BITMAP_SIZE.Item2;

            if (!collisionLookup.TryGetValue(mask, out int[] collisionArray) || collisionArray.Length != size)
            {
                collisionArray = new int[size];
                collisionLookup[mask] = collisionArray;
            }

            if ((uint)pixel < (uint)size)
                collisionArray[pixel] = Time.frameCount;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Vec2ToArrayPixel(Vector2 v)
        {
            Vector2 min = ShmupWorldspace.WorldSpace.min;
            Vector2 max = ShmupWorldspace.WorldSpace.max;

            int width = COLLISION_BITMAP_SIZE.Item1;
            int height = COLLISION_BITMAP_SIZE.Item2;

            float scaleX = (width - 1) / (max.x - min.x);
            float scaleY = (height - 1) / (max.y - min.y);

            int x = Mathf.FloorToInt((v.x - min.x) * scaleX);
            int y = Mathf.FloorToInt((v.y - min.y) * scaleY);

            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);

            return y * width + x;
        }
        private static bool ReadCollisionPixel(int pixel, CollisionBitmask mask)
        {
            if (collisionLookup == null)
                return false;

            if (!collisionLookup.TryGetValue(mask, out int[] table))
                return false;

            if ((uint)pixel >= (uint)table.Length)
                return false;

            return table[pixel] == Time.frameCount;
        }
    }
    #endregion
    public partial class ProjectileRunner : MonoBehaviour
    {
        public static void Bind(Projectile p)
        {
            if (p.data == null)
                return;
            instance.projectiles.Add(p);
            ProjectileRenderer.AddDefine(p.data);
        }
        private void LateUpdate()
        {
            float timescale;
            timescale = GetTargetSlowdown(250);
            TimeSlowHandler.SetSimulatedSlowdownTarget(timescale);
        }
        private HashSet<Projectile> grazedProjectiles = new();
        static ProjectileRunner instance;
        public List<Projectile> projectiles = new();
        [SerializeField] ACWrapper grazeSound;
        [SerializeField] ParticleSystem FlareRenderer;
        [SerializeField] ProjectileRenderer projectileRenderer;
        public static void Flare(Vector2 position, Vector2 velocity, Color32 color)
        {
            if (instance == null || instance.FlareRenderer == null)
            {
                return;
            }
            instance.FlareRenderer.EmitSingleCached(position, velocity, 0f, color);
        }
        public static int BulletCount => instance == null ? 0 : instance.projectiles.Count;
        private void Awake()
        {
            instance = this;
            Application.targetFrameRate = 120;
        }
        public enum ProjectileHit
        {
            NoHit,
            Graze,
            Hit,
            Iframes
        }
        public static void TriggerSweep(float sweepDuration, byte lootChance, bool slowdown, out List<Vector2> sweeps)
        {
            sweeps = new();
            if (instance == null)
            {
                return;
            }
            ProjectileRunner.SetSweepLoot(lootChance);
            ProjectileRunner.SetSweepTime(sweepDuration);
            instance.grazedProjectiles.Clear();
            sweeps = new List<Vector2>(instance.projectiles.Count);
            for (int i = 0; i < instance.projectiles.Count; i++)
            {
                if (instance.projectiles[i] is Projectile p && p.Faction == ProjectileFaction.Enemy)
                {
                    sweeps.Add(instance.projectiles[i].Position);
                    ProjectileRenderer.BulletCancelParticle(p.Position, p.VelocityNotZero);
                    instance.projectiles.RemoveAndReplaceWithLast(i);
                    p.SetActive(false);
                    i--;
                }
            }
            sweeps.RemoveAll(p => p == null);
            int removed = sweeps.Count;
            if (sweepDuration > 0f && slowdown)
            {
                float slowdownDuration = sweepDuration + removed.AsFloat().Multiply(0.001f).Clamp(0f, 2f);
                TimeSlowHandler.CombineSlow("Sweep Slowdown", 0.6f, slowdownDuration);
            }
            ProjectileRenderer.SpawnPointItems(sweeps, lootChance);
        }
        public static void Ungraze(Projectile p)
        {
            instance.grazedProjectiles.Remove(p);
        }
        [SerializeField] LayerMask clearLayer;
        [QFSW.QC.Command("iddqd")]
        static void Invincible(bool state = true)
        {
            FumoSettingsTags.SetBoolTag(FumoSettingsTags.KeysShmup.Invincible, state);
        }
        void Update()
        {
            float dt = Time.deltaTime;
            int frame = Time.frameCount;

            for (int i = 0; i < projectiles.Count; i++)
            {
                Projectile proj = projectiles[i];
                if (proj == null || proj.data == null) continue;

                int pixel = Vec2ToArrayPixel(proj.Position);
                switch (proj.Faction)
                {
                    case ProjectileFaction.Player:
                        WriteCollisionPixel(pixel, CollisionBitmask.PlayerProjectile);
                        break;
                    case ProjectileFaction.Enemy:
                        WriteCollisionPixel(pixel, CollisionBitmask.EnemyProjectile);
                        break;
                    default:
                        WriteCollisionPixel(pixel, CollisionBitmask.DefaultProjectile);
                        break;
                }
            }

            for (int i = 0; i < projectiles.Count; i++)
            {
                Projectile proj = projectiles[i];

                // Null/Data Safety
                if (proj == null || proj.data == null)
                {
                    projectiles.RemoveAndReplaceWithLast(i);
                    Projectile.Wipe(proj);
                    i--;
                    continue;
                }

                proj.RunMods();
                Vector2 nextPosition = proj.Position + (proj.EffectiveVelocity * dt);
                proj.SetNewPosition(nextPosition);

                bool shouldRemove = false;

                if (!Projectile.IsOnScreen(proj))
                {
                    proj.SetNewPosition(proj.PreviousPosition);
                    proj.TriggerOffscreenEvent();
                    shouldRemove = true;
                }

                if (!shouldRemove)
                {
                    int projPixel = Vec2ToArrayPixel(proj.Position);
                    switch (proj.Faction)
                    {
                        case ProjectileFaction.Player:
                            if (ReadCollisionPixel(projPixel, CollisionBitmask.EnemyProjectile))
                                shouldRemove = true;
                            break;

                        case ProjectileFaction.Enemy:
                            if (ReadCollisionPixel(projPixel, CollisionBitmask.PlayerProjectile))
                                shouldRemove = true;
                            break;
                    }
                }

                if (clearLayer != 0 && !shouldRemove)
                {
                    RaycastHit2D terrainHit = Physics2D.Raycast(proj.Position, proj.EffectiveVelocity, proj.HalfLength, clearLayer);
                    if (terrainHit.transform != null)
                    {
                        if (terrainHit.transform.GetComponent<ShmupUnit>() is ShmupUnit hitUnit && hitUnit is not ShmupPlayer)
                        {
                            if (hitUnit is IHit hitable)
                                hitable.Sendhit(new IHit.HitPacket(proj.Position, proj.damageInfo), out _);
                        }
                        shouldRemove = true;
                    }
                }

                if (shouldRemove)
                {
                    Projectile cleared = projectiles.RemoveAndReplaceWithLast(i);
                    if (cleared != null)
                    {
                        cleared.SetActive(false);
                        grazedProjectiles.Remove(cleared);
                        Projectile.Wipe(cleared);
                    }
                    i--;
                    continue;
                }
            }
            projectileRenderer.RenderProjectiles(projectiles);
        }
    }
}