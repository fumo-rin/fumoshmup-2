using UnityEngine;
using System.Collections.Generic;
using rinCore;
using TMPro;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FumoShmup2
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
                return 1f;
            }
            if (player.IframesDurationLeft > 0.8f)
            {
                return 1f;
            }

            float slowdownIdeal = 0.666f;
            float slowdownMax = 0.45f;
            float slowdownNone = 1f;

            float overloadStart = requiredProjectiles * 2f;
            float overloadEnd = requiredProjectiles * 4f;
            int halfRequired = (requiredProjectiles * 0.5f).ToInt();

            int bulletCount = BulletCount + PointItemRunner.ItemCount;

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

    #region Sweeping & Sealing
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
        public static void SealBullets(Vector2 position, ShmupUnit owner, float radius, byte lootChance, out List<Projectile> removed)
        {
            if (instance == null)
            {
                removed = null;
                return;
            }
            removed = new();
            foreach (var item in instance.projectiles.Where(x => x.Sender == owner))
            {
                if (item.Position.SquareDistanceToLessThan(position, radius))
                {
                    removed.Add(item);
                }
            }
            foreach (var item in removed)
            {
                ProjectileRenderer.BulletCancelParticle(item.Position, item.VelocityNotZero, 0.4f);
                if (RNG.Byte255 < lootChance)
                {
                    PointItemRunner.SpawnPointItem(item.Position);
                }
                Projectile.Wipe(item);
            }
        }
    }
    #endregion

    #region Bitmap Collision
    public partial class ProjectileRunner
    {
        #region Gizmos
        private void OnDrawGizmos()
        {
            Rect space = ShmupWorldspace.WorldSpace;
            if (space.width <= 0 || space.height <= 0) return;

            int cols = COLLISION_BITMAP_SIZE_XY.Item1;
            int rows = COLLISION_BITMAP_SIZE_XY.Item2;

            float cellW = space.width / cols;
            float cellH = space.height / rows;

            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            for (int i = 0; i <= cols; i++)
            {
                float x = space.xMin + (i * cellW);
                Gizmos.DrawLine(new Vector3(x, space.yMin, 0), new Vector3(x, space.yMax, 0));
            }
            for (int j = 0; j <= rows; j++)
            {
                float y = space.yMin + (j * cellH);
                Gizmos.DrawLine(new Vector3(space.xMin, y, 0), new Vector3(space.xMax, y, 0));
            }

            if (collisionLookup == null) return;

            DrawDebugCells(CollisionBitmask.EnemyProjectiles, Color.red);
            DrawDebugCells(CollisionBitmask.PlayerProjectiles, Color.blue);

            void DrawDebugCells(CollisionBitmask mask, Color color)
            {
                if (collisionLookup.TryGetValue(mask, out int[] table))
                {
                    Gizmos.color = color;
                    for (int i = 0; i < table.Length; i++)
                    {
                        if (table[i] == Time.frameCount)
                        {
                            int y = i / cols;
                            int x = i % cols;

                            Vector3 pos = new Vector3(
                                space.xMin + (x * cellW) + (cellW * 0.5f),
                                space.yMin + (y * cellH) + (cellH * 0.5f),
                                0
                            );
                            Gizmos.DrawCube(pos, new Vector3(cellW * 0.9f, cellH * 0.9f, 0.1f));
                        }
                    }
                }
            }
        }
        #endregion
        public enum CollisionBitmask
        {
            Default,
            EnemyProjectiles,
            PlayerProjectiles
        }

        public static readonly (int, int) COLLISION_BITMAP_SIZE_XY = (30, 40);
        public static int COLLISION_BITMAP_SIZE => COLLISION_BITMAP_SIZE_XY.Item1 * COLLISION_BITMAP_SIZE_XY.Item2;

        static Dictionary<CollisionBitmask, int[]> collisionLookup = new();

        [Initialize(-99999)]
        private static void ResetCollisionLookup()
        {
            collisionLookup = new();
        }
        #region Mapping
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Vec2ToArrayPixel(Vector2 v)
        {
            Rect space = ShmupWorldspace.WorldSpace;
            if (space.width <= 0 || space.height <= 0) return 0;

            int width = COLLISION_BITMAP_SIZE_XY.Item1;
            int height = COLLISION_BITMAP_SIZE_XY.Item2;

            float tx = (v.x - space.xMin) / space.width;
            float ty = (v.y - space.yMin) / space.height;

            int x = Mathf.FloorToInt(tx * width);
            int y = Mathf.FloorToInt(ty * height);

            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);

            return y * width + x;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<int> GetColliderPixels(Collider2D col)
        {
            if (col == null) yield break;

            Bounds b = col.bounds;
            Rect space = ShmupWorldspace.WorldSpace;
            int width = COLLISION_BITMAP_SIZE_XY.Item1;
            int height = COLLISION_BITMAP_SIZE_XY.Item2;

            float txMin = (b.min.x - space.xMin) / space.width;
            float tyMin = (b.min.y - space.yMin) / space.height;
            float txMax = (b.max.x - space.xMin) / space.width;
            float tyMax = (b.max.y - space.yMin) / space.height;

            int xStart = Mathf.Clamp(Mathf.FloorToInt(txMin * width), 0, width - 1);
            int yStart = Mathf.Clamp(Mathf.FloorToInt(tyMin * height), 0, height - 1);
            int xEnd = Mathf.Clamp(Mathf.FloorToInt(txMax * width), 0, width - 1);
            int yEnd = Mathf.Clamp(Mathf.FloorToInt(tyMax * height), 0, height - 1);

            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    yield return y * width + x;
                }
            }
        }
        #endregion
        #region Read Write
        private static bool ReadCollisionPixel(int pixel, CollisionBitmask mask)
        {
            if (collisionLookup == null) collisionLookup = new();

            if (!collisionLookup.TryGetValue(mask, out int[] table))
            {
                table = new int[COLLISION_BITMAP_SIZE];
                collisionLookup[mask] = table;
            }

            if ((uint)pixel >= (uint)table.Length) return false;

            return table[pixel] == Time.frameCount;
        }

        private static void WriteCollisionPixel(int pixel, CollisionBitmask mask)
        {
            if (collisionLookup == null) collisionLookup = new();

            if (!collisionLookup.TryGetValue(mask, out int[] collisionArray))
            {
                collisionArray = new int[COLLISION_BITMAP_SIZE];
                collisionLookup[mask] = collisionArray;
            }

            if ((uint)pixel < (uint)COLLISION_BITMAP_SIZE)
                collisionArray[pixel] = Time.frameCount;
        }
        #endregion
    }
    #endregion

    #region Removal Delegate
    public partial class ProjectileRunner
    {
        public delegate bool RemoveAction(Projectile p);
        public static RemoveAction RemoveActionOverride = null;
    }
    #endregion

    #region Bullet Backdrop Render
    public partial class ProjectileRunner
    {
        [SerializeField] ParticleSystem backdropRenderer;
        static List<Vector2> backdropIteration;
        private void RenderBackdrop(List<Projectile> projectiles)
        {
            Projectile iteration;
            if (backdropIteration == null) backdropIteration = new();
            backdropIteration.Clear();
            for (int i = 0; i < projectiles.Count; i++)
            {
                iteration = projectiles[i];
                if (iteration == null || !iteration.IsActive || !iteration.isOnScreen)
                {
                    continue;
                }
                if (iteration.Faction == ProjectileFaction.Player)
                    continue;
                backdropIteration.Add(iteration.Position);
            }
            backdropRenderer.RenderAnimatedPoints(backdropIteration, Time.time, true);
        }
    }
    #endregion
    [DefaultExecutionOrder(123)]
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
            TimeSlowHandler.SetSimulatedSlowdownTarget(GetTargetSlowdown(250));
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
            instance.FlareRenderer.EmitSingleParticleCached(position, velocity, 0f, color);
        }

        public static int BulletCount => instance == null ? 0 : instance.projectiles.Count;

        private void Awake()
        {
            instance = this;
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
                float slowdownDuration = sweepDuration + removed.AsFloat().Multiply(0.001f).Clamp(0f, 0.5f);
                TimeSlowHandler.CombineSlow("Sweep Slowdown", 0.75f, slowdownDuration);
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
            void RemoveAndWipe(int index)
            {
                Projectile cleared = projectiles.RemoveAndReplaceWithLast(index);
                if (cleared != null)
                {
                    cleared.SetActive(false);
                    grazedProjectiles.Remove(cleared);
                    Projectile.Wipe(cleared);
                }
            }
            void WriteUnits(CollisionBitmask layer, params FumoUnit[] units)
            {
                foreach (FumoUnit unit in units)
                {
                    foreach (var hitbox in unit.Hitboxes)
                    {
                        foreach (var pixel in GetColliderPixels(hitbox))
                        {
                            WriteCollisionPixel(pixel, layer);
                        }
                    }
                }
            }
            bool TryCollide(Projectile p, out Projectile.HitPacket hit)
            {
                void TryCollideWithUnits(out Projectile.HitPacket hit)
                {
                    hit = null;
                    switch (p.Faction)
                    {
                        case ProjectileFaction.None:
                            break;
                        case ProjectileFaction.Enemy:
                            if (ShmupPlayer.PlayerAs(out ShmupPlayer player) && player.IsAlive)
                            {
                                if (p.CollidesWith(player, out hit))
                                    break;
                            }
                            break;
                        case ProjectileFaction.Player:
                            foreach (var item in FumoUnit.AliveEnemies)
                            {
                                if (p.CollidesWith(item, out hit))
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
                hit = null;
                CollisionBitmask mask = CollisionBitmask.EnemyProjectiles;
                switch (p.Faction)
                {
                    case ProjectileFaction.None:
                        break;
                    case ProjectileFaction.Enemy:
                        break;
                    case ProjectileFaction.Player:
                        mask = CollisionBitmask.PlayerProjectiles;
                        break;
                    default:
                        break;
                }
                int vectorPosition = Vec2ToArrayPixel(p.Position);
                if (ReadCollisionPixel(vectorPosition, mask))
                {
                    TryCollideWithUnits(out hit);
                }
                else
                {
                    int scanSize = 1;
                    for (int i = -scanSize; i <= scanSize; i++)
                    {
                        int biggerPos = vectorPosition + i;
                        for (int j = -scanSize; j <= scanSize; j++)
                        {
                            biggerPos += j * COLLISION_BITMAP_SIZE_XY.Item2;
                            if (biggerPos >= 0 && biggerPos < COLLISION_BITMAP_SIZE)
                            {
                                if (ReadCollisionPixel(biggerPos, mask))
                                {
                                    TryCollideWithUnits(out hit);
                                    break;
                                }
                            }
                        }
                    }
                }

                return hit != null;
            }

            float dt = Time.deltaTime;
            if (ShmupPlayer.PlayerAs(out ShmupPlayer hitPlayer) && hitPlayer.IsAlive)
                WriteUnits(CollisionBitmask.EnemyProjectiles, new ShmupPlayer[1] { hitPlayer });
            WriteUnits(CollisionBitmask.PlayerProjectiles, FumoUnit.AliveEnemies.ToArray());

            for (int i = 0; i < projectiles.Count; i++)
            {
                Projectile proj = projectiles[i];

                if (proj == null || proj.data == null || !proj.IsActive)
                {
                    RemoveAndWipe(i);
                    i--;
                    continue;
                }

                proj.RunMods();
                Vector2 nextPos = proj.Position + (proj.EffectiveVelocity * dt);
                proj.SetNewPosition(nextPos);

                bool shouldRemove = false;

                if (RemoveActionOverride != null && RemoveActionOverride.Invoke(proj) is bool removeActionResult)
                {
                    switch (removeActionResult)
                    {
                        case true:
                            proj.SetNewPosition(proj.PreviousPosition);
                            proj.TriggerOffscreenEvent();
                            shouldRemove = true;
                            break;
                        default:
                            break;
                    }
                }
                else if (!Projectile.IsOnScreen(proj))
                {
                    proj.SetNewPosition(proj.PreviousPosition);
                    proj.TriggerOffscreenEvent();
                    shouldRemove = true;
                }

                if (proj.Faction != ProjectileFaction.Player && ShmupPlayer.PlayerAs(out ShmupPlayer grazePlayer) && grazePlayer.IsAlive && !grazedProjectiles.Contains(proj))
                {//Player Graze
                    if (proj.Position.InBoxDistance(grazePlayer.CurrentPosition, 1f))
                    {
                        grazedProjectiles.Add(proj);
                        grazeSound.Play(proj.Position);
                        ShmupGamemode.TriggerGraze(1);
                    }
                }

                if (!shouldRemove && TryCollide(proj, out Projectile.HitPacket resultPacket))
                {
                    if (resultPacket.hitUnit.TryGetComponent(out IHit hit))
                    {
                        Vector2 closest = resultPacket.hitCollider.ClosestPoint(proj.Position);
                        Vector2 norm = (proj.Position - closest).normalized;
                        hit.SendHit(new(closest, proj.damageInfo), out float damageDealt);
                        if (damageDealt > 0f)
                        {
                            ProjectileRenderer.HitParticle(closest, norm, new()
                            {
                                forceMultiplier = 1f
                            });
                        }
                    }
                    shouldRemove = true;
                }

                if (clearLayer != 0 && !shouldRemove)
                {//terrain fallback hit
                    RaycastHit2D terrainHit = Physics2D.Raycast(proj.Position, proj.EffectiveVelocity, proj.HalfLength, clearLayer);
                    if (terrainHit.transform != null)
                    {
                        if (terrainHit.transform.GetComponent<ShmupUnit>() is ShmupUnit hitUnit && hitUnit is not ShmupPlayer)
                        {
                            if (hitUnit is IHit hitable)
                                hitable.SendHit(new IHit.HitPacket(proj.Position, proj.damageInfo), out _);
                        }
                        shouldRemove = true;
                    }
                }

                if (shouldRemove)
                {
                    RemoveAndWipe(i);
                    i--;
                }
            }

            projectileRenderer.RenderProjectiles(projectiles);
            RenderBackdrop(projectiles);
        }
    }
}