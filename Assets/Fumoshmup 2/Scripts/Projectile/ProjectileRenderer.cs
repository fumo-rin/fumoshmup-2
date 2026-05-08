using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using rinCore;
using UnityEngine.SceneManagement;

namespace FumoShmup2
{
    #region Hit Particle
    public partial class ProjectileRenderer
    {
        [SerializeField] private ParticleSystem hitParticle;
        public struct hitParticleSettings
        {
            public Color32? colorOverride;
            public float forceMultiplier;
        }
        static readonly hitParticleSettings defaultSetting = new()
        {
            colorOverride = null,
            forceMultiplier = 1f
        };
        public static void HitParticle(Vector2 position, Vector2 normal, hitParticleSettings? setting = null)
        {
            if (instance is ProjectileRenderer s && s.hitParticle != null)
            {
                float force = setting == null ? defaultSetting.forceMultiplier : setting.Value.forceMultiplier;
                Color32? color = setting == null ? defaultSetting.colorOverride : setting.Value.colorOverride;
                s.hitParticle.EmitSingleParticleCached(position, normal.QuantizeToStepSize(15f).Rotate2D(10f.RandomPositiveNegativeRange()).ScaleToMagnitude(3f.Spread(55f) * force), 25f, color);
            }
        }
    }
    #endregion
    #region Bullet Cancel
    public partial class ProjectileRenderer
    {
        [SerializeField] private ParticleSystem bulletCancelParticlePrefab;
        public static void BulletCancelParticle(Vector3 position, Vector3? velocity = null, float velocityMultiplier = 0.4f)
        {
            if (instance == null || instance.bulletCancelParticlePrefab == null)
                return;
            instance.bulletCancelParticlePrefab.EmitSingleParticleCached(position, velocity * velocityMultiplier, 50f);
        }
    }
    #endregion
    #region Sweep Particle
    public partial class ProjectileRenderer
    {
        [SerializeField] ParticleSystem lootParticle;
        List<LootBatch> activeBatches = new();
        private bool coroutineRunning = false;
        public static Color32 CurrentParticleColor => instance.lootParticle != null ? instance.lootParticle.GetInitialColor32() : ColorHelper.PastelYellow;
        public static float CurrentParticleSize => instance.lootParticle != null ? instance.lootParticle.GetInitialStartSize() : 0.35f;
        private class LootParticleData
        {
            public ParticleSystem.Particle particle;
            public Vector3 startPosition;
            public float startTime;
            public float duration;
            public bool hasStarted;

            public LootParticleData(Vector3 startPos, float baseStartTime, float startTimeOffset, float baseDuration, float durationSpreadPercent)
            {
                startPosition = startPos;
                startTime = baseStartTime + startTimeOffset;

                float spreadAmount = baseDuration * durationSpreadPercent / 100f;
                duration = UnityEngine.Random.Range(baseDuration - spreadAmount, baseDuration + spreadAmount);

                particle = new ParticleSystem.Particle
                {
                    position = startPos,
                    startColor = CurrentParticleColor,
                    startSize = CurrentParticleSize,
                    startLifetime = duration,
                    remainingLifetime = duration
                };
            }
        }
        private class LootBatch
        {
            public List<LootParticleData> particles;
            public Transform target;
            public float startTime;
            public float baseDuration;

            public LootBatch(List<Vector2> startPositions, Transform target, float startTime, float duration, float startTimeSpreadPercent = 50f, float durationSpreadPercent = 50f)
            {
                this.target = target;
                this.startTime = startTime;
                this.baseDuration = duration;
                particles = new List<LootParticleData>(startPositions.Count);

                float startTimeSpreadAmount = duration * startTimeSpreadPercent / 100f;

                foreach (var pos in startPositions)
                {
                    float startTimeOffset = UnityEngine.Random.Range(-startTimeSpreadAmount, startTimeSpreadAmount);
                    float clampedOffset = Mathf.Max(0f, startTimeOffset);

                    particles.Add(new LootParticleData(pos + Random.insideUnitCircle, startTime, clampedOffset, duration, durationSpreadPercent));
                }
            }
        }
        public static void SpawnPointItem(Vector2 position, int lootValue)
        {
            if (ShmupPlayer.Player.UnitAs<ShmupPlayer>(out ShmupPlayer player))
            {
                //TODO PointItemRunner.Create(position);
            }
        }
        public static void SpawnYellowItem(Vector2 position, int lootValue)
        {
            if (ShmupPlayer.Player.UnitAs<ShmupPlayer>(out ShmupPlayer player))
            {
                SinglePickupParticle(position, player.transform, 0.5f, 50f, 50f, lootValue);
            }
        }
        static void SinglePickupParticle(Vector2 position, Transform target, float duration = 0.5f, float startTimeSpreadPercent = 50f, float durationSpreadPercent = 50f, int lootValue = 1)
        {
            if (instance == null)
            {
                Debug.LogWarning("ProjectileSystem instance not set!");
                return;
            }
            float now = Time.time;

            LootBatch batch = null;
            var activeBatches = instance.activeBatches;
            for (int i = 0; i < activeBatches.Count; i++)
            {
                if (activeBatches[i].target == target && Mathf.Approximately(activeBatches[i].baseDuration, duration))
                {
                    batch = activeBatches[i];
                    break;
                }
            }

            float startTimeSpreadAmount = duration * startTimeSpreadPercent / 100f;
            float startTimeOffset = UnityEngine.Random.Range(-startTimeSpreadAmount, startTimeSpreadAmount);
            float clampedOffset = Mathf.Max(0f, startTimeOffset);
            var data = new LootParticleData(position, now, clampedOffset, duration, durationSpreadPercent);

            if (batch == null)
            {
                batch = new LootBatch(new List<Vector2>(), target, now, duration, startTimeSpreadPercent, durationSpreadPercent);
                batch.particles.Clear();
                activeBatches.Add(batch);
            }

            batch.particles.Add(data);
            //TODO PointItemValueUI.AddPickupCount(1 * lootValue);
            if (!instance.coroutineRunning)
            {
                instance.StartCoroutine(instance.LootParticleUpdateCoroutine());
            }
        }

        public static void SpawnPointItems(List<Vector2> positions, byte lootChance)
        {
            foreach (var item in positions)
            {
                if (lootChance > RNG.Byte255)
                {
                    //TODO PointItemRunner.Create(item);
                }
            }
        }
        private static void ManyPickupParticles(List<Vector2> positions, Transform target, float duration = 0.5f, Color? color = null, float startTimeSpreadPercent = 50f, float durationSpreadPercent = 50f)
        {
            if (instance == null)
            {
                Debug.LogWarning("ProjectileSystem instance not set!");
                return;
            }

            var col = color ?? Color.white;
            float now = Time.time;

            var newBatch = new LootBatch(positions, target, now, duration, startTimeSpreadPercent, durationSpreadPercent);
            var activeBatches = instance.activeBatches;
            activeBatches.Add(newBatch);
            if (!instance.coroutineRunning)
            {
                instance.StartCoroutine(instance.LootParticleUpdateCoroutine());
            }
        }
        private IEnumerator LootParticleUpdateCoroutine()
        {
            coroutineRunning = true;

            lootParticle.Clear();
            lootParticle.Play();

            while (activeBatches.Count > 0)
            {
                float now = Time.time;
                List<ParticleSystem.Particle> allParticles = new();

                for (int b = activeBatches.Count - 1; b >= 0; b--)
                {
                    var batch = activeBatches[b];
                    Vector3 targetPos = batch.target.position;
                    bool batchFinished = true;

                    for (int i = batch.particles.Count - 1; i >= 0; i--)
                    {
                        var data = batch.particles[i];
                        float elapsed = now - data.startTime;
                        float t = Mathf.Clamp01(elapsed / data.duration);
                        t = LerpCurves.EaseInOutCirc(t);

                        if (t >= 0.95f)
                        {
                            data.particle.remainingLifetime = 0f;
                            batch.particles.RemoveAt(i);
                            continue;
                        }

                        Vector3 position = Vector3.Lerp(data.startPosition, targetPos, t);
                        data.particle.position = position;
                        data.particle.remainingLifetime = data.duration - elapsed;

                        batch.particles[i] = data;
                        allParticles.Add(data.particle);
                        batchFinished = false;
                    }

                    if (batchFinished)
                    {
                        activeBatches.RemoveAt(b);
                    }
                }

                lootParticle.SetParticles(allParticles.ToArray(), allParticles.Count);

                yield return null;
            }

            lootParticle.Clear();
            coroutineRunning = false;
        }
    }
    #endregion
    [DefaultExecutionOrder(-500)]
    public partial class ProjectileRenderer : MonoBehaviour
    {
        #region Rotation Lookup
        public class FastRotationGrid
        {
            private readonly int resolution;
            private readonly float[] angleTable;

            public FastRotationGrid(float angleStepDegrees = 360f / 48f)
            {
                angleStepDegrees = Mathf.Max(0.1f, angleStepDegrees);

                int steps = Mathf.CeilToInt(360f / angleStepDegrees);

                resolution = Mathf.Max(4, Mathf.CeilToInt(Mathf.Sqrt(steps) * 2f));

                angleTable = new float[resolution * resolution];

                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float fx = (x / (float)(resolution - 1)) * 2f - 1f;
                        float fy = (y / (float)(resolution - 1)) * 2f - 1f;
                        Vector2 dir = new Vector2(fx, fy);

                        if (dir.sqrMagnitude < 1e-8f)
                        {
                            angleTable[y * resolution + x] = 0f;
                        }
                        else
                        {
                            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                            if (angle < 0f) angle += 360f;

                            angle = Mathf.Round(angle / angleStepDegrees) * angleStepDegrees;

                            if (angle >= 360f) angle -= 360f;

                            angleTable[y * resolution + x] = angle;
                        }
                    }
                }
            }
            public float GetRotation(Vector2 v)
            {
                if (v.sqrMagnitude < 1e-8f)
                    return 0f;

                v.Normalize();

                int xi = Mathf.Clamp((int)((v.x * 0.5f + 0.5f) * (resolution - 1)), 0, resolution - 1);
                int yi = Mathf.Clamp((int)((v.y * 0.5f + 0.5f) * (resolution - 1)), 0, resolution - 1);

                return angleTable[yi * resolution + xi];
            }
        }

        #endregion
        private HashSet<ProjectileDefineSO> registeredDefines = new();
        public static void AddDefine(ProjectileDefineSO d)
        {
            if (instance == null)
                return;
            if (instance.registeredDefines.Add(d))
            {
                instance.CreateParticleSystemForDefine(d);
            }
        }

        private Dictionary<string, ParticleSystem> systemDict;
        private Dictionary<string, List<ParticleSystem.Particle>> particlesByDefine;

        private Dictionary<string, ParticleSystem.Particle[]> particleArrayCache;

        static ProjectileRenderer instance;
        private FastRotationGrid rotationLookup;
        private void Awake()
        {
            instance = this;
            systemDict = new Dictionary<string, ParticleSystem>();
            particlesByDefine = new Dictionary<string, List<ParticleSystem.Particle>>();
            particleArrayCache = new Dictionary<string, ParticleSystem.Particle[]>();
            rotationLookup = new FastRotationGrid(360f / 90f);

            if (lootParticle != null)
            {
                lootParticle.Clear();
                lootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (activeBatches.Count > 0 && !coroutineRunning && lootParticle != null)
            {
                StartCoroutine(LootParticleUpdateCoroutine());
            }
        }
        private void OnDestroy()
        {
            if (instance == this)
            {
                activeBatches.Clear();
                coroutineRunning = false;

                instance = null;
            }

            foreach (var ps in systemDict.Values)
            {
                if (ps != null)
                    Destroy(ps.gameObject);
            }
            systemDict.Clear();
            particlesByDefine.Clear();
            particleArrayCache.Clear();
        }
        private void CreateParticleSystemForDefine(ProjectileDefineSO define)
        {
            if (define == null)
            {
                Debug.LogWarning("ProjectileSystem: Tried to create PS for null define.");
                return;
            }

            if (define.particleSystemPrefab == null)
            {
                Debug.LogWarning($"ProjectileSystem: Define '{define.name}' has no prefab.");
                return;
            }

            string key = string.Intern(define.name);

            if (systemDict.ContainsKey(key))
            {
                Debug.LogWarning(
                    $"ProjectileSystem: ParticleSystem for define '{define.name}' already exists. Ignoring duplicate."
                );
                return;
            }

            try
            {
                var psInstance = Instantiate(define.particleSystemPrefab, transform);

                if (psInstance == null)
                {
                    Debug.LogWarning($"ProjectileSystem: Failed to instantiate prefab for '{define.name}'.");
                    return;
                }

                psInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                systemDict[key] = psInstance;
                particlesByDefine[key] = new List<ParticleSystem.Particle>(1024);
                particleArrayCache[key] = new ParticleSystem.Particle[1024];
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ProjectileSystem: Exception for define '{define.name}': {ex}");
            }
        }
        #region Black Magic scaling consts
        const float growTime = 0.075f;
        const float shrinkTime = 0.1f;
        const float peakScale = 2f;
        #endregion
        public void RenderProjectiles(List<Projectile> projectiles)
        {
            foreach (var list in particlesByDefine.Values)
                list.Clear();

            foreach (var projectile in projectiles)
            {
                var define = projectile.data;
                if (define == null)
                {
                    Debug.LogWarning("ProjectileSystem: Projectile data define is null.");
                    continue;
                }

                string key = string.Intern(define.name);

                if (!systemDict.TryGetValue(key, out var ps))
                {
                    Debug.LogWarning($"No particle system found for projectile define {define?.name}");
                    continue;
                }

                var textureSheetAnimation = ps.textureSheetAnimation;
                int totalFrames = textureSheetAnimation.numTilesX * textureSheetAnimation.numTilesY;

                float animationDuration = define.animationSpeed <= 0.01f
                    ? 1f
                    : totalFrames / define.animationSpeed;

                float animationElapsed = Time.time - (projectile.spawnTime + projectile.animationOffsetSeconds);
                float unmodifiedElapsed = Time.time - (projectile.spawnTime);
                float addedSpin = animationElapsed * define.spin;
                animationElapsed %= animationDuration;

                float angle = rotationLookup.GetRotation(projectile.VelocityNotZero);

                #region black magic scaling
                float scaleFactor = 1f;

                if (projectile.Faction == ProjectileFaction.Enemy)
                {
                    float t = unmodifiedElapsed;

                    if (t <= growTime)
                    {
                        scaleFactor = Mathf.SmoothStep(0f, peakScale, t / growTime);
                    }
                    else if (t <= growTime + shrinkTime)
                    {
                        scaleFactor = Mathf.Lerp(peakScale, 1f, (t - growTime) / shrinkTime);
                    }
                    else
                    {
                        //scaleFactor = 1f;
                        scaleFactor = Mathf.Lerp(peakScale, 1f, (t - growTime) / shrinkTime);
                    }
                }

                float oldGrowScale = unmodifiedElapsed.Multiply(8f).Clamp(0f, 1f);
                #endregion

                var particle = new ParticleSystem.Particle
                {
                    position = new Vector3(projectile.Position.x, projectile.Position.y, 0),
                    startColor = projectile.Faction == ProjectileFaction.Player && FumoSettingsTags.HasBoolTag(FumoSettingsTags.KeysShmup.PlayerShotVisibilityReduction) ? define.Color.Opacity((((float)define.Color.a) * 0.2f).ToByte()) : define.Color,
                    startSize = define.Size * (projectile.Faction != ProjectileFaction.Enemy ? 1f : scaleFactor),
                    startLifetime = animationDuration,
                    remainingLifetime = animationDuration - animationElapsed,
                    rotation3D = new Vector3(0, 0, -(angle + addedSpin)),
                };

                particlesByDefine[key].Add(particle);
            }

            foreach (var kvp in particlesByDefine)
            {
                if (!systemDict.TryGetValue(kvp.Key, out var ps))
                    continue;

                IEnumerable<ProjectileDefineSO> defines = registeredDefines.Where(d => string.Intern(d.name) == kvp.Key);
                foreach (var define in defines)
                {
                    if (define == null)
                    {
                        Debug.LogWarning($"ProjectileSystem: No define found matching key {kvp.Key} for particle update.");
                        continue;
                    }
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();//this is affordable its 1 per frame per system (not per projectile)
                    if (renderer != null)
                    {
                        if (renderer.sortingLayerName != define.SortingLayer)
                            renderer.sortingLayerName = define.SortingLayer;

                        renderer.sortingOrder = 10000 - Mathf.RoundToInt((define.Size * define.SizeSortingMultiplier) * 100);
                    }
                }

                if (kvp.Value.Count > 0)
                {
                    if (!particleArrayCache.TryGetValue(kvp.Key, out var array) || array.Length < kvp.Value.Count)
                    {
                        array = new ParticleSystem.Particle[kvp.Value.Count];
                        particleArrayCache[kvp.Key] = array;
                    }

                    kvp.Value.CopyTo(array);
                    ps.SetParticles(array, kvp.Value.Count);
                }
                else
                {
                    ps.Clear();
                }
            }
        }
    }
}
