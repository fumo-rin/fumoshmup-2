using UnityEngine;
using rinCore;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace FumoShmup2
{

    #region Actions & Tools & Shortcuts
    public static partial class StageTools
    {
        public static bool Spawn(EnemyUnit enemy, out EnemyUnit result, Vector2 position, MoveLerpAction.LerpSettings? lerpSettings)
        {
            result = null;
            if (enemy == null)
            {
                return false;
            }
            result = enemy.Spawn2D(position);
            if (result != null && lerpSettings != null)
            {
                result.Action_MoveWithLerp(lerpSettings.Value);
            }
            return result != null;
        }
        public static Vector2 Map(float x, float y, out Vector2 mapped)
        {
            return ShmupWorldspace.MapToWorldspaceUnclamped(x, y, out mapped);
        }
        public static bool SpawnBoss(EnemyUnit e, out EnemyUnit result, out WaitUntil wait, Vector2? start = null, Vector2? end = null)
        {
            result = null;
            wait = null;
            if (e == null)
            {
                return false;
            }
            ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 1.25f, out Vector2 a);
            ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 0.7f, out Vector2 b);
            if (start != null)
                ShmupWorldspace.MapToWorldspaceUnclamped(start.Value.x, start.Value.y, out a);

            if (end != null)
                ShmupWorldspace.MapToWorldspaceUnclamped(end.Value.x, end.Value.y, out b);

            if (Spawn(e, out result, a, new(a, b, 0.85f)))
            {
                wait = WaitForKill(result);
            }
            return result != null;
        }
        public struct CaveBossInfo
        {
            public EnemyUnit BossUnit;
            public List<UnitAttack> AttackSequence;
            public int AttackLoops;
            public float AttackStall;
            public CaveBossInfo(EnemyUnit e, List<UnitAttack> attackSequence)
            {
                this.BossUnit = e;
                this.AttackSequence = attackSequence;
                this.AttackLoops = 1;
                this.AttackStall = 2f;
            }
        }
        public static bool SpawnCaveBoss(CaveBossInfo boss, out EnemyUnit result, out WaitUntil wait, Vector2? start = null, Vector2? end = null)
        {
            result = null;
            wait = null;

            if (boss.BossUnit == null)
            {
                return false;
            }
            ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 1.25f, out Vector2 a);
            ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 0.7f, out Vector2 b);
            if (start != null)
                ShmupWorldspace.MapToWorldspaceUnclamped(start.Value.x, start.Value.y, out a);

            if (end != null)
                ShmupWorldspace.MapToWorldspaceUnclamped(end.Value.x, end.Value.y, out b);

            if (Spawn(boss.BossUnit, out result, a, new(a, b, 0.85f)))
            {
                void KillAfter(WaitUntil w, EnemyUnit e)
                {
                    e.StartCoroutine(Co_Kill());
                    IEnumerator Co_Kill()
                    {
                        yield return w;
                        if (e != null)
                        {
                            e.ForceKill();
                        }
                    }
                }
                wait = WaitForKill(result);
            }
            return result != null;
        }
        public static void SectionSweep(byte loot = 255)
        {
            ProjectileRunner.TriggerSweep(0.5f, loot, false, out _);
        }
        [NYI("Feature")]
        public static void StartDialogue(DialogueStackSO d, out WaitUntil w, Action whenEndDialogue = null)
        {
            w = null;
            if (ShmupSession.SkipDialogue)
            {
                Dialogue.Stop();
                return;
            }
            if (d == null)
            {
                return;
            }
            d.StartDialogue(out w, whenEndDialogue);
        }
    }
    #endregion
    #region Wait Instructions
    public static partial class StageTools
    {
        public static int AliveEnemies => EnemyUnit.AliveEnemies.Where((FumoUnit e) => e is not TargetDummyUnit).Count();
        public static WaitUntil WaitForTimeOrEnemyCountLessThan(float duration, int enemyCount)
        {
            float startTime = Time.time;

            return new WaitUntil(() =>
            {
                bool timeElapsed = Time.time - startTime >= duration;
                bool enemiesCleared = AliveEnemies < IntExtensions.Clamp(enemyCount, 1, 99);
                return timeElapsed || enemiesCleared;
            });
        }
        public static WaitUntil WaitForKill(EnemyUnit unit)
        {
            return new WaitUntil(() =>
            {
                return unit == null || !unit.IsAlive;
            });
        }
    }
    #endregion
}