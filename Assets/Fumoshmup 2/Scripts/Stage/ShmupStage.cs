using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Search;

namespace FumoShmup2
{
    #region Shortcuts
    public partial class ShmupStage
    {
        public bool Spawn(EnemyUnit enemy, out EnemyUnit result, Vector2 position, MoveLerpAction.LerpSettings? lerpSettings)
        {
            return StageTools.Spawn(enemy, out result, position, lerpSettings);
        }
        public Vector2 Map(float x, float y, out Vector2 mapped)
        {
            return StageTools.Map(x, y, out mapped);
        }
        public bool SpawnBoss(EnemyUnit e, out EnemyUnit result, out WaitUntil wait)
        {
            return StageTools.SpawnBoss(e, out result, out wait);
        }
        public void SectionSweep(byte loot = 255)
        {
            StageTools.SectionSweep(loot);
        }
        public void StartDialogue(DialogueStackSO d, out WaitUntil w, Action whenEndDialogue = null)
        {
            StageTools.StartDialogue(d, out w, whenEndDialogue);
        }
        protected WaitUntil WaitForTimeOrEnemyCountLessThan(float duration, int enemyCount)
        {
            return StageTools.WaitForTimeOrEnemyCountLessThan(duration, enemyCount);
        }
        public WaitUntil WaitForKill(EnemyUnit unit)
        {
            return StageTools.WaitForKill(unit);
        }
    }
    #endregion
    #region Stage Queue
    public partial class ShmupStage
    {
        public static Queue<ShmupStage> StageQueue { get; private set; }
        public static bool TryGetNextStage(out ShmupStage next)
        {
            next = null;
            if (StageQueue == null || StageQueue.Count <= 0)
            {
                return false;
            }
            next = StageQueue.Dequeue();
            return true;
        }
        public static void SetStageQueue(params ShmupStage[] stages)
        {
            if (StageQueue == null)
            {
                StageQueue = new Queue<ShmupStage>();
            }
            StageQueue.Clear();
            foreach (var item in stages)
            {
                StageQueue.Enqueue(item);
            }
        }
    }
    #endregion
    public abstract partial class ShmupStage : ScriptableObject
    {
        [System.Serializable]
        public struct SkipValues
        {
            public string skipName;
            public int skipValue;
            public bool enabled;
        }
        [field: SerializeField] public List<SkipValues> SkipEntries = new();
        protected static EnemyUnit enemyIteration;
        protected static Vector2 vec2Iteration;
        [SerializeField] ScenePairSO stageScene;
        public static bool RanStageThisFrame { get; private set; }
        [NYI("Feature")]
        public void RunStage(int skip)
        {
            IEnumerator CO_FlickRanStage()
            {
                yield return null;
                RanStageThisFrame = false;
            }
            RanStageThisFrame = true;
            Debug.Log("Run Stage : " + skip);
            //ShmupPracticeModeUI.LoadStageToPracticeMode(this);
            Dialogue.Stop();

            ProjectileRunner.TriggerSweep(0.5f, 0, false, out _);
            foreach (var item in EnemyUnit.AliveEnemies.ToArray())
                item.ForceKill();
            ProjectileRunner.TriggerSweep(0f, 0, false, out _);

            StopStage();
            GlobalCoroutineRunner.StartRoutine("Stage", StagePayload(skip));
            CO_FlickRanStage().RunRoutine();
        }
        public static void StopStage()
        {
            GlobalCoroutineRunner.StopAllOfKey("Stage");
        }
        [NYI("Feature")]
        protected static void GoNextStageOrMenu()
        {
            void RunNext(ShmupStage s)
            {
                //ShmupPracticeMode.StageSkipValue = 0;
                //s.RunStage(ShmupPracticeMode.StageSkipValue - 1);
            }
            if (TryGetNextStage(out ShmupStage stage))
            {
                if (stage.stageScene != null)
                {
                    stage.stageScene.Load(() => RunNext(stage));
                }
                else
                {
                    RunNext(stage);
                }
            }
            else
            {
                //ShmupState.EndGameAndMainMenu();
            }
        }
        protected abstract IEnumerator StagePayload(int skip);
    }
}