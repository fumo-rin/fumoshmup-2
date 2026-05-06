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
                yield return packet.delay.WaitForSeconds(false);
                MoveLerpEndTime = lerpEndTime;
                float start = Time.time;
                SetAction("Exit", new MoveLerpAction(this, packet.duration, new()
                {
                    UnMappedStart = CurrentPosition,
                    UnMappedEnd = new(packet.x, packet.y),
                    duration = packet.duration
                }));
                yield return packet.duration.WaitForSeconds(false);
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
            public ACWrapper sweepSound;
        }
        SweepOverride? sweepOverride = null;
        private bool TryGetSweepOverride(out SweepOverride sweep)
        {
            sweep = sweepOverride ?? new SweepOverride();
            return sweepOverride != null && sweep.duration > 0f;
        }
        public void SetSweepOverride(float duration, byte lootChange, ACWrapper sound)
        {
            sweepOverride = new SweepOverride()
            {
                duration = duration,
                lootChance = lootChange,
                sweepSound = sound
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
        public bool IsBoss => false;
        public float PhaseDamageTakenMod => IsBoss ? 1f : 1f;
        public void ClearPhase()
        {
            bool currentPhase = true;
            if (currentPhase)
            {
                int loot = 999;
                //ClearAllAttackRoutines();
                CreateLootItem(loot, 4.5f);
                Action_BossRecenter(0.65f);
            }
            currentPhase = false;
        }
        public void ForceNextPhaseSet()
        {
            /*if (phases.TryStartNextPhase(out BossPhaseSO nextPhase))
            {
                StartNewHealth(nextPhase.phaseHealth, nextPhase.phaseHealth);
            }*/
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
        public bool HasPhases => phases != null && phases.Count > 0;
        public UnitPhase CurrentPhase
        {
            get
            {
                UnitPhase found = null;
                bool overkill = phaseTrackedDamage >= PhasesTotalHealth;
                if (overkill)
                {
                    return null;
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
                return found;
            }
        }
        public bool TryStartNextPhase(out Coroutine routine, Action callback)
        {
            routine = null;
            if (CurrentPhase == null)
            {
                return false;
            }
            if (CurrentPhase.DetermineNext(out UnitAttack next))
            {
                routine = next.StartWithSender(this, callback);
            }
            return routine != null;
        }
        [System.Serializable]
        public class UnitPhase
        {
            [SerializeReference] public List<UnitAttack> phaseAttacks = new();
            public float phaseHealth = 2000f;
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
    #region Phase Skipping & Utility
    public partial class EnemyUnit
    {
        [Initialize(-9999)]
        private static void ReinitializePhaseStall()
        {
            phaseStallTimeEnd = -1f;
        }
        Coroutine CurrentRunningBossAttack;
        public static float phaseStallTimeEnd { get; private set; }
        public void SkipAndStallPhase(float duration)
        {
            phaseStallTimeEnd = Time.time + duration;
            if (CurrentRunningBossAttack != null)
            {
                StopCoroutine(CurrentRunningBossAttack);
            }
            CurrentRunningBossAttack = null;
        }
        private void BossAttackStarterLoop()
        {
            if (phaseStallTimeEnd < Time.time)
            {
                if (this.IsRunningActions)
                {
                    return;
                }
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p) && (!p.IsAlive || p.IframesDurationLeft > 0.75f))
                {
                    return;
                }
                if (CurrentRunningBossAttack != null)
                {
                    return;
                }
                if (TryStartNextPhase(out Coroutine next, () => CurrentRunningBossAttack = null))
                {
                    CurrentRunningBossAttack = next;
                }
            }
        }
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
            if (CurrentHealth > 0)
            {
                aliveEnemies.Add(this);
            }
            else
            {
                aliveEnemies.Remove(this);
            }
        }
        public void SetSealRadius(float r) => sealRadius = r;
        int LootCount => !IsBoss ? CurrentMaxHealth.Multiply(0.333f).ToInt() : 0;
        [NYI("Refactor")]
        public void SendHit(IHit.HitPacket packet, out float damageDealt)
        {
            damageDealt = 0f;
            if (IsAlive)
            {
                //Phase changes is handled elsewhere. apart from the start next phase.
                //it is handled on bossphases.phase class
                if (packet.FinalDamage >= CurrentHealth)
                {
                    CurrentHealth = 0f;
                    if (IsBoss)
                    {
                        //Spellcard.EndSpell();
                    }
                    bool hasNextPhase = false;
                    if (!hasNextPhase)
                    {
                        //WhenPhaseKilled?.Invoke(this, nextPhase);
                        //TriggerPhaseChange(false);
                        float phaseHealth = 1000f;
                        StartNewHealth(phaseHealth, phaseHealth);
                    }
                    else
                    {
                        ClearPhase();
                        KillWithLoot();
                    }
                }
                if (!IsOnScreenAndAlive)
                {
                    return;
                }
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
                damage *= PhaseDamageTakenMod;
                GameSession.TryAddScoreRaw(damage * 250d, "Enemy Damage");
                damageDealt = damage.Min(CurrentHealth);
                StartNewHealth(CurrentHealth - damage, CurrentMaxHealth);
            }
        }
        [NYI("Spellcard, kill event")]
        public void ForceKill()
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
            //TriggerKillEvent(true);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        [NYI("No Point Items spawning yet, and kill event")]
        public void KillWithLoot()
        {
            if (sealRadius > 0.1f) ProjectileRunner.SealBullets(CurrentPosition, this, sealRadius, 255, out _);
            CreateLootItem(LootCount, 2.5f);
            //TriggerKillEvent(false);
            TriggerRevengeOverride();
            if (TryGetSweepOverride(out SweepOverride sweep))
            {
                if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
                {
                    SweepOnKill.SweepData s = new(sweep.duration, sweep.lootChance, p.SweepSound);
                    SweepOnKill.SweepStuff(this, s, false);
                }
            }
            gameObject.SetActive(false);
            if (IsBoss)
            {
                void SpawnLoot(Vector2 position)
                {
                    CreateLootItemOnPosition(position, 20, 0.35f);
                }
                var explosion = new BossExplosion.data()
                {
                    maxRadius = 3.5f,
                    minRadius = 0.35f,
                    delay = 0.1f,
                    repeatDelay = 0.06f,
                    sizeRange = new(0.65f, 1.65f)
                };
                new BossExplosion(CurrentPosition, 6, explosion, SpawnLoot);
            }
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
                //PointItemRunner.Create(position + (Random.insideUnitCircle.normalized * 0.75f.Spread(50f) * areaSize));
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
            while (e != null && e.IsAlive)
            {
                if (!IsRunningActions)
                {
                    if (leashPosition.SquareDistanceToGreaterThan(CurrentPosition, 50f))
                    {
                        leashPosition = CurrentPosition;
                    }
                    if (leashPosition.SquareDistanceToLessThan(e.CurrentPosition, 0.01f))
                    {
                        leashPosition = e.CurrentPosition + RNG.SeededRandomVector2;
                    }
                    RB.VelocityTowards((leashPosition - CurrentPosition) * speed, 1.25f);
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
            foreach (var item in AliveEnemiesOnScreen.ToList())
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
        static EnemyUnit()
        {
            aliveEnemies = new();
        }
        static HashSet<EnemyUnit> aliveEnemies;
        [SerializeField] SpriteRenderer enemyRenderer;
        public static HashSet<EnemyUnit> AliveEnemies => aliveEnemies;
        public static IEnumerable<EnemyUnit> AliveEnemiesOnScreen
        {
            get
            {
                foreach (var item in aliveEnemies)
                {
                    if (item.IsOnScreenAndAlive)
                        yield return item;
                }
            }
        }

        public override IEnumerable<Collider2D> Hitboxes => throw new System.NotImplementedException();

        public override bool HasIframes => false;
        [NYI("No Phases Default Health")]
        protected override void WhenAwake()
        {
            if (aliveEnemies == null)
            {
                aliveEnemies = new();
            }
            bool hasPhases = false;
            if (!hasPhases) StartNewHealth(startingHealth, startingHealth);
        }
        public static void Despawn(in List<EnemyUnit> enemies)
        {
            var validEnemies = enemies.Where(x => x != null).ToList();
            for (int i = 0; i < validEnemies.Count; i++)
            {
                validEnemies[i].ForceKill();
            }
            validEnemies.Clear();
            validEnemies = null;
        }
        [NYI("Missing Spellcard")]
        protected override void WhenDestroy()
        {
            base.WhenDestroy();
            //SpellcardUI.WhenTimerExpire -= ForceNextPhase;
            aliveEnemies.Remove(this);
            ClearAllActions();
            StopAllCoroutines();
        }
        [NYI("Missing Spellcard")]
        protected override void WhenStart()
        {
            base.WhenStart();
            //SpellcardUI.WhenTimerExpire += ForceNextPhase;
        }
        private void OnEnable()
        {
            aliveEnemies.Add(this);
        }
        private void OnDisable()
        {
            aliveEnemies.Remove(this);
            ClearAllActions();
            StopAllCoroutines();
        }
        private void LateUpdate()
        {
            Think();
        }
        private void Think()
        {
            ShmupPlayer.PlayerAs(out ShmupPlayer p);
            if (IsOnScreenAndAlive)
            {
                //TryAttack();
            }
            /*if (BossMover != null)
            {
                Vector2 directionToPlayer = Vector2.down;
                if (p.IsAlive)
                {
                    directionToPlayer = (p.CurrentPosition - CurrentPosition).normalized;
                }
                BossMover.MoveUnit(this, directionToPlayer);
            }*/
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
            return base.CalculateAlive();
        }

        protected override void WhenUpdate()
        {
            base.WhenUpdate();
            BossAttackStarterLoop();
        }
    }
}
