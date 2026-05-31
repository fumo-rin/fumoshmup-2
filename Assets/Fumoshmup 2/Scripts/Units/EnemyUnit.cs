using UnityEngine;
using rinCore;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor;
using System;

namespace FumoShmup2
{
    #region Map Exit Position
    public partial class EnemyUnit
    {
        public struct ExitPacket
        {
            public float delay, duration, x, y;
            public ExitPacket(float delay, float duration, Vector2 xy)
            {
                this.delay = delay;
                this.duration = duration;
                this.x = xy.x;
                this.y = xy.y;
            }
        }
        private void ExitAfter(ExitPacket packet)
        {
            IEnumerator CO_ExitAfter()
            {
                float lerpEndTime = packet.duration + Time.time + packet.delay;
                if (packet.delay < 0)
                {
                    yield break;
                }
                yield return null;
                yield return packet.delay.WaitForSeconds();
                MoveLerpEndTime = lerpEndTime;
                float start = Time.time;
                SetAction("Exit", new MoveLerpAction(this, packet.duration, new()
                {
                    UnMappedStart = CurrentPosition,
                    UnMappedEnd = new(packet.x, packet.y),
                    duration = packet.duration
                }));
                yield return packet.duration.WaitForSeconds();
                this.ForceKill();
            }
            StartCoroutine(CO_ExitAfter());
        }
        private void ForceKillAfter(float delay)
        {
            IEnumerator CO_Forcekill()
            {
                yield return delay.WaitForSeconds();
                this.ForceKill();
            }
            StartCoroutine(CO_Forcekill());
        }
    }
    #endregion
    #region Overrides
    public partial class EnemyUnit
    {
        struct SweepOverride
        {
            public float duration;
            public byte lootChance;
        }
        SweepOverride? sweepOverride = null;
        private bool TryGetSweepOverride(out SweepOverride sweep)
        {
            sweep = sweepOverride ?? new SweepOverride();
            return sweepOverride != null && sweep.duration > 0f;
        }
        public void SetSweepOverride(float duration, byte lootChange)
        {
            sweepOverride = new SweepOverride()
            {
                duration = duration,
                lootChance = lootChange,
            };
        }
        RevengeAttackOverride revengeOverride;
        public void SetRevengeAttackOverride(RevengeAttackOverride revenge)
        {
            revengeOverride = revenge;
        }
        void TriggerRevengeOverride()
        {
            if (revengeOverride != null) revengeOverride.TriggerRevenge(this);
        }
    }
    #endregion
    #region Death Effect
    public partial class EnemyUnit
    {
        [System.Serializable]
        class deathEffect
        {
            [SerializeField] ACWrapper deathSound;
            [SerializeField] ParticleSystem ps;
            [SerializeField] float delay;
            [SerializeField] Vector2 randomRadiusRange = new(0.45f, 0.65f);
            public void Play(EnemyUnit e)
            {
                IEnumerator CO_Play(Vector2 position)
                {
                    yield return delay.WaitForSeconds();
                    Vector2 rng = (RNG.SeededRandomVector2.normalized * RNG.FloatRange(randomRadiusRange.x, randomRadiusRange.y));
                    ps.PlayCachedOnce(position + rng);
                    deathSound.Play(position + rng);
                }
                CO_Play(e.CurrentPosition).RunRoutine();
            }
        }
        [SerializeField] List<deathEffect> deathEffects = new();
        private void PlayDeathEffects()
        {
            if (ShmupWorldspace.WorldSpace is Rect r && r.Contains(CurrentPosition))
                foreach (var effect in deathEffects)
                {
                    effect.Play(this);
                }
        }
    }
    #endregion
    #region Editor Drawer
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UnitAttack), true)]
    public class UnitAttackDrawer : PropertyDrawer
    {
        private static List<Type> cachedTypes;

        static UnitAttackDrawer()
        {
            CacheTypes();
        }

        private static void CacheTypes()
        {
            cachedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    typeof(UnitAttack).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsGenericType &&
                    t.IsClass)
                .ToList();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        private static string GetDisplayName(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue == null)
                return "Select UnitAttack";

            Type type = property.managedReferenceValue.GetType();

            return CleanName(type.Name).SpaceByCapitals();
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.IndentedRect(position);

            string displayName = GetDisplayName(property, label);

            if (property.managedReferenceValue == null)
            {
                if (GUI.Button(position, displayName))
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("None"), false, () =>
                    {
                        property.managedReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    });

                    menu.AddSeparator("");

                    foreach (var type in cachedTypes.OrderBy(t => GetNestedPath(t)))
                    {
                        string path = GetNestedPath(type);

                        menu.AddItem(new GUIContent(path), false, () =>
                        {
                            property.managedReferenceValue = Activator.CreateInstance(type);
                            property.serializedObject.ApplyModifiedProperties();
                            property.serializedObject.Update();
                        });
                    }

                    menu.ShowAsContext();
                }
            }
            else
            {
                label.text = displayName;
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        private static string GetNestedPath(Type type)
        {
            List<string> parts = new List<string>();

            Type current = type;
            while (current != null)
            {
                parts.Add(CleanName(current.Name));
                current = current.DeclaringType;
            }

            parts.Reverse();

            return string.Join("/", parts);
        }

        private static string CleanName(string name)
        {
            int index = name.IndexOf('`');
            if (index >= 0)
                name = name.Substring(0, index);

            return name;
        }
    }
#endif
    #endregion
    #region Boss Phases
    public partial class EnemyUnit
    {
        public bool IsBoss => HasPhases;
        public void StopActionsForBoss()
        {
            StopMovement();
            Action_BossRecenter(0.85f);
            SetIframes(1.25f, 90f);
            StallAttackLoop(0.9f);
        }
        float PhasesTotalHealth
        {
            get
            {
                float total = 0f;
                foreach (var item in phases)
                {
                    total += item.phaseHealth;
                }
                return total;
            }
        }
        float phaseTrackedDamage;
        [SerializeField] List<UnitPhase> phases = new();
        HashSet<UnitPhase> spentPhases = new();
        UnitPhase LastKnownPhase;
        public bool HasPhases => phases != null && phases.Count > 0;
        public UnitPhase CurrentPhase { get; private set; }
        public bool TryGetNextPhase(out UnitPhase nextPhase)
        {
            nextPhase = null;
            UnitPhase found = null;
            bool overkill = phaseTrackedDamage >= PhasesTotalHealth;
            if (overkill)
            {
                return false;
            }
            float remaining = phaseTrackedDamage;

            foreach (var item in phases)
            {
                if (remaining < item.phaseHealth)
                {
                    found = item;
                    break;
                }
                remaining -= item.phaseHealth;
            }

            if (found != null && !spentPhases.TryGetValue(found, out _))
            {
                if ((LastKnownPhase != null && LastKnownPhase != found) || LastKnownPhase == null)
                {
                    CurrentPhase = found;
                    CurrentPhase.PhaseIndex = 0;
                }
                LastKnownPhase = found;
                spentPhases.Add(found);
                return true;
            }
            return false;
        }
        public bool StartPhaseAttack(UnitPhase nextPhase, out Coroutine routine, Action callback)
        {
            routine = null;
            CurrentPhase = nextPhase;
            if (CurrentPhase.DetermineNext(out UnitAttack next))
            {
                routine = next.StartWithSender(this, callback);
            }
            nextPhase = CurrentPhase;
            return routine != null;
        }
        private void ValidatePhaseLoops()
        {
            bool changed = false;
            foreach (var item in phases)
            {
                if (item != null)
                {
                    if (item.Loops < 1)
                    {
                        item.Loops = 1;
                        changed = true;
                        this.Dirty();
                    }
                }
            }
            if (changed)
            {
                this.SetDirtyAndSave();
            }
        }
        [System.Serializable]
        public class UnitPhase
        {
            [SerializeReference] public List<UnitAttack> phaseAttacks = new();
            public float phaseHealth = 2000f;
            public bool blockDash;
            [Min(1)] public int Loops = 1;
            public int PhaseIndex;
            public bool DetermineNext(out UnitAttack next)
            {
                next = null;
                if (phaseAttacks != null && phaseAttacks.Count > 0)
                {
                    int selection = PhaseIndex % phaseAttacks.Count;
                    next = phaseAttacks[selection];
                    PhaseIndex = PhaseIndex + 1;
                }
                return next != null;
            }
            public bool HasAmmo
            {
                get
                {
                    if (phaseAttacks == null || phaseAttacks.Count <= 0)
                    {
                        return false;
                    }
                    int cap = phaseAttacks.Count * (Loops.Max(1));
                    if (cap > PhaseIndex)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
    }
    #endregion
    #region Default Attack Component
    public partial class EnemyUnit
    {
        #region Attack Component
        [System.Serializable]
        public class AttackComponent
        {
            public int Loops = 3;
            private int Steps => attacks == null ? 0 : Loops * attacks.Count;
            [SerializeField, Range(0f, 6f)] public float LoopsDelay = 0f;
            [SerializeReference] public List<UnitAttack> attacks = new();
            int attackIndex;
            public bool DetermineNext(out UnitAttack next)
            {
                next = null;
                if (attacks != null && attacks.Count > 0 && attackIndex < Steps)
                {
                    int selection = attackIndex % attacks.Count;
                    next = attacks[selection];
                    attackIndex = attackIndex + 1;
                }
                return next != null;
            }
            public bool TryStartNext(EnemyUnit e, out Coroutine routine, Action callback)
            {
                routine = null;
                if (DetermineNext(out UnitAttack next))
                {
                    routine = next.StartWithSender(e, callback);
                }
                return routine != null;
            }
            public AttackComponent(int loops, float loopDelay020, params UnitAttack[] attacks)
            {
                this.Loops = loops;
                this.LoopsDelay = loopDelay020;
                this.attacks = new();
                attackIndex = 0;
                foreach (var item in attacks)
                {
                    if (item == null)
                        continue;
                    this.attacks.Add(item.Clone());
                }
            }
        }
        #endregion
        AttackComponent containedBaseAttack = null;
        public void SetBaseAttacks(AttackComponent a) => containedBaseAttack = a;
    }
    #endregion
    #region Phase Skipping & Utility
    public partial class EnemyUnit
    {
        public bool WillChangePhase(float incomingDamage)
        {
            if (!HasPhases)
                return false;

            float before = phaseTrackedDamage;
            float after = phaseTrackedDamage + incomingDamage;
            float accumulated = 0f;
            foreach (var phase in phases)
            {
                accumulated += phase.phaseHealth;

                bool beforeReached = before >= accumulated;
                bool afterReached = after >= accumulated;

                if (!beforeReached && afterReached)
                {
                    return true;
                }
            }
            return false;
        }
        private static float BossPhaseStallEnd;
        public static bool BossPhaseStall => Time.time < BossPhaseStallEnd + 0.15f;
        [Initialize(-123)]
        static void ReinitializeBossPhaseStall()
        {
            BossPhaseStallEnd = -1f;
        }
        Coroutine CurrentRunningAttack;
        public float AttackStallEndTime { get; private set; }
        public WaitUntil WaitForAttackStall => new WaitUntil(() => Time.time > AttackStallEndTime);
        public IEnumerable<float> RemainingPhaseNotches01
        {
            get
            {
                float totalPhaseHealth = PhasesTotalHealth;
                if (totalPhaseHealth <= 0f)
                    yield break;

                float cumulativeDamage = 0f;
                foreach (var phase in phases)
                {
                    cumulativeDamage += phase.phaseHealth;
                    if (cumulativeDamage <= phaseTrackedDamage)
                        continue;
                    if (cumulativeDamage >= totalPhaseHealth)
                        continue;
                    yield return (1f - cumulativeDamage / totalPhaseHealth).Clamp(0f, 1f);
                }
            }
        }
        public float NextPhaseHealth
        {
            get
            {
                float totalHealth = PhasesTotalHealth;
                float cumulative = 0f;
                foreach (var phase in phases)
                {
                    cumulative += phase.phaseHealth;
                    if (phaseTrackedDamage < cumulative)
                    {
                        return totalHealth - cumulative;
                    }
                }
                return 0f;
            }
        }
        public void StallAttackLoop(float duration, bool stopAttack = true)
        {
            AttackStallEndTime = AttackStallEndTime.Max(Time.time + duration);
            if (stopAttack)
            {
                if (CurrentRunningAttack != null)
                {
                    StopCoroutine(CurrentRunningAttack);
                    CurrentRunningAttack = null;
                }
                CurrentRunningAttack = null;
                if (IsBoss)
                {
                    BossPhaseStallEnd = Time.time + duration;
                }
            }
        }
        private bool ShouldExpireBossPhase()
        {
            if (!IsBoss)
                return false;
            if (CurrentRunningAttack != null)
                return false;
            if (CurrentPhase == null)
                return false;
            if (CurrentPhase is UnitPhase p)
            {
                return !p.HasAmmo;
            }
            return true;
        }
        private void AttacksStarterLoop()
        {
            if (Time.time < AttackStallEndTime)
                return;

            if (!IsOnScreenAndAlive)
            {
                AttackStallEndTime = AttackStallEndTime.Max(Time.time + 0.1f);
            }
            if (TryGetNextPhase(out UnitPhase phase))
            {

            }
            if (ShouldExpireBossPhase())
            {
                StartNewHealth(NextPhaseHealth - 0.01f, PhasesTotalHealth);
                phaseTrackedDamage = PhasesTotalHealth - CurrentHealth.Add(0.01f);
                if (CurrentHealth <= 0f)
                {
                    ForceKill();
                    return;
                }
                if (TryGetNextPhase(out UnitPhase postExpirePhase))
                {
                    StopActionsForBoss();
                }
                else
                {
                    ForceKill();
                    return;
                }
            }
            if (this.IsRunningActions)
            {
                return;
            }
            if (ProjectileRunner.IsSweeping)
                return;

            if (ShmupPlayer.PlayerAs(out ShmupPlayer p) && (!p.IsAlive || p.IframesDurationLeft > 0.75f))
            {
                return;
            }
            switch (IsBoss)
            {
                case true:
                    if (CurrentRunningAttack != null)
                    {
                        return;
                    }
                    if (CurrentPhase != null)
                    {
                        StartPhaseAttack(CurrentPhase, out Coroutine nextBossAttack, () => CurrentRunningAttack = null);
                        CurrentRunningAttack = nextBossAttack;
                    }
                    break;
                case false:

                    if (CurrentRunningAttack != null)
                    {
                        return;
                    }
                    if (containedBaseAttack != null && containedBaseAttack.TryStartNext(this, out Coroutine nextBaseAttack, () => StallAttackLoop(containedBaseAttack.LoopsDelay)))
                    {
                        CurrentRunningAttack = nextBaseAttack;
                    }
                    break;
            }
        }
    }
    #endregion
    #region Damage Events
    public partial class EnemyUnit
    {
        public delegate void OnKilledEvent(EnemyUnit e);
        public static event OnKilledEvent WhenEnemyKilled;

        public delegate void OnAnyDamagedEvent(float finalDamage);
        public static event OnAnyDamagedEvent WhenAnyEnemyDamaged;

        public delegate void OnDamaged(float finalDamage, IHit.HitPacket packet);
        public event OnDamaged WhenDamaged;
    }
    #endregion
    #region Hit Interface
    public partial class EnemyUnit : IHit
    {
        float sealRadius;
        public float CurrentHealth { get; private set; } = 1000f;
        public string HealthString => $"{CurrentHealth.Clamp(1f, CurrentMaxHealth).ToString("F0")}/{CurrentMaxHealth.ToString("F0")}";
        [SerializeField] float startingHealth = 350f;
        [SerializeField] ACWrapper hitSound, lowHitSound;
        RollingFloatTracker damageCap;
        public struct RecentDamage
        {
            public float WindowTotal;
            public float PerSecond;
            public float EMA_PerSecond;
            public float WindowDuration;
            public RecentDamage(RollingFloatTracker tracker)
            {
                if (tracker == null)
                {
                    WindowDuration = 0f;
                    WindowTotal = 0f;
                    EMA_PerSecond = 0f;
                    PerSecond = 0f;
                }
                WindowDuration = tracker.interval;
                WindowTotal = tracker.Total;
                EMA_PerSecond = tracker.EMA_PerSecond;
                PerSecond = tracker.PerSecond;
            }
        }
        public RecentDamage RecentDamageTaken => new(damageCap);
        public float CurrentMaxHealth { get; private set; }
        public float HealthPercent100 => CurrentHealth.Clamp(0, CurrentMaxHealth) / CurrentMaxHealth.Clamp(1, 99999999f) * 100f;
        public void StartNewHealth(float newHealth, float maxHealth)
        {
            CurrentMaxHealth = maxHealth;
            CurrentHealth = newHealth.Clamp(0, maxHealth);
            MaintainAliveEnemy(this, new AliveSetterPacket()
            {

            });
            if (IsBoss)
                Debug.Log("New Health : " + CurrentHealth + " of " + CurrentMaxHealth);
        }
        public void SetSealRadius(float r) => sealRadius = r;
        int LootCount => !IsBoss ? CurrentMaxHealth.Multiply(0.04f).ToInt().Max(4) : PhasesTotalHealth.Multiply(0.02f).Clamp(50f, 1000f).ToInt();
        public void SendHit(IHit.HitPacket packet, out float damageDealt)
        {
            damageDealt = 0f;
            if (IsAlive)
            {
                if (packet.FinalDamage >= CurrentHealth)
                {
                    CurrentHealth = 0f;
                    KillWithLoot();
                    return;
                }
                if (ShmupWorldspace.WorldSpace is Rect r && r.Contains(packet.position))
                {
                    if (HasIframes && CurrentIFramesDamageReductionPercent >= 100)
                    {
                        return;
                    }
                    if (HealthPercent100 < 15f && CurrentMaxHealth > 200)
                    {
                        lowHitSound.Play(CurrentPosition);
                    }
                    else
                    {
                        hitSound.Play(CurrentPosition);
                    }
                    float damage = packet.FinalDamage;
                    if (HasIframes && CurrentIFramesDamageReductionPercent > 0)
                    {
                        damage *= Mathf.Max(0f, 1f - CurrentIFramesDamageReductionPercent * 0.01f);
                    }
                    if (IsBoss)
                    {
                        float halfPercent = CurrentMaxHealth * 0.005f;
                        for (int i = 0; i < 40; i++)
                        {
                            if (damageCap.PerSecond > i.AsFloat(halfPercent))
                            {
                                damage *= 0.95f;
                            }
                        }
                    }
                    GameSession.TryAddScoreRaw(damage * 250d, "Enemy Damage");
                    damageDealt = damage.Min(CurrentHealth);
                    if (WillChangePhase(damageDealt))
                    {
                        DoSweep();
                        StopActionsForBoss();
                    }
                    phaseTrackedDamage += damageDealt;
                    WhenAnyEnemyDamaged?.Invoke(damageDealt);
                    WhenDamaged?.Invoke(damageDealt, packet);
                    if (damageCap != null)
                    {
                        damageCap += damageDealt;
                    }
                    double healthPercentDelta = CurrentMaxHealth == 0d ? 0d : (double)damageDealt / (double)CurrentMaxHealth;
                    StartNewHealth(CurrentHealth - damage, CurrentMaxHealth);

                    if (IsBoss)
                    {
                        const double bossScorePercent = 0.05d;
                        GameSession.ReadCurrentRawScore(out double currentScore);
                        double scoreReward = currentScore * healthPercentDelta * bossScorePercent;
                        GameSession.TryAddScoreRaw(scoreReward, "Boss Damage");
                    }
                }
            }
        }
        public override void ForceKill()
        {
            if (this == null)
            {
                return;
            }
            if (IsBoss)
            {
                ProjectileRunner.TriggerSweep(0f, 0, false, out _);
            }
            CurrentHealth = 0f;
            PlayDeathEffects();
            CalculateAlive();
            gameObject.SetActive(false);
        }
        void DoSweep()
        {
            if (TryGetSweepOverride(out SweepOverride sweep))
            {
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
                {
                    SweepOnKill.SweepData s = new(sweep.duration, sweep.lootChance);
                    SweepOnKill.SweepStuff(this, s, false);

                    ShmupWorldspace.MapWorldspaceToNormalized(CurrentPosition, out Vector2 deathPos01, false);
                    ShockwaveEffect.Trigger(deathPos01, 3.5f);
                }
            }
            else
            {
                if (IsBoss)
                {
                    if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
                    {
                        SweepOnKill.SweepData s = new(0.35f, 255);
                        SweepOnKill.SweepStuff(this, s, false);
                    }

                    ShmupWorldspace.MapWorldspaceToNormalized(CurrentPosition, out Vector2 deathPos01, false);
                    ShockwaveEffect.Trigger(deathPos01, 3.5f);
                }
            }
        }
        public void KillWithLoot()
        {
            if (sealRadius > 0.1f)
            {
                ProjectileRunner.SealBullets(CurrentPosition, this, sealRadius, 255, out _);
            }
            TriggerRevengeOverride();

            DoSweep();

            CreateLootItem(LootCount, 2.5f);
            PlayDeathEffects();
            gameObject.SetActive(false);
            CalculateAlive();
            WhenEnemyKilled?.Invoke(this);
            Destroy(gameObject);
        }
        private void CreateLootItem(int lootCount, float areaSize = 1f)
        {
            CreateLootItemOnPosition(CurrentPosition, lootCount, areaSize);
        }
        private void CreateLootItemOnPosition(Vector2 position, int lootCount, float areaSize = 1f)
        {
            for (int i = 0; i < IntExtensions.Clamp(lootCount, 0, 10000); i++)
            {
                PointItemRunner.SpawnPointItem(position + (RNG.SeededRandomVector2.normalized * 0.75f.Spread(50f) * areaSize));
            }
        }
    }
    #endregion
    #region Find Enemies
    public partial class EnemyUnit
    {
        public static bool FindEnemyFromDotProduct(Vector2 origin, Vector2 direction, out EnemyUnit found, float minDot = -1f)
        {
            found = null;
            if (direction.sqrMagnitude < Mathf.Epsilon) return false;

            Vector2 dirNorm = direction.normalized;

            float bestScore = float.NegativeInfinity;

            foreach (var item in AliveEnemiesOnScreen)
            {
                if (item == null) continue;
                if (!ShmupWorldspace.BiggerWorldSpace.Contains(item.CurrentPosition))
                    continue;

                Vector2 toEnemy = item.CurrentPosition - origin;
                float dist = toEnemy.magnitude;
                if (dist < Mathf.Epsilon)
                {
                    found = item;
                    return true;
                }

                Vector2 toDir = toEnemy / dist;
                float dotVal = Vector2.Dot(dirNorm, toDir);

                if (dotVal < minDot) continue;

                float distanceScore = 1f / (1f + dist);
                float score = dotVal - dist * 0.0025f;
                if (score > bestScore)
                {
                    bestScore = score;
                    found = item;
                }
            }

            return found != null;
        }
    }
    #endregion
    #region EnemyActions
    public partial class EnemyUnit
    {
        public float MoveLerpEndTime { get; private set; }
        public bool IsLerpMoving => Time.time < MoveLerpEndTime;
        public void Action_MoveWithLerp(MoveLerpAction.LerpSettings lerp)
        {
            this.SetAction("Move With Lerp", new MoveLerpAction(this, lerp.duration, lerp));
            MoveLerpEndTime = lerp.duration + Time.time;
        }
        public void Action_BossRecenter(float duration)
        {
            ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 0.6f, out Vector2 b);
            this.SetAction("Boss Recenter", new MoveLerpAction(this, duration, new MoveLerpAction.LerpSettings(CurrentPosition, b, duration)));
            MoveLerpEndTime = duration + Time.time;
        }
        public void Action_ExitAfter(ExitPacket packet)
        {
            this.ExitAfter(packet);
        }
        public void Action_ForceKillAfter(float delay) => ForceKillAfter(delay);
    }
    #endregion
    #region Leash
    public partial class EnemyUnit
    {
        Coroutine leashRoutine;
        Vector2 leashPosition;
        private void Leash(Vector2 position, float radius, float maxSpeed)
        {
            if (leashRoutine == null)
            {
                leashRoutine = StartCoroutine(CO_Leash(this, radius, maxSpeed));
            }
        }
        internal void StopLeash()
        {
            if (leashRoutine != null)
            {
                StopCoroutine(leashRoutine);
            }
        }
        public void SetLeashPosition(Vector2 position)
        {
            leashPosition = position;
        }
        private IEnumerator CO_Leash(EnemyUnit e, float leashRadius, float speed)
        {
            Vector2 leash = leashPosition;
            while (e != null && e.IsAlive)
            {
                if (!IsRunningActions)
                {
                    if (leashPosition.SquareDistanceToGreaterThan(CurrentPosition, 50f))
                    {
                        leash = CurrentPosition;
                    }
                    if (leashPosition.SquareDistanceToLessThan(e.CurrentPosition, 0.01f))
                    {
                        leash = e.CurrentPosition + RNG.SeededRandomVector2;
                    }
                    RB.VelocityTowards((leash - CurrentPosition) * speed, 1.25f);
                }
                yield return null;
            }
            leashRoutine = null;
        }
    }
    #endregion
    #region Commands
    public partial class EnemyUnit
    {
        [QFSW.QC.Command("-slay")]
        public static void KillCommand()
        {
            foreach (var item in AliveEnemiesOnScreen)
            {
                item.SendHit(new IHit.HitPacket(item.CurrentPosition, new(null, 1000000, 1f)), out float hit);
            }
        }
    }
    #endregion
    public partial class EnemyUnit : ShmupUnit
    {
        [SerializeField] Collider2D unitCollision;
        public Collider2D UnitCollider
        {
            get
            {
                Collider2D item = unitCollision;
                if (item == null)
                {
                    item = transform.GetComponent<Collider2D>();
                    if (item != null)
                    {
                        unitCollision = item;
                    }
                }
                return item;
            }
        }
        public bool IsOnScreenAndAlive => IsAliveInsideRect(ShmupWorldspace.WorldSpace);
        public bool IsAliveInsideRect(in Rect r) => IsAlive && r.Contains(CurrentPosition);
        [SerializeField] SpriteRenderer enemyRenderer;
        public static IEnumerable<EnemyUnit> AliveEnemiesOnScreen
        {
            get
            {
                foreach (var item in FumoUnit.AliveEnemies)
                {
                    if (item is EnemyUnit e && e != null)
                    {
                        if (e.IsOnScreenAndAlive)
                            yield return e;
                    }
                }
            }
        }
        [SerializeField] List<Collider2D> shotHitboxes = new();
        public override IEnumerable<Collider2D> Hitboxes
        {
            get
            {
                foreach (var item in shotHitboxes)
                {
                    if (item == null)
                        continue;
                    yield return item;
                }
            }
        }

        private float IframesEndTime = 0f;
        public void SetIframes(float duration, float iFramesDamageReduction100)
        {
            Debug.Log("Set Iframes: " + iFramesDamageReduction100 + "%");
            CurrentIFramesDamageReductionPercent = iFramesDamageReduction100.Clamp(0f, 100f);
            IframesEndTime = duration + Time.time;
        }
        public override bool HasIframes => IframesEndTime > Time.time && IframesEndTime > 0f;
        protected override void WhenAwake()
        {
            bool hasPhases = HasPhases;
            if (!hasPhases) StartNewHealth(startingHealth, startingHealth);
            else
            {
                StartNewHealth(PhasesTotalHealth, PhasesTotalHealth);
            }
        }
        public static void Despawn(in List<FumoUnit> enemies)
        {
            var validEnemies = enemies.Where(x => x != null).ToList();
            for (int i = 0; i < validEnemies.Count; i++)
            {
                FumoUnit f = validEnemies[i];
                if (f is EnemyUnit e)
                {
                    e.ForceKill();
                }
            }
            validEnemies.Clear();
            validEnemies = null;
        }
        protected override void WhenDestroy()
        {
            base.WhenDestroy();
            ClearAllActions();
            StopAllCoroutines();
        }
        protected override void WhenStart()
        {
            base.WhenStart();
            if (IsBoss)
            {
                BossPhaseStallEnd = Time.time + 5f;
                damageCap = new(4f, 1f / 20f);
            }
        }
        private void OnDisable()
        {
            ClearAllActions();
            StopAllCoroutines();
            CalculateAlive();
        }
        private void OnEnable()
        {
        }
        private void LateUpdate()
        {
            Think();
        }
        /*private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag(TagHandle.GetExistingTag(string.Intern("Player"))) && collision.TryGetComponent(out IHit hit))
            {
                hit.SendHit(new IHit.HitPacket(collision.ClosestPoint(collision.transform.position), new(this, 100f, 1f)), out float damage);
            }
        }*/
        private void Think()
        {
            if (SceneLoader.IsLoading)
                return;
            bool hasPlayer = ShmupPlayer.PlayerAs(out ShmupPlayer p);
            if (IsOnScreenAndAlive)
            {

            }
            if (hasPlayer && IsAlive)
            {
                if (UnitCollider != null && p.PlayerUnitCollider != null &&
                    UnitCollider.IsTouching(p.PlayerUnitCollider) && p is IHit hit)
                {
                    Vector2 closest = UnitCollider.ClosestPoint(p.PlayerUnitCollider.bounds.center);
                    hit.SendHit(new IHit.HitPacket(closest, new(this, 100f, 1f)), out float damage);
                }
            }
            if (!IsRunningActions && !IsMovingWithAttack)
            {
                if (leashRoutine == null)
                {
                    Leash(CurrentPosition, 1.5f, 0.75f);
                }
            }
        }

        protected override bool CalculateAlive()
        {
            bool isAlive = CurrentHealth > 0f && gameObject != null && gameObject.activeInHierarchy;
            if (IsBoss)
            {
                if (isAlive)
                {
                    BossbarUI.AssignUnit(new()
                    {
                        target = this,
                        weight = GetHashCode()
                    });
                }
                else
                {
                    BossbarUI.UnassignUnit(this);
                }
            }
            return isAlive;
        }
        protected override void WhenUpdate()
        {
            if (damageCap != null)
            {
                damageCap.TickEMA();
            }
            base.WhenUpdate();
            AttacksStarterLoop();
        }
        private void OnValidate()
        {
            ValidatePhaseLoops();
        }
    }
}
