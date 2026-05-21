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
                IEnumerator CO_Play()
                {
                    yield return delay.WaitForSeconds();
                    Vector2 rng = (RNG.SeededRandomVector2.normalized * RNG.RandomFloatRange(randomRadiusRange.x, randomRadiusRange.y));
                    ps.PlayCachedOnce(e.CurrentPosition + rng);
                    deathSound.Play(e.CurrentPosition + rng);
                }
                CO_Play().RunRoutine();
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
        public void ClearPhase()
        {
            bool currentPhase = true;
            if (currentPhase)
            {
                Action_BossRecenter(0.7f);
            }
            currentPhase = false;
        }
        public void ForceNextPhaseSet()
        {
            {
                ProjectileRunner.TriggerSweep(0f, 0, false, out _);
                ForceKill();
            }
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
                    ProjectileRunner.TriggerSweep(0f, 255, false, out _);
                    Action_BossRecenter(0.85f);
                    StallAttackLoop(0.5f);
                    SetIframes(1.25f, 90f);
                    CurrentPhase = found;
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
        [System.Serializable]
        public class UnitPhase
        {
            [SerializeReference] public List<UnitAttack> phaseAttacks = new();
            public float phaseHealth = 2000f;
            public bool blockDash;
            int phaseIndex;
            public bool DetermineNext(out UnitAttack next)
            {
                next = null;
                if (phaseAttacks != null && phaseAttacks.Count > 0)
                {
                    int selection = phaseIndex % phaseAttacks.Count;
                    next = phaseAttacks[selection];
                    phaseIndex = phaseIndex + 1;
                }
                return next != null;
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
            [SerializeField, Range(0f, 6f)] public float LoopsDelay = 0f;
            [SerializeReference] public List<UnitAttack> attacks = new();
            int attackIndex;
            public bool DetermineNext(out UnitAttack next)
            {
                next = null;
                if (attacks != null && attacks.Count > 0 && attackIndex < Loops)
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
        private static float BossPhaseStallEnd;
        public static bool BossPhaseStall => Time.time < BossPhaseStallEnd + 0.15f;
        [Initialize(-123)]
        static void ReinitializeBossPhaseStall()
        {
            BossPhaseStallEnd = -1f;
        }

        Coroutine CurrentRunningAttack;
        public float AttackStallEndTime { get; private set; }
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
        private void AttacksStarterLoop()
        {
            if (Time.time < AttackStallEndTime)
                return;
            if (!IsOnScreenAndAlive)
            {
                AttackStallEndTime = AttackStallEndTime.Max(Time.time + 0.1f);
            }

            if (this.IsRunningActions)
            {
                return;
            }
            if (ProjectileRunner.IsSweeping)
                return;
            if (TryGetNextPhase(out UnitPhase phase))
            {

            }

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
                    if (containedBaseAttack.TryStartNext(this, out Coroutine nextBaseAttack, () => StallAttackLoop(containedBaseAttack.LoopsDelay)))
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

        public delegate void OnAnyDamagedEvent(float damage);
        public static event OnAnyDamagedEvent WhenAnyEnemyDamaged;
    }
    #endregion
    #region Hit Interface
    public partial class EnemyUnit : IHit
    {
        public bool IsPlayer => false;
        float sealRadius;
        public float CurrentHealth { get; private set; } = 1000f;
        [SerializeField] float startingHealth = 350f;
        [SerializeField] ACWrapper hitSound, lowHitSound;
        public float CurrentMaxHealth { get; private set; }
        public float HealthPercent => CurrentHealth.Clamp(0, CurrentMaxHealth) / CurrentMaxHealth.Clamp(1, 99999999f) * 100f;
        public void StartNewHealth(float newHealth, float maxHealth)
        {
            CurrentMaxHealth = maxHealth;
            CurrentHealth = newHealth.Clamp(0, maxHealth);
            MaintainAliveEnemy(this, new AliveSetterPacket()
            {

            });
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
                    if (HealthPercent < 15f && CurrentMaxHealth > 200)
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
                    damage *= 1f;
                    GameSession.TryAddScoreRaw(damage * 250d, "Enemy Damage");
                    damageDealt = damage.Min(CurrentHealth);
                    phaseTrackedDamage += damageDealt;
                    WhenAnyEnemyDamaged?.Invoke(damageDealt);
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
                //Spellcard.FailSpell();
                //Spellcard.EndSpell();
            }
            CurrentHealth = 0f;
            PlayDeathEffects();
            CalculateAlive();
            gameObject.SetActive(false);
        }
        public void KillWithLoot()
        {
            if (sealRadius > 0.1f)
            {
                ProjectileRunner.SealBullets(CurrentPosition, this, sealRadius, 255, out _);
            }
            CreateLootItem(LootCount, 2.5f);
            TriggerRevengeOverride();

            #region Sweeping
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
            #endregion

            PlayDeathEffects();
            gameObject.SetActive(false);
            CalculateAlive();
            WhenEnemyKilled?.Invoke(this);
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
                SetIframes(1.25f, 90f);
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
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.transform.CompareTag(TagHandle.GetExistingTag(string.Intern("Player"))) && collision.TryGetComponent(out IHit hit))
            {
                hit.SendHit(new IHit.HitPacket(collision.ClosestPoint(collision.transform.position), new(this, 100f, 1f)), out float damage);
            }
        }
        private void Think()
        {
            if (SceneLoader.IsLoading)
                return;
            ShmupPlayer.PlayerAs(out ShmupPlayer p);
            if (IsOnScreenAndAlive)
            {

            }
            if (!IsRunningActions)
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
            base.WhenUpdate();
            AttacksStarterLoop();
        }
    }
}
