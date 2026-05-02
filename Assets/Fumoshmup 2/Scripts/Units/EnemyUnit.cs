using UnityEngine;
using rinCore;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

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
                lootChance = lootChange
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
                CreateLoot(loot, 4.5f);
                Action_ClearBossMover();
                Action_BossRecenter(0.65f);
            }
            currentPhase = false;
        }
        public void ForceNextPhase()
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
        public void Sendhit(IHit.HitPacket packet, out float damageDealt)
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
                PlayerScoring.AddScoreWithoutMultiplier(damage * 250d, "Enemy Damage", false);
                damageDealt = damage.Min(CurrentHealth);
                StartNewHealth(CurrentHealth - damage, CurrentMaxHealth);
            }
        }
        public void ForceKill()
        {
            if (this is TargetDummyUnit)
            {
                return;
            }
            if (this == null)
            {
                return;
            }
            if (IsBoss)
            {
                Spellcard.FailSpell();
                Spellcard.EndSpell();
            }
            CurrentHealth = 0f;
            TriggerKillEvent(true);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        public void KillWithLoot()
        {
            if (sealRadius > 0.1f) ProjectileRunner.SealBullets(CurrentPosition, this, sealRadius, 255, out _);
            CreateLoot(LootCount, 2.5f);
            TriggerKillEvent(false);
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
                    CreateLootOnPosition(position, 20, 0.35f);
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
        private void CreateLoot(int lootCount, float areaSize = 1f)
        {
            CreateLootOnPosition(CurrentPosition, lootCount, areaSize);
        }
        private void CreateLootOnPosition(Vector2 position, int lootCount, float areaSize = 1f)
        {
            for (int i = 0; i < IntExtensions.Clamp(lootCount, 0, 10000); i++)
                PointItemRunner.Create(position + (Random.insideUnitCircle.normalized * 0.75f.Spread(50f) * areaSize));
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
                        leashPosition = e.CurrentPosition + Random.insideUnitCircle;
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
                item.SendHit(HitPacket.Create(item.CurrentPosition, new(null, 1000000, 1f)), out float hit);
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

        public override bool HasIframes => throw new System.NotImplementedException();
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
        }
    }
}
