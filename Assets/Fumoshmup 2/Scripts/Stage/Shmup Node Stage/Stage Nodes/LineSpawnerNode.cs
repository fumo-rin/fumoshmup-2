using System.Collections;
using UnityEngine;
using UnityEngine.Search;
using rinCore;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Search;
using static rinCore.EF_Utility;
#endif

namespace FumoShmup2
{
    public class LineSpawnerNode : StageNode, IStageNodeRunable, IStageNodeModable
    {
        [SearchContext("t:EnemyUnit")]
        public EnemyUnit toSpawn;
        public EnemyModifierNode storedModifier;
        public List<UnitAttack> unitAttacks = new();
        public EnemyModifierNode EnemyMod
        {
            get { return storedModifier; }
            set { storedModifier = value; }
        }
        public Vector2 start = new(-0.25f, 1.05f), startEnd = new(0.45f, 1.3f);
        public Vector2 target = new(0.15f, 0.65f), targetEnd = new(0.55f, 0.8f);
        public Vector2 exit = new(-0.35f, 0.75f), exitEnd = new(-0.35f, 0.65f);
        public int EnemyCount = 6;
        public float WaitBetweenEachSpawn = 0.1f;
        public float ExitDelay = 5f;
        public float ExitDuration = 1.25f;
        public float EntryDuration = 0.75f;
        public bool IsLinkable => true;
        public IStageNodeModifier linkedModifier { get; set; }
        public bool RunSeperately => runSeperately;
        public bool runSeperately;
        public float RunDuration => WaitBetweenEachSpawn * EnemyCount;
        public bool WasModifiedByModifier { get; set; } = false;
        public bool HasIndicator = false;
        public bool Sealing = false;
        public float SealingRadius = 0f;
        public bool SweepOverride = false;
        public float SweepDuration = 0f;
        public int SweepLootChance = 255;

        [SerializeReference] public List<UnitAttack> attackLoop = new();
        public int attackLoops = 3;
        public float loopAddedDelay = 0.15f;
        public float attackStall = 2f;

        public IEnumerator RunNode()
        {
            if (toSpawn == null)
            {
                Debug.LogError($"[{this.GetType().ToString()}]Missing Enemy for : " + this.name);
                yield return null;
                yield break;
            }
            IEnumerator Spawn()
            {
                for (int i = 0; i < EnemyCount; i++)
                {
                    float t = (EnemyCount == 1) ? 0.5f : i / (float)(EnemyCount - 1);
                    ShmupWorldspace.MapToWorldspaceUnclamped(Mathf.Lerp(start.x, startEnd.x, t), Mathf.Lerp(start.y, startEnd.y, t), out Vector2 spawnPos);
                    ShmupWorldspace.MapToWorldspaceUnclamped(Mathf.Lerp(target.x, targetEnd.x, t), Mathf.Lerp(target.y, targetEnd.y, t), out Vector2 targetPos);
                    ShmupWorldspace.MapToWorldspaceUnclamped(Mathf.Lerp(exit.x, exitEnd.x, t), Mathf.Lerp(exit.y, exitEnd.y, t), out Vector2 exitPos);

                    var path = new List<Vector2> { spawnPos, targetPos, exitPos };
                    StageTools.Spawn(toSpawn, out EnemyUnit result, spawnPos, new(path[0], path[1], EntryDuration));
                    if (result != null)
                    {
                        result.SetBaseAttacks(new EnemyUnit.AttackComponent(attackLoops, loopAddedDelay, attackLoop.ToArray()));
                        result.StallAttackLoop(attackStall);
                        if (HasIndicator) EnemyIndicator.TrackUnit(result);
                    }
                    if (EnemyMod is EnemyModifierNode mod)
                    {
                        mod.ModifyEnemy(result);
                    }
                    if (SweepOverride) result.SetSweepOverride(SweepDuration, ((byte)SweepLootChance));
                    if (Sealing) result.SetSealRadius(SealingRadius);
                    result.Action_ExitAfter(new(ExitDelay, ExitDuration, exitPos));
                    yield return WaitBetweenEachSpawn.WaitForSeconds();
                }
            }
            if (RunSeperately)
            {
                StageRoutines.StartRoutine("Seperate Line Spawner Call", Spawn(), false);
                yield break;
            }
            yield return Spawn();
        }
        protected override Vector2 BuildSize() => new(350f, 360f);
        private void FlipXPositions()
        {
            Vector2 FlipX(Vector2 v) => new(1f - v.x, v.y);

            start = FlipX(start);
            startEnd = FlipX(startEnd);
            target = FlipX(target);
            targetEnd = FlipX(targetEnd);
            exit = FlipX(exit);
            exitEnd = FlipX(exitEnd);

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            RecordUndo("Modify Node Value");
            //toSpawn = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "To Spawn", toSpawn);

            var listOfEnemies = stage.enemyTable;
            toSpawn = EF_ListDropdown(Helper_BuildFieldRect(rect, ref index), "Enemy", listOfEnemies, toSpawn, enemy => enemy != null ? enemy.name : "(Missing)");
            RecordUndo("Modify Node Value");
            runSeperately = EF_BoolField(Helper_BuildFieldRect(rect, ref index), nameof(runSeperately), runSeperately);
            RecordUndo("Modify Node Value");
            EnemyCount = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(EnemyCount), EnemyCount, 1, 25);
            RecordUndo("Modify Node Value");
            WaitBetweenEachSpawn = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(WaitBetweenEachSpawn), WaitBetweenEachSpawn, 0f, 5f);
            RecordUndo("Modify Node Value");
            ExitDelay = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(ExitDelay), ExitDelay, 0.05f, 20f);
            RecordUndo("Modify Node Value");
            ExitDuration = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(ExitDuration), ExitDuration, 0.35f, 10f);
            RecordUndo("Modify Node Value");
            EntryDuration = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(EntryDuration), EntryDuration, 0.05f, 10f);

            RecordUndo("Modify Node Value");

            HasIndicator = EF_BoolField(Helper_BuildFieldRect(rect, ref index), nameof(HasIndicator), HasIndicator);

            Sealing = EF_BoolField(Helper_BuildFieldRect(rect, ref index), nameof(Sealing), Sealing);

            if (Sealing)
            {
                SealingRadius = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(SealingRadius), SealingRadius, 0.25f, 20f);
            }

            SweepOverride = EF_BoolField(Helper_BuildFieldRect(rect, ref index), nameof(SweepOverride), SweepOverride);
            if (SweepOverride)
            {
                SweepDuration = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(SweepDuration), SweepDuration, 0.05f, 1.5f);
                SweepLootChance = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(SweepLootChance), SweepLootChance, 0, 255);
            }

            RecordUndo("Modify Node Value");
            if (EF_Button(Helper_BuildFieldRect(rect, ref index), "Flip X"))
            {
                FlipXPositions();
            }

            attackLoops = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Attack Loops", attackLoops, 1, 15);
            attackStall = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Attack Stall time", attackStall, 0.05f, 9f);
            loopAddedDelay = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Delay Between Loops", loopAddedDelay, 0f, 6f);
            EF_TypeDropdownList<UnitAttack>(Helper_BuildFieldRect(rect, ref index), "Attack Loop", nameof(attackLoop), unityBackingObject);


            if (selected)
            {
                RecordUndo("Modify Node Value");
                start = EF_ShmupSpace(start, ColorHelper.PastelGreen, nameof(start));
                RecordUndo("Modify Node Value");
                startEnd = EF_ShmupSpace(startEnd, ColorHelper.PastelGreen, nameof(startEnd));
                RecordUndo("Modify Node Value");
                target = EF_ShmupSpace(target, ColorHelper.PastelCyan, nameof(target));
                RecordUndo("Modify Node Value");
                targetEnd = EF_ShmupSpace(targetEnd, ColorHelper.PastelCyan, nameof(targetEnd));
                RecordUndo("Modify Node Value");
                exit = EF_ShmupSpace(exit, ColorHelper.PastelRed, nameof(exit));
                RecordUndo("Modify Node Value");
                exitEnd = EF_ShmupSpace(exitEnd, ColorHelper.PastelRed, nameof(exitEnd));
            }
#endif
        }
    }
}