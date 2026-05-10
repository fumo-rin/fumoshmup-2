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
    public class BossNode : StageNode, IStageNodeRunable
    {
        public EnemyUnit toSpawn;
        public DialogueStackSO bossStartDialogue, bossKillDialogue;
        public MusicWrapper bossMusic;
        public Vector2 start = new(0.5f, 1.25f), end = new(0.5f, 0.7f);
        public IStageNodeModifier linkedModifier { get; set; }
        public bool RunSeperately => false;
        public float RunDuration => 0f;
        public bool IsLinkable => false;
        public bool WasModifiedByModifier { get; set; } = false;

        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            var listOfEnemies = stage.enemyTable;
            RecordUndo("Modify Node Value");
            toSpawn = EF_ListDropdown(Helper_BuildFieldRect(rect, ref index), "Enemy", listOfEnemies, toSpawn, enemy => enemy != null ? enemy.name : "(Missing)");
            RecordUndo("Modify Node Value");
            bossStartDialogue = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Dialogue Start", bossStartDialogue);
            RecordUndo("Modify Node Value");
            bossKillDialogue = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Dialogue End", bossKillDialogue);
            RecordUndo("Modify Node Value");
            bossMusic = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Boss Music", bossMusic);
            RecordUndo("Modify Node Value");
            if (selected)
            {
                RecordUndo("Modify Node Value");
                start = EF_ShmupSpace(start, ColorHelper.PastelGreen, nameof(start));
                RecordUndo("Modify Node Value");
                end = EF_ShmupSpace(end, ColorHelper.PastelGreen, nameof(end));
            }
#endif
        }
        public IEnumerator RunNode()
        {
            yield return StageTools.WaitForTimeOrEnemyCountLessThan(1f, 1);
            EnemyUnit.Despawn(EnemyUnit.AliveEnemies.ToList());
            StageTools.SectionSweep(255);
            yield return 0.35f.WaitForSeconds();
            StageTools.StartDialogue(bossStartDialogue, out WaitUntil bdWait);
            yield return bdWait;
            if (bossMusic != null) bossMusic.Play();
            yield return 0.25f.WaitForSeconds();
            if (StageTools.SpawnBoss(toSpawn, out EnemyUnit enemyIteration, out WaitUntil bossWait, start, end))
            {
                EnemyIndicator.TrackUnit(enemyIteration);
                yield return bossWait;
            }
            StageTools.StartDialogue(bossKillDialogue, out WaitUntil bdKillWait);
            yield return bdKillWait;
        }
        protected override Vector2 BuildSize() => new(300f, 150f);
    }
}