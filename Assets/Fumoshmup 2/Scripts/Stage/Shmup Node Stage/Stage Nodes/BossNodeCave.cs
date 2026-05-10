using System.Collections;
using UnityEngine;
using UnityEngine.Search;
using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Search;
using static rinCore.EF_Utility;
#endif

namespace FumoShmup2
{
    public class BossNodeCave : StageNode, IStageNodeRunable
    {
        public EnemyUnit toSpawn;
        public DialogueStackSO bossStartDialogue, bossKillDialogue;
        public MusicWrapper bossMusic;
        [SerializeReference] public List<UnitAttack> bossAttacks = new();
        public int attackLoops = 1;
        public float attackStall = 2f;
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
            toSpawn = EF_ListDropdown(Helper_BuildFieldRect(rect, ref index), "Enemy", listOfEnemies, toSpawn, enemy => enemy != null ? enemy.name : "(Missing)");
            if (selected)
            {
                start = EF_ShmupSpace(start, ColorHelper.PastelGreen, "Start");
                end = EF_ShmupSpace(end, ColorHelper.PastelRed, "End");
            }
            bossStartDialogue = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Dialogue Start", bossStartDialogue);
            bossKillDialogue = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Dialogue End", bossKillDialogue);
            bossMusic = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Boss Music", bossMusic);

            attackLoops = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Attack Loops", attackLoops, 1, 99);
            attackStall = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Attack Stall time", attackStall, 0.05f, 9f);

            EF_TypeDropdownList<UnitAttack>(Helper_BuildFieldRect(rect, ref index), "Boss Attacks", nameof(bossAttacks), unityBackingObject);
#endif
        }
        public IEnumerator RunNode()
        {
            if (toSpawn == null)
            {
                Debug.LogError($"[{this.GetType().ToString()}]Missing Enemy for : " + this.name);
                yield return null;
                yield break;
            }
            yield return StageTools.WaitForTimeOrEnemyCountLessThan(1f, 1);
            EnemyUnit.Despawn(EnemyUnit.AliveEnemies.ToList());
            StageTools.SectionSweep(255);
            yield return 0.35f.WaitForSeconds();
            StageTools.StartDialogue(bossStartDialogue, out WaitUntil bdWait);
            yield return bdWait;
            if (bossMusic != null) bossMusic.Play();
            yield return 0.25f.WaitForSeconds();
            if (StageTools.SpawnCaveBoss(new(toSpawn, bossAttacks) { AttackLoops = this.attackLoops, AttackStall = this.attackStall }, out EnemyUnit enemyIteration, out WaitUntil bossWait, start, end))
            {
                enemyIteration.SetSweepOverride(0.75f, 255);
                yield return bossWait;
            }
            StageTools.StartDialogue(bossKillDialogue, out WaitUntil bdKillWait);
            yield return bdKillWait;
        }
        protected override Vector2 BuildSize() =>
#if UNITY_EDITOR
            new Vector2(300f, 180f);
#else
        new Vector2(0f,0f);
#endif
    }
}