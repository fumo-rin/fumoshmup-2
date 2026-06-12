using NUnit.Framework.Api;
using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static rinCore.EF_Utility;

namespace FumoShmup2
{
    public class SequenceSpawnerNode : StageNode, IStageNodeModable, IStageNodeRunable
    {
        int EditIndex = 0;
        bool editSequence;
        List<SequenceEntry> entries = new();
        [System.Serializable]
        public struct SequenceEntry
        {
            public Vector2 a, b, c;
            public Vector2 sequencePostDelayVec2;
            public float SequencePostDelay => sequencePostDelayVec2.x * sequencePostDelayVec2.y.Multiply(10f);
        }




        public EnemyModifierNode storedModifier;
        public EnemyModifierNode EnemyMod
        {
            get { return storedModifier; }
            set { storedModifier = value; }
        }
        public EnemyUnit spawnedEnemy;
        public bool runSeperately;
        public bool RunSeperately => runSeperately;
        public float runDuration
        {
            get
            {
                float duration = 0f;
                foreach (var item in entries)
                {
                    duration += item.SequencePostDelay;
                }
                return duration;
            }
        }
        public float RunDuration => runDuration;
        public bool WasModifiedByModifier { get; set; } = false;
        public bool IsLinkable => true;

        public float PostWait = 0.15f;
        public float ExitDelay = 5f;
        public float ExitDuration = 1.25f;
        public float EntryDuration = 0.75f;

        [SerializeReference] public List<UnitAttack> attackLoop = new();
        public int attackLoops = 3;
        public float loopAddedDelay = 0.15f;
        public float attackStall = 2f;

        public bool HasIndicator = false;
        public bool Sealing = false;
        public float SealingRadius = 0f;
        public bool SweepOverride = false;
        public float SweepDuration = 0f;
        public int SweepLootChance = 255;
        public IEnumerator RunNode()
        {
            WasModifiedByModifier = false;
            if (spawnedEnemy == null)
            {
                Debug.LogError($"[{this.GetType().ToString()}]Missing Enemy for : " + this.name);
                yield return null;
                yield break;
            }
            IEnumerator Spawn()
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    SequenceEntry entry = entries[i];
                    ShmupWorldspace.MapToWorldspaceUnclamped(entry.a.x, entry.a.y, out Vector2 spawnPos);
                    ShmupWorldspace.MapToWorldspaceUnclamped(entry.b.x, entry.b.y, out Vector2 targetPos);
                    ShmupWorldspace.MapToWorldspaceUnclamped(entry.c.x, entry.c.y, out Vector2 exitPos);

                    var path = new List<Vector2> { spawnPos, targetPos, exitPos };
                    StageTools.Spawn(spawnedEnemy, out EnemyUnit result, spawnPos, new(path[0], path[1], EntryDuration));
                    if (result != null)
                    {
                        result.SetBaseAttacks(new EnemyUnit.AttackComponent(attackLoops, loopAddedDelay, attackLoop.ToArray()));
                        result.StallAttackLoop(attackStall);
                        if (HasIndicator) EnemyIndicator.TrackUnit(result);
                    }
                    if (EnemyMod is EnemyModifierNode mod && mod.IsEnabled)
                    {
                        mod.ModifyEnemy(result);
                    }
                    if (SweepOverride) result.SetSweepOverride(SweepDuration, ((byte)SweepLootChance));
                    if (Sealing) result.SetSealRadius(SealingRadius);
                    result.Action_ExitAfter(new(ExitDelay, ExitDuration, exitPos));
                    yield return entry.SequencePostDelay.WaitForSeconds();
                }
                if (!WasModifiedByModifier)
                    yield return PostWait.WaitForSeconds();
            }
            if (RunSeperately)
            {
                GlobalCoroutineRunner.StartRoutine("Stage Extras", Spawn(), false);
                yield break;
            }
            yield return Spawn();
        }
        private void EditCurrentIndex(in bool selected)
        {
            if (selected && !editSequence)
            {
                int iteration = 0;
                foreach (var item in entries)
                {
                    SequenceEntry entry = entries[iteration];
                    EF_ShmupSpace(entry.a, ColorHelper.PastelGreen, "");
                    EF_ShmupSpace(entry.b, ColorHelper.PastelCyan, "");
                    EF_ShmupSpace(entry.c, ColorHelper.PastelRed, "");
                    iteration++;
                }
                return;
            }
            if (selected && entries.Count > 0)
            {
                SequenceEntry entry = entries[EditIndex % entries.Count];
                entry.a = EF_ShmupSpace(entry.a, ColorHelper.PastelGreen, nameof(entry.a) + $" {entry.a.ToString("F2")}");
                entry.b = EF_ShmupSpace(entry.b, ColorHelper.PastelCyan, nameof(entry.b) + $" {entry.b.ToString("F2")}");
                entry.c = EF_ShmupSpace(entry.c, ColorHelper.PastelRed, nameof(entry.c) + $" {entry.c.ToString("F2")}");

                entry.sequencePostDelayVec2 = EF_ShmupSpace(entry.sequencePostDelayVec2, ColorHelper.PastelPurple, entry.SequencePostDelay.ToString("F2") + $" post delay");
                entries[EditIndex % entries.Count] = entry;
            }
            else if (editSequence && entries.Count > 0)
            {
                editSequence = false;
                EditIndex = 0;
            }
        }
        private void FlipXPositions()
        {
            Vector2 FlipX(Vector2 v) => new(1f - v.x, v.y);
            for (int i = 0; i < entries.Count; i++)
            {
                SequenceEntry entry = entries[i];
                entry.a = FlipX(entry.a);
                entry.b = FlipX(entry.b);
                entry.c = FlipX(entry.c);
                entries[i] = entry;
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
        protected override Vector2 BuildSize()
        {
            return new(450f, 450f);
        }
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
            title = nameof(ShmupNodeStage).SpaceByCapitals() + $" {entries.Count} steps";
            int index = 0;
            #region Edit Sequence
#if UNITY_EDITOR
            if (!editSequence && EF_Button(Helper_BuildFieldRect(rect, ref index, 1), "Start Edit Sequence"))
            {
                editSequence = true;
                EditIndex = 0;
            }
            if (editSequence && EF_Button(Helper_BuildFieldRect(rect, ref index, 1), "End Edit Sequence"))
            {
                editSequence = false;
            }
            if (editSequence)
            {
                if (EF_Button(Helper_BuildFieldRect(rect, ref index, 1), "Add Sequence Step"))
                {
                    entries.Add(new()
                    {
                        a = new(0.5f, 1.3f),
                        b = new(0.5f, 0.7f),
                        c = new(-0.25f, 0.6f),
                        sequencePostDelayVec2 = new(0.25f, 0.4f)
                    });
                    stage.Dirty();
                }
                if (EF_Button(Helper_BuildFieldRect(rect, ref index, 1), "Remove Current Step"))
                {
                    if (entries.TryGetIndex(EditIndex, out SequenceEntry result))
                    {
                        entries.Remove(result);
                        stage.Dirty();
                    }
                }
                int maxIndex = entries.Count;
                if (EditIndex > maxIndex)
                {
                    editSequence = false;
                }
                if (EditIndex + 1 < maxIndex && editSequence && EF_Button(Helper_BuildFieldRect(rect, ref index, 1), "Next Sequence Step"))
                {
                    EditIndex = EditIndex + 1;
                }
            }
#endif
            #endregion
            #region Draw
#if UNITY_EDITOR
            RecordUndo("Modify Node Value");

            var listOfEnemies = stage.enemyTable;
            spawnedEnemy = EF_ListDropdown(Helper_BuildFieldRect(rect, ref index), "Enemy", listOfEnemies, spawnedEnemy, enemy => enemy != null ? enemy.name : "(Missing)");
            RecordUndo("Modify Node Value");
            runSeperately = EF_BoolField(Helper_BuildFieldRect(rect, ref index), nameof(runSeperately), runSeperately);
            PostWait = EF_Slider(Helper_BuildFieldRect(rect, ref index), nameof(PostWait), PostWait, 0f, 1.5f);
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

            EditCurrentIndex(selected);
#endif
            #endregion
        }
    }
}
