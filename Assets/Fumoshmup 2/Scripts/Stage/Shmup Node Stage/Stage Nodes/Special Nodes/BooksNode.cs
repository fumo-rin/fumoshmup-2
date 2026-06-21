using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Search;
using static rinCore.EF_Utility;
#endif
namespace FumoShmup2
{
    public class BooksNode : StageNode, IStageNodeRunable
    {
        public bool RunSeperately => false;
        public float RunDuration => 8f;
        public bool WasModifiedByModifier { get; set; } = false;
        public bool IsLinkable => false;
        public EnemyUnit toSpawn;
        [SerializeReference] public List<UnitAttack> attackLoop = new();
        public int attackLoops = 15;

        public float loopAddedDelay = 0.03f;
        public float attackStall = 0.75f;
        public float ExitDelay = 15f;
        public float ExitDuration = 1.25f;

        public float EnemyHealth = 375;

        bool collapsedEditable;

        public IEnumerator RunNode()
        {
            HashSet<EnemyUnit> Spawned = new();
            WasModifiedByModifier = false;
            if (toSpawn == null)
            {
                Debug.LogError($"[{this.GetType().ToString()}]Missing Enemy for : " + this.name);
                yield return null;
                yield break;
            }
            IEnumerator Spawn()
            {
                int EnemyCount = 10;
                for (int i = 0; i < EnemyCount; i++)
                {
                    Spawned.RemoveWhere(x => x == null || !x.IsAlive);
                    if (Spawned.Count > 6)
                    {
                        yield return 0.85f.WaitForSeconds();
                        continue;
                    }
                    Vector2 start = i % 2 == 0 ? new(0.25f, 1.35f) : new(0.75f, 1.35f);
                    Vector2 target = i % 2 == 0 ? new(RNG.FloatRange(0.15f, 0.45f), RNG.FloatRange(0.6f, 0.85f)) : new(RNG.FloatRange(0.55f, 0.85f), RNG.FloatRange(0.6f, 0.85f));
                    Vector2 end = i % 2 == 0 ? new(-0.3f, RNG.FloatRange(0.6f, 0.85f) - 0.1f) : new(1.3f, RNG.FloatRange(0.6f, 0.85f) - 0.1f);
                    ShmupWorldspace.MapToWorldspaceUnclamped(start.x, start.y, out Vector2 spawnPos);
                    ShmupWorldspace.MapToWorldspaceUnclamped(target.x, target.y, out Vector2 targetPos);
                    ShmupWorldspace.MapToWorldspaceUnclamped(end.x, end.y, out Vector2 exitPos);

                    var path = new List<Vector2> { spawnPos, targetPos, exitPos };
                    StageTools.Spawn(toSpawn, out EnemyUnit result, spawnPos, new(path[0], path[1], 0.6f));
                    if (result != null)
                    {
                        result.SetBaseAttacks(new EnemyUnit.AttackComponent(attackLoops, loopAddedDelay, attackLoop.ToArray()));
                        result.StallAttackLoop(attackStall);
                    }
                    result.SetSealRadius(5f);
                    result.Action_ExitAfter(new(ExitDelay, ExitDuration, exitPos));
                    result.StartNewHealth(EnemyHealth, EnemyHealth);
                    Spawned.Add(result);
                    yield return 0.85f.WaitForSeconds();
                }
                yield return 1f.WaitForSeconds();
                foreach (var result in Spawned.ToList())
                {
                    result.ForceKill();
                }
                ProjectileRunner.TriggerSweep(0.05f, 255, false, out _);
            }
            yield return Spawn();
        }

        protected override Vector2 BuildSize()
        {
            return new(400f, 180f);
        }

        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {

#if UNITY_EDITOR
            int index = 0;

            var listOfEnemies = stage.enemyTable;
            toSpawn = EF_ListDropdown(Helper_BuildFieldRect(rect, ref index), "Enemy", listOfEnemies, toSpawn, enemy => enemy != null ? enemy.name : "(Missing)");

            EnemyHealth = EF_NumberField<float>(Helper_BuildFieldRect(rect, ref index), "Enemy Health", EnemyHealth);
            attackLoops = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Attack Loops", attackLoops, 1, 15);
            attackStall = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Attack Stall time", attackStall, 0.05f, 9f);
            loopAddedDelay = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Delay Between Loops", loopAddedDelay, 0f, 6f);
            EF_TypeDropdownList<UnitAttack>(Helper_BuildFieldRect(rect, ref index), "Attack Loop", nameof(attackLoop), unityBackingObject);
#endif
        }
    }
}
